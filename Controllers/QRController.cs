using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QRCoder;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

namespace SkladisteRobe.Controllers
{
    [Authorize(Roles = "Radnik,Voditelj,Admin")]
    public class QRController : Controller
    {
        public IActionResult Scan()
        {
            return View();
        }
        [HttpPost]
        public IActionResult ProcessScan(string qrData)
        {
            return Json(new { success = true, data = qrData });
        }
        public IActionResult GenerateQR(int materijalId)
        {
            QRCodeGenerator qrGenerator = new QRCodeGenerator();
            QRCodeData qrCodeData = qrGenerator.CreateQrCode($"MaterijalId:{materijalId}", QRCodeGenerator.ECCLevel.Q);
            QRCoder.QRCode qrCode = new QRCoder.QRCode(qrCodeData);
            Bitmap qrCodeImage = qrCode.GetGraphic(20);
            var stream = new MemoryStream();
            qrCodeImage.Save(stream, ImageFormat.Png);
            stream.Position = 0;
            return File(stream.ToArray(), "image/png");
        }
    }
}