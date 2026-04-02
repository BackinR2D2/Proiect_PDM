namespace Proiect_Planificare_Buget.Models;

public sealed class ReportCategoryItem
{
    public string Name { get; init; } = string.Empty;

    public int TransactionCount { get; init; }

    public decimal Income { get; init; }

    public decimal Expense { get; init; }

    public decimal Net => Income - Expense;

    public string IncomeLabel => $"+{Income:N2} RON";

    public string ExpenseLabel => $"-{Expense:N2} RON";

    public string NetLabel => $"{Net:N2} RON";

    public string NetColor => Net >= 0 ? "#0F766E" : "#B91C1C";
}
