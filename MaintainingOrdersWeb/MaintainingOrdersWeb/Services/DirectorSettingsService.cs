using System.Text.Json;
using MaintainingOrdersWeb.ViewModels;

namespace MaintainingOrdersWeb.Services;

public class DirectorSettingsService
{
    private readonly IWebHostEnvironment _environment;
    private readonly JsonSerializerOptions _serializerOptions = new() { WriteIndented = true };

    public DirectorSettingsService(IWebHostEnvironment environment)
    {
        _environment = environment;
    }

    public async Task<DirectorSettingsViewModel> LoadAsync(string directorName)
    {
        var path = GetSettingsPath();
        if (!File.Exists(path))
        {
            return CreateDefault(directorName);
        }

        await using var stream = File.OpenRead(path);
        var settings = await JsonSerializer.DeserializeAsync<DirectorSettingsViewModel>(stream, _serializerOptions)
            ?? CreateDefault(directorName);

        if (string.IsNullOrWhiteSpace(settings.DirectorName))
        {
            settings.DirectorName = directorName;
        }

        return settings;
    }

    public async Task SaveAsync(DirectorSettingsViewModel model)
    {
        model.UpdatedAtUtc = DateTime.UtcNow;
        var path = GetSettingsPath();
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);

        await using var stream = File.Create(path);
        await JsonSerializer.SerializeAsync(stream, model, _serializerOptions);
    }

    private DirectorSettingsViewModel CreateDefault(string directorName) => new()
    {
        DirectorName = directorName,
        DashboardPeriodDays = 30,
        LowStockThreshold = 10,
        MonthlyRevenueTarget = 500000m,
        MinimumMarginPercent = 15m,
        AutoOpenReports = false,
        ShowStockAlertsOnDashboard = true,
        ReceiveDailyDigest = true,
        StrategicNotes = "Усилить контроль по остаткам, развивать ключевых клиентов и еженедельно проверять маржинальность каталога.",
        UpdatedAtUtc = DateTime.UtcNow
    };

    private string GetSettingsPath() => Path.Combine(_environment.ContentRootPath, "App_Data", "director-settings.json");
}
