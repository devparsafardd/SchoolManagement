namespace SMS.Application.DTOs;

public class SurveyDto
{
    public int SurveyId { get; set; }
    public string Title { get; set; } = null!;
    public string? Description { get; set; }
    public string TargetAudience { get; set; } = "All";
    public int? SchoolId { get; set; }
    public int? ClassroomId { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public bool IsAnonymous { get; set; }
    public bool IsActive { get; set; }
    public string? CreatedByName { get; set; }
    public int QuestionCount { get; set; }
    public int ResponseCount { get; set; }
    public bool IsExpired => DateTime.Today > EndDate.Date;
}

public class SurveyCreateDto
{
    public string Title { get; set; } = null!;
    public string? Description { get; set; }
    public string TargetAudience { get; set; } = "All";
    public int? SchoolId { get; set; }
    public int? ClassroomId { get; set; }
    public DateTime StartDate { get; set; } = DateTime.Today;
    public DateTime EndDate { get; set; } = DateTime.Today.AddDays(7);
    public bool IsAnonymous { get; set; }
    public List<SurveyQuestionDto> Questions { get; set; } = new();
}

public class SurveyQuestionDto
{
    public int QuestionId { get; set; }
    public int SurveyId { get; set; }
    public string Text { get; set; } = null!;
    public string QuestionType { get; set; } = "Text"; // Text/SingleChoice/MultipleChoice/Rating/YesNo
    public string? Options { get; set; }              // با | جدا
    public List<string> OptionsList => string.IsNullOrEmpty(Options) ? new() : Options.Split('|').ToList();
    public bool IsRequired { get; set; } = true;
    public int OrderNo { get; set; }
}

public class SurveySubmitDto
{
    public int SurveyId { get; set; }
    public List<SurveyAnswerDto> Answers { get; set; } = new();
}

public class SurveyAnswerDto
{
    public int QuestionId { get; set; }
    public string? AnswerText { get; set; }
}

public class SurveyResultDto
{
    public int SurveyId { get; set; }
    public string Title { get; set; } = null!;
    public int TotalResponses { get; set; }
    public List<SurveyQuestionResultDto> Questions { get; set; } = new();
}

public class SurveyQuestionResultDto
{
    public int QuestionId { get; set; }
    public string Text { get; set; } = null!;
    public string QuestionType { get; set; } = null!;
    public List<SurveyAnswerSummary> Summary { get; set; } = new();
    public List<string> TextAnswers { get; set; } = new();
}

public class SurveyAnswerSummary
{
    public string Option { get; set; } = null!;
    public int Count { get; set; }
    public decimal Percent { get; set; }
}
