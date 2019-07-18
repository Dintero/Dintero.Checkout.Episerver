using Newtonsoft.Json;

namespace Dintero.Checkout.Episerver.Models
{
    public class DinteroUrlSetting
    {
        [JsonProperty(PropertyName = "return_url")]
        public string ReturnUrl { get; set; }


        [JsonProperty(PropertyName = "callback_url")]
        public string CallbackUrl { get; set; }
    }
}