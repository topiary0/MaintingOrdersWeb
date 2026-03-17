using MaintainingOrdersWeb.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace MaintainingOrdersWeb.Controllers
{
    [Authorize(Roles = "Директор,Менеджер,Логист,Сотрудник,Бухгалтер")]
    public class OrdersController : Controller
    {
        private readonly MyDbContext _context;

        public OrdersController(MyDbContext context)
        {
            _context = context;
        }

        // GET: Orders
        public async Task<IActionResult> Index(int? orderId, int? clientId, DateOnly? orderDate)
        {
            var ordersQuery = _context.Orders
                .Include(o => o.Status)
                .Include(o => o.Client)
                .Include(o => o.User)
                .Include(o => o.Method)
                .AsQueryable();

            if (orderId.HasValue)
            {
                ordersQuery = ordersQuery.Where(o => o.OrderId == orderId.Value);
            }

            if (clientId.HasValue)
            {
                ordersQuery = ordersQuery.Where(o => o.ClientId == clientId.Value);
            }

            if (orderDate.HasValue)
            {
                ordersQuery = ordersQuery.Where(o => o.OrderDate == orderDate.Value);
            }

            ViewBag.FilterOrderId = orderId;
            ViewBag.FilterClientId = clientId;
            ViewBag.FilterOrderDate = orderDate;
            ViewBag.Clients = await _context.Clients
                .OrderBy(c => c.Name)
                .Select(c => new { c.ClientId, c.Name })
                .ToListAsync();

            return View(await ordersQuery.OrderByDescending(o => o.OrderDate).ToListAsync());
        }

        // GET: Orders/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var order = await _context.Orders
                .Include(o => o.Status)
                .Include(o => o.Client)
                .Include(o => o.User)
                .Include(o => o.Method)
                .FirstOrDefaultAsync(m => m.OrderId == id);
            if (order == null) return NotFound();

            return View(order);
        }

        // GET: Orders/Create
        [Authorize(Roles = "Директор,Менеджер")]
        public IActionResult Create()
        {
            ViewData["StatusId"] = new SelectList(_context.OrderStatuses, "StatusId", "StatusName");
            ViewData["ClientId"] = new SelectList(_context.Clients, "ClientId", "Name");
            ViewData["UserId"] = new SelectList(_context.Users, "UserId", "FullName");
            ViewData["MethodId"] = new SelectList(_context.DeliveryMethods, "MethodId", "MethodName");
            return View();
        }

        // POST: Orders/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Директор,Менеджер")]
        public async Task<IActionResult> Create([Bind("OrderId,OrderDate,StatusId,TotalPrice,DeliveryAddress,ClientId,UserId,MethodId")] Order order)
        {
            // Убираем возможные ошибки навигационных свойств
            ModelState.Remove("Status");
            ModelState.Remove("Client");
            ModelState.Remove("User");
            ModelState.Remove("Method");

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Add(order);
                    await _context.SaveChangesAsync();
                    await RecalculateOrderTotalAsync(order.OrderId, preserveManualWhenNoItems: true);
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Ошибка при сохранении: " + ex.Message);
                    if (ex.InnerException != null)
                        ModelState.AddModelError("", "Внутренняя ошибка: " + ex.InnerException.Message);
                }
            }

            // Если ошибка – перезаполняем списки и возвращаем форму
            ViewData["StatusId"] = new SelectList(_context.OrderStatuses, "StatusId", "StatusName", order.StatusId);
            ViewData["ClientId"] = new SelectList(_context.Clients, "ClientId", "Name", order.ClientId);
            ViewData["UserId"] = new SelectList(_context.Users, "UserId", "FullName", order.UserId);
            ViewData["MethodId"] = new SelectList(_context.DeliveryMethods, "MethodId", "MethodName", order.MethodId);
            return View(order);
        }

        // GET: Orders/Edit/5
        [Authorize(Roles = "Директор,Менеджер")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var order = await _context.Orders.FindAsync(id);
            if (order == null) return NotFound();

            ViewData["StatusId"] = new SelectList(_context.OrderStatuses, "StatusId", "StatusName", order.StatusId);
            ViewData["ClientId"] = new SelectList(_context.Clients, "ClientId", "Name", order.ClientId);
            ViewData["UserId"] = new SelectList(_context.Users, "UserId", "FullName", order.UserId);
            ViewData["MethodId"] = new SelectList(_context.DeliveryMethods, "MethodId", "MethodName", order.MethodId);
            return View(order);
        }

        // POST: Orders/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Директор,Менеджер")]
        public async Task<IActionResult> Edit(int id, [Bind("OrderId,OrderDate,StatusId,TotalPrice,DeliveryAddress,ClientId,UserId,MethodId")] Order order)
        {
            if (id != order.OrderId) return NotFound();

            ModelState.Remove("Status");
            ModelState.Remove("Client");
            ModelState.Remove("User");
            ModelState.Remove("Method");

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(order);
                    await _context.SaveChangesAsync();
                    await RecalculateOrderTotalAsync(order.OrderId, preserveManualWhenNoItems: true);
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!OrderExists(order.OrderId)) return NotFound();
                    else throw;
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Ошибка при сохранении: " + ex.Message);
                    if (ex.InnerException != null)
                        ModelState.AddModelError("", "Внутренняя ошибка: " + ex.InnerException.Message);
                }
            }

            ViewData["StatusId"] = new SelectList(_context.OrderStatuses, "StatusId", "StatusName", order.StatusId);
            ViewData["ClientId"] = new SelectList(_context.Clients, "ClientId", "Name", order.ClientId);
            ViewData["UserId"] = new SelectList(_context.Users, "UserId", "FullName", order.UserId);
            ViewData["MethodId"] = new SelectList(_context.DeliveryMethods, "MethodId", "MethodName", order.MethodId);
            return View(order);
        }

        // GET: Orders/Delete/5
        [Authorize(Roles = "Директор,Менеджер")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var order = await _context.Orders
                .Include(o => o.Status)
                .Include(o => o.Client)
                .Include(o => o.User)
                .Include(o => o.Method)
                .FirstOrDefaultAsync(m => m.OrderId == id);
            if (order == null) return NotFound();

            return View(order);
        }

        // POST: Orders/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Директор,Менеджер")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order != null)
                _context.Orders.Remove(order);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Сотрудник,Директор")]
        public async Task<IActionResult> MarkAsPacked(int id)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order == null)
            {
                return NotFound();
            }

            var packedStatus = await _context.OrderStatuses
                .FirstOrDefaultAsync(s => s.StatusName.ToLower().Contains("собран"));
            if (packedStatus != null)
            {
                order.StatusId = packedStatus.StatusId;
                _context.Update(order);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        private async Task RecalculateOrderTotalAsync(int orderId, bool preserveManualWhenNoItems = false)
        {
            var order = await _context.Orders
                .Include(o => o.Method)
                .FirstOrDefaultAsync(o => o.OrderId == orderId);

            if (order == null)
                return;

            var hasOrderItems = await _context.OrderItems.AnyAsync(oi => oi.OrderId == orderId);
            if (preserveManualWhenNoItems && !hasOrderItems)
                return;

            var itemsSum = await _context.OrderItems
                .Where(oi => oi.OrderId == orderId)
                .SumAsync(oi => (decimal?)(oi.Quantity * oi.PriceAtOrder)) ?? 0m;

            var deliveryCost = order.Method?.Price ?? 0m;
            order.TotalPrice = itemsSum + deliveryCost;
            await _context.SaveChangesAsync();
        }

        private bool OrderExists(int id)
        {
            return _context.Orders.Any(e => e.OrderId == id);
        }
    }
}
