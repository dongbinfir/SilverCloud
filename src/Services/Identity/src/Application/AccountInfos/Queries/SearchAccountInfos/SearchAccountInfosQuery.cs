using Identity.Application.AccountInfos.Dtos;


namespace Identity.Application.AccountInfos.Queries.SearchAccountInfos
{
    public record SearchAccountInfosQuery : IRequest<PaginatedList<AccountInfoDto>>
    {
        public int PageNumber { get; init; } = 1;
        public int PageSize { get; init; } = 10;
    }

    public class SearchAccountInfosQueryHandler : IRequestHandler<SearchAccountInfosQuery, PaginatedList<AccountInfoDto>>
    {
        private readonly IApplicationDbContext _context;
        private readonly IMapper _mapper;

        public SearchAccountInfosQueryHandler(IApplicationDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public async Task<PaginatedList<AccountInfoDto>> Handle(SearchAccountInfosQuery request, CancellationToken cancellationToken)
        {
            return await _context.Set<AccountInfo>().AsNoTracking()
                .ProjectTo<AccountInfoDto>(_mapper.ConfigurationProvider)
                .PaginatedListAsync(request.PageNumber, request.PageSize, cancellationToken);
        }
    }
}
