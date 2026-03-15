using Microsoft.Extensions.DependencyInjection;
using Proiect_Planificare_Buget.ViewModels;

namespace Proiect_Planificare_Buget.Pages;

public partial class BudgetsPage : ContentPage
{
    public BudgetsPage()
    {
        InitializeComponent();
        BindingContext = App.Services.GetRequiredService<BudgetsPageViewModel>();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (BindingContext is BudgetsPageViewModel viewModel)
        {
            await viewModel.LoadAsync();
        }
    }
}
