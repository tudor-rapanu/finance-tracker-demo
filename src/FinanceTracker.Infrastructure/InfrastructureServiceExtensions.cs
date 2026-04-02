using FinanceTracker.Application.Interfaces;
using FinanceTracker.Domain.Entities;
using FinanceTracker.Domain.Interfaces;
using FinanceTracker.Infrastructure.ExternalServices;
using FinanceTracker.Infrastructure.Identity;
using FinanceTracker.Infrastructure.Persistence;
using FinanceTracker.Infrastructure.Persistence.Repositories;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Text;

namespace FinanceTracker.Infrastructure;

public static class InfrastructureServiceExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        var connectionString = config.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException(
                "Missing ConnectionStrings:DefaultConnection. Set it via dotnet user-secrets for FinanceTracker.API.");

        var jwtSettings = config.GetSection("JwtSettings");
        var jwtSecret = jwtSettings["SecretKey"];
        if (string.IsNullOrWhiteSpace(jwtSecret))
            throw new InvalidOperationException(
                "Missing JwtSettings:SecretKey. Set it via dotnet user-secrets for FinanceTracker.API.");

        // Database
        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(connectionString,
                b => b.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName)));

        // Identity
        services.AddIdentity<AppUser, IdentityRole>(options =>
        {
            options.Password.RequiredLength = 8;
            options.Password.RequireDigit = true;
            options.Password.RequireUppercase = true;
            options.User.RequireUniqueEmail = true;
        })
        .AddEntityFrameworkStores<AppDbContext>()
        .AddDefaultTokenProviders();

        // JWT Authentication
        var key = Encoding.UTF8.GetBytes(jwtSecret);

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwtSettings["Issuer"],
                ValidAudience = jwtSettings["Audience"],
                IssuerSigningKey = new SymmetricSecurityKey(key),
                RoleClaimType = ClaimTypes.Role
            };
        });

        // Repositories & UoW
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // Services
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IAdminService, AdminService>();
        services.AddHttpClient<IExchangeRateService, ExchangeRateService>();

        services.AddSingleton<TransactionExportBackgroundService>();
        services.AddSingleton<ITransactionExportService>(sp =>
            sp.GetRequiredService<TransactionExportBackgroundService>());
        services.AddHostedService(sp =>
            sp.GetRequiredService<TransactionExportBackgroundService>());

        return services;
    }
}
