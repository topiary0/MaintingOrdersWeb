using MaintainingOrdersWeb.Infrastructure;
using MaintainingOrdersWeb.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace MaintainingOrdersWeb.Controllers
{
    [Authorize(Roles = AppRoles.Director)]
    public class UsersController : Controller
    {
        private readonly MyDbContext _context;

        public UsersController(MyDbContext context)
        {
            _context = context;
        }

        // GET: Users
        public async Task<IActionResult> Index()
        {
            var users = _context.Users.Include(u => u.Role);
            return View(await users.ToListAsync());
        }

        // GET: Users/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var user = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(m => m.UserId == id);
            if (user == null) return NotFound();

            return View(user);
        }

        // GET: Users/Create
        public IActionResult Create()
        {
            ViewData["RoleId"] = new SelectList(_context.Roles, "RoleId", "RoleName");
            return View();
        }

        // POST: Users/Create – УПРОЩЁННЫЙ ВАРИАНТ
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(User user) // Убрал Bind, принимаем всю модель
        {
            // Убираем все проверки, просто пытаемся сохранить
            try
            {
                // Убеждаемся, что UserId не передаётся (он identity)
                user.UserId = 0; // или можно не назначать, но на всякий случай обнулим

                _context.Add(user);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                // Выводим детальную ошибку прямо на страницу
                ModelState.AddModelError("", "Ошибка при сохранении: " + ex.Message);
                if (ex.InnerException != null)
                {
                    ModelState.AddModelError("", "Внутренняя ошибка: " + ex.InnerException.Message);
                }
                // Если это DbUpdateException, можно достать подробности
                if (ex is DbUpdateException dbEx && dbEx.InnerException != null)
                {
                    ModelState.AddModelError("", "SQL ошибка: " + dbEx.InnerException.Message);
                }
            }

            // Если дошли сюда, значит ошибка – возвращаем форму с ошибками
            ViewData["RoleId"] = new SelectList(_context.Roles, "RoleId", "RoleName", user.RoleId);
            return View(user);
        }

        // GET: Users/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound();

            ViewData["RoleId"] = new SelectList(_context.Roles, "RoleId", "RoleName", user.RoleId);
            return View(user);
        }

        // POST: Users/Edit/5 – УПРОЩЁННЫЙ ВАРИАНТ
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, User user) // Убрал Bind
        {
            if (id != user.UserId) return NotFound();

            try
            {
                _context.Update(user);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Ошибка при сохранении: " + ex.Message);
                if (ex.InnerException != null)
                    ModelState.AddModelError("", "Внутренняя ошибка: " + ex.InnerException.Message);
                if (ex is DbUpdateException dbEx && dbEx.InnerException != null)
                    ModelState.AddModelError("", "SQL ошибка: " + dbEx.InnerException.Message);
            }

            ViewData["RoleId"] = new SelectList(_context.Roles, "RoleId", "RoleName", user.RoleId);
            return View(user);
        }

        // GET: Users/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var user = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(m => m.UserId == id);
            if (user == null) return NotFound();

            return View(user);
        }

        // POST: Users/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user != null)
            {
                _context.Users.Remove(user);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        private bool UserExists(int id)
        {
            return _context.Users.Any(e => e.UserId == id);
        }
    }
}
