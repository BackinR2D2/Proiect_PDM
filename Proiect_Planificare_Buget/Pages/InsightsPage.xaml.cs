using Microsoft.Extensions.DependencyInjection;
using Proiect_Planificare_Buget.ViewModels;

namespace Proiect_Planificare_Buget.Pages;

public partial class InsightsPage : ContentPage
{
    public InsightsPage()
    {
        InitializeComponent();
        BindingContext = App.Services.GetRequiredService<InsightsPageViewModel>();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (BindingContext is InsightsPageViewModel viewModel)
        {
            await viewModel.LoadAsync();
        }
    }
}
