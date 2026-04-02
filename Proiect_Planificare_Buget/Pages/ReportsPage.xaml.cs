using Microsoft.Extensions.DependencyInjection;
using Proiect_Planificare_Buget.ViewModels;

namespace Proiect_Planificare_Buget.Pages;

public partial class ReportsPage : ContentPage
{
    public ReportsPage()
    {
        InitializeComponent();
        BindingContext = App.Services.GetRequiredService<ReportsPageViewModel>();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (BindingContext is ReportsPageViewModel viewModel)
        {
            await viewModel.LoadAsync();
        }
    }
}
