namespace SMS.Application.DTOs;

/// <summary>خلاصه داشبورد معلم</summary>
public class TeacherDashboardDto
{
    public int StaffId { get; set; }
    public string FullName { get; set; } = null!;
    public string? PersonnelCode { get; set; }
    public string? PhotoPath { get; set; }

    public int TotalClasses { get; set; }          // تعداد کلاس‌های در دست تدریس
    public int TotalStudents { get; set; }         // مجموع دانش‌آموزان همه کلاس‌ها
    public int TodayClasses { get; set; }          // کلاس‌های امروز (از Schedule)
    public int PendingAttendance { get; set; }     // کلاس‌هایی که امروز حضور و غیابشان ثبت نشده
    public int UpcomingExams { get; set; }         // آزمون‌های ۷ روز آینده
    public int UnreadMessages { get; set; }        // پیام‌های خوانده نشده
    public int PendingHomeworks { get; set; }      // تکالیف بدون تصحیح
    public int TotalAnnouncements { get; set; }    // اعلان‌های فعال

    public List<TeacherClassBriefDto> MyClasses { get; set; } = new();
    public List<TeacherTodayClassDto> TodaySchedule { get; set; } = new();
    public List<TeacherUpcomingExamDto> UpcomingExamsList { get; set; } = new();
    public List<TeacherRecentActivityDto> RecentActivities { get; set; } = new();
}

public class TeacherClassBriefDto
{
    public int ClassSubjectId { get; set; }     // ClassSubjectTeachers.Id
    public int ClassroomId { get; set; }
    public string ClassroomName { get; set; } = null!;
    public int SchoolId { get; set; }
    public string SchoolName { get; set; } = null!;
    public string SubjectName { get; set; } = null!;
    public string GradeName { get; set; } = null!;
    public int StudentCount { get; set; }
    public decimal? WeeklyHours { get; set; }
    public int? CurrentAvgScore { get; set; }   // میانگین نمرات کلاس برای این درس
}

public class TeacherTodayClassDto
{
    public int ClassSubjectId { get; set; }
    public int ClassroomId { get; set; }
    public string ClassroomName { get; set; } = null!;
    public string SubjectName { get; set; } = null!;
    public string StartTime { get; set; } = null!;   // مثلاً "08:00"
    public string EndTime { get; set; } = null!;
    public int? SessionNo { get; set; }              // زنگ چندم
    public byte DayOfWeek { get; set; }              // 0=شنبه ... 6=جمعه
    public bool AttendanceTaken { get; set; }
}

public class TeacherUpcomingExamDto
{
    public long ExamId { get; set; }
    public string Title { get; set; } = null!;
    public DateTime ExamDate { get; set; }
    public string ClassroomName { get; set; } = null!;
    public string SubjectName { get; set; } = null!;
    public string ExamTypeName { get; set; } = null!;
    public bool ScoresEntered { get; set; }
}

public class TeacherRecentActivityDto
{
    public string Type { get; set; } = null!;   // attendance / score / homework / message
    public string Description { get; set; } = null!;
    public DateTime At { get; set; }
    public string? Link { get; set; }
}

/// <summary>جزئیات یک کلاس درسی برای نمای معلم</summary>
public class TeacherClassDetailDto
{
    public int ClassSubjectId { get; set; }
    public int ClassroomId { get; set; }
    public string ClassroomName { get; set; } = null!;
    public string SchoolName { get; set; } = null!;
    public string SubjectName { get; set; } = null!;
    public string GradeName { get; set; } = null!;
    public string AcademicYearTitle { get; set; } = null!;
    public decimal? WeeklyHours { get; set; }

    public List<TeacherClassStudentRow> Students { get; set; } = new();
    public List<TeacherClassExamSummary> Exams { get; set; } = new();
    public List<TeacherClassAttendanceSummary> RecentAttendance { get; set; } = new();
}

public class TeacherClassStudentRow
{
    public int StudentId { get; set; }
    public string StudentCode { get; set; } = null!;
    public string FullName { get; set; } = null!;
    public string Gender { get; set; } = "M";
    public string? Mobile { get; set; }
    public string? PhotoPath { get; set; }
    public decimal? AverageScore { get; set; }
    public int AbsenceCount { get; set; }
    public int TardyCount { get; set; }
    public int DisciplineRewards { get; set; }
    public int DisciplinePunishments { get; set; }
}

public class TeacherClassExamSummary
{
    public long ExamId { get; set; }
    public string Title { get; set; } = null!;
    public DateTime ExamDate { get; set; }
    public string ExamTypeName { get; set; } = null!;
    public bool IsFinalized { get; set; }
    public int TotalStudents { get; set; }
    public int ScoredCount { get; set; }
    public decimal? ClassAverage { get; set; }
}

public class TeacherClassAttendanceSummary
{
    public DateTime Date { get; set; }
    public int PresentCount { get; set; }
    public int AbsentCount { get; set; }
    public int TardyCount { get; set; }
}

/// <summary>تنظیمات فیلتر داشبورد</summary>
public class TeacherDashboardFilter
{
    public int? AcademicYearId { get; set; }
    public int? SchoolId { get; set; }
}
