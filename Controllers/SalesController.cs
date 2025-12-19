using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QwenHT.Data;
using QwenHT.Models;
using QwenHT.Utilities;

namespace QwenHT.Controllers
{
    [ApiController]
    [Route("api")]
    [Authorize(Policy = "NavigationAccess")] // Use custom policy based on navigation permissions
    public class SalesController(ApplicationDbContext _context) : ControllerBase
    {
        // GET api/sales - Retrieve all sales records (with optional filtering)
        [HttpGet("sales")]
        public async Task<ActionResult<IEnumerable<Sales>>> GetSales(
            [FromQuery] DateTime? fromDate = null,
            [FromQuery] DateTime? toDate = null,
            [FromQuery] Guid? staffId = null,
            [FromQuery] string? outlet = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50)
        {
            var query = _context.Sales
                .Include(s => s.Staff)
                .Include(s => s.Menu)
                .AsQueryable();

            // Apply filters if provided
            if (fromDate.HasValue)
            {
                query = query.Where(s => s.SalesDate >= fromDate.Value.Date);
            }

            if (toDate.HasValue)
            {
                query = query.Where(s => s.SalesDate <= toDate.Value.Date.AddDays(1).AddTicks(-1)); // Include the full day
            }

            if (staffId.HasValue)
            {
                query = query.Where(s => s.StaffId == staffId.Value);
            }

            if (!string.IsNullOrEmpty(outlet))
            {
                query = query.Where(s => s.Outlet == outlet);
            }

            // Pagination
            var totalRecords = await query.CountAsync();
            var salesRecords = await query
                .OrderByDescending(s => s.SalesDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var response = new
            {
                Data = salesRecords,
                TotalRecords = totalRecords,
                Page = page,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling((double)totalRecords / pageSize)
            };

            return Ok(response);
        }

        

       

       

    }

    // DTO for sales inquiry response
    


    



   
}
