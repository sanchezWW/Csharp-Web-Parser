namespace ScraperProject.Models;

/// <summary>
/// Одна спарсенная запись о лоте (товаре/услуге на маркетплейсе).
/// </summary>
public class PriceEntry
{
    public DateTime ScrapedAt { get; init; } = DateTime.Now;
    public required string GameName { get; init; }
    public required string Description { get; init; }
    public required double Price { get; init; }

    /// <summary>
    /// Сырые данные с сайта до парсинга — удобно для отладки,
    /// если регулярка не смогла вычленить цену.
    /// </summary>
    public string? RawPriceText { get; init; }
}
