using FluentValidation;

namespace SMS.Application.DTOs;

public class ChangePasswordDto
{
    public string CurrentPassword { get; set; } = null!;
    public string NewPassword { get; set; } = null!;
    public string ConfirmPassword { get; set; } = null!;
}

public class ChangePasswordValidator : AbstractValidator<ChangePasswordDto>
{
    public ChangePasswordValidator()
    {
        RuleFor(x => x.CurrentPassword).NotEmpty().WithMessage("رمز فعلی الزامی است");
        RuleFor(x => x.NewPassword)
            .NotEmpty().WithMessage("رمز جدید الزامی است")
            .MinimumLength(6).WithMessage("رمز جدید حداقل ۶ کاراکتر باشد")
            .Matches(@"[A-Za-z]").WithMessage("رمز جدید باید حداقل یک حرف داشته باشد")
            .Matches(@"\d").WithMessage("رمز جدید باید حداقل یک رقم داشته باشد");
        RuleFor(x => x.ConfirmPassword)
            .Equal(x => x.NewPassword).WithMessage("تکرار رمز با رمز جدید مطابقت ندارد");
    }
}

public class ProfileDto
{
    public int PersonId { get; set; }
    public string FirstName { get; set; } = null!;
    public string LastName { get; set; } = null!;
    public string FullName => $"{FirstName} {LastName}";
    public string? Mobile { get; set; }
    public string? Email { get; set; }
    public string Username { get; set; } = null!;
    public string? PhotoPath { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public List<string> Roles { get; set; } = new();
}
