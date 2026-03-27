using FinanceTracker.Web;
using FinanceTracker.Web.Services;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");

var apiBaseUrl = builder.Configuration["ApiBaseUrl"]!;
builder.Services.AddScoped(sp => new HttpClient
{
    BaseAddress = new Uri("https://finance-tracker-web-app-h7gbepf2dccvfccx.westeurope-01.azurewebsites.net")
});

builder.Services.AddScoped<ApiClient>();

await builder.Build().RunAsync();
