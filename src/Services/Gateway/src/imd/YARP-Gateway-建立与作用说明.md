# YARP Gateway 建立与作用说明（Identity + User）

## 1. 为什么要建立 Gateway

在当前微服务拆分下，前端如果直接访问每个服务，会遇到这些问题：

- 前端需要记住多个服务地址和端口（Identity、User 等）。
- 跨域策略需要在每个服务单独维护，前端联调复杂。
- 网关级能力（统一日志、限流、灰度、路由）无法集中治理。
- 后续服务增多时，前端路由和环境配置会快速膨胀。

因此引入 YARP（Yet Another Reverse Proxy）作为 API Gateway：

- 前端统一请求一个入口。
- 网关按路径转发到对应微服务。
- 微服务保持业务职责，不直接暴露复杂外部入口。

---

## 2. 当前项目中的 Gateway 结构

Gateway 项目路径：

- src/Services/Gateway/src/WebAPI

核心文件：

- WebAPI.csproj：网关项目依赖（Yarp.ReverseProxy）
- Program.cs：网关启动与路由映射入口
- appsettings.json：反向代理路由与集群配置
- appsettings.Development.json：开发环境日志级别
- Properties/launchSettings.json：本地调试端口
- WebAPI.http：网关联调请求示例

---

## 3. Gateway 是如何一步一步建立的

### Step 1：创建独立 WebAPI 项目（Gateway）

目标：让网关成为独立部署单元，和 Identity/User 解耦。

关键点：

- TargetFramework 使用 net10.0（和服务版本一致）。
- 引入 YARP 包：Yarp.ReverseProxy。

结果：Gateway 具备反向代理基础能力。

### Step 2：在 Program 中注册 ReverseProxy

在 Program.cs 中做了两件关键事：

1. 注册代理服务
   - AddReverseProxy()
   - LoadFromConfig("ReverseProxy")

2. 映射网关端点
   - MapHealthChecks("/health")：健康检查
   - MapGet("/")：简单网关信息
   - MapReverseProxy()：真正启用转发

结果：请求进入网关后，可以根据配置路由到下游服务。

### Step 3：在 appsettings.json 配置 Routes + Clusters

YARP 的核心是两层：

- Routes：匹配什么请求路径
- Clusters：把请求发到哪个目标地址

当前配置：

1. identity-route
   - Match.Path: /identity/{**catch-all}
   - ClusterId: identity-cluster

2. user-route
   - Match.Path: /user/{**catch-all}
   - ClusterId: user-cluster
   - Transform.PathRemovePrefix: /user

结果：

- /identity/** 会转发给 Identity。
- /user/** 会转发给 User，并移除 /user 前缀。

### Step 4：配置下游服务地址（Cluster Destinations）

当前下游地址：

- identity-cluster -> https://localhost:7060/
- user-cluster -> https://localhost:7070/

这是根据各服务 launchSettings 中 https 端口来的。

结果：网关可直接对接本地开发的 Identity 和 User。

### Step 5：接入解决方案统一管理

将 Gateway 项目加入 SilverCloud.slnx，保证：

- 可以统一 build。
- 团队成员打开解决方案即可看到网关。
- CI/CD 时便于纳入流水线。

---

## 4. 为什么 User 路由要 PathRemovePrefix

当前 User 控制器路由是：

- [Route("[controller]")]

例如：

- /WeatherForecast
- /WeatherForecast/me

如果网关不做 PathRemovePrefix，转发 /user/WeatherForecast 到 User 时，User 实际收到 /user/WeatherForecast，会 404。

所以配置了：

- PathRemovePrefix: /user

这样网关外部路径仍是 /user/**，但内部转发给 User 时会去掉 /user，匹配到实际控制器路由。

---

## 5. Gateway 建立后的作用（项目收益）

### 5.1 统一入口

前端只需要记住一个网关地址，不必直接耦合多个服务端口。

### 5.2 路由解耦

服务内部路由可以按业务演进，网关通过配置适配，不必频繁改前端。

### 5.3 横切能力集中化

后续可在网关层集中增加：

- 限流
- IP 白名单
- 全局审计日志
- 灰度路由
- 熔断与重试策略

### 5.4 安全边界更清晰

- 网关负责入口治理和转发。
- 业务服务（Identity/User）继续负责 JWT 校验和授权。
- ICurrentAccountService 在服务内部解析 Claims，保持业务边界清晰。

---

## 6. 与 JWT / CurrentAccountService 的关系

关键结论：

- Gateway 不负责“当前用户对象”的业务解析。
- Gateway 透传 Authorization 头。
- Identity/User 各自在服务内校验 JWT。
- 校验成功后，服务内通过 ICurrentAccountService 读取 Claims（Id/Name/Email/Phone）。

这样既避免网关承载业务语义，也保留了服务自治能力。

---

## 7. 当前端口与调试方式

当前 launchSettings 端口：

- Gateway: https://localhost:7080, http://localhost:7081
- Identity: https://localhost:7060, http://localhost:7061
- User: https://localhost:7070, http://localhost:7071

推荐调试顺序：

1. 启动 Identity
2. 启动 User
3. 启动 Gateway
4. 先访问 /health 验证网关存活
5. 用网关地址访问 /identity/** 和 /user/**

---

## 8. 一次完整请求链路示例

场景：获取当前用户信息

1. 前端请求 Gateway：
   - GET /user/WeatherForecast/me
   - Header: Authorization: Bearer <token>

2. Gateway 匹配 user-route
   - 命中 /user/{**catch-all}
   - 去掉 /user 前缀
   - 转发到 https://localhost:7070/WeatherForecast/me

3. User 服务执行认证授权
   - JWT 校验通过
   - Controller 中通过 ICurrentAccountService 读取 Claims

4. User 返回当前用户信息

结果：前端无感知下游服务地址，同时拿到真实业务结果。

---

## 9. 常见问题与排查

### 问题 1：网关返回 502 / 503

排查：

- 下游服务是否启动。
- 端口是否和 launchSettings 一致。
- 网关 appsettings 中 Address 是否正确。

### 问题 2：User 路由 404

排查：

- 是否配置了 PathRemovePrefix: /user。
- User 控制器 Route 是否仍为 [controller]。

### 问题 3：明明带了 Token 但仍 401

排查：

- User 的 JwtSettings（Secret/Issuer/Audience）是否和 Identity 签发一致。
- Gateway 是否转发了 Authorization（YARP 默认会转发）。
- Token 是否过期。

### 问题 4：HTTP/HTTPS 端口混用导致失败

排查：

- Gateway Destination 地址与服务实际协议（http 或 https）必须一致。
- 开发环境尽量统一使用 https 端口，减少重定向干扰。

---

## 10. 后续可演进方向

- 在网关增加全局限流与统一错误响应。
- 增加网关访问日志（traceId、routeId、clusterId）。
- 引入服务发现（替代固定 localhost 端口）。
- 在网关增加 Swagger 聚合入口（多服务文档统一浏览）。

---

## 11. 一句话总结

Gateway 的本质是“统一入口 + 路由转发 + 横切治理”，它不替代业务服务的认证与授权，而是让微服务体系更易维护、更可扩展。