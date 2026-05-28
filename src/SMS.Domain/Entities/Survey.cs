using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SMS.Domain.Entities;

/// <summary>نظرسنجی/فرم سیستم</summary>
[Table("Surveys")]
public class Survey
{
    [Key]
    public int SurveyId { get; set; }

    [Required, MaxLength(200)]
    public string Title { get; set; } = null!;

    [MaxLength(1000)]
    public string? Description { get; set; }

    /// <summary>All / Students / Parents / Teachers</summary>
    [Required, MaxLength(30)]
    public string TargetAudience { get; set; } = "All";

    public int? SchoolId { get; set; }
    public int? ClassroomId { get; set; }

    public DateTime StartDate { get; set; } = DateTime.Today;
    public DateTime EndDate { get; set; }

    public bool IsAnonymous { get; set; }
    public bool IsActive { get; set; } = true;

    public int CreatedByUserId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public User CreatedBy { get; set; } = null!;
    public ICollection<SurveyQuestion> Questions { get; set; } = new List<SurveyQuestion>();
}

[Table("SurveyQuestions")]
public class SurveyQuestion
{
    [Key]
    public int QuestionId { get; set; }
    public int SurveyId { get; set; }

    [Required, MaxLength(500)]
    public string Text { get; set; } = null!;

    /// <summary>Text / SingleChoice / MultipleChoice / Rating / YesNo</summary>
    [Required, MaxLength(20)]
    public string QuestionType { get; set; } = "Text";

    /// <summary>گزینه‌ها برای SingleChoice/MultipleChoice (با | از هم جدا)</summary>
    [MaxLength(2000)]
    public string? Options { get; set; }

    public bool IsRequired { get; set; } = true;
    public int OrderNo { get; set; }

    public Survey Survey { get; set; } = null!;
}

[Table("SurveyAnswers")]
public class SurveyAnswer
{
    [Key]
    public long AnswerId { get; set; }
    public int SurveyId { get; set; }
    public int QuestionId { get; set; }
    public int UserId { get; set; }

    [MaxLength(4000)]
    public string? AnswerText { get; set; }

    public DateTime AnsweredAt { get; set; } = DateTime.UtcNow;

    public Survey Survey { get; set; } = null!;
    public SurveyQuestion Question { get; set; } = null!;
    public User User { get; set; } = null!;
}
