namespace FinanceTracker.Web.Services;

public class PeriodStateService
{
    private const string MonthKey = "fintracker.period.month";
    private const string YearKey = "fintracker.period.year";

    private readonly Microsoft.JSInterop.IJSRuntime _js;
    private bool _isLoaded;

    public int Month { get; private set; } = DateTime.Now.Month;
    public int Year { get; private set; } = DateTime.Now.Year;

    public PeriodStateService(Microsoft.JSInterop.IJSRuntime js)
    {
        _js = js;
    }

    public async Task EnsureLoadedAsync()
    {
        if (_isLoaded) return;

        var monthRaw = await _js.InvokeAsync<string?>("localStorage.getItem", new object?[] { MonthKey });
        if (int.TryParse(monthRaw, out var month) && month is >= 1 and <= 12)
            Month = month;

        var yearRaw = await _js.InvokeAsync<string?>("localStorage.getItem", new object?[] { YearKey });
        if (int.TryParse(yearRaw, out var year) && year >= 2000)
            Year = year;

        _isLoaded = true;
    }

    public async Task SetAsync(int? month, int year)
    {
        if (month.HasValue && month.Value is >= 1 and <= 12)
            Month = month.Value;

        if (year >= 2000)
            Year = year;

        await PersistAsync();
    }

    private async Task PersistAsync()
    {
        await _js.InvokeAsync<object?>("localStorage.setItem", new object?[] { MonthKey, Month.ToString() });
        await _js.InvokeAsync<object?>("localStorage.setItem", new object?[] { YearKey, Year.ToString() });
    }
}
