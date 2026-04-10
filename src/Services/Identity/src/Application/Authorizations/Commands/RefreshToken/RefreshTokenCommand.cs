using Identity.Application.Commons.Models;

namespace Identity.Application.Authorizations.Commands.RefreshToken
{
    public record RefreshTokenCommand : IRequest<TokenResponse>
    {
        public string AccessToken { get; set; } = null!;
        public string RefreshToken { get; set; } = null!;
    }

    public class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, TokenResponse>
    {
        private readonly IApplicationDbContext _context;
        private readonly IAccountRefreshTokenRepository _accountRefreshTokenRepository;
        private readonly ITokenService _tokenService;
        private readonly ICurrentAccountService _currentAccountService;

        public RefreshTokenCommandHandler(
            IApplicationDbContext context,
            IAccountRefreshTokenRepository accountRefreshTokenRepository,
            ITokenService tokenService,
            ICurrentAccountService currentAccountService)
        {
            _context = context;
            _accountRefreshTokenRepository = accountRefreshTokenRepository;
            _tokenService = tokenService;
            _currentAccountService = currentAccountService;
        }

        public async Task<TokenResponse> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
        {
            // 1. 验证 AccessToken（即使过期，也要获取用户信息）
            var principal = _tokenService.ValidateToken(request.AccessToken);
            if (principal == null)
            {
                throw new UnauthorizedAccessException("无效的 AccessToken");
            }

            // 2. 查找 RefreshToken
            var currentRefreshToken = await _accountRefreshTokenRepository.GetByTokenAsync(request.RefreshToken);

            if (currentRefreshToken == null || currentRefreshToken.AccountInfoId != _currentAccountService.Id)
            {
                throw new UnauthorizedAccessException("RefreshToken 不存在");
            }

            // 3. 检查 RefreshToken 状态
            if (currentRefreshToken.RevokedAt != null)
            {
                // 如果已被撤销，可能是安全事件，撤销该用户所有 Token
                await RevokeUserAllRefreshTokensAsync(_currentAccountService.Id.Value, "检测到安全威胁：使用了已撤销的 RefreshToken", cancellationToken);
                throw new UnauthorizedAccessException("RefreshToken 已被撤销");
            }

            if (currentRefreshToken.IsExpired)
            {
                throw new UnauthorizedAccessException("RefreshToken 已过期，请重新登录");
            }

            // 4. 生成新的 Token 对
            var newTokens = _tokenService.GenerateTokenPair(
                _currentAccountService.Id.Value,
                _currentAccountService.Name ?? string.Empty,
                _currentAccountService.Email ?? string.Empty,
                _currentAccountService.PhoneNum ?? string.Empty
            );

            // 5. 创建新的 RefreshToken 记录
            var newRefreshToken = new AccountRefreshToken()
            {
                AccountInfoId = _currentAccountService.Id.Value,
                Token = newTokens.RefreshToken,
                ExpiresAt = DateTime.UtcNow.AddDays(_tokenService.GetRefreshTokenExpirationDays()),
                CreatedByIp = currentRefreshToken.CreatedByIp,
                PreviousRefreshTokenId = currentRefreshToken.Id
            };

            // 6. 撤销旧的 RefreshToken（滚动更新）
            currentRefreshToken.RevokedAt = DateTime.UtcNow;
            currentRefreshToken.RevokedByIp = currentRefreshToken.CreatedByIp;
            currentRefreshToken.RevokedReason = "被新 Token 替换";
            currentRefreshToken.NextRefreshTokenId = newRefreshToken.Id;

            // 7. 保存更改
            await _accountRefreshTokenRepository.UpdateAsync(currentRefreshToken);
            await _accountRefreshTokenRepository.AddAsync(newRefreshToken);

            // 8. 返回新的 Token 对
            return newTokens;
        }

        /// <summary>
        /// 撤销用户的所有 RefreshToken
        /// </summary>
        private async Task RevokeUserAllRefreshTokensAsync(int userId, string reason, CancellationToken cancellationToken)
        {
            var activeTokens = await _accountRefreshTokenRepository.GetListByAccountIdAsync(userId);

            foreach (var token in activeTokens)
            {
                token.RevokedAt = DateTime.UtcNow;
                token.RevokedReason = reason;
                await _accountRefreshTokenRepository.UpdateAsync(token);
            }
        }
    }
}
