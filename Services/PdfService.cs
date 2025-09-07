using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Properties;
using iText.Kernel.Geom;
using iText.IO.Font.Constants;
using iText.Kernel.Font;
using System;
using System.Collections.Generic;
using System.IO;
using SkladisteRobe.Models;
using QRCoder;
using System.Drawing;
using System.Drawing.Imaging;
using iText.IO.Image;
using iText.Kernel.Colors;

namespace SkladisteRobe.Services
{
    public class PdfService
    {
        public byte[] GeneratePdfReport(Transakcija transakcija, Materijal materijal)
        {
            if (transakcija == null) throw new ArgumentNullException(nameof(transakcija));
            if (materijal == null) throw new ArgumentNullException(nameof(materijal));
            using (var ms = new MemoryStream())
            {
                PdfWriter writer = new PdfWriter(ms);
                PdfDocument pdf = new PdfDocument(writer);
                Document doc = new Document(pdf, PageSize.A4);
                doc.SetMargins(25, 25, 30, 30);
                PdfFont titleFont = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);
                PdfFont normalFont = PdfFontFactory.CreateFont(StandardFonts.HELVETICA);
                string naslov = transakcija.Tip == "Ulaz" ? "Radni nalog za unos robe" : "Radni nalog za otpremu robe";
                doc.Add(new Paragraph(naslov).SetFont(titleFont).SetFontSize(18));
                doc.Add(new Paragraph(" "));
                doc.Add(new Paragraph("Naziv materijala: " + (materijal.Naziv ?? "N/A")).SetFont(normalFont).SetFontSize(12));
                doc.Add(new Paragraph("Količina operacije: " + transakcija.Kolicina).SetFont(normalFont).SetFontSize(12));
                doc.Add(new Paragraph("Tip operacije: " + (transakcija.Tip ?? "N/A")).SetFont(normalFont).SetFontSize(12));
                doc.Add(new Paragraph("Datum i vrijeme: " + transakcija.Datum.ToString("dd.MM.yyyy HH:mm:ss")).SetFont(normalFont).SetFontSize(12));
                if (!string.IsNullOrEmpty(materijal.QRCodeData))
                {
                    QRCodeGenerator qrGenerator = new QRCodeGenerator();
                    QRCodeData qrCodeData = qrGenerator.CreateQrCode(materijal.QRCodeData, QRCodeGenerator.ECCLevel.Q);
                    QRCode qrCode = new QRCode(qrCodeData);
                    Bitmap qrBitmap = qrCode.GetGraphic(20);
                    using (MemoryStream bitmapStream = new MemoryStream())
                    {
                        qrBitmap.Save(bitmapStream, ImageFormat.Png);
                        bitmapStream.Position = 0;
                        iText.Layout.Element.Image qrImage = new iText.Layout.Element.Image(ImageDataFactory.Create(bitmapStream.ToArray()));
                        qrImage.ScaleAbsolute(100f, 100f);
                        doc.Add(new Paragraph("QR Kod materijala:").SetFont(normalFont).SetFontSize(12));
                        doc.Add(qrImage);
                    }
                }
                doc.Close();
                return ms.ToArray();
            }
        }
        public byte[] GenerateAllMaterialsPdf(List<Materijal> materijali)
        {
            using (var ms = new MemoryStream())
            {
                PdfWriter writer = new PdfWriter(ms);
                PdfDocument pdf = new PdfDocument(writer);
                Document doc = new Document(pdf, PageSize.A4.Rotate());
                doc.SetMargins(25, 25, 30, 30);
                PdfFont titleFont = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);
                PdfFont normalFont = PdfFontFactory.CreateFont(StandardFonts.HELVETICA);
                doc.Add(new Paragraph("Pregled Materijala").SetFont(titleFont).SetFontSize(18).SetHorizontalAlignment(HorizontalAlignment.CENTER));
                doc.Add(new Paragraph(" "));
                Table table = new Table(3).UseAllAvailableWidth();
                table.AddHeaderCell(new Cell().Add(new Paragraph("ID")).SetBackgroundColor(ColorConstants.LIGHT_GRAY));
                table.AddHeaderCell(new Cell().Add(new Paragraph("Naziv")).SetBackgroundColor(ColorConstants.LIGHT_GRAY));
                table.AddHeaderCell(new Cell().Add(new Paragraph("Količina")).SetBackgroundColor(ColorConstants.LIGHT_GRAY));
                foreach (var m in materijali ?? new List<Materijal>())
                {
                    table.AddCell(new Paragraph(m.Id.ToString()).SetFont(normalFont).SetFontSize(12));
                    table.AddCell(new Paragraph(m.Naziv ?? "N/A").SetFont(normalFont).SetFontSize(12));
                    table.AddCell(new Paragraph(m.Kolicina.ToString()).SetFont(normalFont).SetFontSize(12));
                }
                doc.Add(table);
                doc.Close();
                return ms.ToArray();
            }
        }
        public byte[] GenerateTransakcijePdf(List<Transakcija> transakcije)
        {
            using (var ms = new MemoryStream())
            {
                PdfWriter writer = new PdfWriter(ms);
                PdfDocument pdf = new PdfDocument(writer);
                Document doc = new Document(pdf, PageSize.A4);
                doc.SetMargins(25, 25, 30, 30);
                PdfFont titleFont = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);
                PdfFont normalFont = PdfFontFactory.CreateFont(StandardFonts.HELVETICA);
                doc.Add(new Paragraph("Pregled Prošlih Transakcija").SetFont(titleFont).SetFontSize(18).SetHorizontalAlignment(HorizontalAlignment.CENTER));
                doc.Add(new Paragraph(" "));
                Table table = new Table(4).UseAllAvailableWidth();
                table.AddHeaderCell(new Cell().Add(new Paragraph("ID")).SetBackgroundColor(ColorConstants.LIGHT_GRAY));
                table.AddHeaderCell(new Cell().Add(new Paragraph("Datum")).SetBackgroundColor(ColorConstants.LIGHT_GRAY));
                table.AddHeaderCell(new Cell().Add(new Paragraph("Vrijeme")).SetBackgroundColor(ColorConstants.LIGHT_GRAY));
                table.AddHeaderCell(new Cell().Add(new Paragraph("Kreirao")).SetBackgroundColor(ColorConstants.LIGHT_GRAY));
                foreach (var t in transakcije ?? new List<Transakcija>())
                {
                    table.AddCell(new Paragraph(t.Id.ToString()).SetFont(normalFont).SetFontSize(12));
                    table.AddCell(new Paragraph(t.Datum.ToString("dd.MM.yyyy")).SetFont(normalFont).SetFontSize(12));
                    table.AddCell(new Paragraph(t.Datum.ToString("HH:mm:ss")).SetFont(normalFont).SetFontSize(12));
                    string creator = t.Korisnik != null ? $"{t.Korisnik.Ime ?? "N/A"} {t.Korisnik.Prezime ?? "N/A"}" : "Nepoznato";
                    table.AddCell(new Paragraph(creator).SetFont(normalFont).SetFontSize(12));
                }
                doc.Add(table);
                doc.Close();
                return ms.ToArray();
            }
        }
        public byte[] GenerateBulkTransactionPdf(BulkTransactionViewModel model, string transactionType, string employeeName)
        {
            using (var ms = new MemoryStream())
            {
                PdfWriter writer = new PdfWriter(ms);
                PdfDocument pdf = new PdfDocument(writer);
                Document doc = new Document(pdf, PageSize.A4.Rotate());
                doc.SetMargins(25, 25, 30, 30);
                PdfFont titleFont = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);
                PdfFont normalFont = PdfFontFactory.CreateFont(StandardFonts.HELVETICA);
                string naslov = transactionType == "Primka" ? "Radni nalog za unos robe" : "Radni nalog za otpremu robe";
                doc.Add(new Paragraph(naslov).SetFont(titleFont).SetFontSize(18).SetHorizontalAlignment(HorizontalAlignment.CENTER));
                doc.Add(new Paragraph(" "));
                doc.Add(new Paragraph("Kreirao: " + (employeeName ?? "N/A")).SetFont(normalFont).SetFontSize(12));
                doc.Add(new Paragraph("Datum: " + DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss")).SetFont(normalFont).SetFontSize(12));
                doc.Add(new Paragraph(" "));
                Table table = new Table(2).UseAllAvailableWidth();
                table.AddHeaderCell(new Cell().Add(new Paragraph("Naziv materijala")).SetBackgroundColor(ColorConstants.LIGHT_GRAY));
                table.AddHeaderCell(new Cell().Add(new Paragraph("Količina")).SetBackgroundColor(ColorConstants.LIGHT_GRAY));
                foreach (var item in model.Items ?? new List<BulkTransactionItemViewModel>())
                {
                    table.AddCell(new Paragraph(item.Naziv ?? "N/A").SetFont(normalFont).SetFontSize(12));
                    table.AddCell(new Paragraph(item.Kolicina.ToString()).SetFont(normalFont).SetFontSize(12));
                }
                doc.Add(table);
                doc.Close();
                return ms.ToArray();
            }
        }
    }
}