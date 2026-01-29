using Rently.Management.Domain.Common;

namespace Rently.Management.Domain.Entities;

public class User : AuditableEntity
{
    public int Id { get; set; }                     // PK

    public string? Name { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Role { get; set; }               // "Owner", "Renter", "Admin" أو enum
    public string? Nationality { get; set; }
    public string? PreferredLanguage { get; set; }
    public string? LicenseNumber { get; set; }
    public string? ApprovalStatus { get; set; }     // "Pending", "Approved", "Rejected" ...
    public string? PayoutMethod { get; set; }
    public string? PayoutDetails { get; set; }
    public string? BillingCountry { get; set; }
    public string? ZipCode { get; set; }

    // صور التحقق (غالباً مسارات أو URLs)
    public string? IdImage { get; set; }
    public string? LicenseImage { get; set; }
    public string? PassportImage { get; set; }
    public string? SelfieImage { get; set; }
    public string? ResidenceProofImage { get; set; }
    public string? JobProofImage { get; set; }

    // Navigation properties (العلاقات)
    public List<Car> OwnedCars { get; set; } = new();
    public List<Booking> BookingsAsRenter { get; set; } = new();
    public List<Review> Reviews { get; set; } = new();
    public List<Notification> Notifications { get; set; } = new();
    public List<Favorite> Favorites { get; set; } = new();
    public List<Message> SentMessages { get; set; } = new();
    public List<Message> ReceivedMessages { get; set; } = new();
    public List<Otp> Otps { get; set; } = new();
    public List<Payment> Payments { get; set; } = new();
}