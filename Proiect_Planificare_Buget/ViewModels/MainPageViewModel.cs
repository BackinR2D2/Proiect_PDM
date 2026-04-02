using System.Collections.ObjectModel;
using Proiect_Planificare_Buget.Models;
using Proiect_Planificare_Buget.Services;

namespace Proiect_Planificare_Buget.ViewModels;

public sealed class MainPageViewModel : ViewModelBase
{
    private const double ChartBarMaxWidth = 180;

    private readonly BudgetDataService _budgetDataService;

    private string _greeting = "Salut!";
    private string _incomeSummary = "0.00 RON";
    private string _expenseSummary = "0.00 RON";
    private string _balanceSummary = "0.00 RON";
    private string _goalSummary = "0.00 RON";
    private string _monthLabel = string.Empty;
    private string _savingsRateSummary = "0%";
    private string _savingsRateCaption = "Nu exista suficiente date pentru calcul.";
    private string _savingsRateAccentColor = "#1D4ED8";
    private double _savingsRateProgress;
    private string _averageExpenseSummary = "0.00 RON";
    private string _averageExpenseCaption = "Nu exista cheltuieli in luna curenta.";
    private string _largestExpenseSummary = "Fara cheltuieli";
    private string _largestExpenseCaption = "Adauga tranzactii pentru a evidentia cheltuiala dominanta.";
    private string _budgetPressureSummary = "0 bugete in alerta";
    private string _budgetPressureCaption = "Toate bugetele sunt in grafic.";
    private string _budgetPressureAccentColor = "#0F766E";
    private double _budgetPressureProgress;

    public MainPageViewModel(BudgetDataService budgetDataService)
    {
        _budgetDataService = budgetDataService;
        _budgetDataService.DataChanged += async (_, _) => await HandleDataChangedAsync(LoadAsync);

        OpenTransactionsCommand = new Command(async () => await Shell.Current.GoToAsync("//transactions"));
        OpenInsightsCommand = new Command(async () => await Shell.Current.GoToAsync("//insights"));
    }

    public ObservableCollection<TransactionRecord> RecentTransactions { get; } = [];

    public ObservableCollection<BudgetStatusItem> BudgetHighlights { get; } = [];

    public ObservableCollection<OverviewMonthlyTrendItem> MonthlyTrend { get; } = [];

    public ObservableCollection<OverviewExpenseCategoryItem> TopExpenseCategories { get; } = [];

    public ICommand OpenTransactionsCommand { get; }

    public ICommand OpenInsightsCommand { get; }

    public string Greeting
    {
        get => _greeting;
        private set => SetProperty(ref _greeting, value);
    }

    public string IncomeSummary
    {
        get => _incomeSummary;
        private set => SetProperty(ref _incomeSummary, value);
    }

    public string ExpenseSummary
    {
        get => _expenseSummary;
        private set => SetProperty(ref _expenseSummary, value);
    }

    public string BalanceSummary
    {
        get => _balanceSummary;
        private set => SetProperty(ref _balanceSummary, value);
    }

    public string GoalSummary
    {
        get => _goalSummary;
        private set => SetProperty(ref _goalSummary, value);
    }

    public string MonthLabel
    {
        get => _monthLabel;
        private set => SetProperty(ref _monthLabel, value);
    }

    public string SavingsRateSummary
    {
        get => _savingsRateSummary;
        private set => SetProperty(ref _savingsRateSummary, value);
    }

    public string SavingsRateCaption
    {
        get => _savingsRateCaption;
        private set => SetProperty(ref _savingsRateCaption, value);
    }

    public string SavingsRateAccentColor
    {
        get => _savingsRateAccentColor;
        private set => SetProperty(ref _savingsRateAccentColor, value);
    }

    public double SavingsRateProgress
    {
        get => _savingsRateProgress;
        private set => SetProperty(ref _savingsRateProgress, value);
    }

    public string AverageExpenseSummary
    {
        get => _averageExpenseSummary;
        private set => SetProperty(ref _averageExpenseSummary, value);
    }

    public string AverageExpenseCaption
    {
        get => _averageExpenseCaption;
        private set => SetProperty(ref _averageExpenseCaption, value);
    }

    public string LargestExpenseSummary
    {
        get => _largestExpenseSummary;
        private set => SetProperty(ref _largestExpenseSummary, value);
    }

    public string LargestExpenseCaption
    {
        get => _largestExpenseCaption;
        private set => SetProperty(ref _largestExpenseCaption, value);
    }

    public string BudgetPressureSummary
    {
        get => _budgetPressureSummary;
        private set => SetProperty(ref _budgetPressureSummary, value);
    }

    public string BudgetPressureCaption
    {
        get => _budgetPressureCaption;
        private set => SetProperty(ref _budgetPressureCaption, value);
    }

    public string BudgetPressureAccentColor
    {
        get => _budgetPressureAccentColor;
        private set => SetProperty(ref _budgetPressureAccentColor, value);
    }

    public double BudgetPressureProgress
    {
        get => _budgetPressureProgress;
        private set => SetProperty(ref _budgetPressureProgress, value);
    }

    public Task LoadAsync()
    {
        return RunBusyOperationAsync(async () =>
        {
            var snapshot = await _budgetDataService.GetSnapshotAsync();
            var monthStart = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            var monthEnd = monthStart.AddMonths(1);
            var monthTransactions = snapshot.Transactions
                .Where(transaction => transaction.OccurredOn >= monthStart && transaction.OccurredOn < monthEnd)
                .ToList();

            var income = monthTransactions
                .Where(transaction => transaction.Type == TransactionType.Income)
                .Sum(transaction => transaction.Amount);

            var expenses = monthTransactions
                .Where(transaction => transaction.Type == TransactionType.Expense)
                .Sum(transaction => transaction.Amount);

            var expenseTransactions = monthTransactions
                .Where(transaction => transaction.Type == TransactionType.Expense)
                .ToList();

            //Greeting = string.IsNullOrWhiteSpace(snapshot.Settings.FullName)
            //    ? "Salut!"
            //    : $"Salut, {snapshot.Settings.FullName}!";
            MonthLabel = $"Rezumat pentru {monthStart:MMMM yyyy}";
            IncomeSummary = FormatAmount(income);
            ExpenseSummary = FormatAmount(expenses);
            BalanceSummary = FormatAmount(income - expenses);
            GoalSummary = FormatAmount(snapshot.Goals.Sum(goal => goal.CurrentAmount));

            UpdateInsightCards(income, expenses, expenseTransactions, snapshot);

            RecentTransactions.Clear();
            foreach (var transaction in snapshot.Transactions.OrderByDescending(item => item.OccurredOn).Take(5))
            {
                RecentTransactions.Add(transaction);
            }

            var budgetStatuses = snapshot.Budgets
                .Select(budget => new BudgetStatusItem
                {
                    Name = budget.Name,
                    MonthlyLimit = budget.MonthlyLimit,
                    AlertThreshold = budget.AlertThreshold,
                    Spent = monthTransactions
                        .Where(transaction => transaction.Type == TransactionType.Expense && transaction.Category == budget.Name)
                        .Sum(transaction => transaction.Amount)
                })
                .OrderByDescending(status => status.Progress)
                .Take(3)
                .ToList();

            BudgetHighlights.Clear();
            foreach (var budget in budgetStatuses)
            {
                BudgetHighlights.Add(budget);
            }

            RefreshMonthlyTrend(snapshot.Transactions, monthStart);
            RefreshTopExpenseCategories(expenseTransactions);
        }, errorPrefix: "Nu am putut incarca tabloul de bord");
    }

    private void UpdateInsightCards(decimal income, decimal expenses, List<TransactionRecord> expenseTransactions, BudgetAppData snapshot)
    {
        var savingsRate = income <= 0 ? 0m : (income - expenses) / income;
        var positiveSavingsRate = Math.Max(savingsRate, 0m);
        var largestExpense = expenseTransactions
            .OrderByDescending(transaction => transaction.Amount)
            .FirstOrDefault();

        var monthStart = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        var monthEnd = monthStart.AddMonths(1);
        var budgetStatuses = snapshot.Budgets
            .Select(budget => new BudgetStatusItem
            {
                Name = budget.Name,
                MonthlyLimit = budget.MonthlyLimit,
                AlertThreshold = budget.AlertThreshold,
                Spent = snapshot.Transactions
                    .Where(transaction =>
                        transaction.Type == TransactionType.Expense
                        && transaction.Category == budget.Name
                        && transaction.OccurredOn >= monthStart
                        && transaction.OccurredOn < monthEnd)
                    .Sum(transaction => transaction.Amount)
            })
            .ToList();

        var budgetsAtRisk = budgetStatuses.Count(status => status.IsNearLimit || status.IsOverBudget);
        var overBudget = budgetStatuses.Count(status => status.IsOverBudget);

        SavingsRateSummary = income <= 0 ? "0%" : $"{savingsRate:P0}";
        SavingsRateCaption = savingsRate >= 0
            ? $"Ai pastrat {positiveSavingsRate:P0} din venituri dupa cheltuieli."
            : $"Cheltuielile au depasit veniturile cu {Math.Abs(savingsRate):P0}.";
        SavingsRateAccentColor = savingsRate >= 0 ? "#1D4ED8" : "#B91C1C";
        SavingsRateProgress = income <= 0 ? 0 : Math.Clamp((double)positiveSavingsRate, 0, 1);

        AverageExpenseSummary = expenseTransactions.Count == 0
            ? "0.00 RON"
            : FormatAmount(expenseTransactions.Average(transaction => transaction.Amount));
        AverageExpenseCaption = expenseTransactions.Count == 0
            ? "Nu exista cheltuieli inregistrate in luna curenta."
            : $"{expenseTransactions.Count} cheltuieli analizate in luna curenta.";

        LargestExpenseSummary = largestExpense?.Title ?? "Fara cheltuieli";
        LargestExpenseCaption = largestExpense is null
            ? "Adauga tranzactii pentru a evidentia cheltuiala dominanta."
            : $"{largestExpense.Amount:N2} RON in categoria {largestExpense.Category}.";

        BudgetPressureSummary = budgetStatuses.Count == 0
            ? "Fara bugete active"
            : overBudget > 0
                ? $"{overBudget} bugete depasite"
                : $"{budgetsAtRisk} bugete in alerta";
        BudgetPressureCaption = budgetStatuses.Count == 0
            ? "Configureaza bugete pentru monitorizare automata."
            : budgetsAtRisk == 0
                ? "Toate bugetele sunt in grafic in acest moment."
                : $"{budgetsAtRisk} din {budgetStatuses.Count} bugete necesita atentie acum.";
        BudgetPressureAccentColor = overBudget > 0
            ? "#B91C1C"
            : budgetsAtRisk > 0
                ? "#D97706"
                : "#0F766E";
        BudgetPressureProgress = budgetStatuses.Count == 0
            ? 0
            : (double)budgetsAtRisk / budgetStatuses.Count;
    }

    private void RefreshMonthlyTrend(IEnumerable<TransactionRecord> transactions, DateTime currentMonthStart)
    {
        var monthlyGroups = Enumerable.Range(0, 6)
            .Select(offset => currentMonthStart.AddMonths(offset - 5))
            .Select(monthStart =>
            {
                var monthEnd = monthStart.AddMonths(1);
                var monthTransactions = transactions
                    .Where(transaction => transaction.OccurredOn >= monthStart && transaction.OccurredOn < monthEnd)
                    .ToList();

                return new
                {
                    MonthStart = monthStart,
                    Income = monthTransactions
                        .Where(transaction => transaction.Type == TransactionType.Income)
                        .Sum(transaction => transaction.Amount),
                    Expense = monthTransactions
                        .Where(transaction => transaction.Type == TransactionType.Expense)
                        .Sum(transaction => transaction.Amount)
                };
            })
            .ToList();

        var maxValue = monthlyGroups
            .SelectMany(item => new[] { item.Income, item.Expense })
            .DefaultIfEmpty(1m)
            .Max();

        MonthlyTrend.Clear();
        foreach (var item in monthlyGroups)
        {
            MonthlyTrend.Add(new OverviewMonthlyTrendItem
            {
                MonthLabel = item.MonthStart.ToString("MMM", System.Globalization.CultureInfo.CurrentCulture),
                Income = item.Income,
                Expense = item.Expense,
                IncomeBarWidth = ScaleBarWidth(item.Income, maxValue),
                ExpenseBarWidth = ScaleBarWidth(item.Expense, maxValue)
            });
        }
    }

    private void RefreshTopExpenseCategories(IEnumerable<TransactionRecord> expenseTransactions)
    {
        var palette = new[]
        {
            "#DC2626",
            "#EA580C",
            "#D97706",
            "#16A34A",
            "#2563EB"
        };

        var categoryTotals = expenseTransactions
            .GroupBy(transaction => transaction.Category)
            .Select(group => new
            {
                Name = group.Key,
                Amount = group.Sum(transaction => transaction.Amount)
            })
            .OrderByDescending(item => item.Amount)
            .Take(5)
            .ToList();

        var maxAmount = categoryTotals
            .Select(item => item.Amount)
            .DefaultIfEmpty(1m)
            .Max();

        TopExpenseCategories.Clear();
        for (var index = 0; index < categoryTotals.Count; index++)
        {
            var item = categoryTotals[index];
            var totalAmount = categoryTotals.Sum(category => category.Amount);

            TopExpenseCategories.Add(new OverviewExpenseCategoryItem
            {
                Name = item.Name,
                Amount = item.Amount,
                Share = totalAmount <= 0 ? 0 : (double)(item.Amount / totalAmount),
                BarWidth = ScaleBarWidth(item.Amount, maxAmount),
                AccentColor = palette[index % palette.Length]
            });
        }
    }

    private static string FormatAmount(decimal amount)
    {
        return $"{amount:N2} RON";
    }

    private static double ScaleBarWidth(decimal value, decimal maxValue)
    {
        if (value <= 0 || maxValue <= 0)
            return 0;

        var scaledWidth = (double)(value / maxValue) * ChartBarMaxWidth;
        return Math.Max(12, scaledWidth);
    }
}
