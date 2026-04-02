using System.Collections.ObjectModel;
using Proiect_Planificare_Buget.Models;
using Proiect_Planificare_Buget.Services;

namespace Proiect_Planificare_Buget.ViewModels;

public sealed class BudgetsPageViewModel : ViewModelBase
{
    private readonly BudgetDataService _budgetDataService;

    private string _selectedBudgetCategory = string.Empty;
    private string _monthlyLimit = string.Empty;
    private double _alertThresholdPercent = 80;
    private BudgetStatusItem? _selectedBudget;

    public BudgetsPageViewModel(BudgetDataService budgetDataService)
    {
        _budgetDataService = budgetDataService;
        _budgetDataService.DataChanged += async (_, _) => await HandleDataChangedAsync(LoadAsync);

        SaveBudgetCommand = new Command(async () => await SaveBudgetAsync());
        DeleteBudgetCommand = new Command<BudgetStatusItem>(async budget => await DeleteBudgetAsync(budget));
        ResetFormCommand = new Command(ResetForm);
    }

    public ObservableCollection<BudgetStatusItem> Budgets { get; } = [];

    public ObservableCollection<string> BudgetCategoryOptions { get; } = [];

    public ICommand SaveBudgetCommand { get; }

    public ICommand DeleteBudgetCommand { get; }

    public ICommand ResetFormCommand { get; }

    public string SelectedBudgetCategory
    {
        get => _selectedBudgetCategory;
        set => SetProperty(ref _selectedBudgetCategory, value);
    }

    public string MonthlyLimit
    {
        get => _monthlyLimit;
        set => SetProperty(ref _monthlyLimit, value);
    }

    public double AlertThresholdPercent
    {
        get => _alertThresholdPercent;
        set
        {
            if (SetProperty(ref _alertThresholdPercent, value))
            {
                OnPropertyChanged(nameof(AlertThresholdLabel));
            }
        }
    }

    public string AlertThresholdLabel => $"{AlertThresholdPercent:F0}%";

    public BudgetStatusItem? SelectedBudget
    {
        get => _selectedBudget;
        set
        {
            if (SetProperty(ref _selectedBudget, value) && value is not null)
            {
                SelectedBudgetCategory = value.Name;
                MonthlyLimit = value.MonthlyLimit.ToString("N2");
                AlertThresholdPercent = (double)(value.AlertThreshold * 100m);
            }
        }
    }

    public Task LoadAsync()
    {
        return RunBusyOperationAsync(async () =>
        {
            var snapshot = await _budgetDataService.GetSnapshotAsync();
            var selectedBudgetName = SelectedBudget?.Name;
            var expenseCategories = snapshot.Categories
                .Where(category => category.Kind == CategoryKind.Expense)
                .Select(category => category.Name)
                .ToList();

            BudgetCategoryOptions.Clear();
            foreach (var category in expenseCategories)
            {
                BudgetCategoryOptions.Add(category);
            }

            var monthStart = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            var monthEnd = monthStart.AddMonths(1);
            var monthTransactions = snapshot.Transactions
                .Where(transaction => transaction.Type == TransactionType.Expense && transaction.OccurredOn >= monthStart && transaction.OccurredOn < monthEnd)
                .ToList();

            var budgetStatuses = snapshot.Budgets
                .OrderBy(budget => budget.Name)
                .Select(budget => new BudgetStatusItem
                {
                    Name = budget.Name,
                    MonthlyLimit = budget.MonthlyLimit,
                    AlertThreshold = budget.AlertThreshold,
                    Spent = monthTransactions
                        .Where(transaction => transaction.Category == budget.Name)
                        .Sum(transaction => transaction.Amount)
                })
                .ToList();

            Budgets.Clear();
            foreach (var budget in budgetStatuses)
            {
                Budgets.Add(budget);
            }

            SelectedBudget = budgetStatuses.FirstOrDefault(budget =>
                string.Equals(budget.Name, selectedBudgetName, StringComparison.OrdinalIgnoreCase));

            if (!BudgetCategoryOptions.Contains(SelectedBudgetCategory))
            {
                SelectedBudgetCategory = BudgetCategoryOptions.FirstOrDefault() ?? string.Empty;
            }

            if (SelectedBudget is null && string.IsNullOrWhiteSpace(MonthlyLimit) && budgetStatuses.Count > 0)
            {
                SelectedBudgetCategory = budgetStatuses[0].Name;
                MonthlyLimit = budgetStatuses[0].MonthlyLimit.ToString("N2");
                AlertThresholdPercent = (double)(budgetStatuses[0].AlertThreshold * 100m);
            }
        }, errorPrefix: "Nu am putut incarca bugetele");
    }

    private async Task SaveBudgetAsync()
    {
        await RunBusyOperationAsync(async () =>
        {
            if (string.IsNullOrWhiteSpace(SelectedBudgetCategory))
                throw new InvalidOperationException("Adauga mai intai o categorie de cheltuiala in pagina Categorii.");

            await _budgetDataService.SaveBudgetAsync(SelectedBudgetCategory, MonthlyLimit, AlertThresholdPercent);
            ResetForm();
        }, successMessage: "Bugetul a fost actualizat.", errorPrefix: "Nu am putut salva bugetul");
    }

    private async Task DeleteBudgetAsync(BudgetStatusItem? budget)
    {
        if (budget is null)
            return;

        await RunBusyOperationAsync(async () =>
        {
            await _budgetDataService.DeleteBudgetAsync(budget.Name);

            if (SelectedBudget?.Name == budget.Name)
            {
                ResetForm();
            }
        }, successMessage: "Bugetul a fost sters.", errorPrefix: "Nu am putut sterge bugetul");
    }

    private void ResetForm()
    {
        SelectedBudget = null;
        SelectedBudgetCategory = BudgetCategoryOptions.FirstOrDefault() ?? string.Empty;
        MonthlyLimit = string.Empty;
        AlertThresholdPercent = 80;
    }
}
