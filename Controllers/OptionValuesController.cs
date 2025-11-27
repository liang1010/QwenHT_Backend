using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QwenHT.Data;
using QwenHT.Models;

namespace QwenHT.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OptionValuesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public OptionValuesController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<OptionValue>>> GetOptionValues(
            [FromQuery] string? category = null,
            [FromQuery] string? searchTerm = null,
            [FromQuery] bool includeInactive = false)
        {
            var query = _context.OptionValues.AsQueryable();

            if (!string.IsNullOrEmpty(category))
            {
                query = query.Where(o => o.Category.ToLower() == category.ToLower());
            }

            if (!string.IsNullOrEmpty(searchTerm))
            {
                query = query.Where(o => EF.Functions.Like(o.Value, $"%{searchTerm}%") || 
                                         EF.Functions.Like(o.Description, $"%{searchTerm}%"));
            }

            if (!includeInactive)
            {
                query = query.Where(o => o.IsActive);
            }

            var optionValues = await query
                .OrderBy(o => o.Category)
                .ThenBy(o => o.Value)
                .ToListAsync();

            return Ok(optionValues);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<OptionValue>> GetOptionValue(Guid id)
        {
            var optionValue = await _context.OptionValues.FindAsync(id);

            if (optionValue == null)
            {
                return NotFound();
            }

            return Ok(optionValue);
        }

        [HttpPost]
        public async Task<ActionResult<OptionValue>> CreateOptionValue(OptionValue optionValue)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Check if this combination of category and value already exists
            var existing = await _context.OptionValues
                .FirstOrDefaultAsync(o => o.Category.ToLower() == optionValue.Category.ToLower() 
                                       && o.Value.ToLower() == optionValue.Value.ToLower());
            
            if (existing != null)
            {
                return BadRequest("A value with this category and value already exists.");
            }

            optionValue.Id = Guid.NewGuid();
            optionValue.CreatedAt = DateTime.UtcNow;
            optionValue.LastUpdated = DateTime.UtcNow;

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

            // Check if this combination of category and value already exists for another record
            var duplicateCheck = await _context.OptionValues
                .FirstOrDefaultAsync(o => o.Id != id 
                                       && o.Category.ToLower() == optionValue.Category.ToLower() 
                                       && o.Value.ToLower() == optionValue.Value.ToLower());
            
            if (duplicateCheck != null)
            {
                return BadRequest("A value with this category and value already exists.");
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

            _context.OptionValues.Remove(optionValue);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        [HttpGet("categories")]
        public async Task<ActionResult<IEnumerable<string>>> GetCategories()
        {
            var categories = await _context.OptionValues
                .Where(o => o.IsActive)
                .Select(o => o.Category)
                .Distinct()
                .OrderBy(c => c)
                .ToListAsync();

            return Ok(categories);
        }

        [HttpGet("autocomplete")]
        public async Task<ActionResult<IEnumerable<OptionValue>>> GetAutocompleteOptions(
            [FromQuery] string category,
            [FromQuery] string searchTerm,
            [FromQuery] int limit = 10)
        {
            if (string.IsNullOrEmpty(category) || string.IsNullOrEmpty(searchTerm))
            {
                return BadRequest("Category and searchTerm are required");
            }

            var options = await _context.OptionValues
                .Where(o => o.Category.ToLower() == category.ToLower() 
                         && o.IsActive
                         && (EF.Functions.Like(o.Value, $"%{searchTerm}%") 
                         || EF.Functions.Like(o.Description, $"%{searchTerm}%")))
                .OrderBy(o => o.Value)
                .Take(limit)
                .ToListAsync();

            return Ok(options);
        }
    }
}