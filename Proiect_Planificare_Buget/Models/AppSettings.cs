namespace Proiect_Planificare_Buget.Models;

public sealed class AppSettings
{
    public string FullName { get; set; } = "Echipa Budget Planner";

    public string DefaultCurrency { get; set; } = "RON";

    public string WeekStartsOn { get; set; } = "Luni";

    public bool AutoSyncRates { get; set; } = true;

    public bool RoundUpSavings { get; set; }

    public int ReminderDay { get; set; } = 5;
}
