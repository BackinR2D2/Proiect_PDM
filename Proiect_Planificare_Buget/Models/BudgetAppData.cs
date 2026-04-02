namespace Proiect_Planificare_Buget.Models;

public sealed class BudgetAppData
{
    public List<TransactionRecord> Transactions { get; set; } = [];

    public List<BudgetCategory> Budgets { get; set; } = [];

    public List<SavingsGoal> Goals { get; set; } = [];

    public List<CategoryDefinition> Categories { get; set; } = [];

    public AppSettings Settings { get; set; } = new();

    public static BudgetAppData CreateSample()
    {
        var now = DateTime.Now;

        return new BudgetAppData
        {
            Settings = new AppSettings
            {
                FullName = "Echipa Budget Planner",
                DefaultCurrency = "RON",
                WeekStartsOn = "Luni",
                AutoSyncRates = true,
                ReminderDay = 7
            },
            Categories =
            [
                new CategoryDefinition { Name = "Mancare", Kind = CategoryKind.Expense },
                new CategoryDefinition { Name = "Transport", Kind = CategoryKind.Expense },
                new CategoryDefinition { Name = "Utilitati", Kind = CategoryKind.Expense },
                new CategoryDefinition { Name = "Sanatate", Kind = CategoryKind.Expense },
                new CategoryDefinition { Name = "Educatie", Kind = CategoryKind.Expense },
                new CategoryDefinition { Name = "Timp liber", Kind = CategoryKind.Expense },
                new CategoryDefinition { Name = "Cumparaturi", Kind = CategoryKind.Expense },
                new CategoryDefinition { Name = "Economii", Kind = CategoryKind.Expense },
                new CategoryDefinition { Name = "Altele", Kind = CategoryKind.Expense },
                new CategoryDefinition { Name = "Salariu", Kind = CategoryKind.Income },
                new CategoryDefinition { Name = "Freelance", Kind = CategoryKind.Income },
                new CategoryDefinition { Name = "Bonus", Kind = CategoryKind.Income },
                new CategoryDefinition { Name = "Cadou", Kind = CategoryKind.Income }
            ],
            Budgets =
            [
                new BudgetCategory { Name = "Mancare", MonthlyLimit = 1300m, AlertThreshold = 0.8m },
                new BudgetCategory { Name = "Transport", MonthlyLimit = 550m, AlertThreshold = 0.75m },
                new BudgetCategory { Name = "Utilitati", MonthlyLimit = 900m, AlertThreshold = 0.9m },
                new BudgetCategory { Name = "Sanatate", MonthlyLimit = 450m, AlertThreshold = 0.7m },
                new BudgetCategory { Name = "Timp liber", MonthlyLimit = 600m, AlertThreshold = 0.8m }
            ],
            Goals =
            [
                new SavingsGoal
                {
                    Title = "Fond de urgenta",
                    TargetAmount = 12000m,
                    CurrentAmount = 4800m,
                    Deadline = new DateTime(now.Year, 12, 15),
                    IsPinned = true
                },
                new SavingsGoal
                {
                    Title = "Vacanta de vara",
                    TargetAmount = 3500m,
                    CurrentAmount = 1400m,
                    Deadline = now.AddMonths(5),
                    IsPinned = false
                }
            ],
            Transactions =
            [
                new TransactionRecord
                {
                    Title = "Salariu",
                    Category = "Salariu",
                    Type = TransactionType.Income,
                    Amount = 7200m,
                    OccurredOn = new DateTime(now.Year, now.Month, 5, 9, 0, 0),
                    Notes = "Venit principal",
                    IsRecurring = true
                },
                new TransactionRecord
                {
                    Title = "Cumparaturi saptamanale",
                    Category = "Mancare",
                    Type = TransactionType.Expense,
                    Amount = 285m,
                    OccurredOn = now.AddDays(-1),
                    Notes = "Supermarket + produse casa"
                },
                new TransactionRecord
                {
                    Title = "Abonament transport",
                    Category = "Transport",
                    Type = TransactionType.Expense,
                    Amount = 150m,
                    OccurredOn = now.AddDays(-3),
                    Notes = "Metrou si autobuz",
                    IsRecurring = true
                },
                new TransactionRecord
                {
                    Title = "Factura electricitate",
                    Category = "Utilitati",
                    Type = TransactionType.Expense,
                    Amount = 320m,
                    OccurredOn = now.AddDays(-5),
                    Notes = "Factura lunara"
                },
                new TransactionRecord
                {
                    Title = "Freelancing",
                    Category = "Freelance",
                    Type = TransactionType.Income,
                    Amount = 950m,
                    OccurredOn = now.AddDays(-7),
                    Notes = "Proiect landing page"
                },
                new TransactionRecord
                {
                    Title = "Farmacie",
                    Category = "Sanatate",
                    Type = TransactionType.Expense,
                    Amount = 124m,
                    OccurredOn = now.AddDays(-8),
                    Notes = "Suplimente si medicamente"
                },
                new TransactionRecord
                {
                    Title = "Cinema",
                    Category = "Timp liber",
                    Type = TransactionType.Expense,
                    Amount = 78m,
                    OccurredOn = now.AddDays(-10),
                    Notes = "2 bilete"
                },
                new TransactionRecord
                {
                    Title = "Transfer economii",
                    Category = "Economii",
                    Type = TransactionType.Expense,
                    Amount = 400m,
                    OccurredOn = now.AddDays(-12),
                    Notes = "Contributie lunara",
                    IsRecurring = true
                }
            ]
        };
    }
}
