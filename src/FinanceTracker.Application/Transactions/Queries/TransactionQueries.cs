using FinanceTracker.Application.Common;
using FinanceTracker.Contracts;
using FinanceTracker.Application.Interfaces;
using FinanceTracker.Domain.Enums;
using FinanceTracker.Domain.Interfaces;
using MediatR;

namespace FinanceTracker.Application.Transactions.Queries;

// --- Get Transactions ---
public record GetTransactionsQuery(int? Month = null, int? Year = null) : IRequest<Result<IEnumerable<TransactionDto>>>;

public class GetTransactionsQueryHandler : IRequestHandler<GetTransactionsQuery, Result<IEnumerable<TransactionDto>>>
{
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUserService _currentUser;

    public GetTransactionsQueryHandler(IUnitOfWork uow, ICurrentUserService currentUser)
    {
        _uow = uow;
        _currentUser = currentUser;
    }

    public async Task<Result<IEnumerable<TransactionDto>>> Handle(GetTransactionsQuery request, CancellationToken ct)
    {
        if (_currentUser.UserId is null)
            return Result<IEnumerable<TransactionDto>>.Failure("User not authenticated.");

        var transactions = await _uow.Transactions.GetByUserIdAsync(_currentUser.UserId, request.Month, request.Year);

        var dtos = transactions.Select(t =>
            new TransactionDto(t.Id, t.Amount, t.Currency, t.AmountInBaseCurrency, (int)t.Type, (int)t.Category, t.Description, t.Date, t.Notes));

        return Result<IEnumerable<TransactionDto>>.Success(dtos);
    }
}

// --- Get Dashboard ---
public record GetDashboardQuery(int Month, int Year) : IRequest<Result<DashboardDto>>;

public class GetDashboardQueryHandler : IRequestHandler<GetDashboardQuery, Result<DashboardDto>>
{
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUserService _currentUser;
    private readonly IExchangeRateService _exchangeRateService;

    public GetDashboardQueryHandler(
        IUnitOfWork uow,
        ICurrentUserService currentUser,
        IExchangeRateService exchangeRateService)
    {
        _uow = uow;
        _currentUser = currentUser;
        _exchangeRateService = exchangeRateService;
    }

    public async Task<Result<DashboardDto>> Handle(GetDashboardQuery request, CancellationToken ct)
    {
        if (_currentUser.UserId is null)
            return Result<DashboardDto>.Failure("User not authenticated.");

        var userId = _currentUser.UserId;
        var preferredCurrency = _currentUser.PreferredCurrency ?? "USD";

        var income = await _uow.Transactions.GetTotalByTypeAndPeriodAsync(userId, TransactionType.Income, request.Month, request.Year);
        var expenses = await _uow.Transactions.GetTotalByTypeAndPeriodAsync(userId, TransactionType.Expense, request.Month, request.Year);
        var spendingByCategory = await _uow.Transactions.GetSpendingByCategoryAsync(userId, request.Month, request.Year);
        var budgets = await _uow.Budgets.GetByUserAndPeriodAsync(userId, request.Month, request.Year);

        var topCategories = spendingByCategory
            .OrderByDescending(kv => kv.Value)
            .Take(5)
            .Select(kv => new CategorySpendingDto(
                (int)kv.Key,
                kv.Value,
                expenses > 0 ? (double)(kv.Value / expenses * 100) : 0))
            .ToList();

        var budgetDtos = new List<BudgetDto>();
        foreach (var b in budgets)
        {
            var limitInPreferred = await _exchangeRateService.ConvertAsync(b.LimitAmount, b.Currency, preferredCurrency);
            var spentInPreferred = spendingByCategory.GetValueOrDefault(b.Category, 0);

            var remainingInPreferred = limitInPreferred - spentInPreferred;
            var pct = limitInPreferred > 0 ? (double)(spentInPreferred / limitInPreferred * 100) : 0;

            budgetDtos.Add(new BudgetDto(
                b.Id,
                (int)b.Category,
                limitInPreferred,
                preferredCurrency,
                b.Month,
                b.Year,
                spentInPreferred,
                remainingInPreferred,
                pct));
        }

        var dashboard = new DashboardDto(income, expenses, income - expenses, preferredCurrency, topCategories, budgetDtos);
        return Result<DashboardDto>.Success(dashboard);
    }
}
