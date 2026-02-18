using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rently.Management.WebApi.DTOs;
using Rently.Management.WebApi;
using Rently.Management.WebApi.Services;

namespace Rently.Management.WebApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AccountController : ControllerBase
    {
        /// <summary>
        /// Admin account management: change name/password, request/reset password, add admin.
        /// </summary>
        private readonly ApplicationDbContext _context;
        private readonly PasswordService _passwordService;

        public AccountController(ApplicationDbContext context, PasswordService passwordService)
        {
            _context = context;
            _passwordService = passwordService;
        }

        [HttpPost("change-name")]
        [Authorize]
        /// <summary>
        /// Update current user's display name using the JWT 'sub' claim.
        /// </summary>
        public async Task<IActionResult> ChangeName([FromBody] ChangeNameDto dto)
        {
            var sub = User.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value;
            if (!int.TryParse(sub, out var userId)) return Unauthorized();
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return NotFound();
            user.Name = dto.Name;
            user.UpdatedAt = DateTime.UtcNow;
            _context.Users.Update(user);
            await _context.SaveChangesAsync();
            return NoContent();
        }

        [HttpPost("change-password")]
        [Authorize]
        /// <summary>
        /// Change password after verifying the current one; store new hash and salt.
        /// </summary>
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
        {
            var sub = User.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value;
            if (!int.TryParse(sub, out var userId)) return Unauthorized();
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return NotFound();
            if (string.IsNullOrEmpty(user.PasswordHash) || string.IsNullOrEmpty(user.PasswordSalt)) return BadRequest();
            var ok = _passwordService.Verify(dto.CurrentPassword, user.PasswordHash!, user.PasswordSalt!);
            if (!ok) return Unauthorized();
            var pair = _passwordService.HashPassword(dto.NewPassword);
            user.PasswordHash = pair.hash;
            user.PasswordSalt = pair.salt;
            user.UpdatedAt = DateTime.UtcNow;
            _context.Users.Update(user);
            await _context.SaveChangesAsync();
            return NoContent();
        }

        [HttpPost("request-reset")]
        [AllowAnonymous]
        /// <summary>
        /// Request password reset: create a temporary token valid for 30 minutes.
        /// </summary>
        public async Task<IActionResult> RequestReset([FromBody] RequestResetDto dto, [FromQuery] bool debug = false)
        {
            var email = dto.Email.Trim().ToLowerInvariant();
            var user = _context.Users.FirstOrDefault(u => (u.Email ?? "").ToLower() == email);
            if (user == null) return NoContent();
            var token = Convert.ToBase64String(Guid.NewGuid().ToByteArray());
            user.PasswordResetToken = token;
            user.PasswordResetTokenExpires = DateTime.UtcNow.AddMinutes(30);
            user.UpdatedAt = DateTime.UtcNow;
            _context.Users.Update(user);
            await _context.SaveChangesAsync();
            if (debug)
            {
                var env = HttpContext.RequestServices.GetRequiredService<IWebHostEnvironment>();
                if (string.Equals(env.EnvironmentName, "Development", StringComparison.OrdinalIgnoreCase))
                {
                    return Ok(new { message = "Reset requested", dev_token = token });
                }
            }
            return Ok(new { message = "Reset requested" });
        }

        [HttpPost("reset-password")]
        [AllowAnonymous]
        /// <summary>
        /// Reset password using the temporary token, then clear it after update.
        /// </summary>
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto dto)
        {
            var email = dto.Email.Trim().ToLowerInvariant();
            var user = _context.Users.FirstOrDefault(u => (u.Email ?? "").ToLower() == email);
            if (user == null) return NotFound();
            if (user.PasswordResetToken != dto.Token || user.PasswordResetTokenExpires == null || user.PasswordResetTokenExpires < DateTime.UtcNow)
                return Unauthorized();
            var pair = _passwordService.HashPassword(dto.NewPassword);
            user.PasswordHash = pair.hash;
            user.PasswordSalt = pair.salt;
            user.PasswordResetToken = null;
            user.PasswordResetTokenExpires = null;
            user.UpdatedAt = DateTime.UtcNow;
            _context.Users.Update(user);
            await _context.SaveChangesAsync();
            return NoContent();
        }

        [HttpPost("add-admin")]
        [Authorize]
        /// <summary>
        /// Add a new admin user (requires Role=Admin in the JWT).
        /// </summary>
        public async Task<IActionResult> AddAdmin([FromBody] AddAdminDto dto)
        {
            var role = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value ?? "";
            if (!string.Equals(role, "Admin", StringComparison.OrdinalIgnoreCase)) return Forbid();
            // Only Super Admin (configured Admin:Email) can add another admin
            var actorEmail = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value ?? "";
            var superAdminEmail = (_context.Database != null) // just to avoid nullables; config below
                ? HttpContext.RequestServices.GetRequiredService<IConfiguration>()["Admin:Email"] ?? ""
                : "";
            if (!actorEmail.Equals(superAdminEmail, StringComparison.OrdinalIgnoreCase))
                return Forbid();
            var email = dto.Email.Trim().ToLowerInvariant();
            var exists = _context.Users.Any(u => (u.Email ?? "").ToLower() == email);
            if (exists) return Conflict();
            var pair = _passwordService.HashPassword(dto.Password);
            var user = new Rently.Management.Domain.Entities.User
            {
                Name = dto.Name,
                Email = dto.Email,
                Phone = dto.Phone,
                Role = "Admin",
                ApprovalStatus = "Approved",
                PasswordHash = pair.hash,
                PasswordSalt = pair.salt,
                CreatedAt = DateTime.UtcNow
            };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(AddAdmin), new { id = user.Id }, new { id = user.Id });
        }
    }
}
