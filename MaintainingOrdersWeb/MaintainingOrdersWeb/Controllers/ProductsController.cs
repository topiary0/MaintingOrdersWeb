using MaintainingOrdersWeb.Models;
using MaintainingOrdersWeb.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace MaintainingOrdersWeb.Controllers
{
    [Authorize(Roles = "Директор,Менеджер,Логист,Сотрудник,Бухгалтер")]
    public class ProductsController : Controller
    {
        private readonly MyDbContext _context;
        private readonly DeleteDependencyService _deleteDependencyService;

        public ProductsController(MyDbContext context, DeleteDependencyService deleteDependencyService)
        {
            _context = context;
            _deleteDependencyService = deleteDependencyService;
        }

        // GET: Products
        public async Task<IActionResult> Index()
        {
            var products = _context.Products.Include(p => p.Suppliers);
            return View(await products.ToListAsync());
        }

        // GET: Products/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var product = await _context.Products
                .Include(p => p.Suppliers)
                .FirstOrDefaultAsync(m => m.ProductId == id);
            if (product == null) return NotFound();

            return View(product);
        }

        // GET: Products/Create
        [Authorize(Roles = "Директор")]
        public IActionResult Create()
        {
            ViewData["SuppliersId"] = new SelectList(_context.Suppliers, "SuppliersId", "SupplierName");
            return View();
        }

        // POST: Products/Create
        [Authorize(Roles = "Директор")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ProductId,Article,Name,Price,Description,SalePrice,PurchasePrice,Remains,Weight,Dimensions,SuppliersId")] Product product)
        {
            // Убираем ошибки навигационных свойств
            ModelState.Remove("Suppliers");

            // Дополнительные проверки, если нужно (например, уникальность артикула)
            if (await _context.Products.AnyAsync(p => p.Article == product.Article))
            {
                ModelState.AddModelError("Article", "Товар с таким артикулом уже существует");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Add(product);
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

            ViewData["SuppliersId"] = new SelectList(_context.Suppliers, "SuppliersId", "SupplierName", product.SuppliersId);
            return View(product);
        }

        // GET: Products/Edit/5
        [Authorize(Roles = "Директор")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var product = await _context.Products.FindAsync(id);
            if (product == null) return NotFound();

            ViewData["SuppliersId"] = new SelectList(_context.Suppliers, "SuppliersId", "SupplierName", product.SuppliersId);
            return View(product);
        }

        // POST: Products/Edit/5
        [Authorize(Roles = "Директор")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("ProductId,Article,Name,Price,Description,SalePrice,PurchasePrice,Remains,Weight,Dimensions,SuppliersId")] Product product)
        {
            if (id != product.ProductId) return NotFound();

            ModelState.Remove("Suppliers");

            // Проверка уникальности артикула, исключая текущий товар
            if (await _context.Products.AnyAsync(p => p.Article == product.Article && p.ProductId != id))
            {
                ModelState.AddModelError("Article", "Товар с таким артикулом уже существует");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(product);
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ProductExists(product.ProductId)) return NotFound();
                    else throw;
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Ошибка при сохранении: " + ex.Message);
                    if (ex.InnerException != null)
                        ModelState.AddModelError("", ex.InnerException.Message);
                }
            }

            ViewData["SuppliersId"] = new SelectList(_context.Suppliers, "SuppliersId", "SupplierName", product.SuppliersId);
            return View(product);
        }

        // GET: Products/Delete/5
        [Authorize(Roles = "Директор")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var product = await _context.Products
                .Include(p => p.Suppliers)
                .FirstOrDefaultAsync(m => m.ProductId == id);
            if (product == null) return NotFound();

            await ApplyDeleteInfoAsync("Product", id.Value);

            return View(product);
        }

        // POST: Products/Delete/5
        [Authorize(Roles = "Директор")]
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var deleteInfo = await _deleteDependencyService.CheckAsync("Product", id);
            if (!deleteInfo.CanDelete)
            {
                TempData["DeleteDependencyWarning"] = deleteInfo.WarningMessage;
                return RedirectToAction(nameof(Delete), new { id });
            }
            var product = await _context.Products.FindAsync(id);
            if (product != null)
                _context.Products.Remove(product);

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }


        private async Task ApplyDeleteInfoAsync(string entityName, int id)
        {
            var deleteInfo = await _deleteDependencyService.CheckAsync(entityName, id);
            ViewBag.CanDelete = deleteInfo.CanDelete;
            ViewBag.DependencyWarning = TempData["DeleteDependencyWarning"] as string ?? deleteInfo.WarningMessage;
        }

        private bool ProductExists(int id)
        {
            return _context.Products.Any(e => e.ProductId == id);
        }
    }
}
