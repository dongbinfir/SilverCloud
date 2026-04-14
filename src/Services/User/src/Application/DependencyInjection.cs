using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace User.Application
{
    /// <summary>
    /// Application 层的服务注册
    /// </summary>
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplicationServices(this IServiceCollection services)
        {
            // 注册 AutoMapper
            services.AddAutoMapper(cfg =>
            {
                cfg.AddProfile(new MappingProfile(Assembly.GetExecutingAssembly()));
            });

            // 注册 MediatR
            services.AddMediatR(cfg =>
            {
                cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly());
            });

            // 注册验证器
            services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

            return services;
        }
    }
}
