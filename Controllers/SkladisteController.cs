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
    [Authorize(Roles = "Zaposlenik,Voditelj,Admin")]
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
            return View(new BulkTransactionViewModel());
        }

        // Post RadniNalog
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RadniNalog(BulkTransactionViewModel model, string submitType)
        {
            if (!ModelState.IsValid)
                return View(model);

            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var user = await _context.Korisnici.FindAsync(userId);
            var fullName = user != null ? $"{user.Ime} {user.Prezime}" : "Nepoznato";

            foreach (var item in model.Items)
            {
                var existing = await _context.Materijali
                    .FirstOrDefaultAsync(m => m.Naziv.ToLower() == item.Naziv.ToLower() && m.Jedinica == item.Jedinica);

                if (submitType == "Primka")
                {
                    if (existing != null)
                    {
                        existing.Kolicina += item.Kolicina;
                        item.MaterijalId = existing.Id; // Postavi ID
                    }
                    else
                    {
                        var newMat = new Materijal
                        {
                            Naziv = item.Naziv,
                            Kolicina = item.Kolicina,
                            Jedinica = item.Jedinica
                        };
                        _context.Materijali.Add(newMat);
                        await _context.SaveChangesAsync(); // Spremi da dobije ID
                        item.MaterijalId = newMat.Id; // Postavi ID
                        newMat.QRCodeData = $"MaterijalId:{newMat.Id}"; // Automatski barkod
                        await _context.SaveChangesAsync(); // Spremi QRCodeData
                    }

                    _context.Transakcije.Add(new Transakcija
                    {
                        MaterijalId = item.MaterijalId,
                        Kolicina = item.Kolicina,
                        Datum = DateTime.Now,
                        Tip = "Primka",
                        KorisnikId = userId
                    });
                }
                else if (submitType == "Izdaj robu")
                {
                    if (existing == null || existing.Kolicina < item.Kolicina)
                    {
                        ModelState.AddModelError("", $"Nema dovoljno {item.Naziv} ({item.Jedinica})");
                        return View(model);
                    }
                    existing.Kolicina -= item.Kolicina;
                    item.MaterijalId = existing.Id; // Postavi ID

                    _context.Transakcije.Add(new Transakcija
                    {
                        MaterijalId = item.MaterijalId,
                        Kolicina = item.Kolicina,
                        Datum = DateTime.Now,
                        Tip = "Izdaj robu",
                        KorisnikId = userId
                    });
                }
            }

            await _context.SaveChangesAsync();

            var pdfBytes = _pdfService.GenerateBulkTransactionPdf(model, submitType, fullName);
            var currentDate = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            var fileName = submitType == "Primka" ? $"Primka_{currentDate}.pdf" : $"IzdajRobu_{currentDate}.pdf";

            // Dodaj pravilno encoded Content-Disposition
            var cd = new System.Net.Mime.ContentDisposition
            {
                FileName = fileName,
                Inline = false  // Za download
            };
            Response.Headers.Add("Content-Disposition", cd.ToString());

            return File(pdfBytes, "application/pdf");
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

        public IActionResult GenerateTransakcijePdf()
        {
            var transakcije = _context.Transakcije
                .Include(t => t.Korisnik)
                .OrderByDescending(t => t.Datum)
                .ToList();
            var pdfBytes = _pdfService.GenerateTransakcijePdf(transakcije);
            return File(pdfBytes, "application/pdf", "Transakcije.pdf");
        }

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

        public IActionResult GenerateAllPdf()
        {
            var materijali = _context.Materijali.ToList();
            var pdfBytes = _pdfService.GenerateAllMaterialsPdf(materijali);
            return File(pdfBytes, "application/pdf", "SviMaterijali.pdf");
        }

        // SearchMaterijali za autocomplete
        public async Task<IActionResult> SearchMaterijali(string term)
        {
            if (string.IsNullOrEmpty(term))
                return Json(new List<object>());

            var materijali = await _context.Materijali
                .Where(m => m.Naziv.Contains(term))
                .Select(m => new
                {
                    id = m.Id,
                    naziv = m.Naziv,
                    jedinica = m.Jedinica.ToString(),
                    kolicina = m.Kolicina
                })
                .Take(10)
                .ToListAsync();

            return Json(materijali);
        }

        // GetMaterijal za sken
        public async Task<IActionResult> GetMaterijal(int id)
        {
            var materijal = await _context.Materijali.FindAsync(id);
            if (materijal == null)
                return Json(new { success = false });

            return Json(new { success = true, naziv = materijal.Naziv, jedinica = materijal.Jedinica.ToString(), id = materijal.Id });
        }
    }
}