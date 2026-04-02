using System.Collections.ObjectModel;
using Proiect_Planificare_Buget.Models;
using Proiect_Planificare_Buget.Services;

namespace Proiect_Planificare_Buget.ViewModels;

public sealed class CategoriesPageViewModel : ViewModelBase
{
    private readonly BudgetDataService _budgetDataService;

    private Guid? _editingCategoryId;
    private string _categoryName = string.Empty;
    private string _selectedKind = "Cheltuiala";
    private CategoryDefinition? _selectedCategory;

    public CategoriesPageViewModel(BudgetDataService budgetDataService)
    {
        _budgetDataService = budgetDataService;
        _budgetDataService.DataChanged += async (_, _) => await HandleDataChangedAsync(LoadAsync);

        SaveCategoryCommand = new Command(async () => await SaveCategoryAsync());
        DeleteCategoryCommand = new Command<CategoryDefinition>(async category => await DeleteCategoryAsync(category));
        ResetFormCommand = new Command(ResetForm);
    }

    public ObservableCollection<CategoryDefinition> Categories { get; } = [];

    public IReadOnlyList<string> KindOptions { get; } = ["Cheltuiala", "Venit"];

    public ICommand SaveCategoryCommand { get; }

    public ICommand DeleteCategoryCommand { get; }

    public ICommand ResetFormCommand { get; }

    public string CategoryName
    {
        get => _categoryName;
        set => SetProperty(ref _categoryName, value);
    }

    public string SelectedKind
    {
        get => _selectedKind;
        set => SetProperty(ref _selectedKind, value);
    }

    public CategoryDefinition? SelectedCategory
    {
        get => _selectedCategory;
        set
        {
            if (SetProperty(ref _selectedCategory, value) && value is not null)
            {
                _editingCategoryId = value.Id;
                CategoryName = value.Name;
                SelectedKind = value.Kind == CategoryKind.Expense ? "Cheltuiala" : "Venit";
            }
        }
    }

    public Task LoadAsync()
    {
        return RunBusyOperationAsync(async () =>
        {
            var snapshot = await _budgetDataService.GetSnapshotAsync();

            Categories.Clear();
            foreach (var category in snapshot.Categories.OrderBy(category => category.Kind).ThenBy(category => category.Name))
            {
                Categories.Add(category);
            }
        }, errorPrefix: "Nu am putut incarca categoriile");
    }

    private async Task SaveCategoryAsync()
    {
        await RunBusyOperationAsync(async () =>
        {
            var kind = SelectedKind == "Cheltuiala" ? CategoryKind.Expense : CategoryKind.Income;
            await _budgetDataService.SaveCategoryAsync(_editingCategoryId, CategoryName, kind);
            ResetForm();
        }, successMessage: "Categoria a fost salvata.", errorPrefix: "Nu am putut salva categoria");
    }

    private async Task DeleteCategoryAsync(CategoryDefinition? category)
    {
        if (category is null)
            return;

        await RunBusyOperationAsync(async () =>
        {
            await _budgetDataService.DeleteCategoryAsync(category.Id);

            if (_editingCategoryId == category.Id)
            {
                ResetForm();
            }
        }, successMessage: "Categoria a fost stearsa.", errorPrefix: "Nu am putut sterge categoria");
    }

    private void ResetForm()
    {
        _editingCategoryId = null;
        SelectedCategory = null;
        CategoryName = string.Empty;
        SelectedKind = "Cheltuiala";
    }
}
