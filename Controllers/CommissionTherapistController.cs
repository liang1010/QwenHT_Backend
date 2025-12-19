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
        public async Task<ActionResult<IEnumerable<TherapistCommissionDto>>> GetTherapistCommission(
            [FromQuery] Guid staffId,
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null)
        {
            var query = _context.Sales
                .Include(s => s.Menu)
                .Where(s => s.StaffId == staffId && s.Status == 1) // Filter by staff and active sales
                .AsQueryable();

            // Apply date filters if provided
            if (startDate.HasValue)
            {
                query = query.Where(s => s.SalesDate >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                // End date should be exclusive (before next day)
                query = query.Where(s => s.SalesDate < endDate.Value.AddDays(1));
            }

            // Group by date and menu code to get aggregated values
            // We need to convert in memory since Entity Framework can't translate s.SalesDate.DateTime.Date
            var rawData = await query
                .Select(s => new
                {
                    SalesDate = s.SalesDate, // Convert to date only
                    MenuCode = s.Menu.Code,
                    FootMins = s.Menu.FootMins,
                    BodyMins = s.Menu.BodyMins,
                    StaffCommission = s.StaffCommission,
                    ExtraCommission = s.ExtraCommission
                })
                .ToListAsync();

            // Group in memory by date and menu code
            var groupedData = rawData
                .GroupBy(s => new { Date = s.SalesDate, Code = s.MenuCode })
                .Select(g => new TherapistCommissionDto
                {
                    Id = Guid.NewGuid().ToString(), // We'll generate a random ID for the grouped record
                    SalesDate = g.Key.Date,
                    MenuCode = g.Key.Code,
                    FootMins = g.Sum(s => s.FootMins),
                    BodyMins = g.Sum(s => s.BodyMins),
                    StaffCommission = g.Sum(s => s.StaffCommission),
                    ExtraCommission = g.Sum(s => s.ExtraCommission)
                })
                .ToList();

            // Order by sales date
            var orderedData = groupedData.OrderBy(s => s.SalesDate).ToList();

            return Ok(orderedData);
        }

        // GET api/commission/therapist/pdf - Generate therapist commission PDF report (grouped by date and menu code)
        [HttpGet("therapist/pdf")]
        public async Task<IActionResult> GetTherapistCommissionPdf(
            [FromQuery] Guid staffId,
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null)
        {
            // Get staff name for the report
            var staff = await _context.Staff.FindAsync(staffId);
            if (staff == null)
            {
                return NotFound(new { error = "Staff not found" });
            }

            // Get commission data (same query as the API endpoint)
            var query = _context.Sales
                .Include(s => s.Menu)
                .Where(s => s.StaffId == staffId && s.Status == 1) // Filter by staff and active sales
                .AsQueryable();

            // Apply date filters if provided
            if (startDate.HasValue)
            {
                query = query.Where(s => s.SalesDate >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                // End date should be exclusive (before next day)
                query = query.Where(s => s.SalesDate < endDate.Value.AddDays(1));
            }

            // Group by date and menu code to get aggregated values
            // We need to convert in memory since Entity Framework can't translate s.SalesDate.DateTime.Date
            var rawData = await query
                .Select(s => new
                {
                    SalesDate = s.SalesDate, // Convert to date only
                    MenuCode = s.Menu.Code,
                    FootMins = s.Menu.FootMins,
                    BodyMins = s.Menu.BodyMins,
                    StaffCommission = s.StaffCommission,
                    ExtraCommission = s.ExtraCommission
                })
                .ToListAsync();

            // Group in memory by date and menu code
            var groupedData = rawData
                .GroupBy(s => new { Date = s.SalesDate, Code = s.MenuCode })
                .Select(g => new
                {
                    SalesDate = g.Key.Date,
                    MenuCode = g.Key.Code,
                    TotalFootMins = g.Sum(s => s.FootMins),
                    TotalBodyMins = g.Sum(s => s.BodyMins),
                    TotalStaffCommission = g.Sum(s => s.StaffCommission),
                    TotalExtraCommission = g.Sum(s => s.ExtraCommission)
                })
                .ToList();

            // Map to report item and order by sales date in memory
            var reportItems = groupedData
                .Select(s => new TherapistCommissionReportItem
                {
                    SalesDate = s.SalesDate,
                    MenuCode = s.MenuCode,
                    FootMins = s.TotalFootMins,
                    BodyMins = s.TotalBodyMins,
                    StaffCommission = s.TotalStaffCommission,
                    ExtraCommission = s.TotalExtraCommission
                })
                .OrderBy(s => s.SalesDate)
                .ToList();

            // Generate PDF
            var pdfBytes = PdfGenerator.GenerateTherapistCommissionReport(
                staff.NickName ?? staff.FullName,
                startDate ?? DateTime.Now.AddDays(-30), // Default to 30 days if not provided
                endDate ?? DateTime.Now,
                reportItems);

            // Return PDF as file
            var fileName = $"Therapist-Commission-{(staff.NickName ?? staff.FullName).Replace(" ", "_")}-{(startDate?.ToString("yyyyMMdd") ?? DateTime.Now.ToString("yyyyMMdd"))}-{(endDate?.ToString("yyyyMMdd") ?? DateTime.Now.ToString("yyyyMMdd"))}.pdf";
            return File(pdfBytes, "application/pdf", fileName);
        }
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
    }
}