using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace api.Dtos.PayU
{
    public class PaymentNotification
    {
        public Order[] Orders { get; set; }

        public class Order
        {
            public string OrderId { get; set; }
            public string ExtOrderId { get; set; }
            public string OrderStatus { get; set; }
            public decimal TotalAmount { get; set; }
            public string CurrencyCode { get; set; }
        }
    }
}
