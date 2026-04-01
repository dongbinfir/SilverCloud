using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using User.Application.Common.Interfaces;
using User.Application.Common.Models;
using User.Infrastructure.Caching;
using User.Infrastructure.Common;
using User.Infrastructure.Interceptors;
using User.Infrastructure.Persistence.MongoDb.Interfaces;
using User.Infrastructure.Persistence.SqlServer;

namespace User.Infrastructure
{
    /// <summary>
    /// Infrastructure 层的服务注册
    /// </summary>
    public static class InfrastructureServiceRegistration
    {
        public static IServiceCollection AddInfrastructureServices(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            var connectionString = configuration.GetConnectionString("DefaultConnection");

            services.AddScoped<AuditableEntitySaveChangesInterceptor>();

            services.AddDbContext<ApplicationDbContext>((sp, options) =>
            {
                var interceptor = sp.GetRequiredService<AuditableEntitySaveChangesInterceptor>();
                options
                    .UseSqlServer(connectionString)
                    .AddInterceptors(interceptor);
            });

            services.AddScoped<IApplicationDbContext>(sp =>
                sp.GetRequiredService<ApplicationDbContext>());
            // 添加这一行：注册 Initialiser
            services.AddScoped<ApplicationDbContextInitialiser>();

            #region 配置 HybridCache (L1 内存 + L2 Redis)
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
                options.DefaultEntryOptions = new Microsoft.Extensions.Caching.Hybrid.HybridCacheEntryOptions
                {
                    Expiration = TimeSpan.FromMinutes(10),
                    Flags = HybridCacheEntryFlags.DisableLocalCache
                };
            });

            services.AddSingleton<ICacheService, HybridCacheService>();
            #endregion

            #region 配置 MongoDB 相关服务
            // 1. 注册配置（Options 模式）
            services.Configure<MongoDbSettings>(options => configuration.GetSection("MongoDbSettings").Bind(options));

            // 2. 注册 IMongoClient
            services.AddSingleton<IMongoClient>(sp =>
            {
                // 从容器中获取已绑定的配置对象
                var mongoSettings = sp.GetRequiredService<IOptions<MongoDbSettings>>().Value;

                if (string.IsNullOrEmpty(mongoSettings.ConnectionString))
                {
                    throw new InvalidOperationException("MongoDB ConnectionString is missing in configuration.");
                }

                return new MongoClient(mongoSettings.ConnectionString);
            });

            // 3. 注册上下文和仓库
            //services.AddScoped<IMongoDbContext, MongoDbContext>();
            //services.AddScoped<IRefreshTokenRepository, MongoRefreshTokenRepository>();

            // 4. 批量注册配置（保持原样或使用 Scrutor）
            var configTypes = typeof(ApplicationDbContext).Assembly.GetTypes()
                .Where(t => typeof(IMongoEntityConfiguration).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);

            foreach (var type in configTypes)
            {
                services.AddSingleton(typeof(IMongoEntityConfiguration), type);
            }
            #endregion

            // Existing Scrutor scanning, 实现自动 AddScoped 注册 IScopedDependency<> 的实现类
            services.Scan(scan => scan
            .FromAssemblyOf<ApplicationDbContext>()
                .AddClasses(classes => classes
                    .AssignableTo(typeof(IScopedDependency<>))
                    //.Where(type => type.Namespace != null && type.Namespace.StartsWith("User.Infrastructure.Services"))
                    )
                .As(type =>
                {
                    var i = type.GetInterfaces().First(x =>
                        x.IsGenericType &&
                        x.GetGenericTypeDefinition() == typeof(IScopedDependency<>));

                    return [i.GetGenericArguments()[0]];
                })
                .WithScopedLifetime());

            return services;
        }
    }
}
