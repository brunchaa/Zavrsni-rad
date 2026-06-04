using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SkladisteRobe.Data;
using SkladisteRobe.Models;
using System.Threading.Tasks;

namespace SkladisteRobe.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IPasswordHasher<Korisnik> _passwordHasher;

        public AdminController(AppDbContext context, IPasswordHasher<Korisnik> passwordHasher)
        {
            _context = context;
            _passwordHasher = passwordHasher;
        }

        public async Task<IActionResult> Index()
        {
            var korisnici = await _context.Korisnici.ToListAsync();
            return View(korisnici);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangeRole(int userId, string role)
        {
            if (!Enum.TryParse<Uloga>(role, out var novaUloga))
            {
                return BadRequest("Neispravna uloga.");
            }

            var korisnik = await _context.Korisnici.FindAsync(userId);

            if (korisnik != null)
            {
                korisnik.Role = novaUloga;
                await _context.SaveChangesAsync();
            }

            return RedirectToAction("Index");
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                if (await _context.Korisnici.AnyAsync(k => k.Username == model.Username))
                {
                    ModelState.AddModelError("Username", "Korisničko ime već postoji.");
                    return View(model);
                }

                var korisnik = new Korisnik
                {
                    Username = model.Username,
                    Ime = model.Ime,
                    Prezime = model.Prezime,
                    Role = Uloga.Zaposlenik
                };

                korisnik.PasswordHash = _passwordHasher.HashPassword(korisnik, model.Password);

                _context.Korisnici.Add(korisnik);
                await _context.SaveChangesAsync();

                return RedirectToAction("Index");
            }

            return View(model);
        }
    }
}