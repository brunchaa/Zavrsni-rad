using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using SkladisteRobe.Data;
using SkladisteRobe.Models;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace SkladisteRobe.Controllers
{
    [AllowAnonymous]
    public class AccountController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IPasswordHasher<Korisnik> _passwordHasher;

        public AccountController(AppDbContext context, IPasswordHasher<Korisnik> passwordHasher)
        {
            _context = context;
            _passwordHasher = passwordHasher;
        }

        [HttpGet]
        public IActionResult Login()
        {
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index", "Home");
            }

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                var korisnik = await _context.Korisnici
                    .FirstOrDefaultAsync(k => k.Username == model.Username);

                if (korisnik != null && !string.IsNullOrEmpty(korisnik.PasswordHash))
                {
                    var result = _passwordHasher.VerifyHashedPassword(
                        korisnik,
                        korisnik.PasswordHash,
                        model.Password
                    );

                    if (result == PasswordVerificationResult.Success ||
                        result == PasswordVerificationResult.SuccessRehashNeeded)
                    {
                        if (result == PasswordVerificationResult.SuccessRehashNeeded)
                        {
                            korisnik.PasswordHash = _passwordHasher.HashPassword(korisnik, model.Password);
                        }

                        korisnik.LastLoginTime = DateTime.UtcNow;
                        await _context.SaveChangesAsync();

                        await SignInKorisnik(korisnik);

                        return RedirectToAction("Index", "Home");
                    }
                }

                ModelState.AddModelError("", "Pogrešni podaci.");
            }

            return View(model);
        }

        [Authorize]
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

        public IActionResult Register()
        {
            return RedirectToAction("AccessDenied");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Register(RegisterViewModel model)
        {
            return RedirectToAction("AccessDenied");
        }

        public IActionResult AccessDenied()
        {
            return View();
        }

        private async Task SignInKorisnik(Korisnik korisnik)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, korisnik.Username ?? ""),
                new Claim(ClaimTypes.NameIdentifier, korisnik.Id.ToString()),
                new Claim(ClaimTypes.Role, korisnik.Role.ToString())
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);
        }
    }
}