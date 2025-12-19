using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QwenHT.Data;
using QwenHT.Models;
using QwenHT.Utilities;
using static QwenHT.Controllers.SalesKeyInController;

namespace QwenHT.Controllers
{
    [ApiController]
    [Route("api/commission")]
    [Authorize(Policy = "NavigationAccess")] // Use custom policy based on navigation permissions
    public class CommissionTherapistController(ApplicationDbContext _context) : ControllerBase
    {

        [HttpGet("therapist/staff/active")]
        public async Task<ActionResult<IEnumerable<ActiveStaffDto>>> GetActiveStaff()
        {
            var activeStaff = await _context.Staff
                .Where(s => s.Status == 1) // Only return staff with status = 1
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

        // GET api/commission/therapist - Retrieve therapist commission data (grouped by date and menu code)
        [HttpGet("therapist")]
        public async Task<ActionResult<FullTherapistCommissionDto>> GetTherapistCommission(
            [FromQuery] Guid staffId,
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null)
        {
            // Fetch all required sales data in **one query**
            var salesData = await GetSalesDataAsync(staffId, startDate, endDate);

            // Group main result
            var groupedCommissions = GroupSalesByDateAndMenu(salesData);

            var result = new FullTherapistCommissionDto
            {
                Commissions = groupedCommissions
            };

            var staffCompensation = await _context.StaffCompensations
                .FirstOrDefaultAsync(x => x.StaffId == staffId);

            if (staffCompensation == null)
                return Ok(result);

            // Set common flags
            result.IsGuaranteeIncome = staffCompensation.IsGuaranteeIncome;
            result.IsRate = staffCompensation.IsRate;
            result.IsCommissionPercentage = staffCompensation.IsCommissionPercentage;

            // Only calculate extra details if needed
            if (staffCompensation.IsRate)
            {
                result.RateBase = GetRateBaseDto(groupedCommissions, staffCompensation, startDate);

                if (startDate.HasValue && startDate.Value.Day != 1)
                {
                    // Fetch full month data for guarantee calculation
                    var startOfMonth = new DateTime(startDate.Value.Year, startDate.Value.Month, 1);
                    var endOfMonth = startOfMonth.AddMonths(1).AddDays(-1); // Last day of month
                    var fullMonthSales = await GetSalesDataAsync(staffId, startOfMonth, endOfMonth);
                    var fullMonthGrouped = GroupSalesByDateAndMenu(fullMonthSales);
                    result.RateBase.AllPeriodHrs = (fullMonthGrouped.Sum(x => x.FootMins) + fullMonthGrouped.Sum(x => x.BodyMins)) / 60.00;
                }
            }
            else if (staffCompensation.IsCommissionPercentage)
            {
                result.CommissionPercentage = GetCommissionPercentageDto(groupedCommissions, staffCompensation, startDate);

                if (startDate.HasValue && startDate.Value.Day != 1)
                {
                    // Fetch full month data for guarantee calculation
                    var startOfMonth = new DateTime(startDate.Value.Year, startDate.Value.Month, 1);
                    var endOfMonth = startOfMonth.AddMonths(1).AddDays(-1); // Last day of month
                    var fullMonthSales = await GetSalesDataAsync(staffId, startOfMonth, endOfMonth);
                    var fullMonthGrouped = GroupSalesByDateAndMenu(fullMonthSales);
                    result.CommissionPercentage.AllPeriodHrs = (fullMonthGrouped.Sum(x => x.FootMins) + fullMonthGrouped.Sum(x => x.BodyMins)) / 60.00;
                }
            }

            // Guarantee income logic (only if start date is not 1st and guarantee is enabled)
            if (staffCompensation.IsGuaranteeIncome && startDate.HasValue && startDate.Value.Day != 1)
            {
                // Fetch full month data for guarantee calculation
                var startOfMonth = new DateTime(startDate.Value.Year, startDate.Value.Month, 1);
                var endOfMonth = startOfMonth.AddMonths(1).AddDays(-1); // Last day of month

                var fullMonthSales = await GetSalesDataAsync(staffId, startOfMonth, endOfMonth);
                var fullMonthGrouped = GroupSalesByDateAndMenu(fullMonthSales);

                result.GuaranteeIncome = CalculateGuaranteeIncome(fullMonthGrouped, staffCompensation, endDate ?? endOfMonth);
            }

            return Ok(result);
        }

        private async Task<List<SalesCommissionDto>> GetSalesDataAsync(Guid staffId, DateTime? startDate, DateTime? endDate)
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
                .Select(s => new SalesCommissionDto
                {
                    SalesDate = s.SalesDate,
                    MenuCode = s.Menu.Code,
                    FootMins = s.Menu.FootMins,
                    BodyMins = s.Menu.BodyMins,
                    StaffCommission = s.StaffCommission,
                    ExtraCommission = s.ExtraCommission,
                    Price = s.Price
                })
                .ToListAsync();

            return data;
        }
        private RateBaseDto GetRateBaseDto(
    List<TherapistCommissionDto> commissions,
    StaffCompensation comp,
    DateTime? startDate)
        {
            var response = new RateBaseDto
            {
                SelectedPeriodHrs = (commissions.Sum(x => x.FootMins) + commissions.Sum(x => x.BodyMins)) / 60.00,
                TotalFootMins = commissions.Sum(x => x.FootMins),
                TotalBodyMins = commissions.Sum(x => x.BodyMins),
                TotalStaffCommission = commissions.Sum(x => x.StaffCommission),
                TotalExtraCommission = commissions.Sum(x => x.ExtraCommission)
            };

            response.TotalFootCommission = comp.FootRatePerHour * (response.TotalFootMins / 60m);
            response.TotalBodyCommission = comp.BodyRatePerHour * (response.TotalBodyMins / 60m); // Fixed: was FootRate!
            response.TotalCommission = response.TotalFootCommission + response.TotalBodyCommission
                                       + response.TotalStaffCommission + response.TotalExtraCommission;

            return response;
        }

        private CommissionPercentageDto GetCommissionPercentageDto(
            List<TherapistCommissionDto> commissions,
            StaffCompensation comp,
            DateTime? startDate)
        {
            var response = new CommissionPercentageDto
            {
                SelectedPeriodHrs = (commissions.Sum(x => x.FootMins) + commissions.Sum(x => x.BodyMins)) / 60.00,
                TotalStaffCommission = commissions.Sum(x => x.StaffCommission),
                TotalExtraCommission = commissions.Sum(x => x.ExtraCommission),
                TotalPrice = commissions.Sum(x => x.Price)
            };

            response.TotalCommission = response.TotalPrice * (comp.CommissionBasePercentage / 100m);
            return response;
        }

        private GuaranteeIncomeDto CalculateGuaranteeIncome(
    List<TherapistCommissionDto> fullMonthData,
    StaffCompensation comp,
    DateTime endDate)
        {
            var endOfMonth = new DateTime(endDate.Year, endDate.Month, DateTime.DaysInMonth(endDate.Year, endDate.Month));
            var midDate = new DateTime(endDate.Year, endDate.Month, 15);

            var firstHalf = fullMonthData.Where(x => x.SalesDate <= midDate).ToList();
            var secondHalf = fullMonthData.Where(x => x.SalesDate > midDate && x.SalesDate <= endDate).ToList();

            decimal firstCommission, secondCommission;

            if (comp.IsRate)
            {
                var first = GetRateBaseDto(firstHalf, comp, null);
                var second = GetRateBaseDto(secondHalf, comp, null);
                firstCommission = first.TotalCommission;
                secondCommission = second.TotalCommission;
            }
            else if (comp.IsCommissionPercentage)
            {
                var first = GetCommissionPercentageDto(firstHalf, comp, null);
                var second = GetCommissionPercentageDto(secondHalf, comp, null);
                firstCommission = first.TotalCommission;
                secondCommission = second.TotalCommission;
            }
            else
            {
                firstCommission = firstHalf.Sum(x => x.StaffCommission + x.ExtraCommission);
                secondCommission = secondHalf.Sum(x => x.StaffCommission + x.ExtraCommission);
            }

            var total = firstCommission + secondCommission;
            var guaranteePaid = comp.GuaranteeIncome > total ? comp.GuaranteeIncome - total : 0;

            return new GuaranteeIncomeDto
            {
                FirstPeriodCommission = firstCommission,
                SecondPeriodCommission = secondCommission,
                TotalCommission = total,
                GuaranteeIncomePaid = guaranteePaid
            };
        }
        private List<TherapistCommissionDto> GroupSalesByDateAndMenu(List<SalesCommissionDto> rawData)
        {
            return rawData
                .GroupBy(s => new { Date = s.SalesDate.Date, Code = s.MenuCode })
                .Select(g => new TherapistCommissionDto
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

    public class SalesCommissionDto
    {
        public DateTimeOffset SalesDate { get; set; }
        public string MenuCode { get; set; }
        public int FootMins { get; set; }
        public int BodyMins { get; set; }
        public decimal StaffCommission { get; set; }
        public decimal ExtraCommission { get; set; }
        public decimal Price { get; set; }
    }

    public class TherapistCommissionDto
    {
        public string Id { get; set; } = string.Empty;
        public DateTimeOffset SalesDate { get; set; }
        public string MenuCode { get; set; } = string.Empty;
        public int FootMins { get; set; }
        public int BodyMins { get; set; }
        public decimal StaffCommission { get; set; }
        public decimal ExtraCommission { get; set; }
        public decimal Price { get; set; }
    }
    public class IncentiveDto
    {
        public string Id { get; set; } = string.Empty;
        public DateTimeOffset IncentiveDate { get; set; }
        public string Description { get; set; } = string.Empty;
        public int Remark { get; set; }
        public decimal Amount { get; set; }
    }

    public class FullTherapistCommissionDto
    {
        public List<TherapistCommissionDto> Commissions { get; set; }
        public List<IncentiveDto> Incentives { get; set; }
        public bool IsRate { get; set; }
        public RateBaseDto RateBase { get; set; }
        public bool IsCommissionPercentage { get; set; }
        public CommissionPercentageDto CommissionPercentage { get; set; }
        public bool IsGuaranteeIncome { get; set; }
        public GuaranteeIncomeDto GuaranteeIncome { get; set; }

    }

    public class GuaranteeIncomeDto
    {
        public decimal FirstPeriodCommission { get; set; }
        public decimal SecondPeriodCommission { get; set; }
        public decimal TotalCommission { get; set; }
        public decimal GuaranteeIncomePaid { get; set; }
    }

    public class RateBaseDto
    {
        public double SelectedPeriodHrs { get; set; }
        public double AllPeriodHrs { get; set; }
        public int TotalFootMins { get; set; }
        public decimal TotalFootCommission { get; set; }
        public int TotalBodyMins { get; set; }
        public decimal TotalBodyCommission { get; set; }
        public decimal TotalStaffCommission { get; set; }
        public decimal TotalExtraCommission { get; set; }
        public decimal TotalCommission { get; set; }
    }

    public class CommissionPercentageDto
    {
        public double SelectedPeriodHrs { get; set; }
        public double AllPeriodHrs { get; set; }
        public decimal TotalStaffCommission { get; set; }
        public decimal TotalExtraCommission { get; set; }
        public decimal TotalPrice { get; set; }
        public decimal TotalCommission { get; set; }
    }
}