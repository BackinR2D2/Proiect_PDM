namespace Proiect_Planificare_Buget.Models;

public sealed class ExchangeRateItem
{
    public string CurrencyCode { get; init; } = string.Empty;

    public decimal Value { get; init; }

    public string Description { get; init; } = string.Empty;

    public string DisplayValue => Value.ToString("N4");
}
