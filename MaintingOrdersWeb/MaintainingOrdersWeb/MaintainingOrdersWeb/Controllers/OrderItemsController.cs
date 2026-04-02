using MaintainingOrdersWeb.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace MaintainingOrdersWeb.Controllers
{
    [Authorize(Roles = "Директор,Менеджер,Логист,Сотрудник")]
    public class OrderItemsController : Controller
    {
        private readonly MyDbContext _context;

        public OrderItemsController(MyDbContext context)
        {
            _context = context;
        }

        // GET: OrderItems
        public async Task<IActionResult> Index(int? orderId)
        {
            var orderItems = _context.OrderItems
                .Include(oi => oi.Order)
                .Include(oi => oi.Product)
                .AsQueryable();

            if (orderId.HasValue)
            {
                orderItems = orderItems.Where(oi => oi.OrderId == orderId.Value);
            }

            ViewBag.FilterOrderId = orderId;
            return View(await orderItems
                .OrderBy(oi => oi.OrderId)
                .ThenBy(oi => oi.Product.Name)
                .ToListAsync());
        }

        // GET: OrderItems/Details?orderId=5&productId=3
        public async Task<IActionResult> Details(int orderId, int productId)
        {
            var orderItem = await _context.OrderItems
                .Include(oi => oi.Order)
                .Include(oi => oi.Product)
                .FirstOrDefaultAsync(oi => oi.OrderId == orderId && oi.ProductId == productId);
            if (orderItem == null) return NotFound();

            return View(orderItem);
        }

        // GET: OrderItems/Create
        [Authorize(Roles = "Директор,Менеджер,Сотрудник")]
        public IActionResult Create()
        {
            ViewData["OrderId"] = new SelectList(_context.Orders, "OrderId", "OrderId"); 
            ViewData["ProductId"] = new SelectList(_context.Products, "ProductId", "Name");
            return View();
        }

        // POST: OrderItems/Create
        [Authorize(Roles = "Директор,Менеджер,Сотрудник")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("OrderId,ProductId,Quantity,PriceAtOrder")] OrderItem orderItem)
        {
            ModelState.Remove("Order");
            ModelState.Remove("Product");

            if (await _context.OrderItems.AnyAsync(oi => oi.OrderId == orderItem.OrderId && oi.ProductId == orderItem.ProductId))
            {
                ModelState.AddModelError("", "Такая позиция уже существует в этом заказе.");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    if (orderItem.PriceAtOrder <= 0)
                    {
                        var product = await _context.Products.FindAsync(orderItem.ProductId);
                        if (product != null)
                            orderItem.PriceAtOrder = product.Price;
                    }

                    _context.Add(orderItem);
                    await _context.SaveChangesAsync();
                    await RecalculateOrderTotalAsync(orderItem.OrderId);
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Ошибка при сохранении: " + ex.Message);
                    if (ex.InnerException != null)
                        ModelState.AddModelError("", ex.InnerException.Message);
                }
            }

            ViewData["OrderId"] = new SelectList(_context.Orders, "OrderId", "OrderId", orderItem.OrderId);
            ViewData["ProductId"] = new SelectList(_context.Products, "ProductId", "Name", orderItem.ProductId);
            return View(orderItem);
        }

        // GET: OrderItems/Edit?orderId=5&productId=3
        [Authorize(Roles = "Директор,Менеджер,Сотрудник")]
        public async Task<IActionResult> Edit(int orderId, int productId)
        {
            var orderItem = await _context.OrderItems
                .FirstOrDefaultAsync(oi => oi.OrderId == orderId && oi.ProductId == productId);
            if (orderItem == null) return NotFound();

            ViewData["OrderId"] = new SelectList(_context.Orders, "OrderId", "OrderId", orderItem.OrderId);
            ViewData["ProductId"] = new SelectList(_context.Products, "ProductId", "Name", orderItem.ProductId);
            return View(orderItem);
        }

        // POST: OrderItems/Edit?orderId=5&productId=3
        [Authorize(Roles = "Директор,Менеджер,Сотрудник")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int orderId, int productId, [Bind("OrderId,ProductId,Quantity,PriceAtOrder")] OrderItem orderItem)
        {
            if (orderId != orderItem.OrderId || productId != orderItem.ProductId)
                return NotFound();

            ModelState.Remove("Order");
            ModelState.Remove("Product");

            if (await _context.OrderItems.AnyAsync(oi => oi.OrderId == orderItem.OrderId && oi.ProductId == orderItem.ProductId && !(oi.OrderId == orderId && oi.ProductId == productId)))
            {
                ModelState.AddModelError("", "Такая позиция уже существует.");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    if (orderItem.PriceAtOrder <= 0)
                    {
                        var product = await _context.Products.FindAsync(orderItem.ProductId);
                        if (product != null)
                            orderItem.PriceAtOrder = product.Price;
                    }

                    _context.Update(orderItem);
                    await _context.SaveChangesAsync();
                    await RecalculateOrderTotalAsync(orderItem.OrderId);
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!OrderItemExists(orderItem.OrderId, orderItem.ProductId))
                        return NotFound();
                    else
                        throw;
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Ошибка при сохранении: " + ex.Message);
                    if (ex.InnerException != null)
                        ModelState.AddModelError("", ex.InnerException.Message);
                }
            }

            ViewData["OrderId"] = new SelectList(_context.Orders, "OrderId", "OrderId", orderItem.OrderId);
            ViewData["ProductId"] = new SelectList(_context.Products, "ProductId", "Name", orderItem.ProductId);
            return View(orderItem);
        }

        // GET: OrderItems/Delete?orderId=5&productId=3
        [Authorize(Roles = "Директор,Менеджер,Сотрудник")]
        public async Task<IActionResult> Delete(int orderId, int productId)
        {
            var orderItem = await _context.OrderItems
                .Include(oi => oi.Order)
                .Include(oi => oi.Product)
                .FirstOrDefaultAsync(oi => oi.OrderId == orderId && oi.ProductId == productId);
            if (orderItem == null) return NotFound();

            return View(orderItem);
        }

        // POST: OrderItems/Delete?orderId=5&productId=3
        [Authorize(Roles = "Директор,Менеджер,Сотрудник")]
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int orderId, int productId)
        {
            var orderItem = await _context.OrderItems
                .FirstOrDefaultAsync(oi => oi.OrderId == orderId && oi.ProductId == productId);
            if (orderItem != null)
                _context.OrderItems.Remove(orderItem);

            await _context.SaveChangesAsync();
            await RecalculateOrderTotalAsync(orderId);
            return RedirectToAction(nameof(Index));
        }

        private async Task RecalculateOrderTotalAsync(int orderId)
        {
            var order = await _context.Orders
                .Include(o => o.Method)
                .FirstOrDefaultAsync(o => o.OrderId == orderId);

            if (order == null)
                return;

            var itemsSum = await _context.OrderItems
                .Where(oi => oi.OrderId == orderId)
                .SumAsync(oi => (decimal?)(oi.Quantity * oi.PriceAtOrder)) ?? 0m;

            var deliveryCost = order.Method?.Price ?? 0m;
            order.TotalPrice = itemsSum + deliveryCost;

            _context.Orders.Update(order);
            await _context.SaveChangesAsync();
        }

        private bool OrderItemExists(int orderId, int productId)
        {
            return _context.OrderItems.Any(oi => oi.OrderId == orderId && oi.ProductId == productId);
        }
    }
}
