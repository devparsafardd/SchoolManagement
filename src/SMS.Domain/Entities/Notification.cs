using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SMS.Domain.Entities;

[Table("Announcements")]
public class Announcement
{
    [Key]
    public int AnnouncementId { get; set; }
    public int? SchoolId { get; set; }
    public int? ClassroomId { get; set; }

    [Required, MaxLength(200)] public string Title { get; set; } = null!;
    [Required] public string Body { get; set; } = null!;

    /// <summary>All / Students / Parents / Teachers</summary>
    [Required, MaxLength(30)] public string TargetAudience { get; set; } = "All";

    public DateTime PublishDate { get; set; } = DateTime.UtcNow;
    public DateTime? ExpiryDate { get; set; }
    public int CreatedByUserId { get; set; }
    public bool IsActive { get; set; } = true;
}

[Table("SmsLogs")]
public class SmsLog
{
    [Key]
    public long SmsLogId { get; set; }
    public int? StudentId { get; set; }
    public int? GuardianId { get; set; }
    [Required, MaxLength(15)] public string Mobile { get; set; } = null!;
    [Required] public string Text { get; set; } = null!;

    /// <summary>Sent / Failed / Pending</summary>
    [Required, MaxLength(20)] public string Status { get; set; } = "Pending";

    [MaxLength(100)] public string? ProviderMessageId { get; set; }
    [MaxLength(500)] public string? FailReason { get; set; }
    public int? Cost { get; set; }
    public int? SentByUserId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
