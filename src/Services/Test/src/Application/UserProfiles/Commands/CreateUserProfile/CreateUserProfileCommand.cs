using User.Application.UserProfiles.Common;
using User.Domain.ValueObjects;

namespace User.Application.UserProfiles.Commands.CreateUserProfile;

public record CreateUserProfileCommand : IRequest<int>
{
    public string? Name { get; set; }

    public string? Email { get; set; }

    public string? PhoneNum { get; set; }

    public string Password { get; set; } = null!;
}

public class CreateUserProfileCommandHandler : IRequestHandler<CreateUserProfileCommand, int>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;
    private readonly IPasswordHasher _passwordHasher;

    public CreateUserProfileCommandHandler(
        IApplicationDbContext context,
        IMapper mapper,
        IPasswordHasher passwordHasher)
    {
        _context = context;
        _mapper = mapper;
        _passwordHasher = passwordHasher;
    }

    public async Task<int> Handle(CreateUserProfileCommand request, CancellationToken cancellationToken)
    {
        // 创建实体并映射
        var entity = new UserProfile
        {
            Name = UserProfileHelper.GetOrCreateName(request.Name),
            Email = request.Email != null ? Email.Create(request.Email) : null,
            PhoneNum = request.PhoneNum,
            Password = _passwordHasher.HashPassword(request.Password),
        };

        // 添加到数据库
        _context.Set<UserProfile>().Add(entity);

        // 保存更改
        await _context.SaveChangesAsync(cancellationToken);

        return entity.Id;
    }
}
