namespace User.Domain.Common
{
    public abstract class BaseAuditableEntity<T> : BaseEntity<T>, IBaseAuditableEntity
    {
        public DateTime Created { get; set; }

        public string? CreatedBy { get; set; }

        public DateTime? LastModified { get; set; }

        public string? LastModifiedBy { get; set; }
    }
}
