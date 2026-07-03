using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Playwright;
using ScraperProject.Models;

namespace ScraperProject.Services;

/// <summary>
/// Реализация парсера конкретно для FunPay через Playwright.
/// Вся логика браузера, JS-селекторов и работы с DOM инкапсулирована здесь —
/// вызывающий код про это ничего не знает.
/// </summary>
public class FunPayScraperService : IScraperService
{
    private readonly ScraperSettings _settings;
    private IPlaywright? _playwright;
    private IBrowser? _browser;
    private IPage? _page;
    private int CountSaveElements = 20;

    // JS возвращает JSON-строку, а не объект напрямую — у Playwright.NET есть
    // баг в EvaluateArgumentValueConverter при десериализации
    // List<Dictionary<string,string>> как результата EvaluateAsync<T>.
    private const string ExtractionScript = @"() => {
        const results = [];
        const selectors = ['.tc-item', '.tc-item.a', 'a.tc-item', '.offer'];
        let items = [];

        for (let s of selectors) {
            items = document.querySelectorAll(s);
            if (items.length > 0) break;
        }

        items.forEach(item => {
            const priceEl = item.querySelector('.tc-price, .price');
            const descEl = item.querySelector('.tc-desc-text, .tc-desc, .desc');

            if (priceEl) {
                results.push({
                    d: descEl ? descEl.innerText.trim() : 'Без описания',
                    p: priceEl.innerText.trim()
                });
            }
        });
        return JSON.stringify(results);
    }";

    public FunPayScraperService(ScraperSettings settings)
    {
        _settings = settings;
    }

    public async Task InitializeAsync()
    {
        _playwright = await Playwright.CreateAsync();
        _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = _settings.Headless,
            Args = new[] { "--disable-blink-features=AutomationControlled" }
        });

        var context = await _browser.NewContextAsync(new BrowserNewContextOptions
        {
            UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/122.0.0.0 Safari/537.36"
        });

        _page = await context.NewPageAsync();
    }

    public async Task<List<PriceEntry>> ScrapeAsync(TrackedGame game, CancellationToken cancellationToken = default)
    {
        if (_page is null)
            throw new InvalidOperationException("Сервис не инициализирован. Сначала вызовите InitializeAsync().");

        await _page.GotoAsync(game.Url, new PageGotoOptions { WaitUntil = WaitUntilState.Load, Timeout = 60000 });
        await Task.Delay(5000, cancellationToken); // Ждём прогрузки скриптов
        await _page.WaitForSelectorAsync("body");

        string scrapedJson = await _page.EvaluateAsync<string>(ExtractionScript);

        List<Dictionary<string, string>>? rawItems;
        try
        {
            rawItems = JsonSerializer.Deserialize<List<Dictionary<string, string>>>(scrapedJson);
        }
        catch (JsonException jex)
        {
            throw new InvalidOperationException($"Не удалось разобрать JSON от страницы {game.Url}: {jex.Message}", jex);
        }

        if (rawItems is null || rawItems.Count == 0)
            return new List<PriceEntry>();

        var results = new List<PriceEntry>();
        foreach (var entry in rawItems.Take(CountSaveElements))
        {
            string rawDesc = entry.TryGetValue("d", out var d) ? d : "Нет описания";
            string rawPrice = entry.TryGetValue("p", out var p) ? p : "";

            string cleanPrice = Regex.Replace(rawPrice, @"[^\d]", "");
            if (!double.TryParse(cleanPrice, out double priceValue))
                continue;

            results.Add(new PriceEntry
            {
                GameName = game.GameName,
                Description = rawDesc.Length > 100 ? rawDesc[..97] + "..." : rawDesc,
                Price = priceValue,
                RawPriceText = rawPrice
            });
        }

        return results;
    }

    public async ValueTask DisposeAsync()
    {
        if (_browser is not null)
            await _browser.CloseAsync();

        _playwright?.Dispose();
    }
}
