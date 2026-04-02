namespace WebAPI.ConfigureServices
{
    public static class HttpExtensions
    {
        public static IServiceCollection AddHttpServices(this IServiceCollection services)
        {
            // 用于在非 Controller / Middleware 中访问当前 HTTP 请求上下文（如获取用户信息、Header、Token 等）
            services.AddHttpContextAccessor();

            // 注册 HttpClient 工厂（推荐方式），用于创建和管理 HttpClient，避免 Socket 耗尽问题，并支持命名/类型化客户端
            services.AddHttpClient();

            return services;
        }
    }
}
