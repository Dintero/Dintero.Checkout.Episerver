using System.Collections.Generic;
using Newtonsoft.Json;

namespace Dintero.Checkout.Episerver.Models
{
    public class DinteroTransactionEvent
    {
        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; }

        [JsonProperty(PropertyName = "event")]
        public string Event { get; set; }

        [JsonProperty(PropertyName = "items")]
        public List<DinteroOrderLine> Items { get; set; }

        [JsonProperty(PropertyName = "amount")]
        public int Amount { get; set; }

        [JsonProperty(PropertyName = "success")]
        public bool Success { get; set; }
    }
}