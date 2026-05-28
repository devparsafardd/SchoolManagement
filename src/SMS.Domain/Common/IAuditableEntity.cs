namespace SMS.Domain.Common;

/// <summary>
/// موجودیت‌هایی که می‌خواهیم تغییرات‌شان ثبت شود
/// </summary>
public interface IAuditableEntity
{
    DateTime CreatedAt { get; set; }
    int? CreatedByUserId { get; set; }
    DateTime? ModifiedAt { get; set; }
    int? ModifiedByUserId { get; set; }
}

/// <summary>
/// موجودیت‌هایی که قابلیت Soft Delete دارند
/// </summary>
public interface ISoftDeletable
{
    bool IsDeleted { get; set; }
    DateTime? DeletedAt { get; set; }
    int? DeletedByUserId { get; set; }
}

/// <summary>
/// کلاس پایه استاندارد - هم Audit هم Soft Delete
/// </summary>
public abstract class FullAuditEntity : IAuditableEntity, ISoftDeletable
{
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public int? CreatedByUserId { get; set; }
    public DateTime? ModifiedAt { get; set; }
    public int? ModifiedByUserId { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public int? DeletedByUserId { get; set; }
}
