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
                ? $"MYR {0.ToString("#,##0.00", CultureInfo.InvariantCulture)}"
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

        private static PdfPCell Header(string text, Font font, int align = Element.ALIGN_CENTER, int border = Rectangle.NO_BORDER, int colspan = 1) =>
            new PdfPCell(new Phrase(text, font))
            {
                Colspan = colspan,
                HorizontalAlignment = align,
                Border = border
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
            document.Add(new Paragraph($"Therapist Commission - {therapistName}", titleFont)
            {
                Alignment = Element.ALIGN_CENTER
            });

            var periodStartPlus8 = periodStart.Value.ToOffset(TimeSpan.FromHours(8));           // UTC+8
            var periodEndPlus8 = periodEnd.Value.ToOffset(TimeSpan.FromHours(8));           // UTC+8
            document.Add(new Paragraph(
                $"Period: {periodStartPlus8:dd/MM/yyyy} - {periodEndPlus8:dd/MM/yyyy}",
                dataFont)
            { Alignment = Element.ALIGN_CENTER });

            document.Add(new Paragraph($"Generated on: {DateTime.Now:dd/MM/yyyy HH:mm}", dataFont)
            { Alignment = Element.ALIGN_RIGHT });

            // =======================
            // COMMISSION TABLE
            // =======================

            // Define column widths dynamically
            var columnWidths = data.IsCommissionPercentage
                ? new float[] { 1.2f, 1.2f, 1.5f, 1.5f, 2.1f, 2.1f, 2.1f } // With Price
                : new float[] { 1.2f, 1.2f, 1.5f, 1.5f, 2.1f, 2.1f };       // Without Price

            var table = new PdfPTable(columnWidths)
            {
                WidthPercentage = 100
            };

            // Define headers dynamically
            var headers = data.IsCommissionPercentage
                ? new[] { "Sales Date", "Menu Code", "Foot Mins", "Body Mins", "Staff Commission", "Extra Commission", "Price" }
                : new[] { "Sales Date", "Menu Code", "Foot Mins", "Body Mins", "Staff Commission", "Extra Commission" };

            document.Add(new Paragraph("Sales", headerFont));
            // Add header to the table
            AddHeader(table, headerFont, headers);

            DateTime? lastDate = null;

            foreach (var row in data.Commissions)
            {
                if (lastDate != null && lastDate != row.SalesDate.Date)
                {
                    table.AddCell(Center("", dataFont, Rectangle.LEFT_BORDER));
                    table.AddCell(SpacerCell(data.IsCommissionPercentage ? 5 : 4));
                    table.AddCell(Center("", dataFont, Rectangle.RIGHT_BORDER));
                }

                lastDate = row.SalesDate.Date;

                table.AddCell(Center(row.SalesDate.ToString("dd/MM/yyyy"), dataFont, Rectangle.LEFT_BORDER));
                table.AddCell(Center(row.MenuCode, dataFont));
                table.AddCell(Right(FormatMins(row.FootMins), dataFont));
                table.AddCell(Right(FormatMins(row.BodyMins), dataFont));
                table.AddCell(Right(row.StaffCommission == 0 ? "" : FormatMYR(row.StaffCommission), dataFont));
                table.AddCell(Right(row.ExtraCommission == 0 ? "" : FormatMYR(row.ExtraCommission), dataFont, data.IsCommissionPercentage ? 0 : Rectangle.RIGHT_BORDER));
                if (data.IsCommissionPercentage)
                    table.AddCell(Right(row.Price == 0 ? "" : FormatMYR(row.Price), dataFont, Rectangle.RIGHT_BORDER));
            }

            // =======================
            // TOTALS
            // =======================
            if (data.IsRate)
            {
                table.AddCell(Header("TOTALS", boldFont, 1, Rectangle.LEFT_BORDER));
                table.AddCell(Header($"{data.RateBase.SelectedPeriodHrs:0.#} hrs", boldFont));
                table.AddCell(Header(FormatMYR(data.RateBase.TotalFootCommission), boldFont, Element.ALIGN_RIGHT));
                table.AddCell(Header(FormatMYR(data.RateBase.TotalBodyCommission), boldFont, Element.ALIGN_RIGHT));
                table.AddCell(Header(FormatMYR(data.RateBase.TotalStaffCommission), boldFont, Element.ALIGN_RIGHT));
                table.AddCell(Header(FormatMYR(data.RateBase.TotalExtraCommission), boldFont, Element.ALIGN_RIGHT, Rectangle.RIGHT_BORDER));

                table.AddCell(Header(" ", boldFont, 1, 6));
                var hours = data.RateBase.AllPeriodHrs == 0
    ? data.RateBase.SelectedPeriodHrs
    : data.RateBase.AllPeriodHrs;
                table.AddCell(Header($"{hours:0.#} hrs", boldFont, 1, 2));
                table.AddCell(Header("", boldFont, Element.ALIGN_RIGHT, 2));
                table.AddCell(Header("", boldFont, Element.ALIGN_RIGHT, 2));
                table.AddCell(Header("Total Commission :", boldFont, Element.ALIGN_RIGHT, 2));
                table.AddCell(Header(FormatMYR(data.RateBase.TotalCommission), boldFont, Element.ALIGN_RIGHT, 10));
            }

            else if (data.IsCommissionPercentage)
            {
                table.AddCell(Header("TOTALS", boldFont, 1, Rectangle.LEFT_BORDER));
                table.AddCell(Header($"{data.CommissionPercentage.SelectedPeriodHrs:0.#} hrs", boldFont));
                table.AddCell(Header("", boldFont, Element.ALIGN_RIGHT));
                table.AddCell(Header("", boldFont, Element.ALIGN_RIGHT));
                table.AddCell(Header(FormatMYR(data.CommissionPercentage.TotalStaffCommission), boldFont, Element.ALIGN_RIGHT));
                table.AddCell(Header(FormatMYR(data.CommissionPercentage.TotalExtraCommission), boldFont, Element.ALIGN_RIGHT));
                table.AddCell(Header(FormatMYR(data.CommissionPercentage.TotalPrice), boldFont, Element.ALIGN_RIGHT, Rectangle.RIGHT_BORDER));

                table.AddCell(Header(" ", boldFont, 1, 6));
                var hours = data.CommissionPercentage.AllPeriodHrs == 0
    ? data.CommissionPercentage.SelectedPeriodHrs
    : data.CommissionPercentage.AllPeriodHrs;
                table.AddCell(Header($"{hours:0.#} hrs", boldFont, 1, 2));
                table.AddCell(Header("", boldFont, Element.ALIGN_RIGHT, 2));
                table.AddCell(Header("", boldFont, Element.ALIGN_RIGHT, 2));
                table.AddCell(Header($"Total Sales : {data.CommissionPercentage.Percentage}%", boldFont, Element.ALIGN_RIGHT, 2));
                table.AddCell(Header("Total Commission :", boldFont, Element.ALIGN_RIGHT, 2));
                table.AddCell(Header(FormatMYR(data.CommissionPercentage.TotalCommission), boldFont, Element.ALIGN_RIGHT, 10));
            }
            document.Add(table);

            // =======================
            // INCENTIVE TABLE
            // =======================
            document.Add(Chunk.Newline);
            document.Add(new Paragraph("Incentives", headerFont));

            var incentiveTable = new PdfPTable(new float[]
            {
        1.2f, // Date
        2.7f,   // Description
        3.6f,   // Remark
        2.1f  // Amount
            })
            {
                WidthPercentage = 100
            };

            AddHeader(incentiveTable, headerFont,
                "Date",
                "Description",
                "Remark",
                "Amount");

            foreach (var i in data.Incentives)
            {
                incentiveTable.AddCell(Center(i.IncentiveDate.ToString("dd/MM/yyyy"), dataFont, Rectangle.LEFT_BORDER));
                incentiveTable.AddCell(Left(i.Description, dataFont));
                incentiveTable.AddCell(Left(i.Remark, dataFont));
                incentiveTable.AddCell(Right(FormatMYR(i.Amount), dataFont, Rectangle.RIGHT_BORDER));
            }

            // Totals
            incentiveTable.AddCell(Header("", boldFont, Element.ALIGN_RIGHT, 6));
            incentiveTable.AddCell(Header("", boldFont, Element.ALIGN_RIGHT, 2));
            incentiveTable.AddCell(Header("Total Incentive :", boldFont, Element.ALIGN_RIGHT, 2));
            incentiveTable.AddCell(Header(FormatMYR(data.TotalIncentive), boldFont, Element.ALIGN_RIGHT, 10));


            incentiveTable.AddCell(Header("", boldFont, Element.ALIGN_RIGHT));
            incentiveTable.AddCell(Header("", boldFont, Element.ALIGN_RIGHT));
            incentiveTable.AddCell(Header("Total Payout :", boldFont, Element.ALIGN_RIGHT));
            incentiveTable.AddCell(Header(FormatMYR(data.TotalPayout), boldFont, Element.ALIGN_RIGHT));

            document.Add(incentiveTable);

            document.Close();
            return ms.ToArray();
        }

        public static byte[] GenerateConsultantCommissionReport(
    string therapistName,
    DateTimeOffset? periodStart,
    DateTimeOffset? periodEnd,
    ConsultantCommissionReportDto data)
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
            document.Add(new Paragraph($"Therapist Commission - {therapistName}", titleFont)
            {
                Alignment = Element.ALIGN_CENTER
            });

            var periodStartPlus8 = periodStart.Value.ToOffset(TimeSpan.FromHours(8));           // UTC+8
            var periodEndPlus8 = periodEnd.Value.ToOffset(TimeSpan.FromHours(8));           // UTC+8
            document.Add(new Paragraph(
                $"Period: {periodStartPlus8:dd/MM/yyyy} - {periodEndPlus8:dd/MM/yyyy}",
                dataFont)
            { Alignment = Element.ALIGN_CENTER });

            document.Add(new Paragraph($"Generated on: {DateTime.Now:dd/MM/yyyy HH:mm}", dataFont)
            { Alignment = Element.ALIGN_RIGHT });

            // =======================
            // COMMISSION TABLE
            // =======================

            // Define column widths dynamically
            var columnWidths =
                 new float[] { 1.2f, 1.2f, 2.1f, 2.1f };  // Without Price

            var table = new PdfPTable(columnWidths)
            {
                WidthPercentage = 100
            };

            // Define headers dynamically
            var headers = new[] { "Sales Date", "Menu Code", "Extra Commission", "Price" };

            document.Add(new Paragraph("Product", headerFont));
            // Add header to the table
            AddHeader(table, headerFont, headers);

            DateTime? lastDate = null;

            foreach (var row in data.ProductCommissions)
            {
                if (lastDate != null && lastDate != row.SalesDate.Date)
                {
                    table.AddCell(Center("", dataFont, Rectangle.LEFT_BORDER));
                    table.AddCell(SpacerCell(2));
                    table.AddCell(Center("", dataFont, Rectangle.RIGHT_BORDER));
                }

                lastDate = row.SalesDate.Date;

                table.AddCell(Center(row.SalesDate.ToString("dd/MM/yyyy"), dataFont, Rectangle.LEFT_BORDER));
                table.AddCell(Center(row.MenuCode, dataFont));
                table.AddCell(Right(row.ExtraCommission == 0 ? "" : FormatMYR(row.ExtraCommission), dataFont, 0));
                table.AddCell(Right(row.Price == 0 ? "" : FormatMYR(row.Price), dataFont, Rectangle.RIGHT_BORDER));
            }

            // =======================
            // TOTALS
            // =======================
            table.AddCell(Header("TOTALS", boldFont, 1, Rectangle.LEFT_BORDER));
            table.AddCell(Header("", boldFont));
            table.AddCell(Header(FormatMYR(data.CommissionPercentage.TotalProductExtraCommission), boldFont, Element.ALIGN_RIGHT));
            table.AddCell(Header(FormatMYR(data.CommissionPercentage.TotalProductPrice), boldFont, Element.ALIGN_RIGHT, Rectangle.RIGHT_BORDER));

            table.AddCell(Header(" ", boldFont, 1, 6));
            table.AddCell(Header($"Total Sales : {data.CommissionPercentage.ProductPercentage}%", boldFont, Element.ALIGN_RIGHT, 2));
            table.AddCell(Header("Total Commission :", boldFont, Element.ALIGN_RIGHT, 2));
            table.AddCell(Header(FormatMYR(data.CommissionPercentage.TotalProductCommission), boldFont, Element.ALIGN_RIGHT, 10));

            document.Add(table);

            document.Add(Chunk.Newline);
            document.Add(new Paragraph("Treatment", headerFont)); 
            var table1 = new PdfPTable(columnWidths)
            {
                WidthPercentage = 100
            };
            AddHeader(table1, headerFont, headers);


            foreach (var row in data.TreatmentCommissions)
            {
                if (lastDate != null && lastDate != row.SalesDate.Date)
                {
                    table1.AddCell(Center("", dataFont, Rectangle.LEFT_BORDER));
                    table1.AddCell(SpacerCell(2));
                    table1.AddCell(Center("", dataFont, Rectangle.RIGHT_BORDER));
                }

                lastDate = row.SalesDate.Date;

                table1.AddCell(Center(row.SalesDate.ToString("dd/MM/yyyy"), dataFont, Rectangle.LEFT_BORDER));
                table1.AddCell(Center(row.MenuCode, dataFont));
                table1.AddCell(Right(row.ExtraCommission == 0 ? "" : FormatMYR(row.ExtraCommission), dataFont, 0));
                table1.AddCell(Right(row.Price == 0 ? "" : FormatMYR(row.Price), dataFont, Rectangle.RIGHT_BORDER));
            }

            // =======================
            // TOTALS
            // =======================
            table1.AddCell(Header("TOTALS", boldFont, 1, Rectangle.LEFT_BORDER));
            table1.AddCell(Header("", boldFont));
            table1.AddCell(Header(FormatMYR(data.CommissionPercentage.TotalTreatmentExtraCommission), boldFont, Element.ALIGN_RIGHT));
            table1.AddCell(Header(FormatMYR(data.CommissionPercentage.TotalTreatmentPrice), boldFont, Element.ALIGN_RIGHT, Rectangle.RIGHT_BORDER));

            table1.AddCell(Header(" ", boldFont, 1, 6));
            table1.AddCell(Header($"Total Sales : {data.CommissionPercentage.TreatmentPercentage}%", boldFont, Element.ALIGN_RIGHT, 2));
            table1.AddCell(Header("Total Commission :", boldFont, Element.ALIGN_RIGHT, 2));
            table1.AddCell(Header(FormatMYR(data.CommissionPercentage.TotalTreatmentCommission), boldFont, Element.ALIGN_RIGHT, 10));

            table1.AddCell(Header(" ", boldFont, 1, 0));
            table1.AddCell(Header(" ", boldFont, Element.ALIGN_RIGHT, 0));
            table1.AddCell(Header("Total Payout :", boldFont, Element.ALIGN_RIGHT, 0));
            table1.AddCell(Header(FormatMYR(data.TotalPayout), boldFont, Element.ALIGN_RIGHT, 0));

            document.Add(table1);



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