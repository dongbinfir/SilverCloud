namespace User.Domain.Common;

/// <summary>
/// MongoDB 实体基础接口
/// </summary>
public abstract class MongoEntity
{
    public string Id { get; set; } = null!;

    public DateTime Created { get; set; }

    public string? CreatedBy { get; set; }

    public DateTime? LastModified { get; set; }

    public string? LastModifiedBy { get; set; }
}