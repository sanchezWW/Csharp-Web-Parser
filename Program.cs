using System.CommandLine;
using Microsoft.Extensions.DependencyInjection;
using ScraperProject.Configuration;
using ScraperProject.Services;
using ScraperProject.Models;

var rootCommand = CliOptions.BuildRootCommand(RunAsync);
return await rootCommand.Parse(args).InvokeAsync();

async Task RunAsync(ParsedArgs cliArgs)
{
    Console.WriteLine("=== Коммерческий Парсер (Playwright + EPPlus) ===");

    AppConfig config;
    try
    {
        config = ConfigLoader.Load(cliArgs.ConfigPath);
        config = CliOptions.ApplyOverrides(config, cliArgs);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[Ошибка конфигурации] {ex.Message}");
        return;
    }

    Console.WriteLine($"[Конфигурация] Игр к парсингу: {config.TrackedGames.Count}, " +
                       $"headless: {config.Settings.Headless}, " +
                       $"задержка: {config.Settings.DelayBetweenRequestsSeconds}с");

    var services = new ServiceCollection();
    services.AddSingleton(config.Settings);
    services.AddScoped<IScraperService, FunPayScraperService>();
    services.AddScoped<IExcelExportService, ExcelExportService>();

    await using var provider = services.BuildServiceProvider();
    await using var scope = provider.CreateAsyncScope();

    var scraper = scope.ServiceProvider.GetRequiredService<IScraperService>();
    var exporter = scope.ServiceProvider.GetRequiredService<IExcelExportService>();

    await scraper.InitializeAsync();

    foreach (var game in config.TrackedGames)
    {
        Console.WriteLine($"\n[Парсинг] {game.GameName}...");
        try
        {
            var entries = await scraper.ScrapeAsync(game);

            if (entries.Count == 0)
            {
                Console.WriteLine("[!] Данные не найдены. Проверьте, открыта ли страница со списком товаров.");
                continue;
            }

            exporter.AddEntries(entries);
            Console.WriteLine($"[Успех] Записано лотов: {entries.Count}");
            await Task.Delay(config.Settings.DelayBetweenRequestsSeconds * 1000);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Ошибка] {game.GameName}: {ex}");
        }
    }

    if (exporter.TotalRows > 0)
    {
        await exporter.SaveAsync(cliArgs.OutputPath);
        Console.WriteLine($"\n=== ГОТОВО! Сохранено {exporter.TotalRows} записей в файл {cliArgs.OutputPath} ===");
    }
    else
    {
        Console.WriteLine("\n[!] Не удалось собрать данные для сохранения.");
    }
}