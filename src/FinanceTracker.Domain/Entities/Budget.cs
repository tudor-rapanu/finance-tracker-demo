using FinanceTracker.Domain.Common;
using FinanceTracker.Domain.Enums;

namespace FinanceTracker.Domain.Entities;

public class Budget : BaseEntity
{
    public string UserId { get; set; } = string.Empty;
    public TransactionCategory Category { get; set; }
    public decimal LimitAmount { get; set; }
    public string Currency { get; set; } = "USD";
    public int Month { get; set; }
    public int Year { get; set; }
    public AppUser User { get; set; } = null!;
}
