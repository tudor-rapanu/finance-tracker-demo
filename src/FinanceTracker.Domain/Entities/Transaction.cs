using FinanceTracker.Domain.Common;
using FinanceTracker.Domain.Enums;

namespace FinanceTracker.Domain.Entities;

public class Transaction : BaseEntity
{
    public string UserId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "USD";
    public decimal AmountInBaseCurrency { get; set; }
    public TransactionType Type { get; set; }
    public TransactionCategory Category { get; set; }
    public string Description { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public string? Notes { get; set; }
    public AppUser User { get; set; } = null!;
}
