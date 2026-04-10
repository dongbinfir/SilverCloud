# MongoDB 迁移修改建议与验收清单

日期：2026-03-18  
范围：User 服务中 RefreshToken 从 EF Core 迁移到 MongoDB 的实现

## 1. 目标
本清单用于把当前“可编译”的 MongoDB 迁移实现，提升为“运行稳定、数据一致、可审计”的版本。

---

## 2. 必改项（高优先级）

### 2.1 统一主键策略：使用 Token 作为 Mongo _id

现状问题：
- 配置中把 _id 映射到 Id。
- 仓储查询与更新主要按 Token。
- 新建 token 时未明确设置 Id。

风险：
- 可能出现 _id 为空或重复导致插入异常。
- Id 与 Token 双主键语义冲突，后续排障困难。

修改建议：
1. 在 Mongo 映射配置中，将 _id 映射改为 Token。
2. 避免基类 Id 被重复映射为业务字段。
3. 仓储保持按 Token 查询/更新。

验收标准：
- 新增 RefreshToken 时 _id 与 Token 一致。
- 连续登录或刷新 token 不出现 DuplicateKey 异常。

---

### 2.2 修复 RefreshToken 轮换链路字段

现状问题：
- PreviousRefreshTokenId 与 NextRefreshTokenId 依赖 Id。
- 新 token 在写入前 Id 可能为空，链路可能断裂。

风险：
- 无法可靠追踪 token 替换关系，影响审计和风控。

修改建议：
1. PreviousRefreshTokenId 记录旧 token 的 Token。
2. NextRefreshTokenId 记录新 token 的 Token。
3. 不再依赖数据库生成 Id 完成链路串联。

验收标准：
- 每次 refresh 后，旧 token 的 NextRefreshTokenId 正确指向新 token。
- 新 token 的 PreviousRefreshTokenId 正确指向旧 token。

---

### 2.3 优化轮换写入顺序，避免中间失败导致用户被动下线

现状问题：
- 先撤销旧 token，再插入新 token。

风险：
- 若插入新 token 失败，用户旧 token 已失效且无新 token 可用。

修改建议：
1. 先插入新 token，再撤销旧 token。
2. 或使用 Mongo 事务（副本集环境）。
3. 至少加失败补偿（例如插入成功后更新失败时回滚）。

验收标准：
- 人为制造第二步失败时，不出现用户永久无 token 的状态。

---

## 3. 建议项（中优先级）

### 3.1 增加 Mongo 配置校验

现状问题：
- ConnectionString 与 DatabaseName 缺失时，错误信息不明确。

修改建议：
1. 启动时校验 MongoDbSettings 必填。
2. 缺少配置直接 Fail Fast，并输出明确错误。

验收标准：
- 删除配置后，启动日志能准确指出缺失字段。

---

### 3.2 仓储方法透传 CancellationToken

现状问题：
- 应用层有取消令牌，但仓储接口未接收。

修改建议：
1. 在 IRefreshTokenRepository 所有异步方法增加 CancellationToken 参数。
2. Mongo Driver 调用时透传该参数。

验收标准：
- 请求取消后，Mongo 查询/写入可及时中断。

---

## 4. 推荐改造顺序

1. 先改主键映射（2.1）。
2. 再改 refresh 轮换链路（2.2）。
3. 再改写入顺序或事务补偿（2.3）。
4. 最后补配置校验和取消令牌（3.1、3.2）。

---

## 5. 回归测试清单

### 5.1 功能回归
- 登录成功后写入一条 RefreshToken。
- 使用 RefreshToken 刷新成功，旧 token 被撤销，新 token 可用。
- 登出后 token 状态变为撤销。

### 5.2 异常回归
- Mongo 配置缺失时应用应启动失败并提示清晰。
- 模拟写入失败时，系统不出现“旧 token 已失效且新 token 不存在”的不可恢复状态。

### 5.3 数据回归
- Mongo 集合中 _id 与 Token 一致。
- ExpiresAt 的 TTL 索引存在并生效。
- UserProfileId 索引存在。

---

## 6. 完成定义（DoD）

满足以下条件视为迁移完成：
- 编译通过。
- 登录、刷新、登出主流程全部通过。
- 关键异常流程可控。
- Mongo 索引与字段模型一致。
- 代码审查无高优先级缺陷。
