using FinanceTracker.Contracts;
using FinanceTracker.Application.Common;

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
    Task<AdminDashboardDto> GetAdminDashboardAsync(int? month = null, int? year = null);
    Task SetUserRoleAsync(string userId, string role, bool assign);
    Task<int> RecalculateTransactionAmountsAsync();
}

public record FileExportDto(byte[] Content, string ContentType, string FileName);

public interface ITransactionExportService
{
    bool IsMoreThanOneMonth(TransactionExportRequestDto request);
    Task<Result<FileExportDto>> ExportDirectAsync(string userId, TransactionExportRequestDto request, CancellationToken ct);
    Task<Result<ExportJobCreatedDto>> QueueExportAsync(string userId, TransactionExportRequestDto request, CancellationToken ct);
    Task<Result<ExportJobStatusDto>> GetJobStatusAsync(string userId, Guid jobId, CancellationToken ct);
    Task<Result<FileExportDto>> DownloadJobAsync(string userId, Guid jobId, CancellationToken ct);
}
