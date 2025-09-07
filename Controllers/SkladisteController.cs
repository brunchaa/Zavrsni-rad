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
    [Authorize(Roles = "Radnik,Voditelj,Admin")] // Novo: Ograniči na role (radnik vidi samo osnovno)
    public class SkladisteController : Controller
    {
        private readonly AppDbContext _context;
        private readonly PdfService _pdfService;

        public SkladisteController(AppDbContext context)
        {
            _context = context;
            _pdfService = new PdfService();
        }

        // Zadržano: Index sa search, unaprijeđeno sa paging ako treba (dodaj ako želiš)
        public IActionResult Index(string searchString)
        {
            var materijali = _context.Materijali.AsQueryable();
            if (!string.IsNullOrEmpty(searchString))
            {
                materijali = materijali.Where(m => m.Naziv.Contains(searchString));
            }
            return View(materijali.ToList());
        }

        // Zadržano: RadniNalog
        public IActionResult RadniNalog()
        {
            return View();
        }

        // Zadržano: Post RadniNalog, unaprijeđeno sa boljom validacijom i QR generacijom (dodaj QRData ako treba)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult RadniNalog(BulkTransactionViewModel model, string submitType)
        {
            if (!ModelState.IsValid)
                return View(model);

            // Zadržano: Provjera duplikata
            var duplicateNames = model.Items.GroupBy(x => x.Naziv.ToLower())
                                              .Where(g => g.Count() > 1)
                                              .Select(g => g.Key)
                                              .ToList();
            if (duplicateNames.Any())
            {
                ModelState.AddModelError("", "Ne možete unijeti više stavki s istim nazivom: " + string.Join(", ", duplicateNames));
                return View(model);
            }

            // Zadržano: Uzmi user ID i ime (koristi Identity)
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var fullName = User.FindFirstValue("FullName") ?? "Nepoznati korisnik";

            // Zadržano: Procesiraj items, ali dodaj QRData za novi materijal
            foreach (var item in model.Items)
            {
                if (submitType == "Primka")
                {
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
                            KorisnikId = int.Parse(userId)
                        });
                    }
                    else
                    {
                        var newMat = new Materijal
                        {
                            Naziv = item.Naziv,
                            Kolicina = item.Kolicina,
                            Jedinica = item.Jedinica,
                            QRCodeData = $"Materijal:{item.Naziv}:{item.Jedinica}" // Novo: Generiraj QR data
                        };
                        _context.Materijali.Add(newMat);
                        _context.SaveChanges();
                        _context.Transakcije.Add(new Transakcija
                        {
                            MaterijalId = newMat.Id,
                            Kolicina = item.Kolicina,
                            Datum = DateTime.Now,
                            Tip = "Primka",
                            KorisnikId = int.Parse(userId)
                        });
                    }
                }
                else if (submitType == "Izdaj robu")
                {
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
                        KorisnikId = int.Parse(userId)
                    });
                }
                else
                {
                    ModelState.AddModelError("", "Nepoznat tip transakcije.");
                    return View(model);
                }
            }

            _context.SaveChanges();

            // Zadržano: Generiraj PDF (PdfService će dodati QR ako treba)
            var pdfBytes = _pdfService.GenerateBulkTransactionPdf(model, submitType, fullName);
            string fileName = submitType == "Primka" ? "Primka.pdf" : "IzdajRobu.pdf";
            return File(pdfBytes, "application/pdf", fileName);
        }

        // Zadržano: Transakcije, unaprijeđeno sa include
        public IActionResult Transakcije()
        {
            var transakcije = _context.Transakcije
                .Include(t => t.Korisnik)
                .Include(t => t.Materijal) // Novo: Include za bolji prikaz
                .OrderByDescending(t => t.Datum)
                .ToList();
            return View(transakcije);
        }

        // Zadržano: GenerateTransakcijePdf
        public IActionResult GenerateTransakcijePdf()
        {
            var transakcije = _context.Transakcije
                .Include(t => t.Korisnik)
                .OrderByDescending(t => t.Datum)
                .ToList();
            var pdfBytes = _pdfService.GenerateTransakcijePdf(transakcije);
            return File(pdfBytes, "application/pdf", "Transakcije.pdf");
        }

        // Zadržano: GeneratePdf za transakciju
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

        // Zadržano: GenerateAllPdf
        public IActionResult GenerateAllPdf()
        {
            var materijali = _context.Materijali.ToList();
            var pdfBytes = _pdfService.GenerateAllMaterialsPdf(materijali);
            return File(pdfBytes, "application/pdf", "SviMaterijali.pdf");
        }
    }
}