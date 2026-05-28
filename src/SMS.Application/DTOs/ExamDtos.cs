using FluentValidation;

namespace SMS.Application.DTOs;

public class ExamDto
{
    public long ExamId { get; set; }
    public string Title { get; set; } = null!;
    public int ClassSubjectId { get; set; }
    public string? SubjectName { get; set; }
    public string? ClassroomName { get; set; }
    public string? TeacherName { get; set; }
    public int ExamTypeId { get; set; }
    public string? ExamTypeName { get; set; }
    public int TermId { get; set; }
    public string? TermName { get; set; }
    public DateTime ExamDate { get; set; }
    public decimal MaxScore { get; set; }
    public decimal Weight { get; set; }
    public bool IsDescriptive { get; set; }
    public int? GradeScaleId { get; set; }
    public bool IsFinalized { get; set; }
    public int ScoresEntered { get; set; }
    public int TotalStudents { get; set; }
}

public class ExamCreateDto
{
    public string Title { get; set; } = null!;
    public int ClassSubjectId { get; set; }
    public int ExamTypeId { get; set; }
    public int TermId { get; set; }
    public DateTime ExamDate { get; set; } = DateTime.Today;
    public short? DurationMinutes { get; set; }
    public decimal MaxScore { get; set; } = 20;
    public decimal Weight { get; set; } = 1;
    public bool IsDescriptive { get; set; }
    public int? GradeScaleId { get; set; }
    public string? Description { get; set; }
}

public class ExamScoreRow
{
    public int StudentId { get; set; }
    public string StudentCode { get; set; } = null!;
    public string FullName { get; set; } = null!;

    public long? ScoreId { get; set; }
    public decimal? NumericScore { get; set; }
    public int? DescriptiveScaleItemId { get; set; }
    public bool IsAbsent { get; set; }
    public bool IsExempt { get; set; }
    public string? Comment { get; set; }
}

public class EnterScoresDto
{
    public long ExamId { get; set; }
    public List<ExamScoreRow> Scores { get; set; } = new();
}

public class GradeScaleDto
{
    public int GradeScaleId { get; set; }
    public string Name { get; set; } = null!;
    public bool IsDescriptive { get; set; }
    public List<GradeScaleItemDto> Items { get; set; } = new();
}

public class GradeScaleItemDto
{
    public int GradeScaleItemId { get; set; }
    public string Symbol { get; set; } = null!;
    public string Label { get; set; } = null!;
    public decimal? NumericEquivalent { get; set; }
    public byte OrderNo { get; set; }
}

public class ExamCreateValidator : AbstractValidator<ExamCreateDto>
{
    public ExamCreateValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.ClassSubjectId).GreaterThan(0);
        RuleFor(x => x.ExamTypeId).GreaterThan(0);
        RuleFor(x => x.TermId).GreaterThan(0);
        RuleFor(x => x.MaxScore).GreaterThan(0).LessThanOrEqualTo(100);
        RuleFor(x => x.Weight).GreaterThan(0);
        RuleFor(x => x.GradeScaleId).NotNull().When(x => x.IsDescriptive)
            .WithMessage("برای آزمون توصیفی باید مقیاس انتخاب شود");
    }
}
