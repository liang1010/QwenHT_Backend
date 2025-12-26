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


            // 1. Staff IDs from Sales (in date range, Status = 1)
            var salesStaffIds = _context.Sales
                .Where(sale => sale.Status == 1 &&
                               sale.SalesDate >= startDate &&
                               sale.SalesDate <= endDate)
                .Select(sale => sale.StaffId);


            var incentiveStaffIds = _context.Incentives
                .Where(inc => inc.Status == 1 &&
                              inc.IncentiveDate == endDate)
                .Select(inc => inc.StaffId);

            // 3. Combine IDs (OR logic) and remove duplicates
            var staffIds = salesStaffIds
                .Union(incentiveStaffIds)
                .ToList(); // Materialize ID list

            // 4. Fetch staff info (with optional therapist filter)
            var staffs = _context.Staff
                .AsNoTracking()
                .Where(s => staffIds.Contains(s.Id))
                .Where(s => s.Employments.Any(emp => emp.Type == "fulltime")) // optional
                .Select(s => new
                {
                    s.Id,
                    s.NickName,
                    s.FullName,
                    s.Nationality
                })
                .Distinct()
                .ToList();

            List<string> excludeName = new List<string>() { "HTSA", "HTL", "HTG", "FSPA" };
            foreach (var staff in staffs)
            {
                if (excludeName.Contains(staff.NickName))
                {
                    continue;
                }

                var payout = await _context.ConsultantPayouts.FirstOrDefaultAsync(x => x.StaffId == staff.Id && x.PayoutDate == endDate && x.Status == 1);

                if (payout != null)
                {
                    // Step 3: Initialize the main report object with basic commission data
                    var result = new ConsultantPayoutReportDto
                    {
                        FullName = staff.FullName,
                        NickName = staff.NickName,
                        ProductAmount = payout.ProductAmount,
                        TreatmentAmount = payout.TreatmentAmount,
                        TotalPayout = payout.TotalAmount,

                    };


                    response.Add(result);
                }

            }
            response = response.OrderBy(x => x.NickName).ToList();
            return Ok(response);
        }


    }

}

public class ConsultantPayoutReportDto
{
    public decimal ProductAmount { get; set; }
    public decimal TreatmentAmount { get; set; }
    public decimal TotalPayout { get; set; }
    public string NickName { get; set; }
    public string FullName { get; set; }

}