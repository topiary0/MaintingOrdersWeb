using MaintainingOrdersWeb.Models;
using MaintainingOrdersWeb.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MaintainingOrdersWeb.Controllers
{
    [Authorize(Roles = "Директор,Менеджер,Логист")]
    public class ShipmentStatusController : Controller
    {
        private readonly MyDbContext _context;
        private readonly DeleteDependencyService _deleteDependencyService;

        public ShipmentStatusController(MyDbContext context, DeleteDependencyService deleteDependencyService)
        {
            _context = context;
            _deleteDependencyService = deleteDependencyService;
        }

        // GET: ShipmentStatus
        public async Task<IActionResult> Index()
        {
            return View(await _context.ShipmentStatuses.ToListAsync());
        }

        // GET: ShipmentStatus/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var shipmentStatus = await _context.ShipmentStatuses
                .FirstOrDefaultAsync(m => m.StatusId == id);
            if (shipmentStatus == null) return NotFound();

            return View(shipmentStatus);
        }

        // GET: ShipmentStatus/Create
        [Authorize(Roles = "Директор,Менеджер")]
        public IActionResult Create()
        {
            return View();
        }

        // POST: ShipmentStatus/Create
        [Authorize(Roles = "Директор,Менеджер")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("StatusId,StatusName")] ShipmentStatus shipmentStatus)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    _context.Add(shipmentStatus);
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
            return View(shipmentStatus);
        }

        // GET: ShipmentStatus/Edit/5
        [Authorize(Roles = "Директор,Менеджер")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var shipmentStatus = await _context.ShipmentStatuses.FindAsync(id);
            if (shipmentStatus == null) return NotFound();

            return View(shipmentStatus);
        }

        // POST: ShipmentStatus/Edit/5
        [Authorize(Roles = "Директор,Менеджер")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("StatusId,StatusName")] ShipmentStatus shipmentStatus)
        {
            if (id != shipmentStatus.StatusId) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(shipmentStatus);
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ShipmentStatusExists(shipmentStatus.StatusId))
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
            return View(shipmentStatus);
        }

        // GET: ShipmentStatus/Delete/5
        [Authorize(Roles = "Директор,Менеджер")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var shipmentStatus = await _context.ShipmentStatuses
                .FirstOrDefaultAsync(m => m.StatusId == id);
            if (shipmentStatus == null) return NotFound();

            await ApplyDeleteInfoAsync("ShipmentStatus", id.Value);

            return View(shipmentStatus);
        }

        // POST: ShipmentStatus/Delete/5
        [Authorize(Roles = "Директор,Менеджер")]
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var deleteInfo = await _deleteDependencyService.CheckAsync("ShipmentStatus", id);
            if (!deleteInfo.CanDelete)
            {
                TempData["DeleteDependencyWarning"] = deleteInfo.WarningMessage;
                return RedirectToAction(nameof(Delete), new { id });
            }
            var shipmentStatus = await _context.ShipmentStatuses.FindAsync(id);
            if (shipmentStatus != null)
                _context.ShipmentStatuses.Remove(shipmentStatus);

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }


        private async Task ApplyDeleteInfoAsync(string entityName, int id)
        {
            var deleteInfo = await _deleteDependencyService.CheckAsync(entityName, id);
            ViewBag.CanDelete = deleteInfo.CanDelete;
            ViewBag.DependencyWarning = TempData["DeleteDependencyWarning"] as string ?? deleteInfo.WarningMessage;
        }

        private bool ShipmentStatusExists(int id)
        {
            return _context.ShipmentStatuses.Any(e => e.StatusId == id);
        }
    }
}