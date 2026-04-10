using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using User.Application.Common.Interfaces;
using WebAPI.Services;

namespace WebAPI.ConfigureServices
{
    public static class AuthenticationExtensions
    {
        public static IServiceCollection AddJwtAuth(this IServiceCollection services, IConfiguration config)
        {
            // 1. 添加 JWT 配置
            services.Configure<JwtSettings>(
                config.GetSection(JwtSettings.SectionName));

            // 2. 配置 JWT Bearer 认证
            var jwtSettings = config.GetSection(JwtSettings.SectionName).Get<JwtSettings>();
            var key = Encoding.UTF8.GetBytes(jwtSettings!.Secret);

            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = true,
                    ValidIssuer = jwtSettings.Issuer,
                    ValidateAudience = true,
                    ValidAudience = jwtSettings.Audience,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                };
            });

            services.AddAuthorization();

            services.AddScoped<ICurrentUserService, CurrentUserService>();

            return services;
        }
    }
}
