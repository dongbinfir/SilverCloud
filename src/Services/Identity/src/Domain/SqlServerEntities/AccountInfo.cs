namespace Identity.Domain.SqlServerEntities
{
    public class AccountInfo : BaseAuditableEntity<int>
    {
        public string Name { get; set; } = null!;

        public Email? Email { get; set; }

        public string? PhoneNum { get; set; }

        public string Password { get; set; } = null!;
    }
}
