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
        
        public async Task<IActionResult> Index(string searchString, int? searchId)
        {
            var materijali = _context.Materijali.AsQueryable();
            if (searchId.HasValue)
            {
                materijali = materijali.Where(m => m.Id == searchId.Value);
            }
            else if (!string.IsNullOrEmpty(searchString))
            {
                materijali = materijali.Where(m => m.Naziv.Contains(searchString));
            }
            return View(await materijali.ToListAsync());
        }
        
        public IActionResult RadniNalog()
        {
            return View(new BulkTransactionViewModel());
        }
        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RadniNalog(BulkTransactionViewModel model, string submitType)
        {
            if (!ModelState.IsValid)
                return View(model);
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var user = await _context.Korisnici.FindAsync(userId);
            var fullName = user != null ? $"{user.Ime} {user.Prezime}" : "Nepoznato";
            var batchId = Guid.NewGuid(); 
            foreach (var item in model.Items)
            {
                var existing = await _context.Materijali
                    .FirstOrDefaultAsync(m => m.Naziv.ToLower() == item.Naziv.ToLower() && m.Jedinica == item.Jedinica);
                if (submitType == "Primka")
                {
                    if (existing != null)
                    {
                        existing.Kolicina += item.Kolicina;
                        item.MaterijalId = existing.Id; 
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
                        await _context.SaveChangesAsync(); 
                        item.MaterijalId = newMat.Id; 
                        newMat.QRCodeData = $"MaterijalId:{newMat.Id}"; 
                        await _context.SaveChangesAsync(); 
                    }
                    _context.Transakcije.Add(new Transakcija
                    {
                        MaterijalId = item.MaterijalId,
                        Kolicina = item.Kolicina,
                        Datum = DateTime.Now,
                        Tip = "Primka",
                        KorisnikId = userId,
                        BatchId = batchId 
                    });
                }
                else if (submitType == "Međuskladišnica")
                {
                    if (existing == null || existing.Kolicina < item.Kolicina)
                    {
                        ModelState.AddModelError("", $"Nema dovoljno {item.Naziv} ({item.Jedinica})");
                        return View(model);
                    }
                    existing.Kolicina -= item.Kolicina;
                    item.MaterijalId = existing.Id; 
                    _context.Transakcije.Add(new Transakcija
                    {
                        MaterijalId = item.MaterijalId,
                        Kolicina = item.Kolicina,
                        Datum = DateTime.Now,
                        Tip = "Međuskladišnica",
                        KorisnikId = userId,
                        BatchId = batchId 
                    });
                }
            }
            await _context.SaveChangesAsync();
            var pdfBytes = _pdfService.GenerateBulkTransactionPdf(model, submitType, fullName);
            var currentDate = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            var fileName = submitType == "Primka"
                ? $"Primka_{currentDate}.pdf"
                : $"Meduskladisnica_{currentDate}.pdf";

            var cd = new System.Net.Mime.ContentDisposition
            {
                FileName = fileName,
                Inline = false 
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
            
            var groupedTransakcije = transakcije
                .GroupBy(t => t.BatchId ?? Guid.NewGuid()) 
                .Select(g => new GroupedTransakcija
                {
                    BatchId = g.Key,
                    Datum = g.First().Datum,
                    Tip = g.First().Tip,
                    Korisnik = g.First().Korisnik,
                    Stavke = g.ToList()
                })
                .OrderByDescending(g => g.Datum)
                .ToList();
            return View(groupedTransakcije);
        }

        [Authorize(Roles = "Voditelj,Admin")]
        public IActionResult GenerateTransakcijePdf()
        {
            var transakcije = _context.Transakcije
                .Include(t => t.Korisnik)
                .OrderByDescending(t => t.Datum)
                .ToList();
            var pdfBytes = _pdfService.GenerateTransakcijePdf(transakcije);
            return File(pdfBytes, "application/pdf", "Transakcije.pdf");
        }

        [Authorize(Roles = "Voditelj,Admin")]
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

        [Authorize(Roles = "Zaposlenik,Voditelj,Admin")]
        public IActionResult GenerateAllPdf()
        {
            var materijali = _context.Materijali.ToList();
            var pdfBytes = _pdfService.GenerateAllMaterialsPdf(materijali);
            return File(pdfBytes, "application/pdf", "SviMaterijali.pdf");
        }
        
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
        
        public async Task<IActionResult> GetMaterijal(int id)
        {
            var materijal = await _context.Materijali.FindAsync(id);
            if (materijal == null)
                return Json(new { success = false });
            return Json(new { success = true, naziv = materijal.Naziv, jedinica = materijal.Jedinica.ToString(), id = materijal.Id, kolicina = materijal.Kolicina });
        }

        [Authorize(Roles = "Voditelj,Admin")]
        public IActionResult GeneratePdfForBatch(Guid batchId)
        {
            var transakcije = _context.Transakcije
                .Include(t => t.Korisnik)
                .Include(t => t.Materijal)
                .Where(t => t.BatchId == batchId)
                .ToList();
            if (transakcije.Count == 0)
                return NotFound();
            var model = new BulkTransactionViewModel
            {
                Items = transakcije.Select(t => new BulkTransactionItemViewModel
                {
                    MaterijalId = t.MaterijalId,
                    Naziv = t.Materijal?.Naziv ?? "N/A",
                    Kolicina = t.Kolicina,
                    Jedinica = t.Materijal?.Jedinica ?? MjernaJedinica.KOMAD
                }).ToList()
            };
            var tip = transakcije.First().Tip;
            var fullName = transakcije.First().Korisnik?.Ime + " " + transakcije.First().Korisnik?.Prezime ?? "Nepoznato";
            var pdfBytes = _pdfService.GenerateBulkTransactionPdf(model, tip, fullName);
            return File(pdfBytes, "application/pdf", $"Batch_{batchId}.pdf");
        }
    }
}