using MaintainingOrdersWeb.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace MaintainingOrdersWeb.Controllers
{
    [Authorize(Roles = "Директор,Менеджер,Логист")]
    public class ShipmentController : Controller
    {
        private readonly MyDbContext _context;

        public ShipmentController(MyDbContext context)
        {
            _context = context;
        }

        // GET: Shipment
        public async Task<IActionResult> Index()
        {
            var shipments = _context.Shipments
                .Include(s => s.Suppliers)
                .Include(s => s.User)
                .Include(s => s.Statussh);
            return View(await shipments.ToListAsync());
        }

        // GET: Shipment/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var shipment = await _context.Shipments
                .Include(s => s.Suppliers)
                .Include(s => s.User)
                .Include(s => s.Statussh)
                .FirstOrDefaultAsync(m => m.ShipmentId == id);
            if (shipment == null) return NotFound();

            return View(shipment);
        }

        // GET: Shipment/Create
        [Authorize(Roles = "Директор,Менеджер")]
        public IActionResult Create()
        {
            ViewData["SuppliersId"] = new SelectList(_context.Suppliers, "SuppliersId", "SupplierName");
            ViewData["UserId"] = new SelectList(_context.Users, "UserId", "FullName");
            ViewData["StatusshId"] = new SelectList(_context.ShipmentStatuses, "StatusId", "StatusName");
            return View();
        }

        // POST: Shipment/Create
        [Authorize(Roles = "Директор,Менеджер")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ShipmentId,ShipmentDate,SuppliersId,UserId,StatusshId")] Shipment shipment)
        {
            ModelState.Remove("Suppliers");
            ModelState.Remove("User");
            ModelState.Remove("Statussh");
            ModelState.Remove("Status");

            if (ModelState.IsValid)
            {
                try
                {
                    var selectedStatus = await _context.ShipmentStatuses
                        .AsNoTracking()
                        .FirstOrDefaultAsync(s => s.StatusId == shipment.StatusshId);
                    shipment.Status = selectedStatus?.StatusName;

                    _context.Add(shipment);
                    await _context.SaveChangesAsync();

                    if (selectedStatus != null && IsAcceptedStatus(selectedStatus.StatusName))
                    {
                        await ApplyShipmentToStockAsync(shipment.ShipmentId);
                    }

                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Ошибка при сохранении: " + ex.Message);
                    if (ex.InnerException != null)
                        ModelState.AddModelError("", ex.InnerException.Message);
                }
            }

            ViewData["SuppliersId"] = new SelectList(_context.Suppliers, "SuppliersId", "SupplierName", shipment.SuppliersId);
            ViewData["UserId"] = new SelectList(_context.Users, "UserId", "FullName", shipment.UserId);
            ViewData["StatusshId"] = new SelectList(_context.ShipmentStatuses, "StatusId", "StatusName", shipment.StatusshId);
            return View(shipment);
        }

        // GET: Shipment/Edit/5
        [Authorize(Roles = "Директор,Менеджер")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var shipment = await _context.Shipments.FindAsync(id);
            if (shipment == null) return NotFound();

            ViewData["SuppliersId"] = new SelectList(_context.Suppliers, "SuppliersId", "SupplierName", shipment.SuppliersId);
            ViewData["UserId"] = new SelectList(_context.Users, "UserId", "FullName", shipment.UserId);
            ViewData["StatusshId"] = new SelectList(_context.ShipmentStatuses, "StatusId", "StatusName", shipment.StatusshId);
            return View(shipment);
        }

        // POST: Shipment/Edit/5
        [Authorize(Roles = "Директор,Менеджер")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("ShipmentId,ShipmentDate,SuppliersId,UserId,StatusshId")] Shipment shipment)
        {
            if (id != shipment.ShipmentId) return NotFound();

            ModelState.Remove("Suppliers");
            ModelState.Remove("User");
            ModelState.Remove("Statussh");
            ModelState.Remove("Status");

            if (ModelState.IsValid)
            {
                try
                {
                    var existingShipment = await _context.Shipments.AsNoTracking()
                        .FirstOrDefaultAsync(s => s.ShipmentId == id);
                    if (existingShipment == null)
                    {
                        return NotFound();
                    }

                    var selectedStatus = await _context.ShipmentStatuses
                        .AsNoTracking()
                        .FirstOrDefaultAsync(s => s.StatusId == shipment.StatusshId);
                    shipment.Status = selectedStatus?.StatusName;

                    var wasAccepted = IsAcceptedStatus(existingShipment.Status);
                    var isAccepted = IsAcceptedStatus(selectedStatus?.StatusName);

                    _context.Update(shipment);
                    await _context.SaveChangesAsync();

                    if (!wasAccepted && isAccepted)
                    {
                        await ApplyShipmentToStockAsync(shipment.ShipmentId);
                    }

                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ShipmentExists(shipment.ShipmentId))
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

            ViewData["SuppliersId"] = new SelectList(_context.Suppliers, "SuppliersId", "SupplierName", shipment.SuppliersId);
            ViewData["UserId"] = new SelectList(_context.Users, "UserId", "FullName", shipment.UserId);
            ViewData["StatusshId"] = new SelectList(_context.ShipmentStatuses, "StatusId", "StatusName", shipment.StatusshId);
            return View(shipment);
        }

        // GET: Shipment/Delete/5
        [Authorize(Roles = "Директор,Менеджер")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var shipment = await _context.Shipments
                .Include(s => s.Suppliers)
                .Include(s => s.User)
                .Include(s => s.Statussh)
                .FirstOrDefaultAsync(m => m.ShipmentId == id);
            if (shipment == null) return NotFound();

            return View(shipment);
        }

        // POST: Shipment/Delete/5
        [Authorize(Roles = "Директор,Менеджер")]
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var shipment = await _context.Shipments.FindAsync(id);
            if (shipment != null)
                _context.Shipments.Remove(shipment);

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool ShipmentExists(int id)
        {
            return _context.Shipments.Any(e => e.ShipmentId == id);
        }

        private bool IsAcceptedStatus(string? statusName)
        {
            if (string.IsNullOrWhiteSpace(statusName))
            {
                return false;
            }

            var normalized = statusName.Trim().ToLowerInvariant();
            return normalized.Contains("прин") || normalized.Contains("выполн") || normalized.Contains("достав");
        }

        private async Task ApplyShipmentToStockAsync(int shipmentId)
        {
            var supplyItems = await _context.SupplyItems
                .Where(i => i.ShipmentId == shipmentId)
                .ToListAsync();

            foreach (var item in supplyItems)
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
    }
}
