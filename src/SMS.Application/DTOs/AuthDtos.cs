using FluentValidation;

namespace SMS.Application.DTOs;

public class LoginDto
{
    public string Username { get; set; } = null!;
    public string Password { get; set; } = null!;
}

public class LoginResultDto
{
    public int UserId { get; set; }
    public int PersonId { get; set; }
    public string Token { get; set; } = null!;
    public string Username { get; set; } = null!;
    public string FullName { get; set; } = null!;
    public List<string> Roles { get; set; } = new();
    public DateTime ExpiresAt { get; set; }
}

public class LoginDtoValidator : AbstractValidator<LoginDto>
{
    public LoginDtoValidator()
    {
        RuleFor(x => x.Username).NotEmpty().WithMessage("نام کاربری الزامی است");
        RuleFor(x => x.Password).NotEmpty().MinimumLength(4).WithMessage("رمز عبور الزامی است");
    }
}
