using MaintainingOrdersWeb.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MaintainingOrdersWeb.Controllers
{
    [Authorize(Roles = "Директор,Бухгалтер")]
    public class ReportsController : Controller
    {
        private readonly MyDbContext _context;

        public ReportsController(MyDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var quarterStartMonth = ((DateTime.Today.Month - 1) / 3) * 3 + 1;
            var quarterStart = new DateOnly(DateTime.Today.Year, quarterStartMonth, 1);

            ViewBag.QuarterOrders = await _context.Orders.CountAsync(o => o.OrderDate >= quarterStart);
            ViewBag.QuarterRevenue = await _context.Orders
                .Where(o => o.OrderDate >= quarterStart)
                .Select(o => o.TotalPrice)
                .DefaultIfEmpty(0m)
                .SumAsync();

            ViewBag.ManagerStats = await _context.Orders
                .Include(o => o.User)
                .GroupBy(o => o.User.FullName)
                .Select(g => new { Manager = g.Key, Deals = g.Count(), Revenue = g.Sum(x => x.TotalPrice) })
                .OrderByDescending(x => x.Revenue)
                .ToListAsync();

            ViewBag.TopProducts = await _context.OrderItems
                .Include(oi => oi.Product)
                .GroupBy(oi => oi.Product.Name)
                .Select(g => new { Product = g.Key, Qty = g.Sum(x => x.Quantity), Sales = g.Sum(x => x.PriceAtOrder * x.Quantity) })
                .OrderByDescending(x => x.Sales)
                .Take(10)
                .ToListAsync();

            return View();
        }
    }
}
