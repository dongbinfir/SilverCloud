using Identity.Infrastructure.Persistence.MongoDb;
using Identity.Infrastructure.Persistence.SqlServer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shared.Infrastructure.Caching;

namespace Identity.Infrastructure
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
            services.AddSqlServerServices(configuration);

            services.AddMongoDbServices(configuration);

            services.AddSharedCacheServices(configuration);

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
