namespace Identity.Domain.SqlServerEntities
{
    public class Role : BaseAuditableEntity<int>
    {
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
    }
}
