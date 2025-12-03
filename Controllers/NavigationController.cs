using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using QwenHT.Models;
using QwenHT.Services.Navigation;

namespace QwenHT.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Policy = "NavigationAccess")] // Use custom policy for navigation access
    public class NavigationController(INavigationService _navigationService, UserManager<ApplicationUser> _userManager) : ControllerBase
    {

        [HttpGet]
        public async Task<ActionResult<IEnumerable<NavigationItem>>> GetNavigationItems()
        {
            var items = await _navigationService.GetAllNavigationItemsAsync();
            return Ok(items);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<NavigationItem>> GetNavigationItem(Guid id)
        {
            var item = await _navigationService.GetNavigationItemAsync(id);
            if (item == null)
            {
                return NotFound();
            }

            return Ok(item);
        }

        [HttpPost]
        public async Task<ActionResult<NavigationItem>> CreateNavigationItem([FromBody] NavigationItem item)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Get current user from JWT claims
            var username = User?.Claims?.FirstOrDefault(c => c.Type == "username")?.Value ?? "Unknown";

            // Set audit fields
            item.Id = Guid.NewGuid();
            item.CreatedBy = username;
            item.CreatedAt = DateTime.UtcNow;
            item.LastUpdated = DateTime.UtcNow;
            item.LastModifiedBy = username;

            var createdItem = await _navigationService.CreateNavigationItemAsync(item);
            return CreatedAtAction(nameof(CreateNavigationItem), new { id = createdItem.Id }, createdItem);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateNavigationItem(Guid id, [FromBody] NavigationItem item)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Verify the ID in the URL matches the ID in the item
            if (id != item.Id)
            {
                return BadRequest("Navigation item ID mismatch");
            }

            // Get current user from JWT claims
            var username = User?.Claims?.FirstOrDefault(c => c.Type == "username")?.Value ?? "Unknown";

            // Set audit fields for update
            item.LastUpdated = DateTime.UtcNow;
            item.LastModifiedBy = username;

            var updatedItem = await _navigationService.UpdateNavigationItemAsync(id, item);
            if (updatedItem == null)
            {
                return NotFound();
            }

            return Ok(updatedItem);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteNavigationItem(Guid id)
        {
            var result = await _navigationService.DeleteNavigationItemAsync(id);
            if (!result)
            {
                return NotFound();
            }

            return NoContent();
        }

        [HttpPost("{navigationId}/roles/{roleName}")]
        public async Task<IActionResult> AssignRoleToNavigation(Guid navigationId, string roleName)
        {
            var result = await _navigationService.AssignRoleToNavigationAsync(navigationId, roleName);
            if (!result)
            {
                return BadRequest("Failed to assign role to navigation");
            }

            return Ok();
        }

        [HttpDelete("{navigationId}/roles/{roleName}")]
        public async Task<IActionResult> RemoveRoleFromNavigation(Guid navigationId, string roleName)
        {
            var result = await _navigationService.RemoveRoleFromNavigationAsync(navigationId, roleName);
            if (!result)
            {
                return BadRequest("Failed to remove role from navigation");
            }

            return Ok();
        }
    }
}