namespace Shared.Domain.Common
{
    public abstract class BaseAuditableEntity<T> : BaseEntity<T>, IBaseAuditableEntity
    {
        public DateTime Created { get; set; }

        public int? CreatedBy { get; set; }

        public DateTime? LastModified { get; set; }

        public int? LastModifiedBy { get; set; }
    }
}
