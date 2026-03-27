using FinanceTracker.Contracts;

namespace FinanceTracker.Application.Interfaces;

public interface IAuthService
{
    Task<AuthResponseDto> RegisterAsync(RegisterDto dto);
    Task<AuthResponseDto> LoginAsync(LoginDto dto);
    Task<AuthResponseDto> RefreshTokenAsync(string token, string refreshToken);
    Task<AuthResponseDto> UpdatePreferredCurrencyAsync(string userId, string preferredCurrency);
}

public interface IExchangeRateService
{
    Task<ExchangeRateDto> GetRatesAsync(string baseCurrency = "USD");
    Task<decimal> ConvertAsync(decimal amount, string from, string to);
}

public interface ICurrentUserService
{
    string? UserId { get; }
    string? Email { get; }
    string? PreferredCurrency { get; }
}

public interface IAdminService
{
    Task<List<UserDto>> GetAllUsersAsync();
    Task<AdminDashboardDto> GetAdminDashboardAsync();
    Task SetUserRoleAsync(string userId, string role, bool assign);
    Task<int> RecalculateTransactionAmountsAsync();
}
