using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SkladisteRobe.Models;
using System;
using System.Collections.Generic;
using System.IO;
using ZXing;
using ZXing.Common;
using System.Drawing;
using System.Drawing.Imaging;

namespace SkladisteRobe.Services
{
    public class PdfService
    {
        static PdfService()
        {
            QuestPDF.Settings.License = LicenseType.Community;  // Besplatna licenca
        }

        public byte[] GeneratePdfReport(Transakcija transakcija, Materijal materijal)
        {
            if (transakcija == null) throw new ArgumentNullException(nameof(transakcija));
            if (materijal == null) throw new ArgumentNullException(nameof(materijal));

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(2, Unit.Centimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(12));

                    page.Header()
                        .Text(transakcija.Tip == "Ulaz" ? "Radni nalog za unos robe" : "Radni nalog za otpremu robe")
                        .SemiBold().FontSize(20).FontColor(Colors.Blue.Medium);

                    page.Content()
                        .PaddingVertical(1, Unit.Centimetre)
                        .Column(x =>
                        {
                            x.Spacing(5);

                            x.Item().Text($"Naziv materijala: {materijal.Naziv ?? "N/A"}");
                            x.Item().Text($"Količina operacije: {transakcija.Kolicina}");
                            x.Item().Text($"Tip operacije: {transakcija.Tip ?? "N/A"}");
                            x.Item().Text($"Datum i vrijeme: {transakcija.Datum:dd.MM.yyyy HH:mm:ss}");

                            if (!string.IsNullOrEmpty(materijal.QRCodeData))
                            {
                                x.Item().Text("Barkod materijala:");
                                x.Item().Image(GenerateBarcodeBytes(materijal.QRCodeData)).FitWidth();
                            }
                        });

                    page.Footer()
                        .AlignCenter()
                        .Text(x =>
                        {
                            x.Span("Stranica ");
                            x.CurrentPageNumber();
                        });
                });
            });

            return document.GeneratePdf();
        }

        public byte[] GenerateAllMaterialsPdf(List<Materijal> materijali)
        {
            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4.Landscape());
                    page.Margin(2, Unit.Centimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(12));

                    page.Header()
                        .Text("Pregled Materijala")
                        .SemiBold().FontSize(20).FontColor(Colors.Blue.Medium);

                    page.Content()
                        .PaddingVertical(1, Unit.Centimetre)
                        .Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn();  // ID
                                columns.RelativeColumn(2); // Naziv
                                columns.RelativeColumn();  // Količina
                                columns.RelativeColumn();  // Jedinica
                                columns.RelativeColumn();  // Barkod Data
                            });

                            table.Header(header =>
                            {
                                header.Cell().Background(Colors.Grey.Lighten1).Text("ID").Bold();
                                header.Cell().Background(Colors.Grey.Lighten1).Text("Naziv").Bold();
                                header.Cell().Background(Colors.Grey.Lighten1).Text("Količina").Bold();
                                header.Cell().Background(Colors.Grey.Lighten1).Text("Jedinica").Bold();
                                header.Cell().Background(Colors.Grey.Lighten1).Text("Barkod Data").Bold();
                            });

                            foreach (var m in materijali ?? new List<Materijal>())
                            {
                                table.Cell().Text(m.Id.ToString());
                                table.Cell().Text(m.Naziv ?? "N/A");
                                table.Cell().Text(m.Kolicina.ToString());
                                table.Cell().Text(m.Jedinica.ToString());
                                table.Cell().Text(m.QRCodeData ?? "N/A");
                            }
                        });

                    page.Footer()
                        .AlignCenter()
                        .Text(x =>
                        {
                            x.Span("Stranica ");
                            x.CurrentPageNumber();
                        });
                });
            });

            return document.GeneratePdf();
        }

        public byte[] GenerateTransakcijePdf(List<Transakcija> transakcije)
        {
            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(2, Unit.Centimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(12));

                    page.Header()
                        .Text("Pregled Prošlih Transakcija")
                        .SemiBold().FontSize(20).FontColor(Colors.Blue.Medium);

                    page.Content()
                        .PaddingVertical(1, Unit.Centimetre)
                        .Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn();  // ID
                                columns.RelativeColumn(2); // Datum
                                columns.RelativeColumn();  // Vrijeme
                                columns.RelativeColumn(2); // Kreirao
                            });

                            table.Header(header =>
                            {
                                header.Cell().Background(Colors.Grey.Lighten1).Text("ID").Bold();
                                header.Cell().Background(Colors.Grey.Lighten1).Text("Datum").Bold();
                                header.Cell().Background(Colors.Grey.Lighten1).Text("Vrijeme").Bold();
                                header.Cell().Background(Colors.Grey.Lighten1).Text("Kreirao").Bold();
                            });

                            foreach (var t in transakcije ?? new List<Transakcija>())
                            {
                                table.Cell().Text(t.Id.ToString());
                                table.Cell().Text(t.Datum.ToString("dd.MM.yyyy"));
                                table.Cell().Text(t.Datum.ToString("HH:mm:ss"));
                                string creator = t.Korisnik != null ? $"{t.Korisnik.Ime ?? "N/A"} {t.Korisnik.Prezime ?? "N/A"}" : "Nepoznato";
                                table.Cell().Text(creator);
                            }
                        });

                    page.Footer()
                        .AlignCenter()
                        .Text(x =>
                        {
                            x.Span("Stranica ");
                            x.CurrentPageNumber();
                        });
                });
            });

            return document.GeneratePdf();
        }

        public byte[] GenerateBulkTransactionPdf(BulkTransactionViewModel model, string transactionType, string employeeName)
        {
            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4.Landscape());
                    page.Margin(2, Unit.Centimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(12));

                    page.Header()
                        .Text(transactionType == "Primka" ? "Radni nalog za unos robe" : "Radni nalog za otpremu robe")
                        .SemiBold().FontSize(20).FontColor(Colors.Blue.Medium);

                    page.Content()
                        .PaddingVertical(1, Unit.Centimetre)
                        .Column(x =>
                        {
                            x.Spacing(5);

                            x.Item().Text($"Kreirao: {employeeName ?? "N/A"}");
                            x.Item().Text($"Datum: {DateTime.Now:dd.MM.yyyy HH:mm:ss}");
                        })
                        .Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(2); // Naziv materijala
                                columns.RelativeColumn();  // Količina
                            });

                            table.Header(header =>
                            {
                                header.Cell().Background(Colors.Grey.Lighten1).Text("Naziv materijala").Bold();
                                header.Cell().Background(Colors.Grey.Lighten1).Text("Količina").Bold();
                            });

                            foreach (var item in model.Items ?? new List<BulkTransactionItemViewModel>())
                            {
                                table.Cell().Text(item.Naziv ?? "N/A");
                                table.Cell().Text(item.Kolicina.ToString());
                            }
                        });

                    page.Footer()
                        .AlignCenter()
                        .Text(x =>
                        {
                            x.Span("Stranica ");
                            x.CurrentPageNumber();
                        });
                });
            });

            return document.GeneratePdf();
        }

        // Helper za generiranje barkoda
        private byte[] GenerateBarcodeBytes(string barcodeData)
        {
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

            var pixelData = barcodeWriter.Write(barcodeData);

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
                bitmap.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                return ms.ToArray();
            }
        }
    }
}