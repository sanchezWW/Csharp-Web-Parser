using Microsoft.Playwright;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Collections.Generic;

Console.WriteLine("=== Коммерческий Парсер (Playwright + EPPlus) ===");

ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

// 1. ЗАГРУЗКА КОНФИГУРАЦИИ
string configPath = Path.Combine(Directory.GetCurrentDirectory(), "config.json");
if (!File.Exists(configPath)) { Console.WriteLine("Файл config.json не найден!"); return; }

using JsonDocument configDoc = JsonDocument.Parse(await File.ReadAllTextAsync(configPath));
var trackedGames = configDoc.RootElement.GetProperty("TrackedGames");
int delaySeconds = configDoc.RootElement.GetProperty("Settings").GetProperty("DelayBetweenRequestsSeconds").GetInt32();

// 2. ПОДГОТОВКА EXCEL
using var package = new ExcelPackage();
var worksheet = package.Workbook.Worksheets.Add("Выгрузка");
worksheet.Cells[1, 1].Value = "Дата";
worksheet.Cells[1, 2].Value = "Игра";
worksheet.Cells[1, 3].Value = "Описание";
worksheet.Cells[1, 4].Value = "Цена (число)";

using (var range = worksheet.Cells["A1:D1"]) {
    range.Style.Font.Bold = true;
    range.Style.Fill.PatternType = ExcelFillStyle.Solid;
    range.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(46, 117, 182));
    range.Style.Font.Color.SetColor(Color.White);
}

int row = 2;
int COUNTSAVEELEMENTS = 20;

// 3. ЗАПУСК БРАУЗЕРА
using var playwright = await Playwright.CreateAsync();
await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions {
    Headless = false,
    Args = new[] { "--disable-blink-features=AutomationControlled" }
});

var context = await browser.NewContextAsync(new BrowserNewContextOptions {
    UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/122.0.0.0 Safari/537.36"
});
var page = await context.NewPageAsync();

// 4. ЦИКЛ ПАРСИНГА
foreach (var game in trackedGames.EnumerateArray())
{
    string gameName = game.GetProperty("GameName").GetString() ?? "Game";
    string url = game.GetProperty("Url").GetString() ?? "";

    Console.WriteLine($"\n[Парсинг] {gameName}...");

    try {
        await page.GotoAsync(url, new PageGotoOptions { WaitUntil = WaitUntilState.Load, Timeout = 60000 });
        await Task.Delay(5000); // Ждем прогрузки всех скриптов

        // Ждем появления контейнера с товарами
        await page.WaitForSelectorAsync("body");

        // УНИВЕРСАЛЬНЫЙ СБОР ДАННЫХ ЧЕРЕЗ JS
        // ВАЖНО: возвращаем JSON-строку, а не объект/массив напрямую —
        // у Playwright.NET есть баг в EvaluateArgumentValueConverter при
        // десериализации List<Dictionary<string,string>> как результата EvaluateAsync<T>.
        // Поэтому сериализуем в JS и парсим сами через System.Text.Json.
        var scrapedJson = await page.EvaluateAsync<string>(@"() => {
            const results = [];
            // Ищем любые элементы, которые могут быть строкой товара
            const selectors = ['.tc-item', '.tc-item.a', 'a.tc-item', '.offer'];
            let items = [];

            for (let s of selectors) {
                items = document.querySelectorAll(s);
                if (items.length > 0) break;
            }

            items.forEach(item => {
                // Ищем цену (обычно класс tc-price или текст с символом валюты)
                const priceEl = item.querySelector('.tc-price, .price');
                // Ищем описание (обычно tc-desc-text или просто tc-desc)
                const descEl = item.querySelector('.tc-desc-text, .tc-desc, .desc');

                if (priceEl) {
                    results.push({
                        d: descEl ? descEl.innerText.trim() : 'Без описания',
                        p: priceEl.innerText.trim()
                    });
                }
            });
            return JSON.stringify(results);
        }");

        List<Dictionary<string, string>>? scrapedData = null;
        try
        {
            scrapedData = JsonSerializer.Deserialize<List<Dictionary<string, string>>>(scrapedJson);
        }
        catch (JsonException jex)
        {
            Console.WriteLine($"[!] Не удалось разобрать JSON: {jex.Message}");
        }

        if (scrapedData == null || scrapedData.Count == 0) {
            Console.WriteLine("[!] Данные не найдены. Проверьте, открыта ли страница со списком товаров.");
            continue;
        }

        Console.WriteLine($"[Инфо] Найдено элементов: {scrapedData.Count}. Обрабатываю первые 15...");

        int addedCount = 0;
        foreach (var entry in scrapedData.Take(COUNTSAVEELEMENTS))
        {
            string rawDesc = entry.TryGetValue("d", out var d) ? d : "Нет описания";
            string rawPrice = entry.TryGetValue("p", out var p) ? p : "";

            // Извлекаем только цифры. Если цена "1 250.50 ₽", получим "1250"
            string cleanPrice = Regex.Replace(rawPrice, @"[^\d]", "");

            if (double.TryParse(cleanPrice, out double priceValue))
            {
                worksheet.Cells[row, 1].Value = DateTime.Now.ToString("G");
                worksheet.Cells[row, 2].Value = gameName;
                worksheet.Cells[row, 3].Value = rawDesc.Length > 100 ? rawDesc.Substring(0, 97) + "..." : rawDesc;
                worksheet.Cells[row, 4].Value = priceValue;
                worksheet.Cells[row, 4].Style.Numberformat.Format = "#,##0.00";

                row++;
                addedCount++;
            }
        }
        Console.WriteLine($"[Успех] Записано лотов: {addedCount}");
        await Task.Delay(delaySeconds * 1000);

    } catch (Exception ex) {
        Console.WriteLine($"[Ошибка] {gameName}: {ex}");
    }
}

// 5. СОХРАНЕНИЕ
if (row > 2) {
    worksheet.Cells[1, 1, row - 1, 4].AutoFitColumns();
    string path = Path.Combine(Directory.GetCurrentDirectory(), "gaming_market_prices.xlsx");
    await package.SaveAsAsync(new FileInfo(path));
    Console.WriteLine($"\n=== ГОТОВО! Сохранено {row-2} записей в файл {path} ===");
} else {
    Console.WriteLine("\n[!] Не удалось собрать данные для сохранения.");
}