using System.Security.Claims;
using User.Application.Common.Interfaces;

namespace WebAPI.Services
{
    public class CurrentUserService : ICurrentUserService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CurrentUserService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }
        private ClaimsPrincipal? _user;
        private ClaimsPrincipal? User => _user ??= _httpContextAccessor.HttpContext?.User;

        public bool IsAuthenticated => User?.Identity?.IsAuthenticated == true;

        public int? UserId
        {
            get
            {
                var value = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                return int.TryParse(value, out var id) ? id : null;
            }
        }
        public string? UserName => User?.FindFirst(ClaimTypes.Name)?.Value;

        public string? Email => User?.FindFirst(ClaimTypes.Email)?.Value;

        public string? PhoneNumber => User?.FindFirst(ClaimTypes.MobilePhone)?.Value;
    }
}
