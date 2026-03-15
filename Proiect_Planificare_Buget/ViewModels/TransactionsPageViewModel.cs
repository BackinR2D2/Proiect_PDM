using System.Collections.ObjectModel;
using Proiect_Planificare_Buget.Models;
using Proiect_Planificare_Buget.Services;

namespace Proiect_Planificare_Buget.ViewModels;

public sealed class TransactionsPageViewModel : ViewModelBase
{
    private readonly BudgetDataService _budgetDataService;

    private string _searchText = string.Empty;
    private string _selectedFilterType = "Toate";
    private string _selectedFilterCategory = "Toate";
    private string _entryTitle = string.Empty;
    private string _entryAmount = string.Empty;
    private string _selectedEntryType = "Cheltuiala";
    private string _selectedEntryCategory = "Mancare";
    private DateTime _selectedDate = DateTime.Today;
    private TimeSpan _selectedTime = DateTime.Now.TimeOfDay;
    private string _notes = string.Empty;
    private bool _isRecurring;

    private List<TransactionRecord> _allTransactions = [];

    public TransactionsPageViewModel(BudgetDataService budgetDataService)
    {
        _budgetDataService = budgetDataService;
        _budgetDataService.DataChanged += async (_, _) => await LoadAsync();

        FilterTypeOptions = ["Toate", "Cheltuieli", "Venituri"];
        FilterCategoryOptions = ["Toate", .. _budgetDataService.ExpenseCategories, .. _budgetDataService.IncomeCategories];

        AddTransactionCommand = new Command(async () => await AddTransactionAsync());
        DeleteTransactionCommand = new Command<TransactionRecord>(async transaction => await DeleteTransactionAsync(transaction));
    }

    public ObservableCollection<TransactionRecord> Transactions { get; } = [];

    public IReadOnlyList<string> FilterTypeOptions { get; }

    public IReadOnlyList<string> FilterCategoryOptions { get; }

    public IReadOnlyList<string> EntryTypeOptions { get; } = ["Cheltuiala", "Venit"];

    public IEnumerable<string> EntryCategoryOptions => SelectedEntryType == "Cheltuiala"
        ? _budgetDataService.ExpenseCategories
        : _budgetDataService.IncomeCategories;

    public ICommand AddTransactionCommand { get; }

    public ICommand DeleteTransactionCommand { get; }

    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetProperty(ref _searchText, value))
            {
                ApplyFilters();
            }
        }
    }

    public string SelectedFilterType
    {
        get => _selectedFilterType;
        set
        {
            if (SetProperty(ref _selectedFilterType, value))
            {
                ApplyFilters();
            }
        }
    }

    public string SelectedFilterCategory
    {
        get => _selectedFilterCategory;
        set
        {
            if (SetProperty(ref _selectedFilterCategory, value))
            {
                ApplyFilters();
            }
        }
    }

    public string EntryTitle
    {
        get => _entryTitle;
        set => SetProperty(ref _entryTitle, value);
    }

    public string EntryAmount
    {
        get => _entryAmount;
        set => SetProperty(ref _entryAmount, value);
    }

    public string SelectedEntryType
    {
        get => _selectedEntryType;
        set
        {
            if (SetProperty(ref _selectedEntryType, value))
            {
                SelectedEntryCategory = EntryCategoryOptions.First();
                OnPropertyChanged(nameof(EntryCategoryOptions));
            }
        }
    }

    public string SelectedEntryCategory
    {
        get => _selectedEntryCategory;
        set => SetProperty(ref _selectedEntryCategory, value);
    }

    public DateTime SelectedDate
    {
        get => _selectedDate;
        set => SetProperty(ref _selectedDate, value);
    }

    public TimeSpan SelectedTime
    {
        get => _selectedTime;
        set => SetProperty(ref _selectedTime, value);
    }

    public string Notes
    {
        get => _notes;
        set => SetProperty(ref _notes, value);
    }

    public bool IsRecurring
    {
        get => _isRecurring;
        set => SetProperty(ref _isRecurring, value);
    }

    public Task LoadAsync()
    {
        return RunBusyOperationAsync(async () =>
        {
            var snapshot = await _budgetDataService.GetSnapshotAsync();
            _allTransactions = snapshot.Transactions.OrderByDescending(transaction => transaction.OccurredOn).ToList();
            ApplyFilters();
        }, errorPrefix: "Nu am putut incarca tranzactiile");
    }

    private async Task AddTransactionAsync()
    {
        await RunBusyOperationAsync(async () =>
        {
            var transactionType = SelectedEntryType == "Cheltuiala" ? TransactionType.Expense : TransactionType.Income;

            await _budgetDataService.AddTransactionAsync(
                EntryTitle,
                SelectedEntryCategory,
                EntryAmount,
                transactionType,
                SelectedDate,
                SelectedTime,
                Notes,
                IsRecurring);

            EntryTitle = string.Empty;
            EntryAmount = string.Empty;
            Notes = string.Empty;
            IsRecurring = false;
            SelectedDate = DateTime.Today;
            SelectedTime = DateTime.Now.TimeOfDay;
        }, successMessage: "Tranzactia a fost salvata.", errorPrefix: "Nu am putut salva tranzactia");
    }

    private async Task DeleteTransactionAsync(TransactionRecord? transaction)
    {
        if (transaction is null)
        {
            return;
        }

        await RunBusyOperationAsync(
            async () => await _budgetDataService.DeleteTransactionAsync(transaction.Id),
            successMessage: "Tranzactia a fost stearsa.",
            errorPrefix: "Nu am putut sterge tranzactia");
    }

    private void ApplyFilters()
    {
        if (_allTransactions.Count == 0)
        {
            Transactions.Clear();
            return;
        }

        IEnumerable<TransactionRecord> filtered = _allTransactions;

        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            filtered = filtered.Where(transaction =>
                transaction.Title.Contains(SearchText, StringComparison.OrdinalIgnoreCase)
                || transaction.Category.Contains(SearchText, StringComparison.OrdinalIgnoreCase)
                || transaction.Notes.Contains(SearchText, StringComparison.OrdinalIgnoreCase));
        }

        if (SelectedFilterType == "Cheltuieli")
        {
            filtered = filtered.Where(transaction => transaction.Type == TransactionType.Expense);
        }
        else if (SelectedFilterType == "Venituri")
        {
            filtered = filtered.Where(transaction => transaction.Type == TransactionType.Income);
        }

        if (SelectedFilterCategory != "Toate")
        {
            filtered = filtered.Where(transaction => transaction.Category == SelectedFilterCategory);
        }

        Transactions.Clear();
        foreach (var transaction in filtered)
        {
            Transactions.Add(transaction);
        }
    }
}
