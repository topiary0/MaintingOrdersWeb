using MaintainingOrdersWeb.Infrastructure;
using MaintainingOrdersWeb.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MaintainingOrdersWeb.Controllers
{
    [Authorize(Roles = AppRoles.ManagementRoles)]
    public class ShipmentStatusController : Controller
    {
        private readonly MyDbContext _context;

        public ShipmentStatusController(MyDbContext context)
        {
            _context = context;
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
        public IActionResult Create()
        {
            return View();
        }

        // POST: ShipmentStatus/Create
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
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var shipmentStatus = await _context.ShipmentStatuses.FindAsync(id);
            if (shipmentStatus == null) return NotFound();

            return View(shipmentStatus);
        }

        // POST: ShipmentStatus/Edit/5
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
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var shipmentStatus = await _context.ShipmentStatuses
                .FirstOrDefaultAsync(m => m.StatusId == id);
            if (shipmentStatus == null) return NotFound();

            return View(shipmentStatus);
        }

        // POST: ShipmentStatus/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var shipmentStatus = await _context.ShipmentStatuses.FindAsync(id);
            if (shipmentStatus != null)
                _context.ShipmentStatuses.Remove(shipmentStatus);

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool ShipmentStatusExists(int id)
        {
            return _context.ShipmentStatuses.Any(e => e.StatusId == id);
        }
    }
}