namespace Proiect_Planificare_Buget.Models;

public sealed class OverviewMonthlyTrendItem
{
    public string MonthLabel { get; init; } = string.Empty;

    public decimal Income { get; init; }

    public decimal Expense { get; init; }

    public double IncomeBarWidth { get; init; }

    public double ExpenseBarWidth { get; init; }

    public decimal Net => Income - Expense;

    public string IncomeLabel => $"{Income:N0} RON";

    public string ExpenseLabel => $"{Expense:N0} RON";

    public string NetLabel => Net >= 0
        ? $"+{Net:N0} RON"
        : $"{Net:N0} RON";

    public string NetColor => Net >= 0 ? "#15803D" : "#B91C1C";
}
