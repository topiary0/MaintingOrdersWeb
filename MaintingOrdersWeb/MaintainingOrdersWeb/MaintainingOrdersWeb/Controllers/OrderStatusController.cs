using MaintainingOrdersWeb.Models;
using MaintainingOrdersWeb.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MaintainingOrdersWeb.Controllers
{
    [Authorize(Roles = "Директор,Менеджер,Логист")]
    public class OrderStatusController : Controller
    {
        private readonly MyDbContext _context;
        private readonly DeleteDependencyService _deleteDependencyService;

        public OrderStatusController(MyDbContext context, DeleteDependencyService deleteDependencyService)
        {
            _context = context;
            _deleteDependencyService = deleteDependencyService;
        }

        // GET: OrderStatus
        public async Task<IActionResult> Index()
        {
            return View(await _context.OrderStatuses.ToListAsync());
        }

        // GET: OrderStatus/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var orderStatus = await _context.OrderStatuses
                .FirstOrDefaultAsync(m => m.StatusId == id);
            if (orderStatus == null) return NotFound();

            return View(orderStatus);
        }

        // GET: OrderStatus/Create
        [Authorize(Roles = "Директор,Менеджер")]
        public IActionResult Create()
        {
            return View();
        }

        // POST: OrderStatus/Create
        [Authorize(Roles = "Директор,Менеджер")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("StatusId,StatusName")] OrderStatus orderStatus)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    _context.Add(orderStatus);
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
            return View(orderStatus);
        }

        // GET: OrderStatus/Edit/5
        [Authorize(Roles = "Директор,Менеджер")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var orderStatus = await _context.OrderStatuses.FindAsync(id);
            if (orderStatus == null) return NotFound();

            return View(orderStatus);
        }

        // POST: OrderStatus/Edit/5
        [Authorize(Roles = "Директор,Менеджер")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("StatusId,StatusName")] OrderStatus orderStatus)
        {
            if (id != orderStatus.StatusId) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(orderStatus);
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!OrderStatusExists(orderStatus.StatusId))
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
            return View(orderStatus);
        }

        // GET: OrderStatus/Delete/5
        [Authorize(Roles = "Директор,Менеджер")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var orderStatus = await _context.OrderStatuses
                .FirstOrDefaultAsync(m => m.StatusId == id);
            if (orderStatus == null) return NotFound();

            await ApplyDeleteInfoAsync("OrderStatus", id.Value);

            return View(orderStatus);
        }

        // POST: OrderStatus/Delete/5
        [Authorize(Roles = "Директор,Менеджер")]
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var deleteInfo = await _deleteDependencyService.CheckAsync("OrderStatus", id);
            if (!deleteInfo.CanDelete)
            {
                TempData["DeleteDependencyWarning"] = deleteInfo.WarningMessage;
                return RedirectToAction(nameof(Delete), new { id });
            }
            var orderStatus = await _context.OrderStatuses.FindAsync(id);
            if (orderStatus != null)
                _context.OrderStatuses.Remove(orderStatus);

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }


        private async Task ApplyDeleteInfoAsync(string entityName, int id)
        {
            var deleteInfo = await _deleteDependencyService.CheckAsync(entityName, id);
            ViewBag.CanDelete = deleteInfo.CanDelete;
            ViewBag.DependencyWarning = TempData["DeleteDependencyWarning"] as string ?? deleteInfo.WarningMessage;
        }

        private bool OrderStatusExists(int id)
        {
            return _context.OrderStatuses.Any(e => e.StatusId == id);
        }
    }
}
