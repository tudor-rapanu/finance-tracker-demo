using FinanceTracker.Application.Common;
using FinanceTracker.Contracts;
using FinanceTracker.Application.Interfaces;
using FinanceTracker.Domain.Entities;
using FinanceTracker.Domain.Enums;
using FinanceTracker.Domain.Interfaces;
using MediatR;

namespace FinanceTracker.Application.Transactions.Commands;

// --- Create Transaction ---
public record CreateTransactionCommand(CreateTransactionDto Dto) : IRequest<Result<TransactionDto>>;

public class CreateTransactionCommandHandler : IRequestHandler<CreateTransactionCommand, Result<TransactionDto>>
{
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUserService _currentUser;
    private readonly IExchangeRateService _exchangeRateService;

    public CreateTransactionCommandHandler(IUnitOfWork uow, ICurrentUserService currentUser, IExchangeRateService exchangeRateService)
    {
        _uow = uow;
        _currentUser = currentUser;
        _exchangeRateService = exchangeRateService;
    }

    public async Task<Result<TransactionDto>> Handle(CreateTransactionCommand request, CancellationToken ct)
    {
        if (_currentUser.UserId is null)
            return Result<TransactionDto>.Failure("User not authenticated.");

        var dto = request.Dto;
        var normalizedCurrency = dto.Currency.Trim().ToUpperInvariant();
        var normalizedDescription = dto.Description.Trim();
        var normalizedNotes = string.IsNullOrWhiteSpace(dto.Notes) ? null : dto.Notes.Trim();
        var preferredCurrency = _currentUser.PreferredCurrency ?? "USD";
        var amountInPreferred = await _exchangeRateService.ConvertAsync(dto.Amount, normalizedCurrency, preferredCurrency);

        var transaction = new Transaction
        {
            UserId = _currentUser.UserId,
            Amount = dto.Amount,
            Currency = normalizedCurrency,
            AmountInBaseCurrency = amountInPreferred,
            Type = (TransactionType)dto.Type,
            Category = (TransactionCategory)dto.Category,
            Description = normalizedDescription,
            Date = dto.Date,
            Notes = normalizedNotes
        };

        await _uow.Transactions.AddAsync(transaction);
        await _uow.SaveChangesAsync();

        return Result<TransactionDto>.Success(MapToDto(transaction));
    }

    private static TransactionDto MapToDto(Transaction t) =>
        new(t.Id, t.Amount, t.Currency, t.AmountInBaseCurrency, (int)t.Type, (int)t.Category, t.Description, t.Date, t.Notes);
}

// --- Delete Transaction ---
public record DeleteTransactionCommand(Guid Id) : IRequest<Result<bool>>;

public class DeleteTransactionCommandHandler : IRequestHandler<DeleteTransactionCommand, Result<bool>>
{
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUserService _currentUser;

    public DeleteTransactionCommandHandler(IUnitOfWork uow, ICurrentUserService currentUser)
    {
        _uow = uow;
        _currentUser = currentUser;
    }

    public async Task<Result<bool>> Handle(DeleteTransactionCommand request, CancellationToken ct)
    {
        var transaction = await _uow.Transactions.GetByIdAsync(request.Id);

        if (transaction is null)
            return Result<bool>.Failure("Transaction not found.");

        if (transaction.UserId != _currentUser.UserId)
            return Result<bool>.Failure("Not authorized.");

        await _uow.Transactions.DeleteAsync(request.Id);
        await _uow.SaveChangesAsync();

        return Result<bool>.Success(true);
    }
}
