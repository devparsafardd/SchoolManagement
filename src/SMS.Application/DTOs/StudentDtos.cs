using FluentValidation;

namespace SMS.Application.DTOs;

public class StudentDto
{
    public int StudentId { get; set; }
    public int PersonId { get; set; }
    public string StudentCode { get; set; } = null!;
    public string FirstName { get; set; } = null!;
    public string LastName { get; set; } = null!;
    public string FullName => $"{FirstName} {LastName}";
    public string? FatherName { get; set; }
    public string? NationalCode { get; set; }
    public string Gender { get; set; } = "M";
    public DateTime? BirthDate { get; set; }
    public string? Mobile { get; set; }
    public string? Address { get; set; }
    public string? BloodType { get; set; }
    public string? CurrentClassName { get; set; }
    public string? CurrentSchoolName { get; set; }
    public bool IsActive { get; set; }

    // اطلاعات حساب کاربری (اگر دارد)
    public int? UserId { get; set; }
    public string? Username { get; set; }
    public bool IsUserLocked { get; set; }
}

public class StudentCreateDto
{
    public string FirstName { get; set; } = null!;
    public string LastName { get; set; } = null!;
    public string? FatherName { get; set; }
    public string? NationalCode { get; set; }
    public string Gender { get; set; } = "M";
    public DateTime? BirthDate { get; set; }
    public string? Mobile { get; set; }
    public string? Address { get; set; }

    public string StudentCode { get; set; } = null!;
    public string? BloodType { get; set; }
    public string? SpecialNeeds { get; set; }

    public int? ClassroomId { get; set; }
}

public class StudentUpdateDto : StudentCreateDto
{
    public int StudentId { get; set; }
    public int PersonId { get; set; }
    public bool IsActive { get; set; } = true;
}

public class StudentCreateValidator : AbstractValidator<StudentCreateDto>
{
    public StudentCreateValidator()
    {
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.StudentCode).NotEmpty().MaximumLength(30);
        RuleFor(x => x.Gender).Must(g => g is "M" or "F");
        RuleFor(x => x.NationalCode)
            .Length(10).When(x => !string.IsNullOrEmpty(x.NationalCode))
            .WithMessage("کد ملی باید ۱۰ رقم باشد");
        RuleFor(x => x.Mobile)
            .Matches(@"^09\d{9}$").When(x => !string.IsNullOrEmpty(x.Mobile))
            .WithMessage("شماره موبایل نامعتبر است");
    }
}
