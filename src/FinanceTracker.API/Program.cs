using FinanceTracker.API.Extensions;
using FinanceTracker.API.Middleware;
using FinanceTracker.Application.Interfaces;
using FinanceTracker.Infrastructure;
using MediatR;
using Microsoft.AspNetCore.Identity;

var builder = WebApplication.CreateBuilder(args);

// ── Infrastructure (EF Core, Identity, JWT, Repositories, External Services) ──
builder.Services.AddInfrastructure(builder.Configuration);

// ── Application (MediatR) ──
builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssembly(
        typeof(FinanceTracker.Application.Transactions.Commands.CreateTransactionCommand).Assembly));

// ── Current User ──
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();

// ── Controllers ──
builder.Services.AddControllers();

// ── Native .NET 10 OpenAPI ──
// The document/operation transformers use Microsoft.AspNetCore.OpenApi types,
// which are resolved automatically — no using directive needed.
builder.Services.AddOpenApi();

// ── CORS (for Blazor frontend) ──
builder.Services.AddCors(options =>
    options.AddDefaultPolicy(policy =>
        policy.WithOrigins("https://localhost:7200")
              .AllowAnyHeader()
              .AllowAnyMethod()));

var app = builder.Build();

// ── Seed roles ──
using (var scope = app.Services.CreateScope())
{
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    foreach (var role in new[] { "Admin", "User" })
    {
        if (!await roleManager.RoleExistsAsync(role))
            await roleManager.CreateAsync(new IdentityRole(role));
    }
}

// ── Middleware Pipeline ──
app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    // Serves the OpenAPI document at: /openapi/v1.json
    app.MapOpenApi();

    // Swagger UI pointed at the native OpenAPI endpoint
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "Finance Tracker API v1");
        options.RoutePrefix = "swagger";
    });
}

if (app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
