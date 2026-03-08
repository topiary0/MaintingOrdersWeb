using MaintainingOrdersWeb.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

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
                .FirstOrDefaultAsync(u => u.Login == User.Identity!.Name);
            ViewBag.UserName = user?.FullName ?? User.Identity?.Name;

            ViewBag.TotalProducts = await _context.Products.CountAsync();
            ViewBag.TotalClients = await _context.Clients.CountAsync();
            ViewBag.TotalOrders = await _context.Orders.CountAsync();
            ViewBag.TotalRevenue = await _context.Orders.Select(o => o.TotalPrice).DefaultIfEmpty(0m).SumAsync();

            ViewBag.RecentOrders = await _context.Orders
                .Include(o => o.Client)
                .Include(o => o.Status)
                .OrderByDescending(o => o.OrderDate)
                .Take(5)
                .ToListAsync();

            ViewBag.LowStockProducts = await _context.Products
                .OrderBy(p => p.Remains)
                .Take(5)
                .ToListAsync();

            ViewBag.TodaysOrders = await _context.Orders
                .Include(o => o.Client)
                .Include(o => o.Status)
                .Where(o => o.OrderDate == DateOnly.FromDateTime(DateTime.Today))
                .ToListAsync();

            ViewBag.ActiveShipments = await _context.Shipments
                .Include(s => s.Suppliers)
                .Include(s => s.Statussh)
                .OrderByDescending(s => s.ShipmentDate)
                .Take(5)
                .ToListAsync();

            var startWeek = DateOnly.FromDateTime(DateTime.Today.AddDays(-6));
            ViewBag.WeeklyTopDay = await _context.Orders
                .Where(o => o.OrderDate >= startWeek)
                .GroupBy(o => o.OrderDate)
                .Select(g => new { Date = g.Key, Revenue = g.Sum(x => x.TotalPrice) })
                .OrderByDescending(x => x.Revenue)
                .FirstOrDefaultAsync();

            var quarterStartMonth = ((DateTime.Today.Month - 1) / 3) * 3 + 1;
            var quarterStart = new DateOnly(DateTime.Today.Year, quarterStartMonth, 1);
            ViewBag.QuarterDeals = await _context.Orders.CountAsync(o => o.OrderDate >= quarterStart);

            ViewBag.TopManagers = await _context.Orders
                .Include(o => o.User)
                .GroupBy(o => o.User.FullName)
                .Select(g => new { Manager = g.Key, Revenue = g.Sum(x => x.TotalPrice), Deals = g.Count() })
                .OrderByDescending(x => x.Revenue)
                .Take(5)
                .ToListAsync();

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
