using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using QwenHT.Models;

namespace QwenHT.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Policy = "NavigationAccess")] // Use custom policy based on navigation permissions
    public class RolesController(RoleManager<IdentityRole> _roleManager, UserManager<ApplicationUser> _userManager) : ControllerBase
    {
        [HttpGet]
        public async Task<ActionResult<IEnumerable<RoleDto>>> GetRoles()
        {
            var roles = _roleManager.Roles.Select(r => new RoleDto
            {
                Id = r.Id,
                Name = r.Name
            }).ToList();

            return Ok(roles);
        }

        [HttpPost]
        public async Task<IActionResult> CreateRole([FromBody] CreateRoleDto model)
        {
            if (string.IsNullOrEmpty(model.Name))
            {
                return BadRequest("Role name is required.");
            }

            var role = new IdentityRole
            {
                Name = model.Name
            };

            var result = await _roleManager.CreateAsync(role);
            if (!result.Succeeded)
            {
                return BadRequest(result.Errors);
            }

            return Ok(new { Message = "Role created successfully" });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteRole(string id)
        {
            var role = await _roleManager.FindByIdAsync(id);
            if (role == null)
            {
                return NotFound();
            }

            // Check if role is in use before deleting
            var usersInRole = await _userManager.GetUsersInRoleAsync(role.Name);
            if (usersInRole.Count > 0)
            {
                return BadRequest("Cannot delete role that is currently assigned to users.");
            }

            var result = await _roleManager.DeleteAsync(role);
            if (!result.Succeeded)
            {
                return BadRequest(result.Errors);
            }

            return Ok(new { Message = "Role deleted successfully" });
        }
    }

    public class RoleDto
    {
        public string? Id { get; set; }
        public string? Name { get; set; }
    }

    public class CreateRoleDto
    {
        public string? Name { get; set; }
    }
}