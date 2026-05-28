using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SMS.Domain.Entities;

/// <summary>تکلیف توسط معلم برای یک کلاس درسی</summary>
[Table("Homeworks")]
public class Homework
{
    [Key]
    public long HomeworkId { get; set; }
    public int ClassSubjectId { get; set; }
    public int CreatedByStaffId { get; set; }

    [Required, MaxLength(300)]
    public string Title { get; set; } = null!;

    [MaxLength(4000)]
    public string? Description { get; set; }

    public DateTime AssignedDate { get; set; } = DateTime.Today;
    public DateTime DueDate { get; set; }

    public decimal? MaxScore { get; set; }

    [MaxLength(500)]
    public string? AttachmentPath { get; set; }    // فایل ضمیمه معلم

    public bool AllowFileSubmission { get; set; } = true;
    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ClassSubjectTeacher ClassSubject { get; set; } = null!;
    public Staff CreatedBy { get; set; } = null!;
    public ICollection<HomeworkSubmission> Submissions { get; set; } = new List<HomeworkSubmission>();
}

/// <summary>تحویل تکلیف توسط دانش‌آموز</summary>
[Table("HomeworkSubmissions")]
public class HomeworkSubmission
{
    [Key]
    public long SubmissionId { get; set; }
    public long HomeworkId { get; set; }
    public int StudentId { get; set; }

    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
    public bool IsLate { get; set; }

    [MaxLength(4000)]
    public string? TextAnswer { get; set; }

    [MaxLength(500)]
    public string? AttachmentPath { get; set; }

    // تصحیح معلم
    public decimal? Score { get; set; }

    [MaxLength(1000)]
    public string? TeacherFeedback { get; set; }

    public int? GradedByStaffId { get; set; }
    public DateTime? GradedAt { get; set; }

    public Homework Homework { get; set; } = null!;
    public Student Student { get; set; } = null!;
}
