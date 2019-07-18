using Newtonsoft.Json;
using System.Collections.Generic;

namespace Dintero.Checkout.Episerver.Models
{
    public class DinteroRefundRequest
    {
        [JsonProperty(PropertyName = "amount")]
        public decimal Amount { get; set; }

        [JsonProperty(PropertyName = "reason")]
        public string Reason { get; set; }

        [JsonProperty(PropertyName = "items")]
        public List<DinteroOrderLine> Items { get; set; }
    }
}