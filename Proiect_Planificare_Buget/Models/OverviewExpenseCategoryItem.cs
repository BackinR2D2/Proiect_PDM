namespace Proiect_Planificare_Buget.Models;

public sealed class OverviewExpenseCategoryItem
{
    public string Name { get; init; } = string.Empty;

    public decimal Amount { get; init; }

    public double Share { get; init; }

    public double BarWidth { get; init; }

    public string AccentColor { get; init; } = "#DC2626";

    public string AmountLabel => $"{Amount:N2} RON";

    public string ShareLabel => $"{Share:P0}";
}
