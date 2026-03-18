using System.Security.Claims;
using User.Application.Common.Interfaces;
using User.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace User.Application.Auths.Commands.RefreshToken
{
    public record RefreshTokenCommand : IRequest<TokenResponse>
    {
        public string AccessToken { get; set; } = null!;
        public string RefreshToken { get; set; } = null!;
    }

    public class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, TokenResponse>
    {
        private readonly IApplicationDbContext _context;
        private readonly IRefreshTokenRepository _tokenRepository;
        private readonly ITokenService _tokenService;

        public RefreshTokenCommandHandler(
            IApplicationDbContext context,
            IRefreshTokenRepository tokenRepository,
            ITokenService tokenService)
        {
            _context = context;
            _tokenRepository = tokenRepository;
            _tokenService = tokenService;
        }

        public async Task<TokenResponse> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
        {
            // 1. 验证 AccessToken（即使过期，也要获取用户信息）
            var principal = _tokenService.ValidateToken(request.AccessToken);
            if (principal == null)
            {
                throw new UnauthorizedAccessException("无效的 AccessToken");
            }

            var userIdClaim = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out int userId))
            {
                throw new UnauthorizedAccessException("AccessToken 中的用户标识无效");
            }

            // 2. 查找 RefreshToken
            var currentRefreshToken = await _tokenRepository.GetByTokenAsync(request.RefreshToken);

            if (currentRefreshToken == null || currentRefreshToken.UserProfileId != userId)
            {
                throw new UnauthorizedAccessException("RefreshToken 不存在");
            }

            // 3. 检查 RefreshToken 状态
            if (currentRefreshToken.RevokedAt != null)
            {
                // 如果已被撤销，可能是安全事件，撤销该用户所有 Token
                await RevokeUserAllRefreshTokensAsync(userId, "检测到安全威胁：使用了已撤销的 RefreshToken", cancellationToken);
                throw new UnauthorizedAccessException("RefreshToken 已被撤销");
            }

            if (currentRefreshToken.IsExpired)
            {
                throw new UnauthorizedAccessException("RefreshToken 已过期，请重新登录");
            }

            var currentUserprofile = await _context.Set<UserProfile>()
                .FirstAsync(rt =>
                    rt.Id == userId,
                    cancellationToken);

            // 4. 生成新的 Token 对
            var newTokens = _tokenService.GenerateTokenPair(
                currentUserprofile.Id,
                currentUserprofile.Email?.Value ?? string.Empty,
                currentUserprofile.PhoneNum ?? string.Empty
            );

            // 5. 创建新的 RefreshToken 记录
            var newRefreshToken = new UserRefreshToken
            {
                UserProfileId = currentRefreshToken.UserProfileId,
                Token = newTokens.RefreshToken,
                ExpiresAt = DateTime.UtcNow.AddDays(_tokenService.GetRefreshTokenExpirationDays()),
                CreatedByIp = currentRefreshToken.CreatedByIp,
                //Created = DateTime.UtcNow,
                PreviousRefreshTokenId = currentRefreshToken.Id
            };

            // 6. 撤销旧的 RefreshToken（滚动更新）
            currentRefreshToken.RevokedAt = DateTime.UtcNow;
            currentRefreshToken.RevokedByIp = currentRefreshToken.CreatedByIp;
            currentRefreshToken.RevokedReason = "被新 Token 替换";
            currentRefreshToken.NextRefreshTokenId = newRefreshToken.Id;

            // 7. 保存更改
            await _tokenRepository.UpdateAsync(currentRefreshToken);
            await _tokenRepository.AddAsync(newRefreshToken);

            // 8. 返回新的 Token 对
            return newTokens;
        }

        /// <summary>
        /// 撤销用户的所有 RefreshToken
        /// </summary>
        private async Task RevokeUserAllRefreshTokensAsync(int userId, string reason, CancellationToken cancellationToken)
        {
            var activeTokens = await _tokenRepository.GetActiveTokensByUserIdAsync(userId);

            foreach (var token in activeTokens)
            {
                token.RevokedAt = DateTime.UtcNow;
                token.RevokedReason = reason;
                await _tokenRepository.UpdateAsync(token);
            }
        }
    }
}
