namespace Identity.Domain.SqlServerEntities
{
    public class AccountInfoRole : BaseAuditableEntity<int>
    {
        public int AccountInfoId { get; set; }

        public int RoleId { get; set; }
    }
}
