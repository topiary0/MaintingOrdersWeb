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
        public async Task<IActionResult> Index()
        {
            var orderItems = _context.OrderItems
                .Include(oi => oi.Order)
                .Include(oi => oi.Product);
            return View(await orderItems.ToListAsync());
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
        [Authorize(Roles = "Директор,Менеджер")]
        public IActionResult Create()
        {
            ViewData["OrderId"] = new SelectList(_context.Orders, "OrderId", "OrderId"); // или можно отображать дату+клиента
            ViewData["ProductId"] = new SelectList(_context.Products, "ProductId", "Name");
            return View();
        }

        // POST: OrderItems/Create
        [Authorize(Roles = "Директор,Менеджер")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("OrderId,ProductId,Quantity,PriceAtOrder")] OrderItem orderItem)
        {
            // Убираем ошибки навигационных свойств
            ModelState.Remove("Order");
            ModelState.Remove("Product");

            // Проверка на существование такой позиции (уникальность составного ключа)
            if (await _context.OrderItems.AnyAsync(oi => oi.OrderId == orderItem.OrderId && oi.ProductId == orderItem.ProductId))
            {
                ModelState.AddModelError("", "Такая позиция уже существует в этом заказе.");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Add(orderItem);
                    await _context.SaveChangesAsync();
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
        [Authorize(Roles = "Директор,Менеджер")]
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
        [Authorize(Roles = "Директор,Менеджер")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int orderId, int productId, [Bind("OrderId,ProductId,Quantity,PriceAtOrder")] OrderItem orderItem)
        {
            if (orderId != orderItem.OrderId || productId != orderItem.ProductId)
                return NotFound();

            ModelState.Remove("Order");
            ModelState.Remove("Product");

            // Проверка уникальности (не должна измениться, но если ключи поменялись – не пропустим)
            if (await _context.OrderItems.AnyAsync(oi => oi.OrderId == orderItem.OrderId && oi.ProductId == orderItem.ProductId && !(oi.OrderId == orderId && oi.ProductId == productId)))
            {
                ModelState.AddModelError("", "Такая позиция уже существует.");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(orderItem);
                    await _context.SaveChangesAsync();
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
        [Authorize(Roles = "Директор")]
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
        [Authorize(Roles = "Директор")]
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int orderId, int productId)
        {
            var orderItem = await _context.OrderItems
                .FirstOrDefaultAsync(oi => oi.OrderId == orderId && oi.ProductId == productId);
            if (orderItem != null)
                _context.OrderItems.Remove(orderItem);

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool OrderItemExists(int orderId, int productId)
        {
            return _context.OrderItems.Any(oi => oi.OrderId == orderId && oi.ProductId == productId);
        }
    }
}

