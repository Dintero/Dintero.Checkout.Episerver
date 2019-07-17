using Newtonsoft.Json;
using System.Collections.Generic;

namespace Dintero.Checkout.Episerver.Models
{
    public class DinteroCaptureRequest
    {
        [JsonProperty(PropertyName = "amount")]
        public decimal Amount { get; set; }

        [JsonProperty(PropertyName = "capture_reference")]
        public string CaptureReference { get; set; }

        [JsonProperty(PropertyName = "items")]
        public List<DinteroOrderLine> Items { get; set; }
    }
}