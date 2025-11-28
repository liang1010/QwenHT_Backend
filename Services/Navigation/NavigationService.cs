using Microsoft.EntityFrameworkCore;
using QwenHT.Data;
using QwenHT.Models;

namespace QwenHT.Services.Navigation
{
    public interface INavigationService
    {
        Task<List<NavigationItem>> GetNavigationForUserAsync(string userId, IList<string> userRoles);
        Task<List<NavigationItem>> GetAllNavigationItemsAsync();
        Task<NavigationItem?> GetNavigationItemAsync(Guid id);
        Task<NavigationItem> CreateNavigationItemAsync(NavigationItem item);
        Task<NavigationItem?> UpdateNavigationItemAsync(Guid id, NavigationItem item);
        Task<bool> DeleteNavigationItemAsync(Guid id);
        Task<bool> AssignRoleToNavigationAsync(Guid navigationId, string roleName);
        Task<bool> RemoveRoleFromNavigationAsync(Guid navigationId, string roleName);
    }

    public class NavigationService : INavigationService
    {
        private readonly ApplicationDbContext _context;

        public NavigationService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<NavigationItem>> GetNavigationForUserAsync(string userId, IList<string> userRoles)
        {
            var roleNames = userRoles.Select(r => r.ToLower()).ToList();

            var navigationItems = await _context.NavigationItems
                .Include(n => n.Children)
                    .ThenInclude(c => c.RoleNavigations)
                .Include(n => n.RoleNavigations)
                .Where(n => n.IsVisible && n.ParentId == null) // Only top-level items
                .ToListAsync();

            var accessibleItems = navigationItems
                .Where(item => item.RoleNavigations?.Any(rn => roleNames.Contains(rn.RoleName.ToLower())) == true)
                .ToList();

            // Process children
            foreach (var item in accessibleItems)
            {
                item.Children = item.Children
                    .Where(child => child.IsVisible &&
                           child.RoleNavigations?.Any(rn => roleNames.Contains(rn.RoleName.ToLower())) == true)
                    .ToList();
            }

            return accessibleItems
                .OrderBy(n => n.Order)
                .ToList();
        }

        public async Task<List<NavigationItem>> GetAllNavigationItemsAsync()
        {
            return await _context.NavigationItems
                .Include(n => n.Children)
                    .ThenInclude(c => c.RoleNavigations)
                .Include(n => n.RoleNavigations)
                .OrderBy(n => n.Order)
                .ToListAsync();
        }

        public async Task<NavigationItem?> GetNavigationItemAsync(Guid id)
        {
            return await _context.NavigationItems
                .Include(n => n.Children)
                    .ThenInclude(c => c.RoleNavigations)
                .Include(n => n.RoleNavigations)
                .FirstOrDefaultAsync(n => n.Id == id);
        }

        public async Task<NavigationItem> CreateNavigationItemAsync(NavigationItem item)
        {
            if (item.Id == null)
            {
                item.Id = Guid.NewGuid();
            }
            _context.NavigationItems.Add(item);
            await _context.SaveChangesAsync();
            return item;
        }

        public async Task<NavigationItem?> UpdateNavigationItemAsync(Guid id, NavigationItem item)
        {
            var existingItem = await _context.NavigationItems.FindAsync(id);
            if (existingItem == null)
                return null;

            existingItem.Name = item.Name;
            existingItem.Icon = item.Icon;
            existingItem.Route = item.Route;
            existingItem.ParentId = item.ParentId;
            existingItem.Order = item.Order;
            existingItem.IsVisible = item.IsVisible;
            existingItem.LastUpdated = item.LastUpdated;
            existingItem.LastModifiedBy = item.LastModifiedBy;

            await _context.SaveChangesAsync();
            return existingItem;
        }

        public async Task<bool> DeleteNavigationItemAsync(Guid id)
        {
            var item = await _context.NavigationItems.FindAsync(id);
            if (item == null)
                return false;

            // Remove associated role navigations
            var roleNavigations = _context.RoleNavigations.Where(rn => rn.NavigationItemId == id);
            _context.RoleNavigations.RemoveRange(roleNavigations);

            _context.NavigationItems.Remove(item);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> AssignRoleToNavigationAsync(Guid navigationId, string roleName)
        {
            var existing = await _context.RoleNavigations
                .FirstOrDefaultAsync(rn => rn.NavigationItemId == navigationId && rn.RoleName == roleName);

            if (existing != null)
                return true; // Already assigned

            var roleNav = new RoleNavigation
            {
                NavigationItemId = navigationId,
                RoleName = roleName
            };

            _context.RoleNavigations.Add(roleNav);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> RemoveRoleFromNavigationAsync(Guid navigationId, string roleName)
        {
            var roleNav = await _context.RoleNavigations
                .FirstOrDefaultAsync(rn => rn.NavigationItemId == navigationId && rn.RoleName == roleName);

            if (roleNav == null)
                return false;

            _context.RoleNavigations.Remove(roleNav);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}