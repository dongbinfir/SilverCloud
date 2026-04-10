# JWT 认证系统实现指南

## 📋 目录

- [概述](#概述)
- [架构说明](#架构说明)
- [实现步骤](#实现步骤)
- [代码文件清单](#代码文件清单)
- [测试方法](#测试方法)
- [常见问题](#常见问题)
- [最佳实践](#最佳实践)

---

## 概述

本项目使用 **JWT (JSON Web Token)** 实现用户认证系统。JWT 是一种开放标准 (RFC 7519)，用于在各方之间安全地传输信息。

### 为什么使用 JWT？

- ✅ **无状态**：服务器不需要存储会话信息
- ✅ **跨域支持**：适合微服务和分布式系统
- ✅ **性能优越**：减少数据库查询
- ✅ **安全性高**：使用数字签名防止篡改

### 技术栈

- **.NET 8.0** - 主框架
- **BCrypt.Net-Next** - 密码哈希
- **System.IdentityModel.Tokens.Jwt** - JWT 处理
- **Clean Architecture** - 项目架构

---

## 架构说明

### 项目分层结构

```
src/Services/User/
├── Domain/              # 领域层（实体、值对象）
├── Application/         # 应用层（CQRS、DTO、接口）
│   └── Common/
│       └── Interfaces/  # 服务接口定义
├── Infrastructure/       # 基础设施层（实现、数据访问）
│   └── Services/        # 服务实现
└── WebAPI/              # API 层（控制器、启动配置）
```

### 认证流程

```
┌─────────┐                ┌─────────┐                ┌──────────┐
│  前端   │                │   API   │                │  数据库  │
└────┬────┘                └────┬────┘                └────┬─────┘
     │                          │                          │
     │  1. POST /Login          │                          │
     │  {email, password} ──────>│                          │
     │                          │                          │
     │                          │  2. 查询用户             │
     │                          │ ────────────────────────>│
     │                          │                          │
     │                          │  3. 返回用户信息         │
     │                          │ <────────────────────────│
     │                          │                          │
     │                          │  4. 验证密码（BCrypt）   │
     │                          │                          │
     │                          │  5. 生成 JWT Token       │
     │                          │                          │
     │  6. 返回 Token           │                          │
     │  <───────────────────────│                          │
     │                          │                          │
     │  7. GET /me              │                          │
     │  Authorization: Bearer   │                          │
     │  TOKEN ─────────────────>│                          │
     │                          │                          │
     │                          │  8. 验证 Token           │
     │                          │                          │
     │  9. 返回用户数据         │                          │
     │  <───────────────────────│                          │
```

---

## 实现步骤

### 第一步：安装 NuGet 包

#### WebAPI 项目

```bash
cd src/Services/User/src/WebAPI
dotnet add package Microsoft.AspNetCore.Authentication.JwtBearer --version 8.0.11
dotnet add package System.IdentityModel.Tokens.Jwt --version 8.3.1
dotnet add package Swashbuckle.AspNetCore --version 6.9.0
```

#### Infrastructure 项目

```bash
cd src/Services/User/src/Infrastructure
dotnet add package BCrypt.Net-Next --version 4.0.3
```

---

### 第二步：配置 JWT 设置

修改 `WebAPI/appsettings.json`：

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "Jwt": {
    "Key": "CraftCaseSecretKeyForJWTTokenGeneration2024MustBeLongEnough",
    "Issuer": "CraftCase",
    "Audience": "CraftCaseAdmin",
    "ExpiryMinutes": 60
  }
}
```

**配置说明：**

| 参数 | 说明 | 建议值 |
|------|------|--------|
| `Key` | JWT 签名密钥 | 至少 32 字符，生产环境使用环境变量 |
| `Issuer` | 签发者 | 应用名称或域名 |
| `Audience` | 受众 | 客户端名称 |
| `ExpiryMinutes` | 过期时间 | 15-60 分钟 |

---

### 第三步：定义服务接口

在 `Application/Common/Interfaces/` 创建接口：

#### IPasswordHasher.cs

```csharp
namespace User.Application.Common.Interfaces;

public interface IPasswordHasher
{
    /// <summary>
    /// 对密码进行哈希处理
    /// </summary>
    string HashPassword(string password);

    /// <summary>
    /// 验证密码是否匹配哈希值
    /// </summary>
    bool VerifyPassword(string password, string hashedPassword);
}
```

#### IJwtTokenService.cs

```csharp
using System.Security.Claims;

namespace User.Application.Common.Interfaces;

public interface IJwtTokenService
{
    /// <summary>
    /// 生成 JWT Token
    /// </summary>
    string GenerateToken(int userId, string email);

    /// <summary>
    /// 验证 JWT Token
    /// </summary>
    ClaimsPrincipal? ValidateToken(string token);
}
```

---

### 第四步：实现服务

在 `Infrastructure/Services/` 创建实现：

#### PasswordHasher.cs

```csharp
using BCrypt.Net;
using User.Application.Common.Interfaces;

namespace User.Infrastructure.Services;

public class PasswordHasher : IPasswordHasher
{
    public string HashPassword(string password)
    {
        // 自动生成盐值并哈希
        return BCrypt.Net.BCrypt.HashPassword(password);
    }

    public bool VerifyPassword(string password, string hashedPassword)
    {
        // 验证密码
        return BCrypt.Net.BCrypt.Verify(password, hashedPassword);
    }
}
```

#### JwtTokenService.cs

```csharp
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using User.Application.Common.Interfaces;

namespace User.Infrastructure.Services;

public class JwtTokenService : IJwtTokenService
{
    private readonly string _key;
    private readonly string _issuer;
    private readonly string _audience;
    private readonly int _expiryMinutes;

    public JwtTokenService(string key, string issuer, string audience, int expiryMinutes)
    {
        _key = key;
        _issuer = issuer;
        _audience = audience;
        _expiryMinutes = expiryMinutes;
    }

    public string GenerateToken(int userId, string email)
    {
        // 1. 定义 Claims（声明）
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Email, email),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        // 2. 创建签名密钥
        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_key));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        // 3. 生成 Token
        var token = new JwtSecurityToken(
            issuer: _issuer,
            audience: _audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_expiryMinutes),
            signingCredentials: credentials
        );

        // 4. 返回 Token 字符串
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public ClaimsPrincipal? ValidateToken(string token)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_key));

        var validationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = _issuer,
            ValidAudience = _audience,
            IssuerSigningKey = securityKey,
            ClockSkew = TimeSpan.Zero
        };

        try
        {
            return tokenHandler.ValidateToken(token, validationParameters, out _);
        }
        catch
        {
            return null;
        }
    }
}
```

---

### 第五步：注册服务

修改 `Infrastructure/DependencyInjection.cs`：

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using User.Application.Common.Interfaces;
using User.Infrastructure.Data;
using User.Infrastructure.Services;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructureServices(
            this IServiceCollection services,
            string connectionString)
        {
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(connectionString));

            services.AddScoped<IApplicationDbContext>(sp =>
                sp.GetRequiredService<ApplicationDbContext>());

            services.AddScoped<ApplicationDbContextInitialiser>();

            // 注册密码哈希服务
            services.AddScoped<IPasswordHasher, PasswordHasher>();

            return services;
        }

        public static IServiceCollection AddJwtServices(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            var jwtSettings = configuration.GetSection("Jwt");
            var key = jwtSettings["Key"]!;
            var issuer = jwtSettings["Issuer"]!;
            var audience = jwtSettings["Audience"]!;
            var expiryMinutes = int.Parse(jwtSettings["ExpiryMinutes"]!);

            services.AddSingleton<IJwtTokenService>(sp =>
                new JwtTokenService(key, issuer, audience, expiryMinutes));

            return services;
        }
    }
}
```

---

### 第六步：配置 Program.cs

修改 `WebAPI/Program.cs`：

```csharp
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using User.Infrastructure.Data;

var builder = WebApplication.CreateBuilder(args);

// 注册服务
builder.Services.AddInfrastructureServices(
    builder.Configuration.GetConnectionString("DefaultConnection")!);

builder.Services.AddJwtServices(builder.Configuration);
builder.Services.AddApplicationServices();
builder.Services.AddControllers();

// 配置 Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "CraftCase User API", Version = "v1" });

    // JWT 认证配置
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. " +
                      "Enter 'Bearer' [space] and then your token",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// 配置 JWT 认证
var jwtSettings = builder.Configuration.GetSection("Jwt");
var key = Encoding.UTF8.GetBytes(jwtSettings["Key"]!);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddAuthorization();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    await app.Services.InitialiseDatabaseAsync();
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// 重要：必须在 UseAuthorization 之前
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
```

**⚠️ 关键点：**
1. `UseAuthentication()` **必须**在 `UseAuthorization()` 之前
2. `ClockSkew = TimeSpan.Zero` 消除默认的 5 分钟时钟偏差
3. `AddJwtServices()` 在注册其他服务后调用

---

### 第七步：实现登录功能

修改 `Commands/Login/LoginCommand.cs`：

```csharp
using MediatR;
using User.Application.Common.Interfaces;
using User.Domain.Entities;

namespace User.Application.UserProfiles.Commands.Login;

public record LoginCommand : IRequest<LoginResult>
{
    public string Email { get; set; } = null!;
    public string Password { get; set; } = null!;
}

public class LoginResult
{
    public bool Success { get; set; }
    public int UserId { get; set; }
    public string? Email { get; set; }
    public string? Token { get; set; }
}

public class LoginCommandHandler : IRequestHandler<LoginCommand, LoginResult>
{
    private readonly IApplicationDbContext _context;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenService _jwtTokenService;

    public LoginCommandHandler(
        IApplicationDbContext context,
        IPasswordHasher passwordHasher,
        IJwtTokenService jwtTokenService)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _jwtTokenService = jwtTokenService;
    }

    public async Task<LoginResult> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        // 1️⃣ 查找用户
        var user = await _context.Set<UserProfile>()
            .FirstOrDefaultAsync(u => u.Email == request.Email, cancellationToken);

        if (user == null)
        {
            return new LoginResult { Success = false };
        }

        // 2️⃣ 验证密码
        if (!_passwordHasher.VerifyPassword(request.Password, user.Password))
        {
            return new LoginResult { Success = false };
        }

        // 3️⃣ 生成 JWT Token
        var token = _jwtTokenService.GenerateToken(user.Id, user.Email);

        return new LoginResult
        {
            Success = true,
            UserId = user.Id,
            Email = user.Email,
            Token = token
        };
    }
}
```

---

### 第八步：添加受保护端点

修改 `WebAPI/Controllers/UserProfilesController.cs`：

```csharp
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UserProfilesController : ControllerBase
    {
        // ... 其他代码 ...

        // 公开端点（不需要认证）
        [HttpPost("Login")]
        public async Task<ActionResult<LoginResult>> Login(LoginCommand command)
        {
            return await _sender.Send(command);
        }

        // 受保护端点（需要认证）
        [Authorize]
        [HttpGet("me")]
        public async Task<ActionResult<UserProfileBriefDto>> GetCurrentUser()
        {
            // 从 JWT Token 中提取用户 ID
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userIdClaim == null || !int.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized();
            }

            return await _sender.Send(new GetUserProfileQuery() { Id = userId });
        }
    }
}
```

---

## 代码文件清单

### 新增文件

| 文件路径 | 说明 |
|---------|------|
| `Application/Common/Interfaces/IPasswordHasher.cs` | 密码哈希接口 |
| `Application/Common/Interfaces/IJwtTokenService.cs` | JWT 服务接口 |
| `Infrastructure/Services/PasswordHasher.cs` | 密码哈希实现 |
| `Infrastructure/Services/JwtTokenService.cs` | JWT 服务实现 |

### 修改文件

| 文件路径 | 修改内容 |
|---------|---------|
| `Infrastructure/DependencyInjection.cs` | 注册 JWT 和密码哈希服务 |
| `Infrastructure/Data/ApplicationDbContextInitialiser.cs` | 使用 BCrypt 哈希种子数据密码 |
| `WebAPI/Program.cs` | 配置 JWT 认证中间件 |
| `WebAPI/appsettings.json` | 添加 JWT 配置节 |
| `WebAPI/WebAPI.csproj` | 添加 JWT 相关 NuGet 包 |
| `Infrastructure/Infrastructure.csproj` | 添加 BCrypt 包 |
| `Application/UserProfiles/Commands/Login/LoginCommand.cs` | 实现登录逻辑 |
| `Application/UserProfiles/Commands/CreateUserProfile/CreateUserProfileCommand.cs` | 注册时哈希密码 |
| `WebAPI/Controllers/UserProfilesController.cs` | 添加受保护端点 |

---

## 测试方法

### 方法一：使用 Swagger UI

1. **启动项目**
   ```bash
   cd src/Services/User/src/WebAPI
   dotnet run
   ```

2. **访问 Swagger**
   ```
   http://localhost:5298/swagger
   ```

3. **测试登录**
   - 找到 `POST /api/userprofiles/Login`
   - 点击 "Try it out"
   - 输入测试数据：
     ```json
     {
       "email": "2529411612@qq.com",
       "password": "dongbin123456"
     }
     ```
   - 复制返回的 `token`

4. **配置认证**
   - 点击右上角 "Authorize" 按钮
   - 输入：`Bearer YOUR_TOKEN`（替换 YOUR_TOKEN）
   - 点击 "Authorize"

5. **测试受保护端点**
   - 找到 `GET /api/userprofiles/me`
   - 点击 "Try it out"
   - 点击 "Execute"
   - 应该返回当前用户信息

### 方法二：使用 cURL

```bash
# 1. 登录获取 Token
curl -X POST "http://localhost:5298/api/userprofiles/Login" \
  -H "Content-Type: application/json" \
  -d '{"email":"2529411612@qq.com","password":"dongbin123456"}'

# 响应示例：
# {"success":true,"userId":1,"email":"2529411612@qq.com","token":"eyJhbG..."}

# 2. 使用 Token 访问受保护端点
curl -X GET "http://localhost:5298/api/userprofiles/me" \
  -H "Authorization: Bearer eyJhbG..."
```

### 方法三：使用 Postman

1. **创建登录请求**
   - Method: `POST`
   - URL: `http://localhost:5298/api/userprofiles/Login`
   - Headers: `Content-Type: application/json`
   - Body (raw JSON):
     ```json
     {
       "email": "2529411612@qq.com",
       "password": "dongbin123456"
     }
     ```

2. **保存 Token**
   - 从响应中复制 `token` 值

3. **创建受保护请求**
   - Method: `GET`
   - URL: `http://localhost:5298/api/userprofiles/me`
   - Headers: `Authorization: Bearer YOUR_TOKEN`

---

## 常见问题

### 问题 1：401 Unauthorized

**可能原因：**
- Token 未正确发送
- Token 已过期
- `UseAuthentication()` 未在 `UseAuthorization()` 之前调用

**解决方法：**
```csharp
// 确保 Program.cs 中的顺序正确
app.UseAuthentication();  // 先认证
app.UseAuthorization();   // 后授权
```

### 问题 2：Invalid salt version

**可能原因：**
- 数据库中存在明文密码
- 密码不是 BCrypt 格式

**解决方法：**
```csharp
// 清空旧数据并重新创建
if (_context.Set<UserProfile>().Any())
{
    var existingUsers = await _context.Set<UserProfile>().ToListAsync();
    _context.Set<UserProfile>().RemoveRange(existingUsers);
    await _context.SaveChangesAsync();
}

// 使用 BCrypt 哈希新密码
var hashedPassword = BCrypt.Net.BCrypt.HashPassword("password");
```

### 问题 3：Token 验证失败

**可能原因：**
- 密钥不匹配
- Token 格式错误
- ClockSkew 设置问题

**解决方法：**
```csharp
// 确保配置一致
options.TokenValidationParameters = new TokenValidationParameters
{
    ClockSkew = TimeSpan.Zero  // 消除时钟偏差
};
```

### 问题 4：Swagger 无法测试

**可能原因：**
- 未配置 Swagger 安全定义
- Token 格式错误

**解决方法：**
```
1. 在 Swagger 中点击 "Authorize"
2. 输入：Bearer YOUR_TOKEN（注意有空格）
3. 不要忘记 "Bearer " 前缀
```

---

## 最佳实践

### 1. 密钥管理

#### 生产环境使用环境变量

```bash
# Windows
set JWT_SECRET_KEY=your-very-long-random-secret-key-here
set JWT_ISSUER=CraftCase
set JWT_AUDIENCE=CraftCaseAdmin

# Linux/Mac
export JWT_SECRET_KEY="your-very-long-random-secret-key-here"
export JWT_ISSUER="CraftCase"
export JWT_AUDIENCE="CraftCaseAdmin"
```

#### appsettings.json 使用占位符

```json
{
  "Jwt": {
    "Key": "%JWT_SECRET_KEY%",
    "Issuer": "%JWT_ISSUER%",
    "Audience": "%JWT_AUDIENCE%",
    "ExpiryMinutes": 60
  }
}
```

### 2. 密钥强度

```csharp
// 生成 256 位随机密钥
var key = new byte[32];
using (var generator = System.Security.Cryptography.RandomNumberGenerator.Create())
{
    generator.GetBytes(key);
}
var base64Key = Convert.ToBase64String(key);
Console.WriteLine(base64Key);
```

### 3. Token 过期策略

```csharp
// 短期 Access Token（15分钟）
"ExpiryMinutes": 15

// 长期 Refresh Token（7天）
"RefreshTokenExpiryDays": 7
```

### 4. 密码策略

```csharp
public class PasswordValidator
{
    public static bool Validate(string password)
    {
        // 至少 8 个字符
        if (password.Length < 8)
            return false;

        // 包含大写字母
        if (!password.Any(char.IsUpper))
            return false;

        // 包含小写字母
        if (!password.Any(char.IsLower))
            return false;

        // 包含数字
        if (!password.Any(char.IsDigit))
            return false;

        return true;
    }
}
```

### 5. 错误处理

```csharp
public class LoginCommandHandler : IRequestHandler<LoginCommand, LoginResult>
{
    public async Task<LoginResult> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // 登录逻辑
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Login failed for {Email}", request.Email);
            return new LoginResult { Success = false };
        }
    }
}
```

### 6. 日志记录

```csharp
// 记录登录成功
_logger.LogInformation("User {UserId} logged in successfully", user.Id);

// 记录登录失败
_logger.LogWarning("Failed login attempt for {Email}", request.Email);

// 记录 Token 生成
_logger.LogInformation("Generated token for user {UserId}", user.Id);
```

### 7. 速率限制

```csharp
// 安装速率限制包
dotnet add packageAspNetCoreRateLimit

// 配置速率限制
builder.Services.AddRateLimiter(options =>
{
    options.AddPolicy("LoginPolicy", context =>
        RateLimitPartition.GetSlidingWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString(),
            factory: _ => new SlidingWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromMinutes(1),
                SegmentsPerWindow = 2
            }));
});

// 应用到登录端点
[EnableRateLimiting("LoginPolicy")]
[HttpPost("Login")]
public async Task<ActionResult<LoginResult>> Login(LoginCommand command)
{
    // ...
}
```

---

## 安全建议

### ✅ 推荐做法

1. **使用 HTTPS**
   ```csharp
   app.UseHttpsRedirection();
   app.UseHsts();
   ```

2. **密钥至少 32 字符**
   ```json
   {
     "Jwt": {
       "Key": "this-is-a-very-long-secret-key-at-least-32-characters"
     }
   }
   ```

3. **设置合理的过期时间**
   - Access Token: 15-60 分钟
   - Refresh Token: 7-30 天

4. **验证所有 Token 参数**
   ```csharp
   options.TokenValidationParameters = new TokenValidationParameters
   {
       ValidateIssuer = true,
       ValidateAudience = true,
       ValidateLifetime = true,
       ValidateIssuerSigningKey = true
   };
   ```

5. **使用强密码哈希**
   - BCrypt 自动处理盐值
   - 工作因子默认为 11

### ❌ 避免做法

1. **不要在代码中硬编码密钥**
   ```csharp
   // ❌ 错误
   var key = "my-secret-key";

   // ✅ 正确
   var key = builder.Configuration["Jwt:Key"];
   ```

2. **不要在 URL 中传递 Token**
   ```
   ❌ https://api.com/api/me?token=xxx
   ✅ Authorization: Bearer xxx
   ```

3. **不要在本地存储中保存敏感信息**
   ```javascript
   // ❌ 错误
   localStorage.setItem('password', password);

   // ✅ 正确 - 只保存 Token
   localStorage.setItem('token', token);
   ```

4. **不要忽略 Token 过期**
   ```csharp
   // ❌ 错误
   ClockSkew = TimeSpan.FromDays(1);

   // ✅ 正确
   ClockSkew = TimeSpan.Zero;
   ```

---

## 附录

### JWT Token 结构

```
eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJodHRwOi8vc2NoZW1hcy54bWxzb2FwLm9yZy93cy8yMDA1LzA1L2lkZW50aXR5L2NsYWltcy9uYW1laWRlbnRpZmllciI6IjEiLCJodHRwOi8vc2NoZW1hcy54bWxzb2FwLm9yZy93cy8yMDA1LzA1L2lkZW50aXR5L2NsYWltcy9lbWFpbGFkZHJlc3MiOiIyNTI5NDExNjEyQHFxLmNvbSIsImp0aSI6IjYzNTUwYWM1LTBkNWEtNDRhZS1hZjNhLTU3NzZmZjE2MWU4MSIsImV4cCI6MTc3MjA5OTg2MiwiaXNzIjoiQ3JhZnRDYXNlIiwiYXVkIjoiQ3JhZnRDYXNlQWRtaW4ifQ._7swCfDrBWcuR8iohJonsNIC49E2vgoAidAlX0Enlhs

└─────────────────┬─────────────────┘ └─────────────────┬─────────────────┘ └─────────────────┬─────────────────┘
                  Header                                   Payload                                  Signature
```

### 常用 Claims

| Claim 类型 | 说明 | 示例 |
|-----------|------|------|
| `ClaimTypes.NameIdentifier` | 用户 ID | "1" |
| `ClaimTypes.Email` | 邮箱 | "user@example.com" |
| `ClaimTypes.Name` | 用户名 | "John Doe" |
| `ClaimTypes.Role` | 角色 | "Admin" |
| `JwtRegisteredClaimNames.Jti` | Token ID | GUID |

### 参考资源

- [JWT 官方网站](https://jwt.io/)
- [RFC 7519](https://tools.ietf.org/html/rfc7519)
- [Microsoft JWT 文档](https://docs.microsoft.com/en-us/aspnet/core/security/authentication/jwt)
- [BCrypt 文档](https://bcrypt.sourceforge.io/)

---

## 总结

本指南详细介绍了如何在一个 Clean Architecture 项目中实现 JWT 认证系统。关键要点：

1. **分层架构**：接口在 Application 层，实现在 Infrastructure 层
2. **密码安全**：使用 BCrypt 进行密码哈希
3. **JWT 配置**：正确配置认证中间件和 Token 验证
4. **测试验证**：使用 Swagger UI 进行完整测试
5. **生产就绪**：遵循安全最佳实践

按照本指南，你可以构建一个安全、可靠、生产级别的 JWT 认证系统。

---

**文档版本**: 1.0
**最后更新**: 2026-03-02
**作者**: Claude Code
**许可证**: MIT
