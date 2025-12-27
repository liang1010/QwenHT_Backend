using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using QwenHT.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace QwenHT.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AccountController(UserManager<ApplicationUser> _userManager, IConfiguration _configuration) : ControllerBase
    {
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginModel model)
        {
            var user = await _userManager.FindByNameAsync(model.Username);
            if (user != null && await _userManager.CheckPasswordAsync(user, model.Password))
            {
                var userRoles = await _userManager.GetRolesAsync(user);
                var authClaims = new List<Claim>
                {
                    new Claim("username", user.UserName ?? ""),
                    new Claim("email", user.Email ?? ""),
                    new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                    new Claim(ClaimTypes.Name, user.UserName ?? ""),
                };

                foreach (var userRole in userRoles)
                {
                    authClaims.Add(new Claim("roles", userRole));
                }

                var token = GetToken(authClaims);
                var refreshToken = GenerateRefreshToken();

                // Store refresh token in the database
                user.RefreshToken = refreshToken;
                user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7); // Refresh token expires in 7 days

                var result = await _userManager.UpdateAsync(user);

                if (!result.Succeeded)
                {
                    return StatusCode(500, "Error updating user with refresh token");
                }

                return Ok(new
                {
                    token = new JwtSecurityTokenHandler().WriteToken(token),
                    expiration = token.ValidTo,
                    refreshToken = refreshToken
                });
            }
            return Unauthorized();
        }

        [HttpPost("refresh-token")]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequestDto model)
        {
            if (model.RefreshToken == null)
            {
                return BadRequest("Refresh token is required");
            }

            var user = await _userManager.Users
                .FirstOrDefaultAsync(u =>
                u.RefreshToken == model.RefreshToken &&
                u.RefreshTokenExpiryTime > DateTime.UtcNow);

            if (user == null)
            {
                return Unauthorized("Invalid refresh token");
            }

            var userRoles = await _userManager.GetRolesAsync(user);
            var authClaims = new List<Claim>
            {
                new Claim("username", user.UserName ?? ""),
                new Claim("email", user.Email ?? ""),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(ClaimTypes.Name, user.UserName ?? ""),
            };

            foreach (var userRole in userRoles)
            {
                authClaims.Add(new Claim("roles", userRole));
            }

            var token = GetToken(authClaims);
            var newRefreshToken = GenerateRefreshToken();

            // Update refresh token in the database
            user.RefreshToken = newRefreshToken;
            user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7); // Refresh token expires in 7 days

            var result = await _userManager.UpdateAsync(user);

            if (!result.Succeeded)
            {
                return StatusCode(500, "Error updating user with new refresh token");
            }

            return Ok(new
            {
                token = new JwtSecurityTokenHandler().WriteToken(token),
                expiration = token.ValidTo,
                refreshToken = newRefreshToken
            });
        }

        private string GenerateRefreshToken()
        {
            var randomNumber = new byte[32];
            using var rng = System.Security.Cryptography.RandomNumberGenerator.Create();
            rng.GetBytes(randomNumber);
            return Convert.ToBase64String(randomNumber);
        }

        private JwtSecurityToken GetToken(List<Claim> authClaims)
        {
            var authSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["JWT:Secret"]));

            var token = new JwtSecurityToken(
                issuer: _configuration["JWT:ValidIssuer"],
                audience: _configuration["JWT:ValidAudience"],
                expires: DateTime.Now.AddMinutes(1),
                claims: authClaims,
                signingCredentials: new SigningCredentials(authSigningKey, SecurityAlgorithms.HmacSha256)
                );

            return token;
        }

        [HttpPost("logout")]
        public IActionResult Logout()
        {
            return Ok(new { message = "Logged out successfully" });
        }
    }

    public class LoginModel
    {
        public string? Username { get; set; }
        public string? Password { get; set; }
    }

    public class RegisterModel
    {
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Username { get; set; }
        public string? Email { get; set; }
        public string? Password { get; set; }
        public string? Role { get; set; }
    }

    public class Response
    {
        public string? Status { get; set; }
        public string? Message { get; set; }
    }

    public class RefreshTokenRequestDto
    {
        public string? RefreshToken { get; set; }
    }
}