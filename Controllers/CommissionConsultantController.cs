using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QwenHT.Data;
using QwenHT.Models;
using QwenHT.Utilities;
using System.Text.Json.Serialization;
using static QwenHT.Controllers.SalesKeyInController;

namespace QwenHT.Controllers
{
    [ApiController]
    [Route("api/commission")]
    [Authorize(Policy = "NavigationAccess")] // Use custom policy based on navigation permissions
    public class CommissionConsultantController(ApplicationDbContext _context) : ControllerBase
    {
        [HttpGet("consultant/staff/active")]
        public async Task<ActionResult<IEnumerable<ActiveStaffDto>>> GetActiveStaff()
        {
            var activeStaff = await _context.Staffs
                .Where(s => s.Status == 1 &&
        s.Employments.Any(x => x.Type.ToLower() == "fulltime")) // Only return staff with status = 1
                .Select(s => new ActiveStaffDto
                {
                    Id = s.Id,
                    NickName = s.NickName,
                    FullName = s.FullName
                })
                .OrderBy(s => s.NickName)
                .ToListAsync();

            return Ok(activeStaff);
        }

        /// <summary>
        /// Retrieves therapist commission data grouped by date and menu code for a specified staff member and date range.
        /// Includes detailed compensation calculations based on staff compensation setup (rate-based, percentage-based,
        /// or guarantee income).
        /// </summary>
        /// <param name="staffId">The ID of the staff member to retrieve commission data for</param>
        /// <param name="startDate">Start date for the data retrieval period (optional)</param>
        /// <param name="endDate">End date for the data retrieval period (optional)</param>
        /// <returns>Comprehensive therapist commission report with all calculated values</returns>
        [HttpGet("consultant/pdf")]
        public async Task<IActionResult> GetConsultantCommissionReport(
            [FromQuery] Guid staffId,
            [FromQuery] DateTimeOffset? startDate = null,
            [FromQuery] DateTimeOffset? endDate = null)
        {
            // Get the current user for CreatedBy field
            var currentUser = User?.Identity?.Name ?? "System";
            // Step 1: Fetch all required sales data in a single query for efficiency
            var salesData = await GetSalesDataAsync(staffId, startDate, endDate);

            // Step 2: Group sales by date and menu code to create daily summaries
            var groupedTreatmentCommissions = GroupSalesByDateAndMenu(salesData, true);
            var groupedProductCommissions = GroupSalesByDateAndMenu(salesData, false);

            // Step 3: Initialize the main report object with basic commission data
            var result = new ConsultantCommissionReportDto
            {
                TreatmentCommissions = groupedTreatmentCommissions,
                ProductCommissions = groupedProductCommissions
            };


            var staff = await _context.Staffs
                .FirstOrDefaultAsync(x => x.Id == staffId);


            result.CommissionPercentage = new ConsultantPercentageBasedCommissionDto()
            {
                TotalProductExtraCommission = result.ProductCommissions.Sum(x => x.ExtraCommission),
                TotalProductPrice = result.ProductCommissions.Sum(x => x.Price),

                TotalTreatmentExtraCommission = result.TreatmentCommissions.Sum(x => x.ExtraCommission),
                TotalTreatmentPrice = result.TreatmentCommissions.Sum(x => x.Price),
            };

            var query = _context.OptionValues.AsQueryable();


            List<string> strings = new List<string>() {
"TREATMENT_PERCENT",
"PRODUCT_PERCENT_TIER_1",
"PRODUCT_PERCENT_TIER_2",
"PRODUCT_TARGET" };

            query = query.Where(ov => strings.Contains(ov.Category));
            var optionValues = await query.ToListAsync();

            decimal.TryParse(optionValues.Where(x => x.Category == "TREATMENT_PERCENT").First().Value, out decimal _treatmentPercentage);
            result.CommissionPercentage.TreatmentPercentage = _treatmentPercentage;

            decimal.TryParse(optionValues.Where(x => x.Category == "PRODUCT_TARGET").First().Value, out decimal _productTarget);

            if (result.CommissionPercentage.TotalProductPrice >= _productTarget)
            {
                decimal.TryParse(optionValues.Where(x => x.Category == "PRODUCT_PERCENT_TIER_2").First().Value, out decimal PRODUCT_PERCENT_TIER_2);
                result.CommissionPercentage.ProductPercentage = PRODUCT_PERCENT_TIER_2;
            }
            else
            {
                decimal.TryParse(optionValues.Where(x => x.Category == "PRODUCT_PERCENT_TIER_1").First().Value, out decimal PRODUCT_PERCENT_TIER_1);
                result.CommissionPercentage.ProductPercentage = PRODUCT_PERCENT_TIER_1;
            }

            result.CommissionPercentage.TotalProductCommission = Math.Round((result.CommissionPercentage.TotalProductPrice * (result.CommissionPercentage.ProductPercentage / 100)) + result.CommissionPercentage.TotalProductExtraCommission, 2);
            result.CommissionPercentage.TotalTreatmentCommission = Math.Round((result.CommissionPercentage.TotalTreatmentPrice * (result.CommissionPercentage.TreatmentPercentage / 100)) + result.CommissionPercentage.TotalTreatmentExtraCommission, 2);

            result.TotalPayout = Math.Round(result.CommissionPercentage.TotalProductCommission + result.CommissionPercentage.TotalTreatmentCommission, 2);
            // Generate PDF
            var pdfBytes = PdfGenerator.GenerateConsultantCommissionReport(
                staff.NickName ?? staff.FullName,
                startDate, // Default to 30 days if not provided
                endDate,
                result);

            // Return PDF as file
            var fileName = $"Consultant-Commission-{(staff.NickName ?? staff.FullName).Replace(" ", "_")}-{(startDate?.ToString("yyyyMMdd") ?? DateTime.Now.ToString("yyyyMMdd"))}-{(endDate?.ToString("yyyyMMdd") ?? DateTime.Now.ToString("yyyyMMdd"))}.pdf";
            return File(pdfBytes, "application/pdf", fileName);
        }

        [HttpGet("consultant/insertPayout")]
        public async Task<IActionResult> insertPayout(
            [FromQuery] Guid staffId,
            [FromQuery] DateTimeOffset? startDate = null,
            [FromQuery] DateTimeOffset? endDate = null)
        {
            var username = User?.Claims?.FirstOrDefault(c => c.Type == "username")?.Value ?? "Unknown";
            // Get the current user for CreatedBy field
            var currentUser = User?.Identity?.Name ?? "System";
            // Step 1: Fetch all required sales data in a single query for efficiency
            var salesData = await GetSalesDataAsync(staffId, startDate, endDate);

            // Step 2: Group sales by date and menu code to create daily summaries
            var groupedTreatmentCommissions = GroupSalesByDateAndMenu(salesData, true);
            var groupedProductCommissions = GroupSalesByDateAndMenu(salesData, false);

            // Step 3: Initialize the main report object with basic commission data
            var result = new ConsultantCommissionReportDto
            {
                TreatmentCommissions = groupedTreatmentCommissions,
                ProductCommissions = groupedProductCommissions
            };


            var staff = await _context.Staffs
                .FirstOrDefaultAsync(x => x.Id == staffId);


            result.CommissionPercentage = new ConsultantPercentageBasedCommissionDto()
            {
                TotalProductExtraCommission = result.ProductCommissions.Sum(x => x.ExtraCommission),
                TotalProductPrice = result.ProductCommissions.Sum(x => x.Price),

                TotalTreatmentExtraCommission = result.TreatmentCommissions.Sum(x => x.ExtraCommission),
                TotalTreatmentPrice = result.TreatmentCommissions.Sum(x => x.Price),
            };

            var query = _context.OptionValues.AsQueryable();


            List<string> strings = new List<string>() {
"TREATMENT_PERCENT",
"PRODUCT_PERCENT_TIER_1",
"PRODUCT_PERCENT_TIER_2",
"PRODUCT_TARGET" };

            query = query.Where(ov => strings.Contains(ov.Category));
            var optionValues = await query.ToListAsync();

            decimal.TryParse(optionValues.Where(x => x.Category == "TREATMENT_PERCENT").First().Value, out decimal _treatmentPercentage);
            result.CommissionPercentage.TreatmentPercentage = _treatmentPercentage;

            decimal.TryParse(optionValues.Where(x => x.Category == "PRODUCT_TARGET").First().Value, out decimal _productTarget);

            if (result.CommissionPercentage.TotalProductPrice >= _productTarget)
            {
                decimal.TryParse(optionValues.Where(x => x.Category == "PRODUCT_PERCENT_TIER_2").First().Value, out decimal PRODUCT_PERCENT_TIER_2);
                result.CommissionPercentage.ProductPercentage = PRODUCT_PERCENT_TIER_2;
            }
            else
            {
                decimal.TryParse(optionValues.Where(x => x.Category == "PRODUCT_PERCENT_TIER_1").First().Value, out decimal PRODUCT_PERCENT_TIER_1);
                result.CommissionPercentage.ProductPercentage = PRODUCT_PERCENT_TIER_1;
            }

            result.CommissionPercentage.TotalProductCommission = Math.Round((result.CommissionPercentage.TotalProductPrice * (result.CommissionPercentage.ProductPercentage / 100)) + result.CommissionPercentage.TotalProductExtraCommission, 2);
            result.CommissionPercentage.TotalTreatmentCommission = Math.Round((result.CommissionPercentage.TotalTreatmentPrice * (result.CommissionPercentage.TreatmentPercentage / 100)) + result.CommissionPercentage.TotalTreatmentExtraCommission, 2);

            result.TotalPayout = Math.Round(result.CommissionPercentage.TotalProductCommission + result.CommissionPercentage.TotalTreatmentCommission, 2);

            var existings = await _context.ConsultantPayouts.Where(x => x.StaffId == staffId && x.PayoutDate == endDate && x.Status == 1).ToListAsync();
            if (existings.Count > 0)
            {
                foreach (var item in existings)
                {
                    item.Status = 0;
                }
                _context.ConsultantPayouts.UpdateRange(existings);
                await _context.SaveChangesAsync();
            }

            var consultantPayout = new ConsultantPayout
            {
                Id = Guid.NewGuid(),
                PayoutDate = (DateTimeOffset)endDate,
                StaffId = staffId,
                ProductAmount = result.CommissionPercentage.TotalProductCommission,
                TreatmentAmount = result.CommissionPercentage.TotalTreatmentCommission,
                TotalAmount = result.TotalPayout,
                Status = 1,
                CreatedBy = username,
                CreatedAt = DateTimeOffset.UtcNow,
                LastUpdated = DateTimeOffset.UtcNow,
                LastModifiedBy = username
            };
            await _context.ConsultantPayouts.AddAsync(consultantPayout);
            await _context.SaveChangesAsync();

            return Ok();
        }

        [HttpGet("consultant")]
        public async Task<ActionResult<ConsultantCommissionReportDto>> GetConsultantCommission(
           [FromQuery] Guid staffId,
           [FromQuery] DateTimeOffset? startDate = null,
           [FromQuery] DateTimeOffset? endDate = null,
           [FromQuery] bool? incentive = false)
        {
            // Get the current user for CreatedBy field
            var currentUser = User?.Identity?.Name ?? "System";
            // Step 1: Fetch all required sales data in a single query for efficiency
            var salesData = await GetSalesDataAsync(staffId, startDate, endDate);

            // Step 2: Group sales by date and menu code to create daily summaries
            var groupedTreatmentCommissions = GroupSalesByDateAndMenu(salesData, true);
            var groupedProductCommissions = GroupSalesByDateAndMenu(salesData, false);

            // Step 3: Initialize the main report object with basic commission data
            var result = new ConsultantCommissionReportDto
            {
                TreatmentCommissions = groupedTreatmentCommissions,
                ProductCommissions = groupedProductCommissions
            };


            var staff = await _context.Staffs
                .FirstOrDefaultAsync(x => x.Id == staffId);


            result.CommissionPercentage = new ConsultantPercentageBasedCommissionDto()
            {
                TotalProductExtraCommission = result.ProductCommissions.Sum(x => x.ExtraCommission),
                TotalProductPrice = result.ProductCommissions.Sum(x => x.Price),

                TotalTreatmentExtraCommission = result.TreatmentCommissions.Sum(x => x.ExtraCommission),
                TotalTreatmentPrice = result.TreatmentCommissions.Sum(x => x.Price),
            };

            var query = _context.OptionValues.AsQueryable();


            List<string> strings = new List<string>() {
"TREATMENT_PERCENT",
"PRODUCT_PERCENT_TIER_1",
"PRODUCT_PERCENT_TIER_2",
"PRODUCT_TARGET" };

            query = query.Where(ov => strings.Contains(ov.Category));
            var optionValues = await query.ToListAsync();

            decimal.TryParse(optionValues.Where(x => x.Category == "TREATMENT_PERCENT").First().Value, out decimal _treatmentPercentage);
            result.CommissionPercentage.TreatmentPercentage = _treatmentPercentage;

            decimal.TryParse(optionValues.Where(x => x.Category == "PRODUCT_TARGET").First().Value, out decimal _productTarget);

            if (result.CommissionPercentage.TotalProductPrice >= _productTarget)
            {
                decimal.TryParse(optionValues.Where(x => x.Category == "PRODUCT_PERCENT_TIER_2").First().Value, out decimal PRODUCT_PERCENT_TIER_2);
                result.CommissionPercentage.ProductPercentage = PRODUCT_PERCENT_TIER_2;
            }
            else
            {
                decimal.TryParse(optionValues.Where(x => x.Category == "PRODUCT_PERCENT_TIER_1").First().Value, out decimal PRODUCT_PERCENT_TIER_1);
                result.CommissionPercentage.ProductPercentage = PRODUCT_PERCENT_TIER_1;
            }

            result.CommissionPercentage.TotalProductCommission = Math.Round((result.CommissionPercentage.TotalProductPrice * (result.CommissionPercentage.ProductPercentage / 100)) + result.CommissionPercentage.TotalProductExtraCommission, 2);
            result.CommissionPercentage.TotalTreatmentCommission = Math.Round((result.CommissionPercentage.TotalTreatmentPrice * (result.CommissionPercentage.TreatmentPercentage / 100)) + result.CommissionPercentage.TotalTreatmentExtraCommission, 2);

            result.TotalPayout = Math.Round(result.CommissionPercentage.TotalProductCommission + result.CommissionPercentage.TotalTreatmentCommission, 2);
            return Ok(result);
        }

        private async Task<List<DailyConsultantCommissionSummaryDto>> GetSalesDataAsync(Guid staffId, DateTimeOffset? startDate, DateTimeOffset? endDate)
        {
            var query = _context.Sales
           .Include(s => s.Menu)
           .Where(s => s.StaffId == staffId && s.Status == 1) // Filter by staff and active sales
           .AsQueryable();

            if (startDate.HasValue)
                query = query.Where(s => s.SalesDate >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(s => s.SalesDate <= endDate.Value); // Use <= since we normalized to date

            var data = await query
                .Select(s => new DailyConsultantCommissionSummaryDto
                {
                    SalesDate = s.SalesDate,
                    MenuCode = s.Menu.Code,
                    ExtraCommission = s.ExtraCommission,
                    isTreatment = s.Menu.Category.ToLower() == "treatment",
                    Price = s.StaffCommission != 0 || s.ExtraCommission != 0 ? 0 : s.Price
                })
                .ToListAsync();

            return data;
        }


        private List<DailyConsultantCommissionSummaryDto> GroupSalesByDateAndMenu(List<DailyConsultantCommissionSummaryDto> rawData, bool isTreatment)
        {
            return rawData
                .Where(x => x.isTreatment == isTreatment)
                .GroupBy(s => new { Date = s.SalesDate.Date, Code = s.MenuCode })
                .Select(g => new DailyConsultantCommissionSummaryDto
                {
                    Id = Guid.NewGuid().ToString(),
                    SalesDate = g.Key.Date,
                    MenuCode = g.Key.Code,
                    ExtraCommission = g.Sum(s => s.ExtraCommission),
                    Price = g.Sum(s => s.Price)
                })
                .OrderBy(s => s.SalesDate)
                .ToList();
        }

    }


    public class DailyConsultantCommissionSummaryDto
    {
        public string Id { get; set; } = string.Empty;
        public DateTimeOffset SalesDate { get; set; }
        public bool isTreatment { get; set; }
        public string MenuCode { get; set; } = string.Empty;
        public decimal ExtraCommission { get; set; }
        public decimal Price { get; set; }
    }

    public class ConsultantCommissionReportDto
    {
        public List<DailyConsultantCommissionSummaryDto> ProductCommissions { get; set; }
        public List<DailyConsultantCommissionSummaryDto> TreatmentCommissions { get; set; }
        public ConsultantPercentageBasedCommissionDto CommissionPercentage { get; set; }
        public decimal TotalPayout { get; set; }

    }

    public class ConsultantPercentageBasedCommissionDto
    {
        public decimal TotalProductExtraCommission { get; set; }
        public decimal TotalProductPrice { get; set; }
        public decimal TotalProductCommission { get; set; }
        public decimal ProductPercentage { get; set; }
        public decimal TotalTreatmentExtraCommission { get; set; }
        public decimal TotalTreatmentPrice { get; set; }
        public decimal TotalTreatmentCommission { get; set; }
        public decimal TreatmentPercentage { get; set; }
    }
}