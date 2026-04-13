using Identity.Domain.Enums;

namespace Identity.Domain.SqlServerEntities
{
    public class Permission : BaseAuditableEntity<int>
    {
        public string Resource { get; set; } = string.Empty; // 如: "User", "Order", "Report"
        public PermissionAction Action { get; set; }

        // 权限唯一标识，如 "Order:Approve"
        public string PermissionCode => $"{Resource}:{Action}";
    }
}
