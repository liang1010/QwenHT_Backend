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
    public class SalesController(ApplicationDbContext _context) : ControllerBase
    {

        [HttpGet("sales/staff/active")]
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
                .ToListAsync();

            return Ok(activeStaff);
        }

        [HttpGet("sales/menu/active")]
        public async Task<ActionResult<IEnumerable<ActiveMenuDto>>> GetActiveMenu()
        {
            var activeMenu = await _context.Menus
                .Where(m => m.Status == 1) // Only return menus with status = 1
                .Select(m => new ActiveMenuDto
                {
                    Id = m.Id,
                    Code = m.Code,
                    Description = m.Description,
                    Category = m.Category,
                    FootMins = m.FootMins,
                    BodyMins = m.BodyMins,
                    Price = m.Price,
                    StaffCommission = m.StaffCommission,
                    ExtraCommission = m.ExtraCommission
                })
                .ToListAsync();

            return Ok(activeMenu);
        }

        [HttpGet("sales/outlet/active")]
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


        
    }

    

    public class ActiveStaffDto
    {
        public Guid Id { get; set; }
        public string? NickName { get; set; }
        public string FullName { get; set; } = string.Empty;
    }

    public class ActiveMenuDto
    {
        public Guid Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public int FootMins { get; set; }
        public int BodyMins { get; set; }
        public decimal Price { get; set; }
        public decimal StaffCommission { get; set; }
        public decimal ExtraCommission { get; set; }
    }
}