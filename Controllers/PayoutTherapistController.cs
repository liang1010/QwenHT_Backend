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
    [Route("api/payout")]
    [Authorize(Policy = "NavigationAccess")] // Use custom policy based on navigation permissions
    public class PayoutTherapistController(ApplicationDbContext _context) : ControllerBase
    {




        /// <summary>
        /// Retrieves therapist commission data grouped by date and menu code for a specified staff member and date range.
        /// Includes detailed compensation calculations based on staff compensation setup (rate-based, percentage-based,
        /// or guarantee income).
        /// </summary>
        /// <param name="staffId">The ID of the staff member to retrieve commission data for</param>
        /// <param name="startDate">Start date for the data retrieval period (optional)</param>
        /// <param name="endDate">End date for the data retrieval period (optional)</param>
        /// <returns>Comprehensive therapist commission report with all calculated values</returns>
        [HttpGet("therapist")]
        public async Task<ActionResult<List<TherapistPayoutReportDto>>> GetTherapistCommission(
            [FromQuery] DateTimeOffset? startDate = null,
            [FromQuery] DateTimeOffset? endDate = null,
            [FromQuery] bool? incentive = false)
        {
            // Get the current user for CreatedBy field
            var currentUser = User?.Identity?.Name ?? "System";

            List<TherapistPayoutReportDto> response = new List<TherapistPayoutReportDto>();

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
                .Where(s => s.Employments.Any(emp => emp.Type == "therapist")) // optional
                .Select(s => new
                {
                    s.Id,
                    s.NickName,
                    s.FullName,
                    s.Nationality
                })
                .Distinct()
                .ToList();

            foreach (var staff in staffs)
            {

                var staffBank = await _context.BankAccounts
                    .FirstOrDefaultAsync(x => x.StaffId == staff.Id);

                var staffEmp = await _context.StaffEmployments
                    .FirstOrDefaultAsync(x => x.StaffId == staff.Id);

                var payout = await _context.TherapistPayouts.FirstOrDefaultAsync(x => x.StaffId == staff.Id && x.PayoutDate == endDate && x.Status == 1);

                if (payout != null)
                {
                    // Step 3: Initialize the main report object with basic commission data
                    var result = new TherapistPayoutReportDto
                    {
                        NickName = staff.NickName,
                        FullName = staff.FullName,
                        BankName = staffBank.BankName,
                        BankAccName = staffBank.AccountHolderName,
                        BankAccNo = staffBank.AccountNumber,
                        Nationality = staff.Nationality,
                        Outlet = staffEmp.Outlet,

                        TotalBodyCommission = payout.BodyAmount,
                        TotalExtraCommission = payout.ExtraAmount,
                        TotalFootCommission = payout.FootAmount,
                        TotalIncentive = payout.IncentiveAmount,
                        TotalPayout = payout.TotalAmount,
                        TotalStaffCommission = payout.StaffAmount,

                    };
                    response.Add(result);
                }



            }

            response = response.OrderBy(x => x.NickName).ToList();
            return Ok(response);
        }
        private async Task<List<TherapistSalesCommissionDto>> GetSalesDataAsync(Guid staffId, DateTimeOffset? startDate, DateTimeOffset? endDate)
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
                .Select(s => new TherapistSalesCommissionDto
                {
                    SalesDate = s.SalesDate,
                    MenuCode = s.Menu.Code,
                    FootMins = s.Menu.FootMins,
                    BodyMins = s.Menu.BodyMins,
                    StaffCommission = s.StaffCommission,
                    ExtraCommission = s.ExtraCommission,
                    Price = s.StaffCommission != 0 || s.ExtraCommission != 0 ? 0 : s.Price
                })
                .ToListAsync();

            return data;
        }
        private TherapistRateBasedCommissionDto GetTherapistRateBasedCommissionDto(
    List<DailyTherapistCommissionSummaryDto> commissions,
    StaffCompensation comp,
    DateTimeOffset? startDate)
        {
            var response = new TherapistRateBasedCommissionDto
            {
                SelectedPeriodHrs = Math.Round((commissions.Sum(x => x.FootMins) + commissions.Sum(x => x.BodyMins)) / 60.00, 2),
                TotalFootMins = commissions.Sum(x => x.FootMins),
                TotalBodyMins = commissions.Sum(x => x.BodyMins),
                TotalStaffCommission = commissions.Sum(x => x.StaffCommission),
                TotalExtraCommission = commissions.Sum(x => x.ExtraCommission)
            };

            response.TotalFootCommission = Math.Round(comp.FootRatePerHour * (response.TotalFootMins / 60m), 2);
            response.TotalBodyCommission = Math.Round(comp.BodyRatePerHour * (response.TotalBodyMins / 60m), 2); // Fixed: was FootRate!
            response.TotalCommission = response.TotalFootCommission + response.TotalBodyCommission
                                       + response.TotalStaffCommission + response.TotalExtraCommission;

            return response;
        }

        private TherapistPercentageBasedCommissionDto GetTherapistPercentageBasedCommissionDto(
            List<DailyTherapistCommissionSummaryDto> commissions,
            StaffCompensation comp,
            DateTimeOffset? startDate)
        {
            var response = new TherapistPercentageBasedCommissionDto
            {
                SelectedPeriodHrs = Math.Round((commissions.Sum(x => x.FootMins) + commissions.Sum(x => x.BodyMins)) / 60.00, 2),
                TotalStaffCommission = commissions.Sum(x => x.StaffCommission),
                TotalExtraCommission = commissions.Sum(x => x.ExtraCommission),
                TotalPrice = commissions.Sum(x => x.Price),
                Percentage = comp.CommissionBasePercentage
            };

            response.TotalCommission = (response.TotalPrice * (comp.CommissionBasePercentage / 100m))
                                       + response.TotalStaffCommission + response.TotalExtraCommission; ;
            return response;
        }
        private List<DailyTherapistCommissionSummaryDto> GroupSalesByDateAndMenu(List<TherapistSalesCommissionDto> rawData)
        {
            return rawData
                .GroupBy(s => new { Date = s.SalesDate.Date, Code = s.MenuCode })
                .Select(g => new DailyTherapistCommissionSummaryDto
                {
                    Id = Guid.NewGuid().ToString(),
                    SalesDate = g.Key.Date,
                    MenuCode = g.Key.Code,
                    FootMins = g.Sum(s => s.FootMins),
                    BodyMins = g.Sum(s => s.BodyMins),
                    StaffCommission = g.Sum(s => s.StaffCommission),
                    ExtraCommission = g.Sum(s => s.ExtraCommission),
                    Price = g.Sum(s => s.Price)
                })
                .OrderBy(s => s.SalesDate)
                .ToList();
        }

    }


    public class TherapistPayoutReportDto
    {

        public decimal TotalStaffCommission { get; set; }
        public decimal TotalExtraCommission { get; set; }
        public decimal TotalBodyCommission { get; set; }
        public decimal TotalFootCommission { get; set; }
        public decimal TotalIncentive { get; set; }
        public decimal TotalPayout { get; set; }

        public string Nationality { get; set; }
        public string Outlet { get; set; }
        public string NickName { get; set; }
        public string FullName { get; set; }
        public string BankName { get; set; }
        public string BankAccNo { get; set; }
        public string BankAccName { get; set; }

    }
}