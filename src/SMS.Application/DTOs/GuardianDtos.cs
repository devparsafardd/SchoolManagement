using FluentValidation;

namespace SMS.Application.DTOs;

public class GuardianDto
{
    public int GuardianId { get; set; }
    public int PersonId { get; set; }
    public string FirstName { get; set; } = null!;
    public string LastName { get; set; } = null!;
    public string FullName => $"{FirstName} {LastName}";
    public string? NationalCode { get; set; }
    public string? Mobile { get; set; }
    public string? Occupation { get; set; }
    public string? WorkplacePhone { get; set; }
    public string? EducationLevel { get; set; }
    public int StudentCount { get; set; }

    // حساب کاربری
    public int? UserId { get; set; }
    public string? Username { get; set; }
    public bool IsUserLocked { get; set; }
}

public class StudentGuardianDto
{
    public int StudentId { get; set; }
    public int GuardianId { get; set; }
    public string GuardianName { get; set; } = null!;
    public string? Mobile { get; set; }
    public string Relationship { get; set; } = null!;
    public bool IsPrimary { get; set; }
    public bool HasCustody { get; set; }
    public bool CanPickup { get; set; }
}

public class GuardianCreateDto
{
    public string FirstName { get; set; } = null!;
    public string LastName { get; set; } = null!;
    public string? NationalCode { get; set; }
    public string Gender { get; set; } = "M";
    public string? Mobile { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public string? Occupation { get; set; }
    public string? WorkplacePhone { get; set; }
    public string? EducationLevel { get; set; }
}

public class AssignGuardianDto
{
    public int StudentId { get; set; }
    public int GuardianId { get; set; }
    public string Relationship { get; set; } = "پدر";
    public bool IsPrimary { get; set; }
    public bool HasCustody { get; set; } = true;
    public bool CanPickup { get; set; } = true;
}

public class GuardianCreateValidator : AbstractValidator<GuardianCreateDto>
{
    public GuardianCreateValidator()
    {
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Mobile).Matches(@"^09\d{9}$").When(x => !string.IsNullOrEmpty(x.Mobile))
            .WithMessage("شماره موبایل نامعتبر");
    }
}
