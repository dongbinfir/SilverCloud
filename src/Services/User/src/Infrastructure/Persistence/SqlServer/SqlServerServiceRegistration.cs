using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using User.Application.Common.Interfaces;
using User.Infrastructure.Interceptors;

namespace User.Infrastructure.Persistence.SqlServer
{
    /// <summary>
    /// SqlServer 的服务注册
    /// </summary>
    public static class SqlServerServiceRegistration
    {
        public static IServiceCollection AddSqlServerServices(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            var connectionString = configuration.GetConnectionString("DefaultConnection");

            // 注册 SaveChanges 拦截器
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

            // 注册 Initialiser, 默认数据初始化
            services.AddScoped<ApplicationDbContextInitialiser>();

            return services;
        }
    }
}
