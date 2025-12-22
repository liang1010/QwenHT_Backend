using iTextSharp.text;
using iTextSharp.text.pdf;
using QwenHT.Controllers;
using QwenHT.Models;
using System.Globalization;
using System.IO;

namespace QwenHT.Utilities
{
    public static class PdfGenerator
    {
        private static string FormatMYR(decimal value)
        {
            return value == 0
                ? ""
                : $"MYR {value.ToString("#,##0.00", CultureInfo.InvariantCulture)}";
        }

        private static string FormatMins(int mins)
        {
            return mins == 0 ? "" : mins.ToString();
        }

        private static void AddHeader(PdfPTable table, Font font, params string[] titles)
        {
            foreach (var t in titles)
            {
                table.AddCell(new PdfPCell(new Phrase(t, font))
                {
                    HorizontalAlignment = Element.ALIGN_CENTER,
                    BackgroundColor = BaseColor.LightGray
                });
            }
        }

        private static PdfPCell SpacerCell(int colspan) =>
            new PdfPCell(new Phrase(" "))
            {
                Colspan = colspan,
                FixedHeight = 6,
                Border = Rectangle.NO_BORDER
            };

        private static PdfPCell Center(string text, Font font, int border = Rectangle.NO_BORDER) =>
            new PdfPCell(new Phrase(text, font))
            {
                HorizontalAlignment = Element.ALIGN_CENTER,
                Border = border
            };

        private static PdfPCell Left(string text, Font font) =>
            new PdfPCell(new Phrase(text, font))
            {
                HorizontalAlignment = Element.ALIGN_LEFT,
                Border = Rectangle.NO_BORDER
            };

        private static PdfPCell Right(string text, Font font, int border = Rectangle.NO_BORDER) =>
            new PdfPCell(new Phrase(text, font))
            {
                HorizontalAlignment = Element.ALIGN_RIGHT,
                Border = border
            };

        private static PdfPCell Header(string text, Font font, int align = Element.ALIGN_CENTER) =>
            new PdfPCell(new Phrase(text, font))
            {
                HorizontalAlignment = align,
                BackgroundColor = BaseColor.LightGray
            };

        private static PdfPCell FooterLabel(string text, int colspan) =>
            new PdfPCell(new Phrase(text, FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 9)))
            {
                Colspan = colspan,
                HorizontalAlignment = Element.ALIGN_RIGHT,
                Border = Rectangle.TOP_BORDER
            };

        public static byte[] GenerateTherapistCommissionReport(
    string therapistName,
    DateTimeOffset? periodStart,
    DateTimeOffset? periodEnd,
    TherapistCommissionReportDto data)
        {
            using var ms = new MemoryStream();
            var document = new Document(PageSize.A4, 15, 15, 10, 10);
            PdfWriter.GetInstance(document, ms);
            document.Open();

            // =======================
            // Fonts
            // =======================
            var titleFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 12);
            var headerFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10);
            var dataFont = FontFactory.GetFont(FontFactory.HELVETICA, 9);
            var boldFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 9);

            // =======================
            // Title
            // =======================
            document.Add(new Paragraph($"Therapist Commission Report - {therapistName}", titleFont)
            {
                Alignment = Element.ALIGN_CENTER
            });

            document.Add(new Paragraph(
                $"Period: {periodStart:dd/MM/yyyy} - {periodEnd:dd/MM/yyyy}",
                dataFont)
            { Alignment = Element.ALIGN_CENTER });

            document.Add(new Paragraph($"Generated on: {DateTime.Now:dd/MM/yyyy HH:mm}", dataFont)
            { Alignment = Element.ALIGN_RIGHT });

            // =======================
            // COMMISSION TABLE
            // =======================
            var table = new PdfPTable(new float[]
            {
        1.2f, // Sales Date
        2.8f, // Menu Code
        1f,   // Foot Mins
        1f,   // Body Mins
        1.8f, // Staff Commission
        1.8f  // Extra Commission
            })
            {
                WidthPercentage = 100
            };

            AddHeader(table, headerFont,
                "Sales Date",
                "Menu Code",
                "Foot Mins",
                "Body Mins",
                "Staff Commission",
                "Extra Commission");

            DateTime? lastDate = null;

            foreach (var row in data.Commissions)
            {
                if (lastDate != null && lastDate != row.SalesDate.Date)
                {
                    table.AddCell(Center("", dataFont, Rectangle.LEFT_BORDER));
                    table.AddCell(SpacerCell(4));
                    table.AddCell(Center("", dataFont, Rectangle.RIGHT_BORDER));
                }

                lastDate = row.SalesDate.Date;

                table.AddCell(Center(row.SalesDate.ToString("dd/MM/yyyy"), dataFont, Rectangle.LEFT_BORDER));
                table.AddCell(Center(row.MenuCode, dataFont));
                table.AddCell(Right(FormatMins(row.FootMins), dataFont));
                table.AddCell(Right(FormatMins(row.BodyMins), dataFont));
                table.AddCell(Right(FormatMYR(row.StaffCommission), dataFont));
                table.AddCell(Right(FormatMYR(row.ExtraCommission), dataFont, Rectangle.RIGHT_BORDER));
            }

            // =======================
            // TOTALS
            // =======================
            table.AddCell(Header("TOTALS", boldFont));
            table.AddCell(Header("", boldFont));
            table.AddCell(Header(data.Commissions.Sum(x => x.FootMins).ToString(), boldFont));
            table.AddCell(Header(data.Commissions.Sum(x => x.BodyMins).ToString(), boldFont));
            table.AddCell(Header(FormatMYR(data.Commissions.Sum(x => x.StaffCommission)), boldFont, Element.ALIGN_RIGHT));
            table.AddCell(Header(FormatMYR(data.Commissions.Sum(x => x.ExtraCommission)), boldFont, Element.ALIGN_RIGHT));
            table.AddCell(Header("TOTALS", boldFont));
            table.AddCell(Header("", boldFont));
            table.AddCell(Header(data.Commissions.Sum(x => x.FootMins).ToString(), boldFont));
            table.AddCell(Header(data.Commissions.Sum(x => x.BodyMins).ToString(), boldFont));
            table.AddCell(Header(FormatMYR(data.Commissions.Sum(x => x.StaffCommission)), boldFont, Element.ALIGN_RIGHT));
            table.AddCell(Header(FormatMYR(data.Commissions.Sum(x => x.ExtraCommission)), boldFont, Element.ALIGN_RIGHT));

            document.Add(table);

            // =======================
            // INCENTIVE TABLE
            // =======================
            document.Add(Chunk.Newline);
            document.Add(new Paragraph("Incentives", headerFont));

            var incentiveTable = new PdfPTable(new float[]
            {
        1.5f, // Date
        3f,   // Description
        3f,   // Remark
        1.5f  // Amount
            })
            {
                WidthPercentage = 100
            };

            AddHeader(incentiveTable, headerFont,
                "Incentive Date",
                "Description",
                "Remark",
                "Amount");

            foreach (var i in data.Incentives)
            {
                incentiveTable.AddCell(Center(i.IncentiveDate.ToString("dd/MM/yyyy"), dataFont));
                incentiveTable.AddCell(Left(i.Description, dataFont));
                incentiveTable.AddCell(Left(i.Remark, dataFont));
                incentiveTable.AddCell(Right(FormatMYR(i.Amount), dataFont));
            }

            // Totals
            incentiveTable.AddCell(FooterLabel("Total Incentive :", 3));
            incentiveTable.AddCell(Right(FormatMYR(data.TotalIncentive), boldFont));

            incentiveTable.AddCell(FooterLabel("Total Payout :", 3));
            incentiveTable.AddCell(Right(FormatMYR(data.TotalPayout), boldFont));

            document.Add(incentiveTable);

            document.Close();
            return ms.ToArray();
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
}