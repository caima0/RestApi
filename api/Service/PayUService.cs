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
            try
            {
                if (!string.IsNullOrEmpty(_accessToken) && DateTime.UtcNow < _tokenExpiry)
                {
                    return _accessToken;
                }

                _httpClient.DefaultRequestHeaders.Clear();
                
                var formContent = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("grant_type", "client_credentials"),
                    new KeyValuePair<string, string>("client_id", _clientId),
                    new KeyValuePair<string, string>("client_secret", _clientSecret)
                });

                var response = await _httpClient.PostAsync("pl/standard/user/oauth/authorize", formContent);
                
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"OAuth error: {response.StatusCode} - {errorContent}");
                }

                var responseContent = await response.Content.ReadAsStringAsync();

                Console.WriteLine("Raw token response:");
                Console.WriteLine(responseContent);
                
                var tokenResponse = JsonSerializer.Deserialize<TokenResponse>(responseContent);

                Console.WriteLine($"Token: {tokenResponse?.AccessToken}, expires in: {tokenResponse?.ExpiresIn}");

                if (string.IsNullOrEmpty(tokenResponse?.AccessToken))
                {
                    throw new Exception("Access token is null or empty in the response");
                }

                _accessToken = tokenResponse.AccessToken;
                _tokenExpiry = DateTime.UtcNow.AddSeconds(tokenResponse.ExpiresIn - 60);

                return _accessToken;
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to get access token: {ex.Message}", ex);
            }
        }

        public async Task<PayUResponse> CreatePaymentAsync(decimal amount, string currency, string description)
        {
            try
            {
                if (string.IsNullOrEmpty(_accessToken))
                {
                    _accessToken = await GetAccessTokenAsync();
                }

                var paymentRequest = new
                {
                    notifyUrl = _notifyUrl,
                    customerIp = "127.0.0.1",
                    merchantPosId = _posId,
                    description = description,
                    currencyCode = currency,
                    totalAmount = (int)(amount * 100), // Convert to cents
                    extOrderId = Guid.NewGuid().ToString(), // Generate unique order ID
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
                            name = "Currency Exchange",
                            unitPrice = (int)(amount * 100),
                            quantity = 1
                        }
                    }
                };

                var content = new StringContent(
                    JsonSerializer.Serialize(paymentRequest),
                    Encoding.UTF8,
                    "application/json"
                );

                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                _httpClient.DefaultRequestHeaders.Authorization = 
                    new AuthenticationHeaderValue("Bearer", _accessToken);

                var response = await _httpClient.PostAsync("api/v2_1/orders", content);
                
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"PayU API error: {response.StatusCode} - {errorContent}");
                }

                var responseContent = await response.Content.ReadAsStringAsync();
                var paymentResponse = JsonSerializer.Deserialize<PayUResponse>(responseContent);

                if (paymentResponse == null)
                {
                    throw new Exception("Failed to deserialize payment response");
                }

                return paymentResponse;
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to create PayU payment: {ex.Message}", ex);
            }
        }

        public async Task<OrderStatus> GetOrderStatusAsync(string orderId)
        {
            try
            {
                if (string.IsNullOrEmpty(_accessToken))
                {
                    _accessToken = await GetAccessTokenAsync();
                }

                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                _httpClient.DefaultRequestHeaders.Authorization = 
                    new AuthenticationHeaderValue("Bearer", _accessToken);

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