using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SMS.Domain.Entities;

/// <summary>
/// رویدادهای تقویم آموزشی (آزمون، اردو، تعطیلی، جلسه اولیا و...)
/// </summary>
[Table("CalendarEvents")]
public class CalendarEvent
{
    [Key]
    public int EventId { get; set; }

    /// <summary>اگر null باشد، رویداد سراسری است</summary>
    public int? SchoolId { get; set; }

    /// <summary>اگر null باشد، برای کل مدرسه است</summary>
    public int? ClassroomId { get; set; }

    [Required, MaxLength(200)]
    public string Title { get; set; } = null!;

    [MaxLength(1000)]
    public string? Description { get; set; }

    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public bool IsAllDay { get; set; } = true;

    /// <summary>Holiday / Exam / Trip / Meeting / Ceremony / Other</summary>
    [Required, MaxLength(30)]
    public string EventType { get; set; } = "Other";

    /// <summary>رنگ هگزی برای نمایش (مثل #ff0000)</summary>
    [MaxLength(10)]
    public string? Color { get; set; }

    /// <summary>All / Students / Parents / Teachers / Staff</summary>
    [MaxLength(30)]
    public string TargetAudience { get; set; } = "All";

    public bool SendNotification { get; set; }
    public int CreatedByUserId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public School? School { get; set; }
    public Classroom? Classroom { get; set; }
    public User CreatedBy { get; set; } = null!;
}
