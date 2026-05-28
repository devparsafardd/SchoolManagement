using FluentValidation;

namespace SMS.Application.DTOs;

public class ClassroomDto
{
    public int ClassroomId { get; set; }
    public int SchoolId { get; set; }
    public string? SchoolName { get; set; }
    public int AcademicYearId { get; set; }
    public string? AcademicYearTitle { get; set; }
    public int GradeId { get; set; }
    public string? GradeName { get; set; }
    public string Name { get; set; } = null!;
    public short? Capacity { get; set; }
    public int CurrentStudentCount { get; set; }
    public int? HeadTeacherStaffId { get; set; }
    public string? HeadTeacherName { get; set; }
    public string? RoomNumber { get; set; }
    public bool IsActive { get; set; }
}

public class ClassroomCreateDto
{
    public int SchoolId { get; set; }
    public int AcademicYearId { get; set; }
    public int GradeId { get; set; }
    public int? MajorId { get; set; }
    public string Name { get; set; } = null!;
    public short? Capacity { get; set; }
    public int? HeadTeacherStaffId { get; set; }
    public string? RoomNumber { get; set; }
}

public class ClassroomCreateValidator : AbstractValidator<ClassroomCreateDto>
{
    public ClassroomCreateValidator()
    {
        RuleFor(x => x.SchoolId).GreaterThan(0);
        RuleFor(x => x.AcademicYearId).GreaterThan(0);
        RuleFor(x => x.GradeId).GreaterThan(0);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Capacity).GreaterThan((short)0).When(x => x.Capacity.HasValue);
    }
}
