using Newtonsoft.Json;
using System.Collections.Generic;

namespace Dintero.Checkout.Episerver.Models
{
    public class DinteroTransactionActionResponse : BaseDinteroResponse
    {
        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; }

        [JsonProperty(PropertyName = "payment_product")]
        public string PaymentProduct { get; set; }

        [JsonProperty(PropertyName = "amount")]
        public int Amount { get; set; }

        [JsonProperty(PropertyName = "currency")]
        public string Currency { get; set; }

        [JsonProperty(PropertyName = "merchant_reference")]
        public string MerchantReference { get; set; }

        [JsonProperty(PropertyName = "dynamic_descriptor")]
        public string DynamicDescriptor { get; set; }

        [JsonProperty(PropertyName = "customer_ip")]
        public string CustomerIp { get; set; }

        [JsonProperty(PropertyName = "user_agent")]
        public string UserAgent { get; set; }

        [JsonProperty(PropertyName = "shipping_address")]
        public DinteroAddress ShippingAddress { get; set; }

        [JsonProperty(PropertyName = "status")]
        public string Status { get; set; }

        [JsonProperty(PropertyName = "items")]
        public List<DinteroOrderLine> Items { get; set; }

        [JsonProperty(PropertyName = "url")]
        public DinteroTransactionUrlSetting UrlSetting { get; set; }

        [JsonProperty(PropertyName = "events")]
        public List<DinteroTransactionEvent> Events { get; set; }

        [JsonProperty(PropertyName = "session_id")]
        public string SessionId { get; set; }

        [JsonProperty(PropertyName = "updated_at")]
        public string UpdatedAt { get; set; }

        [JsonProperty(PropertyName = "created_at")]
        public string CreatedAt { get; set; }

        [JsonProperty(PropertyName = "metadata")]
        public string Metadata { get; set; }

        [JsonProperty(PropertyName = "payment_product_type")]
        public string PaymentProductType { get; set; }
    }
}