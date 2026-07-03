using System.Text.Json;
using ScraperProject.Models;

namespace ScraperProject.Configuration;

/// <summary>
/// Отвечает только за загрузку и валидацию config.json.
/// Ничего не знает о Playwright или Excel — единственная ответственность.
/// </summary>
public static class ConfigLoader
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public static AppConfig Load(string path)
    {
        if (!File.Exists(path))
            throw new FileNotFoundException($"Файл конфигурации не найден: {path}");

        string json = File.ReadAllText(path);
        var config = JsonSerializer.Deserialize<AppConfig>(json, JsonOptions)
            ?? throw new InvalidOperationException("Не удалось разобрать config.json — файл пуст или повреждён.");

        Validate(config);
        return config;
    }

    private static void Validate(AppConfig config)
    {
        if (config.TrackedGames.Count == 0)
            throw new InvalidOperationException("В config.json нет ни одной игры в TrackedGames.");

        foreach (var game in config.TrackedGames)
        {
            if (string.IsNullOrWhiteSpace(game.Url))
                throw new InvalidOperationException($"У игры '{game.GameName}' не указан Url.");

            if (!Uri.TryCreate(game.Url, UriKind.Absolute, out _))
                throw new InvalidOperationException($"Некорректный Url у игры '{game.GameName}': {game.Url}");
        }
    }
}
