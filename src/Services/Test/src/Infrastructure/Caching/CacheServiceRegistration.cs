using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using User.Application.Common.Interfaces;
using User.Application.Common.Models;

namespace User.Infrastructure.Caching
{
    /// <summary>
    /// Infrastructure 层的服务注册
    /// </summary>
    public static class CacheServiceRegistration
    {
        public static IServiceCollection AddCacheServices(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            //配置 HybridCache (L1 内存 + L2 Redis)
            var redisSettings = configuration.GetSection(RedisSettings.SectionName).Get<RedisSettings>();

            // 注册 Redis 作为 L2 分布式缓存后端
            services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = $"{redisSettings!.ConnectionString},password={redisSettings.Password}";
                options.InstanceName = redisSettings.InstanceName;
            });

            // 注册 HybridCache（自动使用上面的 IDistributedCache 作为 L2）
            services.AddHybridCache(options =>
            {
                options.DefaultEntryOptions = new HybridCacheEntryOptions
                {
                    // L1 默认关闭
                    Flags = HybridCacheEntryFlags.DisableLocalCache,
                    // L2 默认十分钟过期
                    Expiration = TimeSpan.FromMinutes(10)
                    
                };
            });

            services.AddSingleton<ICacheService, HybridCacheService>();

            return services;
        }
    }
}
