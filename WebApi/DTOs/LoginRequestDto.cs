using System.ComponentModel.DataAnnotations;
namespace Rently.Management.WebApi.DTOs
{
    public class LoginRequestDto
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = "";
        [Required]
        [MinLength(6)]
        public string Password { get; set; } = "";
        public string? Role { get; set; }
    }
}
