using System.ComponentModel.DataAnnotations;

namespace Rently.Management.WebApi.DTOs
{
    public class ChangeNameDto
    {
        [Required]
        [StringLength(150)]
        public string Name { get; set; } = "";
    }

    public class ChangePasswordDto
    {
        [Required]
        [MinLength(6)]
        public string CurrentPassword { get; set; } = "";
        [Required]
        [MinLength(6)]
        public string NewPassword { get; set; } = "";
    }

    public class RequestResetDto
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = "";
    }

    public class ResetPasswordDto
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = "";
        [Required]
        public string Token { get; set; } = "";
        [Required]
        [MinLength(6)]
        public string NewPassword { get; set; } = "";
    }

    public class AddAdminDto
    {
        [Required]
        [StringLength(150)]
        public string Name { get; set; } = "";
        [Required]
        [EmailAddress]
        [StringLength(255)]
        public string Email { get; set; } = "";
        [Required]
        [StringLength(30)]
        public string Phone { get; set; } = "";
        [Required]
        [MinLength(6)]
        public string Password { get; set; } = "";
    }
}
