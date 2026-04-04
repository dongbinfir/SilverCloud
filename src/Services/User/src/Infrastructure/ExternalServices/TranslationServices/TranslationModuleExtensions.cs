using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using User.Domain.ExternalInterfaces.TranslationInterface;
using User.Infrastructure.ExternalServices.TranslationServices.Services;

namespace User.Infrastructure.ExternalServices.TranslationServices
{
    public static class TranslationModuleExtensions
    {
        public static IServiceCollection AddTranslationExtensionService(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            services.Configure<TranslationOptions>(configuration.GetSection(nameof(TranslationOptions)));


            services.AddHttpClient<BaiduTranslationService>();
            services.AddKeyedScoped<ITranslationService, BaiduTranslationService>(TranslationSource.Baidu);

            services.AddKeyedScoped<ITranslationService, GoogleTranslationService>(TranslationSource.Google);

            return services;
        }
    }
}
