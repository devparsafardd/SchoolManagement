using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SMS.Domain.Entities;

/// <summary>
/// زنگ‌های روزانه مدرسه (زنگ اول 8:00-8:45 و...)
/// در سطح مدرسه تعریف می‌شود تا قابل سفارشی‌سازی باشد
/// </summary>
[Table("SchoolPeriods")]
public class SchoolPeriod
{
    [Key]
    public int PeriodId { get; set; }
    public int SchoolId { get; set; }

    /// <summary>زنگ چندم (1, 2, 3, ...)</summary>
    public byte PeriodNo { get; set; }

    [Required, MaxLength(50)]
    public string Name { get; set; } = null!; // "زنگ اول"

    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }

    /// <summary>آیا زنگ تفریح است؟</summary>
    public bool IsBreak { get; set; }

    public bool IsActive { get; set; } = true;

    public School School { get; set; } = null!;
}

/// <summary>
/// برنامه هفتگی - تخصیص یک ClassSubjectTeacher به یک زنگ مشخص در یک روز هفته
/// </summary>
[Table("ClassSchedules")]
public class ClassSchedule
{
    [Key]
    public int ScheduleId { get; set; }
    public int ClassroomId { get; set; }
    public int ClassSubjectId { get; set; }
    public int PeriodId { get; set; }

    /// <summary>روز هفته به‌صورت عدد ایرانی: 0=شنبه, 1=یکشنبه, ..., 6=جمعه</summary>
    public byte DayOfWeek { get; set; }

    [MaxLength(30)]
    public string? RoomNumber { get; set; }

    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Classroom Classroom { get; set; } = null!;
    public ClassSubjectTeacher ClassSubject { get; set; } = null!;
    public SchoolPeriod Period { get; set; } = null!;
}
