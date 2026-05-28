namespace SMS.Application.DTOs;

/// <summary>اطلاعات کلی برای داشبورد دانش‌آموز/ولی</summary>
public class StudentPortalSummaryDto
{
    public int StudentId { get; set; }
    public string StudentCode { get; set; } = null!;
    public string FullName { get; set; } = null!;
    public string? FatherName { get; set; }
    public string? PhotoPath { get; set; }

    public string SchoolName { get; set; } = null!;
    public string ClassroomName { get; set; } = null!;
    public string GradeName { get; set; } = null!;
    public string AcademicYearTitle { get; set; } = null!;

    // آمار ترم جاری
    public decimal? CurrentGPA { get; set; }
    public int? RankInClass { get; set; }
    public int TotalAbsences { get; set; }
    public int UnexcusedAbsences { get; set; }
    public int TotalTardies { get; set; }
    public int RewardsCount { get; set; }
    public int PunishmentsCount { get; set; }

    // مالی
    public decimal TotalDebt { get; set; }
    public decimal TotalPaid { get; set; }
    public decimal RemainingDebt => TotalDebt - TotalPaid;

    // آخرین فعالیت‌ها
    public DateTime? LastAttendanceDate { get; set; }
    public string? LastAttendanceStatus { get; set; }
    public int UnreadAnnouncementsCount { get; set; }
}

/// <summary>سطر نمره دروس برای نمایش در پنل</summary>
public class StudentScoreRow
{
    public string SubjectName { get; set; } = null!;
    public string? TeacherName { get; set; }
    public string ExamTitle { get; set; } = null!;
    public string ExamTypeName { get; set; } = null!;
    public DateTime ExamDate { get; set; }
    public decimal? NumericScore { get; set; }
    public string? DescriptiveLabel { get; set; }
    public decimal? MaxScore { get; set; }
    public bool IsDescriptive { get; set; }
    public bool IsAbsent { get; set; }
    public bool IsExempt { get; set; }
    public string? Comment { get; set; }
}

/// <summary>سطر حضور و غیاب برای پنل دانش‌آموز</summary>
public class StudentAttendanceRow
{
    public DateTime AttendanceDate { get; set; }
    public string DayOfWeek { get; set; } = null!;
    public byte StatusId { get; set; }
    public string StatusName { get; set; } = null!;
    public string? StatusColor { get; set; }
    public short? TardyMinutes { get; set; }
    public string? Description { get; set; }
}

/// <summary>اطلاعات یک فرزند (برای پنل ولی)</summary>
public class ChildSummaryDto
{
    public int StudentId { get; set; }
    public string StudentCode { get; set; } = null!;
    public string FullName { get; set; } = null!;
    public string SchoolName { get; set; } = null!;
    public string ClassroomName { get; set; } = null!;
    public decimal? CurrentGPA { get; set; }
    public int RecentAbsences { get; set; }
    public decimal RemainingDebt { get; set; }
}
