# MongoDB Code-First 详细集成与迁移方案 (UserRefreshToken 迁移)

本方案详细描述了如何将 `UserRefreshToken` 实体从 SQL Server (EF Core) 迁移到 MongoDB，并实现完整的 **Code-First** 架构支持。

## 1. 核心目标
- **解耦持久化**: 将高频变动且具有自然过期特性的 Token 数据移出关系型数据库。
- **零侵入**: 保持 `Domain` 层实体纯净，不添加任何 MongoDB 特性（Attributes）。
- **自动化管理**: 自动处理 BsonClassMap 注册和数据库索引（含 TTL 索引）创建。

## 2. 基础设施层架构

### 2.1 映射配置接口 (`Infrastructure/Persistence/Mongo/IMongoEntityConfiguration.cs`)
定义统一的配置契约，用于分离映射逻辑和索引定义：
```csharp
public interface IMongoEntityConfiguration {
    void Configure(); 
    Task CreateIndexesAsync(IMongoDatabase database);
}
```

### 2.2 UserRefreshToken 专项配置 (`Infrastructure/Persistence/Mongo/Configurations/RefreshTokenConfiguration.cs`)
利用 MongoDB 的 **TTL (Time To Live) 索引** 自动清理过期数据：
```csharp
public class RefreshTokenConfiguration : IMongoEntityConfiguration {
    public void Configure() {
        if (!BsonClassMap.IsClassMapRegistered(typeof(UserRefreshToken))) {
            BsonClassMap.RegisterClassMap<UserRefreshToken>(cm => {
                cm.AutoMap();
                cm.MapIdProperty(c => c.Token); // 使用 Token 字符串作为 MongoDB 的 _id
                cm.SetIgnoreExtraElements(true);
            });
        }
    }

    public async Task CreateIndexesAsync(IMongoDatabase database) {
        var collection = database.GetCollection<UserRefreshToken>("RefreshTokens");

        // 1. 创建 TTL 索引：当当前时间超过 ExpiryDate 时，MongoDB 自动删除该文档
        var ttlIndexKeys = Builders<UserRefreshToken>.IndexKeys.Ascending(x => x.ExpiryDate);
        var ttlOptions = new CreateIndexOptions { ExpireAfter = TimeSpan.Zero }; 
        await collection.Indexes.CreateOneAsync(new CreateIndexModel<UserRefreshToken>(ttlIndexKeys, ttlOptions));

        // 2. 创建 UserId 索引：优化用户 Token 查询性能
        var userIndexKeys = Builders<UserRefreshToken>.IndexKeys.Ascending(x => x.UserId);
        await collection.Indexes.CreateOneAsync(new CreateIndexModel<UserRefreshToken>(userIndexKeys));
    }
}
```

### 2.3 增强版 MongoDbContext 实现
负责协调所有的配置初始化：
```csharp
public class MongoDbContext : IMongoDbContext {
    private readonly IMongoDatabase _database;
    private readonly IEnumerable<IMongoEntityConfiguration> _configurations;

    public MongoDbContext(IMongoClient client, IOptions<MongoDbSettings> settings, IEnumerable<IMongoEntityConfiguration> configurations) {
        _database = client.GetDatabase(settings.Value.DatabaseName);
        _configurations = configurations;
    }

    public IMongoCollection<T> GetCollection<T>(string name = null) 
        => _database.GetCollection<T>(name ?? typeof(T).Name + "s");

    public async Task InitializeAsync() {
        foreach (var config in _configurations) {
            config.Configure(); // 注册 Bson 映射
            await config.CreateIndexesAsync(_database); // 创建/同步索引
        }
    }
}
```

## 3. 迁移实施步骤

### 第一步：清理 EF Core (SQL Server)
1. 从 `ApplicationDbContext.cs` 中移除 `public DbSet<UserRefreshToken> UserRefreshTokens { get; set; }`。
2. 删除 `Infrastructure/Persistence/Configurations/RefreshTokenConfiguration.cs` (EF 版本)。
3. 生成并运行 Migration 脚本，从 SQL Server 物理库中移除该表。

### 第二步：配置 MongoDB 依赖注入
在 `Infrastructure/DependencyInjection.cs` 中添加自动化注册逻辑：
```csharp
public static IServiceCollection AddMongoInfrastructure(this IServiceCollection services, IConfiguration configuration) {
    // 1. 绑定配置模型
    // 2. 注册 IMongoClient (Singleton)
    // 3. 注册 IMongoDbContext (Scoped)
    // 4. 反射注册所有 IMongoEntityConfiguration 的实现类
    var types = Assembly.GetExecutingAssembly().GetTypes()
        .Where(t => typeof(IMongoEntityConfiguration).IsAssignableFrom(t) && !t.IsInterface);
    foreach (var type in types) {
        services.AddSingleton(typeof(IMongoEntityConfiguration), type);
    }
    return services;
}
```

### 第三步：更新业务 Handler
在 `RefreshTokenCommandHandler.cs` 中，将依赖从 `IApplicationDbContext` 切换为 `IMongoRepository<UserRefreshToken>`：
```csharp
// 迁移前 (EF Core):
var token = await _context.UserRefreshTokens.FirstOrDefaultAsync(x => x.Token == command.Token);

// 迁移后 (MongoDB):
var token = await _tokenRepository.GetByFilterAsync(x => x.Token == command.Token);
```

## 4. 关键注意事项
- **一致性**: MongoDB 默认不提供跨 Collection 事务。对于 `UserRefreshToken`，由于它是独立实体，单文档原子性已足够。
- **数据初始化**: 在 `Program.cs` 启动时必须调用 `InitializeAsync()`，否则 TTL 索引不会生效，导致数据堆积。
- **ID 策略**: 本方案将 `Token` 字符串直接映射为 MongoDB 的 `_id`。如果需要使用 MongoDB 自动生成的 `ObjectId`，请在 `Domain` 实体中使用 `string Id` 并在配置中指定 `StringObjectIdGenerator`。
