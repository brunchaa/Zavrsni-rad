using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SkladisteRobe.Data;  // Za AppDbContext
using SkladisteRobe.Models;  // Za Korisnik, Uloga
using System.Threading.Tasks;

namespace SkladisteRobe.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly AppDbContext _context;

        public AdminController(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var korisnici = await _context.Korisnici.ToListAsync();
            return View(korisnici);
        }

        [HttpPost]
        public async Task<IActionResult> AddToRole(int userId, string role)
        {
            var korisnik = await _context.Korisnici.FindAsync(userId);
            if (korisnik != null)
            {
                korisnik.Role = Enum.Parse<Uloga>(role);  // Dodaj ili promijeni ulogu (pretpostavljam single role po korisniku)
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> RemoveFromRole(int userId, string role)
        {
            var korisnik = await _context.Korisnici.FindAsync(userId);
            if (korisnik != null && korisnik.Role.ToString() == role)
            {
                korisnik.Role = Uloga.Zaposlenik;  // Reset na default ako uklanjaš ulogu
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("Index");
        }
    }
}