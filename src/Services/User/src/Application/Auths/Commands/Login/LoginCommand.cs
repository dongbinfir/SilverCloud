using User.Application.UserProfiles.Dtos;
using User.Application.Common.Extensions;
using User.Domain.Entities;
using User.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using User.Application.Common.Interfaces;

namespace User.Application.Auths.Commands.Login
{
    public record LoginCommand : IRequest<LoginResponseDto>
    {
        public string Identity { get; set; } = null!;
        public string Password { get; set; } = null!;
        public string? IpAddress { get; set; }
    }

    public class LoginCommandHandler : IRequestHandler<LoginCommand, LoginResponseDto>
    {
        private readonly IApplicationDbContext _context;
        private readonly IRefreshTokenRepository _tokenRepository;
        private readonly IMapper _mapper;
        private readonly IPasswordHasher _passwordHasher;
        private readonly ITokenService _tokenService;

        public LoginCommandHandler(
            IApplicationDbContext context,
            IRefreshTokenRepository tokenRepository,
            IMapper mapper,
            IPasswordHasher passwordHasher,
            ITokenService tokenService)
        {
            _context = context;
            _tokenRepository = tokenRepository;
            _mapper = mapper;
            _passwordHasher = passwordHasher;
            _tokenService = tokenService;
        }

        public async Task<LoginResponseDto> Handle(LoginCommand request, CancellationToken cancellationToken)
        {
            // 1. 验证用户身份（邮箱或手机号）
            Email? identityEmail = null;
            if (request.Identity.IsValidEmail())
            {
                identityEmail = Email.Create(request.Identity);
            }

            var user = await _context.Set<UserProfile>()
                .FirstOrDefaultAsync(u =>
                    (u.Email == identityEmail || u.PhoneNum == request.Identity),
                    cancellationToken);

            if (user == null)
            {
                throw new UnauthorizedAccessException("用户名或密码错误");
            }

            // 2. 验证密码
            if (!_passwordHasher.VerifyPassword(request.Password, user.Password))
            {
                throw new UnauthorizedAccessException("用户名或密码错误");
            }

            // 3. 生成 Token 对
            var tokens = _tokenService.GenerateTokenPair(
                user.Id,
                user.Email?.Value ?? string.Empty,
                user.PhoneNum ?? string.Empty
            );

            // 4. 保存 RefreshToken 到 MongoDB
            var refreshTokenEntity = new UserRefreshToken
            {
                UserProfileId = user.Id,
                Token = tokens.RefreshToken,
                ExpiresAt = DateTime.UtcNow.AddDays(_tokenService.GetRefreshTokenExpirationDays()),
                CreatedByIp = request.IpAddress ?? "Unknown",
                //Created = DateTime.UtcNow
            };

            await _tokenRepository.AddAsync(refreshTokenEntity);

            // 5. 映射用户信息
            var userDto = _mapper.Map<UserProfileBriefDto>(user);

            // 6. 返回登录响应
            return new LoginResponseDto
            {
                User = userDto,
                AccessToken = tokens.AccessToken,
                RefreshToken = tokens.RefreshToken,
                ExpiresAt = tokens.ExpiresAt
            };
        }
    }
}
