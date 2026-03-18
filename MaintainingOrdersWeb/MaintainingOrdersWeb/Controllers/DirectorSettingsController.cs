using MaintainingOrdersWeb.Models;
using MaintainingOrdersWeb.Services;
using MaintainingOrdersWeb.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MaintainingOrdersWeb.Controllers;

[Authorize(Roles = "Директор")]
public class DirectorSettingsController : Controller
{
    private readonly MyDbContext _context;
    private readonly DirectorSettingsService _settingsService;

    public DirectorSettingsController(MyDbContext context, DirectorSettingsService settingsService)
    {
        _context = context;
        _settingsService = settingsService;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var director = await GetCurrentDirectorAsync();
        var settings = await _settingsService.LoadAsync(director?.FullName ?? User.Identity?.Name ?? "Директор");

        await PopulateSummaryAsync(settings);
        ViewBag.DirectorApprovalCenter = await BuildDirectorApprovalCenterAsync(settings);
        return View(settings);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(DirectorSettingsViewModel model)
    {
        var director = await GetCurrentDirectorAsync();
        model.DirectorName = director?.FullName ?? model.DirectorName;

        if (!ModelState.IsValid)
        {
            await PopulateSummaryAsync(model);
            ViewBag.DirectorApprovalCenter = await BuildDirectorApprovalCenterAsync(model);
            return View(model);
        }

        await _settingsService.SaveAsync(model);
        TempData["SettingsSaved"] = "Настройки директора сохранены.";

        return RedirectToAction(nameof(Index));
    }

    private async Task<User?> GetCurrentDirectorAsync()
    {
        return await _context.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Login == User.Identity!.Name);
    }

    private async Task<DirectorApprovalCenterViewModel> BuildDirectorApprovalCenterAsync(DirectorSettingsViewModel settings)
    {
        return new DirectorApprovalCenterViewModel
        {
            OrdersAwaitingReview = await _context.Orders
                .Include(o => o.Status)
                .CountAsync(o => EF.Functions.Like(o.Status.StatusName, "%нов%")
                              || EF.Functions.Like(o.Status.StatusName, "%соглас%")),
            LowMarginProducts = await _context.Products
                .CountAsync(p => p.SalePrice > 0
                              && ((p.SalePrice - p.PurchasePrice) / p.SalePrice) * 100 < settings.MinimumMarginPercent),
            CriticalStockItems = await _context.Products
                .CountAsync(p => p.Remains <= settings.LowStockThreshold),
            ShipmentsRequiringAttention = await _context.Shipments
                .Include(s => s.Statussh)
                .CountAsync(s => EF.Functions.Like(s.Statussh.StatusName, "%ожид%")
                              || EF.Functions.Like(s.Statussh.StatusName, "%задерж%"))
        };
    }

    private async Task PopulateSummaryAsync(DirectorSettingsViewModel settings)
    {
        ViewBag.SettingsSummary = new
        {
            TotalUsers = await _context.Users.CountAsync(),
            TotalOrders = await _context.Orders.CountAsync(),
            TotalProducts = await _context.Products.CountAsync(),
            LowStockProducts = await _context.Products.CountAsync(p => p.Remains <= settings.LowStockThreshold),
            CurrentMonthRevenue = await _context.Orders
                .Where(o => o.OrderDate.Year == DateOnly.FromDateTime(DateTime.Today).Year
                         && o.OrderDate.Month == DateOnly.FromDateTime(DateTime.Today).Month)
                .SumAsync(o => (decimal?)o.TotalPrice) ?? 0m
        };
    }
}
