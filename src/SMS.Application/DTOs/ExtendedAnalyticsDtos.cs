namespace SMS.Application.DTOs;

// ====== گزارش جامع یک کلاس ======
public class ClassroomAnalyticsDto
{
    public int ClassroomId { get; set; }
    public string ClassroomName { get; set; } = null!;
    public string GradeName { get; set; } = null!;
    public string SchoolName { get; set; } = null!;
    public int TotalStudents { get; set; }
    public int MaleStudents { get; set; }
    public int FemaleStudents { get; set; }
    public decimal? ClassAverage { get; set; }
    public int TotalAbsences { get; set; }
    public int TotalTardies { get; set; }
    public int TotalRewards { get; set; }
    public int TotalPunishments { get; set; }
    public List<SubjectAveragePoint> SubjectAverages { get; set; } = new();
    public List<StudentRankRow> StudentRanking { get; set; } = new();
    public List<DailyAttendancePoint> AttendanceTrend { get; set; } = new();
}

public class StudentRankRow
{
    public int StudentId { get; set; }
    public string StudentCode { get; set; } = null!;
    public string FullName { get; set; } = null!;
    public decimal? Average { get; set; }
    public int? Rank { get; set; }
    public int AbsenceCount { get; set; }
    public int TardyCount { get; set; }
    public int RewardsCount { get; set; }
    public int PunishmentsCount { get; set; }
}

// ====== گزارش جامع یک معلم ======
public class TeacherAnalyticsDto
{
    public int StaffId { get; set; }
    public string TeacherName { get; set; } = null!;
    public string? PersonnelCode { get; set; }
    public int TotalClasses { get; set; }
    public int TotalStudents { get; set; }
    public int TotalExams { get; set; }
    public int TotalScoredExams { get; set; }
    public decimal? OverallAverage { get; set; }
    public int AttendanceRecordsTaken { get; set; }
    public int HomeworksAssigned { get; set; }
    public int HomeworksGraded { get; set; }
    public List<TeacherClassPerformance> Classes { get; set; } = new();
}

public class TeacherClassPerformance
{
    public int ClassSubjectId { get; set; }
    public string ClassroomName { get; set; } = null!;
    public string SubjectName { get; set; } = null!;
    public int StudentCount { get; set; }
    public decimal? Average { get; set; }
    public int ExamCount { get; set; }
    public int PassRate { get; set; }
}

// ====== گزارش جامع یک دانش‌آموز ======
public class StudentAnalyticsDto
{
    public int StudentId { get; set; }
    public string StudentCode { get; set; } = null!;
    public string FullName { get; set; } = null!;
    public string? FatherName { get; set; }
    public string ClassroomName { get; set; } = null!;
    public string SchoolName { get; set; } = null!;
    public decimal? OverallAverage { get; set; }
    public int? RankInClass { get; set; }
    public int TotalAbsences { get; set; }
    public int TotalTardies { get; set; }
    public int RewardsCount { get; set; }
    public int PunishmentsCount { get; set; }
    public decimal TotalDebt { get; set; }
    public decimal TotalPaid { get; set; }
    public List<SubjectAveragePoint> SubjectScores { get; set; } = new();
    public List<DailyAttendancePoint> AttendanceLast30 { get; set; } = new();
    public List<ExamScoreItem> RecentExams { get; set; } = new();
}

public class ExamScoreItem
{
    public string Title { get; set; } = null!;
    public string SubjectName { get; set; } = null!;
    public DateTime Date { get; set; }
    public decimal? Score { get; set; }
    public decimal MaxScore { get; set; }
    public string? DescriptiveLabel { get; set; }
}
