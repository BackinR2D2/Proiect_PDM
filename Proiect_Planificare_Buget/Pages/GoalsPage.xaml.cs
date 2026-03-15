using Microsoft.Extensions.DependencyInjection;
using Proiect_Planificare_Buget.ViewModels;

namespace Proiect_Planificare_Buget.Pages;

public partial class GoalsPage : ContentPage
{
    public GoalsPage()
    {
        InitializeComponent();
        BindingContext = App.Services.GetRequiredService<GoalsPageViewModel>();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (BindingContext is GoalsPageViewModel viewModel)
        {
            await viewModel.LoadAsync();
        }
    }
}
