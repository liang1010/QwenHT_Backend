using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QwenHT.Data;
using QwenHT.Models;

namespace QwenHT.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ApplicationDbContext _context;

        public UsersController(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager, ApplicationDbContext context)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<UserDto>>> GetUsers()
        {
            var users = _userManager.Users.ToList();
            var userDtos = new List<UserDto>();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                userDtos.Add(new UserDto
                {
                    Id = user.Id,
                    Email = user.Email,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Roles = roles.ToList(),
                    IsActive = user.IsActive
                });
            }

            return Ok(userDtos);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<UserDto>> GetUser(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            var roles = await _userManager.GetRolesAsync(user);
            var userDto = new UserDto
            {
                Id = user.Id,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Roles = roles.ToList(),
                IsActive = user.IsActive
            };

            return Ok(userDto);
        }

        [HttpPost]
        public async Task<ActionResult<UserDto>> CreateUser(UserDto userDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var user = new ApplicationUser
            {
                UserName = userDto.Email,
                Email = userDto.Email,
                FirstName = userDto.FirstName,
                LastName = userDto.LastName,
                IsActive = userDto.IsActive
            };

            var result = await _userManager.CreateAsync(user);
            if (!result.Succeeded)
            {
                return BadRequest(result.Errors);
            }

            // Add user to roles if specified
            if (userDto.Roles != null && userDto.Roles.Any())
            {
                var roleResult = await _userManager.AddToRolesAsync(user, userDto.Roles);
                if (!roleResult.Succeeded)
                {
                    return BadRequest(roleResult.Errors);
                }
            }

            userDto.Id = user.Id;
            return CreatedAtAction(nameof(GetUser), new { id = user.Id }, userDto);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateUser(string id, UpdateUserDto model)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            user.Email = model.Email;
            user.UserName = model.Email;
            user.FirstName = model.FirstName;
            user.LastName = model.LastName;
            user.IsActive = model.IsActive;

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                return BadRequest(result.Errors);
            }

            // Update roles
            var currentRoles = await _userManager.GetRolesAsync(user);
            await _userManager.RemoveFromRolesAsync(user, currentRoles);

            if (model.Roles != null && model.Roles.Any())
            {
                await _userManager.AddToRolesAsync(user, model.Roles);
            }

            return Ok(user);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUser(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            var result = await _userManager.DeleteAsync(user);
            if (!result.Succeeded)
            {
                return BadRequest(result.Errors);
            }

            return Ok();
        }

        [HttpPost("{id}/change-password")]
        public async Task<IActionResult> ChangePassword(string id, [FromBody] ChangePasswordModel model)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            // If current password is provided, verify it
            if (!string.IsNullOrEmpty(model.CurrentPassword))
            {
                var isValidCurrentPassword = await _userManager.CheckPasswordAsync(user, model.CurrentPassword);
                if (!isValidCurrentPassword)
                {
                    return BadRequest(new { Error = "Current password is incorrect" });
                }
            }

            // Change the password
            var result = await _userManager.RemovePasswordAsync(user);
            if (!result.Succeeded)
            {
                return BadRequest(result.Errors);
            }

            result = await _userManager.AddPasswordAsync(user, model.NewPassword);
            if (!result.Succeeded)
            {
                return BadRequest(result.Errors);
            }

            return Ok(new { Message = "Password changed successfully" });
        }

        [HttpGet("paginated")]
        public async Task<ActionResult<PaginatedResponse<UserDto>>> GetUsersPaginated(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? sortField = null,
            [FromQuery] string? sortDirection = null,
            [FromQuery] string? searchTerm = null)
        {
            // Validate pagination parameters
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 10;
            if (pageSize > 100) pageSize = 100; // Limit page size for performance

            // 1. Build the base query using the DbContext
            // Use AsNoTracking for better performance if you're only reading
            IQueryable<ApplicationUser> query = _context.Users.AsNoTracking();


            // --- NEW CODE (Database-Translated) ---
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                query = query.Where(u =>
                    EF.Functions.Like(u.Email, $"%{searchTerm}%") ||
                    EF.Functions.Like(u.FirstName, $"%{searchTerm}%") ||
                    EF.Functions.Like(u.LastName, $"%{searchTerm}%")
                );
            }

            // 3. Determine the order to apply (before pagination)
            // Default sorting
            IOrderedQueryable<ApplicationUser> orderedQuery = query switch
            {
                _ when sortField?.ToLower() == "email" => sortDirection?.ToLower() == "desc"
                    ? query.OrderByDescending(u => u.Email)
                    : query.OrderBy(u => u.Email),
                _ when sortField?.ToLower() == "firstname" => sortDirection?.ToLower() == "desc"
                    ? query.OrderByDescending(u => u.FirstName ?? "") // Handle potential nulls
                    : query.OrderBy(u => u.FirstName ?? ""),
                _ when sortField?.ToLower() == "lastname" => sortDirection?.ToLower() == "desc"
                    ? query.OrderByDescending(u => u.LastName ?? "") // Handle potential nulls
                    : query.OrderBy(u => u.LastName ?? ""),
                _ when sortField?.ToLower() == "isactive" => sortDirection?.ToLower() == "desc"
                    ? query.OrderByDescending(u => u.IsActive)
                    : query.OrderBy(u => u.IsActive),
                _ => query.OrderBy(u => u.Email) // Default sort
            };

            // 4. Get total count of matching records *before* pagination
            var totalCount = await orderedQuery.CountAsync();

            // 5. Apply pagination (Skip and Take)
            var pagedUsers = await orderedQuery
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(); // Execute the query with pagination applied

            // 6. Convert to DTOs
            // Note: Getting roles for each user individually is still potentially slow.
            // Consider optimizing role retrieval if necessary (see comments below).
            var userDtos = new List<UserDto>();
            foreach (var user in pagedUsers)
            {
                // This still makes N database calls for roles (one per user on the page)
                // For better performance with roles, consider a single query joining Users and Roles
                // or fetching all role assignments for the paged user IDs in one go.
                var roles = await _userManager.GetRolesAsync(user);
                userDtos.Add(new UserDto
                {
                    Id = user.Id,
                    Email = user.Email,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Roles = roles.ToList(),
                    IsActive = user.IsActive
                });
            }

            var response = new PaginatedResponse<UserDto>
            {
                Data = userDtos,
                TotalCount = totalCount,
                PageSize = pageSize,
                CurrentPage = page
            };

            return Ok(response);
        }

        public class UpdateUserDto
        {
            public string? Email { get; set; }
            public string? FirstName { get; set; }
            public string? LastName { get; set; }
            public bool IsActive { get; set; }
            public string[]? Roles { get; set; }
        }

        public class ChangePasswordModel
        {
            public string? CurrentPassword { get; set; }
            public string? NewPassword { get; set; } = string.Empty;
            public string? ConfirmNewPassword { get; set; }
        }
    }
}