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
        public IActionResult Create()
        {
            return View();
        }

        // POST: DeliveryMethods/Create
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
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var deliveryMethod = await _context.DeliveryMethods.FindAsync(id);
            if (deliveryMethod == null) return NotFound();

            return View(deliveryMethod);
        }

        // POST: DeliveryMethods/Edit/5
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
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var deliveryMethod = await _context.DeliveryMethods
                .FirstOrDefaultAsync(m => m.MethodId == id);
            if (deliveryMethod == null) return NotFound();

            return View(deliveryMethod);
        }

        // POST: DeliveryMethods/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
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
    }
}
