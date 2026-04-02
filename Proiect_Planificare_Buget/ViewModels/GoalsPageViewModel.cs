using System.Collections.ObjectModel;
using Proiect_Planificare_Buget.Models;
using Proiect_Planificare_Buget.Services;

namespace Proiect_Planificare_Buget.ViewModels;

public sealed class GoalsPageViewModel : ViewModelBase
{
    private readonly BudgetDataService _budgetDataService;

    private Guid? _editingGoalId;
    private string _goalTitle = string.Empty;
    private string _targetAmount = string.Empty;
    private string _currentAmount = string.Empty;
    private DateTime _deadline = DateTime.Today.AddMonths(3);
    private bool _isPinned;
    private SavingsGoal? _selectedGoal;

    public GoalsPageViewModel(BudgetDataService budgetDataService)
    {
        _budgetDataService = budgetDataService;
        _budgetDataService.DataChanged += async (_, _) => await HandleDataChangedAsync(LoadAsync);

        SaveGoalCommand = new Command(async () => await SaveGoalAsync());
        DeleteGoalCommand = new Command<SavingsGoal>(async goal => await DeleteGoalAsync(goal));
        ResetFormCommand = new Command(ResetForm);
    }

    public ObservableCollection<SavingsGoal> Goals { get; } = [];

    public ICommand SaveGoalCommand { get; }

    public ICommand DeleteGoalCommand { get; }

    public ICommand ResetFormCommand { get; }

    public string GoalTitle
    {
        get => _goalTitle;
        set => SetProperty(ref _goalTitle, value);
    }

    public string TargetAmount
    {
        get => _targetAmount;
        set => SetProperty(ref _targetAmount, value);
    }

    public string CurrentAmount
    {
        get => _currentAmount;
        set => SetProperty(ref _currentAmount, value);
    }

    public DateTime Deadline
    {
        get => _deadline;
        set => SetProperty(ref _deadline, value);
    }

    public bool IsPinned
    {
        get => _isPinned;
        set => SetProperty(ref _isPinned, value);
    }

    public SavingsGoal? SelectedGoal
    {
        get => _selectedGoal;
        set
        {
            if (SetProperty(ref _selectedGoal, value) && value is not null)
            {
                _editingGoalId = value.Id;
                GoalTitle = value.Title;
                TargetAmount = value.TargetAmount.ToString("N2");
                CurrentAmount = value.CurrentAmount.ToString("N2");
                Deadline = value.Deadline;
                IsPinned = value.IsPinned;
            }
        }
    }

    public Task LoadAsync()
    {
        return RunBusyOperationAsync(async () =>
        {
            var snapshot = await _budgetDataService.GetSnapshotAsync();

            Goals.Clear();
            foreach (var goal in snapshot.Goals.OrderByDescending(goal => goal.IsPinned).ThenBy(goal => goal.Deadline))
            {
                Goals.Add(goal);
            }
        }, errorPrefix: "Nu am putut incarca obiectivele");
    }

    private async Task SaveGoalAsync()
    {
        await RunBusyOperationAsync(async () =>
        {
            await _budgetDataService.SaveGoalAsync(_editingGoalId, GoalTitle, TargetAmount, CurrentAmount, Deadline, IsPinned);
            ResetForm();
        }, successMessage: "Obiectivul a fost salvat.", errorPrefix: "Nu am putut salva obiectivul");
    }

    private async Task DeleteGoalAsync(SavingsGoal? goal)
    {
        if (goal is null)
        {
            return;
        }

        await RunBusyOperationAsync(async () =>
        {
            await _budgetDataService.DeleteGoalAsync(goal.Id);

            if (_editingGoalId == goal.Id)
            {
                ResetForm();
            }
        }, successMessage: "Obiectivul a fost sters.", errorPrefix: "Nu am putut sterge obiectivul");
    }

    private void ResetForm()
    {
        _editingGoalId = null;
        SelectedGoal = null;
        GoalTitle = string.Empty;
        TargetAmount = string.Empty;
        CurrentAmount = string.Empty;
        Deadline = DateTime.Today.AddMonths(3);
        IsPinned = false;
    }
}
