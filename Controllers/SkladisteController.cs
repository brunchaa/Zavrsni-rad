using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SkladisteRobe.Data;
using SkladisteRobe.Models;
using SkladisteRobe.Services;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace SkladisteRobe.Controllers
{
    [Authorize(Roles = "Zaposlenik,Voditelj,Admin")]  // Zaposlenik i Voditelj mogu pristupiti
    public class SkladisteController : Controller
    {
        private readonly AppDbContext _context;
        private readonly PdfService _pdfService;

        public SkladisteController(AppDbContext context, PdfService pdfService)
        {
            _context = context;
            _pdfService = pdfService;
        }

        // Index sa search
        public async Task<IActionResult> Index(string searchString)
        {
            var materijali = _context.Materijali.AsQueryable();
            if (!string.IsNullOrEmpty(searchString))
            {
                materijali = materijali.Where(m => m.Naziv.Contains(searchString));
            }
            return View(await materijali.ToListAsync());
        }

        // RadniNalog view
        public IActionResult RadniNalog()
        {
            return View();
        }

        // Post RadniNalog
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RadniNalog(BulkTransactionViewModel model, string submitType)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Provjera duplikata
            var duplicateNames = model.Items.GroupBy(x => x.Naziv.ToLower())
                                              .Where(g => g.Count() > 1)
                                              .Select(g => g.Key)
                                              .ToList();
            if (duplicateNames.Any())
            {
                ModelState.AddModelError("", "Ne možete unijeti više stavki s istim nazivom: " + string.Join(", ", duplicateNames));
                return View(model);
            }

            // Uzmi user ID i puno ime (Ime + Prezime)
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdClaim, out int userId))
            {
                ModelState.AddModelError("", "Ne može se identificirati korisnik.");
                return View(model);
            }

            var currentUser = await _context.Korisnici.FindAsync(userId);
            var fullName = currentUser != null ? $"{currentUser.Ime} {currentUser.Prezime}" : "Nepoznati korisnik";

            // Procesiraj items
            foreach (var item in model.Items)
            {
                if (submitType == "Primka")
                {
                    var existingMat = await _context.Materijali
                        .FirstOrDefaultAsync(m => m.Naziv.ToLower() == item.Naziv.ToLower() && m.Jedinica == item.Jedinica);
                    if (existingMat != null)
                    {
                        existingMat.Kolicina += item.Kolicina;
                        _context.Transakcije.Add(new Transakcija
                        {
                            MaterijalId = existingMat.Id,
                            Kolicina = item.Kolicina,
                            Datum = DateTime.Now,
                            Tip = "Primka",
                            KorisnikId = userId
                        });
                    }
                    else
                    {
                        var newMat = new Materijal
                        {
                            Naziv = item.Naziv,
                            Kolicina = item.Kolicina,
                            Jedinica = item.Jedinica,
                            QRCodeData = $"Materijal:{item.Naziv}:{item.Jedinica}"
                        };
                        _context.Materijali.Add(newMat);
                        await _context.SaveChangesAsync();
                        _context.Transakcije.Add(new Transakcija
                        {
                            MaterijalId = newMat.Id,
                            Kolicina = item.Kolicina,
                            Datum = DateTime.Now,
                            Tip = "Primka",
                            KorisnikId = userId
                        });
                    }
                }
                else if (submitType == "Izdaj robu")
                {
                    var existingMat = await _context.Materijali
                        .FirstOrDefaultAsync(m => m.Naziv.ToLower() == item.Naziv.ToLower() && m.Jedinica == item.Jedinica);
                    if (existingMat == null)
                    {
                        ModelState.AddModelError("", $"Materijal s nazivom {item.Naziv} i jedinicom {item.Jedinica} ne postoji.");
                        return View(model);
                    }
                    if (existingMat.Kolicina < item.Kolicina)
                    {
                        ModelState.AddModelError("", $"Nema dovoljno količine za {item.Naziv} (jedinica: {item.Jedinica}). Trenutno ima: {existingMat.Kolicina}.");
                        return View(model);
                    }
                    existingMat.Kolicina -= item.Kolicina;
                    _context.Transakcije.Add(new Transakcija
                    {
                        MaterijalId = existingMat.Id,
                        Kolicina = item.Kolicina,
                        Datum = DateTime.Now,
                        Tip = "Izdaj robu",
                        KorisnikId = userId
                    });
                }
                else
                {
                    ModelState.AddModelError("", "Nepoznat tip transakcije.");
                    return View(model);
                }
            }

            await _context.SaveChangesAsync();

            // Generiraj PDF
            var pdfBytes = _pdfService.GenerateBulkTransactionPdf(model, submitType, fullName);
            string fileName = submitType == "Primka" ? "Primka.pdf" : "IzdajRobu.pdf";
            return File(pdfBytes, "application/pdf", fileName);
        }

        // Transakcije
        public async Task<IActionResult> Transakcije()
        {
            var transakcije = await _context.Transakcije
                .Include(t => t.Korisnik)
                .Include(t => t.Materijal)
                .OrderByDescending(t => t.Datum)
                .ToListAsync();
            return View(transakcije);
        }

        // GenerateTransakcijePdf
        public IActionResult GenerateTransakcijePdf()
        {
            var transakcije = _context.Transakcije
                .Include(t => t.Korisnik)
                .OrderByDescending(t => t.Datum)
                .ToList();
            var pdfBytes = _pdfService.GenerateTransakcijePdf(transakcije);
            return File(pdfBytes, "application/pdf", "Transakcije.pdf");
        }

        // GeneratePdf za transakciju
        public IActionResult GeneratePdf(int id)
        {
            var transakcija = _context.Transakcije
                .Include(t => t.Korisnik)
                .FirstOrDefault(t => t.Id == id);
            if (transakcija == null)
                return NotFound();

            var materijal = _context.Materijali.FirstOrDefault(m => m.Id == transakcija.MaterijalId);
            var pdfBytes = _pdfService.GeneratePdfReport(transakcija, materijal);
            return File(pdfBytes, "application/pdf", $"Transakcija_{transakcija.Id}.pdf");
        }

        // GenerateAllPdf
        public IActionResult GenerateAllPdf()
        {
            var materijali = _context.Materijali.ToList();
            var pdfBytes = _pdfService.GenerateAllMaterialsPdf(materijali);
            return File(pdfBytes, "application/pdf", "SviMaterijali.pdf");
        }
    }
}