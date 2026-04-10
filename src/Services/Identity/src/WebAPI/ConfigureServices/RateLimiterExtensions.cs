using Identity.WebAPI.Helpers;
using System.Threading.RateLimiting;

namespace Identity.WebAPI.ConfigureServices
{
    public static class RateLimiterExtensions
    {
        public static IServiceCollection AddRateLimiting(this IServiceCollection services)
        {
            services.AddRateLimiter(options =>
            {
                // 全局拒绝处理
                options.OnRejected = async (context, cancellationToken) =>
                {
                    context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                    await context.HttpContext.Response.WriteAsJsonAsync(new
                    {
                        message = "请求过于频繁，请稍后再试",
                        retryAfter = TimeSpan.FromMinutes(1).TotalSeconds
                    }, cancellationToken);
                };

                // 全局默认限流器：所有未标记的接口都使用此策略（每分钟 100 次）
                options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
                    RateLimitPartition.GetFixedWindowLimiter(
                        partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
                        factory: _ => RateLimiterHelper.CreateFixedWindowLimiter(100, 1)));

                // 身份验证专用策略：登录、刷新Token 等敏感操作（每分钟 10 次）
                options.AddPolicy("IdentityAuth", httpContext =>
                {
                    string username = httpContext.Request.Headers["identity"].ToString() ?? "anon";
                    string ip = httpContext.Connection.RemoteIpAddress?.ToString() ?? "anon-ip";

                    return RateLimitPartition.GetFixedWindowLimiter(
                         partitionKey: $"auth_{ip}_{username}",
                         factory: _ => RateLimiterHelper.CreateFixedWindowLimiter(10, 1));
                });
            });

            return services;
        }
    }
}
