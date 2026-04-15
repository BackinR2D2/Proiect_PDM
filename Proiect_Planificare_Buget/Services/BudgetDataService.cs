using System.Globalization;
using System.Text.Json;
using Proiect_Planificare_Buget.Models;

namespace Proiect_Planificare_Buget.Services;

public sealed partial class BudgetDataService
{
    private const string DatabaseFileName = "budget-planner.sqlite3";
    private const string DebugStatusFileName = "budget-db-status.txt";

    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);

    private BudgetAppData? _cache;

    public event EventHandler? DataChanged;

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

    public string StorageEngine => "SQLite";

    public string DatabasePath => Path.Combine(FileSystem.AppDataDirectory, DatabaseFileName);

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
            throw new InvalidOperationException("Titlul tranzactiei este obligatoriu.");

        if (!TryParseAmount(amountText, out var amount) || amount <= 0)
            throw new InvalidOperationException("Introdu o suma valida mai mare decat zero.");

        var normalizedCategory = NormalizeName(category);
        if (string.IsNullOrWhiteSpace(normalizedCategory))
            throw new InvalidOperationException("Selecteaza o categorie valida.");

        await MutateAsync(data =>
        {
            EnsureCategoryExists(data, normalizedCategory, ToCategoryKind(type));
            data.Transactions.Add(new TransactionRecord
            {
                Title = title.Trim(),
                Category = normalizedCategory,
                Amount = amount,
                Type = type,
                OccurredOn = selectedDate.Date.Add(selectedTime),
                Notes = notes.Trim(),
                IsRecurring = isRecurring
            });
        });
    }

    public async Task DeleteTransactionAsync(Guid id)
    {
        await MutateAsync(data =>
        {
            var removed = data.Transactions.RemoveAll(transaction => transaction.Id == id);
            if (removed == 0)
                throw new InvalidOperationException("Tranzactia selectata nu mai exista.");
        });
    }

    public async Task UpdateTransactionAsync(
        Guid id,
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
            throw new InvalidOperationException("Titlul tranzactiei este obligatoriu.");

        if (!TryParseAmount(amountText, out var amount) || amount <= 0)
            throw new InvalidOperationException("Introdu o suma valida mai mare decat zero.");

        var normalizedCategory = NormalizeName(category);
        if (string.IsNullOrWhiteSpace(normalizedCategory))
            throw new InvalidOperationException("Selecteaza o categorie valida.");

        await MutateAsync(data =>
        {
            var transaction = data.Transactions.FirstOrDefault(t => t.Id == id)
                ?? throw new InvalidOperationException("Tranzactia selectata nu mai exista.");

            EnsureCategoryExists(data, normalizedCategory, ToCategoryKind(type));
            transaction.Title = title.Trim();
            transaction.Category = normalizedCategory;
            transaction.Amount = amount;
            transaction.Type = type;
            transaction.OccurredOn = selectedDate.Date.Add(selectedTime);
            transaction.Notes = notes.Trim();
            transaction.IsRecurring = isRecurring;
        });
    }

    public async Task SaveBudgetAsync(string category, string monthlyLimitText, double alertThresholdPercent)
    {
        if (!TryParseAmount(monthlyLimitText, out var monthlyLimit) || monthlyLimit <= 0)
            throw new InvalidOperationException("Limita lunara trebuie sa fie o suma valida.");

        var normalizedCategory = NormalizeName(category);
        if (string.IsNullOrWhiteSpace(normalizedCategory))
            throw new InvalidOperationException("Selecteaza o categorie valida pentru buget.");

        var alertThreshold = Math.Clamp((decimal)alertThresholdPercent / 100m, 0.1m, 1m);

        await MutateAsync(data =>
        {
            EnsureCategoryExists(data, normalizedCategory, CategoryKind.Expense);

            var existingBudget = data.Budgets.FirstOrDefault(budget =>
                string.Equals(budget.Name, normalizedCategory, StringComparison.OrdinalIgnoreCase));

            if (existingBudget is null)
            {
                data.Budgets.Add(new BudgetCategory
                {
                    Name = normalizedCategory,
                    MonthlyLimit = monthlyLimit,
                    AlertThreshold = alertThreshold
                });
                return;
            }

            existingBudget.Name = normalizedCategory;
            existingBudget.MonthlyLimit = monthlyLimit;
            existingBudget.AlertThreshold = alertThreshold;
        });
    }

    public async Task DeleteBudgetAsync(string category)
    {
        var normalizedCategory = NormalizeName(category);
        if (string.IsNullOrWhiteSpace(normalizedCategory))
            throw new InvalidOperationException("Selecteaza un buget valid pentru stergere.");

        await MutateAsync(data =>
        {
            var budget = data.Budgets.FirstOrDefault(item =>
                string.Equals(item.Name, normalizedCategory, StringComparison.OrdinalIgnoreCase));

            if (budget is null)
                throw new InvalidOperationException("Bugetul selectat nu mai exista.");

            data.Budgets.Remove(budget);
        });
    }

    public async Task SaveGoalAsync(Guid? goalId, string title, string targetAmountText, string currentAmountText, DateTime deadline, bool isPinned)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new InvalidOperationException("Numele obiectivului este obligatoriu.");

        if (!TryParseAmount(targetAmountText, out var targetAmount) || targetAmount <= 0)
            throw new InvalidOperationException("Valoarea tinta trebuie sa fie valida.");

        if (!TryParseAmount(currentAmountText, out var currentAmount) || currentAmount < 0)
            throw new InvalidOperationException("Valoarea economisita trebuie sa fie valida.");

        await MutateAsync(data =>
        {
            var normalizedTitle = title.Trim();
            var goal = goalId.HasValue
                ? data.Goals.FirstOrDefault(item => item.Id == goalId.Value)
                : null;

            if (goal is null)
            {
                data.Goals.Add(new SavingsGoal
                {
                    Title = normalizedTitle,
                    TargetAmount = targetAmount,
                    CurrentAmount = currentAmount,
                    Deadline = deadline.Date,
                    IsPinned = isPinned
                });
                return;
            }

            goal.Title = normalizedTitle;
            goal.TargetAmount = targetAmount;
            goal.CurrentAmount = currentAmount;
            goal.Deadline = deadline.Date;
            goal.IsPinned = isPinned;
        });
    }

    public async Task DeleteGoalAsync(Guid id)
    {
        await MutateAsync(data =>
        {
            var removed = data.Goals.RemoveAll(goal => goal.Id == id);
            if (removed == 0)
                throw new InvalidOperationException("Obiectivul selectat nu mai exista.");
        });
    }

    public async Task SaveSettingsAsync(AppSettings settings)
    {
        await MutateAsync(data =>
        {
            data.Settings = new AppSettings
            {
                FullName = string.IsNullOrWhiteSpace(settings.FullName) ? "Echipa Budget Planner" : settings.FullName.Trim(),
                DefaultCurrency = settings.DefaultCurrency,
                WeekStartsOn = settings.WeekStartsOn,
                AutoSyncRates = settings.AutoSyncRates,
                RoundUpSavings = settings.RoundUpSavings,
                ReminderDay = settings.ReminderDay
            };
        });
    }

    public async Task ResetSampleDataAsync()
    {
        await MutateAsync(data =>
        {
            var sample = BudgetAppData.CreateSample();
            data.Settings = sample.Settings;
            data.Categories = sample.Categories;
            data.Budgets = sample.Budgets;
            data.Goals = sample.Goals;
            data.Transactions = sample.Transactions;
        });
    }

    public async Task SaveCategoryAsync(Guid? categoryId, string name, CategoryKind kind)
    {
        var normalizedName = NormalizeName(name);
        if (string.IsNullOrWhiteSpace(normalizedName))
            throw new InvalidOperationException("Numele categoriei este obligatoriu.");

        await MutateAsync(data =>
        {
            var duplicate = data.Categories.FirstOrDefault(category =>
                category.Id != categoryId
                && string.Equals(category.Name, normalizedName, StringComparison.OrdinalIgnoreCase));

            if (duplicate is not null)
                throw new InvalidOperationException("Exista deja o categorie cu acest nume.");

            if (!categoryId.HasValue)
            {
                data.Categories.Add(new CategoryDefinition
                {
                    Name = normalizedName,
                    Kind = kind
                });
                return;
            }

            var categoryToEdit = data.Categories.FirstOrDefault(category => category.Id == categoryId.Value)
                ?? throw new InvalidOperationException("Categoria selectata nu mai exista.");

            var originalName = categoryToEdit.Name;
            var originalKind = categoryToEdit.Kind;

            if (originalKind != kind && IsCategoryInUse(data, categoryToEdit))
                throw new InvalidOperationException("Nu poti schimba tipul unei categorii deja folosite in tranzactii sau bugete.");

            categoryToEdit.Name = normalizedName;
            categoryToEdit.Kind = kind;

            if (!string.Equals(originalName, normalizedName, StringComparison.OrdinalIgnoreCase))
            {
                foreach (var transaction in data.Transactions.Where(transaction =>
                             string.Equals(transaction.Category, originalName, StringComparison.OrdinalIgnoreCase)
                             && ToCategoryKind(transaction.Type) == originalKind))
                {
                    transaction.Category = normalizedName;
                }

                if (originalKind == CategoryKind.Expense)
                {
                    foreach (var budget in data.Budgets.Where(budget =>
                                 string.Equals(budget.Name, originalName, StringComparison.OrdinalIgnoreCase)))
                    {
                        budget.Name = normalizedName;
                    }
                }
            }
        });
    }

    public async Task DeleteCategoryAsync(Guid id)
    {
        await MutateAsync(data =>
        {
            var category = data.Categories.FirstOrDefault(item => item.Id == id)
                ?? throw new InvalidOperationException("Categoria selectata nu mai exista.");

            if (IsCategoryInUse(data, category))
                throw new InvalidOperationException("Categoria este deja folosita in tranzactii sau bugete si nu poate fi stearsa.");

            data.Categories.Remove(category);
        });
    }

    private async Task MutateAsync(Action<BudgetAppData> mutation)
    {
        await _gate.WaitAsync();
        try
        {
            await EnsureInitializedAsync();
            mutation(_cache!);
            NormalizeData(_cache!);
            await PersistSnapshotAsync(_cache!);
        }
        finally
        {
            _gate.Release();
        }

        DataChanged?.Invoke(this, EventArgs.Empty);
    }

    private async Task EnsureInitializedAsync()
    {
        if (_cache is not null)
            return;

        await EnsureStorageReadyAsync();

        var rawJson = await ReadSerializedStateAsync();
        if (string.IsNullOrWhiteSpace(rawJson))
        {
            _cache = BudgetAppData.CreateSample();
            NormalizeData(_cache);
            await PersistSnapshotAsync(_cache);
            return;
        }

        var loadedData = JsonSerializer.Deserialize<BudgetAppData>(rawJson, _jsonOptions) ?? BudgetAppData.CreateSample();
        NormalizeData(loadedData);
        _cache = loadedData;
        await PersistSnapshotAsync(_cache);
    }

    private async Task PersistSnapshotAsync(BudgetAppData data)
    {
        var json = JsonSerializer.Serialize(data, _jsonOptions);
        await SaveSerializedStateAsync(json);
        await WriteDebugStatusAsync(data);
    }

    private async Task WriteDebugStatusAsync(BudgetAppData data)
    {
#if DEBUG
        var filePath = Path.Combine(FileSystem.AppDataDirectory, DebugStatusFileName);
        var lines = new[]
        {
            $"Timestamp={DateTime.Now:O}",
            $"StorageEngine={StorageEngine}",
            $"DatabasePath={DatabasePath}",
            $"BudgetCount={data.Budgets.Count}",
            $"GoalCount={data.Goals.Count}",
            $"TransactionCount={data.Transactions.Count}",
            $"CategoryCount={data.Categories.Count}",
            $"FullName={data.Settings.FullName}"
        };

        await File.WriteAllLinesAsync(filePath, lines);
#endif
    }

    private static void NormalizeData(BudgetAppData data)
    {
        data.Settings ??= new AppSettings();
        data.Categories ??= [];
        data.Budgets ??= [];
        data.Goals ??= [];
        data.Transactions ??= [];

        foreach (var defaultCategory in GetDefaultCategories())
        {
            EnsureCategoryExists(data, defaultCategory.Name, defaultCategory.Kind);
        }

        foreach (var budget in data.Budgets)
        {
            budget.Name = NormalizeName(budget.Name);
            EnsureCategoryExists(data, budget.Name, CategoryKind.Expense);
        }

        foreach (var transaction in data.Transactions)
        {
            transaction.Title = string.IsNullOrWhiteSpace(transaction.Title) ? "Tranzactie" : transaction.Title.Trim();
            transaction.Category = NormalizeName(transaction.Category);
            EnsureCategoryExists(data, transaction.Category, ToCategoryKind(transaction.Type));
        }

        data.Categories = data.Categories
            .Where(category => !string.IsNullOrWhiteSpace(category.Name))
            .Select(category =>
            {
                category.Id = category.Id == Guid.Empty ? Guid.NewGuid() : category.Id;
                category.Name = NormalizeName(category.Name);
                return category;
            })
            .GroupBy(category => category.Name, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .OrderBy(category => category.Kind)
            .ThenBy(category => category.Name)
            .ToList();

        data.Budgets = data.Budgets
            .Where(budget => !string.IsNullOrWhiteSpace(budget.Name))
            .GroupBy(budget => budget.Name, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.Last())
            .OrderBy(budget => budget.Name)
            .ToList();

        data.Goals = data.Goals
            .OrderByDescending(goal => goal.IsPinned)
            .ThenBy(goal => goal.Deadline)
            .ToList();

        data.Transactions = data.Transactions
            .OrderByDescending(transaction => transaction.OccurredOn)
            .ToList();
    }

    private static IEnumerable<CategoryDefinition> GetDefaultCategories() =>
    [
        new() { Name = "Mancare", Kind = CategoryKind.Expense },
        new() { Name = "Transport", Kind = CategoryKind.Expense },
        new() { Name = "Utilitati", Kind = CategoryKind.Expense },
        new() { Name = "Sanatate", Kind = CategoryKind.Expense },
        new() { Name = "Educatie", Kind = CategoryKind.Expense },
        new() { Name = "Timp liber", Kind = CategoryKind.Expense },
        new() { Name = "Cumparaturi", Kind = CategoryKind.Expense },
        new() { Name = "Economii", Kind = CategoryKind.Expense },
        new() { Name = "Altele", Kind = CategoryKind.Expense },
        new() { Name = "Salariu", Kind = CategoryKind.Income },
        new() { Name = "Freelance", Kind = CategoryKind.Income },
        new() { Name = "Bonus", Kind = CategoryKind.Income },
        new() { Name = "Cadou", Kind = CategoryKind.Income }
    ];

    private static void EnsureCategoryExists(BudgetAppData data, string name, CategoryKind kind)
    {
        var normalizedName = NormalizeName(name);
        if (string.IsNullOrWhiteSpace(normalizedName))
            return;

        var existing = data.Categories.FirstOrDefault(category =>
            string.Equals(category.Name, normalizedName, StringComparison.OrdinalIgnoreCase));

        if (existing is not null)
        {
            existing.Kind = kind;
            existing.Name = normalizedName;
            return;
        }

        data.Categories.Add(new CategoryDefinition
        {
            Name = normalizedName,
            Kind = kind
        });
    }

    private static bool IsCategoryInUse(BudgetAppData data, CategoryDefinition category)
    {
        var usedInTransactions = data.Transactions.Any(transaction =>
            string.Equals(transaction.Category, category.Name, StringComparison.OrdinalIgnoreCase)
            && ToCategoryKind(transaction.Type) == category.Kind);

        var usedInBudgets = category.Kind == CategoryKind.Expense
            && data.Budgets.Any(budget => string.Equals(budget.Name, category.Name, StringComparison.OrdinalIgnoreCase));

        return usedInTransactions || usedInBudgets;
    }

    private static CategoryKind ToCategoryKind(TransactionType type) =>
        type == TransactionType.Expense ? CategoryKind.Expense : CategoryKind.Income;

    private static string NormalizeName(string value) =>
        string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();

    private static BudgetAppData Clone(BudgetAppData source) => new()
    {
        Settings = new AppSettings
        {
            FullName = source.Settings.FullName,
            DefaultCurrency = source.Settings.DefaultCurrency,
            WeekStartsOn = source.Settings.WeekStartsOn,
            AutoSyncRates = source.Settings.AutoSyncRates,
            RoundUpSavings = source.Settings.RoundUpSavings,
            ReminderDay = source.Settings.ReminderDay
        },
        Categories =
        [
            .. source.Categories.Select(category => new CategoryDefinition
            {
                Id = category.Id,
                Name = category.Name,
                Kind = category.Kind
            })
        ],
        Budgets =
        [
            .. source.Budgets.Select(budget => new BudgetCategory
            {
                Name = budget.Name,
                MonthlyLimit = budget.MonthlyLimit,
                AlertThreshold = budget.AlertThreshold
            })
        ],
        Goals =
        [
            .. source.Goals.Select(goal => new SavingsGoal
            {
                Id = goal.Id,
                Title = goal.Title,
                TargetAmount = goal.TargetAmount,
                CurrentAmount = goal.CurrentAmount,
                Deadline = goal.Deadline,
                IsPinned = goal.IsPinned
            })
        ],
        Transactions =
        [
            .. source.Transactions.Select(transaction => new TransactionRecord
            {
                Id = transaction.Id,
                Title = transaction.Title,
                Category = transaction.Category,
                Type = transaction.Type,
                Amount = transaction.Amount,
                OccurredOn = transaction.OccurredOn,
                Notes = transaction.Notes,
                IsRecurring = transaction.IsRecurring
            })
        ]
    };

    private static bool TryParseAmount(string rawAmount, out decimal amount) =>
        decimal.TryParse(rawAmount, NumberStyles.Number, CultureInfo.CurrentCulture, out amount)
        || decimal.TryParse(rawAmount, NumberStyles.Number, CultureInfo.InvariantCulture, out amount);

    private partial Task EnsureStorageReadyAsync();

    private partial Task<string?> ReadSerializedStateAsync();

    private partial Task SaveSerializedStateAsync(string json);
}
