using User.Application.Common.Extensions;
using User.Application.UserProfiles.Dtos;
using User.Domain.ValueObjects;

namespace User.Application.UserProfiles.Queries.GetUserProfile
{
    public record GetUserProfileQuery : IRequest<LoginResponseDto>
    {
        public string Identity { get; set; } = null!;
        public string Password { get; set; } = null!;
    }

    public class GetUserProfileQueryHandler : IRequestHandler<GetUserProfileQuery, LoginResponseDto>
    {
        private readonly IApplicationDbContext _context;
        private readonly IMapper _mapper;
        private readonly IPasswordHasher _passwordHasher;
        private readonly ITokenService _tokenService;

        public GetUserProfileQueryHandler(IApplicationDbContext context, IMapper mapper, IPasswordHasher passwordHasher, ITokenService tokenService)
        {
            _context = context;
            _mapper = mapper;
            _passwordHasher = passwordHasher;
            _tokenService = tokenService;
        }

        public async Task<LoginResponseDto> Handle(GetUserProfileQuery request, CancellationToken cancellationToken)
        {
            Email? identityEmail = null;
            if (request.Identity.IsValidEmail())
            {
                identityEmail = Email.Create(request.Identity);
            }

            var entity = await _context.Set<UserProfile>()
                .Where(u =>
                (u.Email == identityEmail ||
                 u.PhoneNum == request.Identity))
            .FirstOrDefaultAsync(cancellationToken);  // 修改：不在这里比较密码

            if (entity == null)
            {
                throw new UnauthorizedAccessException("用户名或密码错误");
            }

            // 2. 验证密码（使用 BCrypt）
            if (!_passwordHasher.VerifyPassword(request.Password, entity.Password))
            {
                throw new UnauthorizedAccessException("用户名或密码错误");
            }

            // 3. 生成 Token
            var tokens = _tokenService.GenerateTokenPair(
                entity.Id,
                entity.Email?.Value ?? string.Empty,
                entity.PhoneNum ?? string.Empty
            );

            // 4. 映射用户信息
            var userDto = _mapper.Map<UserProfileBriefDto>(entity);

            // 5. 返回用户信息 + Token
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
