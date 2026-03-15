using Microsoft.Extensions.DependencyInjection;
using Proiect_Planificare_Buget.ViewModels;

namespace Proiect_Planificare_Buget.Pages;

public partial class SettingsPage : ContentPage
{
    public SettingsPage()
    {
        InitializeComponent();
        BindingContext = App.Services.GetRequiredService<SettingsPageViewModel>();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (BindingContext is SettingsPageViewModel viewModel)
        {
            await viewModel.LoadAsync();
        }
    }
}
