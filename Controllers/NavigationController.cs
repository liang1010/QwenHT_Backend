using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using QwenHT.Models;
using QwenHT.Services.Navigation;

namespace QwenHT.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class NavigationController : ControllerBase
    {
        private readonly INavigationService _navigationService;
        private readonly UserManager<ApplicationUser> _userManager;

        public NavigationController(INavigationService navigationService, UserManager<ApplicationUser> userManager)
        {
            _navigationService = navigationService;
            _userManager = userManager;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<NavigationItem>>> GetNavigationItems()
        {
            var items = await _navigationService.GetAllNavigationItemsAsync();
            return Ok(items);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<NavigationItem>> GetNavigationItem(int id)
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
            var createdItem = await _navigationService.CreateNavigationItemAsync(item);
            return CreatedAtAction(nameof(GetNavigationItem), new { id = createdItem.Id }, createdItem);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateNavigationItem(int id, [FromBody] NavigationItem item)
        {
            var updatedItem = await _navigationService.UpdateNavigationItemAsync(id, item);
            if (updatedItem == null)
            {
                return NotFound();
            }

            return Ok(updatedItem);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteNavigationItem(int id)
        {
            var result = await _navigationService.DeleteNavigationItemAsync(id);
            if (!result)
            {
                return NotFound();
            }

            return NoContent();
        }

        [HttpPost("{navigationId}/roles/{roleName}")]
        public async Task<IActionResult> AssignRoleToNavigation(int navigationId, string roleName)
        {
            var result = await _navigationService.AssignRoleToNavigationAsync(navigationId, roleName);
            if (!result)
            {
                return BadRequest("Failed to assign role to navigation");
            }

            return Ok();
        }

        [HttpDelete("{navigationId}/roles/{roleName}")]
        public async Task<IActionResult> RemoveRoleFromNavigation(int navigationId, string roleName)
        {
            var result = await _navigationService.RemoveRoleFromNavigationAsync(navigationId, roleName);
            if (!result)
            {
                return BadRequest("Failed to remove role from navigation");
            }

            return Ok();
        }
    }

    [ApiController]
    [Route("api/navigation")]
    public class UserNavigationController : ControllerBase
    {
        private readonly INavigationService _navigationService;
        private readonly UserManager<ApplicationUser> _userManager;

        public UserNavigationController(INavigationService navigationService, UserManager<ApplicationUser> userManager)
        {
            _navigationService = navigationService;
            _userManager = userManager;
        }

        [HttpGet("user")]
        [Authorize]
        public async Task<ActionResult<IEnumerable<NavigationItem>>> GetUserNavigation()
        {
            var user = await _userManager.FindByNameAsync(User.Identity.Name);
            if (user == null)
            {
                return Unauthorized();
            }

            var userRoles = await _userManager.GetRolesAsync(user);
            var navigationItems = await _navigationService.GetNavigationForUserAsync(user.Id, userRoles);

            return Ok(navigationItems);
        }
    }
}