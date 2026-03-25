namespace User.Application.Common.Interfaces
{
    public interface ICurrentUserService
    {
        int? UserId { get; }
        string? UserName { get; }
        string? Email { get; }
        string? PhoneNumber { get; }
    }
}
