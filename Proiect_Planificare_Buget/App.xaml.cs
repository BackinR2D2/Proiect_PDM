namespace Proiect_Planificare_Buget;

public partial class App : Application
{
    public static IServiceProvider Services { get; private set; } = default!;

    public App(IServiceProvider serviceProvider)
    {
        InitializeComponent();
        Services = serviceProvider;
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        return new Window(new AppShell());
    }
}
