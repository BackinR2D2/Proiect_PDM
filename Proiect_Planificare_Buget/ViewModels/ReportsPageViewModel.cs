using System.Collections.ObjectModel;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Storage;
using Proiect_Planificare_Buget.Models;
using Proiect_Planificare_Buget.Services;

namespace Proiect_Planificare_Buget.ViewModels;

public sealed class ReportsPageViewModel : ViewModelBase
{
    private readonly BudgetDataService _budgetDataService;
    private readonly XmlReportService _xmlReportService;

    private DateTime _startDate = new(DateTime.Today.Year, DateTime.Today.Month, 1);
    private DateTime _endDate = DateTime.Today;
    private string _incomeSummary = "0.00 RON";
    private string _expenseSummary = "0.00 RON";
    private string _netSummary = "0.00 RON";
    private string _transactionCountSummary = "0";
    private string _exportedReportPath = "Raportul XML nu a fost generat inca.";

    public ReportsPageViewModel(BudgetDataService budgetDataService, XmlReportService xmlReportService)
    {
        _budgetDataService = budgetDataService;
        _xmlReportService = xmlReportService;
        _budgetDataService.DataChanged += async (_, _) => await HandleDataChangedAsync(LoadAsync);

        GenerateReportCommand = new Command(async () => await GenerateReportAsync());
        CurrentMonthCommand = new Command(SetCurrentMonth);
    }

    public ObservableCollection<ReportCategoryItem> CategoryBreakdown { get; } = [];

    public ObservableCollection<TransactionRecord> TransactionsInRange { get; } = [];

    public ICommand GenerateReportCommand { get; }

    public ICommand CurrentMonthCommand { get; }

    public DateTime StartDate
    {
        get => _startDate;
        set => SetProperty(ref _startDate, value);
    }

    public DateTime EndDate
    {
        get => _endDate;
        set => SetProperty(ref _endDate, value);
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

    public string NetSummary
    {
        get => _netSummary;
        private set => SetProperty(ref _netSummary, value);
    }

    public string TransactionCountSummary
    {
        get => _transactionCountSummary;
        private set => SetProperty(ref _transactionCountSummary, value);
    }

    public string ExportedReportPath
    {
        get => _exportedReportPath;
        private set => SetProperty(ref _exportedReportPath, value);
    }

    public Task LoadAsync()
    {
        return RunBusyOperationAsync(
            async () => await RunReportAsync(updateCollections: true),
            errorPrefix: "Nu am putut genera raportul");
    }

    private async Task GenerateReportAsync()
    {
        await RunBusyOperationAsync(async () =>
        {
            var reportData = await RefreshAsync();
            var reportPath = await _xmlReportService.ExportAsync(reportData.Snapshot, reportData.BudgetStatuses);
            ExportedReportPath = reportPath;

            var reportOpened = await TryOpenReportAsync(reportPath);
            var successMessage = reportOpened
                ? "Raportul XML a fost exportat si deschis cu succes."
                : "Raportul XML a fost exportat cu succes.";

            StatusMessage = successMessage;
            await ShowSuccessAlertAsync(successMessage, reportPath);
        }, errorPrefix: "Nu am putut genera raportul XML");
    }

    private Task<ReportGenerationData> RefreshAsync()
    {
        return RunReportAsync(updateCollections: true);
    }

    private async Task<ReportGenerationData> RunReportAsync(bool updateCollections)
    {
        if (EndDate < StartDate)
            throw new InvalidOperationException("Data de sfarsit trebuie sa fie dupa data de inceput.");

        var snapshot = await _budgetDataService.GetSnapshotAsync();
        var endExclusive = EndDate.Date.AddDays(1);
        var transactions = snapshot.Transactions
            .Where(transaction => transaction.OccurredOn >= StartDate.Date && transaction.OccurredOn < endExclusive)
            .OrderByDescending(transaction => transaction.OccurredOn)
            .ToList();

        var income = transactions
            .Where(transaction => transaction.Type == TransactionType.Income)
            .Sum(transaction => transaction.Amount);

        var expense = transactions
            .Where(transaction => transaction.Type == TransactionType.Expense)
            .Sum(transaction => transaction.Amount);

        var breakdown = transactions
            .GroupBy(transaction => transaction.Category)
            .Select(group => new ReportCategoryItem
            {
                Name = group.Key,
                TransactionCount = group.Count(),
                Income = group.Where(item => item.Type == TransactionType.Income).Sum(item => item.Amount),
                Expense = group.Where(item => item.Type == TransactionType.Expense).Sum(item => item.Amount)
            })
            .OrderByDescending(item => Math.Abs(item.Net))
            .ThenBy(item => item.Name)
            .ToList();

        var budgetStatuses = snapshot.Budgets
            .OrderBy(budget => budget.Name)
            .Select(budget => new BudgetStatusItem
            {
                Name = budget.Name,
                MonthlyLimit = budget.MonthlyLimit,
                AlertThreshold = budget.AlertThreshold,
                Spent = transactions
                    .Where(transaction => transaction.Type == TransactionType.Expense && transaction.Category == budget.Name)
                    .Sum(transaction => transaction.Amount)
            })
            .ToList();

        IncomeSummary = $"{income:N2} RON";
        ExpenseSummary = $"{expense:N2} RON";
        NetSummary = $"{income - expense:N2} RON";
        TransactionCountSummary = transactions.Count.ToString();

        if (updateCollections)
        {
            CategoryBreakdown.Clear();
            foreach (var item in breakdown)
            {
                CategoryBreakdown.Add(item);
            }

            TransactionsInRange.Clear();
            foreach (var transaction in transactions.Take(8))
            {
                TransactionsInRange.Add(transaction);
            }
        }

        var filteredSnapshot = new BudgetAppData
        {
            Settings = snapshot.Settings,
            Categories = snapshot.Categories,
            Budgets = snapshot.Budgets,
            Goals = snapshot.Goals,
            Transactions = transactions
        };

        return new ReportGenerationData(filteredSnapshot, budgetStatuses);
    }

    private async Task<bool> TryOpenReportAsync(string reportPath)
    {
        try
        {
            return await Launcher.Default.OpenAsync(new OpenFileRequest(
                "Raport XML",
                new ReadOnlyFile(reportPath)));
        }
        catch
        {
            return false;
        }
    }

    private static Task ShowSuccessAlertAsync(string message, string reportPath)
    {
        return MainThread.InvokeOnMainThreadAsync(async () =>
        {
            if (Shell.Current is null)
                return;

            await Shell.Current.DisplayAlertAsync(
                "Raport exportat",
                $"{message}\n\nFisier: {reportPath}",
                "OK");
        });
    }

    private void SetCurrentMonth()
    {
        StartDate = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        EndDate = DateTime.Today;
    }

    private sealed record ReportGenerationData(BudgetAppData Snapshot, IReadOnlyList<BudgetStatusItem> BudgetStatuses);
}
