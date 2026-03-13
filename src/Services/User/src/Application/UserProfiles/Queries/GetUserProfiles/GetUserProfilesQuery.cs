using User.Application.UserProfiles.Dtos;

namespace User.Application.UserProfiles.Queries.GetUserProfiles
{
    public record GetUserProfilesQuery : IRequest<List<UserProfileBriefDto>>;

    public class GetUserProfilesQueryHandler : IRequestHandler<GetUserProfilesQuery, List<UserProfileBriefDto>>
    {
        private readonly IApplicationDbContext _context;
        private readonly IMapper _mapper;

        public GetUserProfilesQueryHandler(IApplicationDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public async Task<List<UserProfileBriefDto>> Handle(GetUserProfilesQuery request, CancellationToken cancellationToken)
        {
            return await _context.Set<UserProfile>()
                .AsNoTracking()
                .ProjectTo<UserProfileBriefDto>(_mapper.ConfigurationProvider)
                .ToListAsync(cancellationToken);
        }
    }
}
