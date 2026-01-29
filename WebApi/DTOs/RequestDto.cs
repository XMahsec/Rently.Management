namespace Rently.Management.WebApi.DTOs;

public class RequestDto
{
    public int Id { get; set; }
    public string Type { get; set; } = "";          // "Owner verification", "Car listing"
    public string SubmittedBy { get; set; } = "";
    public DateTime SubmittedOn { get; set; }
    public decimal TotalPrice { get; set; }
    public string Status { get; set; } = "";        // "Pending", "Approved", "Rejected"
}

public class UpdateRequestStatusDto
{
    public string Type { get; set; } = "";          // which bucket (owner / car)
    public string Status { get; set; } = "";        // "Approved", "Rejected", "Pending"
}

public class RequestDetailsDto
{
    public int Id { get; set; }
    public string Type { get; set; } = "";

    // User info
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
    public string PhoneNumber { get; set; } = "";

    // Request meta
    public DateTime SubmittedOn { get; set; }
    public string Status { get; set; } = "";

    // Extra details (for activation request)
    public string? DrivingLicenseNumber { get; set; }
    public string? IdNumber { get; set; }

    // Documents URLs
    public string? IdImageUrl { get; set; }
    public string? LicenseImageUrl { get; set; }
    public string? SelfieImageUrl { get; set; }
}

