using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SMS.Domain.Entities;

/// <summary>کاتالوگ دروس کلی (ریاضی، علوم، فارسی، ...)</summary>
[Table("Subjects")]
public class Subject
{
    [Key]
    public int SubjectId { get; set; }

    [Required, MaxLength(150)]
    public string Name { get; set; } = null!;

    [MaxLength(30)]
    public string? Code { get; set; }

    [MaxLength(500)]
    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;
}

/// <summary>ارائه درس در یک پایه (ریاضی پایه ششم = ۴ ساعت در هفته، ضریب ۲)</summary>
[Table("GradeSubjects")]
public class GradeSubject
{
    [Key]
    public int GradeSubjectId { get; set; }
    public int GradeId { get; set; }
    public int SubjectId { get; set; }

    public decimal? Credits { get; set; }
    public decimal Coefficient { get; set; } = 1;
    public decimal? WeeklyHours { get; set; }

    /// <summary>توصیفی یا عددی</summary>
    public bool IsDescriptive { get; set; }

    public decimal MaxScore { get; set; } = 20;
    public decimal PassingScore { get; set; } = 10;

    public Grade Grade { get; set; } = null!;
    public Subject Subject { get; set; } = null!;
}

/// <summary>تخصیص معلم به یک درس در یک کلاس</summary>
[Table("ClassSubjectTeachers")]
public class ClassSubjectTeacher
{
    [Key]
    public int ClassSubjectId { get; set; }
    public int ClassroomId { get; set; }
    public int GradeSubjectId { get; set; }
    public int StaffId { get; set; }

    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public bool IsActive { get; set; } = true;

    public Classroom Classroom { get; set; } = null!;
    public GradeSubject GradeSubject { get; set; } = null!;
    public Staff Staff { get; set; } = null!;
}
