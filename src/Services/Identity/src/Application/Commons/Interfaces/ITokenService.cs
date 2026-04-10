using Identity.Application.Commons.Models;
using System.Security.Claims;

namespace Identity.Application.Commons.Interfaces
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
