using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using SkladisteRobe.Data;
using SkladisteRobe.Models;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace SkladisteRobe.Controllers
{
    public class AccountController : Controller
    {
        private readonly AppDbContext _context;
        private readonly PasswordHasher<Korisnik> _passwordHasher;

        public AccountController(AppDbContext context)
        {
            _context = context;
            _passwordHasher = new PasswordHasher<Korisnik>();
        }

        // gettaj account registraciju
        public IActionResult Register()
        {
            return View();
        }

        // postaj account registraciju
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(Korisnik model)
        {
            if (ModelState.IsValid)
            {
                // vidi jel postoji taj username vec
                if (_context.Korisnici.Any(k => k.Username.ToLower() == model.Username.ToLower()))
                {
                    ModelState.AddModelError("Username", "Korisničko ime je već zauzeto.");
                    return View(model);
                }

                //hashiraj sifru
                model.Password = _passwordHasher.HashPassword(model, model.Password);
                _context.Korisnici.Add(model);
                await _context.SaveChangesAsync();
                return RedirectToAction("Login");
            }
            return View(model);
        }

        // gettaj account login
        public IActionResult Login()
        {
            return View();
        }

        // postaj account login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string username, string password)
        {
            var user = _context.Korisnici.FirstOrDefault(k => k.Username == username);
            if (user != null)
            {
                var result = _passwordHasher.VerifyHashedPassword(user, user.Password, password);
                if (result == PasswordVerificationResult.Success)
                {
                    // kreira claimove i puno ime claim takoder
                    var claims = new[]
                    {
                        new Claim(ClaimTypes.Name, user.Username),
                        new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                        new Claim("FullName", $"{user.Ime} {user.Prezime}"),
                        new Claim(ClaimTypes.Role, user.Role.ToString())
                    };

                    var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                    var principal = new ClaimsPrincipal(identity);

                    // session only cookie
                    var authProperties = new AuthenticationProperties
                    {
                        IsPersistent = false,
                        ExpiresUtc = System.DateTimeOffset.UtcNow.AddMinutes(30)
                    };

                    await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, authProperties);
                    return RedirectToAction("Index", "Home");
                }
            }
            ModelState.AddModelError("", "Invalid username or password.");
            return View();
        }

        // gettaj account logout
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login");
        }
    }
}