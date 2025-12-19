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
    public class SalesKeyInController(ApplicationDbContext _context) : ControllerBase
    {
        [HttpGet("sales-key-in/staff/active")]
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

        [HttpGet("sales-key-in/menu/active")]
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

        [HttpGet("sales-key-in/outlet/active")]
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

        // POST api/sales - Save new sales record
        [HttpPost("sales-key-in")]
        public async Task<ActionResult<Sales>> SaveSales([FromBody] CreateSalesRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Validate that the provided StaffId exists
            var staff = await _context.Staff.FindAsync(request.StaffId);
            if (staff == null)
            {
                return BadRequest(new { error = "Invalid Staff ID" });
            }

            // Validate that the provided MenuId exists
            var menu = await _context.Menus.FindAsync(request.MenuId);
            if (menu == null)
            {
                return BadRequest(new { error = "Invalid Menu ID" });
            }

            // Get the current user for CreatedBy field
            var currentUser = User?.Identity?.Name ?? "System";

            var sales = new Sales
            {
                Id = Guid.NewGuid(),
                SalesDate = request.SalesDate,
                StaffId = request.StaffId,
                Outlet = request.Outlet,
                MenuId = request.MenuId,
                Request = request.Request,
                FootCream = request.FootCream,
                Oil = request.Oil,
                Price = request.Price,
                ExtraCommission = request.ExtraCommission,
                StaffCommission = request.StaffCommission,
                Remark = request.Remark,
                Status = 1, // Active by default
                CreatedBy = currentUser,
                CreatedAt = DateTimeOffset.UtcNow,
                LastUpdated = DateTimeOffset.UtcNow,
                LastModifiedBy = currentUser
            };

            _context.Sales.Add(sales);
            await _context.SaveChangesAsync();

            // Return the created sales record with related entities
            var createdSales = await _context.Sales
                .Include(s => s.Staff)
                .Include(s => s.Menu)
                .FirstOrDefaultAsync(s => s.Id == sales.Id);

            return CreatedAtAction(nameof(SaveSales), new { id = createdSales.Id }, createdSales);
        }

        public class CreateSalesRequest
        {
            public DateTime SalesDate { get; set; } = DateTime.UtcNow;
            public Guid StaffId { get; set; }
            public string Outlet { get; set; } = string.Empty;
            public Guid MenuId { get; set; }
            public bool? Request { get; set; }
            public bool? FootCream { get; set; }
            public bool? Oil { get; set; }
            public decimal Price { get; set; }
            public decimal ExtraCommission { get; set; }
            public decimal StaffCommission { get; set; }
            public string? Remark { get; set; }
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
}
