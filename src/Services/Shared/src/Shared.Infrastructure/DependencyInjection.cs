using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shared.Domain.Common;
using Shared.Infrastructure.Caching;
using Shared.Infrastructure.Persistence.MongoDb.Repositories;

namespace Shared.Infrastructure
{
    /// <summary>
    /// Infrastructure 层的服务注册
    /// </summary>
    public static class InfrastructureServiceRegistration
    {
        public static IServiceCollection AddSharedInfrastructureServices(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            services.AddCacheServices(configuration);

            services.AddScoped(typeof(IMongoDbRepository<>), typeof(MongoDbRepository<>));

            return services;
        }
    }
}
