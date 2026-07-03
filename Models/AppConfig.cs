namespace ScraperProject.Models;

/// <summary>
/// Корневая модель config.json. Строго типизированная — вместо ручного
/// обхода JsonDocument.GetProperty(), что легко сломать опечаткой.
/// </summary>
public class AppConfig
{
    public ScraperSettings Settings { get; set; } = new();
    public List<TrackedGame> TrackedGames { get; set; } = new();
}

public class ScraperSettings
{
    public int DelayBetweenRequestsSeconds { get; set; } = 3;
    public bool UseProxy { get; set; } = false;
    public string? ProxyAddress { get; set; }

    /// <summary>
    /// Headless-режим браузера. По умолчанию false для отладки (видно, что происходит),
    /// но для планировщика/сервера обычно нужен true.
    /// </summary>
    public bool Headless { get; set; } = false;
}

public class TrackedGame
{
    public required string GameName { get; set; }
    public required string Url { get; set; }
    public string? PlatformName { get; set; }
}
