using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using User.Application.Common.Models;
using User.Infrastructure.Persistence.MongoDb.Interfaces;
using User.Infrastructure.Persistence.SqlServer;

namespace User.Infrastructure.Persistence.MongoDb
{
    /// <summary>
    /// Infrastructure 层的服务注册
    /// </summary>
    public static class MongoDbServiceRegistration
    {
        public static IServiceCollection AddMongoDbServices(
            this IServiceCollection services,
            IConfiguration configuration)
        {

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

            return services;
        }
    }
}
