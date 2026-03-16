# RefreshToken 实现方案

## 一、项目架构分析

### 1.1 现有架构
项目采用 Clean Architecture（洋葱架构），分为以下层次：

```
src/Services/User/
├── Domain/              # 领域层：实体、值对象、领域逻辑
├── Application/         # 应用层：CQRS、DTO、验证、接口定义
├── Infrastructure/      # 基础设施层：数据持久化、外部服务
└── WebAPI/             # 表示层：API 接口
```

**核心技术栈：**
- **框架**: .NET 10.0 + ASP.NET Core Web API
- **架构模式**: CQRS + MediatR
- **认证**: JWT Bearer Token
- **数据访问**: EF Core + SQL Server
- **密码加密**: BCrypt
- **对象映射**: AutoMapper
- **验证**: FluentValidation

### 1.2 现有 JWT 实现

#### TokenService ([TokenService.cs](src/Services/User/src/Infrastructure/Services/TokenService.cs))
```csharp
public interface ITokenService
{
    string GenerateAccessToken(int userId, string email, string phoneNum);
    string GenerateRefreshToken();
    ClaimsPrincipal? ValidateToken(string token);
    TokenResponse GenerateTokenPair(int userId, string email, string phoneNum);
}
```

**已实现功能：**
- ✅ AccessToken 生成（含用户 Claims）
- ✅ RefreshToken 生成（随机字符串）
- ✅ Token 验证
- ✅ Token 对生成

#### JwtSettings ([JwtSettings.cs](src/Services/User/src/Application/Common/Models/JwtSettings.cs))
```csharp
public class JwtSettings
{
    public string Secret { get; set; }
    public string Issuer { get; set; }
    public string Audience { get; set; }
    public int AccessTokenExpirationMinutes { get; set; }  // 60 分钟
    public int RefreshTokenExpirationDays { get; set; }     // 7 天
}
```

#### 当前登录流程 ([GetUserProfileQueryHandler.cs](src/Services/User/src/Application/UserProfiles/Queries/GetUserProfile/GetUserProfileQueryHandler.cs))
```csharp
// 1. 验证用户名密码
// 2. 生成 Token 对
var tokens = _tokenService.GenerateTokenPair(
    entity.Id,
    entity.Email?.Value ?? string.Empty,
    entity.PhoneNum ?? string.Empty
);

// 3. 返回用户信息 + Token
return new LoginResponseDto
{
    User = userDto,
    AccessToken = tokens.AccessToken,
    RefreshToken = tokens.RefreshToken,
    ExpiresAt = tokens.ExpiresAt
};
```

### 1.3 存在的问题

⚠️ **关键问题：RefreshToken 未持久化**

1. **无存储**: RefreshToken 生成后未保存到数据库
2. **无验证**: 无法验证 RefreshToken 的有效性
3. **无刷新**: 没有 RefreshToken 刷新接口
4. **无撤销**: 无法撤销 RefreshToken（登出/修改密码后仍有效）
5. **无管理**: 无法管理多设备 RefreshToken
6. **安全风险**: RefreshToken 永不过期（除非客户端删除）

---

## 二、RefreshToken 方案设计

### 2.1 核心设计原则

1. **持久化存储**: RefreshToken 必须存储到数据库
2. **安全验证**: 验证 RefreshToken 的有效性、过期时间、设备指纹
3. **滚动更新**: 刷新时生成新的 RefreshToken，旧的失效
4. **多设备支持**: 每个设备维护独立的 RefreshToken
5. **撤销机制**: 支持主动撤销（登出、修改密码）

### 2.2 数据模型设计

#### 方案 A: 独立 RefreshToken 实体（推荐）

```csharp
// Domain/Entities/RefreshToken.cs
public class RefreshToken : BaseEntity<int>
{
    public int UserId { get; set; }
    public UserProfile User { get; set; } = null!;

    public string Token { get; set; } = null!;
    public DateTime ExpiresAt { get; set; }

    // 设备信息（可选）
    public string? DeviceId { get; set; }
    public string? DeviceName { get; set; }
    public string? DeviceType { get; set; }  // Mobile/Web/Desktop

    // 状态管理
    public bool IsRevoked { get; set; }
    public DateTime? RevokedAt { get; set; }
    public string? RevokedReason { get; set; }  // Logout/PasswordChanged/Security

    // 审计
    public DateTime Created { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }

    // 替换关系（滚动更新）
    public int? ReplacedByTokenId { get; set; }
    public RefreshToken? ReplacedByToken { get; set; }
    public int? ReplacesTokenId { get; set; }
    public RefreshToken? ReplacesToken { get; set; }
}
```

**优点：**
- ✅ 清晰的职责分离
- ✅ 便于扩展（设备管理、审计）
- ✅ 支持多设备场景
- ✅ 完整的生命周期管理

**缺点：**
- ❌ 需要额外的数据库表
- ❌ 需要额外的关联查询

#### 方案 B: UserProfile 扩展属性

```csharp
// Domain/Entities/UserProfile.cs
public class UserProfile : BaseAuditableEntity<int>
{
    // ... 现有属性 ...

    public string? RefreshToken { get; set; }
    public DateTime? RefreshTokenExpiry { get; set; }
}
```

**优点：**
- ✅ 实现简单
- ✅ 无需额外表

**缺点：**
- ❌ 只支持单设备
- ❌ 无法记录历史
- ❌ 无法区分设备

**推荐**: 方案 A（独立实体），支持多设备和完整生命周期管理。

### 2.3 数据库表设计

```sql
CREATE TABLE RefreshTokens (
    Id INT PRIMARY KEY IDENTITY(1,1),
    UserId INT NOT NULL,
    Token NVARCHAR(512) NOT NULL,
    ExpiresAt DATETIME2 NOT NULL,
    DeviceId NVARCHAR(256) NULL,
    DeviceName NVARCHAR(256) NULL,
    DeviceType NVARCHAR(50) NULL,
    IsRevoked BIT NOT NULL DEFAULT 0,
    RevokedAt DATETIME2 NULL,
    RevokedReason NVARCHAR(256) NULL,
    Created DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    IpAddress NVARCHAR(64) NULL,
    UserAgent NVARCHAR(512) NULL,
    ReplacedByTokenId INT NULL,
    ReplacesTokenId INT NULL,

    CONSTRAINT FK_RefreshTokens_Users FOREIGN KEY (UserId)
        REFERENCES UserProfiles(Id) ON DELETE CASCADE,
    CONSTRAINT FK_RefreshTokens_ReplacedBy FOREIGN KEY (ReplacedByTokenId)
        REFERENCES RefreshTokens(Id),
    CONSTRAINT FK_RefreshTokens_Replaces FOREIGN KEY (ReplacesTokenId)
        REFERENCES RefreshTokens(Id)
);

CREATE INDEX IX_RefreshTokens_Token ON RefreshTokens(Token);
CREATE INDEX IX_RefreshTokens_UserId ON RefreshTokens(UserId);
CREATE INDEX IX_RefreshTokens_ExpiresAt ON RefreshTokens(ExpiresAt);
```

### 2.4 API 接口设计

#### 2.4.1 刷新 Token 接口

**请求:**
```http
POST /api/auth/refresh-token
Content-Type: application/json

{
  "accessToken": "eyJhbGciOiJIUzI1NiIs...",
  "refreshToken": "dGhpcyBpcyBhIHJlZnJlc2g..."
}
```

**响应 (成功):**
```json
{
  "accessToken": "new_access_token",
  "refreshToken": "new_refresh_token",
  "expiresAt": "2026-03-16T12:00:00Z"
}
```

**响应 (失败):**
```json
{
  "error": "invalid_refresh_token",
  "message": "Refresh token is invalid or expired"
}
```

#### 2.4.2 登出接口（撤销 RefreshToken）

**请求:**
```http
POST /api/auth/logout
Authorization: Bearer {access_token}
Content-Type: application/json

{
  "refreshToken": "dGhpcyBpcyBhIHJlZnJlc2g..."
}
```

#### 2.4.3 撤销所有设备接口

**请求:**
```http
POST /api/auth/revoke-all
Authorization: Bearer {access_token}
```

---

## 三、实现方案

### 3.1 Domain 层实现

#### 3.1.1 创建 RefreshToken 实体

**文件**: [src/Services/User/src/Domain/Entities/RefreshToken.cs](src/Services/User/src/Domain/Entities/RefreshToken.cs)

```csharp
namespace User.Domain.Entities
{
    public class RefreshToken : BaseEntity<int>
    {
        public int UserId { get; set; }
        public UserProfile User { get; set; } = null!;

        public string Token { get; set; } = null!;
        public DateTime ExpiresAt { get; set; }

        public string? DeviceId { get; set; }
        public string? DeviceName { get; set; }
        public string? DeviceType { get; set; }

        public bool IsRevoked { get; set; }
        public DateTime? RevokedAt { get; set; }
        public string? RevokedReason { get; set; }

        public DateTime Created { get; set; }
        public string? IpAddress { get; set; }
        public string? UserAgent { get; set; }

        public int? ReplacedByTokenId { get; set; }
        public RefreshToken? ReplacedByToken { get; set; }
        public int? ReplacesTokenId { get; set; }
        public RefreshToken? ReplacesToken { get; set; }

        /// <summary>
        /// 检查 Token 是否过期
        /// </summary>
        public bool IsExpired => DateTime.UtcNow >= ExpiresAt;

        /// <summary>
        /// 检查 Token 是否活跃（未撤销且未过期）
        /// </summary>
        public bool IsActive => !IsRevoked && !IsExpired;

        /// <summary>
        /// 撤销 Token
        /// </summary>
        public void Revoke(string reason)
        {
            IsRevoked = true;
            RevokedAt = DateTime.UtcNow;
            RevokedReason = reason;
        }
    }
}
```

### 3.2 Application 层实现

#### 3.2.1 创建 DTO

**文件**: [src/Services/User/src/Application/Auth/Dtos/RefreshTokenRequestDto.cs](src/Services/User/src/Application/Auth/Dtos/RefreshTokenRequestDto.cs)

```csharp
using System.ComponentModel.DataAnnotations;

namespace User.Application.Auth.Dtos
{
    public class RefreshTokenRequestDto
    {
        [Required]
        public string AccessToken { get; set; } = null!;

        [Required]
        public string RefreshToken { get; set; } = null!;
    }
}
```

**文件**: [src/Services/User/src/Application/Auth/Dtos/LogoutRequestDto.cs](src/Services/User/src/Application/Auth/Dtos/LogoutRequestDto.cs)

```csharp
using System.ComponentModel.DataAnnotations;

namespace User.Application.Auth.Dtos
{
    public class LogoutRequestDto
    {
        [Required]
        public string RefreshToken { get; set; } = null!;
    }
}
```

#### 3.2.2 创建 Command

**文件**: [src/Services/User/src/Application/Auth/Commands/RefreshToken/RefreshTokenCommand.cs](src/Services/User/src/Application/Auth/Commands/RefreshToken/RefreshTokenCommand.cs)

```csharp
using MediatR;
using User.Application.Auth.Dtos;
using User.Application.Common.Models;

namespace User.Application.Auth.Commands.RefreshToken
{
    public record RefreshTokenCommand : IRequest<TokenResponse>
    {
        public string AccessToken { get; set; } = null!;
        public string RefreshToken { get; set; } = null!;
    }

    public class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, TokenResponse>
    {
        private readonly IApplicationDbContext _context;
        private readonly ITokenService _tokenService;

        public RefreshTokenCommandHandler(
            IApplicationDbContext context,
            ITokenService tokenService)
        {
            _context = context;
            _tokenService = tokenService;
        }

        public async Task<TokenResponse> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
        {
            // 1. 验证 AccessToken（即使过期，也要获取用户信息）
            var principal = _tokenService.ValidateToken(request.AccessToken);
            if (principal == null)
            {
                throw new UnauthorizedAccessException("Invalid access token");
            }

            var userIdClaim = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out int userId))
            {
                throw new UnauthorizedAccessException("Invalid user identifier in token");
            }

            // 2. 查找 RefreshToken
            var refreshToken = await _context.Set<RefreshToken>()
                .Include(rt => rt.User)
                .FirstOrDefaultAsync(rt => rt.Token == request.RefreshToken && rt.UserId == userId, cancellationToken);

            if (refreshToken == null)
            {
                throw new UnauthorizedAccessException("Refresh token not found");
            }

            // 3. 检查 RefreshToken 状态
            if (refreshToken.IsRevoked)
            {
                // 如果已被撤销，可能是安全事件，撤销该用户所有 Token
                await RevokeUserRefreshTokensAsync(userId, "Potential security breach", cancellationToken);
                throw new UnauthorizedAccessException("Refresh token has been revoked");
            }

            if (refreshToken.IsExpired)
            {
                throw new UnauthorizedAccessException("Refresh token has expired");
            }

            // 4. 生成新的 Token 对
            var newTokens = _tokenService.GenerateTokenPair(
                refreshToken.User.Id,
                refreshToken.User.Email?.Value ?? string.Empty,
                refreshToken.User.PhoneNum ?? string.Empty
            );

            // 5. 创建新的 RefreshToken 记录
            var newRefreshToken = new RefreshToken
            {
                UserId = refreshToken.User.Id,
                Token = newTokens.RefreshToken,
                ExpiresAt = DateTime.UtcNow.AddDays(_tokenService.GetRefreshTokenExpirationDays()),
                DeviceId = refreshToken.DeviceId,
                DeviceName = refreshToken.DeviceName,
                DeviceType = refreshToken.DeviceType,
                IpAddress = refreshToken.IpAddress,
                UserAgent = refreshToken.UserAgent,
                Created = DateTime.UtcNow,
                ReplacesTokenId = refreshToken.Id
            };

            // 6. 撤销旧的 RefreshToken
            refreshToken.Revoke("Replaced by new token");
            refreshToken.ReplacedByTokenId = newRefreshToken.Id;

            // 7. 保存更改
            _context.Set<RefreshToken>().Add(newRefreshToken);
            await _context.SaveChangesAsync(cancellationToken);

            // 8. 返回新的 Token 对
            return newTokens;
        }

        private async Task RevokeUserRefreshTokensAsync(int userId, string reason, CancellationToken cancellationToken)
        {
            var activeTokens = await _context.Set<RefreshToken>()
                .Where(rt => rt.UserId == userId && rt.IsActive)
                .ToListAsync(cancellationToken);

            foreach (var token in activeTokens)
            {
                token.Revoke(reason);
            }

            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
```

**文件**: [src/Services/User/src/Application/Auth/Commands/Logout/LogoutCommand.cs](src/Services/User/src/Application/Auth/Commands/Logout/LogoutCommand.cs)

```csharp
using MediatR;
using User.Application.Auth.Dtos;

namespace User.Application.Auth.Commands.Logout
{
    public record LogoutCommand : IRequest
    {
        public int UserId { get; set; }
        public string RefreshToken { get; set; } = null!;
    }

    public class LogoutCommandHandler : IRequestHandler<LogoutCommand>
    {
        private readonly IApplicationDbContext _context;

        public LogoutCommandHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task Handle(LogoutCommand request, CancellationToken cancellationToken)
        {
            // 查找并撤销指定的 RefreshToken
            var refreshToken = await _context.Set<RefreshToken>()
                .FirstOrDefaultAsync(rt => rt.Token == request.RefreshToken && rt.UserId == request.UserId, cancellationToken);

            if (refreshToken != null && !refreshToken.IsRevoked)
            {
                refreshToken.Revoke("User logout");
                await _context.SaveChangesAsync(cancellationToken);
            }

            return Unit.Value;
        }
    }
}
```

**文件**: [src/Services/User/src/Application/Auth/Commands/RevokeAllTokens/RevokeAllTokensCommand.cs](src/Services/User/src/Application/Auth/Commands/RevokeAllTokens/RevokeAllTokensCommand.cs)

```csharp
using MediatR;

namespace User.Application.Auth.Commands.RevokeAllTokens
{
    public record RevokeAllTokensCommand : IRequest
    {
        public int UserId { get; set; }
    }

    public class RevokeAllTokensCommandHandler : IRequestHandler<RevokeAllTokensCommand>
    {
        private readonly IApplicationDbContext _context;

        public RevokeAllTokensCommandHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task Handle(RevokeAllTokensCommand request, CancellationToken cancellationToken)
        {
            var activeTokens = await _context.Set<RefreshToken>()
                .Where(rt => rt.UserId == request.UserId && rt.IsActive)
                .ToListAsync(cancellationToken);

            foreach (var token in activeTokens)
            {
                token.Revoke("Revoked all tokens");
            }

            await _context.SaveChangesAsync(cancellationToken);
            return Unit.Value;
        }
    }
}
```

#### 3.2.3 更新 ITokenService 接口

**文件**: [src/Services/User/src/Application/Common/Interfaces/ITokenService.cs](src/Services/User/src/Application/Common/Interfaces/ITokenService.cs)

```csharp
using System.Security.Claims;

namespace User.Application.Common.Interfaces
{
    public interface ITokenService
    {
        string GenerateAccessToken(int userId, string email, string phoneNum);
        string GenerateRefreshToken();
        ClaimsPrincipal? ValidateToken(string token);
        TokenResponse GenerateTokenPair(int userId, string email, string phoneNum);
        int GetRefreshTokenExpirationDays();
    }
}
```

#### 3.2.4 更新 TokenService 实现

**文件**: [src/Services/User/src/Infrastructure/Services/TokenService.cs](src/Services/User/src/Infrastructure/Services/TokenService.cs)

```csharp
public int GetRefreshTokenExpirationDays()
{
    return _jwtSettings.RefreshTokenExpirationDays;
}
```

### 3.3 Infrastructure 层实现

#### 3.3.1 更新 ApplicationDbContext

**文件**: [src/Services/User/src/Infrastructure/Persistence/ApplicationDbContext.cs](src/Services/User/src/Infrastructure/Persistence/ApplicationDbContext.cs)

```csharp
public class ApplicationDbContext : DbContext, IApplicationDbContext
{
    public DbSet<UserProfile> UserProfiles => Set<UserProfile>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // UserProfile 配置
        modelBuilder.ApplyConfiguration(new UserProfileConfiguration());

        // RefreshToken 配置
        modelBuilder.ApplyConfiguration(new RefreshTokenConfiguration());
    }
}
```

#### 3.3.2 创建 RefreshToken 配置

**文件**: [src/Services/User/src/Infrastructure/Persistence/Configurations/RefreshTokenConfiguration.cs](src/Services/User/src/Infrastructure/Persistence/Configurations/RefreshTokenConfiguration.cs)

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using User.Domain.Entities;

namespace User.Infrastructure.Persistence.Configurations
{
    public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
    {
        public void Configure(EntityTypeBuilder<RefreshToken> builder)
        {
            builder.ToTable("RefreshTokens");

            builder.HasKey(rt => rt.Id);

            builder.Property(rt => rt.Token)
                .IsRequired()
                .HasMaxLength(512);

            builder.Property(rt => rt.DeviceId)
                .HasMaxLength(256);

            builder.Property(rt => rt.DeviceName)
                .HasMaxLength(256);

            builder.Property(rt => rt.DeviceType)
                .HasMaxLength(50);

            builder.Property(rt => rt.RevokedReason)
                .HasMaxLength(256);

            builder.Property(rt => rt.IpAddress)
                .HasMaxLength(64);

            builder.Property(rt => rt.UserAgent)
                .HasMaxLength(512);

            // 关系配置
            builder.HasOne(rt => rt.User)
                .WithMany(u => u.RefreshTokens)
                .HasForeignKey(rt => rt.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(rt => rt.ReplacedByToken)
                .WithMany()
                .HasForeignKey(rt => rt.ReplacedByTokenId);

            builder.HasOne(rt => rt.ReplacesToken)
                .WithMany()
                .HasForeignKey(rt => rt.ReplacesTokenId);

            // 索引
            builder.HasIndex(rt => rt.Token);
            builder.HasIndex(rt => rt.UserId);
            builder.HasIndex(rt => rt.ExpiresAt);
        }
    }
}
```

#### 3.3.3 更新 UserProfile 实体

**文件**: [src/Services/User/src/Domain/Entities/UserProfile.cs](src/Services/User/src/Domain/Entities/UserProfile.cs)

```csharp
namespace User.Domain.Entities
{
    public class UserProfile : BaseAuditableEntity<int>
    {
        public string Name { get; set; } = null!;
        public Email? Email { get; set; }
        public string? PhoneNum { get; set; }
        public string Password { get; set; } = null!;

        // 添加导航属性
        public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
    }
}
```

### 3.4 WebAPI 层实现

#### 3.4.1 创建 AuthController

**文件**: [src/Services/User/src/WebAPI/Controllers/AuthController.cs](src/Services/User/src/WebAPI/Controllers/AuthController.cs)

```csharp
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using User.Application.Auth.Commands.Logout;
using User.Application.Auth.Commands.RefreshToken;
using User.Application.Auth.Commands.RevokeAllTokens;
using User.Application.Auth.Dtos;

namespace WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly ISender _sender;

        public AuthController(ISender sender)
        {
            _sender = sender;
        }

        /// <summary>
        /// 刷新 Token
        /// </summary>
        [AllowAnonymous]
        [HttpPost("refresh-token")]
        public async Task<ActionResult<TokenResponse>> RefreshToken([FromBody] RefreshTokenRequestDto request)
        {
            var command = new RefreshTokenCommand
            {
                AccessToken = request.AccessToken,
                RefreshToken = request.RefreshToken
            };

            var result = await _sender.Send(command);
            return Ok(result);
        }

        /// <summary>
        /// 登出（撤销当前 RefreshToken）
        /// </summary>
        [Authorize]
        [HttpPost("logout")]
        public async Task<ActionResult> Logout([FromBody] LogoutRequestDto request)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

            var command = new LogoutCommand
            {
                UserId = userId,
                RefreshToken = request.RefreshToken
            };

            await _sender.Send(command);
            return Ok();
        }

        /// <summary>
        /// 撤销所有设备的 RefreshToken
        /// </summary>
        [Authorize]
        [HttpPost("revoke-all")]
        public async Task<ActionResult> RevokeAllTokens()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

            var command = new RevokeAllTokensCommand { UserId = userId };
            await _sender.Send(command);

            return Ok();
        }
    }
}
```

#### 3.4.2 更新登录逻辑

**文件**: [src/Services/User/src/Application/UserProfiles/Queries/GetUserProfile/GetUserProfileQueryHandler.cs](src/Services/User/src/Application/UserProfiles/Queries/GetUserProfile/GetUserProfileQueryHandler.cs)

```csharp
public async Task<LoginResponseDto> Handle(GetUserProfileQuery request, CancellationToken cancellationToken)
{
    // ... 现有的用户验证逻辑 ...

    // 生成 Token
    var tokens = _tokenService.GenerateTokenPair(
        entity.Id,
        entity.Email?.Value ?? string.Empty,
        entity.PhoneNum ?? string.Empty
    );

    // 保存 RefreshToken 到数据库
    var refreshTokenEntity = new RefreshToken
    {
        UserId = entity.Id,
        Token = tokens.RefreshToken,
        ExpiresAt = tokens.ExpiresAt.AddDays(_tokenService.GetRefreshTokenExpirationDays()),
        Created = DateTime.UtcNow,
        // 可以从 HttpContext 中提取设备信息
        DeviceId = null,  // TODO: 从请求中提取
        DeviceName = null,  // TODO: 从请求中提取
        IpAddress = null,  // TODO: 从请求中提取
        UserAgent = null  // TODO: 从请求中提取
    };

    _context.Set<RefreshToken>().Add(refreshTokenEntity);
    await _context.SaveChangesAsync(cancellationToken);

    // 映射用户信息
    var userDto = _mapper.Map<UserProfileBriefDto>(entity);

    return new LoginResponseDto
    {
        User = userDto,
        AccessToken = tokens.AccessToken,
        RefreshToken = tokens.RefreshToken,
        ExpiresAt = tokens.ExpiresAt
    };
}
```

### 3.5 数据库迁移

```bash
# 创建迁移
dotnet ef migrations add AddRefreshTokens --project src/Infrastructure --startup-project src/WebAPI

# 应用迁移
dotnet ef database update --project src/Infrastructure --startup-project src/WebAPI
```

---

## 四、前端集成指南

### 4.1 Token 存储策略

```typescript
// 推荐使用 localStorage 存储 Token
interface TokenStorage {
  accessToken: string;
  refreshToken: string;
  expiresAt: string;
}

// 保存 Token
function saveTokens(tokens: TokenStorage) {
  localStorage.setItem('auth_tokens', JSON.stringify(tokens));
}

// 获取 Token
function getTokens(): TokenStorage | null {
  const stored = localStorage.getItem('auth_tokens');
  return stored ? JSON.parse(stored) : null;
}

// 清除 Token
function clearTokens() {
  localStorage.removeItem('auth_tokens');
}
```

### 4.2 Axios 拦截器（自动刷新）

```typescript
import axios from 'axios';

const api = axios.create({
  baseURL: 'https://api.example.com'
});

let isRefreshing = false;
let failedQueue: any[] = [];

api.interceptors.request.use((config) => {
  const tokens = getTokens();
  if (tokens) {
    config.headers.Authorization = `Bearer ${tokens.accessToken}`;
  }
  return config;
});

api.interceptors.response.use(
  (response) => response,
  async (error) => {
    const originalRequest = error.config;

    if (error.response?.status === 401 && !originalRequest._retry) {
      if (isRefreshing) {
        return new Promise((resolve, reject) => {
          failedQueue.push({ resolve, reject });
        }).then(token => {
          originalRequest.headers.Authorization = `Bearer ${token}`;
          return api(originalRequest);
        });
      }

      originalRequest._retry = true;
      isRefreshing = true;

      try {
        const tokens = getTokens();
        const response = await api.post('/api/auth/refresh-token', {
          accessToken: tokens?.accessToken,
          refreshToken: tokens?.refreshToken
        });

        const newTokens = response.data;
        saveTokens(newTokens);

        failedQueue.forEach(prom => prom.resolve(newTokens.accessToken));
        failedQueue = [];

        originalRequest.headers.Authorization = `Bearer ${newTokens.accessToken}`;
        return api(originalRequest);
      } catch (refreshError) {
        failedQueue.forEach(prom => prom.reject(refreshError));
        failedQueue = [];
        clearTokens();
        window.location.href = '/login';
        return Promise.reject(refreshError);
      } finally {
        isRefreshing = false;
      }
    }

    return Promise.reject(error);
  }
);
```

### 4.3 登出流程

```typescript
async function logout() {
  try {
    const tokens = getTokens();
    await api.post('/api/auth/logout', {
      refreshToken: tokens?.refreshToken
    });
  } catch (error) {
    console.error('Logout failed:', error);
  } finally {
    clearTokens();
    window.location.href = '/login';
  }
}
```

---

## 五、安全建议

### 5.1 Token 安全性

1. **HTTPS**: 强制使用 HTTPS 传输 Token
2. **Token 过期**:
   - AccessToken: 30-60 分钟
   - RefreshToken: 7-14 天
3. **Token 存储**:
   - 前端: 使用 `localStorage` 或 `sessionStorage`
   - 避免 Cookie (防止 CSRF)
   - 敏感应用考虑使用内存存储

### 5.2 RefreshToken 安全

1. **随机性**: 使用加密安全的随机数生成器
2. **唯一性**: 每次生成唯一的 RefreshToken
3. **一次性**: 刷新后旧的 RefreshToken 立即失效
4. **设备绑定**: 绑定设备指纹/IP 地址
5. **异常检测**:
   - 监控异常的刷新行为
   - 超过阈值时撤销所有 Token 并要求重新登录

### 5.3 防止攻击

1. **Token 劫持**:
   - 使用 HTTPS
   - 实现 Token 指纹（设备 ID + User-Agent）
   - 监控异常的 Token 使用

2. **重放攻击**:
   - RefreshToken 使用后立即失效
   - 实现时间窗口验证

3. **暴力破解**:
   - 限制刷新频率（如每分钟最多 5 次）
   - IP 封禁策略

### 5.4 审计日志

建议添加审计日志，记录：
- Token 生成时间、设备信息
- Token 刷新记录
- Token 撤销记录
- 异常访问尝试

---

## 六、测试方案

### 6.1 单元测试

```csharp
// 测试 RefreshTokenCommandHandler
public class RefreshTokenCommandHandlerTests
{
    [Fact]
    public async Task Handle_ValidRefreshToken_ReturnsNewTokens()
    {
        // Arrange
        var context = CreateDbContext();
        var tokenService = CreateTokenService();
        var handler = new RefreshTokenCommandHandler(context, tokenService);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotNull(result.AccessToken);
        Assert.NotNull(result.RefreshToken);
    }

    [Fact]
    public async Task Handle_ExpiredRefreshToken_ThrowsUnauthorizedException()
    {
        // Arrange
        var expiredToken = CreateExpiredRefreshToken();

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedException>(
            () => handler.Handle(command, CancellationToken.None));
    }
}
```

### 6.2 集成测试

```csharp
[Fact]
public async Task RefreshToken_RefreshFlow_Success()
{
    // 1. 登录获取 Token
    var loginResponse = await Client.PostAsJsonAsync("/api/auth/login", loginDto);
    var tokens = await loginResponse.Content.ReadFromJsonAsync<TokenResponse>();

    // 2. 使用 RefreshToken 刷新
    var refreshResponse = await Client.PostAsJsonAsync("/api/auth/refresh-token", tokens);
    var newTokens = await refreshResponse.Content.ReadFromJsonAsync<TokenResponse>();

    // 3. 验证新 Token 有效
    Assert.NotEqual(tokens.AccessToken, newTokens.AccessToken);
    Assert.NotEqual(tokens.RefreshToken, newTokens.RefreshToken);

    // 4. 验证旧 Token 失效
    var oldRefreshResponse = await Client.PostAsJsonAsync("/api/auth/refresh-token", tokens);
    Assert.Equal(HttpStatusCode.Unauthorized, oldRefreshResponse.StatusCode);
}
```

---

## 七、实施步骤

### 阶段一：基础实现（必需）

- [ ] 1. 创建 `RefreshToken` 实体
- [ ] 2. 创建数据库配置和迁移
- [ ] 3. 实现 `RefreshTokenCommand`
- [ ] 4. 实现 `LogoutCommand`
- [ ] 5. 创建 `AuthController`
- [ ] 6. 更新登录逻辑，保存 RefreshToken
- [ ] 7. 运行数据库迁移

### 阶段二：增强功能（推荐）

- [ ] 8. 实现 `RevokeAllTokensCommand`
- [ ] 9. 添加设备信息提取（DeviceId, DeviceName 等）
- [ ] 10. 添加刷新频率限制
- [ ] 11. 实现 Token 审计日志
- [ ] 12. 添加异常检测机制

### 阶段三：前端集成（必需）

- [ ] 13. 实现前端 Token 存储逻辑
- [ ] 14. 实现 Axios 拦截器（自动刷新）
- [ ] 15. 实现登出流程
- [ ] 16. 添加 Token 过期提示

### 阶段四：测试和优化（推荐）

- [ ] 17. 编写单元测试
- [ ] 18. 编写集成测试
- [ ] 19. 性能测试和优化
- [ ] 20. 安全审计

---

## 八、总结

本方案提供了完整的 RefreshToken 实现指南，包括：

1. **数据模型**: 独立的 RefreshToken 实体，支持多设备和生命周期管理
2. **业务逻辑**: Token 刷新、撤销、管理等功能
3. **API 接口**: RESTful API 设计
4. **前端集成**: Token 存储和自动刷新机制
5. **安全建议**: 防止常见攻击的安全措施
6. **实施步骤**: 分阶段实施指南

**核心优势：**
- ✅ 完整的 RefreshToken 生命周期管理
- ✅ 支持多设备登录
- ✅ 安全的滚动更新机制
- ✅ 灵活的撤销策略
- ✅ 可扩展的设备管理
- ✅ 完善的审计能力

遵循本方案，你可以在现有架构基础上快速实现安全、可靠的 RefreshToken 功能。
