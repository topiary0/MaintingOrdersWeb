using MaintainingOrdersWeb.Documents;
using MaintainingOrdersWeb.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;

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
                .SumAsync(o => (decimal?)o.TotalPrice) ?? 0m;

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


        [HttpGet]
        public async Task<IActionResult> ExportSalesPdf(DateOnly? dateFrom, DateOnly? dateTo)
        {
            var from = dateFrom ?? new DateOnly(DateTime.Today.Year, DateTime.Today.Month, 1);
            var to = dateTo ?? DateOnly.FromDateTime(DateTime.Today);

            var sales = await _context.OrderItems
                .Include(oi => oi.Order)
                .Include(oi => oi.Product)
                .Where(oi => oi.Order.OrderDate >= from && oi.Order.OrderDate <= to)
                .GroupBy(oi => oi.Product.Name)
                .Select(g => new SalesReportDocument.SalesRow
                {
                    Product = g.Key,
                    Quantity = g.Sum(x => x.Quantity),
                    Amount = g.Sum(x => x.PriceAtOrder * x.Quantity)
                })
                .OrderByDescending(x => x.Amount)
                .ToListAsync();

            var document = new SalesReportDocument(from, to, sales);
            var pdfBytes = document.GeneratePdf();
            return File(pdfBytes, "application/pdf", $"SalesReport_{from:yyyyMMdd}_{to:yyyyMMdd}.pdf");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Бухгалтер")]
        public async Task<IActionResult> ExportAccountingPdf()
        {
            var quarterStartMonth = ((DateTime.Today.Month - 1) / 3) * 3 + 1;
            var quarterStart = new DateOnly(DateTime.Today.Year, quarterStartMonth, 1);

            var quarterOrders = await _context.Orders.CountAsync(o => o.OrderDate >= quarterStart);
            var quarterRevenue = await _context.Orders
                .Where(o => o.OrderDate >= quarterStart)
                .SumAsync(o => (decimal?)o.TotalPrice) ?? 0m;
            var clientsCount = await _context.Clients.CountAsync();

            var document = new AccountingReportDocument(quarterOrders, quarterRevenue, clientsCount, DateTime.Now);
            var pdfBytes = document.GeneratePdf();

            return File(pdfBytes, "application/pdf", $"AccountingReport_{DateTime.Now:yyyyMMdd_HHmm}.pdf");
        }
    }
}
