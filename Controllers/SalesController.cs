using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QwenHT.Data;
using QwenHT.Models;

namespace QwenHT.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SalesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public SalesController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet("dashboard")]
        public async Task<ActionResult<DashboardSalesData>> GetDashboardSalesData([FromQuery] string? date = null, [FromQuery] string? month = null, [FromQuery] string? year = null, [FromQuery] int? timezoneOffset = null)
        {
            var currentPeriodSales = new List<OutletSalesData>();
            var previousPeriodSales = new List<OutletSalesData>();

            // Filter by specific date if provided
            if (!string.IsNullOrEmpty(date))
            {
                if (DateTime.TryParse(date, out DateTime parsedDate))
                {
                    int offsetMinutes = timezoneOffset ?? 0;
                    TimeSpan offsetSpan = TimeSpan.FromMinutes(offsetMinutes);

                    // Current day
                    var localStartOfDay = new DateTime(parsedDate.Year, parsedDate.Month, parsedDate.Day, 0, 0, 0, DateTimeKind.Unspecified);
                    var localEndOfDay = localStartOfDay.AddDays(1);
                    var utcStartOfDay = localStartOfDay.Add(offsetSpan);
                    var utcEndOfDay = localEndOfDay.Add(offsetSpan);

                    // Previous day
                    var localStartOfPrevDay = localStartOfDay.AddDays(-1);
                    var localEndOfPrevDay = localEndOfDay.AddDays(-1);
                    var utcStartOfPrevDay = localStartOfPrevDay.Add(offsetSpan);
                    var utcEndOfPrevDay = localEndOfPrevDay.Add(offsetSpan);

                    // Get current day sales
                    var currentQuery = _context.Sales.Where(s => s.Status == 1 && s.SalesDate >= utcStartOfDay && s.SalesDate < utcEndOfDay);
                    currentPeriodSales = await currentQuery
                        .GroupBy(s => s.Outlet)
                        .Select(g => new OutletSalesData { Outlet = g.Key, TotalSales = g.Sum(s => s.Price) })
                        .ToListAsync();

                    // Get previous day sales
                    var previousQuery = _context.Sales.Where(s => s.Status == 1 && s.SalesDate >= utcStartOfPrevDay && s.SalesDate < utcEndOfPrevDay);
                    previousPeriodSales = await previousQuery
                        .GroupBy(s => s.Outlet)
                        .Select(g => new OutletSalesData { Outlet = g.Key, TotalSales = g.Sum(s => s.Price) })
                        .ToListAsync();
                }
            }
            // Filter by specific month if provided
            else if (!string.IsNullOrEmpty(month))
            {
                var parts = month.Split('-');
                if (parts.Length == 2 && int.TryParse(parts[0], out int yearNum) && int.TryParse(parts[1], out int monthNum))
                {
                    // Current month
                    var startOfCurrentMonth = new DateTime(yearNum, monthNum, 1, 0, 0, 0, DateTimeKind.Utc);
                    var startOfNextMonth = startOfCurrentMonth.AddMonths(1);

                    // Previous month
                    var startOfPrevMonth = startOfCurrentMonth.AddMonths(-1);

                    // Get current month sales
                    var currentQuery = _context.Sales.Where(s => s.Status == 1 && s.SalesDate >= startOfCurrentMonth && s.SalesDate < startOfNextMonth);
                    currentPeriodSales = await currentQuery
                        .GroupBy(s => s.Outlet)
                        .Select(g => new OutletSalesData { Outlet = g.Key, TotalSales = g.Sum(s => s.Price) })
                        .ToListAsync();

                    // Get previous month sales
                    var previousQuery = _context.Sales.Where(s => s.Status == 1 && s.SalesDate >= startOfPrevMonth && s.SalesDate < startOfCurrentMonth);
                    previousPeriodSales = await previousQuery
                        .GroupBy(s => s.Outlet)
                        .Select(g => new OutletSalesData { Outlet = g.Key, TotalSales = g.Sum(s => s.Price) })
                        .ToListAsync();
                }
            }
            // Filter by specific year if provided
            else if (!string.IsNullOrEmpty(year))
            {
                if (int.TryParse(year, out int yearNum))
                {
                    // Current year
                    var startOfCurrentYear = new DateTime(yearNum, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                    var startOfNextYear = startOfCurrentYear.AddYears(1);

                    // Previous year
                    var startOfPrevYear = startOfCurrentYear.AddYears(-1);

                    // Get current year sales
                    var currentQuery = _context.Sales.Where(s => s.Status == 1 && s.SalesDate >= startOfCurrentYear && s.SalesDate < startOfNextYear);
                    currentPeriodSales = await currentQuery
                        .GroupBy(s => s.Outlet)
                        .Select(g => new OutletSalesData { Outlet = g.Key, TotalSales = g.Sum(s => s.Price) })
                        .ToListAsync();

                    // Get previous year sales
                    var previousQuery = _context.Sales.Where(s => s.Status == 1 && s.SalesDate >= startOfPrevYear && s.SalesDate < startOfCurrentYear);
                    previousPeriodSales = await previousQuery
                        .GroupBy(s => s.Outlet)
                        .Select(g => new OutletSalesData { Outlet = g.Key, TotalSales = g.Sum(s => s.Price) })
                        .ToListAsync();
                }
            }
            // Default to today if no specific date/month/year is provided
            else
            {
                int offsetMinutes = timezoneOffset ?? 0;
                TimeSpan offsetSpan = TimeSpan.FromMinutes(offsetMinutes);

                // Determine "today" in the user's timezone
                var userLocalNow = DateTime.UtcNow.AddMinutes(offsetMinutes);
                var userToday = new DateTime(userLocalNow.Year, userLocalNow.Month, userLocalNow.Day, 0, 0, 0, DateTimeKind.Unspecified);
                var userTomorrow = userToday.AddDays(1);
                var utcStartOfToday = userToday.Add(offsetSpan);
                var utcStartOfTomorrow = userTomorrow.Add(offsetSpan);

                // Previous day
                var userPrevDay = userToday.AddDays(-1);
                var utcStartOfPrevDay = userPrevDay.Add(offsetSpan);
                var utcStartOfTodayPrev = userToday.Add(offsetSpan);

                // Get current day sales
                var currentQuery = _context.Sales.Where(s => s.Status == 1 && s.SalesDate >= utcStartOfToday && s.SalesDate < utcStartOfTomorrow);
                currentPeriodSales = await currentQuery
                    .GroupBy(s => s.Outlet)
                    .Select(g => new OutletSalesData { Outlet = g.Key, TotalSales = g.Sum(s => s.Price) })
                    .ToListAsync();

                // Get previous day sales
                var previousQuery = _context.Sales.Where(s => s.Status == 1 && s.SalesDate >= utcStartOfPrevDay && s.SalesDate < utcStartOfTodayPrev);
                previousPeriodSales = await previousQuery
                    .GroupBy(s => s.Outlet)
                    .Select(g => new OutletSalesData { Outlet = g.Key, TotalSales = g.Sum(s => s.Price) })
                    .ToListAsync();
            }

            // Calculate the change percentages
            var result = new DashboardSalesData
            {
                TotalSalesAllOutlets = currentPeriodSales.Sum(s => s.TotalSales),
                TotalSalesHTSA = currentPeriodSales.FirstOrDefault(s => s.Outlet == "HTSA")?.TotalSales ?? 0,
                TotalSalesHTL = currentPeriodSales.FirstOrDefault(s => s.Outlet == "HTL")?.TotalSales ?? 0,
                TotalSalesHTG = currentPeriodSales.FirstOrDefault(s => s.Outlet == "HTG")?.TotalSales ?? 0,

                PreviousSalesAllOutlets = previousPeriodSales.Sum(s => s.TotalSales),
                PreviousSalesHTSA = previousPeriodSales.FirstOrDefault(s => s.Outlet == "HTSA")?.TotalSales ?? 0,
                PreviousSalesHTL = previousPeriodSales.FirstOrDefault(s => s.Outlet == "HTL")?.TotalSales ?? 0,
                PreviousSalesHTG = previousPeriodSales.FirstOrDefault(s => s.Outlet == "HTG")?.TotalSales ?? 0,
                // Calculate percentage changes compared to the previous period
                SalesChangePercentageAll = CalculateChangePercentage(
                    currentPeriodSales.Sum(s => s.TotalSales),
                    previousPeriodSales.Sum(s => s.TotalSales)
                ),
                SalesChangePercentageHTSA = CalculateChangePercentage(
                    currentPeriodSales.FirstOrDefault(s => s.Outlet == "HTSA")?.TotalSales ?? 0,
                    previousPeriodSales.FirstOrDefault(s => s.Outlet == "HTSA")?.TotalSales ?? 0
                ),
                SalesChangePercentageHTL = CalculateChangePercentage(
                    currentPeriodSales.FirstOrDefault(s => s.Outlet == "HTL")?.TotalSales ?? 0,
                    previousPeriodSales.FirstOrDefault(s => s.Outlet == "HTL")?.TotalSales ?? 0
                ),
                SalesChangePercentageHTG = CalculateChangePercentage(
                    currentPeriodSales.FirstOrDefault(s => s.Outlet == "HTG")?.TotalSales ?? 0,
                    previousPeriodSales.FirstOrDefault(s => s.Outlet == "HTG")?.TotalSales ?? 0
                ),
            };

            return Ok(result);
        }

        private int CalculateChangePercentage(decimal current, decimal previous)
        {
            if (previous == 0)
            {
                // If the previous period value was 0, and current is >0, consider it 100% increase
                // If both are 0, return 0%
                return current > 0 ? 100 : 0;
            }

            var change = ((current - previous) / previous) * 100;
            return (int)Math.Round(change);
        }
    }

    public class DashboardSalesData
    {
        public decimal TotalSalesAllOutlets { get; set; }
        public decimal TotalSalesHTSA { get; set; }
        public decimal TotalSalesHTL { get; set; }
        public decimal TotalSalesHTG { get; set; }

        public decimal PreviousSalesAllOutlets { get; set; }
        public decimal PreviousSalesHTSA { get; set; }
        public decimal PreviousSalesHTL { get; set; }
        public decimal PreviousSalesHTG { get; set; }

        public int SalesChangePercentageAll { get; set; }
        public int SalesChangePercentageHTSA { get; set; }
        public int SalesChangePercentageHTL { get; set; }
        public int SalesChangePercentageHTG { get; set; }
    }

    public class OutletSalesData
    {
        public string Outlet { get; set; } = string.Empty;
        public decimal TotalSales { get; set; }
    }
}