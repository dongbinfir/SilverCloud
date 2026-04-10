# Redis 缓存配置实现文档（基于 HybridCache）

## 一、概述

本文档记录了在 SilverCloud User 微服务中集成 **HybridCache**（L1 本地内存 + L2 Redis 分布式缓存）的完整过程。

HybridCache 是 `Microsoft.Extensions.Caching.Hybrid` 提供的两级缓存方案：
- **L1 缓存**：进程内内存缓存（`MemoryCache`），速度最快，零网络开销。
- **L2 缓存**：Redis 分布式缓存（`IDistributedCache`），跨进程 / 跨实例共享。

默认配置中 **L1 关闭**（`LocalCacheExpiration = TimeSpan.Zero`），仅使用 Redis 作为缓存后端。需要时可通过 `CacheOptions` 按需开启 L1。

**Redis 连接信息：**
- 地址：`localhost:6379`
- 密码：`password123`
- 实例前缀：`SilverCloud_User_`

---

## 二、涉及的文件变更清单

| 操作 | 文件路径 | 说明 |
|------|---------|------|
| **新增** | `src/Application/Common/Interfaces/ICacheService.cs` | 缓存服务抽象接口 |
| **新增** | `src/Application/Common/Models/RedisSettings.cs` | Redis 连接配置模型 |
| **新增** | `src/Application/Common/Models/CacheOptions.cs` | 缓存选项模型（过期时间、L1 开关） |
| **新增** | `src/Infrastructure/Caching/RedisCacheService.cs` | HybridCache 缓存服务实现 |
| **修改** | `src/Infrastructure/Infrastructure.csproj` | 添加 `Hybrid` + `StackExchangeRedis` 包引用 |
| **修改** | `src/Infrastructure/DependencyInjection.cs` | 注册 Redis + HybridCache + ICacheService |
| **修改** | `src/WebAPI/appsettings.Development.json` | 添加 `RedisSettings` 配置节 |
| **修改** | `src/Application/UserProfiles/Queries/GetUserProfile/GetUserProfileQuery.cs` | 添加缓存逻辑 |

---

## 三、详细配置步骤

### 步骤 1：添加 NuGet 包

只需要在 **Infrastructure.csproj** 中添加包引用。Application 层的 `ICacheService` 接口只使用了自定义的 `CacheOptions` 类，不依赖任何缓存框架的类型，因此 **Application 层无需安装任何缓存相关 NuGet 包**。

Infrastructure 层需要两个包：

```xml
<!-- HybridCache 核心包：提供 HybridCache 类和 AddHybridCache 扩展方法 -->
<PackageReference Include="Microsoft.Extensions.Caching.Hybrid" Version="10.0.5" />

<!-- StackExchange.Redis 包：提供 IDistributedCache 的 Redis 实现，作为 HybridCache 的 L2 后端 -->
<PackageReference Include="Microsoft.Extensions.Caching.StackExchangeRedis" Version="10.0.5" />
```

**两个包的关系：**
- `StackExchangeRedis` 包注册 `IDistributedCache` 的 Redis 实现。
- `Hybrid` 包的 `AddHybridCache()` 会自动检测容器中是否有 `IDistributedCache` 注册，如果有就作为 L2 后端使用。
- 如果不注册 `StackExchangeRedis`，HybridCache 只有 L1 内存缓存，没有 L2。

---

### 步骤 2：创建 Redis 连接配置模型

**文件路径:** `src/Application/Common/Models/RedisSettings.cs`

```csharp
namespace User.Application.Common.Models
{
    /// <summary>
    /// Redis 配置模型
    /// </summary>
    public class RedisSettings
    {
        public const string SectionName = "RedisSettings";

        /// <summary>
        /// Redis 连接字符串，例如 localhost:6379
        /// </summary>
        public string ConnectionString { get; set; } = null!;

        /// <summary>
        /// Redis 密码
        /// </summary>
        public string? Password { get; set; }

        /// <summary>
        /// 实例名称前缀
        /// </summary>
        public string InstanceName { get; set; } = "SilverCloud_User_";
    }
}
```

**字段说明：**
| 字段 | 作用 | 示例 |
|------|------|------|
| `SectionName` | 对应 `appsettings.json` 中的配置节名称 | `"RedisSettings"` |
| `ConnectionString` | Redis 服务器地址（不含密码） | `"localhost:6379"` |
| `Password` | Redis 认证密码 | `"password123"` |
| `InstanceName` | Redis Key 前缀，防止多服务 Key 冲突 | `"SilverCloud_User_"` |

> **实际存入 Redis 的 Key 格式：** `{InstanceName}{业务Key}`，例如 `SilverCloud_User_UserProfile_42`。

---

### 步骤 3：创建缓存选项模型

**文件路径:** `src/Application/Common/Models/CacheOptions.cs`

```csharp
namespace User.Application.Common.Models
{
    /// <summary>
    /// 缓存配置选项
    /// </summary>
    public class CacheOptions
    {
        /// <summary>
        /// L2（Redis）过期时间，默认 10 分钟
        /// </summary>
        public TimeSpan Expiration { get; set; } = TimeSpan.FromMinutes(10);

        /// <summary>
        /// 是否启用 L1 本地内存缓存
        /// </summary>
        public bool EnableLocalCache { get; set; } = false;

        /// <summary>
        /// L1 本地内存缓存过期时间，默认 5 分钟（仅 EnableLocalCache = true 时生效）
        /// </summary>
        public TimeSpan LocalCacheExpiration { get; set; } = TimeSpan.FromMinutes(5);
    }
}
```

**设计目的：**

封装 `HybridCacheEntryOptions`，让 Application 层的调用者不需要直接引用 `HybridCacheEntryOptions`，而是通过更直观的 `CacheOptions` 来配置。

**字段说明：**
| 字段 | 默认值 | 作用 |
|------|--------|------|
| `Expiration` | 10 分钟 | L2（Redis）缓存的绝对过期时间 |
| `EnableLocalCache` | `false` | 是否开启 L1 进程内内存缓存 |
| `LocalCacheExpiration` | 5 分钟 | L1 缓存过期时间（仅 `EnableLocalCache = true` 时生效） |

**映射规则：** 在 `HybridCacheService` 中，`CacheOptions` 会被转换为 `HybridCacheEntryOptions`：
- `EnableLocalCache = false` → `LocalCacheExpiration = TimeSpan.Zero`（禁用 L1）
- `EnableLocalCache = true` → `LocalCacheExpiration = CacheOptions.LocalCacheExpiration`

---

### 步骤 4：定义缓存服务接口

**文件路径:** `src/Application/Common/Interfaces/ICacheService.cs`

```csharp
namespace User.Application.Common.Interfaces
{
    /// <summary>
    /// 缓存服务接口
    /// </summary>
    public interface ICacheService
    {
        /// <summary>
        /// 获取或设置缓存（使用默认配置）
        /// </summary>
        Task<T> GetOrSetAsync<T>(string key, Func<CancellationToken, ValueTask<T>> factory,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 获取或设置缓存（自定义配置：过期时间、是否启用 L1、L1 过期时间）
        /// </summary>
        Task<T> GetOrSetAsync<T>(string key, Func<CancellationToken, ValueTask<T>> factory,
            Action<CacheOptions> configure, CancellationToken cancellationToken = default);

        /// <summary>
        /// 移除缓存
        /// </summary>
        Task RemoveAsync(string key, CancellationToken cancellationToken = default);
    }
}
```

**接口设计要点：**

1. **接口定义在 Application 层**，遵循 Clean Architecture 依赖反转原则，Application 层不依赖 Infrastructure。
2. **`GetOrSetAsync` 封装了"查缓存 + 回源 + 写缓存"三步操作**：缓存命中直接返回；未命中则调用 `factory` 获取数据，自动写入缓存后返回。
3. **两个重载**：
   - 无参版本：使用 DI 注册时设定的全局默认配置。
   - `Action<CacheOptions>` 版本：通过委托按需覆盖默认配置。
4. **`factory` 参数类型是 `Func<CancellationToken, ValueTask<T>>`**：这是 `HybridCache.GetOrCreateAsync` 要求的签名，使用 `ValueTask<T>` 而非 `Task<T>` 以减少异步状态机的堆分配。

---

### 步骤 5：实现缓存服务

**文件路径:** `src/Infrastructure/Caching/RedisCacheService.cs`

```csharp
using Microsoft.Extensions.Caching.Hybrid;
using User.Application.Common.Interfaces;
using User.Application.Common.Models;

namespace User.Infrastructure.Caching
{
    /// <summary>
    /// 基于 HybridCache 的缓存服务实现（L1 内存 + L2 Redis）
    /// </summary>
    public class HybridCacheService : ICacheService
    {
        private readonly HybridCache _cache;

        public HybridCacheService(HybridCache cache)
        {
            _cache = cache;
        }

        public async Task<T> GetOrSetAsync<T>(string key,
            Func<CancellationToken, ValueTask<T>> factory,
            CancellationToken cancellationToken = default)
        {
            return await _cache.GetOrCreateAsync(key, factory,
                cancellationToken: cancellationToken);
        }

        public async Task<T> GetOrSetAsync<T>(string key,
            Func<CancellationToken, ValueTask<T>> factory,
            Action<CacheOptions> configure,
            CancellationToken cancellationToken = default)
        {
            var cacheOptions = new CacheOptions();
            configure(cacheOptions);

            var entryOptions = new HybridCacheEntryOptions
            {
                Expiration = cacheOptions.Expiration,
                LocalCacheExpiration = cacheOptions.EnableLocalCache
                    ? cacheOptions.LocalCacheExpiration
                    : TimeSpan.Zero
            };

            return await _cache.GetOrCreateAsync(key, factory, entryOptions,
                cancellationToken: cancellationToken);
        }

        public async Task RemoveAsync(string key,
            CancellationToken cancellationToken = default)
        {
            await _cache.RemoveAsync(key, cancellationToken);
        }
    }
}
```

**实现解析：**

#### 无参重载 `GetOrSetAsync(key, factory, ct)`

```csharp
return await _cache.GetOrCreateAsync(key, factory, cancellationToken: cancellationToken);
```

- 不传 `HybridCacheEntryOptions`，HybridCache 使用 `AddHybridCache()` 注册时设定的 `DefaultEntryOptions`。
- 即全局默认：L2 = 10 分钟，L1 = 关闭。

#### 带配置重载 `GetOrSetAsync(key, factory, configure, ct)`

```csharp
var cacheOptions = new CacheOptions();   // 创建默认 CacheOptions 实例
configure(cacheOptions);                  // 调用方通过委托修改需要的字段

var entryOptions = new HybridCacheEntryOptions
{
    Expiration = cacheOptions.Expiration,
    LocalCacheExpiration = cacheOptions.EnableLocalCache
        ? cacheOptions.LocalCacheExpiration  // 启用 L1 → 使用指定的过期时间
        : TimeSpan.Zero                       // 不启用 L1 → TimeSpan.Zero 表示禁用
};
```

- **关键映射逻辑：** `EnableLocalCache` 为 `false` 时，强制 `LocalCacheExpiration = TimeSpan.Zero`，`HybridCache` 内部遇到 `Zero` 会跳过 L1 存储。

#### `RemoveAsync` 方法

```csharp
await _cache.RemoveAsync(key, cancellationToken);
```

- 同时清除 L1 和 L2 中对应 Key 的缓存。
- 应在数据变更（Update / Delete）时调用，避免脏读。

---

### 步骤 6：Infrastructure 层的服务注册（核心）

**文件路径:** `src/Infrastructure/DependencyInjection.cs`

这是整个缓存配置的核心，注册了三个关键服务，**顺序很重要**：

```csharp
#region 配置 HybridCache (L1 内存 + L2 Redis)
var redisSettings = configuration.GetSection(RedisSettings.SectionName).Get<RedisSettings>();

// ① 注册 Redis 作为 L2 分布式缓存后端
services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = $"{redisSettings!.ConnectionString},password={redisSettings.Password}";
    options.InstanceName = redisSettings.InstanceName;
});

// ② 注册 HybridCache（自动使用上面的 IDistributedCache 作为 L2）
services.AddHybridCache(options =>
{
    options.DefaultEntryOptions = new Microsoft.Extensions.Caching.Hybrid.HybridCacheEntryOptions
    {
        Expiration = TimeSpan.FromMinutes(10),
        LocalCacheExpiration = TimeSpan.Zero,
    };
});

// ③ 注册 ICacheService（业务层使用的封装接口）
services.AddSingleton<ICacheService, HybridCacheService>();
#endregion
```

#### ① `AddStackExchangeRedisCache` — 注册 Redis 连接

```csharp
services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = $"{redisSettings!.ConnectionString},password={redisSettings.Password}";
    options.InstanceName = redisSettings.InstanceName;
});
```

| 配置项 | 值 | 说明 |
|--------|-----|------|
| `Configuration` | `"localhost:6379,password=password123"` | StackExchange.Redis 标准连接字符串格式 |
| `InstanceName` | `"SilverCloud_User_"` | 所有 Redis Key 都会自动加上这个前缀 |

**内部原理：**
- 向 DI 容器注册 `IDistributedCache` → `RedisCache` 实现。
- 后续 `AddHybridCache` 会自动检测 `IDistributedCache` 并用它作为 L2 后端。

#### ② `AddHybridCache` — 注册 HybridCache（两级缓存核心）

```csharp
services.AddHybridCache(options =>
{
    options.DefaultEntryOptions = new Microsoft.Extensions.Caching.Hybrid.HybridCacheEntryOptions
    {
        Expiration = TimeSpan.FromMinutes(10),
        LocalCacheExpiration = TimeSpan.Zero,
    };
});
```

| 配置项 | 值 | 说明 |
|--------|-----|------|
| `Expiration` | `TimeSpan.FromMinutes(10)` | **L2（Redis）默认过期时间 = 10 分钟** |
| `LocalCacheExpiration` | `TimeSpan.Zero` | **L1（内存）默认关闭**（`Zero` = 不缓存到进程内） |

**`AddHybridCache` 内部做了什么：**
1. 注册 `HybridCache` 为 Singleton。
2. 内部自动创建一个 `MemoryCache` 实例作为 L1。
3. 检测 DI 容器中是否已注册 `IDistributedCache`：
   - 有 → 用它作为 L2（即我们注册的 Redis）。
   - 没有 → 只有 L1，没有 L2 分布式缓存。
4. `DefaultEntryOptions` 是全局默认配置，当调用 `GetOrCreateAsync` 时不传 `HybridCacheEntryOptions` 就会使用它。

**为什么默认关闭 L1：**
- L1 是进程内缓存，在单实例部署中效果好，但在多实例部署时各实例的 L1 互相独立，可能导致数据不一致。
- 默认关闭 L1，需要时通过 `Action<CacheOptions>` 按需开启，更安全。

#### ③ `AddSingleton<ICacheService, HybridCacheService>` — 注册业务缓存服务

```csharp
services.AddSingleton<ICacheService, HybridCacheService>();
```

- 注册为 **Singleton**，因为 `HybridCache` 本身是 Singleton 且线程安全。
- `HybridCacheService` 只是一个薄封装层，不持有任何请求级状态。

#### 三层注册的完整依赖链

```
ICacheService (业务层接口)
    └── HybridCacheService (实现)
            └── HybridCache (微软提供的两级缓存)
                    ├── L1: MemoryCache (进程内, 默认关闭)
                    └── L2: IDistributedCache → RedisCache (StackExchange.Redis)
                                └── Redis Server (localhost:6379)
```

---

### 步骤 7：添加配置到 appsettings

**文件路径:** `src/WebAPI/appsettings.Development.json`

```json
{
  "RedisSettings": {
    "ConnectionString": "localhost:6379",
    "Password": "password123",
    "InstanceName": "SilverCloud_User_"
  }
}
```

| 字段 | 值 | 对应代码 |
|------|-----|---------|
| `ConnectionString` | `localhost:6379` | `options.Configuration` 中的主机部分 |
| `Password` | `password123` | 拼接到连接字符串 `password=xxx` |
| `InstanceName` | `SilverCloud_User_` | Redis Key 前缀 |

> **安全提醒：** 生产环境中密码不应明文写在配置文件，应使用 Azure Key Vault、User Secrets 或环境变量。

---

### 步骤 8：在 GetUserProfileQuery 中使用缓存

**文件路径:** `src/Application/UserProfiles/Queries/GetUserProfile/GetUserProfileQuery.cs`

```csharp
using User.Application.UserProfiles.Dtos;

namespace User.Application.UserProfiles.Queries.GetUserProfile
{
    public record GetUserProfileQuery : IRequest<UserProfileBriefDto>
    {
        public int Id { get; set; }
    }

    public class GetUserProfileQueryHandler : IRequestHandler<GetUserProfileQuery, UserProfileBriefDto>
    {
        private readonly IApplicationDbContext _context;
        private readonly IMapper _mapper;
        private readonly IPasswordHasher _passwordHasher;
        private readonly ITokenService _tokenService;
        private readonly ICacheService _cacheService;

        private const string CacheKeyPrefix = "UserProfile_";

        public GetUserProfileQueryHandler(
            IApplicationDbContext context,
            IMapper mapper,
            IPasswordHasher passwordHasher,
            ITokenService tokenService,
            ICacheService cacheService)
        {
            _context = context;
            _mapper = mapper;
            _passwordHasher = passwordHasher;
            _tokenService = tokenService;
            _cacheService = cacheService;
        }

        public async Task<UserProfileBriefDto> Handle(
            GetUserProfileQuery request, CancellationToken cancellationToken)
        {
            var cacheKey = $"{CacheKeyPrefix}{request.Id}";

            return await _cacheService.GetOrSetAsync(cacheKey,
                async (CancellationToken ct) =>
                {
                    var entity = await _context.Set<UserProfile>()
                        .Where(u => u.Id == request.Id)
                        .FirstOrDefaultAsync(ct);

                    if (entity == null)
                    {
                        throw new NotFoundException();
                    }

                    return _mapper.Map<UserProfileBriefDto>(entity);
                },
                cancellationToken);
        }
    }
}
```

**代码逐行分析：**

1. **`private const string CacheKeyPrefix = "UserProfile_";`**
   - 定义缓存 Key 前缀常量，配合 `InstanceName` 后最终 Redis Key 为 `SilverCloud_User_UserProfile_{Id}`。

2. **构造函数注入 `ICacheService`**
   - 通过 DI 注入缓存服务，不直接依赖 `HybridCache`。

3. **`GetOrSetAsync(cacheKey, factory, cancellationToken)`**
   - 使用无参重载，采用全局默认配置（L2 10 分钟，L1 关闭）。
   - **执行流程：**
     1. HybridCache 先检查 L1（内存）→ 因为默认关闭，所以跳过。
     2. 检查 L2（Redis）→ 命中则反序列化返回。
     3. L2 也没命中 → 调用 `factory` 委托查询数据库。
     4. 数据库返回结果后，自动写入 L2（Redis）。
     5. 返回结果。

4. **`factory` 使用 `async (CancellationToken ct) =>`**
   - 显式标注参数类型 `CancellationToken ct`，帮助编译器推断返回类型为 `ValueTask<UserProfileBriefDto>`。
   - `async` lambda 返回 `Task<T>` 会被自动包装为 `ValueTask<T>`。

---

## 四、调用方式速查

### 方式一：使用全局默认配置（最简写法）

```csharp
// L2 = 10 分钟（DI 注册时的默认值），L1 = 关闭
return await _cacheService.GetOrSetAsync(cacheKey,
    async (CancellationToken ct) =>
    {
        // 数据库查询逻辑...
        return result;
    },
    cancellationToken);
```

### 方式二：自定义 L2 过期时间

```csharp
// L2 = 5 分钟，L1 = 关闭
return await _cacheService.GetOrSetAsync(cacheKey,
    async (CancellationToken ct) =>
    {
        return result;
    },
    opts => opts.Expiration = TimeSpan.FromMinutes(5),
    cancellationToken);
```

### 方式三：开启 L1 本地缓存

```csharp
// L2 = 10 分钟，L1 = 开启，L1 过期 = 3 分钟
return await _cacheService.GetOrSetAsync(cacheKey,
    async (CancellationToken ct) =>
    {
        return result;
    },
    opts =>
    {
        opts.Expiration = TimeSpan.FromMinutes(10);
        opts.EnableLocalCache = true;
        opts.LocalCacheExpiration = TimeSpan.FromMinutes(3);
    },
    cancellationToken);
```

### 方式四：删除缓存（数据变更时）

```csharp
// 在 Update / Delete Handler 中
await _cacheService.RemoveAsync($"UserProfile_{request.Id}", cancellationToken);
```

---

## 五、架构层次关系

```
┌─────────────────────────────────────────────────────────────┐
│                        WebAPI 层                             │
│  appsettings.json (RedisSettings 配置)                      │
│  Program.cs → AddInfrastructureServices(configuration)       │
└──────────────────────────┬──────────────────────────────────┘
                           │ 调用
┌──────────────────────────▼──────────────────────────────────┐
│                     Application 层                           │
│  ICacheService         (缓存接口)                           │
│  CacheOptions          (缓存选项: Expiration, L1 开关)       │
│  RedisSettings         (Redis 连接配置模型)                  │
│  GetUserProfileQuery   (业务查询, 使用缓存)                  │
└──────────────────────────┬──────────────────────────────────┘
                           │ 依赖反转
┌──────────────────────────▼──────────────────────────────────┐
│                   Infrastructure 层                           │
│  HybridCacheService : ICacheService (封装 HybridCache)       │
│  DependencyInjection.cs:                                     │
│    ① AddStackExchangeRedisCache → IDistributedCache (L2)     │
│    ② AddHybridCache → HybridCache (L1 + L2 调度)            │
│    ③ AddSingleton<ICacheService, HybridCacheService>         │
│  NuGet: Hybrid + StackExchangeRedis                          │
└──────────────────────────┬──────────────────────────────────┘
                           │ 连接
┌──────────────────────────▼──────────────────────────────────┐
│                      Redis Server                            │
│  localhost:6379, password: password123                        │
│  Key 前缀: SilverCloud_User_                                │
└─────────────────────────────────────────────────────────────┘
```

---

## 六、HybridCache 缓存查找流程

```
请求 GetOrSetAsync(key, factory)
        │
        ▼
   ┌─ L1 检查（MemoryCache）──── 命中 ──→ 直接返回（零网络开销）
   │       │
   │     未命中
   │       ▼
   ├─ L2 检查（Redis）────────── 命中 ──→ 反序列化 → 写入 L1* → 返回
   │       │
   │     未命中
   │       ▼
   ├─ 调用 factory ─────────────────────→ 查数据库 → 写入 L2 → 写入 L1* → 返回
   │
   * 仅在 L1 启用（LocalCacheExpiration > TimeSpan.Zero）时才写入 L1
```

---

## 七、后续建议

### 1. 缓存失效（更新/删除用户时清除缓存）

当用户信息被修改或删除时，应主动清除对应缓存，避免脏读：

```csharp
// 在 UpdateUserProfileCommandHandler 中
await _cacheService.RemoveAsync($"UserProfile_{request.Id}", cancellationToken);

// 在 DeleteUserProfileCommandHandler 中
await _cacheService.RemoveAsync($"UserProfile_{request.Id}", cancellationToken);
```

### 2. 使用 MediatR Pipeline Behavior 统一缓存

可以创建通用的 `CachingBehavior<TRequest, TResponse>` 作为 MediatR Pipeline，通过标记接口或特性自动缓存查询结果，避免在每个 Handler 中重复编写缓存逻辑。

### 3. 生产环境安全建议

- Redis 密码应通过 **User Secrets** 或 **环境变量** 注入，不要明文存放在配置文件中。
- 建议启用 Redis 的 TLS 加密连接。
- 配置 Redis 的连接池和超时策略。

### 4. 健康检查

可添加 Redis 健康检查：

```csharp
builder.Services.AddHealthChecks()
    .AddRedis(redisConnectionString, name: "redis");
```
