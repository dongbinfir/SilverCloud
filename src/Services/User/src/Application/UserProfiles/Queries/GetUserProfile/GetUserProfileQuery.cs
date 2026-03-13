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

        public GetUserProfileQueryHandler(IApplicationDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public async Task<UserProfileBriefDto> Handle(GetUserProfileQuery request, CancellationToken cancellationToken)
        {
            var entity = await _context.Set<UserProfile>().AsNoTracking()
            .ProjectTo<UserProfileBriefDto>(_mapper.ConfigurationProvider)
            .FirstOrDefaultAsync(a => a.Id == request.Id);

            if (entity == null)
            {
                throw new NotFoundException(nameof(UserProfile), request.Id);
            }

            return entity;
        }
    }
}
