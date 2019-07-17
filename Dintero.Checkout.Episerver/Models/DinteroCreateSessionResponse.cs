using Newtonsoft.Json;

namespace Dintero.Checkout.Episerver.Models
{
    public class DinteroCreateSessionResponse
    {
        [JsonProperty(PropertyName = "id")]
        public string TransactionId { get; set; }

        [JsonProperty(PropertyName = "url")]
        public string CheckoutUrl { get; set; }
    }
}