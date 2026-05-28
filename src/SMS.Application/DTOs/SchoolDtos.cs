using FluentValidation;

namespace SMS.Application.DTOs;

public class SchoolDto
{
    public int SchoolId { get; set; }
    public string Name { get; set; } = null!;
    public string Code { get; set; } = null!;
    public int CityId { get; set; }
    public string? CityName { get; set; }
    public string? ProvinceName { get; set; }
    public string Gender { get; set; } = "B";
    public string? GenderText => Gender switch { "M" => "پسرانه", "F" => "دخترانه", _ => "مختلط" };
    public string? SchoolType { get; set; }
    public int EducationLevelId { get; set; }
    public string? EducationLevelName { get; set; }
    public string? Address { get; set; }
    public string? Phone { get; set; }
    public bool IsActive { get; set; }
    public int ClassroomCount { get; set; }
    public int StudentCount { get; set; }
}

public class SchoolCreateDto
{
    public string Name { get; set; } = null!;
    public string Code { get; set; } = null!;
    public int CityId { get; set; }
    public string Gender { get; set; } = "B";
    public string? SchoolType { get; set; }
    public int EducationLevelId { get; set; }
    public string? Address { get; set; }
    public string? Phone { get; set; }
}

public class SchoolUpdateDto : SchoolCreateDto
{
    public int SchoolId { get; set; }
    public bool IsActive { get; set; } = true;
}

public class SchoolCreateValidator : AbstractValidator<SchoolCreateDto>
{
    public SchoolCreateValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200).WithMessage("نام مدرسه الزامی است");
        RuleFor(x => x.Code).NotEmpty().MaximumLength(50).WithMessage("کد مدرسه الزامی است");
        RuleFor(x => x.CityId).GreaterThan(0).WithMessage("شهر را انتخاب کنید");
        RuleFor(x => x.EducationLevelId).GreaterThan(0).WithMessage("مقطع را انتخاب کنید");
        RuleFor(x => x.Gender).Must(g => g is "M" or "F" or "B").WithMessage("جنسیت نامعتبر");
    }
}
