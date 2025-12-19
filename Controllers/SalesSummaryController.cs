using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QwenHT.Data;
using QwenHT.Models;

namespace QwenHT.Controllers
{
    [ApiController]
    [Route("api")]
    [Authorize(Policy = "NavigationAccess")] // Use custom policy based on navigation permissions
    public class SalesSummaryController(ApplicationDbContext _context) : ControllerBase
    {
        [HttpGet("sales-summary/outlet/active")]
        public async Task<ActionResult<IEnumerable<OptionValue>>> GetOptionValues([FromQuery] string? category = null)
        {
            var query = _context.OptionValues.AsQueryable();

            // Apply category filter if provided
            if (!string.IsNullOrEmpty(category))
            {
                query = query.Where(ov => ov.Category!.ToLower() == category.ToLower());
            }

            // Apply active filter
            query = query.Where(ov => ov.IsActive);

            // Order by category and value
            var optionValues = await query
                .OrderBy(ov => ov.Category)
                .ThenBy(ov => ov.Value)
                .ToListAsync();

            return Ok(optionValues);
        }

        // GET api/sales/summary - Retrieve sales summary grouped by menu code and description
        [HttpGet("sales-summary/summary")]
        public async Task<ActionResult<IEnumerable<SalesSummaryDto>>> GetSalesSummary(
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null,
            [FromQuery] string? category = null,
            [FromQuery] string? outlet = null)
        {
            var query = _context.Sales
                .Include(s => s.Menu)
                .Where(s => s.Status == 1) // Only active sales
                .AsQueryable();

            // Apply filters if provided
            if (startDate.HasValue)
            {
                query = query.Where(s => s.SalesDate >= startDate.Value.Date);
            }

            if (endDate.HasValue)
            {
                query = query.Where(s => s.SalesDate <= endDate.Value.Date.AddDays(1).AddTicks(-1)); // Include the full day
            }

            if (!string.IsNullOrEmpty(outlet) && outlet != "ALL")
            {
                query = query.Where(s => s.Outlet == outlet);
            }


            if (!string.IsNullOrEmpty(category) && category != "ALL")
            {
                query = query.Where(s => s.Menu.Category == category);
            }
            // Join with menus and filter by category

            // Group by menu code, description, and price to get summary data
            var summaryData = await query
                .GroupBy(s => new { s.Menu.Code, s.Menu.Description, s.Price })
                .Select(g => new SalesSummaryDto
                {
                    Code = g.Key.Code,
                    Description = g.Key.Description,
                    Price = g.Key.Price,
                    SaleCount = g.Count(),
                    TotalSales = g.Sum(s => s.Price)
                })
                .OrderByDescending(s => s.SaleCount)
                .ThenByDescending(s => s.Code) // Order by sale count descending
                .ToListAsync();

            return Ok(summaryData);
        }
        public class SalesSummaryDto
        {
            public string Code { get; set; } = string.Empty;
            public string Description { get; set; } = string.Empty;
            public decimal Price { get; set; }
            public int SaleCount { get; set; }
            public decimal TotalSales { get; set; }
        }
    }
}
