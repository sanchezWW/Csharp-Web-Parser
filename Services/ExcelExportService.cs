using System.Drawing;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using ScraperProject.Models;

namespace ScraperProject.Services;

/// <summary>
/// Собирает PriceEntry в Excel-лист и сохраняет на диск.
/// Ничего не знает про парсинг — принимает уже готовые данные.
/// </summary>
public class ExcelExportService : IExcelExportService
{
    private readonly ExcelPackage _package;
    private readonly ExcelWorksheet _worksheet;
    private int _row = 2;

    public int TotalRows => _row - 2;

    public ExcelExportService()
    {
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        _package = new ExcelPackage();
        _worksheet = _package.Workbook.Worksheets.Add("Выгрузка");

        _worksheet.Cells[1, 1].Value = "Дата";
        _worksheet.Cells[1, 2].Value = "Игра";
        _worksheet.Cells[1, 3].Value = "Описание";
        _worksheet.Cells[1, 4].Value = "Цена (число)";

        using var headerRange = _worksheet.Cells["A1:D1"];
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Fill.PatternType = ExcelFillStyle.Solid;
        headerRange.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(46, 117, 182));
        headerRange.Style.Font.Color.SetColor(Color.White);
    }

    public void AddEntries(IEnumerable<PriceEntry> entries)
    {
        foreach (var entry in entries)
        {
            _worksheet.Cells[_row, 1].Value = entry.ScrapedAt.ToString("G");
            _worksheet.Cells[_row, 2].Value = entry.GameName;
            _worksheet.Cells[_row, 3].Value = entry.Description;
            _worksheet.Cells[_row, 4].Value = entry.Price;
            _worksheet.Cells[_row, 4].Style.Numberformat.Format = "#,##0.00";
            _row++;
        }
    }

    public async Task<string> SaveAsync(string outputPath)
    {
        if (TotalRows == 0)
            throw new InvalidOperationException("Нечего сохранять — не было добавлено ни одной записи.");

        _worksheet.Cells[1, 1, _row - 1, 4].AutoFitColumns();
        await _package.SaveAsAsync(new FileInfo(outputPath));
        return outputPath;
    }
}
