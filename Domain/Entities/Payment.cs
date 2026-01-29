using Rently.Management.Domain.Common;

namespace Rently.Management.Domain.Entities;

public class Payment : AuditableEntity
{
    public int Id { get; set; }                      // PK

    public int BookingId { get; set; }               // FK → Booking
    public int? UserId { get; set; }                 // اختياري: FK → User (الدافع)

    public decimal Amount { get; set; }              // المبلغ المطلوب أو المدفوع
    public string Currency { get; set; } = "SAR";    // العملة، افتراضيًا SAR

    public string Status { get; set; } = "Pending";  // Pending, Succeeded, Failed, Refunded...
    public string? Provider { get; set; }            // بوابة الدفع (Stripe, Tap, Moyasar...)
    public string? ProviderPaymentId { get; set; }   // رقم العملية عند مزود الدفع
    public string? ProviderReceiptUrl { get; set; }  // لينك الإيصال لو متاح

    public string? FailureCode { get; set; }         // كود الخطأ من المزود
    public string? FailureMessage { get; set; }      // رسالة الخطأ

    // Navigation
    public Booking? Booking { get; set; }
    public User? User { get; set; }
}

