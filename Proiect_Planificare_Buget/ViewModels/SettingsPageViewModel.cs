using Proiect_Planificare_Buget.Models;
using Proiect_Planificare_Buget.Services;

namespace Proiect_Planificare_Buget.ViewModels;

public sealed class SettingsPageViewModel : ViewModelBase
{
    private readonly BudgetDataService _budgetDataService;

    private string _fullName = string.Empty;
    private string _selectedCurrency = "RON";
    private string _selectedWeekDay = "Luni";
    private bool _autoSyncRates;
    private bool _roundUpSavings;
    private double _reminderDay = 5;

    public SettingsPageViewModel(BudgetDataService budgetDataService)
    {
        _budgetDataService = budgetDataService;
        _budgetDataService.DataChanged += async (_, _) => await HandleDataChangedAsync(LoadAsync);
        SaveSettingsCommand = new Command(async () => await SaveSettingsAsync());
        ResetSampleDataCommand = new Command(async () => await ResetSampleDataAsync());
    }

    public ICommand SaveSettingsCommand { get; }

    public ICommand ResetSampleDataCommand { get; }

    public IReadOnlyList<string> CurrencyOptions => _budgetDataService.SupportedCurrencies;

    public IReadOnlyList<string> WeekDayOptions => _budgetDataService.WeekDayOptions;

    public string StorageEngine => _budgetDataService.StorageEngine;

    public string DatabasePath => _budgetDataService.DatabasePath;

    public string FullName
    {
        get => _fullName;
        set => SetProperty(ref _fullName, value);
    }

    public string SelectedCurrency
    {
        get => _selectedCurrency;
        set => SetProperty(ref _selectedCurrency, value);
    }

    public string SelectedWeekDay
    {
        get => _selectedWeekDay;
        set => SetProperty(ref _selectedWeekDay, value);
    }

    public bool AutoSyncRates
    {
        get => _autoSyncRates;
        set => SetProperty(ref _autoSyncRates, value);
    }

    public bool RoundUpSavings
    {
        get => _roundUpSavings;
        set => SetProperty(ref _roundUpSavings, value);
    }

    public double ReminderDay
    {
        get => _reminderDay;
        set
        {
            if (SetProperty(ref _reminderDay, value))
            {
                OnPropertyChanged(nameof(ReminderDayLabel));
            }
        }
    }

    public string ReminderDayLabel => $"Ziua {ReminderDay:F0}";

    public Task LoadAsync()
    {
        return RunBusyOperationAsync(async () =>
        {
            var snapshot = await _budgetDataService.GetSnapshotAsync();
            FullName = snapshot.Settings.FullName;
            SelectedCurrency = snapshot.Settings.DefaultCurrency;
            SelectedWeekDay = snapshot.Settings.WeekStartsOn;
            AutoSyncRates = snapshot.Settings.AutoSyncRates;
            RoundUpSavings = snapshot.Settings.RoundUpSavings;
            ReminderDay = snapshot.Settings.ReminderDay;
        }, errorPrefix: "Nu am putut incarca setarile");
    }

    private async Task SaveSettingsAsync()
    {
        await RunBusyOperationAsync(async () =>
        {
            var settings = new AppSettings
            {
                FullName = FullName.Trim(),
                DefaultCurrency = SelectedCurrency,
                WeekStartsOn = SelectedWeekDay,
                AutoSyncRates = AutoSyncRates,
                RoundUpSavings = RoundUpSavings,
                ReminderDay = (int)Math.Round(ReminderDay)
            };

            await _budgetDataService.SaveSettingsAsync(settings);
        }, successMessage: "Setarile au fost salvate.", errorPrefix: "Nu am putut salva setarile");
    }

    private async Task ResetSampleDataAsync()
    {
        await RunBusyOperationAsync(
            async () => await _budgetDataService.ResetSampleDataAsync(),
            successMessage: "Datele demo au fost regenerate.",
            errorPrefix: "Nu am putut reseta datele demo");
    }
}
