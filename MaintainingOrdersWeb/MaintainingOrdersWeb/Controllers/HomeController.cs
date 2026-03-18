using MaintainingOrdersWeb.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MaintainingOrdersWeb.Services;
using System.Diagnostics;

namespace MaintainingOrdersWeb.Controllers
{
    public class HomeController : Controller
    {
        private readonly MyDbContext _context;
        private readonly ILogger<HomeController> _logger;
        private readonly DirectorSettingsService _directorSettingsService;

        public HomeController(MyDbContext context, ILogger<HomeController> logger, DirectorSettingsService directorSettingsService)
        {
            _context = context;
            _logger = logger;
            _directorSettingsService = directorSettingsService;
        }

        [Authorize]
        public async Task<IActionResult> Index()
        {
            var user = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Login == User.Identity!.Name);
            ViewBag.UserName = user?.FullName ?? User.Identity?.Name;

            ViewBag.TotalProducts = await _context.Products.CountAsync();
            ViewBag.TotalClients = await _context.Clients.CountAsync();
            ViewBag.TotalOrders = await _context.Orders.CountAsync();
            ViewBag.TotalRevenue = await _context.Orders.SumAsync(o => (decimal?)o.TotalPrice) ?? 0m;

            var directorSettings = await _directorSettingsService.LoadAsync(ViewBag.UserName);
            ViewBag.DirectorSettings = directorSettings;

            var today = DateOnly.FromDateTime(DateTime.Today);
            ViewBag.TodayOrdersCount = await _context.Orders.CountAsync(o => o.OrderDate == today);
            ViewBag.ActiveShipmentsCount = await _context.Shipments.CountAsync();
            ViewBag.LowStockCount = await _context.Products.CountAsync(p => p.Remains <= directorSettings.LowStockThreshold);

            var lowStockProducts = await _context.Products
                .OrderBy(p => p.Remains)
                .Where(p => p.Remains <= directorSettings.LowStockThreshold)
                .Take(7)
                .ToListAsync();
            ViewBag.LowStockProducts = lowStockProducts;
            ViewBag.LowStockLabels = lowStockProducts.Select(p => p.Name).ToList();
            ViewBag.LowStockCounts = lowStockProducts.Select(p => p.Remains).ToList();

            var monthStarts = Enumerable.Range(0, 6)
                .Select(i => new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1).AddMonths(-5 + i))
                .ToList();
            var minMonth = monthStarts.First();
            var minOrderDate = new DateOnly(minMonth.Year, minMonth.Month, 1);

            var recentOrders = await _context.Orders
                .Where(o => o.OrderDate >= minOrderDate)
                .ToListAsync();

            var monthStats = recentOrders
                .GroupBy(o => new { o.OrderDate.Year, o.OrderDate.Month })
                .Select(g => new { g.Key.Year, g.Key.Month, Revenue = g.Sum(x => x.TotalPrice), Count = g.Count() })
                .ToList();

            ViewBag.MonthLabels = monthStarts.Select(m => m.ToString("MM.yyyy")).ToList();
            ViewBag.MonthRevenue = monthStarts
                .Select(m => monthStats.FirstOrDefault(r => r.Year == m.Year && r.Month == m.Month)?.Revenue ?? 0m)
                .ToList();
            ViewBag.MonthOrders = monthStarts
                .Select(m => monthStats.FirstOrDefault(r => r.Year == m.Year && r.Month == m.Month)?.Count ?? 0)
                .ToList();

            var managerStatusStats = await _context.Orders
                .Include(o => o.Status)
                .GroupBy(o => o.Status.StatusName)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .ToListAsync();
            ViewBag.ManagerStatusStats = managerStatusStats;
            ViewBag.ManagerStatusLabels = managerStatusStats.Select(x => x.Status).ToList();
            ViewBag.ManagerStatusCounts = managerStatusStats.Select(x => x.Count).ToList();

            var shipmentStatusStats = await _context.Shipments
                .Include(s => s.Statussh)
                .GroupBy(s => s.Statussh.StatusName)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .ToListAsync();
            ViewBag.ShipmentStatusLabels = shipmentStatusStats.Select(x => x.Status).ToList();
            ViewBag.ShipmentStatusCounts = shipmentStatusStats.Select(x => x.Count).ToList();

            return View();
        }

        public IActionResult Privacy() => View();

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
