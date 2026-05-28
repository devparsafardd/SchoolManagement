using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SMS.Domain.Entities;

/// <summary>وضعیت‌های حضور: حاضر، غایب، تاخیر، مرخصی، ...</summary>
[Table("AttendanceStatuses")]
public class AttendanceStatus
{
    [Key]
    public byte StatusId { get; set; }
    [Required, MaxLength(30)] public string Name { get; set; } = null!;
    [Required, MaxLength(20)] public string Code { get; set; } = null!;
    public bool IsAbsent { get; set; }
    public bool IsTardy { get; set; }

    /// <summary>رنگ برای نمایش (Hex)</summary>
    [MaxLength(10)] public string? Color { get; set; }
}

[Table("Attendances")]
public class Attendance
{
    [Key]
    public long AttendanceId { get; set; }
    public int StudentId { get; set; }
    public int ClassroomId { get; set; }
    public int? ClassSubjectId { get; set; }

    public DateTime AttendanceDate { get; set; }
    public byte StatusId { get; set; }
    public short? TardyMinutes { get; set; }

    [MaxLength(300)]
    public string? Description { get; set; }

    public int? RecordedByStaffId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Student Student { get; set; } = null!;
    public Classroom Classroom { get; set; } = null!;
    public AttendanceStatus Status { get; set; } = null!;
}
