using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using FinanceTracker.Contracts;
using Microsoft.JSInterop;

namespace FinanceTracker.Web.Services;

public class ApiClient
{
    private readonly HttpClient _http;
    private readonly IJSRuntime _js;

    private const string TokenKey = "authToken";

    public record ApiResult(bool Success, string? Error = null)
    {
        public static ApiResult Ok() => new(true);
        public static ApiResult Fail(string? error) => new(false, string.IsNullOrWhiteSpace(error) ? "Request failed." : error);
    }

    public record ApiResult<T>(bool Success, T? Data = default, string? Error = null, int? StatusCode = null)
    {
        public static ApiResult<T> Ok(T data) => new(true, data);
        public static ApiResult<T> Fail(string? error, int? statusCode = null) =>
            new(false, default, string.IsNullOrWhiteSpace(error) ? "Request failed." : error, statusCode);
    }

    public record DownloadedFile(string FileName, string ContentType, byte[] Bytes);

    private sealed class ApiErrorResponse
    {
        public string? Error { get; set; }
    }

    public ApiClient(HttpClient http, IJSRuntime js)
    {
        _http = http;
        _js = js;
    }

    // ── Token helpers via browser localStorage JS API ──
    private async Task<string?> GetTokenAsync() =>
        await _js.InvokeAsync<string?>("localStorage.getItem", TokenKey);

    private async Task SetTokenAsync(string token) =>
        await _js.InvokeVoidAsync("localStorage.setItem", TokenKey, token);

    private async Task RemoveTokenAsync() =>
        await _js.InvokeVoidAsync("localStorage.removeItem", TokenKey);

    private async Task AttachTokenAsync()
    {
        var token = await GetTokenAsync();
        if (!string.IsNullOrEmpty(token))
            _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    private static async Task<string?> TryReadErrorAsync(HttpResponseMessage response)
    {
        try
        {
            var body = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
            if (!string.IsNullOrWhiteSpace(body?.Error))
                return body.Error;
        }
        catch
        {
            // Ignore JSON parsing errors and fall back to status text.
        }

        return response.ReasonPhrase;
    }

    // ── Auth ──
    public async Task<(AuthResponseDto? Data, string? Error)> LoginAsync(LoginDto dto)
    {
        var response = await _http.PostAsJsonAsync("api/auth/login", dto);
        if (!response.IsSuccessStatusCode)
            return (null, await TryReadErrorAsync(response));

        var result = await response.Content.ReadFromJsonAsync<AuthResponseDto>();
        if (result is not null)
            await SetTokenAsync(result.Token);

        return result is null
            ? (null, "Invalid response from server.")
            : (result, null);
    }

    public async Task<(AuthResponseDto? Data, string? Error)> RegisterAsync(RegisterDto dto)
    {
        var response = await _http.PostAsJsonAsync("api/auth/register", dto);
        if (!response.IsSuccessStatusCode)
            return (null, await TryReadErrorAsync(response));

        var result = await response.Content.ReadFromJsonAsync<AuthResponseDto>();
        if (result is not null)
            await SetTokenAsync(result.Token);

        return result is null
            ? (null, "Invalid response from server.")
            : (result, null);
    }

    public async Task<ApiResult> UpdatePreferredCurrencyAsync(string preferredCurrency)
    {
        await AttachTokenAsync();
        var response = await _http.PutAsJsonAsync(
            "api/auth/preferred-currency",
            new UpdatePreferredCurrencyDto(preferredCurrency));

        if (!response.IsSuccessStatusCode)
            return ApiResult.Fail(await TryReadErrorAsync(response));

        var result = await response.Content.ReadFromJsonAsync<AuthResponseDto>();
        if (result is null) return ApiResult.Fail("Invalid response from server.");

        await SetTokenAsync(result.Token);
        return ApiResult.Ok();
    }

    public async Task LogoutAsync() =>
        await RemoveTokenAsync();

    public async Task<bool> IsAuthenticatedAsync() =>
        !string.IsNullOrEmpty(await GetTokenAsync());

    // ── Transactions ──
    public async Task<List<TransactionDto>> GetTransactionsAsync(int? month = null, int? year = null)
    {
        await AttachTokenAsync();
        var query = month.HasValue ? $"?month={month}&year={year}" : "";
        return await _http.GetFromJsonAsync<List<TransactionDto>>($"api/transactions{query}") ?? new();
    }

    public async Task<ApiResult<DashboardDto>> GetDashboardAsync(int? month = null, int? year = null)
    {
        await AttachTokenAsync();
        var now = DateTime.Now;

        var response = await _http.GetAsync(
            $"api/transactions/dashboard?month={month ?? now.Month}&year={year ?? now.Year}");

        if (!response.IsSuccessStatusCode)
        {
            var error = await TryReadErrorAsync(response);
            return ApiResult<DashboardDto>.Fail(error, (int)response.StatusCode);
        }

        var data = await response.Content.ReadFromJsonAsync<DashboardDto>();
        if (data is null)
            return ApiResult<DashboardDto>.Fail("Invalid response from server.", (int)response.StatusCode);

        return ApiResult<DashboardDto>.Ok(data);
    }

    public async Task<ApiResult> CreateTransactionAsync(CreateTransactionDto dto)
    {
        await AttachTokenAsync();
        var response = await _http.PostAsJsonAsync("api/transactions", dto);
        if (response.IsSuccessStatusCode) return ApiResult.Ok();
        return ApiResult.Fail(await TryReadErrorAsync(response));
    }

    public async Task<ApiResult> UpdateTransactionAsync(Guid id, UpdateTransactionDto dto)
    {
        await AttachTokenAsync();
        var response = await _http.PutAsJsonAsync($"api/transactions/{id}", dto);
        if (response.IsSuccessStatusCode) return ApiResult.Ok();
        return ApiResult.Fail(await TryReadErrorAsync(response));
    }

    public async Task<ApiResult<DownloadedFile>> ExportTransactionsMonthAsync(string format, int month, int year)
    {
        await AttachTokenAsync();
        var response = await _http.GetAsync($"api/transactions/export?format={format}&month={month}&year={year}");
        return await ReadDownloadResponseAsync(response, $"transactions_{year}-{month:00}.{format}");
    }

    public async Task<ApiResult<ExportJobCreatedDto>> QueueTransactionExportJobAsync(TransactionExportRequestDto request)
    {
        await AttachTokenAsync();
        var response = await _http.PostAsJsonAsync("api/transactions/export/jobs", request);

        if (!response.IsSuccessStatusCode)
            return ApiResult<ExportJobCreatedDto>.Fail(await TryReadErrorAsync(response), (int)response.StatusCode);

        var data = await response.Content.ReadFromJsonAsync<ExportJobCreatedDto>();
        return data is null
            ? ApiResult<ExportJobCreatedDto>.Fail("Invalid response from server.", (int)response.StatusCode)
            : ApiResult<ExportJobCreatedDto>.Ok(data);
    }

    public async Task<ApiResult<ExportJobStatusDto>> GetTransactionExportJobStatusAsync(Guid jobId)
    {
        await AttachTokenAsync();
        var response = await _http.GetAsync($"api/transactions/export/jobs/{jobId}");

        if (!response.IsSuccessStatusCode)
            return ApiResult<ExportJobStatusDto>.Fail(await TryReadErrorAsync(response), (int)response.StatusCode);

        var data = await response.Content.ReadFromJsonAsync<ExportJobStatusDto>();
        return data is null
            ? ApiResult<ExportJobStatusDto>.Fail("Invalid response from server.", (int)response.StatusCode)
            : ApiResult<ExportJobStatusDto>.Ok(data);
    }

    public async Task<ApiResult<DownloadedFile>> DownloadTransactionExportJobAsync(Guid jobId, string? format = null)
    {
        await AttachTokenAsync();
        var response = await _http.GetAsync($"api/transactions/export/jobs/{jobId}/download");
        var normalizedFormat = (format ?? string.Empty).Trim().ToLowerInvariant();
        var extension = normalizedFormat is "pdf" or "csv" ? normalizedFormat : "bin";
        return await ReadDownloadResponseAsync(response, $"transactions_export_{jobId}.{extension}");
    }

    public async Task<bool> DeleteTransactionAsync(Guid id)
    {
        await AttachTokenAsync();
        var response = await _http.DeleteAsync($"api/transactions/{id}");
        return response.IsSuccessStatusCode;
    }

    private static async Task<ApiResult<DownloadedFile>> ReadDownloadResponseAsync(HttpResponseMessage response, string fallbackFileName)
    {
        if (!response.IsSuccessStatusCode)
        {
            var error = await TryReadErrorAsync(response);
            return ApiResult<DownloadedFile>.Fail(error, (int)response.StatusCode);
        }

        var bytes = await response.Content.ReadAsByteArrayAsync();
        var contentType = response.Content.Headers.ContentType?.MediaType ?? "application/octet-stream";
        var contentDisposition = response.Content.Headers.ContentDisposition;
        var fileName = contentDisposition?.FileNameStar ?? contentDisposition?.FileName;
        if (string.IsNullOrWhiteSpace(fileName))
        {
            var inferredExtension = contentType switch
            {
                "application/pdf" => "pdf",
                "text/csv" => "csv",
                _ => string.Empty
            };

            if (!string.IsNullOrWhiteSpace(inferredExtension) && fallbackFileName.EndsWith(".bin", StringComparison.OrdinalIgnoreCase))
                fileName = Path.ChangeExtension(fallbackFileName, inferredExtension);
            else
                fileName = fallbackFileName;
        }

        fileName = fileName.Trim('"');

        return ApiResult<DownloadedFile>.Ok(new DownloadedFile(fileName, contentType, bytes));
    }

    // ── Budgets ──
    public async Task<ApiResult> CreateBudgetAsync(CreateBudgetDto dto)
    {
        await AttachTokenAsync();
        var response = await _http.PostAsJsonAsync("api/budgets", dto);
        if (response.IsSuccessStatusCode) return ApiResult.Ok();
        return ApiResult.Fail(await TryReadErrorAsync(response));
    }

    public async Task<ApiResult> UpdateBudgetAsync(Guid id, UpdateBudgetDto dto)
    {
        await AttachTokenAsync();
        var response = await _http.PutAsJsonAsync($"api/budgets/{id}", dto);
        if (response.IsSuccessStatusCode) return ApiResult.Ok();
        return ApiResult.Fail(await TryReadErrorAsync(response));
    }

    public async Task<bool> DeleteBudgetAsync(Guid id)
    {
        await AttachTokenAsync();
        var response = await _http.DeleteAsync($"api/budgets/{id}");
        return response.IsSuccessStatusCode;
    }

    // ── Role helpers ──
    /// <summary>Parses the JWT payload and returns the user's role (e.g. "Admin" or "User"), or null if unauthenticated.</summary>
    public async Task<string?> GetUserRoleAsync()
    {
        var token = await GetTokenAsync();
        if (string.IsNullOrEmpty(token)) return null;

        var parts = token.Split('.');
        if (parts.Length != 3) return null;

        // Base64url → Base64
        var payload = parts[1];
        var padded = payload.PadRight(payload.Length + (4 - payload.Length % 4) % 4, '=')
                            .Replace('-', '+').Replace('_', '/');

        var json = Encoding.UTF8.GetString(Convert.FromBase64String(padded));
        using var doc = JsonDocument.Parse(json);

        // Check for standard role claim type key
        const string standardRoleClaimType = "http://schemas.microsoft.com/ws/2008/06/identity/claims/role";
        if (doc.RootElement.TryGetProperty(standardRoleClaimType, out var roleEl))
        {
            if (roleEl.ValueKind == JsonValueKind.Array && roleEl.GetArrayLength() > 0)
                return roleEl[0].GetString();
            else if (roleEl.ValueKind == JsonValueKind.String)
                return roleEl.GetString();
        }

        return null;
    }

    public async Task<bool> IsAdminAsync() =>
        string.Equals(await GetUserRoleAsync(), "Admin", StringComparison.OrdinalIgnoreCase);

    public async Task<string?> GetPreferredCurrencyAsync()
    {
        var token = await GetTokenAsync();
        if (string.IsNullOrEmpty(token)) return null;

        var parts = token.Split('.');
        if (parts.Length != 3) return null;

        var payload = parts[1];
        var padded = payload.PadRight(payload.Length + (4 - payload.Length % 4) % 4, '=')
                            .Replace('-', '+').Replace('_', '/');

        var json = Encoding.UTF8.GetString(Convert.FromBase64String(padded));
        using var doc = JsonDocument.Parse(json);

        if (doc.RootElement.TryGetProperty("preferredCurrency", out var currencyEl))
            return currencyEl.GetString();

        return null;
    }

    // ── Admin ──
    public async Task<List<UserDto>> GetAdminUsersAsync()
    {
        await AttachTokenAsync();
        return await _http.GetFromJsonAsync<List<UserDto>>("api/admin/users") ?? new();
    }

    public async Task<AdminDashboardDto?> GetAdminStatsAsync(int? month = null, int? year = null)
    {
        await AttachTokenAsync();
        var url = month.HasValue && year.HasValue
            ? $"api/admin/dashboard?month={month}&year={year}"
            : "api/admin/dashboard";
        return await _http.GetFromJsonAsync<AdminDashboardDto>(url);
    }

    public async Task<ApiResult> SetUserRoleAsync(SetUserRoleDto dto)
    {
        await AttachTokenAsync();
        var response = await _http.PutAsJsonAsync("api/admin/users/role", dto);
        if (response.IsSuccessStatusCode) return ApiResult.Ok();
        return ApiResult.Fail(await TryReadErrorAsync(response));
    }
}

