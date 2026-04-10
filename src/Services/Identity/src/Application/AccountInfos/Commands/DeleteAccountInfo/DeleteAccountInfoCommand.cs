namespace Identity.Application.AccountInfos.Commands.DeleteAccountInfo
{
    public record DeleteAccountInfoCommand(int Id) : IRequest<Unit>;

    public class DeleteAccountInfoCommandHandler : IRequestHandler<DeleteAccountInfoCommand, Unit>
    {
        private readonly IApplicationDbContext _context;

        public DeleteAccountInfoCommandHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Unit> Handle(DeleteAccountInfoCommand request, CancellationToken cancellationToken)
        {
            var entity = await _context.Set<AccountInfo>().FindAsync(request.Id, cancellationToken);

            if (entity == null)
            {
                throw new NotFoundException(nameof(AccountInfo), request.Id);
            }

            _context.Set<AccountInfo>().Remove(entity);

            await _context.SaveChangesAsync(cancellationToken);

            return Unit.Value;
        }
    }
}
