namespace SMS.Application.DTOs;

// ================== گزارش حضور و غیاب ==================
public class AttendanceAnalyticsDto
{
    public int? SchoolId { get; set; }
    public string? SchoolName { get; set; }
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public int TotalRecords { get; set; }
    public int PresentCount { get; set; }
    public int AbsentCount { get; set; }
    public int TardyCount { get; set; }
    public int LeaveCount { get; set; }
    public decimal PresentRate { get; set; }
    public decimal AbsentRate { get; set; }

    public List<DailyAttendancePoint> Daily { get; set; } = new();
    public List<ClassAttendanceRow> ByClassroom { get; set; } = new();
    public List<StudentAttendanceProblemRow> TopAbsentees { get; set; } = new();
}

public class DailyAttendancePoint
{
    public DateTime Date { get; set; }
    public string Label { get; set; } = null!;
    public int Present { get; set; }
    public int Absent { get; set; }
    public int Tardy { get; set; }
}

public class ClassAttendanceRow
{
    public int ClassroomId { get; set; }
    public string ClassroomName { get; set; } = null!;
    public string GradeName { get; set; } = null!;
    public int TotalStudents { get; set; }
    public int PresentCount { get; set; }
    public int AbsentCount { get; set; }
    public int TardyCount { get; set; }
    public decimal PresentRate { get; set; }
}

public class StudentAttendanceProblemRow
{
    public int StudentId { get; set; }
    public string StudentCode { get; set; } = null!;
    public string FullName { get; set; } = null!;
    public string ClassroomName { get; set; } = null!;
    public int AbsentCount { get; set; }
    public int TardyCount { get; set; }
    public int UnexcusedCount { get; set; }
}

// ================== گزارش نمرات و عملکرد تحصیلی ==================
public class AcademicAnalyticsDto
{
    public int? SchoolId { get; set; }
    public string? SchoolName { get; set; }
    public int? TermId { get; set; }
    public string? TermName { get; set; }

    public decimal? OverallGPA { get; set; }
    public int PassedStudents { get; set; }
    public int FailedStudents { get; set; }
    public decimal PassRate { get; set; }

    public List<SubjectAveragePoint> SubjectAverages { get; set; } = new();
    public List<GradeAveragePoint> GradeAverages { get; set; } = new();
    public List<TopStudentRow> TopStudents { get; set; } = new();
    public List<TopStudentRow> WeakStudents { get; set; } = new();

    // توزیع نمرات (هیستوگرام)
    public List<ScoreDistributionBucket> ScoreDistribution { get; set; } = new();
}

public class SubjectAveragePoint
{
    public int SubjectId { get; set; }
    public string SubjectName { get; set; } = null!;
    public decimal AverageScore { get; set; }
    public int TotalScores { get; set; }
    public int PassedCount { get; set; }
}

public class GradeAveragePoint
{
    public int GradeId { get; set; }
    public string GradeName { get; set; } = null!;
    public decimal AverageGPA { get; set; }
    public int StudentCount { get; set; }
}

public class TopStudentRow
{
    public int StudentId { get; set; }
    public string FullName { get; set; } = null!;
    public string ClassroomName { get; set; } = null!;
    public string GradeName { get; set; } = null!;
    public decimal GPA { get; set; }
    public int? Rank { get; set; }
}

public class ScoreDistributionBucket
{
    public string Range { get; set; } = null!;  // "0-5", "5-10", ...
    public int Count { get; set; }
}

// ================== گزارش مالی ==================
public class FinancialAnalyticsDto
{
    public int? SchoolId { get; set; }
    public string? SchoolName { get; set; }
    public int? AcademicYearId { get; set; }
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }

    public decimal TotalInvoiced { get; set; }
    public decimal TotalPaid { get; set; }
    public decimal TotalOutstanding { get; set; }
    public decimal CollectionRate { get; set; }
    public int InvoiceCount { get; set; }
    public int OverdueInvoiceCount { get; set; }
    public decimal OverdueAmount { get; set; }

    public List<MonthlyFinancePoint> MonthlyCollection { get; set; } = new();
    public List<FeeTypeBreakdown> ByFeeType { get; set; } = new();
    public List<PaymentMethodBreakdown> ByPaymentMethod { get; set; } = new();
    public List<DebtorRow> TopDebtors { get; set; } = new();
}

public class MonthlyFinancePoint
{
    public string MonthLabel { get; set; } = null!;
    public decimal Invoiced { get; set; }
    public decimal Collected { get; set; }
}

public class FeeTypeBreakdown
{
    public string FeeTypeName { get; set; } = null!;
    public decimal Amount { get; set; }
    public int Count { get; set; }
}

public class PaymentMethodBreakdown
{
    public string Method { get; set; } = null!;
    public decimal Amount { get; set; }
    public int Count { get; set; }
}

public class DebtorRow
{
    public int StudentId { get; set; }
    public string StudentName { get; set; } = null!;
    public string ClassroomName { get; set; } = null!;
    public decimal TotalDebt { get; set; }
    public int OverdueInvoiceCount { get; set; }
}

// ================== گزارش انضباطی ==================
public class DisciplineAnalyticsDto
{
    public int? SchoolId { get; set; }
    public string? SchoolName { get; set; }
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }

    public int TotalRewards { get; set; }
    public int TotalPunishments { get; set; }

    public List<DisciplineTypeBreakdown> ByType { get; set; } = new();
    public List<MonthlyDisciplinePoint> Monthly { get; set; } = new();
    public List<StudentDisciplineRow> TopRewarded { get; set; } = new();
    public List<StudentDisciplineRow> TopPunished { get; set; } = new();
}

public class DisciplineTypeBreakdown
{
    public string TypeName { get; set; } = null!;
    public string Category { get; set; } = null!;  // R / P
    public int Count { get; set; }
}

public class MonthlyDisciplinePoint
{
    public string MonthLabel { get; set; } = null!;
    public int Rewards { get; set; }
    public int Punishments { get; set; }
}

public class StudentDisciplineRow
{
    public int StudentId { get; set; }
    public string FullName { get; set; } = null!;
    public string ClassroomName { get; set; } = null!;
    public int RecordCount { get; set; }
}
