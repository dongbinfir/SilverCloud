using Identity.Application.AccountInfos.Dtos;

namespace Identity.Application.Authorizations.Dtos
{
    public class LoginResponseDto
    {
        public AccountInfoDto User { get; set; } = null!;
        public string AccessToken { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
    }
}
