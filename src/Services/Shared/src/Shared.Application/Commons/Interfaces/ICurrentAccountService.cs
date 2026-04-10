namespace Shared.Application.Commons.Interfaces
{
    public interface ICurrentAccountService
    {
        int? Id { get; }
        string? Name { get; }
        string? Email { get; }
        string? PhoneNum { get; }
    }
}
