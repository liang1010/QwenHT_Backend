using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QwenHT.Controllers;
using QwenHT.Data;
using QwenHT.Models;
using QwenHT.Utilities;
using System.Text.Json.Serialization;
using static QwenHT.Controllers.SalesKeyInController;

namespace QwenHT.Controllers
{
    [ApiController]
    [Route("api/payout")]
    [Authorize(Policy = "NavigationAccess")] // Use custom policy based on navigation permissions
    public class PayoutConsultantController(ApplicationDbContext _context) : ControllerBase
    {
        [HttpGet("consultant")]
        public async Task<ActionResult<List<ConsultantPayoutReportDto>>> GetConsultantCommission(
           [FromQuery] DateTimeOffset? startDate = null,
           [FromQuery] DateTimeOffset? endDate = null,
           [FromQuery] bool? incentive = false)
        {
            // Get the current user for CreatedBy field
            var currentUser = User?.Identity?.Name ?? "System";
            List<ConsultantPayoutReportDto> response = new List<ConsultantPayoutReportDto>();


            var staffs = _context.Staff.AsNoTracking()
    .Where(s => s.Employments.Any(emp => emp.Type == "fulltime"))
    .Where(s => s.SalesRecords.Any(sale =>
        sale.Status == 1 &&
        sale.SalesDate >= startDate &&
        sale.SalesDate <= endDate))
    .Select(s => new { id = s.Id, NickName = s.NickName, FullName = s.FullName })
    .Distinct()
    .ToList();

            List<string> excludeName = new List<string>() { "HTSA","HTL","HTG","FSPA"};
            foreach (var staff in staffs)
            {
                if (excludeName.Contains(staff.NickName))
                {
                    continue;
                }

                // Step 1: Fetch all required sales data in a single query for efficiency
                var salesData = await GetSalesDataAsync(staff.id, startDate, endDate);

                // Step 2: Group sales by date and menu code to create daily summaries
                var groupedTreatmentCommissions = GroupSalesByDateAndMenu(salesData, true);
                var groupedProductCommissions = GroupSalesByDateAndMenu(salesData, false);

                // Step 3: Initialize the main report object with basic commission data
                var result = new ConsultantPayoutReportDto
                {
                    TreatmentCommissions = groupedTreatmentCommissions,
                    ProductCommissions = groupedProductCommissions
                };

                result.NickName = staff.NickName;
                result.FullName = staff.FullName;

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
                response.Add(result);

            }
            response = response.OrderBy(x=>x.NickName).ToList();
            return Ok(response);
        }

        private async Task<List<ConsultantSalesCommissionDto>> GetSalesDataAsync(Guid staffId, DateTimeOffset? startDate, DateTimeOffset? endDate)
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
                .Select(s => new ConsultantSalesCommissionDto
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


        private List<DailyConsultantPayoutSummaryDto> GroupSalesByDateAndMenu(List<ConsultantSalesCommissionDto> rawData, bool isTreatment)
        {
            return rawData
                .Where(x => x.isTreatment == isTreatment)
                .GroupBy(s => new { Date = s.SalesDate.Date, Code = s.MenuCode })
                .Select(g => new DailyConsultantPayoutSummaryDto
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

}
public class DailyConsultantPayoutSummaryDto
{
    public string Id { get; set; } = string.Empty;
    public DateTimeOffset SalesDate { get; set; }
    public string MenuCode { get; set; } = string.Empty;
    public decimal ExtraCommission { get; set; }
    public decimal Price { get; set; }
}

public class ConsultantPayoutReportDto
{
    [JsonIgnore]
    public List<DailyConsultantPayoutSummaryDto> ProductCommissions { get; set; }
    [JsonIgnore]
    public List<DailyConsultantPayoutSummaryDto> TreatmentCommissions { get; set; }
    public ConsultantPercentageBasedCommissionDto CommissionPercentage { get; set; }
    public decimal TotalPayout { get; set; }
    public string NickName { get; set; }
    public string FullName { get; set; }

}