using System.Text;
using Proiect_Planificare_Buget.Models;

namespace Proiect_Planificare_Buget.Services;

public sealed class CsvReportService
{
    public async Task<string> ExportAsync(IEnumerable<TransactionRecord> transactions)
    {
        var fileName = $"budget-report-{DateTime.Now:yyyyMMdd-HHmmss}.csv";
        var filePath = Path.Combine(FileSystem.AppDataDirectory, fileName);

        var sb = new StringBuilder();
        sb.Append('\uFEFF');
        sb.AppendLine("Data,Titlu,Tip,Categorie,Suma,Recursiv,Note");

        foreach (var t in transactions.OrderByDescending(t => t.OccurredOn))
        {
            sb.AppendLine(string.Join(",",
                EscapeField(t.OccurredOn.ToString("yyyy-MM-dd HH:mm")),
                EscapeField(t.Title),
                EscapeField(t.TypeLabel),
                EscapeField(t.Category),
                t.Amount.ToString("N2"),
                t.IsRecurring ? "Da" : "Nu",
                EscapeField(t.Notes)));
        }

        await File.WriteAllTextAsync(filePath, sb.ToString(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        return filePath;
    }

    private static string EscapeField(string value)
    {
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n') || value.Contains('\r'))
            return $"\"{value.Replace("\"", "\"\"")}\"";
        return value;
    }
}
