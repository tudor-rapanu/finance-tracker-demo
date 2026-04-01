using FinanceTracker.API.Extensions;
using FinanceTracker.API.Middleware;
using FinanceTracker.Application.Admin.Commands;
using FinanceTracker.Application.Auth.Commands;
using FinanceTracker.Application.Budgets.Commands;
using FinanceTracker.Application.Common.Behaviors;
using FinanceTracker.Application.Interfaces;
using FinanceTracker.Application.Transactions.Commands;
using FinanceTracker.Application.Transactions.Queries;
using FinanceTracker.Infrastructure;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.FileProviders;

var builder = WebApplication.CreateBuilder(args);

// ── Infrastructure (EF Core, Identity, JWT, Repositories, External Services) ──
builder.Services.AddInfrastructure(builder.Configuration);

// ── Application (MediatR) ──
builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssembly(
        typeof(FinanceTracker.Application.Transactions.Commands.CreateTransactionCommand).Assembly));
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

// FluentValidation validators
builder.Services.AddTransient<IValidator<RegisterCommand>, RegisterCommandValidator>();
builder.Services.AddTransient<IValidator<LoginCommand>, LoginCommandValidator>();
builder.Services.AddTransient<IValidator<UpdatePreferredCurrencyCommand>, UpdatePreferredCurrencyCommandValidator>();
builder.Services.AddTransient<IValidator<CreateTransactionCommand>, CreateTransactionCommandValidator>();
builder.Services.AddTransient<IValidator<CreateBudgetCommand>, CreateBudgetCommandValidator>();
builder.Services.AddTransient<IValidator<GetTransactionsQuery>, GetTransactionsQueryValidator>();
builder.Services.AddTransient<IValidator<GetDashboardQuery>, GetDashboardQueryValidator>();
builder.Services.AddTransient<IValidator<SetUserRoleCommand>, SetUserRoleCommandValidator>();

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
        policy.WithOrigins(builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()!)
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

app.MapGet("/routes", (IEnumerable<EndpointDataSource> endpointSources) =>
    endpointSources
        .SelectMany(e => e.Endpoints)
        .Select(e => e.DisplayName)
);

app.UseHttpsRedirection();

// Enables serving static files from wwwroot for documentation site
app.UseFileServer(new FileServerOptions
{
    FileProvider = new PhysicalFileProvider(
        Path.Combine(builder.Environment.ContentRootPath, "wwwroot", "docs")),
    RequestPath = "/docs",
    EnableDefaultFiles = true
});

// Keep this to serve normal API static assets (if you have any)
app.UseStaticFiles();

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
