namespace User.Domain.SqlServerEntities
{
    public class UserProfile : BaseAuditableEntity<int>
    {
        public int AccountInfoId { get; set; }
    }
}
