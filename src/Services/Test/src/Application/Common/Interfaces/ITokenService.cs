using System.Security.Claims;

namespace User.Application.Common.Interfaces
{
    public interface ITokenService
    {
        string GenerateAccessToken(int userId, string userName, string email, string phoneNum);
        string GenerateRefreshToken();
        ClaimsPrincipal? ValidateToken(string token);
        TokenResponse GenerateTokenPair(int userId, string userName, string email, string phoneNum);
        int GetRefreshTokenExpirationDays();
    }
}
