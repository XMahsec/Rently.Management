namespace Rently.Management.Domain.Entities;
using Rently.Management.Domain.Common;


public class Booking : AuditableEntity
{
    public int Id { get; set; }                     // PK

    public int CarId { get; set; }
    public int RenterId { get; set; }

    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }

    public string? Status { get; set; }             // "Pending", "Confirmed", "Completed", "Cancelled"...
    public string? TransactionId { get; set; }
    public decimal TotalPrice { get; set; }             // ← لو مش موجود، أضفه
    public decimal PaidAmount { get; set; }             // المبلغ الفعلي اللي اتدفع
    public DateTime? PaymentConfirmedAt { get; set; }   // تاريخ تأكيد الدفع
    // Navigation (اختياري في API responses إذا ما كنتش عايز تُرجعهم دايماً)
    public Car? Car { get; set; }
    public User? Renter { get; set; }
    public List<Payment> Payments { get; set; } = new();
}