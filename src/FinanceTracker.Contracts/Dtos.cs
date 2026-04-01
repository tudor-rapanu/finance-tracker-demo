namespace FinanceTracker.Contracts;

public record TransactionDto(
    Guid Id,
    decimal Amount,
    string Currency,
    decimal AmountInBaseCurrency,
    int Type,
    int Category,
    string Description,
    DateTime Date,
    string? Notes
);

public record CreateTransactionDto(
    decimal Amount,
    string Currency,
    int Type,
    int Category,
    string Description,
    DateTime Date,
    string? Notes
);

public record UpdateTransactionDto(
    decimal Amount,
    string Currency,
    int Type,
    int Category,
    string Description,
    DateTime Date,
    string? Notes
);

public record BudgetDto(
    Guid Id,
    int Category,
    decimal LimitAmount,
    string Currency,
    int Month,
    int Year,
    decimal SpentAmount,
    decimal RemainingAmount,
    double PercentageUsed
);

public record CreateBudgetDto(
    int Category,
    decimal LimitAmount,
    string Currency,
    int Month,
    int Year
);

public record UpdateBudgetDto(
    int Category,
    decimal LimitAmount,
    string Currency,
    int Month,
    int Year
);

public record DashboardDto(
    decimal TotalIncome,
    decimal TotalExpenses,
    decimal NetBalance,
    string Currency,
    IEnumerable<CategorySpendingDto> TopCategories,
    IEnumerable<BudgetDto> ActiveBudgets
);

public record CategorySpendingDto(
    int Category,
    decimal Amount,
    double Percentage
);

public record AuthResponseDto(
    string Token,
    string RefreshToken,
    DateTime Expiry,
    string UserId,
    string Email,
    string FirstName,
    string LastName
);

public record RegisterDto(
    string FirstName,
    string LastName,
    string Email,
    string Password,
    string PreferredCurrency
);

public record LoginDto(
    string Email,
    string Password
);

public record UpdatePreferredCurrencyDto(string PreferredCurrency);

public record ExchangeRateDto(
    string BaseCurrency,
    Dictionary<string, decimal> Rates,
    DateTime LastUpdated
);

// ── Admin ──────────────────────────────────────────────────────────────────
public record UserDto(
    string Id,
    string Email,
    string FirstName,
    string LastName,
    string PreferredCurrency,
    DateTime CreatedAt,
    IEnumerable<string> Roles
);

public record AdminDashboardDto(
    int TotalUsers,
    int TotalTransactions,
    decimal TotalSystemIncome,
    decimal TotalSystemExpenses,
    IEnumerable<UserDto> RecentUsers
);

public record SetUserRoleDto(string UserId, string Role, bool Assign);
