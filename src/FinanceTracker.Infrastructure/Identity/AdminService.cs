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

    public async Task<AdminDashboardDto> GetAdminDashboardAsync(int? month = null, int? year = null)
    {
        var totalUsers = await _userManager.Users.CountAsync();

        var txQuery = _context.Transactions.AsQueryable();
        if (month.HasValue && year.HasValue)
            txQuery = txQuery.Where(t => t.Date.Month == month.Value && t.Date.Year == year.Value);

        var totalTransactions = await txQuery.CountAsync();

        // Load only the fields needed for currency conversion
        var transactions = await txQuery
            .Select(t => new { t.Amount, t.Currency, t.Type })
            .ToListAsync();

        // Fetch exchange rates once (cached for 1 hour) and convert all amounts to USD
        var rates = await _exchangeRateService.GetRatesAsync("USD");

        decimal totalIncome = 0m;
        decimal totalExpenses = 0m;

        foreach (var t in transactions)
        {
            var fromRate = rates.Rates.GetValueOrDefault(
                (t.Currency ?? "USD").Trim().ToUpperInvariant(), 1m);
            var amountUsd = fromRate == 0m ? t.Amount : t.Amount / fromRate;

            if (t.Type == TransactionType.Income) totalIncome += amountUsd;
            else totalExpenses += amountUsd;
        }

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
        role = role.Trim();

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
