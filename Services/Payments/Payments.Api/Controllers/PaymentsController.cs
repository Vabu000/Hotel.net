using HotelPlatform.ServiceDefaults.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelPayments.Api.Controllers;

[ApiController]
[Route("api/payments")]
public sealed class PaymentsController : ControllerBase
{
    public sealed record ProcessPaymentRequest(int ReservationId, decimal Amount, string Method);
    public sealed record RefundPaymentRequest(string TransactionId, decimal Amount, string Reason);

    [HttpGet]
    [RequirePermission("payments:read")]
    public IActionResult GetPayments()
    {
        return Ok(new[]
        {
            new { id = "pay-1001", reservationId = 1, amount = 450.00m, status = "Captured" }
        });
    }

    [HttpPost("process")]
    [RequirePermission("payments:process")]
    public IActionResult ProcessPayment([FromBody] ProcessPaymentRequest request)
    {
        return Ok(new
        {
            transactionId = $"txn-{Guid.NewGuid():N}",
            request.ReservationId,
            request.Amount,
            request.Method,
            status = "Processed"
        });
    }

    [HttpPost("refund")]
    [RequirePermission("payments:refund")]
    public IActionResult RefundPayment([FromBody] RefundPaymentRequest request)
    {
        return Ok(new
        {
            request.TransactionId,
            request.Amount,
            request.Reason,
            status = "Refunded"
        });
    }
}
