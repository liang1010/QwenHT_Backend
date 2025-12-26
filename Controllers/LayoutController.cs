using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using QwenHT.Models;
using QwenHT.Services;

namespace QwenHT.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class LayoutController(UserManager<ApplicationUser> _userManager, INavigationService _navigationService) : ControllerBase
    {
        [HttpGet("user")]
        [Authorize]
        public async Task<ActionResult<IEnumerable<NavigationItem>>> GetUserNavigation()
        {
            var username = User.Claims.FirstOrDefault(c => c.Type == "username")?.Value;
            var user = await _userManager.FindByNameAsync(username);
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
