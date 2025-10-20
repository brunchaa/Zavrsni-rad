using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ZXing;
using ZXing.Common;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using SkladisteRobe.Data;  // Dodaj svoj namespace za kontekst baze (pretpostavljam da imaš AppDbContext)
using SkladisteRobe.Models;  // Dodaj za modele (npr. Materijal, RadniNalog, StavkaRadnogNaloga)
using Microsoft.EntityFrameworkCore;  // Za EF

namespace SkladisteRobe.Controllers
{
    [Authorize(Roles = "Radnik,Voditelj,Admin")]
    public class BarcodeController : Controller
    {
        private readonly AppDbContext _context;  // Tvoja baza kontekst – injectaj ga

        public BarcodeController(AppDbContext context)
        {
            _context = context;
        }

        // View za skeniranje (otvori ga unutar radnog naloga)
        public IActionResult Scan(int radniNalogId)  // Dodaj ID radnog naloga da znaš kamo dodati stavke
        {
            ViewBag.RadniNalogId = radniNalogId;  // Proslijedi u view za JS
            return View();
        }

        // Obrada skeniranog barkoda – ovdje automatski dodaj stavku u radni nalog
        [HttpPost]
        public async Task<IActionResult> ProcessScan(string barcodeData, int radniNalogId)
        {
            try
            {
                // Parsiraj podatke iz barkoda (npr. "MaterijalId:123")
                var parts = barcodeData.Split(':');
                if (parts.Length != 2 || parts[0] != "MaterijalId")
                    return Json(new { success = false, message = "Neispravan barkod" });

                int materijalId = int.Parse(parts[1]);

                // Dohvati materijal iz baze
                var materijal = await _context.Materijali.FindAsync(materijalId);
                if (materijal == null)
                    return Json(new { success = false, message = "Proizvod ne postoji" });

                // Dohvati radni nalog
                var radniNalog = await _context.RadniNalozi.FindAsync(radniNalogId);
                if (radniNalog == null)
                    return Json(new { success = false, message = "Radni nalog ne postoji" });

                // Kreiraj novu stavku sa svim podacima ispunjenim (automatski)
                var stavka = new StavkaRadnogNaloga
                {
                    RadniNalogId = radniNalogId,
                    MaterijalId = materijalId,
                    Kolicina = 1,  // Default 1, ili možeš pitati korisnika kasnije
                    Naziv = materijal.Naziv,  // Ispunjeno iz baze
                   
                };

                _context.StavkeRadnogNaloga.Add(stavka);
                await _context.SaveChangesAsync();

                return Json(new { success = true, data = barcodeData, stavkaId = stavka.Id });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // Generiranje barkoda za proizvod
        public IActionResult GenerateBarcode(int materijalId)
        {
            var barcodeWriter = new BarcodeWriterPixelData
            {
                Format = BarcodeFormat.CODE_128,  // Standardni za tekst
                Options = new EncodingOptions
                {
                    Height = 80,
                    Width = 300,
                    Margin = 10
                }
            };

            var pixelData = barcodeWriter.Write($"MaterijalId:{materijalId}");

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
                return File(ms.ToArray(), "image/png", $"barkod_{materijalId}.png");  // Možeš downloadati
            }
        }
    }
}