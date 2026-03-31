using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using FinanceTracker.Contracts;
using FinanceTracker.Application.Interfaces;
using FinanceTracker.Domain.Entities;
using FinanceTracker.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace FinanceTracker.Infrastructure.Identity;

public class AuthService : IAuthService
{
    private readonly UserManager<AppUser> _userManager;
    private readonly IConfiguration _config;
    private readonly AppDbContext _context;
    private readonly IExchangeRateService _exchangeRateService;

    public AuthService(
        UserManager<AppUser> userManager,
        IConfiguration config,
        AppDbContext context,
        IExchangeRateService exchangeRateService)
    {
        _userManager = userManager;
        _config = config;
        _context = context;
        _exchangeRateService = exchangeRateService;
    }

    public async Task<AuthResponseDto> RegisterAsync(RegisterDto dto)
    {
        var normalizedEmail = dto.Email.Trim();
        var normalizedFirstName = dto.FirstName.Trim();
        var normalizedLastName = dto.LastName.Trim();
        var normalizedCurrency = string.IsNullOrWhiteSpace(dto.PreferredCurrency)
            ? "USD"
            : dto.PreferredCurrency.Trim().ToUpperInvariant();

        var existingUser = await _userManager.FindByEmailAsync(normalizedEmail);
        if (existingUser is not null)
            throw new Exception("Email already registered.");

        var user = new AppUser
        {
            UserName = normalizedEmail,
            Email = normalizedEmail,
            FirstName = normalizedFirstName,
            LastName = normalizedLastName,
            PreferredCurrency = normalizedCurrency
        };

        var result = await _userManager.CreateAsync(user, dto.Password);
        if (!result.Succeeded)
            throw new Exception(string.Join(", ", result.Errors.Select(e => e.Description)));

        await _userManager.AddToRoleAsync(user, "User");

        return await GenerateAuthResponse(user);
    }

    public async Task<AuthResponseDto> LoginAsync(LoginDto dto)
    {
        var normalizedEmail = dto.Email.Trim();

        var user = await _userManager.FindByEmailAsync(normalizedEmail)
            ?? throw new Exception("Invalid credentials.");

        if (!await _userManager.CheckPasswordAsync(user, dto.Password))
            throw new Exception("Invalid credentials.");

        return await GenerateAuthResponse(user);
    }

    public Task<AuthResponseDto> RefreshTokenAsync(string token, string refreshToken)
    {
        // Simplified: in production, validate and rotate refresh token from DB
        throw new NotImplementedException("Implement refresh token storage in a production app.");
    }

    public async Task<AuthResponseDto> UpdatePreferredCurrencyAsync(string userId, string preferredCurrency)
    {
        var user = await _userManager.FindByIdAsync(userId)
            ?? throw new Exception("User not found.");

        var normalizedCurrency = string.IsNullOrWhiteSpace(preferredCurrency)
            ? "USD"
            : preferredCurrency.Trim().ToUpperInvariant();

        user.PreferredCurrency = normalizedCurrency;
        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
            throw new Exception("Failed to update preferred currency.");

        var transactions = await _context.Transactions
            .Where(t => t.UserId == userId)
            .ToListAsync();

        foreach (var transaction in transactions)
        {
            transaction.AmountInBaseCurrency = await _exchangeRateService.ConvertAsync(
                transaction.Amount,
                transaction.Currency,
                normalizedCurrency);
        }

        await _context.SaveChangesAsync();

        return await GenerateAuthResponse(user);
    }

    private async Task<AuthResponseDto> GenerateAuthResponse(AppUser user)
    {
        var roles = await _userManager.GetRolesAsync(user);
        var (token, expiry) = GenerateJwtToken(user, roles);
        var refreshToken = GenerateRefreshToken();
        return new AuthResponseDto(token, refreshToken, expiry, user.Id, user.Email!, user.FirstName, user.LastName);
    }

    private (string Token, DateTime Expiry) GenerateJwtToken(AppUser user, IList<string> roles)
    {
        var jwtSettings = _config.GetSection("JwtSettings");
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["SecretKey"]!));
        var expiry = DateTime.UtcNow.AddHours(int.Parse(jwtSettings["ExpiryHours"] ?? "24"));

        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id),
            new Claim(JwtRegisteredClaimNames.Email, user.Email!),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim("firstName", user.FirstName),
            new Claim("lastName", user.LastName),
            new Claim("preferredCurrency", user.PreferredCurrency)
        };
        claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));

        var token = new JwtSecurityToken(
            issuer: jwtSettings["Issuer"],
            audience: jwtSettings["Audience"],
            claims: claims,
            expires: expiry,
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256)
        );

        return (new JwtSecurityTokenHandler().WriteToken(token), expiry);
    }

    private static string GenerateRefreshToken()
    {
        var bytes = new byte[64];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToBase64String(bytes);
    }
}
