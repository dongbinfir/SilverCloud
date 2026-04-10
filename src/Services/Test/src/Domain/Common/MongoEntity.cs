namespace User.Domain.Common;

/// <summary>
/// MongoDB 实体基础接口
/// </summary>
public abstract class MongoEntity: IBaseAuditableEntity
{
    public string Id { get; set; } = null!;

    public DateTime Created { get; set; }

    public int? CreatedBy { get; set; }

    public DateTime? LastModified { get; set; }

    public int? LastModifiedBy { get; set; }
}