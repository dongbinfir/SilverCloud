using Identity.Application.AccountInfos.Commons;
using Identity.Application.AccountInfos.Dtos;

namespace Identity.Application.AccountInfos.Queries.GetAccountInfo
{
    public record GetAccountInfoQuery : IRequest<AccountInfoDto>
    {
        public int Id { get; set; }
    }

    public class GetAccountInfoQueryHandler : IRequestHandler<GetAccountInfoQuery, AccountInfoDto>
    {
        private readonly IApplicationDbContext _context;
        private readonly IMapper _mapper;
        private readonly IPasswordHashService _passwordHashService;
        private readonly ITokenService _tokenService;
        private readonly ICacheService _cacheService;

        public GetAccountInfoQueryHandler(
            IApplicationDbContext context,
            IMapper mapper,
            IPasswordHashService passwordHashService,
            ITokenService tokenService,
            ICacheService cacheService)
        {
            _context = context;
            _mapper = mapper;
            _passwordHashService = passwordHashService;
            _tokenService = tokenService;
            _cacheService = cacheService;
        }

        public async Task<AccountInfoDto> Handle(GetAccountInfoQuery request, CancellationToken cancellationToken)
        {
            return await _cacheService.GetOrSetAsync(AccountInfoCacheKeys.AccountInfoCacheKey(request.Id),
                async (CancellationToken ct) =>
                {
                    var entity = await _context.Set<AccountInfo>()
                        .Where(u => u.Id == request.Id)
                        .FirstOrDefaultAsync(ct);

                    if (entity == null)
                    {
                        throw new NotFoundException();
                    }

                    return _mapper.Map<AccountInfoDto>(entity);

                },
                cancellationToken);
        }
    }
}
