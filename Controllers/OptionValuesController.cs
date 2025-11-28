using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using QwenHT.Data;
using QwenHT.Models;

namespace QwenHT.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Policy = "NavigationAccess")] // Use custom policy based on navigation permissions
    public class OptionValuesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public OptionValuesController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
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

        [HttpGet("{id}")]
        public async Task<ActionResult<OptionValue>> GetOptionValue(Guid id)
        {
            var optionValue = await _context.OptionValues.FindAsync(id);

            if (optionValue == null || !optionValue.IsActive)
            {
                return NotFound();
            }

            return optionValue;
        }

        [HttpPost]
        public async Task<ActionResult<OptionValue>> CreateOptionValue(OptionValue optionValue)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Check if an option value with the same category and value already exists
            var existingOptionValue = await _context.OptionValues
                .FirstOrDefaultAsync(ov => ov.Category.ToLower() == optionValue.Category.ToLower() 
                                        && ov.Value.ToLower() == optionValue.Value.ToLower());

            if (existingOptionValue != null)
            {
                return BadRequest("An option value with the same category and value already exists.");
            }

            optionValue.Id = Guid.NewGuid();
            optionValue.CreatedAt = DateTime.UtcNow;
            optionValue.LastUpdated = DateTime.UtcNow;
            optionValue.IsActive = true; // Default to active

            _context.OptionValues.Add(optionValue);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetOptionValue), new { id = optionValue.Id }, optionValue);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateOptionValue(Guid id, OptionValue optionValue)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var existingOptionValue = await _context.OptionValues.FindAsync(id);

            if (existingOptionValue == null)
            {
                return NotFound();
            }

            // Check if an option value with the same category and value already exists (excluding this one)
            var duplicateOptionValue = await _context.OptionValues
                .FirstOrDefaultAsync(ov => ov.Id != id 
                                        && ov.Category.ToLower() == optionValue.Category.ToLower() 
                                        && ov.Value.ToLower() == optionValue.Value.ToLower());

            if (duplicateOptionValue != null)
            {
                return BadRequest("An option value with the same category and value already exists.");
            }

            existingOptionValue.Category = optionValue.Category;
            existingOptionValue.Value = optionValue.Value;
            existingOptionValue.Description = optionValue.Description;
            existingOptionValue.IsActive = optionValue.IsActive;
            existingOptionValue.LastUpdated = DateTime.UtcNow;

            _context.OptionValues.Update(existingOptionValue);
            await _context.SaveChangesAsync();

            return Ok(existingOptionValue);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteOptionValue(Guid id)
        {
            var optionValue = await _context.OptionValues.FindAsync(id);

            if (optionValue == null)
            {
                return NotFound();
            }

            // Soft delete - set IsActive to false instead of removing from DB
            optionValue.IsActive = false;
            optionValue.LastUpdated = DateTime.UtcNow;
            _context.OptionValues.Update(optionValue);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        [HttpGet("categories")]
        public async Task<ActionResult<IEnumerable<string>>> GetCategories()
        {
            var categories = await _context.OptionValues
                .Where(ov => ov.IsActive)
                .Select(ov => ov.Category)
                .Distinct()
                .OrderBy(c => c)
                .ToListAsync();

            return Ok(categories);
        }

        [HttpGet("paginated")]
        public async Task<ActionResult<PaginatedResponse<OptionValue>>> GetOptionValuesPaginated(
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
            IQueryable<OptionValue> query = _context.OptionValues.AsNoTracking();

            // Apply search filter if provided
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                query = query.Where(ov => 
                    EF.Functions.Like(ov.Category, $"%{searchTerm}%") ||
                    EF.Functions.Like(ov.Value, $"%{searchTerm}%") ||
                    EF.Functions.Like(ov.Description, $"%{searchTerm}%"));
            }

            // Apply active filter only if not explicitly requesting inactive items
            query = query.Where(ov => ov.IsActive);

            // Determine the order to apply (before pagination)
            IOrderedQueryable<OptionValue> orderedQuery = query switch
            {
                _ when sortField?.ToLower() == "category" => sortDirection?.ToLower() == "desc"
                    ? query.OrderByDescending(ov => ov.Category ?? "")
                    : query.OrderBy(ov => ov.Category ?? ""),
                _ when sortField?.ToLower() == "value" => sortDirection?.ToLower() == "desc"
                    ? query.OrderByDescending(ov => ov.Value ?? "")
                    : query.OrderBy(ov => ov.Value ?? ""),
                _ when sortField?.ToLower() == "description" => sortDirection?.ToLower() == "desc"
                    ? query.OrderByDescending(ov => ov.Description ?? "")
                    : query.OrderBy(ov => ov.Description ?? ""),
                _ when sortField?.ToLower() == "isactive" => sortDirection?.ToLower() == "desc"
                    ? query.OrderByDescending(ov => ov.IsActive)
                    : query.OrderBy(ov => ov.IsActive),
                _ => query.OrderBy(ov => ov.Category).ThenBy(ov => ov.Value) // Default sort
            };

            // Get total count of matching records before pagination
            var totalCount = await orderedQuery.CountAsync();

            // Apply pagination (Skip and Take)
            var pagedOptionValues = await orderedQuery
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var response = new PaginatedResponse<OptionValue>
            {
                Data = pagedOptionValues,
                TotalCount = totalCount,
                PageSize = pageSize,
                CurrentPage = page
            };

            return Ok(response);
        }

        [HttpGet("autocomplete")]
        public async Task<ActionResult<IEnumerable<OptionValue>>> GetAutocompleteOptions(
            [FromQuery] string category,
            [FromQuery] string searchTerm,
            [FromQuery] int limit = 10)
        {
            if (string.IsNullOrEmpty(category) || string.IsNullOrEmpty(searchTerm))
            {
                return BadRequest("Category and searchTerm are required.");
            }

            var options = await _context.OptionValues
                .Where(ov => ov.Category.ToLower() == category.ToLower() 
                          && ov.IsActive
                          && (EF.Functions.Like(ov.Value, $"%{searchTerm}%") 
                          || EF.Functions.Like(ov.Description, $"%{searchTerm}%")))
                .OrderBy(ov => ov.Value)
                .Take(limit)
                .ToListAsync();

            return Ok(options);
        }
    }
}