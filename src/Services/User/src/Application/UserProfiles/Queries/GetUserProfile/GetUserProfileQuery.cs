using User.Application.Common.Extensions;
using User.Application.UserProfiles.Dtos;
using User.Domain.ValueObjects;

namespace User.Application.UserProfiles.Queries.GetUserProfile
{
    public record GetUserProfileQuery : IRequest<UserProfileBriefDto>
    {
        public string Identity { get; set; } = null!;
        public string Password { get; set; } = null!;
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
            Email? identityEmail = null;
            if (request.Identity.IsValidEmail())
            {
                identityEmail = Email.Create(request.Identity);
            }

            var entity = await _context.Set<UserProfile>().AsNoTracking()
                .Where(a => a.Password == request.Password &&
                    (a.PhoneNum == request.Identity || (a.Email == identityEmail)))
                .ProjectTo<UserProfileBriefDto>(_mapper.ConfigurationProvider)
                .FirstOrDefaultAsync(cancellationToken);

            if (entity == null)
            {
                throw new NotFoundException(nameof(UserProfile));
            }

            return entity;
        }
    }
}
