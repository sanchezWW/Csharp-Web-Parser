Dev plans:
Паттерн Strategy для парсеров разных площадок — сегодня FunPay, завтра добавить ещё один маркетплейс без переписывания цикла

Экспорт в SQLite вместо/вместе с Excel

# Как раньше, по умолчанию
dotnet run

# Другой конфиг и выходной файл
dotnet run -- --config prod-config.json --output result.xlsx

# Без окна браузера (для сервера/планировщика)
dotnet run -- --headless

# Спарсить только одну игру
dotnet run -- --game "World of Warcraft"

# Несколько флагов сразу + своя задержка
dotnet run -- --headless --delay 5 --game "Counter-Strike"

# Встроенная справка (System.CommandLine генерирует сама)
dotnet run -- --help
