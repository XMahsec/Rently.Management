using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Rently.Management.Domain.Repositories;
using Rently.Management.WebApi.DTOs;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Rently.Management.WebApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IUserRepository _userRepository;
        private readonly IConfiguration _configuration;

        public AuthController(IUserRepository userRepository, IConfiguration configuration)
        {
            _userRepository = userRepository;
            _configuration = configuration;
        }

        [HttpPost("login")]
        [Microsoft.AspNetCore.Authorization.AllowAnonymous]
        public async Task<ActionResult<LoginResponseDto>> Login([FromBody] LoginRequestDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Email))
                return BadRequest();

            var usersPaged = await _userRepository.GetUsersAsync(dto.Email, "All", 1, 1);
            var user = usersPaged.Data.FirstOrDefault(u => (u.Email ?? "").Equals(dto.Email, StringComparison.OrdinalIgnoreCase));
            if (user == null)
                return Unauthorized();

            var adminEmail = _configuration["Admin:Email"] ?? "";
            var adminPassword = _configuration["Admin:Password"] ?? "";
            var providedPassword = dto.Password ?? "";

            if (!user.Email!.Equals(adminEmail, StringComparison.OrdinalIgnoreCase) || providedPassword != adminPassword)
                return Unauthorized();

            var issuer = _configuration["Jwt:Issuer"] ?? "";
            var audience = _configuration["Jwt:Audience"] ?? "";
            var key = _configuration["Jwt:Key"] ?? "";
            var expiresMinutes = int.TryParse(_configuration["Jwt:ExpiresMinutes"], out var m) ? m : 60;

            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, user.Email ?? ""),
                new Claim(ClaimTypes.Name, user.Name ?? ""),
                new Claim(ClaimTypes.Role, user.Role ?? "User")
            };

            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);
            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(expiresMinutes),
                signingCredentials: credentials
            );

            var tokenString = new JwtSecurityTokenHandler().WriteToken(token);
            return Ok(new LoginResponseDto
            {
                Token = tokenString,
                Name = user.Name,
                Email = user.Email,
                Role = user.Role,
                UserId = user.Id
            });
        }
    }
}
