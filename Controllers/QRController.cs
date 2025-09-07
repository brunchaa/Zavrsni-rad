using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QRCoder;
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
            PngByteQRCode qrCode = new PngByteQRCode(qrCodeData);
            byte[] qrCodeBytes = qrCode.GetGraphic(20);
            return File(qrCodeBytes, "image/png");
        }
    }
}