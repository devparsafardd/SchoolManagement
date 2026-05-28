using FluentValidation;

namespace SMS.Application.DTOs;

public class StaffDto
{
    public int StaffId { get; set; }
    public int PersonId { get; set; }
    public string FirstName { get; set; } = null!;
    public string LastName { get; set; } = null!;
    public string FullName => $"{FirstName} {LastName}";
    public string? PersonnelCode { get; set; }
    public string? NationalCode { get; set; }
    public string Gender { get; set; } = "M";
    public string GenderText => Gender == "M" ? "آقا" : "خانم";
    public string? Mobile { get; set; }
    public string? Email { get; set; }
    public string? EmploymentType { get; set; }
    public string? Degree { get; set; }
    public string? FieldOfStudy { get; set; }
    public DateTime? HireDate { get; set; }
    public string? IBAN { get; set; }
    public bool IsActive { get; set; }
    public int AssignmentCount { get; set; }
    public List<string> Positions { get; set; } = new();
}

public class StaffCreateDto
{
    public string FirstName { get; set; } = null!;
    public string LastName { get; set; } = null!;
    public string? FatherName { get; set; }
    public string? NationalCode { get; set; }
    public string Gender { get; set; } = "M";
    public DateTime? BirthDate { get; set; }
    public string? Mobile { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }

    public string? PersonnelCode { get; set; }
    public string? EmploymentType { get; set; }
    public string? Degree { get; set; }
    public string? FieldOfStudy { get; set; }
    public DateTime? HireDate { get; set; }
    public string? IBAN { get; set; }

    // ایجاد User account؟
    public bool CreateUserAccount { get; set; }
    public string? Username { get; set; }
    public string? Password { get; set; }
}

public class StaffUpdateDto : StaffCreateDto
{
    public int StaffId { get; set; }
    public int PersonId { get; set; }
    public bool IsActive { get; set; } = true;
}

public class StaffAssignmentDto
{
    public int AssignmentId { get; set; }
    public int StaffId { get; set; }
    public string? StaffName { get; set; }
    public int SchoolId { get; set; }
    public string? SchoolName { get; set; }
    public int AcademicYearId { get; set; }
    public string? AcademicYearTitle { get; set; }
    public string Position { get; set; } = "Teacher";
    public string PositionText => Position switch
    {
        "Principal" => "مدیر",
        "VicePrincipal" => "معاون",
        "Teacher" => "معلم",
        "Counselor" => "مشاور",
        "Admin" => "کارشناس اداری",
        _ => Position
    };
    public decimal? WeeklyHours { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public bool IsActive { get; set; } = true;
}

public class StaffAssignmentCreateDto
{
    public int StaffId { get; set; }
    public int SchoolId { get; set; }
    public int AcademicYearId { get; set; }
    public string Position { get; set; } = "Teacher";
    public decimal? WeeklyHours { get; set; }
    public DateTime StartDate { get; set; } = DateTime.Now;
}

public class StaffCreateValidator : AbstractValidator<StaffCreateDto>
{
    public StaffCreateValidator()
    {
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Gender).Must(g => g is "M" or "F");
        RuleFor(x => x.NationalCode).Length(10).When(x => !string.IsNullOrEmpty(x.NationalCode));
        RuleFor(x => x.Mobile).Matches(@"^09\d{9}$").When(x => !string.IsNullOrEmpty(x.Mobile))
            .WithMessage("شماره موبایل نامعتبر");
        RuleFor(x => x.Username).NotEmpty().MinimumLength(4).When(x => x.CreateUserAccount)
            .WithMessage("نام کاربری حداقل ۴ کاراکتر");
        RuleFor(x => x.Password).NotEmpty().MinimumLength(6).When(x => x.CreateUserAccount)
            .WithMessage("رمز عبور حداقل ۶ کاراکتر");
    }
}
