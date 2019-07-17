using Newtonsoft.Json;

namespace Dintero.Checkout.Episerver.Models
{
    public class DinteroCustomer
    {
        [JsonProperty(PropertyName = "email")]
        public string Email { get; set; }


        [JsonProperty(PropertyName = "phone_number")]
        public string PhoneNumber { get; set; }
    }
}
