# JWT 认证实施方案

> **项目：** SilverCloud User Service
> **架构：** Clean Architecture (Domain + Application + Infrastructure + WebAPI)
> **目标：** 实现完整的 JWT 认证系统

---

## 📋 目录

1. [方案概述](#方案概述)
2. [架构设计](#架构设计)
3. [技术选型](#技术选型)
4. [实施步骤](#实施步骤)
5. [文件清单](#文件清单)
6. [测试验证](#测试验证)

---

## 方案概述

### 当前项目结构
```
src/
├── Domain/              # 领域层（实体、值对象）
├── Application/         # 应用层（Commands、Queries、DTOs）
├── Infrastructure/      # 基础设施层（EF Core、配置）
└── WebAPI/             # 表示层（Controllers、Program.cs）
```

### 当前认证状态
- ✅ 有 `UserProfile` 实体（包含 Email/PhoneNum + Password）
- ✅ 有登录查询 `GetUserProfileQuery`（通过邮箱/手机号 + 密码登录）
- ⚠️ **密码为明文存储** - 安全风险，需要改为哈希加密
- ❌ **缺少** JWT Token 生成
- ❌ **缺少** Token 验证机制
- ❌ **缺少** 授权保护

### 实施目标
- **密码安全**：使用 BCrypt 对密码进行哈希加密存储
- **登录**：修改现有 `GetUserProfile` API，登录成功后返回 JWT Token
- **授权**：保护 API 端点，只有持有有效 Token 才能访问
- **Token 刷新**：支持 Access Token + Refresh Token 双 Token 机制
- **权限管理**：基于 Claims 的角色权限控制

### 实施说明
- ✅ **不创建新的 AuthController** - 复用现有的 `UserProfilesController`
- ✅ **修改 GetUserProfile Query** - 登录成功时返回 Token
- ✅ **使用 BCrypt 加密密码** - 生产环境必须使用密码哈希
- ✅ **使用 Scalar** - 用于 API 测试和调试

---

## 架构设计

### JWT 认证流程

```
┌─────────────┐
│   用户      │
└──────┬──────┘
       │ 1. POST /api/UserProfiles/Get
       │    { identity, password }
       ▼
┌─────────────────────┐
│ UserProfilesController│
│  - 验证用户凭据      │
│  - 生成 JWT Token   │
└──────┬──────────────┘
       │ 2. 返回用户信息 + Token
       │    { user, accessToken, refreshToken }
       ▼
┌─────────────────────┐
│    客户端存储        │
└──────┬──────────────┘
       │ 3. 后续请求携带 Token
       │    Header: Authorization: Bearer {token}
       ▼
┌─────────────────────┐
│  API 端点           │
│  [Authorize]        │
│  - 验证 Token       │
│  - 提取 Claims      │
└─────────────────────┘
```

### 文件组织结构

```
src/
├── Domain/
│   └── Common/
│       └── ICurrentUserService.cs           # 当前用户服务接口
│
├── Application/
│   ├── Common/
│   │   ├── Interfaces/
│   │   │   ├── ITokenService.cs            # Token 服务接口
│   │   │   ├── IPasswordHasher.cs          # 密码哈希服务接口
│   │   │   └── ICurrentUserService.cs       # 当前用户服务接口
│   │   └── Models/
│   │       ├── JwtSettings.cs               # JWT 配置模型
│   │       └── TokenResponse.cs             # Token 响应模型
│   │
│   └── UserProfiles/
│       ├── Queries/
│       │   └── GetUserProfile/
│       │       ├── GetUserProfileQuery.cs   # 修改：添加 Token 返回
│       │       └── GetUserProfileQueryHandler.cs
│       ├── Commands/
│       │   └── CreateUserProfile/
│       │       └── CreateUserProfileCommandHandler.cs  # 修改：哈希密码
│       └── Dtos/
│           └── LoginResponseDto.cs          # 新增：登录响应（包含 Token）
│
├── Infrastructure/
│   ├── Services/
│   │   ├── TokenService.cs                  # Token 服务实现
│   │   └── PasswordHasher.cs                # 密码哈希实现（BCrypt）
│   └── Persistence/
│       └── Configurations/
│           └── RefreshTokenConfiguration.cs # RefreshToken 实体配置
│
└── WebAPI/
    ├── Controllers/
    │   └── UserProfilesController.cs       # 修改：复用现有 Controller
    └── appsettings.json                     # JWT 配置
```

---

## 技术选型

| 组件 | 技术方案 | 说明 |
|------|---------|------|
| JWT 库 | `Microsoft.AspNetCore.Authentication.JwtBearer` | 官方 JWT Bearer 认证 |
| Token 生成 | `System.IdentityModel.Tokens.Jwt` | 创建和验证 JWT |
| 密码哈希 | `BCrypt.Net-Next` | 安全的密码哈希（推荐） |
| Token 存储 | 数据库 `RefreshTokens` 表 | 持久化 Refresh Token |
| API 文档 | Scalar | 现代化 API 测试界面 |
| 配置管理 | `appsettings.json` + `IOptions` | JWT 密钥和过期时间 |

### NuGet 包清单

```xml
<!-- JWT 认证 -->
<PackageReference Include="Microsoft.AspNetCore.Authentication.JwtBearer" Version="10.0.0" />

<!-- 密码哈希（必需） -->
<PackageReference Include="BCrypt.Net-Next" Version="4.0.3" />
```

---

## 实施步骤

### ⚠️ 重要提示
- **按顺序执行**：每个步骤都是独立的，完成后可以测试验证
- **备份代码**：开始前建议提交当前代码到 Git
- **渐进式实施**：可以先实施基础版（只有 Access Token），再扩展到完整版（+ Refresh Token）

---

## 步骤 1：安装 NuGet 包

### 目标
安装 JWT 相关的 NuGet 包

### 操作
在 **WebAPI.csproj** 中添加：

```xml
<ItemGroup>
  <PackageReference Include="Microsoft.AspNetCore.Authentication.JwtBearer" Version="10.0.0" />
  <PackageReference Include="BCrypt.Net-Next" Version="4.0.3" />
</ItemGroup>
```

### 验证
```bash
dotnet restore
```

---

## 步骤 2：创建密码哈希服务

### 目标
创建密码哈希服务，使用 BCrypt 加密密码

### 为什么需要密码哈希？

**当前问题：**
- ⚠️ 密码以明文形式存储在数据库中
- ⚠️ 数据库泄露时，所有用户密码直接暴露
- ⚠️ 开发人员可以看到用户密码（隐私风险）

**BCrypt 优势：**
- ✅ 自动加盐（防止彩虹表攻击）
- ✅ 单向加密（无法解密）
- ✅ 可调计算成本（抵御暴力破解）
- ✅ 行业标准（广泛使用和验证）

### 操作

#### 2.1 创建密码哈希接口
创建文件 **Application/Common/Interfaces/IPasswordHasher.cs**：

```csharp
namespace Application.Common.Interfaces;

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

#### 2.2 实现密码哈希服务
创建文件 **Infrastructure/Services/PasswordHasher.cs**：

```csharp
using Application.Common.Interfaces;

namespace Infrastructure.Services;

public class PasswordHasher : IPasswordHasher
{
    public string HashPassword(string password)
    {
        // BCrypt 自动生成盐值并哈希
        // 工作因子默认为 11，可调整
        return BCrypt.Net.BCrypt.HashPassword(password);
    }

    public bool VerifyPassword(string password, string hashedPassword)
    {
        // 验证明文密码是否与哈希值匹配
        return BCrypt.Net.BCrypt.Verify(password, hashedPassword);
    }
}
```

#### 2.3 注册密码哈希服务
修改 **Application/DependencyInjection.cs**：

```csharp
using System.Reflection;
using Application.Common.Interfaces;
using Infrastructure.Services;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplicationServices(this IServiceCollection services)
        {
            // 注册 AutoMapper
            services.AddAutoMapper(cfg =>
                cfg.AddMaps(Assembly.GetExecutingAssembly()));

            // 注册 MediatR
            services.AddMediatR(cfg =>
            {
                cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly());
            });

            // 注册验证器
            services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

            // 注册密码哈希服务（必需）
            services.AddScoped<IPasswordHasher, PasswordHasher>();

            // 注册 Token 服务
            services.AddScoped<ITokenService, TokenService>();

            return services;
        }
    }
}
```

### 验证
```bash
# 测试 BCrypt 是否正常工作
# 在 Program.cs 中添加临时测试代码
var hasher = new PasswordHasher();
var hash = hasher.HashPassword("test123");
Console.WriteLine(hash);  // $2a$11$...
Console.WriteLine(hasher.VerifyPassword("test123", hash));  // True
```

---

## 步骤 3：配置 JWT 设置

### 目标
在 `appsettings.json` 中添加 JWT 配置

### 操作
修改 **WebAPI/appsettings.json**：

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "JwtSettings": {
    "Secret": "YourSuperSecretKeyThatIsAtLeast32CharactersLong!",
    "Issuer": "SilverCloud",
    "Audience": "SilverCloudUsers",
    "AccessTokenExpirationMinutes": 60,
    "RefreshTokenExpirationDays": 7
  }
}
```

### 说明
- **Secret**：JWT 签名密钥（生产环境必须使用环境变量）
- **Issuer**：签发者标识
- **Audience**：受众标识
- **AccessTokenExpirationMinutes**：访问令牌过期时间（分钟）
- **RefreshTokenExpirationDays**：刷新令牌过期时间（天）

---

## 步骤 4：创建配置模型和 DTOs

### 目标
创建强类型配置类和请求/响应模型

### 操作

#### 3.1 创建 JWT 配置类
创建文件 **Application/Common/Models/JwtSettings.cs**：

```csharp
namespace Application.Common.Models;

public class JwtSettings
{
    public const string SectionName = "JwtSettings";

    public string Secret { get; set; } = string.Empty;
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public int AccessTokenExpirationMinutes { get; set; }
    public int RefreshTokenExpirationDays { get; set; }
}
```

#### 3.2 创建 Token 响应模型
创建文件 **Application/Common/Models/TokenResponse.cs**：

```csharp
namespace Application.Common.Models;

public class TokenResponse
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
}
```

#### 3.3 创建登录请求 DTO
创建文件 **Application/Auth/Dtos/LoginRequestDto.cs**：

```csharp
using FluentValidation;

namespace Application.Auth.Dtos;

public class LoginRequestDto
{
    public string Identity { get; set; } = string.Empty; // Email 或 PhoneNum
    public string Password { get; set; } = string.Empty;
}

public class LoginRequestValidator : AbstractValidator<LoginRequestDto>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.Identity)
            .NotEmpty().WithMessage("邮箱或手机号不能为空");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("密码不能为空");
    }
}
```

#### 3.4 创建登录响应 DTO
创建文件 **Application/Auth/Dtos/LoginResponseDto.cs**：

```csharp
namespace Application.Auth.Dtos;

public class LoginResponseDto
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public UserDto User { get; set; } = null!;
}
```

---

## 步骤 5：创建 Token 服务接口和实现

### 目标
创建生成和验证 JWT Token 的服务

### 操作

#### 4.1 创建 Token 服务接口
创建文件 **Application/Common/Interfaces/ITokenService.cs**：

```csharp
using Application.Common.Models;

namespace Application.Common.Interfaces;

public interface ITokenService
{
    string GenerateAccessToken(int userId, string email, string phoneNum);
    string GenerateRefreshToken();
    ClaimsPrincipal? ValidateToken(string token);
    TokenResponse GenerateTokenPair(int userId, string email, string phoneNum);
}
```

#### 4.2 实现 Token 服务
创建文件 **Infrastructure/Services/TokenService.cs**：

```csharp
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Application.Common.Interfaces;
using Application.Common.Models;
using Microsoft.Extensions.Options;

namespace Infrastructure.Services;

public class TokenService : ITokenService
{
    private readonly JwtSettings _jwtSettings;

    public TokenService(IOptions<JwtSettings> jwtSettings)
    {
        _jwtSettings = jwtSettings.Value;
    }

    public string GenerateAccessToken(int userId, string email, string phoneNum)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(ClaimTypes.Email, email ?? string.Empty),
            new(ClaimTypes.MobilePhone, phoneNum ?? string.Empty)
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _jwtSettings.Issuer,
            audience: _jwtSettings.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenExpirationMinutes),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public string GenerateRefreshToken()
    {
        var randomNumber = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        return Convert.ToBase64String(randomNumber);
    }

    public ClaimsPrincipal? ValidateToken(string token)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(_jwtSettings.Secret);

        try
        {
            var principal = tokenHandler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidIssuer = _jwtSettings.Issuer,
                ValidateAudience = true,
                ValidAudience = _jwtSettings.Audience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            }, out _);

            return principal;
        }
        catch
        {
            return null;
        }
    }

    public TokenResponse GenerateTokenPair(int userId, string email, string phoneNum)
    {
        return new TokenResponse
        {
            AccessToken = GenerateAccessToken(userId, email, phoneNum),
            RefreshToken = GenerateRefreshToken(),
            ExpiresAt = DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenExpirationMinutes)
        };
    }
}
```

---

## 步骤 6：配置 JWT 认证

### 目标
在 `Program.cs` 中配置 JWT Bearer 认证

### 操作
修改 **WebAPI/Program.cs**：

```csharp
using Scalar.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Application.Common.Models;

var builder = WebApplication.CreateBuilder(args);

// 1. 添加 JWT 配置
builder.Services.Configure<JwtSettings>(
    builder.Configuration.GetSection(JwtSettings.SectionName));

// 2. 配置 JWT Bearer 认证
var jwtSettings = builder.Configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>();
var key = Encoding.UTF8.GetBytes(jwtSettings!.Secret);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = true,
        ValidIssuer = jwtSettings.Issuer,
        ValidateAudience = true,
        ValidAudience = jwtSettings.Audience,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddAuthorization();

// 添加数据连接
builder.Services.AddInfrastructureServices(
    builder.Configuration.GetConnectionString("DefaultConnection")!);

// 添加 application 服务
builder.Services.AddApplicationServices();

// Add services to the container.
builder.Services.AddControllers();

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((document, context, cancellationToken) =>
    {
        document.Info.Title = "SilverCloud User API";
        document.Info.Version = "v1";
        return Task.CompletedTask;
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();

// 3. 启用认证和授权
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
```

---

## 步骤 7：修改创建用户和登录逻辑使用密码哈希

### 目标
在创建用户时哈希密码，登录时验证密码

### 操作

#### 7.1 修改创建用户逻辑
修改 **Application/UserProfiles/Commands/CreateUserProfile/CreateUserProfileCommandHandler.cs**：

```csharp
using Application.Common.Interfaces;
using Domain.Entities;
using Domain.ValueObjects;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.UserProfiles.Commands.CreateUserProfile;

public class CreateUserProfileCommandHandler : IRequestHandler<CreateUserProfileCommand, int>
{
    private readonly IApplicationDbContext _context;
    private readonly IPasswordHasher _passwordHasher;  // 新增

    public CreateUserProfileCommandHandler(
        IApplicationDbContext context,
        IPasswordHasher passwordHasher)  // 新增
    {
        _context = context;
        _passwordHasher = passwordHasher;  // 新增
    }

    public async Task<int> Handle(CreateUserProfileCommand request, CancellationToken cancellationToken)
    {
        // 创建实体并映射
        var entity = new UserProfile
        {
            Name = UserProfileHelper.GetOrCreateName(request.Name),
            Email = request.Email != null ? Email.Create(request.Email) : null,
            PhoneNum = request.PhoneNum,
            Password = _passwordHasher.HashPassword(request.Password),  // 修改：哈希密码
        };

        // 添加到数据库
        _context.Set<UserProfile>().Add(entity);

        // 保存更改
        await _context.SaveChangesAsync(cancellationToken);

        return entity.Id;
    }
}
```

#### 7.2 数据库迁移说明

**重要**：如果你已经有明文密码的用户数据，需要进行数据迁移：

```sql
-- 方案 1：清空现有数据，重新注册（推荐用于开发环境）
DELETE FROM UserProfiles

-- 方案 2：迁移现有密码（仅用于生产环境，需要一次性脚本）
-- 注意：无法将明文转换为 BCrypt 哈希，需要用户重置密码
-- 可以添加一个 "IsPasswordHashed" 字段标记哪些用户已更新
```

---

## 步骤 8：修改 GetUserProfile Query 返回 Token

### 目标
修改现有的登录查询，返回 JWT Token 并验证密码

### 操作

#### 8.1 创建登录响应 DTO
创建文件 **Application/UserProfiles/Dtos/LoginResponseDto.cs**：

```csharp
namespace Application.UserProfiles.Dtos;

public class LoginResponseDto
{
    public UserProfileBriefDto User { get; set; } = null!;
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
}
```

#### 8.2 修改 GetUserProfileQuery
修改 **Application/UserProfiles/Queries/GetUserProfile/GetUserProfileQuery.cs**：

```csharp
using Application.UserProfiles.Dtos;
using MediatR;

namespace Application.UserProfiles.Queries.GetUserProfile;

public record GetUserProfileQuery : IRequest<LoginResponseDto>
{
    public string Identity { get; set; } = string.Empty;  // Email 或 PhoneNum
    public string Password { get; set; } = string.Empty;
}
```

#### 8.3 修改 GetUserProfileQueryHandler
修改 **Application/UserProfiles/Queries/GetUserProfile/GetUserProfileQueryHandler.cs**：

```csharp
using Application.Common.Interfaces;
using Application.UserProfiles.Dtos;
using Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.UserProfiles.Queries.GetUserProfile;

public class GetUserProfileQueryHandler : IRequestHandler<GetUserProfileQuery, LoginResponseDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ITokenService _tokenService;
    private readonly IPasswordHasher _passwordHasher;  // 新增
    private readonly IMapper _mapper;

    public GetUserProfileQueryHandler(
        IApplicationDbContext context,
        ITokenService tokenService,
        IPasswordHasher passwordHasher,  // 新增
        IMapper mapper)
    {
        _context = context;
        _tokenService = tokenService;
        _passwordHasher = passwordHasher;  // 新增
        _mapper = mapper;
    }

    public async Task<LoginResponseDto> Handle(GetUserProfileQuery request, CancellationToken cancellationToken)
    {
        // 1. 查找用户（支持邮箱或手机号登录）
        var user = await _context.Set<UserProfile>()
            .FirstOrDefaultAsync(u =>
                (u.Email != null && u.Email.Value == request.Identity ||
                 u.PhoneNum == request.Identity),
                cancellationToken);  // 修改：不在这里比较密码

        if (user == null)
        {
            throw new UnauthorizedAccessException("用户名或密码错误");
        }

        // 2. 验证密码（使用 BCrypt）
        if (!_passwordHasher.VerifyPassword(request.Password, user.Password))
        {
            throw new UnauthorizedAccessException("用户名或密码错误");
        }

        // 3. 生成 Token
        var tokens = _tokenService.GenerateTokenPair(
            user.Id,
            user.Email?.Value ?? string.Empty,
            user.PhoneNum ?? string.Empty
        );

        // 4. 映射用户信息
        var userDto = _mapper.Map<UserProfileBriefDto>(user);

        // 5. 返回用户信息 + Token
        return new LoginResponseDto
        {
            User = userDto,
            AccessToken = tokens.AccessToken,
            RefreshToken = tokens.RefreshToken,
            ExpiresAt = tokens.ExpiresAt
        };
    }
}
```

---

## 步骤 9：添加 Authorization 注册

### 目标
在 Application 层注册 Token 服务

### 操作
修改 **Application/DependencyInjection.cs**：

```csharp
using System.Reflection;
using Application.Common.Interfaces;
using Infrastructure.Services;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplicationServices(this IServiceCollection services)
        {
            // 注册 AutoMapper
            services.AddAutoMapper(cfg =>
                cfg.AddMaps(Assembly.GetExecutingAssembly()));

            // 注册 MediatR
            services.AddMediatR(cfg =>
            {
                cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly());
            });

            // 注册验证器
            services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

            // 注册 Token 服务
            services.AddScoped<ITokenService, TokenService>();

            return services;
        }
    }
}
```

---

## 步骤 10：更新 UserProfilesController 添加授权

### 目标
保护现有的 API 端点

### 操作
修改 **WebAPI/Controllers/UserProfilesController.cs**，添加 `[Authorize]` 特性：

```csharp
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using User.Application.UserProfiles.Commands.CreateUserProfile;
using User.Application.UserProfiles.Commands.DeleteUserProfile;
using User.Application.UserProfiles.Commands.UpdateUserProfile;
using User.Application.UserProfiles.Dtos;
using User.Application.UserProfiles.Queries.GetUserProfile;
using User.Application.UserProfiles.Queries.SearchUserProfiles;

namespace WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UserProfilesController : ControllerBase
    {
        // ... 现有代码 ...

        [HttpPost("Get")]
        [AllowAnonymous]  // 允许匿名访问（登录需要）
        public async Task<ActionResult<UserProfileBriefDto>> Get(GetUserProfileQuery query)
        {
            return await _sender.Send(query);
        }

        [HttpPost]
        [AllowAnonymous]  // 允许匿名访问（注册需要）
        public async Task<ActionResult<int>> Create(CreateUserProfileCommand command)
        {
            return await _sender.Send(command);
        }

        [HttpDelete("{id}")]
        [Authorize]  // 需要认证
        public async Task<ActionResult> Delete(int id)
        {
            await _sender.Send(new DeleteUserProfileCommand(id));
            return Ok();
        }

        [HttpPut("{id}")]
        [Authorize]  // 需要认证
        public async Task<ActionResult> Update(int id, UpdateUserProfileCommand command)
        {
            if (id != command.Id)
            {
                return BadRequest();
            }

            await _sender.Send(command);
            return Ok();
        }

        [HttpPost("Search")]
        [Authorize]  // 需要认证
        public async Task<ActionResult<PaginatedList<UserProfileBriefDto>>> Search(SearchUserProfilesQuery query)
        {
            return await _sender.Send(query);
        }

        /// <summary>
        /// 获取当前用户信息
        /// </summary>
        [HttpGet("Me")]
        [Authorize]
        public async Task<ActionResult> GetCurrentUser()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (!int.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized();
            }

            var query = new GetUserProfileQuery
            {
                Identity = userId.ToString(),  // 这里可以简化
                Password = string.Empty  // 不需要密码验证
            };

            // 这里需要修改 GetUserProfileQuery 来支持按 ID 查询
            // 暂时返回用户 ID
            return Ok(new { userId });
        }
    }
}
```

---

## 步骤 11：创建 HTTP 测试文件

### 目标
创建 `.http` 文件测试 JWT 认证

### 操作
创建文件 **WebAPI/JwtTest.http**：

```http
@baseUrl = http://localhost:5000
@apiPath = {{baseUrl}}/api/UserProfiles

### ============================================
### 1. 用户登录（获取 Token）
### ============================================
POST {{apiPath}}/Get
Content-Type: application/json

{
  "identity": "zhangsan@example.com",
  "password": "Password123!"
}

### ============================================
### 2. 搜索用户（需要 Token）
### ============================================
### 将下方 {your_token_here} 替换为登录返回的 accessToken
POST {{apiPath}}/Search
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
Content-Type: application/json

{
  "pageNumber": 1,
  "pageSize": 10,
  "searchTerm": ""
}

### ============================================
### 3. 更新用户（需要 Token）
### ============================================
### 修改 URL 中的 {id} 和 Body 中的 id
PUT {{apiPath}}/1
Authorization: Bearer {your_token_here}
Content-Type: application/json

{
  "id": 1,
  "name": "张三（已更新）",
  "email": "zhangsan_new@example.com",
  "phoneNum": "13800138001",
  "password": "NewPassword123!"
}

### ============================================
### 4. 删除用户（需要 Token）
### ============================================
### 修改 URL 中的 {id}
DELETE {{apiPath}}/1
Authorization: Bearer {your_token_here}

### ============================================
### 5. 无 Token 访问（应该失败）
### ============================================
POST {{apiPath}}/Search
Content-Type: application/json

{
  "pageNumber": 1,
  "pageSize": 10,
  "searchTerm": ""
}
```

### 使用 Scalar 测试（推荐）

1. **启动项目**
   ```bash
   cd src/Services/User/src/WebAPI
   dotnet run
   ```

2. **访问 Scalar UI**
   ```
   http://localhost:5000/scalar
   ```

3. **测试登录**
   - 找到 `POST /api/UserProfiles/Get`
   - 点击 "Execute"
   - 输入测试数据：
     ```json
     {
       "identity": "zhangsan@example.com",
       "password": "Password123!"
     }
     ```
   - 复制返回的 `accessToken`

4. **配置认证**
   - 点击 Scalar 右上角的 "Authorize" 或锁图标 🔒
   - 输入：`Bearer YOUR_TOKEN`（替换 YOUR_TOKEN）
   - 点击 "Login" 或确认

5. **测试受保护端点**
   - 现在所有请求都会自动携带 Token
   - 尝试 `POST /api/UserProfiles/Search`

---

## 文件清单

### 需要创建的文件

| # | 文件路径 | 说明 |
|---|---------|------|
| 1 | `Application/Common/Models/JwtSettings.cs` | JWT 配置模型 |
| 2 | `Application/Common/Models/TokenResponse.cs` | Token 响应模型 |
| 3 | `Application/Common/Interfaces/IPasswordHasher.cs` | 密码哈希接口 |
| 4 | `Application/Common/Interfaces/ITokenService.cs` | Token 服务接口 |
| 5 | `Application/UserProfiles/Dtos/LoginResponseDto.cs` | 登录响应 DTO（包含 Token） |
| 6 | `Infrastructure/Services/PasswordHasher.cs` | 密码哈希实现（BCrypt） |
| 7 | `Infrastructure/Services/TokenService.cs` | Token 服务实现 |
| 8 | `WebAPI/JwtTest.http` | JWT 测试文件 |

### 需要修改的文件

| # | 文件路径 | 修改内容 |
|---|---------|---------|
| 1 | `WebAPI/WebAPI.csproj` | 添加 NuGet 包引用（JWT + BCrypt） |
| 2 | `WebAPI/appsettings.json` | 添加 JWT 配置节 |
| 3 | `WebAPI/Program.cs` | 配置 JWT 认证和授权 |
| 4 | `Application/DependencyInjection.cs` | 注册 Token 服务和密码哈希服务 |
| 5 | `Application/UserProfiles/Commands/CreateUserProfile/CreateUserProfileCommandHandler.cs` | 使用密码哈希 |
| 6 | `Application/UserProfiles/Queries/GetUserProfile/GetUserProfileQuery.cs` | 修改返回类型为 `LoginResponseDto` |
| 7 | `Application/UserProfiles/Queries/GetUserProfile/GetUserProfileQueryHandler.cs` | 添加密码验证和 Token 生成逻辑 |
| 8 | `WebAPI/Controllers/UserProfilesController.cs` | 添加 `[Authorize]` 特性 |
| 9 | **数据库** | 迁移现有用户密码（如有）|

---

## 测试验证

### 测试步骤

#### 1. 启动项目
```bash
cd src/Services/User/src/WebAPI
dotnet run
```

#### 2. 测试登录

**使用 Scalar UI（推荐）：**
1. 打开 `http://localhost:5000/scalar`
2. 找到 `POST /api/UserProfiles/Get`
3. 输入测试数据：
   ```json
   {
     "identity": "zhangsan@example.com",
     "password": "Password123!"
   }
   ```
4. 点击 Execute

**或使用 .http 文件：**
打开 `JwtTest.http`，执行第一个请求

**期望结果：**
```json
{
  "user": {
    "id": 1,
    "name": "张三",
    "email": "zhangsan@example.com",
    "phoneNum": "13800138000"
  },
  "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "refreshToken": "base64encodedtoken...",
  "expiresAt": "2026-03-14T12:00:00Z"
}
```

#### 3. 测试受保护端点

**在 Scalar 中配置 Token：**
1. 点击右上角的 🔒 图标
2. 输入：`Bearer YOUR_ACCESS_TOKEN`
3. 确认保存

**测试搜索用户：**
- 找到 `POST /api/UserProfiles/Search`
- 点击 Execute

**期望结果：** 返回用户列表

#### 4. 测试无 Token 访问

**清除 Token：**
- 在 Scalar 中删除已保存的 Token

**再次访问受保护端点：**
- 尝试 `POST /api/UserProfiles/Search`

**期望结果：** 401 Unauthorized

#### 5. 测试过期 Token

使用已过期的 Token 访问

**期望结果：** 401 Unauthorized

---

## 高级功能扩展

### 1. Refresh Token 机制（可选）

#### 创建 RefreshToken 实体
**Domain/Entities/RefreshToken.cs**：
```csharp
using Domain.Common;

namespace Domain.Entities;

public class RefreshToken : BaseAuditableEntity<int>
{
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public bool IsUsed { get; set; }
    public bool IsRevoked { get; set; }
    public int UserId { get; set; }
    public UserProfile User { get; set; } = null!;
}
```

#### 创建 RefreshToken Query
**Application/UserProfiles/Queries/RefreshToken/RefreshTokenQuery.cs**：
```csharp
using Application.UserProfiles.Dtos;
using MediatR;

namespace Application.UserProfiles.Queries.RefreshToken;

public record RefreshTokenQuery : IRequest<LoginResponseDto>
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
}
```

#### 创建 RefreshToken Handler
**Application/UserProfiles/Queries/RefreshToken/RefreshTokenQueryHandler.cs**：
```csharp
using Application.Common.Interfaces;
using Application.UserProfiles.Dtos;
using Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.UserProfiles.Queries.RefreshToken;

public class RefreshTokenQueryHandler : IRequestHandler<RefreshTokenQuery, LoginResponseDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ITokenService _tokenService;
    private readonly IMapper _mapper;

    public RefreshTokenQueryHandler(
        IApplicationDbContext context,
        ITokenService tokenService,
        IMapper mapper)
    {
        _context = context;
        _tokenService = tokenService;
        _mapper = mapper;
    }

    public async Task<LoginResponseDto> Handle(RefreshTokenQuery request, CancellationToken cancellationToken)
    {
        // 1. 验证 Access Token（即使过期也要能提取 Claims）
        var principal = _tokenService.ValidateToken(request.AccessToken);
        if (principal == null)
        {
            throw new UnauthorizedAccessException("无效的 Access Token");
        }

        // 2. 查找 Refresh Token
        var refreshToken = await _context.Set<RefreshToken>()
            .Include(r => r.User)
            .FirstOrDefaultAsync(r =>
                r.Token == request.RefreshToken &&
                !r.IsUsed &&
                !r.IsRevoked &&
                r.ExpiresAt > DateTime.UtcNow,
                cancellationToken);

        if (refreshToken == null)
        {
            throw new UnauthorizedAccessException("无效的 Refresh Token");
        }

        // 3. 标记旧 Refresh Token 为已使用
        refreshToken.IsUsed = true;

        // 4. 生成新的 Token 对
        var newTokens = _tokenService.GenerateTokenPair(
            refreshToken.User.Id,
            refreshToken.User.Email?.Value ?? string.Empty,
            refreshToken.User.PhoneNum ?? string.Empty
        );

        // 5. 保存新的 Refresh Token
        var newRefreshTokenEntity = new RefreshToken
        {
            Token = newTokens.RefreshToken,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            UserId = refreshToken.User.Id
        };

        _context.Set<RefreshToken>().Add(newRefreshTokenEntity);
        await _context.SaveChangesAsync(cancellationToken);

        // 6. 返回结果
        var userDto = _mapper.Map<UserProfileBriefDto>(refreshToken.User);

        return new LoginResponseDto
        {
            User = userDto,
            AccessToken = newTokens.AccessToken,
            RefreshToken = newTokens.RefreshToken,
            ExpiresAt = newTokens.ExpiresAt
        };
    }
}
```

### 2. 权限管理（基于角色）

#### 添加 Role Claim
在生成 Token 时添加角色：
```csharp
public string GenerateAccessToken(int userId, string email, string phoneNum, string role = "User")
{
    var claims = new List<Claim>
    {
        new(ClaimTypes.NameIdentifier, userId.ToString()),
        new(ClaimTypes.Role, role),  // 添加角色
        // ... 其他 claims
    };

    // ...
}
```

#### 使用授权策略
在 **Program.cs** 中配置策略：
```csharp
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy =>
        policy.RequireRole("Admin"));

    options.AddPolicy("UserOrAdmin", policy =>
        policy.RequireRole("User", "Admin"));
});
```

在 Controller 中使用：
```csharp
[HttpPost("AdminAction")]
[Authorize(Policy = "AdminOnly")]
public ActionResult AdminAction()
{
    return Ok("只有管理员可以访问");
}
```

---

## 常见问题

### Q1: Token 过期时间设置多少合适？
**A:**
- **Access Token**: 15-60 分钟（建议 30 分钟）
- **Refresh Token**: 7-30 天（建议 7 天）

### Q2: Secret Key 应该多长？
**A:** 至少 32 字符，建议使用环境变量：
```bash
export JWT_SECRET="your_super_secret_key_at_least_32_characters"
```

### Q3: 如何在开发环境调试 Token？
**A:** 使用 https://jwt.io/ 解码和验证 Token

### Q4: 可以在多个服务间共享 Token 吗？
**A:** 可以，只要：
- 使用相同的 Secret Key
- 使用相同的 Issuer 和 Audience
- Token 的 Claims 包含用户标识

---

## 总结

完成以上步骤后，你的项目将拥有：

✅ **密码安全**：使用 BCrypt 哈希加密存储密码
✅ **用户认证**：通过现有的 `GetUserProfile` API 登录获取 Token
✅ **API 保护**：使用 `[Authorize]` 保护端点
✅ **Token 管理**：自动生成和验证 JWT
✅ **可扩展性**：支持 Refresh Token、角色权限等高级功能
✅ **复用现有架构**：不创建新的 Controller，保持代码简洁

### 设计亮点

**1. 密码安全（重要）**
- 使用 BCrypt 自动加盐哈希
- 无法逆向解密
- 防止数据库泄露导致密码暴露

**2. 复用现有 API**
- 不创建 `AuthController`
- 修改 `GetUserProfile` 返回 Token
- 保持 API 端点数量不变

**3. 使用 Scalar**
- 现代化的 API 文档界面
- 更好的用户体验
- 支持在线测试

**4. 渐进式实施**
- ✅ 密码哈希（已完成）
- ✅ JWT 基础认证（已完成）
- 🔲 Refresh Token（可选）
- 🔲 角色权限管理（可选）

### 密码哈希的重要性

**如果不使用密码哈希：**
- ❌ 数据库泄露 → 所有用户密码直接暴露
- ❌ 内部人员可以看到用户密码
- ❌ 违反数据保护法规（GDPR、个人信息保护法）
- ❌ 用户在多个网站使用相同密码时，连锁风险

**使用 BCrypt 后：**
- ✅ 即使数据库泄露，攻击者也无法获得明文密码
- ✅ 符合安全最佳实践
- ✅ 满足合规要求
- ✅ 自动加盐，防止彩虹表攻击

### 下一步建议
1. ✅ 密码哈希（已完成）
2. 添加 Refresh Token 机制
3. 实现登出功能（将 Refresh Token 标记为失效）
4. 添加日志记录（登录、Token 刷新等）
5. 实现限流和防暴力破解
6. 添加双因素认证（2FA）

---

**文档版本：** 2.0
**最后更新：** 2026-03-14
**作者：** Claude Code Assistant
