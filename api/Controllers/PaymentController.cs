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
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpGet("status/{orderId}")]
        public async Task<IActionResult> GetOrderStatus(string orderId)
        {
            try
            {
                var orderStatus = await _payUService.GetOrderStatusAsync(orderId);
                return Ok(orderStatus);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPost("notify")]
        public IActionResult Notify([FromBody] PaymentNotification notification)
        {
            if (notification?.Orders == null || notification.Orders.Length == 0)
                return BadRequest("Invalid notification payload.");

            var order = notification.Orders.First();

            // Log or process payment status
            Console.WriteLine("✅ PayU notification received:");
            Console.WriteLine($"Order ID: {order.OrderId}");
            Console.WriteLine($"Ext Order ID: {order.ExtOrderId}");
            Console.WriteLine($"Status: {order.OrderStatus}");
            Console.WriteLine($"Amount: {order.TotalAmount} {order.CurrencyCode}");

            return Ok(); 
        }
    }
} 