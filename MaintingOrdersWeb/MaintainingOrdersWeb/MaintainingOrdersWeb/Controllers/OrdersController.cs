using MaintainingOrdersWeb.Models;
using MaintainingOrdersWeb.Services;
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
        private readonly DeleteDependencyService _deleteDependencyService;

        public OrdersController(MyDbContext context, DeleteDependencyService deleteDependencyService)
        {
            _context = context;
            _deleteDependencyService = deleteDependencyService;
        }

        // GET: Orders
        public async Task<IActionResult> Index(
            int? orderId,
            int? clientId,
            int? statusId,
            int? methodId,
            DateOnly? orderDate,
            decimal? minTotal,
            string? search,
            string? sortOrder)
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

            if (statusId.HasValue)
            {
                ordersQuery = ordersQuery.Where(o => o.StatusId == statusId.Value);
            }

            if (methodId.HasValue)
            {
                ordersQuery = ordersQuery.Where(o => o.MethodId == methodId.Value);
            }

            if (orderDate.HasValue)
            {
                ordersQuery = ordersQuery.Where(o => o.OrderDate == orderDate.Value);
            }

            if (minTotal.HasValue)
            {
                ordersQuery = ordersQuery.Where(o => o.TotalPrice >= minTotal.Value);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var normalizedSearch = search.Trim();
                ordersQuery = ordersQuery.Where(o =>
                    o.Client.Name.Contains(normalizedSearch) ||
                    (o.DeliveryAddress != null && o.DeliveryAddress.Contains(normalizedSearch)) ||
                    o.Status.StatusName.Contains(normalizedSearch));
            }

            ordersQuery = sortOrder switch
            {
                "price_desc" => ordersQuery.OrderByDescending(o => o.TotalPrice).ThenByDescending(o => o.OrderDate),
                "price_asc" => ordersQuery.OrderBy(o => o.TotalPrice).ThenByDescending(o => o.OrderDate),
                "date_asc" => ordersQuery.OrderBy(o => o.OrderDate).ThenBy(o => o.OrderId),
                _ => ordersQuery.OrderByDescending(o => o.OrderDate).ThenByDescending(o => o.OrderId)
            };

            ViewBag.FilterOrderId = orderId;
            ViewBag.FilterClientId = clientId;
            ViewBag.FilterStatusId = statusId;
            ViewBag.FilterMethodId = methodId;
            ViewBag.FilterOrderDate = orderDate;
            ViewBag.FilterMinTotal = minTotal;
            ViewBag.FilterSearch = search;
            ViewBag.FilterSortOrder = sortOrder;
            ViewBag.Clients = await _context.Clients
                .OrderBy(c => c.Name)
                .Select(c => new { c.ClientId, c.Name })
                .ToListAsync();
            ViewBag.Statuses = await _context.OrderStatuses
                .OrderBy(s => s.StatusName)
                .Select(s => new { s.StatusId, s.StatusName })
                .ToListAsync();
            ViewBag.Methods = await _context.DeliveryMethods
                .OrderBy(m => m.MethodName)
                .Select(m => new { m.MethodId, m.MethodName })
                .ToListAsync();

            ViewBag.FilteredOrdersCount = await ordersQuery.CountAsync();
            ViewBag.FilteredTotalRevenue = await ordersQuery.SumAsync(o => (decimal?)o.TotalPrice) ?? 0m;
            ViewBag.AverageOrderTotal = await ordersQuery.AverageAsync(o => (decimal?)o.TotalPrice) ?? 0m;

            return View(await ordersQuery.ToListAsync());
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
            ModelState.Remove("Status");
            ModelState.Remove("Client");
            ModelState.Remove("User");
            ModelState.Remove("Method");

            if (ModelState.IsValid)
            {
                try
                {
                    var selectedStatus = await _context.OrderStatuses
                        .AsNoTracking()
                        .FirstOrDefaultAsync(s => s.StatusId == order.StatusId);

                    _context.Add(order);
                    await _context.SaveChangesAsync();

                    if (selectedStatus != null && IsCompletedStatus(selectedStatus.StatusName))
                    {
                        await ApplyOrderToStockAsync(order.OrderId);
                    }

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
                    var existingOrder = await _context.Orders
                        .AsNoTracking()
                        .FirstOrDefaultAsync(o => o.OrderId == id);
                    if (existingOrder == null)
                    {
                        return NotFound();
                    }

                    var previousStatus = await _context.OrderStatuses
                        .AsNoTracking()
                        .FirstOrDefaultAsync(s => s.StatusId == existingOrder.StatusId);
                    var selectedStatus = await _context.OrderStatuses
                        .AsNoTracking()
                        .FirstOrDefaultAsync(s => s.StatusId == order.StatusId);

                    var wasCompleted = IsCompletedStatus(previousStatus?.StatusName);
                    var isCompleted = IsCompletedStatus(selectedStatus?.StatusName);

                    _context.Update(order);
                    await _context.SaveChangesAsync();

                    if (!wasCompleted && isCompleted)
                    {
                        await ApplyOrderToStockAsync(order.OrderId);
                    }
                    else if (wasCompleted && !isCompleted)
                    {
                        await RevertOrderFromStockAsync(order.OrderId);
                    }

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

            await ApplyDeleteInfoAsync("Order", id.Value);

            return View(order);
        }

        // POST: Orders/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Директор,Менеджер")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var deleteInfo = await _deleteDependencyService.CheckAsync("Order", id);
            if (!deleteInfo.CanDelete)
            {
                TempData["DeleteDependencyWarning"] = deleteInfo.WarningMessage;
                return RedirectToAction(nameof(Delete), new { id });
            }
            var order = await _context.Orders.FindAsync(id);
            if (order != null)
                _context.Orders.Remove(order);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Директор")]
        public async Task<IActionResult> ConfirmPacked(int id, bool isPacked)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order == null)
            {
                return NotFound();
            }

            var targetStatus = await ResolvePackedConfirmationStatusAsync(isPacked, order.StatusId);
            if (targetStatus != null && targetStatus.StatusId != order.StatusId)
            {
                var previousStatus = await _context.OrderStatuses
                    .AsNoTracking()
                    .FirstOrDefaultAsync(s => s.StatusId == order.StatusId);

                var wasCompleted = IsCompletedStatus(previousStatus?.StatusName);
                var isCompleted = IsCompletedStatus(targetStatus.StatusName);

                order.StatusId = targetStatus.StatusId;
                _context.Update(order);
                await _context.SaveChangesAsync();

                if (!wasCompleted && isCompleted)
                {
                    await ApplyOrderToStockAsync(order.OrderId);
                }
                else if (wasCompleted && !isCompleted)
                {
                    await RevertOrderFromStockAsync(order.OrderId);
                }
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Логист,Директор")]
        public async Task<IActionResult> UpdateLogistics(int id, int statusId, int methodId)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order == null)
            {
                return NotFound();
            }

            var selectedStatus = await _context.OrderStatuses
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.StatusId == statusId);
            var selectedMethod = await _context.DeliveryMethods
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.MethodId == methodId);
            if (selectedStatus == null || selectedMethod == null)
            {
                return RedirectToAction(nameof(Index));
            }

            var previousStatus = await _context.OrderStatuses
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.StatusId == order.StatusId);

            var wasCompleted = IsCompletedStatus(previousStatus?.StatusName);
            var isCompleted = IsCompletedStatus(selectedStatus.StatusName);

            order.StatusId = statusId;
            order.MethodId = methodId;
            _context.Update(order);
            await _context.SaveChangesAsync();

            if (!wasCompleted && isCompleted)
            {
                await ApplyOrderToStockAsync(order.OrderId);
            }
            else if (wasCompleted && !isCompleted)
            {
                await RevertOrderFromStockAsync(order.OrderId);
            }

            await RecalculateOrderTotalAsync(order.OrderId, preserveManualWhenNoItems: true);
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


        private async Task ApplyDeleteInfoAsync(string entityName, int id)
        {
            var deleteInfo = await _deleteDependencyService.CheckAsync(entityName, id);
            ViewBag.CanDelete = deleteInfo.CanDelete;
            ViewBag.DependencyWarning = TempData["DeleteDependencyWarning"] as string ?? deleteInfo.WarningMessage;
        }

        private bool OrderExists(int id)
        {
            return _context.Orders.Any(e => e.OrderId == id);
        }

        private static bool IsCompletedStatus(string? statusName)
        {
            if (string.IsNullOrWhiteSpace(statusName))
            {
                return false;
            }

            var normalized = statusName.Trim().ToLowerInvariant();
            return normalized.Contains("выполн")
                || normalized.Contains("достав")
                || normalized.Contains("заверш")
                || normalized.Contains("выдан")
                || normalized.Contains("complet")
                || normalized.Contains("deliver")
                || normalized.Contains("done");
        }

        private async Task ApplyOrderToStockAsync(int orderId)
        {
            var orderItems = await _context.OrderItems
                .Where(oi => oi.OrderId == orderId)
                .ToListAsync();

            foreach (var item in orderItems)
            {
                var product = await _context.Products.FirstOrDefaultAsync(p => p.ProductId == item.ProductId);
                if (product == null)
                {
                    continue;
                }

                product.Remains = Math.Max(0, (product.Remains ?? 0) - item.Quantity);
            }

            await _context.SaveChangesAsync();
        }

        private async Task RevertOrderFromStockAsync(int orderId)
        {
            var orderItems = await _context.OrderItems
                .Where(oi => oi.OrderId == orderId)
                .ToListAsync();

            foreach (var item in orderItems)
            {
                var product = await _context.Products.FirstOrDefaultAsync(p => p.ProductId == item.ProductId);
                if (product == null)
                {
                    continue;
                }

                product.Remains = (product.Remains ?? 0) + item.Quantity;
            }

            await _context.SaveChangesAsync();
        }

        private async Task<OrderStatus?> ResolvePackedConfirmationStatusAsync(bool isPacked, int currentStatusId)
        {
            var statuses = await _context.OrderStatuses
                .AsNoTracking()
                .ToListAsync();

            if (isPacked)
            {
                return statuses.FirstOrDefault(s =>
                    !string.IsNullOrWhiteSpace(s.StatusName)
                    && s.StatusName.Trim().ToLowerInvariant().Contains("собран"));
            }

            return statuses.FirstOrDefault(s =>
                       !string.IsNullOrWhiteSpace(s.StatusName)
                       && s.StatusName.Trim().ToLowerInvariant().Contains("не собран"))
                   ?? statuses.FirstOrDefault(s =>
                       !string.IsNullOrWhiteSpace(s.StatusName)
                       && s.StatusName.Trim().ToLowerInvariant().Contains("нов"))
                   ?? statuses.FirstOrDefault(s =>
                       !string.IsNullOrWhiteSpace(s.StatusName)
                       && s.StatusName.Trim().ToLowerInvariant().Contains("обработ"))
                   ?? statuses.FirstOrDefault(s => s.StatusId != currentStatusId);
        }
    }
}
