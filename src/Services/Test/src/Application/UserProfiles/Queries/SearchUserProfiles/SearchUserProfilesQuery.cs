using User.Application.UserProfiles.Dtos;

namespace User.Application.UserProfiles.Queries.SearchUserProfiles
{
    public record SearchUserProfilesQuery : IRequest<PaginatedList<UserProfileBriefDto>>
    {
        public int PageNumber { get; init; } = 1;
        public int PageSize { get; init; } = 10;
    }

    public class SearchUserProfilesQueryHandler : IRequestHandler<SearchUserProfilesQuery, PaginatedList<UserProfileBriefDto>>
    {
        private readonly IApplicationDbContext _context;
        private readonly IMapper _mapper;

        public SearchUserProfilesQueryHandler(IApplicationDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public async Task<PaginatedList<UserProfileBriefDto>> Handle(SearchUserProfilesQuery request, CancellationToken cancellationToken)
        {
            return await _context.Set<UserProfile>().AsNoTracking()
                .ProjectTo<UserProfileBriefDto>(_mapper.ConfigurationProvider)
                .PaginatedListAsync(request.PageNumber, request.PageSize, cancellationToken);
        }
    }
}
