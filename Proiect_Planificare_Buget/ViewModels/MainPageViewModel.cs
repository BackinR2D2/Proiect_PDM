using System.Collections.ObjectModel;
using Proiect_Planificare_Buget.Models;
using Proiect_Planificare_Buget.Services;

namespace Proiect_Planificare_Buget.ViewModels;

public sealed class MainPageViewModel : ViewModelBase
{
    private readonly BudgetDataService _budgetDataService;

    private string _greeting = "Salut!";
    private string _incomeSummary = "0.00 RON";
    private string _expenseSummary = "0.00 RON";
    private string _balanceSummary = "0.00 RON";
    private string _goalSummary = "0.00 RON";
    private string _monthLabel = string.Empty;

    public MainPageViewModel(BudgetDataService budgetDataService)
    {
        _budgetDataService = budgetDataService;
        _budgetDataService.DataChanged += async (_, _) => await LoadAsync();

        OpenTransactionsCommand = new Command(async () => await Shell.Current.GoToAsync("//transactions"));
        OpenInsightsCommand = new Command(async () => await Shell.Current.GoToAsync("//insights"));
    }

    public ObservableCollection<TransactionRecord> RecentTransactions { get; } = [];

    public ObservableCollection<BudgetStatusItem> BudgetHighlights { get; } = [];

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

            Greeting = $"Salut, {snapshot.Settings.FullName}!";
            MonthLabel = monthStart.ToString("MMMM yyyy");
            IncomeSummary = FormatAmount(income);
            ExpenseSummary = FormatAmount(expenses);
            BalanceSummary = FormatAmount(income - expenses);
            GoalSummary = FormatAmount(snapshot.Goals.Sum(goal => goal.CurrentAmount));

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
        }, errorPrefix: "Nu am putut incarca tabloul de bord");
    }

    private static string FormatAmount(decimal amount)
    {
        return $"{amount:N2} RON";
    }
}
