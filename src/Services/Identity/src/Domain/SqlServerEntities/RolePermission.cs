namespace Identity.Domain.SqlServerEntities
{
    public class RolePermission : BaseAuditableEntity<int>
    {
        public int RoleId { get; set; }

        public int PermissionId { get; set; }
    }
}
