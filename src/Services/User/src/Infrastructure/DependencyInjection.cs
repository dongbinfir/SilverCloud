using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Scrutor;
using User.Application.Common.Interfaces;
using User.Infrastructure.Dependencies;
using User.Infrastructure.Interceptors;
using User.Infrastructure.Persistence;
using User.Infrastructure.Services;

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

            services.Scan(scan => scan
            .FromAssemblyOf<ApplicationDbContext>()
                .AddClasses(classes => classes
                    .AssignableTo(typeof(IScopedDependency<>))
                    .Where(type => type.Namespace != null && type.Namespace.StartsWith("User.Infrastructure.Services")))
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
