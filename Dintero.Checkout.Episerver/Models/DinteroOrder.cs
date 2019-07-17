using Newtonsoft.Json;
using System.Collections.Generic;

namespace Dintero.Checkout.Episerver.Models
{
    public class DinteroOrder
    {
        [JsonProperty(PropertyName = "amount")]
        public decimal Amount { get; set; }


        [JsonProperty(PropertyName = "vat_amount")]
        public decimal VatAmount { get; set; }


        [JsonProperty(PropertyName = "currency")]
        public string Currency { get; set; }


        [JsonProperty(PropertyName = "merchant_reference")]
        public string MerchantReference { get; set; }


        [JsonProperty(PropertyName = "billing_address")]
        public DinteroAddress BillingAddress { get; set; }


        [JsonProperty(PropertyName = "shipping_address")]
        public DinteroAddress ShippingAddress { get; set; }


        [JsonProperty(PropertyName = "partial_payment")]
        public bool PartialPayment { get; set; }


        [JsonProperty(PropertyName = "items")]
        public List<DinteroOrderLine> Items { get; set; }
    }
}