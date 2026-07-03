using System.CommandLine;
using ScraperProject.Models;

namespace ScraperProject.Configuration;

/// <summary>
/// Разбор аргументов командной строки и применение их поверх config.json.
/// Флаги CLI имеют приоритет над файлом конфигурации — так удобно и для
/// разработчика (быстро проверить один флаг), и для планировщика
/// (разные задачи с разными аргументами при одном общем config.json).
/// </summary>
public static class CliOptions
{
    public static RootCommand BuildRootCommand(Func<ParsedArgs, Task> runAsync)
    {
        var configOption = new Option<string>("--config", "-c")
        {
            Description = "Путь к config.json",
            DefaultValueFactory = _ => Path.Combine(Directory.GetCurrentDirectory(), "config.json")
        };

        var outputOption = new Option<string>("--output", "-o")
        {
            Description = "Путь к выходному .xlsx файлу",
            DefaultValueFactory = _ => Path.Combine(Directory.GetCurrentDirectory(), "gaming_market_prices.xlsx")
        };

        var headlessOption = new Option<bool>("--headless")
        {
            Description = "Запустить браузер в headless-режиме (без окна). Перекрывает значение из config.json."
        };

        var gameOption = new Option<string[]>("--game", "-g")
        {
            Description = "Спарсить только указанные игры (можно повторять флаг). По умолчанию — все из config.json.",
            AllowMultipleArgumentsPerToken = true
        };

        var delayOption = new Option<int?>("--delay")
        {
            Description = "Задержка между запросами в секундах. Перекрывает значение из config.json."
        };

        var rootCommand = new RootCommand("Парсер цен на игровые товары с маркетплейсов")
        {
            configOption, outputOption, headlessOption, gameOption, delayOption
        };

        rootCommand.SetAction(async parseResult =>
        {
            var args = new ParsedArgs
            {
                ConfigPath = parseResult.GetValue(configOption)!,
                OutputPath = parseResult.GetValue(outputOption)!,
                Headless = parseResult.GetValue(headlessOption),
                GameFilter = parseResult.GetValue(gameOption) ?? Array.Empty<string>(),
                DelayOverrideSeconds = parseResult.GetValue(delayOption)
            };

            await runAsync(args);
        });

        return rootCommand;
    }

    /// <summary>
    /// Применяет переопределения из CLI поверх загруженного AppConfig.
    /// </summary>
    public static AppConfig ApplyOverrides(AppConfig config, ParsedArgs args)
    {
        if (args.Headless)
            config.Settings.Headless = true;

        if (args.DelayOverrideSeconds is { } delay)
            config.Settings.DelayBetweenRequestsSeconds = delay;

        if (args.GameFilter.Length > 0)
        {
            config.TrackedGames = config.TrackedGames
                .Where(g => args.GameFilter.Any(f =>
                    g.GameName.Contains(f, StringComparison.OrdinalIgnoreCase)))
                .ToList();

            if (config.TrackedGames.Count == 0)
                throw new InvalidOperationException(
                    $"Ни одна игра из config.json не совпала с фильтром: {string.Join(", ", args.GameFilter)}");
        }

        return config;
    }
}

public class ParsedArgs
{
    public required string ConfigPath { get; init; }
    public required string OutputPath { get; init; }
    public bool Headless { get; init; }
    public required string[] GameFilter { get; init; }
    public int? DelayOverrideSeconds { get; init; }
}
