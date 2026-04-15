namespace User.Application.UserProfiles.Commands.CreateUserProfile;

public record CreateUserProfileCommand : IRequest<int>
{
    public int AccountInfoId { get; set; }

}

public class CreateUserProfileCommandHandler : IRequestHandler<CreateUserProfileCommand, int>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;

    public CreateUserProfileCommandHandler(
        IApplicationDbContext context,
        IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<int> Handle(CreateUserProfileCommand request, CancellationToken cancellationToken)
    {
        // 创建实体并映射
        var entity = new UserProfile
        {
            AccountInfoId = request.AccountInfoId,
        };

        // 添加到数据库
        _context.Set<UserProfile>().Add(entity);

        // 保存更改
        await _context.SaveChangesAsync(cancellationToken);

        return entity.Id;
    }
}
