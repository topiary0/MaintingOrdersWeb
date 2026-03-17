using MaintainingOrdersWeb.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace MaintainingOrdersWeb.Controllers
{
    [Authorize(Roles = "Директор,Менеджер,Логист")]
    public class SupplyItemsController : Controller
    {
        private readonly MyDbContext _context;

        public SupplyItemsController(MyDbContext context)
        {
            _context = context;
        }

        // GET: SupplyItems
        public async Task<IActionResult> Index()
        {
            var supplyItems = _context.SupplyItems
                .Include(s => s.Shipment)
                .Include(s => s.Product);
            return View(await supplyItems.ToListAsync());
        }

        // GET: SupplyItems/Details/5?productId=3
        public async Task<IActionResult> Details(int shipmentId, int productId)
        {
            var supplyItem = await _context.SupplyItems
                .Include(s => s.Shipment)
                .Include(s => s.Product)
                .FirstOrDefaultAsync(m => m.ShipmentId == shipmentId && m.ProductId == productId);
            if (supplyItem == null) return NotFound();

            return View(supplyItem);
        }

        // GET: SupplyItems/Create
        public IActionResult Create()
        {
            ViewData["ShipmentId"] = new SelectList(_context.Shipments, "ShipmentId", "ShipmentId"); // или другое поле для отображения
            ViewData["ProductId"] = new SelectList(_context.Products, "ProductId", "Name");
            return View();
        }

        // POST: SupplyItems/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ShipmentId,ProductId,Quantity,PriceAtShipment")] SupplyItem supplyItem)
        {
            // Убираем ошибки навигационных свойств
            ModelState.Remove("Shipment");
            ModelState.Remove("Product");

            // Проверка уникальности составного ключа
            if (await _context.SupplyItems.AnyAsync(s => s.ShipmentId == supplyItem.ShipmentId && s.ProductId == supplyItem.ProductId))
            {
                ModelState.AddModelError("", "Данный товар уже добавлен в эту поставку.");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Add(supplyItem);
                    await _context.SaveChangesAsync();

                    await ApplyStockChangeIfShipmentAcceptedAsync(supplyItem.ShipmentId, supplyItem.ProductId, supplyItem.Quantity);
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Ошибка при сохранении: " + ex.Message);
                    if (ex.InnerException != null)
                        ModelState.AddModelError("", ex.InnerException.Message);
                }
            }

            ViewData["ShipmentId"] = new SelectList(_context.Shipments, "ShipmentId", "ShipmentId", supplyItem.ShipmentId);
            ViewData["ProductId"] = new SelectList(_context.Products, "ProductId", "Name", supplyItem.ProductId);
            return View(supplyItem);
        }

        // GET: SupplyItems/Edit/5?productId=3
        public async Task<IActionResult> Edit(int shipmentId, int productId)
        {
            var supplyItem = await _context.SupplyItems.FindAsync(shipmentId, productId);
            if (supplyItem == null) return NotFound();

            ViewData["ShipmentId"] = new SelectList(_context.Shipments, "ShipmentId", "ShipmentId", supplyItem.ShipmentId);
            ViewData["ProductId"] = new SelectList(_context.Products, "ProductId", "Name", supplyItem.ProductId);
            return View(supplyItem);
        }

        // POST: SupplyItems/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int shipmentId, int productId, [Bind("ShipmentId,ProductId,Quantity,PriceAtShipment")] SupplyItem supplyItem)
        {
            if (shipmentId != supplyItem.ShipmentId || productId != supplyItem.ProductId)
                return NotFound();

            ModelState.Remove("Shipment");
            ModelState.Remove("Product");

            if (ModelState.IsValid)
            {
                try
                {
                    var existingItem = await _context.SupplyItems
                        .AsNoTracking()
                        .FirstOrDefaultAsync(s => s.ShipmentId == shipmentId && s.ProductId == productId);

                    _context.Update(supplyItem);
                    await _context.SaveChangesAsync();

                    if (existingItem != null)
                    {
                        var delta = supplyItem.Quantity - existingItem.Quantity;
                        await ApplyStockChangeIfShipmentAcceptedAsync(supplyItem.ShipmentId, supplyItem.ProductId, delta);
                    }

                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!SupplyItemExists(supplyItem.ShipmentId, supplyItem.ProductId))
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

            ViewData["ShipmentId"] = new SelectList(_context.Shipments, "ShipmentId", "ShipmentId", supplyItem.ShipmentId);
            ViewData["ProductId"] = new SelectList(_context.Products, "ProductId", "Name", supplyItem.ProductId);
            return View(supplyItem);
        }

        // GET: SupplyItems/Delete/5?productId=3
        public async Task<IActionResult> Delete(int shipmentId, int productId)
        {
            var supplyItem = await _context.SupplyItems
                .Include(s => s.Shipment)
                .Include(s => s.Product)
                .FirstOrDefaultAsync(m => m.ShipmentId == shipmentId && m.ProductId == productId);
            if (supplyItem == null) return NotFound();

            return View(supplyItem);
        }

        // POST: SupplyItems/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int shipmentId, int productId)
        {
            var supplyItem = await _context.SupplyItems.FindAsync(shipmentId, productId);
            if (supplyItem != null)
            {
                var quantityToSubtract = -supplyItem.Quantity;
                _context.SupplyItems.Remove(supplyItem);
                await _context.SaveChangesAsync();
                await ApplyStockChangeIfShipmentAcceptedAsync(shipmentId, productId, quantityToSubtract);
            }
            else
            {
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }


        private async Task ApplyStockChangeIfShipmentAcceptedAsync(int shipmentId, int productId, int quantityDelta)
        {
            var shipment = await _context.Shipments
                .Include(s => s.Statussh)
                .FirstOrDefaultAsync(s => s.ShipmentId == shipmentId);

            if (shipment == null || !IsAcceptedStatus(shipment.Statussh?.StatusName ?? shipment.Status))
            {
                return;
            }

            var product = await _context.Products.FirstOrDefaultAsync(p => p.ProductId == productId);
            if (product == null)
            {
                return;
            }

            if (quantityDelta != 0)
            {
                product.Remains = Math.Max(0, (product.Remains ?? 0) + quantityDelta);
            }

            await RefreshProductPricesAsync(product);
            await _context.SaveChangesAsync();
        }

        private async Task RefreshProductPricesAsync(Product product)
        {
            var latestAcceptedSupply = await _context.SupplyItems
                .Where(si => si.ProductId == product.ProductId)
                .Join(
                    _context.Shipments.Include(sh => sh.Statussh),
                    si => si.ShipmentId,
                    sh => sh.ShipmentId,
                    (si, sh) => new { Supply = si, Shipment = sh })
                .Where(x => IsAcceptedStatus(x.Shipment.Statussh!.StatusName ?? x.Shipment.Status))
                .OrderByDescending(x => x.Shipment.ShipmentDate)
                .ThenByDescending(x => x.Shipment.ShipmentId)
                .Select(x => (decimal?)x.Supply.PriceAtShipment)
                .FirstOrDefaultAsync();

            if (!latestAcceptedSupply.HasValue)
            {
                return;
            }

            product.PurchasePrice = latestAcceptedSupply.Value;

            if (!product.SalePrice.HasValue || product.SalePrice.Value <= 0)
            {
                product.SalePrice = Math.Round(latestAcceptedSupply.Value * 1.20m, 2);
            }

            product.Price = product.SalePrice ?? product.Price;
        }

        private static bool IsAcceptedStatus(string? statusName)
        {
            if (string.IsNullOrWhiteSpace(statusName))
            {
                return false;
            }

            var normalized = statusName.Trim().ToLowerInvariant();
            return normalized.Contains("прин")
                || normalized.Contains("приш")
                || normalized.Contains("выполн")
                || normalized.Contains("достав")
                || normalized.Contains("arriv")
                || normalized.Contains("receiv")
                || normalized.Contains("complet")
                || normalized.Contains("deliver");
        }

        private bool SupplyItemExists(int shipmentId, int productId)
        {
            return _context.SupplyItems.Any(e => e.ShipmentId == shipmentId && e.ProductId == productId);
        }
    }
}
