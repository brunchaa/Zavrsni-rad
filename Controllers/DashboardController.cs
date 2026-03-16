using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SkladisteRobe.Data;
using SkladisteRobe.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace SkladisteRobe.Controllers
{
    [Authorize(Roles = "Voditelj,Admin")]
    public class DashboardController : Controller
    {
        private readonly AppDbContext _context;

        public DashboardController(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var sevenDaysAgo = DateTime.Now.AddDays(-7);
            var today = DateTime.Now.Date;

            var viewModel = new DashboardViewModel
            {
                MaterijalPoKategoriji = await _context.Materijali.GroupBy(m => m.Jedinica)
                    .Select(g => new DashboardViewModel.MaterijalKategorija { Kategorija = g.Key.ToString(), Kolicina = g.Sum(m => m.Kolicina) }).ToListAsync(),
                TransakcijeStats = await _context.Transakcije.GroupBy(t => t.Tip)
                    .Select(g => new DashboardViewModel.TransakcijaStat { Tip = g.Key, Broj = g.Count() }).ToListAsync(),
                UserStats = await _context.Korisnici.Select(u => new DashboardViewModel.UserStat
                {
                    UserName = u.Username,
                    LastLoginTime = u.LastLoginTime,
                    TotalLoginDuration = u.TotalLoginDuration,
                    DailyLoginDuration = (u.LastLoginTime.HasValue && u.LastLoginTime.Value.Date == today) ? (DateTime.UtcNow - u.LastLoginTime.Value) : TimeSpan.Zero  // Dnevno logiranje (ako logiran danas)
                }).ToListAsync(),

                // Dodani korisni featureovi
                LowStockMaterials = await _context.Materijali.Where(m => m.Kolicina < 10).ToListAsync(),  // Alert za niske stockove
                RecentTransakcije = await _context.Transakcije
                    .Include(t => t.Materijal).Include(t => t.Korisnik)
                    .OrderByDescending(t => t.Datum).Take(10).ToListAsync(),  // Recentne transakcije
                TransakcijePoDanima = await _context.Transakcije.Where(t => t.Datum >= sevenDaysAgo)
                    .GroupBy(t => t.Datum.Date)
                    .Select(g => new DashboardViewModel.TransakcijaPoDanu { Datum = g.Key, Broj = g.Count() }).ToListAsync(),  // Za graf transakcija po danima
                TopMaterials = await _context.Transakcije.GroupBy(t => t.MaterijalId)
                    .Select(g => new DashboardViewModel.TopMaterijal
                    {
                        Naziv = _context.Materijali.FirstOrDefault(m => m.Id == g.Key).Naziv ?? "N/A",
                        BrojTransakcija = g.Count()
                    })
                    .OrderByDescending(tm => tm.BrojTransakcija).Take(5).ToListAsync(),  // Top materijali po transakcijama
                TopUsersByTransakcije = await _context.Transakcije.GroupBy(t => t.KorisnikId)
                    .Select(g => new DashboardViewModel.TopUser
                    {
                        UserName = _context.Korisnici.FirstOrDefault(k => k.Id == g.Key).Username ?? "N/A",
                        BrojTransakcija = g.Count()
                    })
                    .OrderByDescending(tu => tu.BrojTransakcija).Take(5).ToListAsync(),  // Top korisnici po broju transakcija (korisno za praćenje aktivnosti)
                AverageDailyTransactions = (await _context.Transakcije.Where(t => t.Datum >= sevenDaysAgo).CountAsync()) / 7.0  // Prosječan broj transakcija po danu zadnjih 7 dana
            };

            return View(viewModel);
        }
    }
}