namespace SMS.Application.DTOs;

public class HomeworkDto
{
    public long HomeworkId { get; set; }
    public int ClassSubjectId { get; set; }
    public string Title { get; set; } = null!;
    public string? Description { get; set; }
    public DateTime AssignedDate { get; set; }
    public DateTime DueDate { get; set; }
    public decimal? MaxScore { get; set; }
    public string? AttachmentPath { get; set; }
    public bool AllowFileSubmission { get; set; }
    public bool IsActive { get; set; }

    public string? SubjectName { get; set; }
    public string? ClassroomName { get; set; }
    public string? CreatedByName { get; set; }
    public int SubmissionsCount { get; set; }
    public int GradedCount { get; set; }
    public int TotalStudents { get; set; }
    public bool IsOverdue => DateTime.Today > DueDate.Date;
}

public class HomeworkCreateDto
{
    public int ClassSubjectId { get; set; }
    public string Title { get; set; } = null!;
    public string? Description { get; set; }
    public DateTime DueDate { get; set; } = DateTime.Today.AddDays(7);
    public decimal? MaxScore { get; set; }
    public string? AttachmentPath { get; set; }
    public bool AllowFileSubmission { get; set; } = true;
}

public class HomeworkSubmissionDto
{
    public long SubmissionId { get; set; }
    public long HomeworkId { get; set; }
    public int StudentId { get; set; }
    public string StudentCode { get; set; } = null!;
    public string FullName { get; set; } = null!;
    public DateTime SubmittedAt { get; set; }
    public bool IsLate { get; set; }
    public string? TextAnswer { get; set; }
    public string? AttachmentPath { get; set; }
    public decimal? Score { get; set; }
    public string? TeacherFeedback { get; set; }
    public bool IsGraded => Score.HasValue;
}

public class HomeworkSubmitDto
{
    public long HomeworkId { get; set; }
    public int StudentId { get; set; }
    public string? TextAnswer { get; set; }
    public string? AttachmentPath { get; set; }
}

public class HomeworkGradeDto
{
    public long SubmissionId { get; set; }
    public decimal Score { get; set; }
    public string? TeacherFeedback { get; set; }
}
