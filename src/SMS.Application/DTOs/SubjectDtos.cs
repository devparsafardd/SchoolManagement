using FluentValidation;

namespace SMS.Application.DTOs;

public class SubjectDto
{
    public int SubjectId { get; set; }
    public string Name { get; set; } = null!;
    public string? Code { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public int GradeCount { get; set; }
}

public class SubjectCreateDto
{
    public string Name { get; set; } = null!;
    public string? Code { get; set; }
    public string? Description { get; set; }
}

public class GradeSubjectDto
{
    public int GradeSubjectId { get; set; }
    public int GradeId { get; set; }
    public string? GradeName { get; set; }
    public int SubjectId { get; set; }
    public string? SubjectName { get; set; }
    public decimal? Credits { get; set; }
    public decimal Coefficient { get; set; } = 1;
    public decimal? WeeklyHours { get; set; }
    public bool IsDescriptive { get; set; }
    public decimal MaxScore { get; set; } = 20;
    public decimal PassingScore { get; set; } = 10;
}

public class GradeSubjectCreateDto
{
    public int GradeId { get; set; }
    public int SubjectId { get; set; }
    public decimal Coefficient { get; set; } = 1;
    public decimal? Credits { get; set; }
    public decimal? WeeklyHours { get; set; }
    public bool IsDescriptive { get; set; }
    public decimal MaxScore { get; set; } = 20;
    public decimal PassingScore { get; set; } = 10;
}

public class ClassSubjectTeacherDto
{
    public int ClassSubjectId { get; set; }
    public int ClassroomId { get; set; }
    public string? ClassroomName { get; set; }
    public int GradeSubjectId { get; set; }
    public string? SubjectName { get; set; }
    public decimal Coefficient { get; set; }
    public int StaffId { get; set; }
    public string? TeacherName { get; set; }
    public bool IsActive { get; set; }
}

public class AssignTeacherDto
{
    public int ClassroomId { get; set; }
    public int GradeSubjectId { get; set; }
    public int StaffId { get; set; }
}

public class SubjectCreateValidator : AbstractValidator<SubjectCreateDto>
{
    public SubjectCreateValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
    }
}
