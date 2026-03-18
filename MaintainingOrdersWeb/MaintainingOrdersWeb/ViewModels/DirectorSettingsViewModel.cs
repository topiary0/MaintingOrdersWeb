using System.ComponentModel.DataAnnotations;

namespace MaintainingOrdersWeb.ViewModels;

public class DirectorSettingsViewModel
{
    [Display(Name = "Ответственный директор")]
    public string DirectorName { get; set; } = string.Empty;

    [Range(1, 365)]
    [Display(Name = "Период аналитики, дней")]
    public int DashboardPeriodDays { get; set; } = 30;

    [Range(1, 1000)]
    [Display(Name = "Порог низкого остатка")]
    public int LowStockThreshold { get; set; } = 10;

    [Range(typeof(decimal), "0", "1000000000")]
    [Display(Name = "План выручки на месяц, ₽")]
    public decimal MonthlyRevenueTarget { get; set; } = 500000m;

    [Range(typeof(decimal), "0", "1000000000")]
    [Display(Name = "Минимальная маржинальность, %")]
    public decimal MinimumMarginPercent { get; set; } = 15m;

    [Display(Name = "Автоматически открывать отчеты при входе")]
    public bool AutoOpenReports { get; set; }

    [Display(Name = "Показывать предупреждения по остаткам на главной")]
    public bool ShowStockAlertsOnDashboard { get; set; } = true;

    [Display(Name = "Получать ежедневную сводку по заказам")]
    public bool ReceiveDailyDigest { get; set; } = true;

    [StringLength(500)]
    [Display(Name = "Фокус на ближайший период")]
    public string StrategicNotes { get; set; } = string.Empty;

    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}
