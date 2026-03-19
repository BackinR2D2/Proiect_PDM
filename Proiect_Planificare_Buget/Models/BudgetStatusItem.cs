namespace Proiect_Planificare_Buget.Models;

public sealed class BudgetStatusItem
{
    public string Name { get; init; } = string.Empty;

    public decimal MonthlyLimit { get; init; }

    public decimal Spent { get; init; }

    public decimal AlertThreshold { get; init; }

    public decimal Remaining => MonthlyLimit - Spent;

    public double Progress => MonthlyLimit <= 0 ? 0 : (double)Math.Min(Spent / MonthlyLimit, 1m);

    public bool IsOverBudget => Remaining < 0;

    public bool IsNearLimit => !IsOverBudget && MonthlyLimit > 0 && Spent / MonthlyLimit >= AlertThreshold;

    public string ProgressLabel => $"{Spent:N2} / {MonthlyLimit:N2} RON";

    public string SpentLabel => $"{Spent:N2} RON";

    public string RemainingAmountLabel => $"{Math.Abs(Remaining):N2} RON";

    public string RemainingCaption => IsOverBudget ? "Depasit cu" : "Ramas";

    public string LimitLabel => $"Limita lunara: {MonthlyLimit:N2} RON";

    public string RemainingLabel => IsOverBudget
        ? $"{Math.Abs(Remaining):N2} RON peste limita"
        : $"{Remaining:N2} RON disponibili";

    public string StatusLabel => IsOverBudget
        ? "Depasit"
        : IsNearLimit
            ? "Aproape de limita"
            : "In grafic";

    public string AccentColor => IsOverBudget
        ? "#B91C1C"
        : IsNearLimit
            ? "#D97706"
            : "#0F766E";
}
