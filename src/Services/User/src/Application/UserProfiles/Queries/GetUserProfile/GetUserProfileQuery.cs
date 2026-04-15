using User.Application.UserProfiles.Dtos;

namespace User.Application.UserProfiles.Queries.GetUserProfile
{
    public record GetUserProfileQuery : IRequest<UserProfileDto>
    {
        public int Id { get; set; }
    }

    public class GetUserProfileQueryHandler : IRequestHandler<GetUserProfileQuery, UserProfileDto>
    {
        private readonly IApplicationDbContext _context;
        private readonly IMapper _mapper;

        public GetUserProfileQueryHandler(
            IApplicationDbContext context,
            IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public async Task<UserProfileDto> Handle(GetUserProfileQuery request, CancellationToken cancellationToken)
        {
            var entity = await _context.Set<UserProfile>()
                .Where(u => u.Id == request.Id)
                .FirstOrDefaultAsync(cancellationToken);

            if (entity == null)
            {
                throw new NotFoundException();
            }
            return _mapper.Map<UserProfileDto>(entity);
        }
    }
}
