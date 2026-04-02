namespace Proiect_Planificare_Buget.Models;

public sealed class CategoryDefinition
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Name { get; set; } = string.Empty;

    public CategoryKind Kind { get; set; }

    public string KindLabel => Kind == CategoryKind.Expense ? "Cheltuiala" : "Venit";

    public string AccentColor => Kind == CategoryKind.Expense ? "#B91C1C" : "#0F766E";
}
