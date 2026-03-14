using Microsoft.EntityFrameworkCore;
using User.Application.Common.Interfaces;
using User.Infrastructure.Interceptors;
using User.Infrastructure.Persistence;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Infrastructure 层的服务注册
    /// </summary>
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructureServices(
            this IServiceCollection services,
            string connectionString)
        {
            // 1. 注册拦截器 (建议用 Scoped，以便未来扩展记录“当前用户”的功能)
            services.AddScoped<AuditableEntitySaveChangesInterceptor>();

            // 注册 DbContext
            services.AddDbContext<ApplicationDbContext>((sp, options) =>
            {
                var interceptor = sp.GetRequiredService<AuditableEntitySaveChangesInterceptor>();
                options
                    .UseSqlServer(connectionString)
                    .AddInterceptors(interceptor);
            });

            // 注册接口实现
            services.AddScoped<IApplicationDbContext>(sp =>
                sp.GetRequiredService<ApplicationDbContext>());

            return services;
        }
    }
}
