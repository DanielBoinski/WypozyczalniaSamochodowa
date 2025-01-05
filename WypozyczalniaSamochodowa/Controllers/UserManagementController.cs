using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WypozyczalniaSamochodowa.Data;

namespace WypozyczalniaSamochodowa.Controllers
{
    [Authorize(Roles = "Administrator")]
    public class UserManagementController : Controller
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ApplicationDbContext _context;

        public UserManagementController(
            UserManager<IdentityUser> userManager,
            RoleManager<IdentityRole> roleManager,
            ApplicationDbContext context)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _context = context;
        }

        // GET: UserManagement
        public async Task<IActionResult> Index()
        {
            var users = _userManager.Users.ToList();
            var adminUsers = new List<string>();

            foreach (var user in users)
            {
                if (await _userManager.IsInRoleAsync(user, "Administrator"))
                {
                    adminUsers.Add(user.Email);
                }
            }

            ViewBag.AdminUsers = adminUsers;

            return View(users);
        }

        // POST: UserManagement/AssignAdminRole
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignAdminRole(string id)
        {
            if (string.IsNullOrEmpty(id))
                return BadRequest("ID użytkownika nie może być puste.");

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
                return NotFound("Nie znaleziono użytkownika.");

            if (!await _roleManager.RoleExistsAsync("Administrator"))
            {
                await _roleManager.CreateAsync(new IdentityRole("Administrator"));
            }

            if (!await _userManager.IsInRoleAsync(user, "Administrator"))
            {
                var result = await _userManager.AddToRoleAsync(user, "Administrator");
                if (result.Succeeded)
                {
                    TempData["SuccessMessage"] = "Rola Administratora została przypisana pomyślnie.";
                }
                else
                {
                    TempData["ErrorMessage"] = "Nie udało się przypisać roli Administratora.";
                }
            }
            else
            {
                TempData["InfoMessage"] = "Użytkownik już posiada rolę Administratora.";
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: UserManagement/Delete/5
        public async Task<IActionResult> Delete(string id)
        {
            if (string.IsNullOrEmpty(id))
                return NotFound();

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
                return NotFound();

            return View(user);
        }

        // POST: UserManagement/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string id, bool confirmed = true)
        {
            if (string.IsNullOrEmpty(id))
                return NotFound();

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
                return NotFound();

            // Usuń role użytkownika
            var roles = await _userManager.GetRolesAsync(user);
            if (roles.Any())
            {
                await _userManager.RemoveFromRolesAsync(user, roles);
            }

            // Usuń powiązane wynajmy (jeśli istnieją w bazie danych)
            var wynajmy = _context.Wynajecie.Where(w => w.UserId == id).ToList();
            if (wynajmy.Any())
            {
                _context.Wynajecie.RemoveRange(wynajmy);
                await _context.SaveChangesAsync();
            }

            // Usuń użytkownika
            var result = await _userManager.DeleteAsync(user);
            if (result.Succeeded)
            {
                TempData["SuccessMessage"] = "Użytkownik został usunięty pomyślnie.";
            }
            else
            {
                TempData["ErrorMessage"] = "Wystąpił błąd podczas usuwania użytkownika.";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
