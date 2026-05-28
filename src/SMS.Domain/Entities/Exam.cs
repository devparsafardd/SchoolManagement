using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SMS.Domain.Entities;

/// <summary>انواع آزمون: کلاسی، نوبت اول، پایانی، پودمانی، ...</summary>
[Table("ExamTypes")]
public class ExamType
{
    [Key]
    public int ExamTypeId { get; set; }
    [Required, MaxLength(80)] public string Name { get; set; } = null!;
    [MaxLength(30)] public string? Code { get; set; }
    public decimal DefaultWeight { get; set; } = 1;
    public bool IsFinal { get; set; }
    public bool CountsForGPA { get; set; } = true;
}

/// <summary>ترم/نوبت: نوبت اول، نوبت دوم، پودمان ۱، ...</summary>
[Table("Terms")]
public class Term
{
    [Key]
    public int TermId { get; set; }
    public int AcademicYearId { get; set; }
    [Required, MaxLength(50)] public string Name { get; set; } = null!;
    public byte OrderNo { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }

    public AcademicYear AcademicYear { get; set; } = null!;
}

/// <summary>مقیاس‌های ارزشیابی توصیفی (خیلی خوب، خوب، ...)</summary>
[Table("GradeScales")]
public class GradeScale
{
    [Key]
    public int GradeScaleId { get; set; }
    [Required, MaxLength(100)] public string Name { get; set; } = null!;
    public bool IsDescriptive { get; set; } = true;
    public bool IsActive { get; set; } = true;

    public ICollection<GradeScaleItem> Items { get; set; } = new List<GradeScaleItem>();
}

[Table("GradeScaleItems")]
public class GradeScaleItem
{
    [Key]
    public int GradeScaleItemId { get; set; }
    public int GradeScaleId { get; set; }
    [Required, MaxLength(20)] public string Symbol { get; set; } = null!;
    [Required, MaxLength(80)] public string Label { get; set; } = null!;
    public decimal? NumericEquivalent { get; set; }
    public byte OrderNo { get; set; }

    public GradeScale GradeScale { get; set; } = null!;
}

/// <summary>تعریف یک آزمون مشخص</summary>
[Table("Exams")]
public class Exam
{
    [Key]
    public long ExamId { get; set; }
    [Required, MaxLength(200)] public string Title { get; set; } = null!;

    public int ClassSubjectId { get; set; }
    public int ExamTypeId { get; set; }
    public int TermId { get; set; }
    public DateTime ExamDate { get; set; }
    public TimeSpan? StartTime { get; set; }
    public short? DurationMinutes { get; set; }
    public decimal MaxScore { get; set; } = 20;
    public decimal Weight { get; set; } = 1;
    public bool IsDescriptive { get; set; }
    public int? GradeScaleId { get; set; }

    [MaxLength(500)] public string? Description { get; set; }
    public bool IsFinalized { get; set; }
    public int CreatedByStaffId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ClassSubjectTeacher ClassSubject { get; set; } = null!;
    public ExamType ExamType { get; set; } = null!;
    public Term Term { get; set; } = null!;
    public GradeScale? GradeScale { get; set; }
    public ICollection<ExamScore> Scores { get; set; } = new List<ExamScore>();
}

/// <summary>نمره یک دانش‌آموز در یک آزمون (پشتیبانی از توصیفی و عددی)</summary>
[Table("ExamScores")]
public class ExamScore
{
    [Key]
    public long ScoreId { get; set; }
    public long ExamId { get; set; }
    public int StudentId { get; set; }

    public decimal? NumericScore { get; set; }
    public int? DescriptiveScaleItemId { get; set; }
    public bool IsAbsent { get; set; }
    public bool IsExempt { get; set; }

    [MaxLength(500)] public string? Comment { get; set; }
    public int? EnteredByStaffId { get; set; }
    public DateTime EnteredAt { get; set; } = DateTime.UtcNow;
    public DateTime? ModifiedAt { get; set; }

    public Exam Exam { get; set; } = null!;
    public Student Student { get; set; } = null!;
    public GradeScaleItem? DescriptiveScaleItem { get; set; }
}
