using FluentValidation;

namespace FinanceTracker.Application.Budgets.Commands;

public class CreateBudgetCommandValidator : AbstractValidator<CreateBudgetCommand>
{
    public CreateBudgetCommandValidator()
    {
        RuleFor(x => x.Dto.Category)
            .InclusiveBetween(10, 19)
            .WithMessage("Budget category must be an expense category.");

        RuleFor(x => x.Dto.LimitAmount)
            .GreaterThan(0)
            .WithMessage("Budget limit must be greater than 0.");

        RuleFor(x => x.Dto.Currency)
            .NotEmpty().WithMessage("Currency is required.")
            .Length(3).WithMessage("Currency must be a 3-letter code.")
            .Matches("^[A-Za-z]{3}$").WithMessage("Currency must contain only letters.");

        RuleFor(x => x.Dto.Month)
            .InclusiveBetween(1, 12)
            .WithMessage("Month must be between 1 and 12.");

        RuleFor(x => x.Dto.Year)
            .InclusiveBetween(2000, 2100)
            .WithMessage("Year must be between 2000 and 2100.");
    }
}