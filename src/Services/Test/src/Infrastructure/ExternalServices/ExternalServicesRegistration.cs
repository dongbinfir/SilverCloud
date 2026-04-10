using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using User.Infrastructure.ExternalServices.TranslationServices;

namespace User.Infrastructure.ExternalServices
{
    /// <summary>
    /// Infrastructure 层的服务注册
    /// </summary>
    public static class ExternalServicesRegistration
    {
        public static IServiceCollection AddExternalServices(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            services.AddTranslationExtensionService(configuration);

            return services;
        }
    }
}
