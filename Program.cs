using AngleSharp;
using OfficeOpenXml;
using System.IO;

Console.WriteLine("=== Запуск парсера Цитат ===");

ExcelPackage.License.SetNonCommercialOrganization("My Noncommercial organization");

var config = Configuration.Default.WithDefaultLoader();
var context = BrowsingContext.New(config);
var url = "https://quotes.toscrape.com/";

Console.WriteLine($"Загрузка страницы: {url} ...");
var document = await context.OpenAsync(url);

var quoteElements = document.QuerySelectorAll(".quote");
Console.WriteLine($"Найдено цитат на странице: {quoteElements.Length}\n");

using var package = new ExcelPackage();

var worksheet = package.Workbook.Worksheets.Add("Цитаты");

worksheet.Cells[1, 1].Value = "Автор";
worksheet.Cells[1, 2].Value = "Цитата";

worksheet.Cells["A1:B1"].Style.Font.Bold = true;

int row = 2;

foreach (var element in quoteElements)
{
    var author = element.QuerySelector(".author")?.TextContent ?? "Неизвестный автор";

    var text = element.QuerySelector(".text")?.TextContent ?? "Текст отсутствует";

    text = text.Trim('“', '”');

    Console.WriteLine($"Автор: {author} | Цитата: {text}\n");

    worksheet.Cells[row, 1].Value = author; // Колонка 1 (А) — Автор
    worksheet.Cells[row, 2].Value = text;   // Колонка 2 (B) — Цитата
    
    row++;
}
worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

var filePath = Path.Combine(Directory.GetCurrentDirectory(), "quotes_report.xlsx");
FileInfo file = new FileInfo(filePath);
await package.SaveAsAsync(file);

Console.WriteLine($"=== Парсинг завершен. Файл успешно сохранен по адресу: ===");
Console.WriteLine(filePath);