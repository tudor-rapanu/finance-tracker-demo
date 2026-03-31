using FluentValidation;

namespace FinanceTracker.Application.Admin.Commands;

public class SetUserRoleCommandValidator : AbstractValidator<SetUserRoleCommand>
{
    public SetUserRoleCommandValidator()
    {
        RuleFor(x => x.Dto.UserId)
            .Must(s => !string.IsNullOrWhiteSpace(s))
            .WithMessage("User id is required.")
            .MaximumLength(128)
            .WithMessage("User id is too long.");

        RuleFor(x => x.Dto.Role)
            .Must(s => !string.IsNullOrWhiteSpace(s))
            .WithMessage("Role is required.")
            .Must(role => role.Equals("Admin", StringComparison.OrdinalIgnoreCase)
                       || role.Equals("User", StringComparison.OrdinalIgnoreCase))
            .WithMessage("Role must be either 'Admin' or 'User'.");
    }
}