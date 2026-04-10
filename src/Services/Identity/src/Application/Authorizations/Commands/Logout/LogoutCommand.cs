
namespace Identity.Application.Authorizations.Commands.Logout
{
    public record LogoutCommand : IRequest<Unit>
    {
        public int? AccountInfoId { get; set; }
        public string RefreshToken { get; set; } = null!;
        public string? IpAddress { get; set; }
    }

    public class LogoutCommandHandler : IRequestHandler<LogoutCommand, Unit>
    {
        private readonly IAccountRefreshTokenRepository _accountRefreshTokenRepository;

        public LogoutCommandHandler(IAccountRefreshTokenRepository accountRefreshTokenRepository)
        {
            _accountRefreshTokenRepository = accountRefreshTokenRepository;
        }

        public async Task<Unit> Handle(LogoutCommand request, CancellationToken cancellationToken)
        {
            // 查找并撤销指定的 RefreshToken
            var refreshToken = await _accountRefreshTokenRepository.GetByTokenAsync(request.RefreshToken);

            if (refreshToken != null && refreshToken.AccountInfoId == request.AccountInfoId && refreshToken.IsActive)
            {
                // 标记为已撤销
                refreshToken.RevokedAt = DateTime.UtcNow;
                refreshToken.RevokedByIp = request.IpAddress ?? "Unknown";
                refreshToken.RevokedReason = "用户主动登出";

                await _accountRefreshTokenRepository.UpdateAsync(refreshToken);
            }

            return Unit.Value;
        }
    }
}
