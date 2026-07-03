using ScraperProject.Models;

namespace ScraperProject.Services;

/// <summary>
/// Абстракция экспорта результатов. Позже можно добавить
/// CsvExportService или SqliteExportService, реализовав тот же интерфейс.
/// </summary>
public interface IExcelExportService
{
    void AddEntries(IEnumerable<PriceEntry> entries);
    Task<string> SaveAsync(string outputPath);
    int TotalRows { get; }
}
