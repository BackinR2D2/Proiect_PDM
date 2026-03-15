using System.Text.Json.Serialization;

namespace Proiect_Planificare_Buget.Models;

public sealed class ExchangeRatesResponse
{
    [JsonPropertyName("amount")]
    public decimal Amount { get; set; }

    [JsonPropertyName("base")]
    public string BaseCurrency { get; set; } = string.Empty;

    [JsonPropertyName("date")]
    public string Date { get; set; } = string.Empty;

    [JsonPropertyName("rates")]
    public Dictionary<string, decimal> Rates { get; set; } = [];
}
