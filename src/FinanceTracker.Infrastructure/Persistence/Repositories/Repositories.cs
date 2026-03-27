using FinanceTracker.Domain.Entities;
using FinanceTracker.Domain.Enums;
using FinanceTracker.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FinanceTracker.Infrastructure.Persistence.Repositories;

public class TransactionRepository : ITransactionRepository
{
    private readonly AppDbContext _context;

    public TransactionRepository(AppDbContext context) => _context = context;

    public async Task<Transaction?> GetByIdAsync(Guid id) =>
        await _context.Transactions.FindAsync(id);

    public async Task<IEnumerable<Transaction>> GetByUserIdAsync(string userId, int? month, int? year)
    {
        var query = _context.Transactions.Where(t => t.UserId == userId);

        if (month.HasValue && year.HasValue)
            query = query.Where(t => t.Date.Month == month.Value && t.Date.Year == year.Value);

        return await query.OrderByDescending(t => t.Date).ToListAsync();
    }

    public async Task<Transaction> AddAsync(Transaction transaction)
    {
        _context.Transactions.Add(transaction);
        return transaction;
    }

    public async Task UpdateAsync(Transaction transaction) =>
        _context.Transactions.Update(transaction);

    public async Task DeleteAsync(Guid id)
    {
        var t = await _context.Transactions.FindAsync(id);
        if (t is not null) _context.Transactions.Remove(t);
    }

    public async Task<decimal> GetTotalByTypeAndPeriodAsync(string userId, TransactionType type, int month, int year) =>
        await _context.Transactions
            .Where(t => t.UserId == userId && t.Type == type
                     && t.Date.Month == month && t.Date.Year == year)
            .SumAsync(t => t.AmountInBaseCurrency);

    public async Task<Dictionary<TransactionCategory, decimal>> GetSpendingByCategoryAsync(string userId, int month, int year) =>
        await _context.Transactions
            .Where(t => t.UserId == userId && t.Type == TransactionType.Expense
                     && t.Date.Month == month && t.Date.Year == year)
            .GroupBy(t => t.Category)
            .Select(g => new { Category = g.Key, Total = g.Sum(t => t.AmountInBaseCurrency) })
            .ToDictionaryAsync(x => x.Category, x => x.Total);
}

public class BudgetRepository : IBudgetRepository
{
    private readonly AppDbContext _context;

    public BudgetRepository(AppDbContext context) => _context = context;

    public async Task<Budget?> GetByIdAsync(Guid id) =>
        await _context.Budgets.FindAsync(id);

    public async Task<IEnumerable<Budget>> GetByUserAndPeriodAsync(string userId, int month, int year) =>
        await _context.Budgets
            .Where(b => b.UserId == userId && b.Month == month && b.Year == year)
            .ToListAsync();

    public async Task<Budget> AddAsync(Budget budget)
    {
        _context.Budgets.Add(budget);
        return budget;
    }

    public async Task UpdateAsync(Budget budget) =>
        _context.Budgets.Update(budget);

    public async Task DeleteAsync(Guid id)
    {
        var b = await _context.Budgets.FindAsync(id);
        if (b is not null) _context.Budgets.Remove(b);
    }
}

public class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _context;

    public ITransactionRepository Transactions { get; }
    public IBudgetRepository Budgets { get; }

    public UnitOfWork(AppDbContext context)
    {
        _context = context;
        Transactions = new TransactionRepository(context);
        Budgets = new BudgetRepository(context);
    }

    public Task<int> SaveChangesAsync() => _context.SaveChangesAsync();
}
