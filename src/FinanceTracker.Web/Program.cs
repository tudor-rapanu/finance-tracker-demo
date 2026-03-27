using FinanceTracker.Web;
using FinanceTracker.Web.Services;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");

builder.Services.AddScoped(sp => new HttpClient
{
    BaseAddress = new Uri("https://localhost:7100")
});

builder.Services.AddScoped<ApiClient>();

await builder.Build().RunAsync();
