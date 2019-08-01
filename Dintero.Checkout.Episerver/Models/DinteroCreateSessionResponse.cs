using Newtonsoft.Json;

namespace Dintero.Checkout.Episerver.Models
{
    public class DinteroCreateSessionResponse : BaseDinteroResponse
    {
        [JsonProperty(PropertyName = "id")]
        public string SessionId { get; set; }

        [JsonProperty(PropertyName = "url")]
        public string CheckoutUrl { get; set; }
    }
}