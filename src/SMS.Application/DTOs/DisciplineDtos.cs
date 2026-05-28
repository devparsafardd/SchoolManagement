using FluentValidation;

namespace SMS.Application.DTOs;

public class DisciplinaryRecordDto
{
    public long RecordId { get; set; }
    public int StudentId { get; set; }
    public string? StudentName { get; set; }
    public string? StudentCode { get; set; }
    public int ClassroomId { get; set; }
    public string? ClassroomName { get; set; }
    public int TypeId { get; set; }
    public string? TypeName { get; set; }
    public string Category { get; set; } = "R";
    public string CategoryText => Category == "R" ? "تشویق" : "تنبیه";
    public DateTime RecordDate { get; set; }
    public string Description { get; set; } = null!;
    public string? ActionTaken { get; set; }
    public decimal? ScoreImpact { get; set; }
    public bool IsParentNotified { get; set; }
    public string? RecordedByName { get; set; }
}

public class DisciplinaryRecordCreateDto
{
    public int StudentId { get; set; }
    public int TypeId { get; set; }
    public DateTime RecordDate { get; set; } = DateTime.Today;
    public string Description { get; set; } = null!;
    public string? ActionTaken { get; set; }
    public decimal? ScoreImpact { get; set; }
    public bool NotifyParent { get; set; }
}

public class DisciplinaryTypeDto
{
    public int TypeId { get; set; }
    public string Name { get; set; } = null!;
    public string Category { get; set; } = "R";
    public byte? Severity { get; set; }
    public decimal? DefaultScoreImpact { get; set; }
    public bool IsActive { get; set; } = true;
}

public class DisciplinaryRecordValidator : AbstractValidator<DisciplinaryRecordCreateDto>
{
    public DisciplinaryRecordValidator()
    {
        RuleFor(x => x.StudentId).GreaterThan(0);
        RuleFor(x => x.TypeId).GreaterThan(0);
        RuleFor(x => x.Description).NotEmpty().MaximumLength(1000);
    }
}
