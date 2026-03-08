using MaintainingOrdersWeb.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.Security.Claims;

namespace MaintainingOrdersWeb.Controllers
{
    public class HomeController : Controller
    {
        private readonly MyDbContext _context;
        private readonly ILogger<HomeController> _logger;

        public HomeController(MyDbContext context, ILogger<HomeController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [Authorize]
        public async Task<IActionResult> Index()
        {
            var user = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Login == User.Identity.Name);
            ViewBag.UserName = user?.FullName ?? User.Identity.Name;

            // Общие показатели
            ViewBag.TotalProducts = await _context.Products.CountAsync();
            ViewBag.TotalClients = await _context.Clients.CountAsync();

            // Директор
            ViewBag.TotalOrders = await _context.Orders.CountAsync();
            ViewBag.TotalRevenue = await _context.Orders.SumAsync(o => o.TotalPrice);

            // Последние 5 заказов
            ViewBag.RecentOrders = await _context.Orders
                .Include(o => o.Client)
                .Include(o => o.Status)
                .OrderByDescending(o => o.OrderDate)
                .Take(5)
                .ToListAsync();

            // Активные поставки (Ожидание или В пути)
            ViewBag.ActiveShipments = await _context.Shipments
                .Include(s => s.Suppliers)
                .Include(s => s.Statussh)
                .Where(s => s.Statussh.StatusName == "Ожидание" || s.Statussh.StatusName == "В пути")
                .ToListAsync();

            // Заказы на сегодня
            var today = DateOnly.FromDateTime(DateTime.Today);
            ViewBag.TodaysOrders = await _context.Orders
                .Include(o => o.Client)
                .Include(o => o.Status)
                .Where(o => o.OrderDate == today)
                .ToListAsync();

            // Заказы, ожидающие сборки (например, статус "Новый" или не "Собран")
            ViewBag.PendingOrders = await _context.Orders
                .Include(o => o.Client)
                .Include(o => o.Status)
                .Where(o => o.Status.StatusName != "Собран" && o.Status.StatusName != "Выполнен")
                .OrderBy(o => o.OrderDate)
                .ToListAsync();

            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}