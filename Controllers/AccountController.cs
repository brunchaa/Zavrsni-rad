using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SkladisteRobe.Data; 
using SkladisteRobe.Models; 
using System.Security.Claims; 
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication; 
using Microsoft.AspNetCore.Authentication.Cookies; 

namespace SkladisteRobe.Controllers
{
    public class AccountController : Controller
    {
        private readonly AppDbContext _context; 

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

        
        public IActionResult Register()
        {
            return RedirectToAction("AccessDenied"); 
        }

        [HttpPost]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            return RedirectToAction("AccessDenied");
        }

        
        public IActionResult AccessDenied()
        {
            return View(); 
        }
    }
}