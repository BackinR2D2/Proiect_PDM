namespace Proiect_Planificare_Buget.Models;

public sealed class BudgetCategory
{
    public string Name { get; set; } = string.Empty;

    public decimal MonthlyLimit { get; set; }

    public decimal AlertThreshold { get; set; } = 0.8m;
}
