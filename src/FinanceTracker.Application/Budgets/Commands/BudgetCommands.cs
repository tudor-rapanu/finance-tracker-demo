using FinanceTracker.Application.Common;
using FinanceTracker.Contracts;
using FinanceTracker.Application.Interfaces;
using FinanceTracker.Domain.Entities;
using FinanceTracker.Domain.Enums;
using FinanceTracker.Domain.Interfaces;
using MediatR;

namespace FinanceTracker.Application.Budgets.Commands;

public record CreateBudgetCommand(CreateBudgetDto Dto) : IRequest<Result<BudgetDto>>;

public class CreateBudgetCommandHandler : IRequestHandler<CreateBudgetCommand, Result<BudgetDto>>
{
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUserService _currentUser;

    public CreateBudgetCommandHandler(IUnitOfWork uow, ICurrentUserService currentUser)
    {
        _uow = uow;
        _currentUser = currentUser;
    }

    public async Task<Result<BudgetDto>> Handle(CreateBudgetCommand request, CancellationToken ct)
    {
        if (_currentUser.UserId is null)
            return Result<BudgetDto>.Failure("User not authenticated.");

        var dto = request.Dto;
        var budget = new Budget
        {
            UserId = _currentUser.UserId,
            Category = (TransactionCategory)dto.Category,
            LimitAmount = dto.LimitAmount,
            Currency = dto.Currency,
            Month = dto.Month,
            Year = dto.Year
        };

        await _uow.Budgets.AddAsync(budget);
        await _uow.SaveChangesAsync();

        return Result<BudgetDto>.Success(new BudgetDto(
            budget.Id, (int)budget.Category, budget.LimitAmount,
            budget.Currency, budget.Month, budget.Year, 0, budget.LimitAmount, 0));
    }
}

public record DeleteBudgetCommand(Guid Id) : IRequest<Result<bool>>;

public class DeleteBudgetCommandHandler : IRequestHandler<DeleteBudgetCommand, Result<bool>>
{
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUserService _currentUser;

    public DeleteBudgetCommandHandler(IUnitOfWork uow, ICurrentUserService currentUser)
    {
        _uow = uow;
        _currentUser = currentUser;
    }

    public async Task<Result<bool>> Handle(DeleteBudgetCommand request, CancellationToken ct)
    {
        var budget = await _uow.Budgets.GetByIdAsync(request.Id);

        if (budget is null) return Result<bool>.Failure("Budget not found.");
        if (budget.UserId != _currentUser.UserId) return Result<bool>.Failure("Not authorized.");

        await _uow.Budgets.DeleteAsync(request.Id);
        await _uow.SaveChangesAsync();

        return Result<bool>.Success(true);
    }
}
