using System.Collections.ObjectModel;
using Proiect_Planificare_Buget.Models;
using Proiect_Planificare_Buget.Services;

namespace Proiect_Planificare_Buget.ViewModels;

public sealed class InsightsPageViewModel : ViewModelBase
{
    private readonly BudgetDataService _budgetDataService;
    private readonly ExchangeRateService _exchangeRateService;
    private readonly XmlReportService _xmlReportService;

    private string _selectedBaseCurrency = "RON";
    private string _lastSyncLabel = "Nesincronizat";
    private string _exportedReportPath = "Raportul XML nu a fost generat inca.";

    public InsightsPageViewModel(
        BudgetDataService budgetDataService,
        ExchangeRateService exchangeRateService,
        XmlReportService xmlReportService)
    {
        _budgetDataService = budgetDataService;
        _exchangeRateService = exchangeRateService;
        _xmlReportService = xmlReportService;
        _budgetDataService.DataChanged += async (_, _) => await HandleDataChangedAsync(LoadAsync);

        RefreshRatesCommand = new Command(async () => await RefreshRatesAsync());
        ExportReportCommand = new Command(async () => await ExportReportAsync());

        Task.Run(async () =>
        {
            await Task.Delay(100);
            await MainThread.InvokeOnMainThreadAsync(async () => await LoadAsync());
        });
    }

    public ObservableCollection<ExchangeRateItem> ExchangeRates { get; } = [];

    public ObservableCollection<BudgetStatusItem> PressureBudgets { get; } = [];

    public IReadOnlyList<string> BaseCurrencyOptions => _budgetDataService.SupportedCurrencies;

    public ICommand RefreshRatesCommand { get; }

    public ICommand ExportReportCommand { get; }

    public string SelectedBaseCurrency
    {
        get => _selectedBaseCurrency;
        set => SetProperty(ref _selectedBaseCurrency, value);
    }

    public string LastSyncLabel
    {
        get => _lastSyncLabel;
        private set => SetProperty(ref _lastSyncLabel, value);
    }

    public string ExportedReportPath
    {
        get => _exportedReportPath;
        private set => SetProperty(ref _exportedReportPath, value);
    }

    public Task LoadAsync()
    {
        return RunBusyOperationAsync(async () =>
        {
            var snapshot = await _budgetDataService.GetSnapshotAsync();
            SelectedBaseCurrency = snapshot.Settings.DefaultCurrency;

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
                .OrderByDescending(status => status.Progress)
                .Take(4)
                .ToList();

            PressureBudgets.Clear();
            foreach (var budget in budgetStatuses)
            {
                PressureBudgets.Add(budget);
            }
        }, errorPrefix: "Nu am putut incarca insight-urile");
    }

    private async Task RefreshRatesAsync()
    {
        await RunBusyOperationAsync(async () =>
        {
            var rates = await _exchangeRateService.GetExchangeRatesAsync(SelectedBaseCurrency);
            ExchangeRates.Clear();

            foreach (var rate in rates)
            {
                ExchangeRates.Add(rate);
            }

            LastSyncLabel = $"Actualizat la {DateTime.Now:dd MMM yyyy, HH:mm}";
        }, successMessage: "Cursurile valutare au fost actualizate.", errorPrefix: "Nu am putut sincroniza cursurile");
    }

    private async Task ExportReportAsync()
    {
        string? reportPath = null;

        await RunBusyOperationAsync(async () =>
        {
            var snapshot = await _budgetDataService.GetSnapshotAsync();
            reportPath = await _xmlReportService.ExportAsync(snapshot, PressureBudgets);
            ExportedReportPath = reportPath;
        }, successMessage: "Raportul XML a fost generat.", errorPrefix: "Nu am putut genera raportul XML");

        if (reportPath is null) return;

        var open = await Shell.Current.DisplayAlert(
            "Raport generat",
            "Raportul XML a fost salvat. Vrei sa il deschizi acum?",
            "Deschide",
            "Nu");

        if (open)
        {
            await Launcher.Default.OpenAsync(new OpenFileRequest
            {
                File = new ReadOnlyFile(reportPath)
            });
        }
    }
}
