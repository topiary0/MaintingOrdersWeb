using MaintainingOrdersWeb.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MaintainingOrdersWeb.Controllers
{
    [Authorize(Roles = "Директор,Менеджер,Логист")]
    public class DeliveryMethodsController : Controller
    {
        private readonly MyDbContext _context;

        public DeliveryMethodsController(MyDbContext context)
        {
            _context = context;
        }

        // GET: DeliveryMethods
        public async Task<IActionResult> Index()
        {
            return View(await _context.DeliveryMethods.ToListAsync());
        }

        // GET: DeliveryMethods/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var deliveryMethod = await _context.DeliveryMethods
                .FirstOrDefaultAsync(m => m.MethodId == id);
            if (deliveryMethod == null) return NotFound();

            return View(deliveryMethod);
        }

        // GET: DeliveryMethods/Create
        [Authorize(Roles = "Директор,Менеджер")]
        public IActionResult Create()
        {
            return View();
        }

        // POST: DeliveryMethods/Create
        [Authorize(Roles = "Директор,Менеджер")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("MethodId,MethodName,Price,EstimatedTime")] DeliveryMethod deliveryMethod)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    _context.Add(deliveryMethod);
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
            return View(deliveryMethod);
        }

        // GET: DeliveryMethods/Edit/5
        [Authorize(Roles = "Директор,Менеджер")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var deliveryMethod = await _context.DeliveryMethods.FindAsync(id);
            if (deliveryMethod == null) return NotFound();

            return View(deliveryMethod);
        }

        // POST: DeliveryMethods/Edit/5
        [Authorize(Roles = "Директор,Менеджер")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("MethodId,MethodName,Price,EstimatedTime")] DeliveryMethod deliveryMethod)
        {
            if (id != deliveryMethod.MethodId) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(deliveryMethod);
                    await _context.SaveChangesAsync();
                    await RecalculateOrdersByMethodAsync(deliveryMethod.MethodId, deliveryMethod.Price);
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!DeliveryMethodExists(deliveryMethod.MethodId))
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
            return View(deliveryMethod);
        }

        // GET: DeliveryMethods/Delete/5
        [Authorize(Roles = "Директор,Менеджер")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var deliveryMethod = await _context.DeliveryMethods
                .FirstOrDefaultAsync(m => m.MethodId == id);
            if (deliveryMethod == null) return NotFound();

            return View(deliveryMethod);
        }

        // POST: DeliveryMethods/Delete/5
        [Authorize(Roles = "Директор,Менеджер")]
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var isUsed = await _context.Orders.AnyAsync(o => o.MethodId == id);
            if (isUsed)
            {
                TempData["DeliveryMethodError"] = "Способ доставки используется в заказах и не может быть удален.";
                return RedirectToAction(nameof(Index));
            }

            var deliveryMethod = await _context.DeliveryMethods.FindAsync(id);
            if (deliveryMethod != null)
                _context.DeliveryMethods.Remove(deliveryMethod);

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool DeliveryMethodExists(int id)
        {
            return _context.DeliveryMethods.Any(e => e.MethodId == id);
        }

        private async Task RecalculateOrdersByMethodAsync(int methodId, decimal deliveryPrice)
        {
            var relatedOrders = await _context.Orders
                .Where(o => o.MethodId == methodId)
                .ToListAsync();

            foreach (var order in relatedOrders)
            {
                var itemsTotal = await _context.OrderItems
                    .Where(oi => oi.OrderId == order.OrderId)
                    .SumAsync(oi => (decimal?)(oi.Quantity * oi.PriceAtOrder)) ?? 0m;

                order.TotalPrice = itemsTotal + deliveryPrice;
            }

            await _context.SaveChangesAsync();
        }
    }
}
