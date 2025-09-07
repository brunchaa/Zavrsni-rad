using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using SkladisteRobe.Data;
using SkladisteRobe.Models;
using System.Threading.Tasks;

namespace SkladisteRobe.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<Korisnik> _userManager;
        private readonly SignInManager<Korisnik> _signInManager;

        public AccountController(UserManager<Korisnik> userManager, SignInManager<Korisnik> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }

        // Zadržano: Get za register
        public IActionResult Register()
        {
            return View();
        }

        // Modificirano: Post za register (koristi Identity za hash i role)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = new Korisnik
                {
                    UserName = model.Username,
                    Ime = model.Ime,
                    Prezime = model.Prezime,
                    Role = Uloga.Zaposlenik // Default za kompatibilnost
                };
                var result = await _userManager.CreateAsync(user, model.Password); // Identity hashira
                if (result.Succeeded)
                {
                    await _userManager.AddToRoleAsync(user, "Radnik"); // Novo: Dodaj rolu
                    await _signInManager.SignInAsync(user, isPersistent: false);
                    return RedirectToAction("Index", "Home");
                }
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }
            return View(model);
        }

        // Zadržano: Get za login
        public IActionResult Login()
        {
            return View();
        }

        // Modificirano: Post za login (koristi Identity, dodaje praćenje vremena)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                var result = await _signInManager.PasswordSignInAsync(model.Username, model.Password, false, false);
                if (result.Succeeded)
                {
                    var user = await _userManager.FindByNameAsync(model.Username);
                    user.LastLoginTime = DateTime.UtcNow; // Novo: Počni praćenje
                    await _userManager.UpdateAsync(user);
                    return RedirectToAction("Index", "Home");
                }
                ModelState.AddModelError("", "Invalid username or password.");
            }
            return View(model);
        }

        // Modificirano: Logout (update duration)
        public async Task<IActionResult> Logout()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user != null && user.LastLoginTime.HasValue)
            {
                var duration = DateTime.UtcNow - user.LastLoginTime.Value;
                user.TotalLoginDuration += duration; // Novo: Dodaj dužinu sesije
                await _userManager.UpdateAsync(user);
            }
            await _signInManager.SignOutAsync();
            return RedirectToAction("Login");
        }
    }
}