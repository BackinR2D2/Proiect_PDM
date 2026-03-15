using Microsoft.Extensions.Logging;
using Proiect_Planificare_Buget.Services;
using Proiect_Planificare_Buget.ViewModels;

namespace Proiect_Planificare_Buget;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        builder.Services.AddSingleton(new HttpClient());
        builder.Services.AddSingleton<BudgetDataService>();
        builder.Services.AddSingleton<ExchangeRateService>();
        builder.Services.AddSingleton<XmlReportService>();

        builder.Services.AddSingleton<MainPageViewModel>();
        builder.Services.AddSingleton<TransactionsPageViewModel>();
        builder.Services.AddSingleton<BudgetsPageViewModel>();
        builder.Services.AddSingleton<GoalsPageViewModel>();
        builder.Services.AddSingleton<InsightsPageViewModel>();
        builder.Services.AddSingleton<SettingsPageViewModel>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
