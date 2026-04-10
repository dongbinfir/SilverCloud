using User.Application.UserProfiles.Common;
using User.Application.UserProfiles.Dtos;

namespace User.Application.UserProfiles.Queries.GetUserProfile
{
    public record GetUserProfileQuery : IRequest<UserProfileBriefDto>
    {
        public int Id { get; set; }
    }

    public class GetUserProfileQueryHandler : IRequestHandler<GetUserProfileQuery, UserProfileBriefDto>
    {
        private readonly IApplicationDbContext _context;
        private readonly IMapper _mapper;
        private readonly IPasswordHasher _passwordHasher;
        private readonly ITokenService _tokenService;
        private readonly ICacheService _cacheService;

        public GetUserProfileQueryHandler(
            IApplicationDbContext context,
            IMapper mapper,
            IPasswordHasher passwordHasher,
            ITokenService tokenService,
            ICacheService cacheService)
        {
            _context = context;
            _mapper = mapper;
            _passwordHasher = passwordHasher;
            _tokenService = tokenService;
            _cacheService = cacheService;
        }

        public async Task<UserProfileBriefDto> Handle(GetUserProfileQuery request, CancellationToken cancellationToken)
        {
            return await _cacheService.GetOrSetAsync(CachKeys.UserProfileCacheKey(request.Id),
                async (CancellationToken ct) =>
                {
                    var entity = await _context.Set<UserProfile>()
                        .Where(u => u.Id == request.Id)
                        .FirstOrDefaultAsync(ct);

                    if (entity == null)
                    {
                        throw new NotFoundException();
                    }

                    return _mapper.Map<UserProfileBriefDto>(entity);

                },
                cancellationToken);
        }
    }
}
