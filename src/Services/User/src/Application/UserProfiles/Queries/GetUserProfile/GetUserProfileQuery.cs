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

        public GetUserProfileQueryHandler(IApplicationDbContext context, IMapper mapper, IPasswordHasher passwordHasher, ITokenService tokenService)
        {
            _context = context;
            _mapper = mapper;
            _passwordHasher = passwordHasher;
            _tokenService = tokenService;
        }

        public async Task<UserProfileBriefDto> Handle(GetUserProfileQuery request, CancellationToken cancellationToken)
        {
            var entity = await _context.Set<UserProfile>()
                .Where(u => u.Id == request.Id)
                .FirstOrDefaultAsync(cancellationToken);

            if (entity == null)
            {
                throw new NotFoundException();
            }

            var userDto = _mapper.Map<UserProfileBriefDto>(entity);

            return userDto;
        }
    }
}
