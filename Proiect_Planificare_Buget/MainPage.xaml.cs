using Microsoft.Extensions.DependencyInjection;
using Proiect_Planificare_Buget.ViewModels;

namespace Proiect_Planificare_Buget.Pages;

public partial class MainPage : ContentPage
{
    public MainPage()
    {
        InitializeComponent();
        BindingContext = App.Services.GetRequiredService<MainPageViewModel>();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (BindingContext is MainPageViewModel viewModel)
        {
            await viewModel.LoadAsync();
        }
    }
}
