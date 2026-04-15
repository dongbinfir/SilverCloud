using Microsoft.Extensions.DependencyInjection;
using Shared.Application.Commons.Interfaces;

namespace Shared.Infrastructure.Commons
{
    public static class CurrentAccountServiceExtensions
    {
        public static IServiceCollection AddCurrentAccountService(this IServiceCollection services)
        {
            services.AddHttpContextAccessor();
            services.AddScoped<ICurrentAccountService, CurrentAccountService>();

            return services;
        }
    }
}