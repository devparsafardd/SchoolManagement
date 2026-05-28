namespace SMS.Application.DTOs;

/// <summary>داشبورد مدیر مدرسه - آمار کلی یک مدرسه</summary>
public class PrincipalDashboardDto
{
    public int SchoolId { get; set; }
    public string SchoolName { get; set; } = null!;
    public string SchoolCode { get; set; } = null!;
    public string AcademicYearTitle { get; set; } = null!;
    public int? AcademicYearId { get; set; }

    // آمار کلی
    public int TotalStudents { get; set; }
    public int TotalMaleStudents { get; set; }
    public int TotalFemaleStudents { get; set; }
    public int TotalClassrooms { get; set; }
    public int TotalTeachers { get; set; }
    public int TotalStaff { get; set; }
    public int TotalGuardians { get; set; }
    public int TotalSubjects { get; set; }

    // آمار امروز
    public int TodayPresent { get; set; }
    public int TodayAbsent { get; set; }
    public int TodayTardy { get; set; }
    public decimal TodayPresentRate { get; set; } // درصد حضور

    // مالی
    public decimal TotalRevenue { get; set; }
    public decimal TotalPaid { get; set; }
    public decimal TotalUnpaid { get; set; }
    public int OverdueInvoicesCount { get; set; }

    // انضباطی
    public int RewardsThisMonth { get; set; }
    public int PunishmentsThisMonth { get; set; }

    // داده برای نمودار
    public List<PrincipalChartPoint> AttendanceLast7Days { get; set; } = new();
    public List<PrincipalGradeStudentCount> StudentsByGrade { get; set; } = new();
    public List<PrincipalClassroomBrief> TopClassrooms { get; set; } = new();
    public List<PrincipalRecentActivity> RecentActivities { get; set; } = new();
}

public class PrincipalChartPoint
{
    public string Label { get; set; } = null!;
    public int Present { get; set; }
    public int Absent { get; set; }
    public int Tardy { get; set; }
}

public class PrincipalGradeStudentCount
{
    public int GradeId { get; set; }
    public string GradeName { get; set; } = null!;
    public int StudentCount { get; set; }
    public int ClassroomCount { get; set; }
}

public class PrincipalClassroomBrief
{
    public int ClassroomId { get; set; }
    public string ClassroomName { get; set; } = null!;
    public string GradeName { get; set; } = null!;
    public int StudentCount { get; set; }
    public string? HeadTeacherName { get; set; }
    public decimal? AverageGPA { get; set; }
}

public class PrincipalRecentActivity
{
    public string Type { get; set; } = null!;     // enrollment / payment / discipline / exam
    public string Description { get; set; } = null!;
    public DateTime At { get; set; }
    public string? Link { get; set; }
}

public class PrincipalFilter
{
    public int? AcademicYearId { get; set; }
}
