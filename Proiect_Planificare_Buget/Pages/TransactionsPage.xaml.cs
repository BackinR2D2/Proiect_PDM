using Microsoft.Extensions.DependencyInjection;
using Proiect_Planificare_Buget.ViewModels;

namespace Proiect_Planificare_Buget.Pages;

public partial class TransactionsPage : ContentPage
{
    public TransactionsPage()
    {
        InitializeComponent();
        BindingContext = App.Services.GetRequiredService<TransactionsPageViewModel>();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (BindingContext is TransactionsPageViewModel viewModel)
        {
            await viewModel.LoadAsync();
        }
    }
}
