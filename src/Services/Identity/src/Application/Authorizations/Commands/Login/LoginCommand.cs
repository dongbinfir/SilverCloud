using Identity.Application.AccountInfos.Dtos;
using Identity.Application.Authorizations.Dtos;
using Identity.Application.Commons.Extensions;
using Identity.Application.Commons.MongoDbRepositories;

namespace Identity.Application.Authorizations.Commands.Login
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
        private readonly IAccountRefreshTokenRepository _accountRefreshTokenRepository;
        private readonly IMapper _mapper;
        private readonly IPasswordHashService _passwordHashService;
        private readonly ITokenService _tokenService;

        public LoginCommandHandler(
            IApplicationDbContext context,
            IAccountRefreshTokenRepository accountRefreshTokenRepository,
            IMapper mapper,
            IPasswordHashService passwordHashService,
            ITokenService tokenService)
        {
            _context = context;
            _accountRefreshTokenRepository = accountRefreshTokenRepository;
            _mapper = mapper;
            _passwordHashService = passwordHashService;
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

            var user = await _context.Set<AccountInfo>()
                .FirstOrDefaultAsync(u =>
                    (u.Email == identityEmail || u.PhoneNum == request.Identity),
                    cancellationToken);

            // 2. 验证密码
            if (user == null || !_passwordHashService.VerifyPassword(request.Password, user.Password))
            {
                throw new UnauthorizedAccessException("用户名或密码错误");
            }

            // 3. 生成 Token 对
            var tokens = _tokenService.GenerateTokenPair(
                user.Id,
                user.Name,
                user.Email?.Value ?? string.Empty,
                user.PhoneNum ?? string.Empty
            );

            // 4. 保存 RefreshToken 到 MongoDB
            var refreshTokenEntity = new AccountRefreshToken()
            {
                AccountInfoId = user.Id,
                Token = tokens.RefreshToken,
                ExpiresAt = DateTime.UtcNow.AddDays(_tokenService.GetRefreshTokenExpirationDays()),
                CreatedByIp = request.IpAddress ?? "Unknown",
                //Created = DateTime.UtcNow
            };

            await _accountRefreshTokenRepository.AddAsync(refreshTokenEntity);

            // 5. 映射用户信息
            var userDto = _mapper.Map<AccountInfoDto>(user);

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
