using FinanceTracker.Domain.Entities;
using FinanceTracker.Domain.Enums;

namespace FinanceTracker.Domain.Interfaces;

public interface ITransactionRepository
{
    Task<Transaction?> GetByIdAsync(Guid id);
    Task<IEnumerable<Transaction>> GetByUserIdAsync(string userId, int? month = null, int? year = null);
    Task<Transaction> AddAsync(Transaction transaction);
    Task UpdateAsync(Transaction transaction);
    Task DeleteAsync(Guid id);
    Task<decimal> GetTotalByTypeAndPeriodAsync(string userId, TransactionType type, int month, int year);
    Task<Dictionary<TransactionCategory, decimal>> GetSpendingByCategoryAsync(string userId, int month, int year);
}

public interface IBudgetRepository
{
    Task<Budget?> GetByIdAsync(Guid id);
    Task<IEnumerable<Budget>> GetByUserAndPeriodAsync(string userId, int month, int year);
    Task<Budget> AddAsync(Budget budget);
    Task UpdateAsync(Budget budget);
    Task DeleteAsync(Guid id);
}

public interface IUnitOfWork
{
    ITransactionRepository Transactions { get; }
    IBudgetRepository Budgets { get; }
    Task<int> SaveChangesAsync();
}
