namespace SMS.Application.DTOs;

/// <summary>کارنامه ترمی دانش‌آموز</summary>
public class StudentReportCardDto
{
    public int StudentId { get; set; }
    public string StudentCode { get; set; } = null!;
    public string FullName { get; set; } = null!;
    public string? FatherName { get; set; }
    public string? NationalCode { get; set; }
    public string SchoolName { get; set; } = null!;
    public string ClassroomName { get; set; } = null!;
    public string GradeName { get; set; } = null!;
    public string AcademicYearTitle { get; set; } = null!;
    public string TermName { get; set; } = null!;

    public List<ReportCardSubjectRow> Subjects { get; set; } = new();

    public decimal? GPA { get; set; }
    public decimal? DisciplineScore { get; set; }
    public int? RankInClass { get; set; }
    public int TotalAbsences { get; set; }
    public int TotalTardies { get; set; }
    public int UnexcusedAbsences { get; set; }

    public int PassedCount => Subjects.Count(s => s.IsPassed == true);
    public int FailedCount => Subjects.Count(s => s.IsPassed == false);

    public string? GeneralComment { get; set; }
}

public class ReportCardSubjectRow
{
    public string SubjectName { get; set; } = null!;
    public decimal Coefficient { get; set; }
    public bool IsDescriptive { get; set; }
    public decimal? NumericScore { get; set; }
    public string? DescriptiveLabel { get; set; }
    public bool? IsPassed { get; set; }
    public string? TeacherName { get; set; }
    public string? TeacherComment { get; set; }
}

public class ClassroomGradesReportDto
{
    public string ClassroomName { get; set; } = null!;
    public string TermName { get; set; } = null!;
    public List<ReportCardSubjectRow> Subjects { get; set; } = new();
    public List<StudentClassReportRow> Students { get; set; } = new();
}

public class StudentClassReportRow
{
    public int StudentId { get; set; }
    public string StudentCode { get; set; } = null!;
    public string FullName { get; set; } = null!;
    public Dictionary<string, decimal?> SubjectScores { get; set; } = new();
    public decimal? GPA { get; set; }
    public int? Rank { get; set; }
}

public class AuditLogDto
{
    public long AuditId { get; set; }
    public int? UserId { get; set; }
    public string? Username { get; set; }
    public string Action { get; set; } = null!;
    public string EntityName { get; set; } = null!;
    public string? EntityId { get; set; }
    public string? ChangedColumns { get; set; }
    public string? IpAddress { get; set; }
    public string? OldValues { get; set; }
    public string? NewValues { get; set; }
    public DateTime CreatedAt { get; set; }
}
