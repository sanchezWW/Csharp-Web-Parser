using ScraperProject.Models;

namespace ScraperProject.Services;

/// <summary>
/// Абстракция парсера маркетплейса. Реализация не должна протекать
/// наружу (Playwright, HTML-селекторы — детали реализации).
/// Это позволяет позже добавить, например, SteamScraperService
/// без изменения кода, который вызывает парсинг.
/// </summary>
public interface IScraperService : IAsyncDisposable
{
    /// <summary>
    /// Запускает браузер и готовит сессию к работе.
    /// Вызывается один раз перед серией ScrapeAsync.
    /// </summary>
    Task InitializeAsync();

    /// <summary>
    /// Парсит один источник (одну игру/категорию) и возвращает найденные лоты.
    /// Пустой список — не ошибка (например, распродано), исключение — сбой.
    /// </summary>
    Task<List<PriceEntry>> ScrapeAsync(TrackedGame game, CancellationToken cancellationToken = default);
}
