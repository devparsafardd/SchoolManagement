using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SMS.Domain.Entities;

/// <summary>
/// لاگ تغییرات سیستم - ثبت خودکار توسط Interceptor
/// </summary>
[Table("AuditLogs")]
public class AuditLog
{
    [Key]
    public long AuditId { get; set; }

    public int? UserId { get; set; }
    [MaxLength(100)] public string? Username { get; set; }

    /// <summary>Insert / Update / Delete / Login / Logout / Action</summary>
    [Required, MaxLength(50)]
    public string Action { get; set; } = null!;

    [Required, MaxLength(100)]
    public string EntityName { get; set; } = null!;

    [MaxLength(50)]
    public string? EntityId { get; set; }

    /// <summary>JSON از مقادیر قبلی</summary>
    public string? OldValues { get; set; }

    /// <summary>JSON از مقادیر جدید</summary>
    public string? NewValues { get; set; }

    /// <summary>لیست فیلدهای تغییر کرده (با کاما)</summary>
    [MaxLength(1000)]
    public string? ChangedColumns { get; set; }

    [MaxLength(45)] public string? IpAddress { get; set; }
    [MaxLength(300)] public string? UserAgent { get; set; }
    [MaxLength(500)] public string? Description { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
