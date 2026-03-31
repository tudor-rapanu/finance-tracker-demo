using FluentValidation;

namespace FinanceTracker.Application.Auth.Commands;

public class RegisterCommandValidator : AbstractValidator<RegisterCommand>
{
    public RegisterCommandValidator()
    {
        RuleFor(x => x.Dto.FirstName)
            .NotEmpty().WithMessage("First name is required.")
            .MaximumLength(100).WithMessage("First name cannot exceed 100 characters.");

        RuleFor(x => x.Dto.LastName)
            .NotEmpty().WithMessage("Last name is required.")
            .MaximumLength(100).WithMessage("Last name cannot exceed 100 characters.");

        RuleFor(x => x.Dto.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Email format is invalid.")
            .MaximumLength(254).WithMessage("Email cannot exceed 254 characters.");

        RuleFor(x => x.Dto.Password)
            .NotEmpty().WithMessage("Password is required.")
            .MinimumLength(8).WithMessage("Password must be at least 8 characters long.")
            .Matches("[A-Z]").WithMessage("Password must contain at least one uppercase letter.")
            .Matches("[0-9]").WithMessage("Password must contain at least one digit.");

        RuleFor(x => x.Dto.PreferredCurrency)
            .NotEmpty().WithMessage("Preferred currency is required.")
            .Length(3).WithMessage("Preferred currency must be a 3-letter code.")
            .Matches("^[A-Za-z]{3}$").WithMessage("Preferred currency must contain only letters.");
    }
}

public class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(x => x.Dto.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Email format is invalid.")
            .MaximumLength(254).WithMessage("Email cannot exceed 254 characters.");

        RuleFor(x => x.Dto.Password)
            .NotEmpty().WithMessage("Password is required.");
    }
}

public class UpdatePreferredCurrencyCommandValidator : AbstractValidator<UpdatePreferredCurrencyCommand>
{
    public UpdatePreferredCurrencyCommandValidator()
    {
        RuleFor(x => x.Dto.PreferredCurrency)
            .NotEmpty().WithMessage("Preferred currency is required.")
            .Length(3).WithMessage("Preferred currency must be a 3-letter code.")
            .Matches("^[A-Za-z]{3}$").WithMessage("Preferred currency must contain only letters.");
    }
}