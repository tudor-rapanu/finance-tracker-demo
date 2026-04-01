using FluentValidation;

namespace FinanceTracker.Application.Transactions.Commands;

public class CreateTransactionCommandValidator : AbstractValidator<CreateTransactionCommand>
{
    public CreateTransactionCommandValidator()
    {
        RuleFor(x => x.Dto.Amount)
            .GreaterThan(0).WithMessage("Amount must be greater than 0.");

        RuleFor(x => x.Dto.Currency)
            .NotEmpty().WithMessage("Currency is required.")
            .Length(3).WithMessage("Currency must be a 3-letter code.")
            .Matches("^[A-Za-z]{3}$").WithMessage("Currency must contain only letters.");

        RuleFor(x => x.Dto.Type)
            .Must(v => v is 1 or 2)
            .WithMessage("Transaction type is invalid.");

        RuleFor(x => x.Dto.Category)
            .Must((command, category) =>
                command.Dto.Type == 1
                    ? category is >= 1 and <= 5
                    : category is >= 10 and <= 19)
            .WithMessage("Transaction category is invalid for the selected type.");

        RuleFor(x => x.Dto.Description)
            .Must(s => !string.IsNullOrWhiteSpace(s))
            .WithMessage("Description is required.")
            .MaximumLength(250).WithMessage("Description cannot exceed 250 characters.");

        RuleFor(x => x.Dto.Date)
            .GreaterThanOrEqualTo(new DateTime(2000, 1, 1)).WithMessage("Transaction date is too old.")
            .LessThanOrEqualTo(_ => DateTime.UtcNow.Date.AddDays(1)).WithMessage("Transaction date cannot be in the far future.");

        RuleFor(x => x.Dto.Notes)
            .MaximumLength(1000).When(x => x.Dto.Notes is not null)
            .WithMessage("Notes cannot exceed 1000 characters.");
    }
}

public class UpdateTransactionCommandValidator : AbstractValidator<UpdateTransactionCommand>
{
    public UpdateTransactionCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Transaction ID is required.");

        RuleFor(x => x.Dto.Amount)
            .GreaterThan(0).WithMessage("Amount must be greater than 0.");

        RuleFor(x => x.Dto.Currency)
            .NotEmpty().WithMessage("Currency is required.")
            .Length(3).WithMessage("Currency must be a 3-letter code.")
            .Matches("^[A-Za-z]{3}$").WithMessage("Currency must contain only letters.");

        RuleFor(x => x.Dto.Type)
            .Must(v => v is 1 or 2)
            .WithMessage("Transaction type is invalid.");

        RuleFor(x => x.Dto.Category)
            .Must((command, category) =>
                command.Dto.Type == 1
                    ? category is >= 1 and <= 5
                    : category is >= 10 and <= 19)
            .WithMessage("Transaction category is invalid for the selected type.");

        RuleFor(x => x.Dto.Description)
            .Must(s => !string.IsNullOrWhiteSpace(s))
            .WithMessage("Description is required.")
            .MaximumLength(250).WithMessage("Description cannot exceed 250 characters.");

        RuleFor(x => x.Dto.Date)
            .GreaterThanOrEqualTo(new DateTime(2000, 1, 1)).WithMessage("Transaction date is too old.")
            .LessThanOrEqualTo(_ => DateTime.UtcNow.Date.AddDays(1)).WithMessage("Transaction date cannot be in the far future.");

        RuleFor(x => x.Dto.Notes)
            .MaximumLength(1000).When(x => x.Dto.Notes is not null)
            .WithMessage("Notes cannot exceed 1000 characters.");
    }
}