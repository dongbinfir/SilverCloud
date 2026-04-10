# MongoDB 项目实现逐步解析

日期：2026-03-23  
适用范围：SilverCloud 的 User 服务（RefreshToken 使用 MongoDB，用户主体数据仍在 SQL Server）

## 1. 先看整体架构

当前项目采用“双存储”模式：

1. SQL Server（EF Core）
- 负责用户主数据，如 UserProfile。

2. MongoDB（MongoDB.Driver）
- 负责 RefreshToken 的存储、查询、撤销和过期清理。

这样做的目的：
- Token 是高频、短生命周期数据，适合文档库。
- 通过 TTL 索引自动过期回收，降低业务清理成本。

---

## 2. 怎么构建与运行（从零到可用）

## 2.1 依赖包
在 Infrastructure 工程中已引入：

- MongoDB.Driver
- Microsoft.Extensions.Configuration.Binder

同时保留 EF Core 相关包，说明当前是混合存储方案。

## 2.2 环境准备

1. 启动 MongoDB 实例。
2. 确保 Development 配置中存在：
- MongoDbSettings.ConnectionString
- MongoDbSettings.DatabaseName

当前配置位置：
- src/Services/User/src/WebAPI/appsettings.Development.json

## 2.3 构建命令

1. 还原依赖
- dotnet restore

2. 编译解决方案
- dotnet build SilverCloud.slnx -v minimal

3. 启动 WebAPI
- dotnet run --project src/Services/User/src/WebAPI/WebAPI.csproj

## 2.4 启动时会发生什么

1. Program 调用 AddInfrastructureServices 注册 Mongo 相关服务。
2. 应用启动后创建作用域并调用 IMongoDbContext.InitializeAsync。
3. InitializeAsync 里执行：
- Bson 映射注册
- 索引创建（TTL、UserProfileId）

---

## 3. 启动链路逐步分析

## 3.1 Program.cs
职责：应用启动入口，触发 Mongo 初始化。

关键步骤：

1. builder.Services.AddInfrastructureServices(builder.Configuration)
- 把 SQL + Mongo 基础设施一起注册到 DI。

2. 应用构建后执行初始化
- 创建 scope
- 解析 IMongoDbContext
- 调用 InitializeAsync

价值：
- 保证服务启动即完成映射和索引准备，避免第一次请求才发现缺索引。

---

## 3.2 DependencyInjection.cs
类：InfrastructureServiceRegistration  
方法：AddInfrastructureServices

这个方法是 Mongo 构建的核心编排器，步骤如下：

1. 读取 SQL 连接并注册 ApplicationDbContext
- 这是原有 EF 主链路。

2. 绑定 MongoDbSettings
- 使用 Options 模式，把配置节映射到对象。

3. 注册 IMongoClient（单例）
- 从配置读取 ConnectionString。
- 若缺失则抛异常，防止错误配置带病启动。

4. 注册 IMongoDbContext（作用域）
- 每个请求作用域拿到统一上下文封装。

5. 注册 IRefreshTokenRepository（作用域）
- 应用层通过接口操作 RefreshToken，不直接依赖 Driver。

6. 自动扫描 IMongoEntityConfiguration
- 反射找出所有映射/索引配置类并注册。
- InitializeAsync 会遍历这些配置执行初始化。

---

## 4. Mongo 基础抽象层（每个类与每个方法）

## 4.1 MongoDbSettings
文件：Application/Common/Models/MongoDbSettings.cs

类用途：承载配置。

属性说明：

1. ConnectionString
- MongoDB 连接字符串。

2. DatabaseName
- 使用的数据库名称。

---

## 4.2 IMongoDbContext
文件：Infrastructure/Persistence/Mongo/IMongoDbContext.cs

接口用途：隔离 MongoDB.Driver，统一提供集合访问与初始化能力。

方法说明：

1. GetCollection<T>(string? name = null)
- 获取集合句柄。
- 若不传 name，按约定用类型名加 s。

2. InitializeAsync()
- 在应用启动时统一执行映射与索引初始化。

---

## 4.3 IMongoEntityConfiguration
文件：Infrastructure/Persistence/Mongo/IMongoEntityConfiguration.cs

接口用途：把“实体映射”和“索引创建”从上下文中拆开，按实体扩展。

方法说明：

1. Configure()
- 注册 BsonClassMap、字段映射规则。

2. CreateIndexesAsync(IMongoDatabase database)
- 负责实体对应集合的索引创建。

---

## 4.4 MongoDbContext
文件：Infrastructure/Persistence/Mongo/MongoDbContext.cs

类用途：实现 IMongoDbContext，聚合所有 Mongo 初始化流程。

字段说明：

1. _database
- 当前 Mongo 数据库句柄。

2. _configurations
- 全部 IMongoEntityConfiguration 实现集合。

方法说明：

1. 构造函数 MongoDbContext(IMongoClient, IOptions<MongoDbSettings>, IEnumerable<IMongoEntityConfiguration>)
- 使用 client + settings 获取数据库实例。
- 注入所有配置实现，供初始化时遍历。

2. GetCollection<T>(string? name = null)
- 返回指定集合。

3. InitializeAsync()
- 逐个配置执行：先 Configure，再 CreateIndexesAsync。

---

## 5. RefreshToken 的 Mongo 实体配置层

## 5.1 RefreshTokenConfiguration
文件：Infrastructure/Persistence/MongoDb/Configurations/RefreshTokenConfiguration.cs

类用途：定义 UserRefreshToken 的 BSON 映射和索引策略。

方法说明：

1. Configure()
- 防重复注册 ClassMap。
- AutoMap 自动映射属性。
- 指定主键映射为 Id（当前实现是这样）。
- 忽略额外字段，提高向后兼容性。

2. CreateIndexesAsync(IMongoDatabase database)
- 集合名：UserRefreshTokens。
- 创建 ExpiresAt TTL 索引，ExpireAfter = 0，表示到期即进入后台清理队列。
- 创建 UserProfileId 普通索引，优化按用户查 Token。

---

## 6. 仓储层（每个方法）

## 6.1 IRefreshTokenRepository
文件：Application/Common/Interfaces/IRefreshTokenRepository.cs

接口用途：应用层依赖这个接口，不依赖 MongoDB.Driver。

方法说明：

1. GetByTokenAsync(string token)
- 按 token 查询单条 RefreshToken。

2. GetActiveTokensByUserIdAsync(int userId)
- 查询用户所有活跃 token（未撤销且未过期）。

3. AddAsync(UserRefreshToken refreshToken)
- 新增 token 文档。

4. UpdateAsync(UserRefreshToken refreshToken)
- 覆盖更新指定 token 文档。

5. DeleteAsync(string token)
- 按 token 删除。

6. DeleteAllByUserIdAsync(int userId)
- 删除某用户全部 token。

## 6.2 MongoRefreshTokenRepository
文件：Infrastructure/Persistence/MongoDb/Repositories/MongoRefreshTokenRepository.cs

类用途：IRefreshTokenRepository 的 MongoDB 实现。

字段说明：

1. _collection
- UserRefreshTokens 集合句柄。

方法说明：

1. 构造函数 MongoRefreshTokenRepository(IMongoDbContext dbContext)
- 通过上下文拿到 UserRefreshTokens 集合。

2. GetByTokenAsync
- _collection.Find + FirstOrDefaultAsync。

3. GetActiveTokensByUserIdAsync
- 过滤条件：UserProfileId 匹配，RevokedAt 为空，ExpiresAt 晚于当前时间。

4. AddAsync
- InsertOneAsync 插入。

5. UpdateAsync
- ReplaceOneAsync 按 Token 覆盖。

6. DeleteAsync
- DeleteOneAsync 按 Token 删除。

7. DeleteAllByUserIdAsync
- DeleteManyAsync 按 UserProfileId 批量删除。

---

## 7. 业务层调用链（每个方法）

## 7.1 LoginCommandHandler.Handle
文件：Application/Auths/Commands/Login/LoginCommand.cs

Mongo 相关步骤：

1. 验证账号密码（SQL 查用户）。
2. 生成 AccessToken + RefreshToken。
3. 构造 UserRefreshToken 实体。
4. 调用 _tokenRepository.AddAsync 写入 Mongo。

作用：登录时把刷新令牌落库到 Mongo。

## 7.2 LogoutCommandHandler.Handle
文件：Application/Auths/Commands/Logout/LogoutCommand.cs

Mongo 相关步骤：

1. 用 refresh token 查 Mongo。
2. 校验用户归属和 IsActive。
3. 写入撤销信息（RevokedAt、RevokedByIp、RevokedReason）。
4. 调用 UpdateAsync 覆盖更新。

作用：登出时撤销当前 token。

## 7.3 RefreshTokenCommandHandler.Handle
文件：Application/Auths/Commands/RefreshToken/RefreshTokenCommand.cs

Mongo 相关步骤：

1. 验证 AccessToken 并取 userId。
2. 用 refresh token 查 Mongo。
3. 判断是否撤销、是否过期。
4. 生成新的 token 对。
5. 创建新 UserRefreshToken。
6. 撤销旧 token 并建立前后关联。
7. Update 旧 token + Add 新 token。

作用：实现 refresh token 轮换。

## 7.4 RefreshTokenCommandHandler.RevokeUserAllRefreshTokensAsync

Mongo 相关步骤：

1. 查询用户全部活跃 token。
2. 遍历逐条写入撤销状态。

作用：检测到风险时执行全量撤销。

---

## 8. 领域实体说明

## 8.1 UserRefreshToken
文件：Domain/Entities/UserRefreshToken.cs

实体用途：保存 refresh token 生命周期状态。

关键字段：

1. Id
- 文档主键字段。

2. Token
- 实际刷新令牌字符串。

3. ExpiresAt
- 过期时间，配合 TTL 索引自动回收。

4. RevokedAt、RevokedByIp、RevokedReason
- 撤销审计字段。

5. PreviousRefreshTokenId、NextRefreshTokenId
- 轮换链路追踪字段。

计算属性：

1. IsExpired
- 当前时间是否超过 ExpiresAt。

2. IsActive
- 未撤销且未过期。

---

## 9. 你当前实现的关键注意点

1. 命名空间有混用
- 上下文接口在 Infrastructure/Persistence/Mongo
- 配置和仓储在 Infrastructure/Persistence/MongoDb
- 运行不一定出错，但建议统一目录与命名空间，降低维护成本。

2. 主键策略要统一
- 目前配置映射主键是 Id。
- 仓储更新和查询主要按 Token。
- 建议在后续版本明确统一策略，避免链路字段出现空值或语义不一致。

3. InitializeAsync 在启动时执行是正确做法
- 这是你当前架构的关键点，保证映射和索引在服务启动即就绪。

---

## 10. 一句话总结

你的 MongoDB 构建方式是：
- 配置绑定 + DI 注入 + 上下文封装 + 配置扫描初始化 + 仓储隔离 + 业务命令调用。

它已经具备完整工程闭环，下一步重点是统一命名与主键策略，让实现更稳、更易维护。
