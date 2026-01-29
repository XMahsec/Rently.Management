
namespace Rently.Management.Domain.Entities;

public class Otp
{
    public int Id { get; set; }

    public int UserId { get; set; }
    public string? OtpHash { get; set; }            // hashed OTP للأمان

    public User? User { get; set; }

    // ممكن تضيف: DateTime CreatedAt, DateTime ExpiresAt, bool IsUsed
}