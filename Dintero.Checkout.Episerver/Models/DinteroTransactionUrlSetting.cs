using Newtonsoft.Json;

namespace Dintero.Checkout.Episerver.Models
{
    public class DinteroTransactionUrlSetting
    {
        [JsonProperty(PropertyName = "redirect_url")]
        public string RedirectUrl { get; set; }


        [JsonProperty(PropertyName = "approval_url")]
        public string ApprovalUrl { get; set; }


        [JsonProperty(PropertyName = "callback_url")]
        public string CallbackUrl { get; set; }
    }
}