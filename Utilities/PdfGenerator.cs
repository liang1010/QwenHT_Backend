using iTextSharp.text;
using iTextSharp.text.pdf;
using System.IO;
using QwenHT.Models;

namespace QwenHT.Utilities
{
    public static class PdfGenerator
    {
        public static byte[] GenerateTherapistCommissionReport(
            string therapistName,
            DateTime periodStart,
            DateTime periodEnd,
            List<TherapistCommissionReportItem> commissionData)
        {
            using var memoryStream = new MemoryStream();
            var document = new Document(PageSize.A4); // Landscape orientation for more columns
            var writer = PdfWriter.GetInstance(document, memoryStream);
            
            document.Open();

            // Add title
            var titleFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 16);
            var title = new Paragraph($"Therapist Commission Report - {therapistName}", titleFont)
            {
                Alignment = Element.ALIGN_CENTER
            };
            document.Add(title);
            
            document.Add(new Paragraph($"Period: {periodStart.ToUniversalTime().AddHours(8):dd/MM/yyyy} - {periodEnd.ToUniversalTime().AddHours(8):dd/MM/yyyy}")
            {
                Alignment = Element.ALIGN_CENTER
            });
            
            document.Add(new Paragraph(" ")); // Space

            // Add report date
            var dateFont = FontFactory.GetFont(FontFactory.HELVETICA, 10);
            var dateParagraph = new Paragraph($"Generated on: {DateTime.Now:dd/MM/yyyy HH:mm}", dateFont)
            {
                Alignment = Element.ALIGN_RIGHT
            };
            document.Add(dateParagraph);

            document.Add(new Paragraph(" ")); // Space

            // Create table
            var columns = new[] { 1f, 1f, 1f, 1f, 1f, 1f };
            var table = new PdfPTable(columns)
            {
                WidthPercentage = 100
            };

            // Header row
            var headerFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10);
            var headers = new[] { "Sales Date", "Menu Code", "Foot Mins", "Body Mins", "Staff Commission", "Extra Commission" };
            
            foreach (var header in headers)
            {
                var cell = new PdfPCell(new Phrase(header, headerFont))
                {
                    HorizontalAlignment = Element.ALIGN_CENTER,
                    VerticalAlignment = Element.ALIGN_MIDDLE,
                    BackgroundColor = BaseColor.LightGray
                };
                table.AddCell(cell);
            }

            // Data rows
            var dataFont = FontFactory.GetFont(FontFactory.HELVETICA, 9);
            foreach (var item in commissionData)
            {
                table.AddCell(new PdfPCell(new Phrase(item.SalesDate.ToUniversalTime().AddHours(8).ToString("dd/MM/yyyy"), dataFont))
                {
                    HorizontalAlignment = Element.ALIGN_CENTER
                });
                
                table.AddCell(new PdfPCell(new Phrase(item.MenuCode, dataFont))
                {
                    HorizontalAlignment = Element.ALIGN_CENTER
                });
                
                table.AddCell(new PdfPCell(new Phrase(item.FootMins.ToString(), dataFont))
                {
                    HorizontalAlignment = Element.ALIGN_CENTER
                });
                
                table.AddCell(new PdfPCell(new Phrase(item.BodyMins.ToString(), dataFont))
                {
                    HorizontalAlignment = Element.ALIGN_CENTER
                });
                
                table.AddCell(new PdfPCell(new Phrase($"MYR {item.StaffCommission:F2}", dataFont))
                {
                    HorizontalAlignment = Element.ALIGN_RIGHT
                });
                
                table.AddCell(new PdfPCell(new Phrase($"MYR {item.ExtraCommission:F2}", dataFont))
                {
                    HorizontalAlignment = Element.ALIGN_RIGHT
                });
            }

            // Add totals row
            var totalFootMins = commissionData.Sum(x => x.FootMins);
            var totalBodyMins = commissionData.Sum(x => x.BodyMins);
            var totalStaffCommission = commissionData.Sum(x => x.StaffCommission);
            var totalExtraCommission = commissionData.Sum(x => x.ExtraCommission);
            
            var totalsFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 9);
            table.AddCell(new PdfPCell(new Phrase("TOTALS", totalsFont))
            {
                HorizontalAlignment = Element.ALIGN_CENTER,
                BackgroundColor = BaseColor.LightGray
            });
            
            table.AddCell(new PdfPCell(new Phrase("", totalsFont))
            {
                BackgroundColor = BaseColor.LightGray
            });
            
            table.AddCell(new PdfPCell(new Phrase(totalFootMins.ToString(), totalsFont))
            {
                HorizontalAlignment = Element.ALIGN_CENTER,
                BackgroundColor = BaseColor.LightGray
            });
            
            table.AddCell(new PdfPCell(new Phrase(totalBodyMins.ToString(), totalsFont))
            {
                HorizontalAlignment = Element.ALIGN_CENTER,
                BackgroundColor = BaseColor.LightGray
            });
            
            table.AddCell(new PdfPCell(new Phrase($"${totalStaffCommission:F2}", totalsFont))
            {
                HorizontalAlignment = Element.ALIGN_RIGHT,
                BackgroundColor = BaseColor.LightGray
            });
            
            table.AddCell(new PdfPCell(new Phrase($"${totalExtraCommission:F2}", totalsFont))
            {
                HorizontalAlignment = Element.ALIGN_RIGHT,
                BackgroundColor = BaseColor.LightGray
            });

            document.Add(table);

            document.Close();

            return memoryStream.ToArray();
        }
    }

    public class TherapistCommissionReportItem
    {
        public DateTimeOffset SalesDate { get; set; }
        public string MenuCode { get; set; } = string.Empty;
        public int FootMins { get; set; }
        public int BodyMins { get; set; }
        public decimal StaffCommission { get; set; }
        public decimal ExtraCommission { get; set; }
    }
}