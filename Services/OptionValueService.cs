using Microsoft.EntityFrameworkCore;
using QwenHT.Data;
using QwenHT.Models;

namespace QwenHT.Services
{
    public interface IOptionValueService
    {
        Task<List<OptionValue>> GetOptionsByCategoryAsync(string category, string? searchTerm = null, bool includeInactive = false);
        Task<List<OptionValue>> GetAutocompleteOptionsAsync(string category, string searchTerm, int limit = 10);
        Task<OptionValue?> GetOptionByIdAsync(Guid id);
        Task<OptionValue> CreateOptionAsync(OptionValue optionValue);
        Task<OptionValue?> UpdateOptionAsync(Guid id, OptionValue optionValue);
        Task<bool> DeleteOptionAsync(Guid id);
        Task<List<string>> GetAllCategoriesAsync();
    }

    public class OptionValueService : IOptionValueService
    {
        private readonly ApplicationDbContext _context;

        public OptionValueService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<OptionValue>> GetOptionsByCategoryAsync(string category, string? searchTerm = null, bool includeInactive = false)
        {
            var query = _context.OptionValues.AsQueryable();

            query = query.Where(o => o.Category.ToLower() == category.ToLower());

            if (!string.IsNullOrEmpty(searchTerm))
            {
                query = query.Where(o => EF.Functions.Like(o.Value, $"%{searchTerm}%") || 
                                         EF.Functions.Like(o.Description, $"%{searchTerm}%"));
            }

            if (!includeInactive)
            {
                query = query.Where(o => o.IsActive);
            }

            return await query
                .OrderBy(o => o.Value)
                .ToListAsync();
        }

        public async Task<List<OptionValue>> GetAutocompleteOptionsAsync(string category, string searchTerm, int limit = 10)
        {
            return await _context.OptionValues
                .Where(o => o.Category.ToLower() == category.ToLower() 
                         && o.IsActive
                         && (EF.Functions.Like(o.Value, $"%{searchTerm}%") 
                         || EF.Functions.Like(o.Description, $"%{searchTerm}%")))
                .OrderBy(o => o.Value)
                .Take(limit)
                .ToListAsync();
        }

        public async Task<OptionValue?> GetOptionByIdAsync(Guid id)
        {
            return await _context.OptionValues.FindAsync(id);
        }

        public async Task<OptionValue> CreateOptionAsync(OptionValue optionValue)
        {
            // Check if this combination of category and value already exists
            var existing = await _context.OptionValues
                .FirstOrDefaultAsync(o => o.Category.ToLower() == optionValue.Category.ToLower() 
                                       && o.Value.ToLower() == optionValue.Value.ToLower());
            
            if (existing != null)
            {
                throw new ArgumentException($"A value with category '{optionValue.Category}' and value '{optionValue.Value}' already exists.");
            }

            optionValue.Id = Guid.NewGuid();
            optionValue.CreatedAt = DateTime.UtcNow;
            optionValue.LastUpdated = DateTime.UtcNow;

            _context.OptionValues.Add(optionValue);
            await _context.SaveChangesAsync();

            return optionValue;
        }

        public async Task<OptionValue?> UpdateOptionAsync(Guid id, OptionValue optionValue)
        {
            var existingOptionValue = await _context.OptionValues.FindAsync(id);

            if (existingOptionValue == null)
            {
                return null;
            }

            // Check if this combination of category and value already exists for another record
            var duplicateCheck = await _context.OptionValues
                .FirstOrDefaultAsync(o => o.Id != id 
                                       && o.Category.ToLower() == optionValue.Category.ToLower() 
                                       && o.Value.ToLower() == optionValue.Value.ToLower());
            
            if (duplicateCheck != null)
            {
                throw new ArgumentException($"A value with category '{optionValue.Category}' and value '{optionValue.Value}' already exists.");
            }

            existingOptionValue.Category = optionValue.Category;
            existingOptionValue.Value = optionValue.Value;
            existingOptionValue.Description = optionValue.Description;
            existingOptionValue.IsActive = optionValue.IsActive;
            existingOptionValue.LastUpdated = DateTime.UtcNow;

            _context.OptionValues.Update(existingOptionValue);
            await _context.SaveChangesAsync();

            return existingOptionValue;
        }

        public async Task<bool> DeleteOptionAsync(Guid id)
        {
            var optionValue = await _context.OptionValues.FindAsync(id);

            if (optionValue == null)
            {
                return false;
            }

            _context.OptionValues.Remove(optionValue);
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<List<string>> GetAllCategoriesAsync()
        {
            return await _context.OptionValues
                .Where(o => o.IsActive)
                .Select(o => o.Category)
                .Distinct()
                .OrderBy(c => c)
                .ToListAsync();
        }
    }
}