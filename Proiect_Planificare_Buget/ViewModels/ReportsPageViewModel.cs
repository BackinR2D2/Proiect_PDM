using System.Collections.ObjectModel;
using System.Globalization;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Storage;
using Proiect_Planificare_Buget.Models;
using Proiect_Planificare_Buget.Services;

namespace Proiect_Planificare_Buget.ViewModels;

public sealed class ReportsPageViewModel : ViewModelBase
{
    private readonly BudgetDataService _budgetDataService;
    private readonly XmlReportService _xmlReportService;
    private readonly CsvReportService _csvReportService;

    private DateTime _startDate = new(DateTime.Today.Year, DateTime.Today.Month, 1);
    private DateTime _endDate = DateTime.Today;
    private string _incomeSummary = "0.00 RON";
    private string _expenseSummary = "0.00 RON";
    private string _netSummary = "0.00 RON";
    private string _transactionCountSummary = "0";
    private string _exportedReportPath = "Raportul XML nu a fost generat inca.";
    private string _exportedCsvPath = "Raportul CSV nu a fost generat inca.";

    public ReportsPageViewModel(BudgetDataService budgetDataService, XmlReportService xmlReportService, CsvReportService csvReportService)
    {
        _budgetDataService = budgetDataService;
        _xmlReportService = xmlReportService;
        _csvReportService = csvReportService;
        _budgetDataService.DataChanged += async (_, _) => await HandleDataChangedAsync(LoadAsync);

        GenerateReportCommand = new Command(async () => await GenerateReportAsync());
        ExportCsvCommand = new Command(async () => await ExportCsvAsync());
        CurrentMonthCommand = new Command(SetCurrentMonth);
    }

    public ObservableCollection<ReportCategoryItem> CategoryBreakdown { get; } = [];

    public ObservableCollection<TransactionRecord> TransactionsInRange { get; } = [];

    public ObservableCollection<OverviewExpenseCategoryItem> ReportExpenseChart { get; } = [];

    public ObservableCollection<OverviewMonthlyTrendItem> ReportTrendChart { get; } = [];

    public ICommand GenerateReportCommand { get; }

    public ICommand ExportCsvCommand { get; }

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

    public string ExportedCsvPath
    {
        get => _exportedCsvPath;
        private set => SetProperty(ref _exportedCsvPath, value);
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
            StatusMessage = "Raportul XML a fost exportat cu succes.";
            await OpenFileAsync(reportPath);
        }, errorPrefix: "Nu am putut genera raportul XML");
    }

    private async Task ExportCsvAsync()
    {
        await RunBusyOperationAsync(async () =>
        {
            var reportData = await RefreshAsync();
            var csvPath = await _csvReportService.ExportAsync(reportData.Snapshot.Transactions);
            ExportedCsvPath = csvPath;
            StatusMessage = "Raportul CSV a fost exportat cu succes.";
            await OpenFileAsync(csvPath);
        }, errorPrefix: "Nu am putut exporta raportul CSV");
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

            RefreshExpenseChart(transactions);
            RefreshTrendChart(transactions, StartDate, EndDate);
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

    private void RefreshExpenseChart(IEnumerable<TransactionRecord> transactions)
    {
        var palette = new[] { "#DC2626", "#EA580C", "#D97706", "#16A34A", "#2563EB" };

        var categoryTotals = transactions
            .Where(t => t.Type == TransactionType.Expense)
            .GroupBy(t => t.Category)
            .Select(g => new { Name = g.Key, Amount = g.Sum(t => t.Amount) })
            .OrderByDescending(x => x.Amount)
            .Take(5)
            .ToList();

        var totalAmount = categoryTotals.Sum(x => x.Amount);
        var maxAmount = categoryTotals.Select(x => x.Amount).DefaultIfEmpty(1m).Max();

        ReportExpenseChart.Clear();
        for (var i = 0; i < categoryTotals.Count; i++)
        {
            var item = categoryTotals[i];
            ReportExpenseChart.Add(new OverviewExpenseCategoryItem
            {
                Name = item.Name,
                Amount = item.Amount,
                Share = totalAmount <= 0 ? 0 : (double)(item.Amount / totalAmount),
                BarWidth = maxAmount <= 0 ? 0 : (double)(item.Amount / maxAmount) * 280,
                AccentColor = palette[i % palette.Length]
            });
        }
    }

    private void RefreshTrendChart(IEnumerable<TransactionRecord> transactions, DateTime start, DateTime end)
    {
        var byMonth = transactions
            .GroupBy(t => new DateTime(t.OccurredOn.Year, t.OccurredOn.Month, 1))
            .ToDictionary(g => g.Key, g => g.ToList());

        var months = new List<DateTime>();
        var cursor = new DateTime(start.Year, start.Month, 1);
        var endMonth = new DateTime(end.Year, end.Month, 1);
        while (cursor <= endMonth)
        {
            months.Add(cursor);
            cursor = cursor.AddMonths(1);
        }

        var allValues = months.SelectMany(m =>
        {
            var ts = byMonth.GetValueOrDefault(m) ?? [];
            return new[]
            {
                ts.Where(t => t.Type == TransactionType.Income).Sum(t => t.Amount),
                ts.Where(t => t.Type == TransactionType.Expense).Sum(t => t.Amount)
            };
        });

        var maxValue = allValues.DefaultIfEmpty(1m).Max();
        if (maxValue <= 0) maxValue = 1;

        ReportTrendChart.Clear();
        foreach (var month in months)
        {
            var ts = byMonth.GetValueOrDefault(month) ?? [];
            var inc = ts.Where(t => t.Type == TransactionType.Income).Sum(t => t.Amount);
            var exp = ts.Where(t => t.Type == TransactionType.Expense).Sum(t => t.Amount);

            ReportTrendChart.Add(new OverviewMonthlyTrendItem
            {
                MonthLabel = month.ToString("MMM", CultureInfo.CurrentCulture),
                Income = inc,
                Expense = exp,
                IncomeBarWidth = (double)(inc / maxValue) * 280,
                ExpenseBarWidth = (double)(exp / maxValue) * 280
            });
        }
    }

    private static async Task OpenFileAsync(string filePath)
    {
        try
        {
            await Launcher.Default.OpenAsync(new Uri("file://" + filePath));
        }
        catch
        {
            // fallback: ignore if platform can't open
        }
    }

    private void SetCurrentMonth()
    {
        StartDate = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        EndDate = DateTime.Today;
    }

    private sealed record ReportGenerationData(BudgetAppData Snapshot, IReadOnlyList<BudgetStatusItem> BudgetStatuses);
}
