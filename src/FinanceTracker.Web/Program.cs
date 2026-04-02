using FinanceTracker.Web;
using FinanceTracker.Web.Services;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");

var apiBaseUrl = "https://finance-tracker-web-app-h7gbepf2dccvfccx.westeurope-01.azurewebsites.net"; // fallback
//var apiBaseUrl = "https://localhost:7100";

builder.Services.AddScoped(sp => new HttpClient
{
    BaseAddress = new Uri(apiBaseUrl)
});

builder.Services.AddScoped<ApiClient>();
builder.Services.AddSingleton<ToastService>();
builder.Services.AddScoped<PeriodStateService>();

await builder.Build().RunAsync();
