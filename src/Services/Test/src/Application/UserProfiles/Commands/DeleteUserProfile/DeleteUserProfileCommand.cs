namespace User.Application.UserProfiles.Commands.DeleteUserProfile
{
    public record DeleteUserProfileCommand(int Id) : IRequest<Unit>;

    public class DeleteUserProfileCommandHandler : IRequestHandler<DeleteUserProfileCommand, Unit>
    {
        private readonly IApplicationDbContext _context;

        public DeleteUserProfileCommandHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Unit> Handle(DeleteUserProfileCommand request, CancellationToken cancellationToken)
        {
            var entity = await _context.Set<UserProfile>().FindAsync(request.Id, cancellationToken);

            if (entity == null)
            {
                throw new NotFoundException(nameof(UserProfile), request.Id);
            }

            _context.Set<UserProfile>().Remove(entity);

            await _context.SaveChangesAsync(cancellationToken);

            return Unit.Value;
        }
    }
}
