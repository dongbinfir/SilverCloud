namespace Identity.Application.AccountInfos.Commands.UpdateAccountInfo
{
    public record UpdateAccountInfoCommand : IRequest<Unit>, IMapToWithExcludedId<AccountInfo>
    {
        public int Id { get; set; }
        public string Email { get; set; } = null!;
        public string Password { get; set; } = null!;
    }

    public class UpdateAccountInfoCommandHandler : IRequestHandler<UpdateAccountInfoCommand, Unit>
    {
        private readonly IApplicationDbContext _context;
        private readonly IMapper _mapper;

        public UpdateAccountInfoCommandHandler(IApplicationDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public async Task<Unit> Handle(UpdateAccountInfoCommand request, CancellationToken cancellationToken)
        {

            var entity = await _context.Set<AccountInfo>().FindAsync(request.Id, cancellationToken);

            if (entity == null)
            {
                throw new NotFoundException(nameof(AccountInfo), request.Id);
            }

            // 将 Command 的属性映射到现有实体（不创建新实例）
            _mapper.Map(request, entity);

            // 保存更改
            await _context.SaveChangesAsync(cancellationToken);

            return Unit.Value;
        }
    }
}
