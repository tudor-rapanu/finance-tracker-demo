using System.Security.Claims;
using FinanceTracker.Application.Interfaces;

namespace FinanceTracker.API.Extensions;

public class CurrentUserService : ICurrentUserService
{
    public string? UserId { get; }
    public string? Email { get; }
    public string? PreferredCurrency { get; }

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        var user = httpContextAccessor.HttpContext?.User;
        UserId = user?.FindFirstValue(ClaimTypes.NameIdentifier)
              ?? user?.FindFirstValue("sub");
        Email = user?.FindFirstValue(ClaimTypes.Email)
             ?? user?.FindFirstValue("email");
        PreferredCurrency = user?.FindFirstValue("preferredCurrency");
    }
}
