using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SMS.Domain.Entities;

/// <summary>کارنامه ترمی دانش‌آموز (cache)</summary>
[Table("TermSubjectGrades")]
public class TermSubjectGrade
{
    [Key]
    public long TermSubjectGradeId { get; set; }
    public int StudentId { get; set; }
    public int TermId { get; set; }
    public int ClassSubjectId { get; set; }

    public decimal? FinalNumericScore { get; set; }
    public int? FinalDescriptiveItemId { get; set; }

    [MaxLength(500)] public string? TeacherComment { get; set; }
    public bool? IsPassed { get; set; }
    public DateTime CalculatedAt { get; set; } = DateTime.UtcNow;

    public Student Student { get; set; } = null!;
    public Term Term { get; set; } = null!;
    public ClassSubjectTeacher ClassSubject { get; set; } = null!;
    public GradeScaleItem? FinalDescriptiveItem { get; set; }
}

[Table("StudentTermGPA")]
public class StudentTermGPA
{
    [Key]
    public long StudentTermGPAId { get; set; }
    public int StudentId { get; set; }
    public int TermId { get; set; }
    public int ClassroomId { get; set; }

    public decimal? GPA { get; set; }
    public decimal? DisciplineScore { get; set; }
    public int? RankInClass { get; set; }
    public short? TotalAbsences { get; set; }
    public short? TotalTardies { get; set; }
    public DateTime CalculatedAt { get; set; } = DateTime.UtcNow;

    public Student Student { get; set; } = null!;
    public Term Term { get; set; } = null!;
    public Classroom Classroom { get; set; } = null!;
}
