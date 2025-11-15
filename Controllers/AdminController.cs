using Microsoft.AspNetCore.Authorization;
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
        public async Task<IActionResult> ChangeRole(int userId, string role)
        {
            var korisnik = await _context.Korisnici.FindAsync(userId);
            if (korisnik != null)
            {
                korisnik.Role = Enum.Parse<Uloga>(role);  // Promijeni ulogu na odabranu
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("Index");
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
    }
}