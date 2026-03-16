using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SkladisteRobe.Data; // Za AppDbContext
using SkladisteRobe.Models; // Za Korisnik, Uloga
using System.Security.Claims; // Za custom claims
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication; // Za SignInAsync
using Microsoft.AspNetCore.Authentication.Cookies; // Za CookieAuthenticationDefaults

namespace SkladisteRobe.Controllers
{
    public class AccountController : Controller
    {
        private readonly AppDbContext _context; // Koristi DbContext za direktan pristup

        public AccountController(AppDbContext context)
        {
            _context = context;
        }

        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                // Provjera plain text lozinke direktno
                var korisnik = await _context.Korisnici
                    .FirstOrDefaultAsync(k => k.Username == model.Username && k.Password == model.Password);
                if (korisnik != null)
                {
                    korisnik.LastLoginTime = DateTime.UtcNow;
                    await _context.SaveChangesAsync();
                    await SignInKorisnik(korisnik);
                    return RedirectToAction("Index", "Home");
                }
                ModelState.AddModelError("", "Pogrešni podaci.");
            }
            return View(model);
        }

        public async Task<IActionResult> Logout()
        {
            var korisnikIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (int.TryParse(korisnikIdClaim, out int korisnikId))
            {
                var korisnik = await _context.Korisnici.FindAsync(korisnikId);
                if (korisnik != null && korisnik.LastLoginTime.HasValue)
                {
                    var duration = DateTime.UtcNow - korisnik.LastLoginTime.Value;
                    korisnik.TotalLoginDuration += duration;
                    await _context.SaveChangesAsync();
                }
            }
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login");
        }

        public async Task<IActionResult> Transakcije()
        {
            var transakcije = await _context.Transakcije
                .Include(t => t.Korisnik)
                .Include(t => t.Materijal)
                .OrderByDescending(t => t.Datum)
                .ToListAsync();
            return View(transakcije);
        }

        // Helper metoda za custom sign in sa claims za role
        private async Task SignInKorisnik(Korisnik korisnik)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, korisnik.Username),
                new Claim(ClaimTypes.NameIdentifier, korisnik.Id.ToString()),
                new Claim(ClaimTypes.Role, korisnik.Role.ToString())
            };
            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);
        }

        // Ako netko pokuša pristupiti Register, redirect na error
        public IActionResult Register()
        {
            return RedirectToAction("AccessDenied"); // Ili custom error view
        }

        [HttpPost]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            return RedirectToAction("AccessDenied");
        }

        // Dodaj AccessDenied ako treba
        public IActionResult AccessDenied()
        {
            return View(); // Kreiraj Views/Shared/AccessDenied.cshtml sa porukom "Pristup zabranjen"
        }
    }
}