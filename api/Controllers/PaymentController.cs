using System;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using api.Services;
using api.Models;
using api.Interfaces;
using api.Dtos.PayU;

namespace api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PaymentController : ControllerBase
    {
        private readonly PayUService _payUService;
        private readonly IRateRepository _rateRepository;

        public PaymentController(PayUService payUService, IRateRepository rateRepository)
        {
            _payUService = payUService;
            _rateRepository = rateRepository;
        }

        [HttpPost("create")]
        public async Task<IActionResult> CreatePayment([FromBody] PaymentRequest request)
        {
            try
            {
                // Validate request
                if (request == null)
                {
                    return BadRequest(new { error = "Request body cannot be null" });
                }

                if (request.Amount <= 0)
                {
                    return BadRequest(new { error = "Amount must be greater than zero" });
                }

                if (string.IsNullOrEmpty(request.Currency))
                {
                    return BadRequest(new { error = "Currency is required" });
                }

                // Get rate from database
                var rate = await _rateRepository.GetByCodeAsync(request.Currency);
                
                if (rate == null)
                {
                    return BadRequest(new { error = $"Currency {request.Currency} not found in available rates" });
                }

                // Convert amount to PLN using the ask rate (since we're buying the currency)
                decimal amountInPLN = request.Amount * (decimal)rate.Ask;

                // Create payment in PLN
                var paymentResponse = await _payUService.CreatePaymentAsync(
                    amountInPLN,
                    "PLN", // PayU expects payments in PLN
                    $"Currency exchange: {request.Amount} {request.Currency} to PLN"
                );

                if (paymentResponse == null)
                {
                    return BadRequest(new { error = "Failed to create payment" });
                }

                if (string.IsNullOrEmpty(paymentResponse.RedirectUri))
                {
                    return BadRequest(new { error = "No payment redirect URL received from PayU" });
                }

                return Ok(new { 
                    redirectUrl = paymentResponse.RedirectUri,
                    orderId = paymentResponse.OrderId,
                    extOrderId = paymentResponse.ExtOrderId,
                    originalAmount = request.Amount,
                    originalCurrency = request.Currency,
                    convertedAmount = amountInPLN,
                    convertedCurrency = "PLN",
                    exchangeRate = rate.Ask,
                    status = paymentResponse.Status
                });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                // Log the full exception details here
                return StatusCode(500, new { error = "An unexpected error occurred while processing the payment" });
            }
        }

        [HttpGet("status/{orderId}")]
        public async Task<IActionResult> GetOrderStatus(string orderId)
        {
            try
            {
                if (string.IsNullOrEmpty(orderId))
                {
                    return BadRequest(new { error = "Order ID is required" });
                }

                var orderStatus = await _payUService.GetOrderStatusAsync(orderId);
                
                if (orderStatus == null)
                {
                    return NotFound(new { error = $"Order {orderId} not found" });
                }

                return Ok(orderStatus);
            }
            catch (Exception ex)
            {
                // Log the full exception details here
                return StatusCode(500, new { error = "An unexpected error occurred while checking order status" });
            }
        }
    }
} 