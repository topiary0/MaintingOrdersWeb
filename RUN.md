# Запуск проекта MaintainingOrdersWeb

## Файл решения
В репозиторий добавлен файл решения:
- `MaintainingOrdersWeb.sln`

Открывайте именно его в Visual Studio.

## Запуск через HTTP / HTTPS
Профили запуска находятся в:
- `MaintainingOrdersWeb/MaintainingOrdersWeb/Properties/launchSettings.json`

Доступные профили:
- `http`: `http://localhost:5053`
- `https`: `https://localhost:7160` (и также `http://localhost:5053`)

## Команды
Из корня репозитория:

```bash
dotnet restore MaintainingOrdersWeb.sln
dotnet build MaintainingOrdersWeb.sln
dotnet run --project MaintainingOrdersWeb/MaintainingOrdersWeb/MaintainingOrdersWeb.csproj --launch-profile http
# или
dotnet run --project MaintainingOrdersWeb/MaintainingOrdersWeb/MaintainingOrdersWeb.csproj --launch-profile https
```
