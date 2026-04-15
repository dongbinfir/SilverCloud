namespace User.WebAPI.ConfigureServices
{
    public static class HttpExtensions
    {
        public static IServiceCollection AddHttpServices(this IServiceCollection services)
        {
            services.AddHttpContextAccessor();
            services.AddHttpClient();

            return services;
        }
    }
}