using Microsoft.AspNetCore.Mvc;
using Rently.Management.Domain.Entities;
using Rently.Management.Domain.Repositories;
using Rently.Management.WebApi.DTOs;

namespace Rently.Management.WebApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PaymentController : ControllerBase
    {
        private readonly IPaymentRepository _paymentRepository;

        public PaymentController(IPaymentRepository paymentRepository)
        {
            _paymentRepository = paymentRepository;
        }

        [HttpGet("statistics")]
        public async Task<ActionResult<PaymentStatisticsDto>> GetStatistics()
        {
            var statistics = await _paymentRepository.GetStatisticsAsync();

            var dto = new PaymentStatisticsDto
            {
                TotalRevenue = statistics.TotalRevenue,
                TotalRevenueChangePercent = statistics.TotalRevenueChangePercent,
                PendingPayout = statistics.PendingPayout,
                ProfitLast30Days = statistics.ProfitLast30Days,
                ProfitChangePercent = statistics.ProfitChangePercent
            };

            return Ok(dto);
        }

        [HttpGet("transactions")]
        public async Task<ActionResult<PagedResultDto<PaymentDto>>> GetTransactions(
            [FromQuery] string? search = null,
            [FromQuery] string? type = null,
            [FromQuery] string? status = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            var result = await _paymentRepository.GetTransactionsAsync(search, type, status, page, pageSize);

            var paymentDtos = result.Data.Select(p => new PaymentDto
            {
                Id = p.Id,
                TxId = p.ProviderPaymentId ?? p.Id.ToString(),
                Date = p.CreatedAt,
                Type = GetPaymentType(p),
                PayerName = p.Booking?.Renter?.Name ?? p.User?.Name ?? "",
                PayeeName = p.Booking?.Car?.Owner?.Name ?? "",
                Amount = p.Amount,
                Status = p.Status,
                Currency = p.Currency,
                BookingId = p.BookingId
            }).ToList();

            return Ok(new PagedResultDto<PaymentDto>
            {
                Data = paymentDtos,
                TotalCount = result.TotalCount,
                Page = result.Page,
                PageSize = result.PageSize,
                TotalPages = result.TotalPages
            });
        }

        [HttpGet("owner-payouts")]
        public async Task<ActionResult<PagedResultDto<OwnerPayoutDto>>> GetOwnerPayouts(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            var result = await _paymentRepository.GetOwnerPayoutsAsync(page, pageSize);

            var payoutDtos = result.Data
                .GroupBy(p => p.Booking!.Car!.OwnerId)
                .Select(g => new OwnerPayoutDto
                {
                    Id = g.Key,
                    OwnerName = g.First().Booking!.Car!.Owner?.Name ?? "",
                    LastPayout = g.Max(p => p.CreatedAt),
                    Amount = g.Sum(p => p.Amount),
                    Status = "Completed",
                    Currency = g.First().Currency
                })
                .ToList();

            return Ok(new PagedResultDto<OwnerPayoutDto>
            {
                Data = payoutDtos,
                TotalCount = payoutDtos.Count,
                Page = page,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling(payoutDtos.Count / (double)pageSize)
            });
        }

        [HttpGet("refunds")]
        public async Task<ActionResult<PagedResultDto<RefundDto>>> GetRefunds(
            [FromQuery] string? status = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            var result = await _paymentRepository.GetRefundsAsync(status, page, pageSize);

            var refundDtos = result.Data.Select(p => new RefundDto
            {
                Id = p.Id,
                RenterName = p.Booking?.Renter?.Name ?? "",
                RefundAmount = p.Amount,
                Status = GetRefundStatus(p.Status),
                CreatedAt = p.CreatedAt,
                Reason = p.FailureMessage,
                BookingId = p.BookingId
            }).ToList();

            return Ok(new PagedResultDto<RefundDto>
            {
                Data = refundDtos,
                TotalCount = result.TotalCount,
                Page = result.Page,
                PageSize = result.PageSize,
                TotalPages = result.TotalPages
            });
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<PaymentDto>> GetPayment(int id)
        {
            var payment = await _paymentRepository.GetByIdAsync(id);

            if (payment == null)
            {
                return NotFound();
            }

            return Ok(new PaymentDto
            {
                Id = payment.Id,
                TxId = payment.ProviderPaymentId ?? payment.Id.ToString(),
                Date = payment.CreatedAt,
                Type = GetPaymentType(payment),
                PayerName = payment.Booking?.Renter?.Name ?? payment.User?.Name ?? "",
                PayeeName = payment.Booking?.Car?.Owner?.Name ?? "",
                Amount = payment.Amount,
                Status = payment.Status,
                Currency = payment.Currency,
                BookingId = payment.BookingId
            });
        }

        [HttpPost]
        public async Task<ActionResult<PaymentDto>> CreatePayment([FromBody] CreatePaymentDto dto)
        {
            var payment = new Payment
            {
                BookingId = dto.BookingId,
                UserId = dto.UserId,
                Amount = dto.Amount,
                Currency = dto.Currency,
                Status = dto.Status,
                Provider = dto.Provider,
                ProviderPaymentId = dto.ProviderPaymentId,
                ProviderReceiptUrl = dto.ProviderReceiptUrl
            };

            var createdPayment = await _paymentRepository.CreateAsync(payment);
            var paymentWithDetails = await _paymentRepository.GetByIdAsync(createdPayment.Id);

            return CreatedAtAction(nameof(GetPayment), new { id = createdPayment.Id }, new PaymentDto
            {
                Id = createdPayment.Id,
                TxId = createdPayment.ProviderPaymentId ?? createdPayment.Id.ToString(),
                Date = createdPayment.CreatedAt,
                Type = GetPaymentType(paymentWithDetails!),
                PayerName = paymentWithDetails?.Booking?.Renter?.Name ?? paymentWithDetails?.User?.Name ?? "",
                PayeeName = paymentWithDetails?.Booking?.Car?.Owner?.Name ?? "",
                Amount = createdPayment.Amount,
                Status = createdPayment.Status,
                Currency = createdPayment.Currency,
                BookingId = createdPayment.BookingId
            });
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdatePayment(int id, [FromBody] UpdatePaymentDto dto)
        {
            var payment = await _paymentRepository.GetByIdAsync(id);

            if (payment == null)
            {
                return NotFound();
            }

            if (!string.IsNullOrEmpty(dto.Status))
                payment.Status = dto.Status;
            if (!string.IsNullOrEmpty(dto.ProviderPaymentId))
                payment.ProviderPaymentId = dto.ProviderPaymentId;
            if (!string.IsNullOrEmpty(dto.ProviderReceiptUrl))
                payment.ProviderReceiptUrl = dto.ProviderReceiptUrl;
            if (!string.IsNullOrEmpty(dto.FailureCode))
                payment.FailureCode = dto.FailureCode;
            if (!string.IsNullOrEmpty(dto.FailureMessage))
                payment.FailureMessage = dto.FailureMessage;

            await _paymentRepository.UpdateAsync(payment);

            return NoContent();
        }

        [HttpPost("process-payout")]
        public async Task<IActionResult> ProcessPayout([FromBody] ProcessPayoutDto dto)
        {
            // This would typically create a payment record for owner payout
            // Implementation depends on your business logic
            return Ok(new { message = "Payout processed successfully" });
        }

        [HttpPost("process-refund")]
        public async Task<IActionResult> ProcessRefund([FromBody] ProcessRefundDto dto)
        {
            var payment = await _paymentRepository.GetByIdAsync(dto.PaymentId);

            if (payment == null)
            {
                return NotFound();
            }

            // Create refund payment
            var refund = new Payment
            {
                BookingId = payment.BookingId,
                UserId = payment.UserId,
                Amount = dto.Amount,
                Currency = payment.Currency,
                Status = "Pending",
                Provider = payment.Provider,
                FailureMessage = dto.Reason
            };

            await _paymentRepository.CreateAsync(refund);

            // Update original payment status
            payment.Status = "Refunding";
            await _paymentRepository.UpdateAsync(payment);

            return Ok(new { message = "Refund request created successfully", refundId = refund.Id });
        }

        [HttpPost("refund-all")]
        public async Task<IActionResult> RefundAll([FromBody] List<int> paymentIds)
        {
            foreach (var paymentId in paymentIds)
            {
                var payment = await _paymentRepository.GetByIdAsync(paymentId);
                if (payment != null && payment.Status == "Succeeded")
                {
                    payment.Status = "Refunding";
                    await _paymentRepository.UpdateAsync(payment);
                }
            }

            return Ok(new { message = "Refund requests processed successfully" });
        }

        private string GetPaymentType(Payment? payment)
        {
            if (payment == null) return "Unknown";

            if (payment.Status == "Refunded" || payment.Status == "Refunding")
                return "Refund";

            if (payment.Booking != null)
            {
                if (payment.CreatedAt >= DateTime.UtcNow.AddDays(-7))
                    return "New booking";
                return "Reservation payment";
            }

            return "Owner Payout";
        }

        private string GetRefundStatus(string paymentStatus)
        {
            return paymentStatus switch
            {
                "Pending" => "Under Review",
                "Refunding" => "Approved",
                "Refunded" => "Completed",
                "Failed" => "Rejected",
                _ => "Under Review"
            };
        }
    }
}
