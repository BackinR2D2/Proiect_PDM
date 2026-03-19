using System.Globalization;
using Microsoft.Data.SqlClient;
using Proiect_Planificare_Buget.Models;

namespace Proiect_Planificare_Buget.Services;

public sealed class BudgetDataService
{
    private const string DefaultConnectionString =
        "Server=localhost;Database=BudgetPlannerDB;Trusted_Connection=True;TrustServerCertificate=True;";

    private readonly SemaphoreSlim _gate = new(1, 1);
    private BudgetAppData? _cache;

    public event EventHandler? DataChanged;

    public string ConnectionString
    {
        get => Preferences.Default.Get("db_connection_string", DefaultConnectionString);
        set => Preferences.Default.Set("db_connection_string", value);
    }

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

    // ----------------------------------------------------------------
    //  Public API
    // ----------------------------------------------------------------

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

        var transaction = new TransactionRecord
        {
            Title = title.Trim(),
            Category = category,
            Amount = amount,
            Type = type,
            OccurredOn = selectedDate.Date.Add(selectedTime),
            Notes = notes.Trim(),
            IsRecurring = isRecurring
        };

        await using var conn = new SqlConnection(ConnectionString);
        await conn.OpenAsync();

        const string sql = """
            INSERT INTO Transactions (Id, Title, Category, Type, Amount, OccurredOn, Notes, IsRecurring)
            VALUES (@Id, @Title, @Category, @Type, @Amount, @OccurredOn, @Notes, @IsRecurring)
            """;

        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@Id", transaction.Id);
        cmd.Parameters.AddWithValue("@Title", transaction.Title);
        cmd.Parameters.AddWithValue("@Category", transaction.Category);
        cmd.Parameters.AddWithValue("@Type", (int)transaction.Type);
        cmd.Parameters.AddWithValue("@Amount", transaction.Amount);
        cmd.Parameters.AddWithValue("@OccurredOn", transaction.OccurredOn);
        cmd.Parameters.AddWithValue("@Notes", transaction.Notes);
        cmd.Parameters.AddWithValue("@IsRecurring", transaction.IsRecurring);
        await cmd.ExecuteNonQueryAsync();

        _cache = null;
        DataChanged?.Invoke(this, EventArgs.Empty);
    }

    public async Task DeleteTransactionAsync(Guid id)
    {
        await using var conn = new SqlConnection(ConnectionString);
        await conn.OpenAsync();

        await using var cmd = new SqlCommand("DELETE FROM Transactions WHERE Id = @Id", conn);
        cmd.Parameters.AddWithValue("@Id", id);
        await cmd.ExecuteNonQueryAsync();

        _cache = null;
        DataChanged?.Invoke(this, EventArgs.Empty);
    }

    public async Task SaveBudgetAsync(string category, string monthlyLimitText, double alertThresholdPercent)
    {
        if (!TryParseAmount(monthlyLimitText, out var monthlyLimit) || monthlyLimit <= 0)
            throw new InvalidOperationException("Limita lunara trebuie sa fie o suma valida.");

        var alertThreshold = Math.Clamp((decimal)alertThresholdPercent / 100m, 0.1m, 1m);

        await using var conn = new SqlConnection(ConnectionString);
        await conn.OpenAsync();

        const string sql = """
            IF EXISTS (SELECT 1 FROM BudgetCategories WHERE Name = @Name)
                UPDATE BudgetCategories SET MonthlyLimit = @Limit, AlertThreshold = @Threshold WHERE Name = @Name
            ELSE
                INSERT INTO BudgetCategories (Name, MonthlyLimit, AlertThreshold) VALUES (@Name, @Limit, @Threshold)
            """;

        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@Name", category);
        cmd.Parameters.AddWithValue("@Limit", monthlyLimit);
        cmd.Parameters.AddWithValue("@Threshold", alertThreshold);
        await cmd.ExecuteNonQueryAsync();

        _cache = null;
        DataChanged?.Invoke(this, EventArgs.Empty);
    }

    public async Task SaveGoalAsync(Guid? goalId, string title, string targetAmountText, string currentAmountText, DateTime deadline, bool isPinned)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new InvalidOperationException("Numele obiectivului este obligatoriu.");

        if (!TryParseAmount(targetAmountText, out var targetAmount) || targetAmount <= 0)
            throw new InvalidOperationException("Valoarea tinta trebuie sa fie valida.");

        if (!TryParseAmount(currentAmountText, out var currentAmount) || currentAmount < 0)
            throw new InvalidOperationException("Valoarea economisita trebuie sa fie valida.");

        await using var conn = new SqlConnection(ConnectionString);
        await conn.OpenAsync();

        if (goalId.HasValue)
        {
            const string sql = """
                UPDATE SavingsGoals
                SET Title = @Title, TargetAmount = @Target, CurrentAmount = @Current, Deadline = @Deadline, IsPinned = @IsPinned
                WHERE Id = @Id
                """;
            await using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@Id", goalId.Value);
            cmd.Parameters.AddWithValue("@Title", title.Trim());
            cmd.Parameters.AddWithValue("@Target", targetAmount);
            cmd.Parameters.AddWithValue("@Current", currentAmount);
            cmd.Parameters.AddWithValue("@Deadline", deadline.Date);
            cmd.Parameters.AddWithValue("@IsPinned", isPinned);
            await cmd.ExecuteNonQueryAsync();
        }
        else
        {
            const string sql = """
                INSERT INTO SavingsGoals (Id, Title, TargetAmount, CurrentAmount, Deadline, IsPinned)
                VALUES (@Id, @Title, @Target, @Current, @Deadline, @IsPinned)
                """;
            await using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@Id", Guid.NewGuid());
            cmd.Parameters.AddWithValue("@Title", title.Trim());
            cmd.Parameters.AddWithValue("@Target", targetAmount);
            cmd.Parameters.AddWithValue("@Current", currentAmount);
            cmd.Parameters.AddWithValue("@Deadline", deadline.Date);
            cmd.Parameters.AddWithValue("@IsPinned", isPinned);
            await cmd.ExecuteNonQueryAsync();
        }

        _cache = null;
        DataChanged?.Invoke(this, EventArgs.Empty);
    }

    public async Task DeleteGoalAsync(Guid id)
    {
        await using var conn = new SqlConnection(ConnectionString);
        await conn.OpenAsync();

        await using var cmd = new SqlCommand("DELETE FROM SavingsGoals WHERE Id = @Id", conn);
        cmd.Parameters.AddWithValue("@Id", id);
        await cmd.ExecuteNonQueryAsync();

        _cache = null;
        DataChanged?.Invoke(this, EventArgs.Empty);
    }

    public async Task SaveSettingsAsync(AppSettings settings)
    {
        await using var conn = new SqlConnection(ConnectionString);
        await conn.OpenAsync();

        const string sql = """
            UPDATE AppSettings SET
                FullName        = @FullName,
                DefaultCurrency = @Currency,
                WeekStartsOn    = @WeekStart,
                AutoSyncRates   = @AutoSync,
                RoundUpSavings  = @RoundUp,
                ReminderDay     = @ReminderDay
            WHERE Id = 1
            """;

        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@FullName", settings.FullName);
        cmd.Parameters.AddWithValue("@Currency", settings.DefaultCurrency);
        cmd.Parameters.AddWithValue("@WeekStart", settings.WeekStartsOn);
        cmd.Parameters.AddWithValue("@AutoSync", settings.AutoSyncRates);
        cmd.Parameters.AddWithValue("@RoundUp", settings.RoundUpSavings);
        cmd.Parameters.AddWithValue("@ReminderDay", settings.ReminderDay);
        await cmd.ExecuteNonQueryAsync();

        _cache = null;
        DataChanged?.Invoke(this, EventArgs.Empty);
    }

    public async Task ResetSampleDataAsync()
    {
        var sample = BudgetAppData.CreateSample();

        await using var conn = new SqlConnection(ConnectionString);
        await conn.OpenAsync();
        await using var tx = conn.BeginTransaction();

        try
        {
            await ExecuteAsync("DELETE FROM Transactions", conn, tx);
            await ExecuteAsync("DELETE FROM SavingsGoals", conn, tx);
            await ExecuteAsync("DELETE FROM BudgetCategories", conn, tx);

            foreach (var budget in sample.Budgets)
            {
                var cmd = new SqlCommand(
                    "INSERT INTO BudgetCategories (Name, MonthlyLimit, AlertThreshold) VALUES (@Name, @Limit, @Threshold)",
                    conn, tx);
                cmd.Parameters.AddWithValue("@Name", budget.Name);
                cmd.Parameters.AddWithValue("@Limit", budget.MonthlyLimit);
                cmd.Parameters.AddWithValue("@Threshold", budget.AlertThreshold);
                await cmd.ExecuteNonQueryAsync();
            }

            foreach (var goal in sample.Goals)
            {
                var cmd = new SqlCommand(
                    "INSERT INTO SavingsGoals (Id, Title, TargetAmount, CurrentAmount, Deadline, IsPinned) VALUES (@Id, @Title, @Target, @Current, @Deadline, @Pinned)",
                    conn, tx);
                cmd.Parameters.AddWithValue("@Id", goal.Id);
                cmd.Parameters.AddWithValue("@Title", goal.Title);
                cmd.Parameters.AddWithValue("@Target", goal.TargetAmount);
                cmd.Parameters.AddWithValue("@Current", goal.CurrentAmount);
                cmd.Parameters.AddWithValue("@Deadline", goal.Deadline.Date);
                cmd.Parameters.AddWithValue("@Pinned", goal.IsPinned);
                await cmd.ExecuteNonQueryAsync();
            }

            foreach (var t in sample.Transactions)
            {
                var cmd = new SqlCommand(
                    "INSERT INTO Transactions (Id, Title, Category, Type, Amount, OccurredOn, Notes, IsRecurring) VALUES (@Id, @Title, @Category, @Type, @Amount, @OccurredOn, @Notes, @Recurring)",
                    conn, tx);
                cmd.Parameters.AddWithValue("@Id", t.Id);
                cmd.Parameters.AddWithValue("@Title", t.Title);
                cmd.Parameters.AddWithValue("@Category", t.Category);
                cmd.Parameters.AddWithValue("@Type", (int)t.Type);
                cmd.Parameters.AddWithValue("@Amount", t.Amount);
                cmd.Parameters.AddWithValue("@OccurredOn", t.OccurredOn);
                cmd.Parameters.AddWithValue("@Notes", t.Notes);
                cmd.Parameters.AddWithValue("@Recurring", t.IsRecurring);
                await cmd.ExecuteNonQueryAsync();
            }

            await tx.CommitAsync();
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }

        _cache = null;
        DataChanged?.Invoke(this, EventArgs.Empty);
    }

    public IEnumerable<string> GetCategoriesFor(TransactionType type) =>
        type == TransactionType.Expense ? ExpenseCategories : IncomeCategories;

    // ----------------------------------------------------------------
    //  Private helpers
    // ----------------------------------------------------------------

    private async Task EnsureInitializedAsync()
    {
        if (_cache is not null)
            return;

        await using var conn = new SqlConnection(ConnectionString);
        await conn.OpenAsync();

        var data = new BudgetAppData();

        // Settings
        await using (var cmd = new SqlCommand("SELECT * FROM AppSettings WHERE Id = 1", conn))
        await using (var reader = await cmd.ExecuteReaderAsync())
        {
            if (await reader.ReadAsync())
            {
                data.Settings = new AppSettings
                {
                    FullName        = reader.GetString(reader.GetOrdinal("FullName")),
                    DefaultCurrency = reader.GetString(reader.GetOrdinal("DefaultCurrency")),
                    WeekStartsOn    = reader.GetString(reader.GetOrdinal("WeekStartsOn")),
                    AutoSyncRates   = reader.GetBoolean(reader.GetOrdinal("AutoSyncRates")),
                    RoundUpSavings  = reader.GetBoolean(reader.GetOrdinal("RoundUpSavings")),
                    ReminderDay     = reader.GetInt32(reader.GetOrdinal("ReminderDay"))
                };
            }
        }

        // Budgets
        await using (var cmd = new SqlCommand("SELECT Name, MonthlyLimit, AlertThreshold FROM BudgetCategories", conn))
        await using (var reader = await cmd.ExecuteReaderAsync())
        {
            while (await reader.ReadAsync())
            {
                data.Budgets.Add(new BudgetCategory
                {
                    Name           = reader.GetString(reader.GetOrdinal("Name")),
                    MonthlyLimit   = reader.GetDecimal(reader.GetOrdinal("MonthlyLimit")),
                    AlertThreshold = reader.GetDecimal(reader.GetOrdinal("AlertThreshold"))
                });
            }
        }

        // Goals
        await using (var cmd = new SqlCommand("SELECT * FROM SavingsGoals", conn))
        await using (var reader = await cmd.ExecuteReaderAsync())
        {
            while (await reader.ReadAsync())
            {
                data.Goals.Add(new SavingsGoal
                {
                    Id            = reader.GetGuid(reader.GetOrdinal("Id")),
                    Title         = reader.GetString(reader.GetOrdinal("Title")),
                    TargetAmount  = reader.GetDecimal(reader.GetOrdinal("TargetAmount")),
                    CurrentAmount = reader.GetDecimal(reader.GetOrdinal("CurrentAmount")),
                    Deadline      = reader.GetDateTime(reader.GetOrdinal("Deadline")),
                    IsPinned      = reader.GetBoolean(reader.GetOrdinal("IsPinned"))
                });
            }
        }

        // Transactions
        await using (var cmd = new SqlCommand("SELECT * FROM Transactions ORDER BY OccurredOn DESC", conn))
        await using (var reader = await cmd.ExecuteReaderAsync())
        {
            while (await reader.ReadAsync())
            {
                data.Transactions.Add(new TransactionRecord
                {
                    Id          = reader.GetGuid(reader.GetOrdinal("Id")),
                    Title       = reader.GetString(reader.GetOrdinal("Title")),
                    Category    = reader.GetString(reader.GetOrdinal("Category")),
                    Type        = (TransactionType)reader.GetByte(reader.GetOrdinal("Type")),
                    Amount      = reader.GetDecimal(reader.GetOrdinal("Amount")),
                    OccurredOn  = reader.GetDateTime(reader.GetOrdinal("OccurredOn")),
                    Notes       = reader.GetString(reader.GetOrdinal("Notes")),
                    IsRecurring = reader.GetBoolean(reader.GetOrdinal("IsRecurring"))
                });
            }
        }

        _cache = data;
    }

    private static async Task ExecuteAsync(string sql, SqlConnection conn, SqlTransaction tx)
    {
        await using var cmd = new SqlCommand(sql, conn, tx);
        await cmd.ExecuteNonQueryAsync();
    }

    private static BudgetAppData Clone(BudgetAppData source) => new()
    {
        Settings = new AppSettings
        {
            FullName        = source.Settings.FullName,
            DefaultCurrency = source.Settings.DefaultCurrency,
            WeekStartsOn    = source.Settings.WeekStartsOn,
            AutoSyncRates   = source.Settings.AutoSyncRates,
            RoundUpSavings  = source.Settings.RoundUpSavings,
            ReminderDay     = source.Settings.ReminderDay
        },
        Budgets      = [.. source.Budgets.Select(b => new BudgetCategory { Name = b.Name, MonthlyLimit = b.MonthlyLimit, AlertThreshold = b.AlertThreshold })],
        Goals        = [.. source.Goals.Select(g => new SavingsGoal { Id = g.Id, Title = g.Title, TargetAmount = g.TargetAmount, CurrentAmount = g.CurrentAmount, Deadline = g.Deadline, IsPinned = g.IsPinned })],
        Transactions = [.. source.Transactions.Select(t => new TransactionRecord { Id = t.Id, Title = t.Title, Category = t.Category, Type = t.Type, Amount = t.Amount, OccurredOn = t.OccurredOn, Notes = t.Notes, IsRecurring = t.IsRecurring })]
    };

    private static bool TryParseAmount(string rawAmount, out decimal amount) =>
        decimal.TryParse(rawAmount, NumberStyles.Number, CultureInfo.CurrentCulture, out amount)
        || decimal.TryParse(rawAmount, NumberStyles.Number, CultureInfo.InvariantCulture, out amount);
}
