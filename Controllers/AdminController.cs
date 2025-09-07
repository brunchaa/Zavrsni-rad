using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using SkladisteRobe.Data;
using SkladisteRobe.Models;
using System.Linq;
using System.Threading.Tasks;

namespace SkladisteRobe.Controllers
{
    [Authorize(Roles = "Admin")] // Zadržano: Samo admin
    public class AdminController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<Korisnik> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public AdminController(AppDbContext context, UserManager<Korisnik> userManager, RoleManager<IdentityRole> roleManager)
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
        }

        // Modificirano: Index sa userima, include role i praćenje
        public async Task<IActionResult> Index()
        {
            var users = _context.Users.ToList(); // Koristi Identity Users
            foreach (var user in users)
            {
                user.Roles = (await _userManager.GetRolesAsync(user)).ToList(); // Novo: Dohvati role za prikaz
            }
            return View(users);
        }

        // Novo: Action za add role (pozvano iz view forme)
        [HttpPost]
        public async Task<IActionResult> AddToRole(string userId, string role)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user != null && await _roleManager.RoleExistsAsync(role))
            {
                await _userManager.AddToRoleAsync(user, role);
            }
            return RedirectToAction("Index");
        }

        // Novo: Action za remove role
        [HttpPost]
        public async Task<IActionResult> RemoveFromRole(string userId, string role)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user != null)
            {
                await _userManager.RemoveFromRoleAsync(user, role);
            }
            return RedirectToAction("Index");
        }
    }
}