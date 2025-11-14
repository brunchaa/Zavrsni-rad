using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SkladisteRobe.Data;
using SkladisteRobe.Models;
using System.Linq;
using System.Threading.Tasks;

namespace SkladisteRobe.Controllers
{
    [Authorize(Roles = "Voditelj,Admin")]  // Voditelj može vidjeti
    public class DashboardController : Controller
    {
        private readonly AppDbContext _context;

        public DashboardController(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var viewModel = new DashboardViewModel
            {
                MaterijalPoKategoriji = await _context.Materijali.GroupBy(m => m.Jedinica)
                    .Select(g => new DashboardViewModel.MaterijalKategorija { Kategorija = g.Key.ToString(), Kolicina = g.Sum(m => m.Kolicina) }).ToListAsync(),
                TransakcijeStats = await _context.Transakcije.GroupBy(t => t.Tip)
                    .Select(g => new DashboardViewModel.TransakcijaStat { Tip = g.Key, Broj = g.Count() }).ToListAsync(),
                UserStats = await _context.Korisnici.Select(u => new DashboardViewModel.UserStat { UserName = u.Username, LastLoginTime = u.LastLoginTime, TotalLoginDuration = u.TotalLoginDuration }).ToListAsync()
            };

            return View(viewModel);
        }
    }
}