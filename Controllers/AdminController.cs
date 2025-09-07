using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using SkladisteRobe.Data;
using System.Linq;

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

        // getaj adminov index i prikazi sve usere
        public IActionResult Index()
        {
            var users = _context.Korisnici.ToList();
            return View(users);
        }
    }
}