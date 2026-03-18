using MediatR;
using User.Application.Common.Interfaces;
using User.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace User.Application.Auths.Commands.Logout
{
    public record LogoutCommand : IRequest<Unit>
    {
        public int UserId { get; set; }
        public string RefreshToken { get; set; } = null!;
        public string? IpAddress { get; set; }
    }

    public class LogoutCommandHandler : IRequestHandler<LogoutCommand, Unit>
    {
        private readonly IRefreshTokenRepository _tokenRepository;

        public LogoutCommandHandler(IRefreshTokenRepository tokenRepository)
        {
            _tokenRepository = tokenRepository;
        }

        public async Task<Unit> Handle(LogoutCommand request, CancellationToken cancellationToken)
        {
            // 查找并撤销指定的 RefreshToken
            var refreshToken = await _tokenRepository.GetByTokenAsync(request.RefreshToken);

            if (refreshToken != null && refreshToken.UserProfileId == request.UserId && refreshToken.IsActive)
            {
                // 标记为已撤销
                refreshToken.RevokedAt = DateTime.UtcNow;
                refreshToken.RevokedByIp = request.IpAddress ?? "Unknown";
                refreshToken.RevokedReason = "用户主动登出";

                await _tokenRepository.UpdateAsync(refreshToken);
            }

            return Unit.Value;
        }
    }
}
