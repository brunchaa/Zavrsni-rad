using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using SkladisteRobe.Data;
using SkladisteRobe.Models;
using SkladisteRobe.Services;
using System;
using System.Linq;
using System.Security.Claims;

namespace SkladisteRobe.Controllers
{
    [Authorize]
    public class SkladisteController : Controller
    {
        private readonly AppDbContext _context;
        private readonly PdfService _pdfService;

        public SkladisteController(AppDbContext context)
        {
            _context = context;
            _pdfService = new PdfService();
        }

        // gettaj skladiste index
        public IActionResult Index(string searchString)
        {
            var materijali = _context.Materijali.AsQueryable();
            if (!string.IsNullOrEmpty(searchString))
            {
                materijali = materijali.Where(m => m.Naziv.Contains(searchString));
            }
            return View(materijali.ToList());
        }

        // gettaj  skladiste/radni nalog prikazi bulk transakciju
        public IActionResult RadniNalog()
        {
            return View();
        }

        // postaj skladiste/radni nalog procesiraj bulk transakciju
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult RadniNalog(BulkTransactionViewModel model, string submitType)
        {
            if (!ModelState.IsValid)
                return View(model);

            //provjeri za duplikate imena
            var duplicateNames = model.Items.GroupBy(x => x.Naziv.ToLower())
                                              .Where(g => g.Count() > 1)
                                              .Select(g => g.Key)
                                              .ToList();
            if (duplicateNames.Any())
            {
                ModelState.AddModelError("", "Ne možete unijeti više stavki s istim nazivom: " + string.Join(", ", duplicateNames));
                return View(model);
            }

            // uzmi ulogiranom useru id i puno ime
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            int userId = 0;
            if (userIdClaim != null)
                int.TryParse(userIdClaim.Value, out userId);
            var fullName = User.FindFirst("FullName")?.Value ?? "Nepoznati korisnik";

            // procesuiraj svaki bulk item
            foreach (var item in model.Items)
            {
                if (submitType == "Primka")
                {
                    // ako vec postoji dodaj mu ako ne onda napravi novi
                    var existingMat = _context.Materijali
                        .FirstOrDefault(m => m.Naziv.ToLower() == item.Naziv.ToLower() && m.Jedinica == item.Jedinica);
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
                            Jedinica = item.Jedinica
                        };
                        _context.Materijali.Add(newMat);
                        _context.SaveChanges(); // potvrdi da se newmaterijal napravi
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
                    // uvjeti za postojanje
                    var existingMat = _context.Materijali
                        .FirstOrDefault(m => m.Naziv.ToLower() == item.Naziv.ToLower() && m.Jedinica == item.Jedinica);
                    if (existingMat == null)
                    {
                        ModelState.AddModelError("", $"Materijal s nazivom {item.Naziv} i jedinicom {item.Jedinica} ne postoji.");
                        return View(model);
                    }
                    if (existingMat.Kolicina < item.Kolicina)
                    {
                        ModelState.AddModelError("", $"Nema dovoljno količine za {item.Naziv} (jedinica: {item.Jedinica}). Trenutno ima: {existingMat.Kolicina}");
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

            _context.SaveChanges();

            // kreiraj bulk transakciju u pdfu za radni nalog
            var pdfBytes = _pdfService.GenerateBulkTransactionPdf(model, submitType, fullName);
            string fileName = submitType == "Primka" ? "Primka.pdf" : "IzdajRobu.pdf";
            return File(pdfBytes, "application/pdf", fileName);
        }

        // gettaj skladiste/transakcije ispisuje listu proslih transakcija
        public IActionResult Transakcije()
        {
            // ispisi puna imena
            var transakcije = _context.Transakcije
                .Include(t => t.Korisnik)
                .OrderByDescending(t => t.Datum)
                .ToList();
            return View(transakcije);
        }

        // gettaj skladiste/generatetransakcijepdf – generiraj pdf sa svim proslim transakcijama
        public IActionResult GenerateTransakcijePdf()
        {
            var transakcije = _context.Transakcije
                .Include(t => t.Korisnik)
                .OrderByDescending(t => t.Datum)
                .ToList();
            var pdfBytes = _pdfService.GenerateTransakcijePdf(transakcije);
            return File(pdfBytes, "application/pdf", "Transakcije.pdf");
        }

        // gettaj skladiste/generatepdf/{id} generiraj pdf za specificnu transakciju
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

        // gettaj skladiste/generateallpdf generiraj izvjesće za sve materijale
        public IActionResult GenerateAllPdf()
        {
            var materijali = _context.Materijali.ToList();
            var pdfBytes = _pdfService.GenerateAllMaterialsPdf(materijali);
            return File(pdfBytes, "application/pdf", "SviMaterijali.pdf");
        }
    }
}