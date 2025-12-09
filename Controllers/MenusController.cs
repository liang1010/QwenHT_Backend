using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QwenHT.Data;
using QwenHT.Models;

namespace QwenHT.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Policy = "NavigationAccess")] // Use custom policy based on navigation permissions
    public class MenusController(ApplicationDbContext _context) : ControllerBase
    {

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Menu>>> GetMenus([FromQuery] string? searchTerm = null)
        {
            var query = _context.Menus.AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                query = query.Where(m =>
                    EF.Functions.Like(m.Code, $"%{searchTerm}%") ||
                    EF.Functions.Like(m.Description, $"%{searchTerm}%"));
            }

            var menus = await query
                .OrderBy(m => m.Code)
                .ThenBy(m => m.Description)
                .ToListAsync();

            var menuDtos = menus.ToList();

            return Ok(menuDtos);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Menu>> GetMenu(Guid id)
        {
            var menu = await _context.Menus.FindAsync(id);

            if (menu == null)
            {
                return NotFound();
            }

            return Ok(menu);
        }

        [HttpPost]
        public async Task<ActionResult<Menu>> CreateMenu(Menu menuDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Check if a menu with the same code already exists
            if (!string.IsNullOrEmpty(menuDto.Code))
            {
                var existingMenu = await _context.Menus
                    .FirstOrDefaultAsync(m => m.Code!.ToLower() == menuDto.Code.ToLower());

                if (existingMenu != null)
                {
                    return BadRequest("A menu with the same code already exists.");
                }
            }

            // Get current user from JWT claims
            var username = User?.Claims?.FirstOrDefault(c => c.Type == "username")?.Value ?? "Unknown";

            var menu = new Menu
            {
                Id = Guid.NewGuid(),
                Code = menuDto.Code,
                Description = menuDto.Description,
                Category = menuDto.Category,
                FootMins = menuDto.FootMins,
                BodyMins = menuDto.BodyMins,
                StaffCommission = menuDto.StaffCommission,
                ExtraCommission = menuDto.ExtraCommission,
                Price = menuDto.Price,
                Status = 1,
                CreatedBy = username,
                CreatedAt = DateTime.UtcNow,
                LastUpdated = DateTime.UtcNow,
                LastModifiedBy = username
            };

            _context.Menus.Add(menu);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetMenu), new { id = menu.Id }, menu);
        }

        [HttpPost("{id}")]
        public async Task<IActionResult> UpdateMenu(Guid id, Menu menuDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var menu = await _context.Menus.FindAsync(id);

            if (menu == null)
            {
                return NotFound();
            }

            // Check if a menu with the same code already exists (for a different menu)
            if (!string.IsNullOrEmpty(menuDto.Code))
            {
                var existingMenu = await _context.Menus
                    .FirstOrDefaultAsync(m => m.Code!.ToLower() == menuDto.Code.ToLower() && m.Id != id);

                if (existingMenu != null)
                {
                    return BadRequest("A menu with the same code already exists.");
                }
            }

            // Get current user from JWT claims
            var username = User?.Claims?.FirstOrDefault(c => c.Type == "username")?.Value ?? "Unknown";
            menu.Description = menuDto.Description;
            menu.Category = menuDto.Category;
            menu.FootMins = menuDto.FootMins;
            menu.BodyMins = menuDto.BodyMins;
            menu.StaffCommission = menuDto.StaffCommission;
            menu.ExtraCommission = menuDto.ExtraCommission;
            menu.Price = menuDto.Price;
            menu.Status = menuDto.Status;
            menu.LastUpdated = DateTime.UtcNow;
            menu.LastModifiedBy = username;

            _context.Menus.Update(menu);
            await _context.SaveChangesAsync();

            return Ok(menu);
        }

        [HttpPost("{id}/delete")]
        public async Task<IActionResult> DeleteMenu(Guid id)
        {
            var menu = await _context.Menus.FindAsync(id);

            if (menu == null)
            {
                return NotFound();
            }

            menu.Status = 0;
            _context.Menus.Update(menu);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        [HttpGet("paginated")]
        public async Task<ActionResult<PaginatedResponse<Menu>>> GetMenusPaginated(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? sortField = null,
            [FromQuery] string? sortDirection = null,
            [FromQuery] string? searchTerm = null)
        {
            // Validate pagination parameters
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 10;

            // Build the base query
            IQueryable<Menu> query = _context.Menus.AsNoTracking();

            // Apply search filter if provided
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                query = query.Where(m =>
                    EF.Functions.Like(m.Code, $"%{searchTerm}%") ||
                    EF.Functions.Like(m.Description, $"%{searchTerm}%"));
            }

            // Determine the order to apply (before pagination)
            IOrderedQueryable<Menu> orderedQuery = query switch
            {
                _ when sortField?.ToLower() == "description" => sortDirection?.ToLower() == "desc"
                    ? query.OrderByDescending(m => m.Description ?? "")
                    : query.OrderBy(m => m.Description ?? ""),
                _ when sortField?.ToLower() == "code" => sortDirection?.ToLower() == "desc"
                    ? query.OrderByDescending(m => m.Code ?? "")
                    : query.OrderBy(m => m.Code ?? ""),
                _ => query.OrderBy(m => m.Code).ThenBy(m => m.Description) // Default sort
            };

            // Get total count of matching records before pagination
            var totalCount = await orderedQuery.CountAsync();

            // Apply pagination (Skip and Take)
            var pagedMenus = await orderedQuery
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var menuDtos = pagedMenus.ToList();

            var response = new PaginatedResponse<Menu>
            {
                Data = menuDtos,
                TotalCount = totalCount,
                PageSize = pageSize,
                CurrentPage = page
            };

            return Ok(response);
        }
    }
}