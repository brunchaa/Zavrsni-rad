using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ZXing;
using ZXing.Common;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using SkladisteRobe.Data;
using SkladisteRobe.Models;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace SkladisteRobe.Controllers
{
    [Authorize(Roles = "Zaposlenik,Voditelj,Admin")]
    public class BarcodeController : Controller
    {
        private readonly AppDbContext _context;

        public BarcodeController(AppDbContext context)
        {
            _context = context;
        }

        public IActionResult Scan(string tip)
        {
            if (string.IsNullOrEmpty(tip) || (tip != "ulaz" && tip != "izlaz"))
            {
                return BadRequest("Neispravan tip operacije.");
            }
            ViewBag.Tip = tip;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ProcessScan(string barcodeData, string tip)
        {
            try
            {
                if (string.IsNullOrEmpty(tip) || (tip != "ulaz" && tip != "izlaz"))
                    return Json(new { success = false, message = "Neispravan tip operacije" });

                var parts = barcodeData.Split(':');
                if (parts.Length != 2 || parts[0] != "MaterijalId")
                    return Json(new { success = false, message = "Neispravan barkod" });

                if (!int.TryParse(parts[1], out int materijalId))
                    return Json(new { success = false, message = "Neispravan ID materijala" });

                var materijal = await _context.Materijali.FindAsync(materijalId);
                if (materijal == null)
                    return Json(new { success = false, message = "Materijal ne postoji" });

                if (tip == "izlaz" && materijal.Kolicina < 1)
                    return Json(new { success = false, message = "Nedovoljna količina na stanju" });

                var korisnikIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (!int.TryParse(korisnikIdClaim, out int korisnikId) || korisnikId <= 0)
                    return Json(new { success = false, message = "Ne može se identificirati korisnik" });

                var korisnik = await _context.Korisnici.FindAsync(korisnikId);
                if (korisnik == null)
                    return Json(new { success = false, message = "Korisnik ne postoji" });

                var transakcija = new Transakcija
                {
                    MaterijalId = materijalId,
                    Materijal = materijal,
                    Kolicina = 1,
                    Datum = DateTime.Now,
                    Tip = tip,
                    KorisnikId = korisnikId,
                    Korisnik = korisnik
                };

                _context.Transakcije.Add(transakcija);

                if (tip == "ulaz")
                {
                    materijal.Kolicina += 1;
                }
                else if (tip == "izlaz")
                {
                    materijal.Kolicina -= 1;
                }

                await _context.SaveChangesAsync();

                return Json(new { success = true, data = barcodeData, transakcijaId = transakcija.Id, novaKolicina = materijal.Kolicina });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        public IActionResult GenerateBarcode(int materijalId)
        {
            var materijal = _context.Materijali.Find(materijalId);
            if (materijal == null)
                return NotFound("Materijal ne postoji");

            var barcodeText = $"MaterijalId:{materijalId}";  // Standardni format bez nula
            materijal.QRCodeData = barcodeText;
            _context.SaveChanges();

            var barcodeWriter = new BarcodeWriterPixelData
            {
                Format = BarcodeFormat.CODE_128,
                Options = new EncodingOptions
                {
                    Height = 80,
                    Width = 300,
                    Margin = 10
                }
            };

            var pixelData = barcodeWriter.Write(barcodeText);

            using (var bitmap = new Bitmap(pixelData.Width, pixelData.Height, PixelFormat.Format32bppRgb))
            using (var ms = new MemoryStream())
            {
                var bitmapData = bitmap.LockBits(new Rectangle(0, 0, pixelData.Width, pixelData.Height), ImageLockMode.WriteOnly, PixelFormat.Format32bppRgb);
                try
                {
                    System.Runtime.InteropServices.Marshal.Copy(pixelData.Pixels, 0, bitmapData.Scan0, pixelData.Pixels.Length);
                }
                finally
                {
                    bitmap.UnlockBits(bitmapData);
                }
                bitmap.Save(ms, ImageFormat.Png);
                return File(ms.ToArray(), "image/png", $"barkod_{materijalId}.png");
            }
        }
    }
}