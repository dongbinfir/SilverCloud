using Shared.Application.Commons.Interfaces;
using System.Security.Claims;

namespace Identity.WebAPI.Services
{
    public class CurrentAccountService : ICurrentAccountService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CurrentAccountService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }
        private ClaimsPrincipal? _accountInfo;
        private ClaimsPrincipal? AccountInfo => _accountInfo ??= _httpContextAccessor.HttpContext?.User;


        public bool IsAuthenticated => AccountInfo?.Identity?.IsAuthenticated == true;

        public int? Id
        {
            get
            {
                var value = AccountInfo?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                return int.TryParse(value, out var id) ? id : null;
            }
        }
        public string? Name => AccountInfo?.FindFirst(ClaimTypes.Name)?.Value;

        public string? Email => AccountInfo?.FindFirst(ClaimTypes.Email)?.Value;

        public string? PhoneNum => AccountInfo  ?.FindFirst(ClaimTypes.MobilePhone)?.Value;
    }
}
