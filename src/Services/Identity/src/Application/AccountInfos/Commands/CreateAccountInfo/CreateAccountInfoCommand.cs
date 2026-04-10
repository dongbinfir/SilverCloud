using Identity.Application.AccountInfos.Commons;

namespace Identity.Application.AccountInfos.Commands.CreateAccountInfo;

public record CreateAccountInfoCommand : IRequest<int>
{
    public string? Name { get; set; }

    public string? Email { get; set; }

    public string? PhoneNum { get; set; }

    public string Password { get; set; } = null!;
}

public class CreateAccountInfoCommandHandler : IRequestHandler<CreateAccountInfoCommand, int>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;
    private readonly IPasswordHashService _passwordHashService;

    public CreateAccountInfoCommandHandler(
        IApplicationDbContext context,
        IMapper mapper,
        IPasswordHashService passwordHashService)
    {
        _context = context;
        _mapper = mapper;
        _passwordHashService = passwordHashService;
    }

    public async Task<int> Handle(CreateAccountInfoCommand request, CancellationToken cancellationToken)
    {
        // 创建实体并映射
        var entity = new AccountInfo
        {
            Name = AccountInfoHelper.GetOrCreateName(request.Name),
            Email = request.Email != null ? Email.Create(request.Email) : null,
            PhoneNum = request.PhoneNum,
            Password = _passwordHashService.HashPassword(request.Password),
        };

        // 添加到数据库
        _context.Set<AccountInfo>().Add(entity);

        // 保存更改
        await _context.SaveChangesAsync(cancellationToken);

        return entity.Id;
    }
}
