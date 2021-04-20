using Newtonsoft.Json;

namespace Dintero.Checkout.Episerver.Models
{
    public class DinteroCreateSessionRequest
    {
        [JsonProperty(PropertyName = "url")]
        public DinteroUrlSetting UrlSetting { get; set; }

        [JsonProperty(PropertyName = "customer")]
        public DinteroCustomer Customer { get; set; }

        [JsonProperty(PropertyName = "order")]
        public DinteroOrder Order { get; set; }

        [JsonProperty(PropertyName = "profile_id")]
        public string ProfileId { get; set; }

        [JsonProperty(PropertyName = "partial_payment")]
        public bool PartialPayment { get; set; }
    }
}