using System.Text.Json;
using Proiect_Planificare_Buget.Models;

namespace Proiect_Planificare_Buget.Services;

public sealed class ExchangeRateService
{
    private readonly HttpClient _httpClient;

    public ExchangeRateService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<IReadOnlyList<ExchangeRateItem>> GetExchangeRatesAsync(string baseCurrency)
    {
        var targetCurrencies = new[] { "EUR", "USD", "GBP" }
            .Where(currency => !currency.Equals(baseCurrency, StringComparison.OrdinalIgnoreCase))
            .ToArray();

        var requestUri = $"https://api.frankfurter.app/latest?from={baseCurrency}&to={string.Join(",", targetCurrencies)}";
        using var response = await _httpClient.GetAsync(requestUri);
        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadAsStringAsync();
        var exchangeRates = JsonSerializer.Deserialize<ExchangeRatesResponse>(payload)
                            ?? throw new InvalidOperationException("Raspunsul primit pentru cursurile valutare este invalid.");

        return targetCurrencies
            .Select(code => new ExchangeRateItem
            {
                CurrencyCode = code,
                Value = exchangeRates.Rates.TryGetValue(code, out var rate) ? rate : 0m,
                Description = code switch
                {
                    "EUR" => "Euro",
                    "USD" => "Dolar american",
                    "GBP" => "Lira sterlina",
                    _ => code
                }
            })
            .ToList();
    }
}
