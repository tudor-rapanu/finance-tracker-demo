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

    // ── Auth ──
    public async Task<AuthResponseDto?> LoginAsync(LoginDto dto)
    {
        var response = await _http.PostAsJsonAsync("api/auth/login", dto);
        if (!response.IsSuccessStatusCode) return null;
        var result = await response.Content.ReadFromJsonAsync<AuthResponseDto>();
        if (result is not null)
            await SetTokenAsync(result.Token);
        return result;
    }

    public async Task<AuthResponseDto?> RegisterAsync(RegisterDto dto)
    {
        var response = await _http.PostAsJsonAsync("api/auth/register", dto);
        if (!response.IsSuccessStatusCode) return null;
        var result = await response.Content.ReadFromJsonAsync<AuthResponseDto>();
        if (result is not null)
            await SetTokenAsync(result.Token);
        return result;
    }

    public async Task<bool> UpdatePreferredCurrencyAsync(string preferredCurrency)
    {
        await AttachTokenAsync();
        var response = await _http.PutAsJsonAsync(
            "api/auth/preferred-currency",
            new UpdatePreferredCurrencyDto(preferredCurrency));

        if (!response.IsSuccessStatusCode) return false;

        var result = await response.Content.ReadFromJsonAsync<AuthResponseDto>();
        if (result is null) return false;

        await SetTokenAsync(result.Token);
        return true;
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

    public async Task<DashboardDto?> GetDashboardAsync(int? month = null, int? year = null)
    {
        await AttachTokenAsync();
        var now = DateTime.Now;
        return await _http.GetFromJsonAsync<DashboardDto>(
            $"api/transactions/dashboard?month={month ?? now.Month}&year={year ?? now.Year}");
    }

    public async Task<bool> CreateTransactionAsync(CreateTransactionDto dto)
    {
        await AttachTokenAsync();
        var response = await _http.PostAsJsonAsync("api/transactions", dto);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> DeleteTransactionAsync(Guid id)
    {
        await AttachTokenAsync();
        var response = await _http.DeleteAsync($"api/transactions/{id}");
        return response.IsSuccessStatusCode;
    }

    // ── Budgets ──
    public async Task<bool> CreateBudgetAsync(CreateBudgetDto dto)
    {
        await AttachTokenAsync();
        var response = await _http.PostAsJsonAsync("api/budgets", dto);
        return response.IsSuccessStatusCode;
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

    public async Task<AdminDashboardDto?> GetAdminStatsAsync()
    {
        await AttachTokenAsync();
        return await _http.GetFromJsonAsync<AdminDashboardDto>("api/admin/dashboard");
    }

    public async Task<bool> SetUserRoleAsync(SetUserRoleDto dto)
    {
        await AttachTokenAsync();
        var response = await _http.PutAsJsonAsync("api/admin/users/role", dto);
        return response.IsSuccessStatusCode;
    }
}

