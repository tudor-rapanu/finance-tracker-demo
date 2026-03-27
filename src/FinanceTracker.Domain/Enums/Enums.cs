namespace FinanceTracker.Domain.Enums;

public enum TransactionType
{
    Income = 1,
    Expense = 2
}

public enum TransactionCategory
{
    // Income
    Salary = 1,
    Freelance = 2,
    Investment = 3,
    Gift = 4,
    OtherIncome = 5,

    // Expense
    Housing = 10,
    Food = 11,
    Transport = 12,
    Healthcare = 13,
    Entertainment = 14,
    Shopping = 15,
    Utilities = 16,
    Education = 17,
    Travel = 18,
    OtherExpense = 19
}
