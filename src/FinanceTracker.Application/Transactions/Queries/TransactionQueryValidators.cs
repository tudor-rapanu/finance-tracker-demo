using FluentValidation;

namespace FinanceTracker.Application.Transactions.Queries;

public class GetTransactionsQueryValidator : AbstractValidator<GetTransactionsQuery>
{
    public GetTransactionsQueryValidator()
    {
        RuleFor(x => x.Month)
            .Must(m => m is null || (m >= 1 && m <= 12))
            .WithMessage("Month must be between 1 and 12.");

        RuleFor(x => x.Year)
            .Must(y => y is null || (y >= 2000 && y <= 2100))
            .WithMessage("Year must be between 2000 and 2100.");
    }
}

public class GetDashboardQueryValidator : AbstractValidator<GetDashboardQuery>
{
    public GetDashboardQueryValidator()
    {
        RuleFor(x => x.Month)
            .InclusiveBetween(1, 12)
            .WithMessage("Month must be between 1 and 12.");

        RuleFor(x => x.Year)
            .InclusiveBetween(2000, 2100)
            .WithMessage("Year must be between 2000 and 2100.");
    }
}