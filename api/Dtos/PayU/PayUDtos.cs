using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace api.Dtos.PayU
{
    public class PayUResponse
    {
        public  string RedirectUri { get; set; }
        public  string OrderId { get; set; }
        public string ExtOrderId { get; set; }
        public  Status Status { get; set; }
    }

    public class TokenResponse
    {
        [JsonPropertyName("access_token")]
        public  string AccessToken { get; set; }

        [JsonPropertyName("expires_in")]
        public int ExpiresIn { get; set; }

        [JsonPropertyName("token_type")]
        public  string TokenType { get; set; }

        [JsonPropertyName("grant_type")]
        public  string GrantType { get; set; }
    }

    public class Status
    {
        public  string StatusCode { get; set; }
        public  string StatusDesc { get; set; }
        public  string Code { get; set; }
        public  string CodeLiteral { get; set; }
    }

    public class OrderStatus
    {
        public  List<Order> Orders { get; set; }
        public  Status Status { get; set; }
    }

    public class Order
    {
        public  string OrderId { get; set; }
        public  string ExtOrderId { get; set; }
        public  string OrderCreateDate { get; set; }
        public  string NotifyUrl { get; set; }
        public  string CustomerIp { get; set; }
        public  string MerchantPosId { get; set; }
        public  string Description { get; set; }
        public  string CurrencyCode { get; set; }
        public  string TotalAmount { get; set; }
        public  string Status { get; set; }
        public  List<Product> Products { get; set; }
    }

    public class Product
    {
        public  string Name { get; set; }
        public  string UnitPrice { get; set; }
        public  string Quantity { get; set; }
    }

    public class PayUStatus
    {
        [JsonPropertyName("statusCode")]
        public string StatusCode { get; set; }

        [JsonPropertyName("statusDesc")]
        public  string StatusDesc { get; set; }

        [JsonPropertyName("redirectUri")]
        public  string RedirectUri { get; set; }
    }
} 