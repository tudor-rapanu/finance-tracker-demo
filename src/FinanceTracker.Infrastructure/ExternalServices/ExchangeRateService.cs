using System.Text.Json;
using FinanceTracker.Contracts;
using FinanceTracker.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace FinanceTracker.Infrastructure.ExternalServices;

public class ExchangeRateService : IExchangeRateService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _config;
    private readonly ILogger<ExchangeRateService> _logger;

    // Simple in-memory cache to avoid hammering the free API
    private static ExchangeRateDto? _cachedRates;
    private static DateTime _cacheExpiry = DateTime.MinValue;

    public ExchangeRateService(HttpClient httpClient, IConfiguration config, ILogger<ExchangeRateService> logger)
    {
        _httpClient = httpClient;
        _config = config;
        _logger = logger;
    }

    public async Task<ExchangeRateDto> GetRatesAsync(string baseCurrency = "USD")
    {
        var normalizedBaseCurrency = NormalizeCurrency(baseCurrency);

        // Return cached rates if still valid (cache for 1 hour)
        if (_cachedRates is not null && DateTime.UtcNow < _cacheExpiry)
            return _cachedRates;

        try
        {
            var apiKey = _config["ExchangeRateApi:ApiKey"];
            var url = $"https://v6.exchangerate-api.com/v6/{apiKey}/latest/{normalizedBaseCurrency}";

            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var doc = JsonDocument.Parse(json);

            var rates = doc.RootElement
                .GetProperty("conversion_rates")
                .EnumerateObject()
                .ToDictionary(p => p.Name, p => p.Value.GetDecimal());

            _cachedRates = new ExchangeRateDto(normalizedBaseCurrency, rates, DateTime.UtcNow);
            _cacheExpiry = DateTime.UtcNow.AddHours(1);

            return _cachedRates;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to fetch exchange rates. Using 1:1 fallback.");
            // Fallback: return 1:1 rates so the app still works without an API key
            return new ExchangeRateDto("USD", new Dictionary<string, decimal> { { "USD", 1m } }, DateTime.UtcNow);
        }
    }

    public async Task<decimal> ConvertAsync(decimal amount, string from, string to)
    {
        from = NormalizeCurrency(from);
        to = NormalizeCurrency(to);

        if (from == to) return amount;

        var rates = await GetRatesAsync("USD");

        var fromRate = rates.Rates.GetValueOrDefault(from, 1m);
        var toRate = rates.Rates.GetValueOrDefault(to, 1m);

        // Convert: amount -> USD -> target currency
        return amount / fromRate * toRate;
    }

    private static string NormalizeCurrency(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "USD";

        var normalized = value.Trim().ToUpperInvariant();
        if (normalized.Length != 3 || normalized.Any(c => c is < 'A' or > 'Z'))
            return "USD";

        return normalized;
    }
}
