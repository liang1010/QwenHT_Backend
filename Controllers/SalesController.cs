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

        // POST api/sales - Save new sales record
        [HttpPost("sales")]
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

            return CreatedAtAction(nameof(GetSalesById), new { id = createdSales.Id }, createdSales);
        }

        // GET api/sales/{id} - Retrieve a specific sales record by ID
        [HttpGet("sales/{id}")]
        public async Task<ActionResult<Sales>> GetSalesById(Guid id)
        {
            var sales = await _context.Sales
                .Include(s => s.Staff)
                .Include(s => s.Menu)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (sales == null)
            {
                return NotFound();
            }

            return Ok(sales);
        }

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

        // GET api/sales/inquiry - Retrieve sales records for inquiry with virtual scrolling support
        [HttpGet("sales/inquiry")]
        public async Task<ActionResult<PagedResponse<SalesInquiryDto>>> GetSalesInquiry(
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null,
            [FromQuery] string? outlet = null,
            [FromQuery] int offset = 0,
            [FromQuery] int limit = 20)
        {
            var query = _context.Sales
                .Include(s => s.Staff)
                .Include(s => s.Menu)
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

            if (!string.IsNullOrEmpty(outlet))
            {
                query = query.Where(s => s.Outlet == outlet);
            }

            // Apply active status filter
            query = query.Where(s => s.Status == 1);

            // Count total records for pagination
            var totalRecords = await query.CountAsync();

            // Apply ordering and pagination
            var salesRecords = await query
                .OrderByDescending(s => s.SalesDate)
                .ThenByDescending(s => s.CreatedAt)
                .Skip(offset)
                .Take(limit)
                .Select(s => new SalesInquiryDto
                {
                    Id = s.Id,
                    SalesDate = s.SalesDate,
                    StaffId = s.StaffId,
                    StaffName = s.Staff.FullName,
                    Outlet = s.Outlet,
                    OutletName = s.Outlet, // In a real scenario, this would come from an Outlet entity
                    MenuId = s.MenuId,
                    MenuDescription = s.Menu.Description,
                    Price = s.Price,
                    BodyMins = s.Menu.BodyMins,
                    FootMins = s.Menu.FootMins,
                    StaffCommission = s.StaffCommission,
                    ExtraCommission = s.ExtraCommission,
                    Remark = s.Remark,
                    Request = s.Request ?? false,
                    FootCream = s.FootCream ?? false,
                    Oil = s.Oil ?? false
                })
                .ToListAsync();

            var response = new PagedResponse<SalesInquiryDto>
            {
                Data = salesRecords,
                TotalCount = totalRecords,
                Offset = offset,
                Limit = limit
            };

            return Ok(response);
        }

        // PUT api/sales/{id} - Update an existing sales record
        [HttpPut("sales/{id}")]
        public async Task<ActionResult<Sales>> UpdateSales(Guid id, [FromBody] UpdateSalesRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var sales = await _context.Sales.FindAsync(id);
            if (sales == null)
            {
                return NotFound();
            }

            // Validate that the provided StaffId exists (if changing)
            if (request.StaffId != sales.StaffId)
            {
                var staff = await _context.Staff.FindAsync(request.StaffId);
                if (staff == null)
                {
                    return BadRequest(new { error = "Invalid Staff ID" });
                }
            }

            // Validate that the provided MenuId exists (if changing)
            if (request.MenuId != sales.MenuId)
            {
                var menu = await _context.Menus.FindAsync(request.MenuId);
                if (menu == null)
                {
                    return BadRequest(new { error = "Invalid Menu ID" });
                }
            }

            // Update the sales record
            sales.SalesDate = request.SalesDate;
            sales.StaffId = request.StaffId;
            sales.Outlet = request.Outlet;
            sales.MenuId = request.MenuId;
            sales.Request = request.Request;
            sales.FootCream = request.FootCream;
            sales.Oil = request.Oil;
            sales.Price = request.Price;
            sales.ExtraCommission = request.ExtraCommission;
            sales.StaffCommission = request.StaffCommission;
            sales.Remark = request.Remark;
            sales.LastUpdated = DateTimeOffset.UtcNow;
            sales.LastModifiedBy = User?.Identity?.Name ?? "System";

            await _context.SaveChangesAsync();

            // Return the updated sales record
            var updatedSales = await _context.Sales
                .Include(s => s.Staff)
                .Include(s => s.Menu)
                .FirstOrDefaultAsync(s => s.Id == sales.Id);

            return Ok(updatedSales);
        }

        // DELETE api/sales/{id} - Delete a sales record
        [HttpDelete("sales/{id}")]
        public async Task<IActionResult> DeleteSales(Guid id)
        {
            var sales = await _context.Sales.FindAsync(id);
            if (sales == null)
            {
                return NotFound();
            }

            // Soft delete by updating status to inactive
            sales.Status = 0; // Inactive
            sales.LastUpdated = DateTimeOffset.UtcNow;
            sales.LastModifiedBy = User?.Identity?.Name ?? "System";

            await _context.SaveChangesAsync();

            return Ok(new { message = "Sales record deleted successfully" });
        }

    }

    // DTO for sales inquiry response
    public class SalesInquiryDto
    {
        public Guid Id { get; set; }
        public DateTimeOffset SalesDate { get; set; }
        public Guid StaffId { get; set; }
        public string StaffName { get; set; } = string.Empty;
        public string Outlet { get; set; } = string.Empty;
        public string OutletName { get; set; } = string.Empty;
        public Guid MenuId { get; set; }
        public string MenuDescription { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int BodyMins { get; set; }
        public int FootMins { get; set; }
        public decimal StaffCommission { get; set; }
        public decimal ExtraCommission { get; set; }
        public string? Remark { get; set; }
        public bool Request { get; set; }
        public bool FootCream { get; set; }
        public bool Oil { get; set; }
    }

    public class UpdateSalesRequest
    {
        public DateTimeOffset SalesDate { get; set; }
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

    public class PagedResponse<T>
    {
        public List<T> Data { get; set; } = new();
        public int TotalCount { get; set; }
        public int Offset { get; set; }
        public int Limit { get; set; }
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
