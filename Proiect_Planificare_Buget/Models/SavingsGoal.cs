namespace Proiect_Planificare_Buget.Models;

public sealed class SavingsGoal
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Title { get; set; } = string.Empty;

    public decimal TargetAmount { get; set; }

    public decimal CurrentAmount { get; set; }

    public DateTime Deadline { get; set; } = DateTime.Today.AddMonths(3);

    public bool IsPinned { get; set; }

    public double Progress => TargetAmount <= 0 ? 0 : (double)Math.Min(CurrentAmount / TargetAmount, 1m);

    public string ProgressLabel => $"{CurrentAmount:N2} / {TargetAmount:N2} RON";

    public string DeadlineLabel => Deadline.ToString("dd MMM yyyy");

    public string StatusLabel => Progress >= 1
        ? "Completat"
        : Deadline.Date < DateTime.Today
            ? "Intarziat"
            : "Activ";

    public string AccentColor => Progress >= 1
        ? "#0F766E"
        : Deadline.Date < DateTime.Today
            ? "#B91C1C"
            : "#1D4ED8";
}
