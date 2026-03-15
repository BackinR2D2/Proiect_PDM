using System.Xml.Linq;
using Proiect_Planificare_Buget.Models;

namespace Proiect_Planificare_Buget.Services;

public sealed class XmlReportService
{
    public async Task<string> ExportAsync(BudgetAppData data, IEnumerable<BudgetStatusItem> budgetStatuses)
    {
        var fileName = $"budget-report-{DateTime.Now:yyyyMMdd-HHmmss}.xml";
        var filePath = Path.Combine(FileSystem.AppDataDirectory, fileName);

        var document = new XDocument(
            new XElement("BudgetPlannerReport",
                new XAttribute("generatedAt", DateTime.Now.ToString("O")),
                new XElement("Settings",
                    new XElement("FullName", data.Settings.FullName),
                    new XElement("DefaultCurrency", data.Settings.DefaultCurrency),
                    new XElement("WeekStartsOn", data.Settings.WeekStartsOn),
                    new XElement("AutoSyncRates", data.Settings.AutoSyncRates),
                    new XElement("RoundUpSavings", data.Settings.RoundUpSavings),
                    new XElement("ReminderDay", data.Settings.ReminderDay)
                ),
                new XElement("Budgets",
                    budgetStatuses.Select(budget =>
                        new XElement("Budget",
                            new XAttribute("name", budget.Name),
                            new XElement("MonthlyLimit", budget.MonthlyLimit),
                            new XElement("Spent", budget.Spent),
                            new XElement("Remaining", budget.Remaining),
                            new XElement("Status", budget.StatusLabel)
                        ))
                ),
                new XElement("Goals",
                    data.Goals.Select(goal =>
                        new XElement("Goal",
                            new XAttribute("title", goal.Title),
                            new XElement("TargetAmount", goal.TargetAmount),
                            new XElement("CurrentAmount", goal.CurrentAmount),
                            new XElement("Deadline", goal.Deadline.ToString("yyyy-MM-dd")),
                            new XElement("Pinned", goal.IsPinned)
                        ))
                ),
                new XElement("Transactions",
                    data.Transactions.OrderByDescending(transaction => transaction.OccurredOn).Select(transaction =>
                        new XElement("Transaction",
                            new XAttribute("id", transaction.Id),
                            new XElement("Title", transaction.Title),
                            new XElement("Type", transaction.TypeLabel),
                            new XElement("Category", transaction.Category),
                            new XElement("Amount", transaction.Amount),
                            new XElement("OccurredOn", transaction.OccurredOn.ToString("O")),
                            new XElement("Recurring", transaction.IsRecurring),
                            new XElement("Notes", transaction.Notes)
                        ))
                )
            )
        );

        await File.WriteAllTextAsync(filePath, document.ToString());
        return filePath;
    }
}
