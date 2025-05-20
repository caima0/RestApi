using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;
using api.Models;
using System.Net.Http.Headers;
using System.Collections.Generic;
using api.Dtos.PayU;
using Microsoft.Extensions.Configuration;

namespace api.Services
{
    public class PayUService
    {
        private readonly HttpClient _httpClient;
        private readonly string _posId;
        private readonly string _clientId;
        private readonly string _clientSecret;
        private readonly string _baseUrl;
        private readonly string _notifyUrl;
        private string _accessToken;
        private DateTime _tokenExpiry;

        public PayUService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _posId = configuration["PayU:PosId"];
            _clientId = configuration["PayU:ClientId"];
            _clientSecret = configuration["PayU:ClientSecret"];
            _baseUrl = configuration["PayU:BaseUrl"];
            _notifyUrl = configuration["PayU:NotifyUrl"];
            _httpClient.BaseAddress = new Uri(_baseUrl);
        }

        private async Task<string> GetAccessTokenAsync()
        {
            if (!string.IsNullOrEmpty(_accessToken) && DateTime.UtcNow < _tokenExpiry)
            {
                return _accessToken;
            }

            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            
            // Debug request details
            Console.WriteLine("=== OAuth Request Details ===");
            Console.WriteLine($"Base URL: {_httpClient.BaseAddress}");
            Console.WriteLine($"Client ID: {_clientId}");
            Console.WriteLine($"POS ID: {_posId}");
            
            var formContent = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("grant_type", "client_credentials"),
                new KeyValuePair<string, string>("client_id", _clientId),
                new KeyValuePair<string, string>("client_secret", _clientSecret)
            });

            // Debug request headers
            Console.WriteLine("\n=== Request Headers ===");
            foreach (var header in _httpClient.DefaultRequestHeaders)
            {
                Console.WriteLine($"{header.Key}: {string.Join(", ", header.Value)}");
            }

            Console.WriteLine("\n=== Request Content ===");
            var formContentString = await formContent.ReadAsStringAsync();
            Console.WriteLine(formContentString);

            Console.WriteLine("\n=== Sending Request ===");
            var response = await _httpClient.PostAsync("pl/standard/user/oauth/authorize", formContent);
            
            // Debug response details
            Console.WriteLine("\n=== Response Details ===");
            Console.WriteLine($"Status Code: {response.StatusCode}");
            Console.WriteLine($"Response Headers:");
            foreach (var header in response.Headers)
            {
                Console.WriteLine($"{header.Key}: {string.Join(", ", header.Value)}");
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            Console.WriteLine("\n=== Response Content ===");
            Console.WriteLine(responseContent);

            if (!response.IsSuccessStatusCode)
            {
                // Log unexpected HTML content
                if (responseContent.TrimStart().StartsWith("<!DOCTYPE html", StringComparison.OrdinalIgnoreCase) ||
                    responseContent.TrimStart().StartsWith("<html", StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine("\n=== HTML Response Detected ===");
                    Console.WriteLine("Received HTML instead of JSON response. This usually indicates:");
                    Console.WriteLine("1. Incorrect endpoint URL");
                    Console.WriteLine("2. Invalid credentials");
                    Console.WriteLine("3. Server-side redirect");
                }

                throw new Exception($"OAuth error: {response.StatusCode} - {responseContent}");
            }

            try
            {
                var tokenResponse = JsonSerializer.Deserialize<TokenResponse>(responseContent);
                
                if (string.IsNullOrEmpty(tokenResponse?.AccessToken))
                {
                    throw new Exception("Access token is null or empty in the response");
                }

                _accessToken = tokenResponse.AccessToken;
                Console.WriteLine("\n=== Token Details ===");
                Console.WriteLine($"Access Token: {_accessToken}");
                Console.WriteLine($"Token Type: {tokenResponse.TokenType}");
                Console.WriteLine($"Expires In: {tokenResponse.ExpiresIn} seconds");
                
                _tokenExpiry = DateTime.UtcNow.AddSeconds(tokenResponse.ExpiresIn - 60);
                return _accessToken;
            }
            catch (JsonException ex)
            {
                Console.WriteLine("\n=== JSON Parsing Error ===");
                Console.WriteLine($"Error parsing response: {ex.Message}");
                Console.WriteLine($"Response content that failed to parse: {responseContent}");
                throw new Exception($"Failed to parse OAuth response: {ex.Message}", ex);
            }
        }

        private int ConvertAmountToLowestCurrencyUnit(decimal amount, string currency)
        {
            // Ensure amount is positive
            if (amount <= 0)
            {
                throw new ArgumentException("Amount must be greater than zero", nameof(amount));
            }

            // Convert to lowest currency unit (cents)
            return (int)Math.Round(amount * 100, MidpointRounding.AwayFromZero);
        }

        public async Task<PayUResponse> CreatePaymentAsync(decimal amount, string currency, string description)
        {
            // Validate amount
            if (amount <= 0)
            {
                throw new ArgumentException("Amount must be greater than zero", nameof(amount));
            }

            var accessToken = await GetAccessTokenAsync();

            // Debug payment details
            Console.WriteLine("\n=== Payment Request Details ===");
            Console.WriteLine($"Amount: {amount.ToString("F2", System.Globalization.CultureInfo.InvariantCulture)}");
            Console.WriteLine($"Currency: {currency}");
            Console.WriteLine($"Description: {description}");
            Console.WriteLine($"Converted Amount: {ConvertAmountToLowestCurrencyUnit(amount, currency)}");

            var paymentRequest = new
            {
                notifyUrl = _notifyUrl,
                customerIp = "127.0.0.1",
                merchantPosId = _posId,
                description = description,
                currencyCode = currency,
                totalAmount = ConvertAmountToLowestCurrencyUnit(amount, currency),
                extOrderId = Guid.NewGuid().ToString(),
                buyer = new
                {
                    email = "buyer@example.com",
                    phone = "654111654",
                    firstName = "John",
                    lastName = "Doe",
                    language = "pl"
                },
                products = new[]
                {
                    new
                    {
                        name = description,
                        unitPrice = ConvertAmountToLowestCurrencyUnit(amount, currency),
                        quantity = 1
                    }
                }
            };

            var jsonRequest = JsonSerializer.Serialize(paymentRequest, new JsonSerializerOptions { WriteIndented = true });
            Console.WriteLine("\n=== Payment Request JSON ===");
            Console.WriteLine(jsonRequest);

            var content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");

            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            _httpClient.DefaultRequestHeaders.Authorization = 
                new AuthenticationHeaderValue("Bearer", accessToken);

            Console.WriteLine("\n=== Sending Payment Request ===");
            Console.WriteLine($"URL: {_httpClient.BaseAddress}api/v2_1/orders");
            Console.WriteLine("Headers:");
            foreach (var header in _httpClient.DefaultRequestHeaders)
            {
                Console.WriteLine($"{header.Key}: {string.Join(", ", header.Value)}");
            }

            var response = await _httpClient.PostAsync("api/v2_1/orders", content);
            
            var responseContent = await response.Content.ReadAsStringAsync();
            Console.WriteLine("\n=== Payment Response ===");
            Console.WriteLine($"Status Code: {response.StatusCode}");
            Console.WriteLine("Response Headers:");
            foreach (var header in response.Headers)
            {
                Console.WriteLine($"{header.Key}: {string.Join(", ", header.Value)}");
            }

            // Check if we got HTML response (payment page)
            if (responseContent.TrimStart().StartsWith("<!DOCTYPE html", StringComparison.OrdinalIgnoreCase) ||
                responseContent.TrimStart().StartsWith("<html", StringComparison.OrdinalIgnoreCase))
            {
                // Extract the redirect URL from the HTML response
                var redirectUrl = response.Headers.Location?.ToString();
                if (string.IsNullOrEmpty(redirectUrl))
                {
                    // If no redirect URL in headers, try to find it in the HTML
                    var match = System.Text.RegularExpressions.Regex.Match(responseContent, @"window\.location\.href\s*=\s*['""]([^'""]+)['""]");
                    if (match.Success)
                    {
                        redirectUrl = match.Groups[1].Value;
                    }
                }

                if (!string.IsNullOrEmpty(redirectUrl))
                {
                    return new PayUResponse
                    {
                        Status = new Status
                        {
                            StatusCode = "SUCCESS",
                            StatusDesc = "Payment page generated"
                        },
                        RedirectUri = redirectUrl
                    };
                }
            }

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"PayU API error: {response.StatusCode} - {responseContent}");
            }

            var paymentResponse = JsonSerializer.Deserialize<PayUResponse>(responseContent);

            if (paymentResponse == null)
            {
                throw new Exception("Failed to deserialize payment response");
            }

            return paymentResponse;
        }

        public async Task<OrderStatus> GetOrderStatusAsync(string orderId)
        {
            try
            {
                var accessToken = await GetAccessTokenAsync();

                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                _httpClient.DefaultRequestHeaders.Authorization = 
                    new AuthenticationHeaderValue("Bearer", accessToken);

                var response = await _httpClient.GetAsync($"api/v2_1/orders/{orderId}");
                
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Failed to get order status: {response.StatusCode} - {errorContent}");
                }

                var responseContent = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<OrderStatus>(responseContent);
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to get order status: {ex.Message}", ex);
            }
        }
    }
}
