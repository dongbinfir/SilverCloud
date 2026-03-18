using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using MongoDB.Driver;
using Scrutor;
using User.Application.Common.Interfaces;
using User.Application.Common.Models;
using User.Infrastructure.Dependencies;
using User.Infrastructure.Interceptors;
using User.Infrastructure.Persistence;
using User.Infrastructure.Persistence.Mongo;
using User.Infrastructure.Persistence.Mongo.Repositories;

namespace Microsoft.Extensions.DependencyInjection
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

            // MongoDB Configuration
            services.Configure<MongoDbSettings>(options => configuration.GetSection("MongoDbSettings").Bind(options));

            services.AddSingleton<IMongoClient>(sp =>
            {
                var mongoSettings = configuration.GetSection("MongoDbSettings").Get<MongoDbSettings>();
                return new MongoClient(mongoSettings!.ConnectionString);
            });

            services.AddScoped<IMongoDbContext, MongoDbContext>();
            services.AddScoped<IRefreshTokenRepository, MongoRefreshTokenRepository>();

            // Register all IMongoEntityConfiguration implementations
            var configTypes = typeof(ApplicationDbContext).Assembly.GetTypes()
                .Where(t => typeof(IMongoEntityConfiguration).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);

            foreach (var type in configTypes)
            {
                services.AddSingleton(typeof(IMongoEntityConfiguration), type);
            }

            // Existing Scrutor scanning
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
