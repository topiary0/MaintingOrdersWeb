using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MaintainingOrdersWeb.Models;
using MaintainingOrdersWeb.Services;

namespace MaintainingOrdersWeb.Controllers
{
    [Authorize(Roles = "Директор")]
    public class RolesController : Controller
    {
        private readonly MyDbContext _context;
        private readonly DeleteDependencyService _deleteDependencyService;

        public RolesController(MyDbContext context, DeleteDependencyService deleteDependencyService)
        {
            _context = context;
            _deleteDependencyService = deleteDependencyService;
        }

        // GET: Roles (список всех ролей)
        public async Task<IActionResult> Index()
        {
            return View(await _context.Roles.ToListAsync());
        }

        // GET: Roles/Details/5 (просмотр одной роли)
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var role = await _context.Roles.FirstOrDefaultAsync(m => m.RoleId == id);
            if (role == null) return NotFound();

            return View(role);
        }

        // GET: Roles/Create (форма создания)
        public IActionResult Create()
        {
            return View();
        }

        // POST: Roles/Create (сохранение новой роли)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("RoleId,RoleName")] Role role)
        {
            if (ModelState.IsValid)
            {
                _context.Add(role);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(role);
        }

        // GET: Roles/Edit/5 (форма редактирования)
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var role = await _context.Roles.FindAsync(id);
            if (role == null) return NotFound();

            return View(role);
        }

        // POST: Roles/Edit/5 (сохранение изменений)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("RoleId,RoleName")] Role role)
        {
            if (id != role.RoleId) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(role);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!RoleExists(role.RoleId)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(role);
        }

        // GET: Roles/Delete/5 (подтверждение удаления)
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var role = await _context.Roles.FirstOrDefaultAsync(m => m.RoleId == id);
            if (role == null) return NotFound();

            await ApplyDeleteInfoAsync("Role", id.Value);

            return View(role);
        }

        // POST: Roles/Delete/5 (фактическое удаление)
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var deleteInfo = await _deleteDependencyService.CheckAsync("Role", id);
            if (!deleteInfo.CanDelete)
            {
                TempData["DeleteDependencyWarning"] = deleteInfo.WarningMessage;
                return RedirectToAction(nameof(Delete), new { id });
            }
            var role = await _context.Roles.FindAsync(id);
            if (role != null) _context.Roles.Remove(role);

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }


        private async Task ApplyDeleteInfoAsync(string entityName, int id)
        {
            var deleteInfo = await _deleteDependencyService.CheckAsync(entityName, id);
            ViewBag.CanDelete = deleteInfo.CanDelete;
            ViewBag.DependencyWarning = TempData["DeleteDependencyWarning"] as string ?? deleteInfo.WarningMessage;
        }

        private bool RoleExists(int id)
        {
            return _context.Roles.Any(e => e.RoleId == id);
        }
    }
}
