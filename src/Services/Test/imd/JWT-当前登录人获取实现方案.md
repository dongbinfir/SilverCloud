# JWT 当前登录人获取实现方案（基于现有项目架构）

日期：2026-03-23  
适用项目：SilverCloud / User Service

## 1. 现状分析（你项目现在怎么做）

从当前代码看：

1. JWT 认证配置已完整存在
- `WebAPI/Program.cs` 已配置 `AddAuthentication().AddJwtBearer(...)`
- 已启用 `app.UseAuthentication(); app.UseAuthorization();`

2. JWT 中已经写入用户标识
- `Infrastructure/Identity/TokenService.cs` 在生成 AccessToken 时写入：
  - `ClaimTypes.NameIdentifier` = `userId`
  - `sub` = `userId`

3. 当前读取方式是“分散式”
- `WebAPI/Controllers/AuthsController.cs` 的登出处使用：
  - `User.FindFirst(ClaimTypes.NameIdentifier)`

结论：
- 你已经具备从 JWT 获取当前登录人的基础能力。
- 当前缺点是“每个 Controller/Handler 手动解析 Claim”，重复且容易出错。

---

## 2. 目标

构建统一的“当前用户上下文”能力，让应用层/控制器都能通过接口拿到当前登录人信息。

目标能力：

1. 统一读取 UserId（必需）
2. 可扩展读取 Email、Phone、Jti（可选）
3. 在未登录或 Claim 缺失时有一致行为（返回 null/抛业务异常）
4. 兼容当前 JWT 结构，不改现有签发逻辑

---

## 3. 推荐落地方式（符合你现有分层）

采用三层实现：

1. Application 层定义接口（依赖倒置）
2. WebAPI/Infrastructure 提供 HttpContext + Claims 实现
3. 在 Controller/CommandHandler 中注入接口使用

这样做的好处：
- 应用层不依赖 ASP.NET Core 具体对象
- 解析规则集中管理
- 后续替换认证方式时改动最小

---

## 4. 具体实施步骤

## 第一步：定义当前用户接口（Application）

建议新增文件：
- `src/Services/User/src/Application/Common/Interfaces/ICurrentUserService.cs`

建议接口：

```csharp
namespace User.Application.Common.Interfaces
{
    public interface ICurrentUserService
    {
        int? UserId { get; }
        string? Email { get; }
        string? PhoneNumber { get; }
        bool IsAuthenticated { get; }
    }
}
```

说明：
- `int? UserId` 用可空，便于处理匿名请求。
- `IsAuthenticated` 让调用方可先判断登录状态。

---

## 第二步：实现接口（WebAPI 或 Infrastructure）

建议新增文件：
- `src/Services/User/src/WebAPI/Services/CurrentUserService.cs`

建议实现：

```csharp
using System.Security.Claims;
using User.Application.Common.Interfaces;

namespace WebAPI.Services
{
    public class CurrentUserService : ICurrentUserService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CurrentUserService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        private ClaimsPrincipal? User => _httpContextAccessor.HttpContext?.User;

        public bool IsAuthenticated => User?.Identity?.IsAuthenticated == true;

        public int? UserId
        {
            get
            {
                var value = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                return int.TryParse(value, out var id) ? id : null;
            }
        }

        public string? Email => User?.FindFirst(ClaimTypes.Email)?.Value;

        public string? PhoneNumber => User?.FindFirst(ClaimTypes.MobilePhone)?.Value;
    }
}
```

说明：
- 与你 `TokenService` 的 Claim 写入方式完全匹配。
- 不直接抛异常，保持“读取服务”职责单一。

---

## 第三步：注册 DI

在 `WebAPI/Program.cs` 增加：

```csharp
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
```

注意：
- 你已经有 `AddHttpContextAccessor()`，可直接复用。

---

## 第四步：在 Controller 里替换手动解析

以 `AuthsController.Logout` 为例：

当前：
```csharp
var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
```

建议改为：
```csharp
var userId = _currentUserService.UserId;
if (userId is null)
{
    return Unauthorized(new { message = "未登录或 Token 无效" });
}
```

然后传 `userId.Value` 给 command。

---

## 第五步：按需下沉到 Application 命令处理器

如果你希望 CommandHandler 也能直接拿当前用户（减少 Controller 传参），可在 Handler 注入 `ICurrentUserService`。

建议策略：

1. 与当前风格一致（最小改动）
- 仍在 Controller 提取 userId 并通过 command 参数传入。

2. 进阶方案（更整洁）
- Command 不再带 UserId
- Handler 内通过 `ICurrentUserService.UserId` 获取

当前建议先走策略 1，避免一次改动过大。

---

## 5. 错误处理与安全建议

1. 对需要登录的接口统一 `[Authorize]`
2. 不要对缺失 UserId 使用默认值 0 继续业务
3. 在 CurrentUserService 内只做“读取”，不要掺杂业务逻辑
4. 如果后续网关会改 ClaimType，可在一个地方集中适配

---

## 6. 验收清单

1. 带有效 JWT 调用受保护接口，`UserId` 可正确获取
2. 不带 JWT 调用受保护接口，返回 401
3. Token 缺 NameIdentifier Claim 时，业务可返回明确错误
4. `AuthsController` 不再手写 `FindFirst(...)`

---

## 7. 与你当前架构的兼容性结论

这个方案与现有代码高度兼容：

1. 不影响你已上线的 JWT 签发逻辑
2. 不影响 MongoDB RefreshToken 逻辑
3. 只新增一个接口和一个实现，改造成本低
4. 可分阶段替换已有 Controller 的手工 Claim 解析

---

## 8. 可选增强（后续）

1. 增加 `GetRequiredUserId()` 扩展方法（缺失即抛统一异常）
2. 在日志中附带 `UserId/Jti` 便于排障
3. 增加集成测试：验证不同 Token 场景下 `CurrentUserService` 行为
