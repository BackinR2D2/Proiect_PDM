using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using Proiect_Planificare_Buget.Models;

namespace Proiect_Planificare_Buget.Services;

public sealed class BudgetDataService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly string _filePath = Path.Combine(FileSystem.AppDataDirectory, "budget-planner-data.json");

    private BudgetAppData? _cache;

    public event EventHandler? DataChanged;

    public IReadOnlyList<string> ExpenseCategories { get; } =
    [
        "Mancare",
        "Transport",
        "Utilitati",
        "Sanatate",
        "Educatie",
        "Timp liber",
        "Cumparaturi",
        "Economii"
    ];

    public IReadOnlyList<string> IncomeCategories { get; } =
    [
        "Salariu",
        "Freelance",
        "Bonus",
        "Cadou"
    ];

    public IReadOnlyList<string> SupportedCurrencies { get; } = ["RON", "EUR", "USD"];

    public IReadOnlyList<string> WeekDayOptions { get; } =
    [
        "Luni",
        "Marti",
        "Miercuri",
        "Joi",
        "Vineri",
        "Sambata",
        "Duminica"
    ];

    public string StoragePath => _filePath;

    public async Task<BudgetAppData> GetSnapshotAsync()
    {
        await _gate.WaitAsync();
        try
        {
            await EnsureInitializedAsync();
            return Clone(_cache!);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task AddTransactionAsync(
        string title,
        string category,
        string amountText,
        TransactionType type,
        DateTime selectedDate,
        TimeSpan selectedTime,
        string notes,
        bool isRecurring)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new InvalidOperationException("Titlul tranzactiei este obligatoriu.");
        }

        if (!TryParseAmount(amountText, out var amount) || amount <= 0)
        {
            throw new InvalidOperationException("Introdu o suma valida mai mare decat zero.");
        }

        await _gate.WaitAsync();
        try
        {
            await EnsureInitializedAsync();

            _cache!.Transactions.Add(new TransactionRecord
            {
                Title = title.Trim(),
                Category = category,
                Amount = amount,
                Type = type,
                OccurredOn = selectedDate.Date.Add(selectedTime),
                Notes = notes.Trim(),
                IsRecurring = isRecurring
            });

            await SaveAsync();
        }
        finally
        {
            _gate.Release();
        }

        DataChanged?.Invoke(this, EventArgs.Empty);
    }

    public async Task DeleteTransactionAsync(Guid id)
    {
        await _gate.WaitAsync();
        try
        {
            await EnsureInitializedAsync();

            _cache!.Transactions.RemoveAll(transaction => transaction.Id == id);
            await SaveAsync();
        }
        finally
        {
            _gate.Release();
        }

        DataChanged?.Invoke(this, EventArgs.Empty);
    }

    public async Task SaveBudgetAsync(string category, string monthlyLimitText, double alertThresholdPercent)
    {
        if (!TryParseAmount(monthlyLimitText, out var monthlyLimit) || monthlyLimit <= 0)
        {
            throw new InvalidOperationException("Limita lunara trebuie sa fie o suma valida.");
        }

        var alertThreshold = Math.Clamp((decimal)alertThresholdPercent / 100m, 0.1m, 1m);

        await _gate.WaitAsync();
        try
        {
            await EnsureInitializedAsync();

            var existingBudget = _cache!.Budgets.FirstOrDefault(budget => budget.Name == category);
            if (existingBudget is null)
            {
                _cache.Budgets.Add(new BudgetCategory
                {
                    Name = category,
                    MonthlyLimit = monthlyLimit,
                    AlertThreshold = alertThreshold
                });
            }
            else
            {
                existingBudget.MonthlyLimit = monthlyLimit;
                existingBudget.AlertThreshold = alertThreshold;
            }

            await SaveAsync();
        }
        finally
        {
            _gate.Release();
        }

        DataChanged?.Invoke(this, EventArgs.Empty);
    }

    public async Task SaveGoalAsync(Guid? goalId, string title, string targetAmountText, string currentAmountText, DateTime deadline, bool isPinned)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new InvalidOperationException("Numele obiectivului este obligatoriu.");
        }

        if (!TryParseAmount(targetAmountText, out var targetAmount) || targetAmount <= 0)
        {
            throw new InvalidOperationException("Valoarea tinta trebuie sa fie valida.");
        }

        if (!TryParseAmount(currentAmountText, out var currentAmount) || currentAmount < 0)
        {
            throw new InvalidOperationException("Valoarea economisita trebuie sa fie valida.");
        }

        await _gate.WaitAsync();
        try
        {
            await EnsureInitializedAsync();

            var existingGoal = _cache!.Goals.FirstOrDefault(goal => goal.Id == goalId);
            if (existingGoal is null)
            {
                _cache.Goals.Add(new SavingsGoal
                {
                    Title = title.Trim(),
                    TargetAmount = targetAmount,
                    CurrentAmount = currentAmount,
                    Deadline = deadline,
                    IsPinned = isPinned
                });
            }
            else
            {
                existingGoal.Title = title.Trim();
                existingGoal.TargetAmount = targetAmount;
                existingGoal.CurrentAmount = currentAmount;
                existingGoal.Deadline = deadline;
                existingGoal.IsPinned = isPinned;
            }

            await SaveAsync();
        }
        finally
        {
            _gate.Release();
        }

        DataChanged?.Invoke(this, EventArgs.Empty);
    }

    public async Task DeleteGoalAsync(Guid id)
    {
        await _gate.WaitAsync();
        try
        {
            await EnsureInitializedAsync();

            _cache!.Goals.RemoveAll(goal => goal.Id == id);
            await SaveAsync();
        }
        finally
        {
            _gate.Release();
        }

        DataChanged?.Invoke(this, EventArgs.Empty);
    }

    public async Task SaveSettingsAsync(AppSettings settings)
    {
        await _gate.WaitAsync();
        try
        {
            await EnsureInitializedAsync();

            _cache!.Settings = settings;
            await SaveAsync();
        }
        finally
        {
            _gate.Release();
        }

        DataChanged?.Invoke(this, EventArgs.Empty);
    }

    public async Task ResetSampleDataAsync()
    {
        await _gate.WaitAsync();
        try
        {
            _cache = BudgetAppData.CreateSample();
            await SaveAsync();
        }
        finally
        {
            _gate.Release();
        }

        DataChanged?.Invoke(this, EventArgs.Empty);
    }

    public IEnumerable<string> GetCategoriesFor(TransactionType type)
    {
        return type == TransactionType.Expense ? ExpenseCategories : IncomeCategories;
    }

    private async Task EnsureInitializedAsync()
    {
        if (_cache is not null)
        {
            return;
        }

        Directory.CreateDirectory(Path.GetDirectoryName(_filePath)!);

        if (File.Exists(_filePath))
        {
            var content = await File.ReadAllTextAsync(_filePath);
            _cache = JsonSerializer.Deserialize<BudgetAppData>(content, JsonOptions) ?? BudgetAppData.CreateSample();
            return;
        }

        _cache = BudgetAppData.CreateSample();
        await SaveAsync();
    }

    private async Task SaveAsync()
    {
        var json = JsonSerializer.Serialize(_cache, JsonOptions);
        await File.WriteAllTextAsync(_filePath, json);
    }

    private static BudgetAppData Clone(BudgetAppData source)
    {
        var json = JsonSerializer.Serialize(source, JsonOptions);
        return JsonSerializer.Deserialize<BudgetAppData>(json, JsonOptions) ?? new BudgetAppData();
    }

    private static bool TryParseAmount(string rawAmount, out decimal amount)
    {
        return decimal.TryParse(rawAmount, NumberStyles.Number, CultureInfo.CurrentCulture, out amount)
               || decimal.TryParse(rawAmount, NumberStyles.Number, CultureInfo.InvariantCulture, out amount);
    }
}
