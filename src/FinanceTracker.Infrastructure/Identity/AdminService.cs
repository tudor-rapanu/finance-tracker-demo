using FinanceTracker.Application.Interfaces;
using FinanceTracker.Contracts;
using FinanceTracker.Domain.Entities;
using FinanceTracker.Domain.Enums;
using FinanceTracker.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace FinanceTracker.Infrastructure.Identity;

public class AdminService : IAdminService
{
    private readonly UserManager<AppUser> _userManager;
    private readonly AppDbContext _context;
    private readonly IExchangeRateService _exchangeRateService;

    public AdminService(UserManager<AppUser> userManager, AppDbContext context, IExchangeRateService exchangeRateService)
    {
        _userManager = userManager;
        _context = context;
        _exchangeRateService = exchangeRateService;
    }

    public async Task<List<UserDto>> GetAllUsersAsync()
    {
        var users = await _userManager.Users
            .OrderByDescending(u => u.CreatedAt)
            .ToListAsync();

        var result = new List<UserDto>(users.Count);
        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);
            result.Add(new UserDto(
                user.Id,
                user.Email!,
                user.FirstName,
                user.LastName,
                user.PreferredCurrency,
                user.CreatedAt,
                roles));
        }

        return result;
    }

    public async Task<AdminDashboardDto> GetAdminDashboardAsync()
    {
        var totalUsers = await _userManager.Users.CountAsync();
        var totalTransactions = await _context.Transactions.CountAsync();

        var totalIncome = await _context.Transactions
            .Where(t => t.Type == TransactionType.Income)
            .SumAsync(t => (decimal?)t.AmountInBaseCurrency) ?? 0m;

        var totalExpenses = await _context.Transactions
            .Where(t => t.Type == TransactionType.Expense)
            .SumAsync(t => (decimal?)t.AmountInBaseCurrency) ?? 0m;

        var recentUsers = await _userManager.Users
            .OrderByDescending(u => u.CreatedAt)
            .Take(5)
            .ToListAsync();

        var recentUserDtos = new List<UserDto>(recentUsers.Count);
        foreach (var user in recentUsers)
        {
            var roles = await _userManager.GetRolesAsync(user);
            recentUserDtos.Add(new UserDto(
                user.Id,
                user.Email!,
                user.FirstName,
                user.LastName,
                user.PreferredCurrency,
                user.CreatedAt,
                roles));
        }

        return new AdminDashboardDto(totalUsers, totalTransactions, totalIncome, totalExpenses, recentUserDtos);
    }

    public async Task SetUserRoleAsync(string userId, string role, bool assign)
    {
        var user = await _userManager.FindByIdAsync(userId)
            ?? throw new Exception($"User '{userId}' not found.");

        if (assign)
        {
            if (!await _userManager.IsInRoleAsync(user, role))
                await _userManager.AddToRoleAsync(user, role);
        }
        else
        {
            if (await _userManager.IsInRoleAsync(user, role))
                await _userManager.RemoveFromRoleAsync(user, role);
        }
    }

    public async Task<int> RecalculateTransactionAmountsAsync()
    {
        var userCurrencyMap = await _userManager.Users
            .Select(u => new { u.Id, u.PreferredCurrency })
            .ToDictionaryAsync(x => x.Id, x => string.IsNullOrWhiteSpace(x.PreferredCurrency) ? "USD" : x.PreferredCurrency);

        var transactions = await _context.Transactions.ToListAsync();

        foreach (var transaction in transactions)
        {
            var preferredCurrency = userCurrencyMap.GetValueOrDefault(transaction.UserId, "USD");
            transaction.AmountInBaseCurrency = await _exchangeRateService.ConvertAsync(
                transaction.Amount,
                transaction.Currency,
                preferredCurrency);
        }

        await _context.SaveChangesAsync();
        return transactions.Count;
    }
}
