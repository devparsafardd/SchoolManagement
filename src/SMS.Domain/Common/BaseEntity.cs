namespace SMS.Domain.Common;

/// <summary>
/// کلاس پایه برای موجودیت‌هایی که نیاز به فیلدهای استاندارد دارند
/// </summary>
public abstract class BaseEntity
{
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ModifiedAt { get; set; }
    public bool IsActive { get; set; } = true;
}
