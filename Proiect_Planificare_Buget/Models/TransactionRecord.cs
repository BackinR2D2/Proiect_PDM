namespace Proiect_Planificare_Buget.Models;

public sealed class TransactionRecord
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Title { get; set; } = string.Empty;

    public string Category { get; set; } = string.Empty;

    public TransactionType Type { get; set; }

    public decimal Amount { get; set; }

    public DateTime OccurredOn { get; set; } = DateTime.Now;

    public string Notes { get; set; } = string.Empty;

    public bool IsRecurring { get; set; }

    public string TypeLabel => Type == TransactionType.Expense ? "Cheltuiala" : "Venit";

    public string AmountLabel => $"{(Type == TransactionType.Expense ? "-" : "+")}{Amount:N2} RON";

    public string AmountColor => Type == TransactionType.Expense ? "#B91C1C" : "#0F766E";

    public string DateLabel => OccurredOn.ToString("dd MMM yyyy, HH:mm");

    public string BadgeColor => Type == TransactionType.Expense ? "#FEE2E2" : "#DCFCE7";

    public string BadgeTextColor => Type == TransactionType.Expense ? "#B91C1C" : "#0F766E";

    public string TypeIcon => Type == TransactionType.Expense ? "\uE8E3" : "\uE8E5";
}
