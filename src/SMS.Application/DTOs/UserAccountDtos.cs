using FluentValidation;

namespace SMS.Application.DTOs;

public class CreateUserAccountDto
{
    public int PersonId { get; set; }
    public string Username { get; set; } = null!;
    public string Password { get; set; } = null!;
    public string Role { get; set; } = null!; // Student, Parent, Teacher, ...
}

public class CreateUserAccountValidator : AbstractValidator<CreateUserAccountDto>
{
    public CreateUserAccountValidator()
    {
        RuleFor(x => x.PersonId).GreaterThan(0);
        RuleFor(x => x.Username).NotEmpty().MinimumLength(4).MaximumLength(100);
        RuleFor(x => x.Password).NotEmpty().MinimumLength(6).MaximumLength(50);
        RuleFor(x => x.Role).NotEmpty();
    }
}
