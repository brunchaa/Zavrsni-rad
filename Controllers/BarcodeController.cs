
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ZXing;
using ZXing.Common;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using SkladisteRobe.Data; // Tvoj namespace za AppDbContext
using SkladisteRobe.Models; // Za Materijal, Transakcija, Korisnik, itd.
using Microsoft.EntityFrameworkCore; // Za EF
using System.Security.Claims; // Za User claims

namespace SkladisteRobe.Controllers
{
    [Authorize(Roles = "Zaposlenik,Voditelj,Admin")]
    public class BarcodeController : Controller
    {
        private readonly AppDbContext _context; // Tvoj DbContext

        public BarcodeController(AppDbContext context)
        {
            _context = context;
        }

        // View za skeniranje (npr. za ulaz ili izlaz materijala)
        public IActionResult Scan(string tip) // "ulaz" ili "izlaz" – proslijedi iz linka ili forme
        {
            if (string.IsNullOrEmpty(tip) || (tip != "ulaz" && tip != "izlaz"))
            {
                return BadRequest("Neispravan tip operacije.");
            }
            ViewBag.Tip = tip; // Proslijedi u view za JS ili formu
            return View();
        }

        // Obrada skeniranog barkoda – dodaje Transakciju i ažurira količinu materijala
        [HttpPost]
        public async Task<IActionResult> ProcessScan(string barcodeData, string tip)
        {
            try
            {
                if (string.IsNullOrEmpty(tip) || (tip != "ulaz" && tip != "izlaz"))
                    return Json(new { success = false, message = "Neispravan tip operacije" });

                // Parsiraj podatke iz barkoda (npr. "MaterijalId:123")
                var parts = barcodeData.Split(':');
                if (parts.Length != 2 || parts[0] != "MaterijalId")
                    return Json(new { success = false, message = "Neispravan barkod" });

                if (!int.TryParse(parts[1], out int materijalId))
                    return Json(new { success = false, message = "Neispravan ID materijala" });

                // Dohvati materijal iz baze
                var materijal = await _context.Materijali.FindAsync(materijalId);
                if (materijal == null)
                    return Json(new { success = false, message = "Materijal ne postoji" });

                // Provjeri količinu za izlaz (ne smije ići u negativno)
                if (tip == "izlaz" && materijal.Kolicina < 1)
                    return Json(new { success = false, message = "Nedovoljna količina na stanju" });

                // Dohvati trenutnog korisnika iz autentifikacije
                var korisnikIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (!int.TryParse(korisnikIdClaim, out int korisnikId) || korisnikId <= 0)
                    return Json(new { success = false, message = "Ne može se identificirati korisnik" });

                var korisnik = await _context.Korisnici.FindAsync(korisnikId); // Korisnici (plural)
                if (korisnik == null)
                    return Json(new { success = false, message = "Korisnik ne postoji" });

                // Kreiraj novu Transakciju sa svim podacima ispunjenim (automatski)
                var transakcija = new Transakcija
                {
                    MaterijalId = materijalId,
                    Materijal = materijal, // EF će ga linkati
                    Kolicina = 1, // Default 1; možeš dodati parametar za više
                    Datum = DateTime.Now,
                    Tip = tip, // "ulaz" ili "izlaz"
                    KorisnikId = korisnikId,
                    Korisnik = korisnik
                };

                _context.Transakcije.Add(transakcija); // Transakcije (plural)

                // Ažuriraj količinu materijala
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

        // Generiranje barkoda za materijal (možeš ga pozvati iz MaterijalControllera nakon kreiranja)
        public IActionResult GenerateBarcode(int materijalId)
        {
            var materijal = _context.Materijali.Find(materijalId);
            if (materijal == null)
                return NotFound("Materijal ne postoji");

            var barcodeWriter = new BarcodeWriterPixelData
            {
                Format = BarcodeFormat.CODE_128, // Standardni za tekst
                Options = new EncodingOptions
                {
                    Height = 80,
                    Width = 300,
                    Margin = 10
                }
            };

            var barcodeText = $"MaterijalId:{materijalId}";
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

                // Opcionalno: Spremi u Materijal.QRCodeData za kasniji prikaz
                materijal.QRCodeData = barcodeText;
                _context.SaveChanges();

                return File(ms.ToArray(), "image/png", $"barkod_{materijalId}.png");
            }
        }
    }
}