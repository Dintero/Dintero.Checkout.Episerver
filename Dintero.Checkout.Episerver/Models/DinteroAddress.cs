using Newtonsoft.Json;

namespace Dintero.Checkout.Episerver.Models
{
    public class DinteroAddress
    {
        [JsonProperty(PropertyName = "first_name")]
        public string FirstName { get; set; }


        [JsonProperty(PropertyName = "last_name")]
        public string LastName { get; set; }


        [JsonProperty(PropertyName = "address_line")]
        public string AddressLine { get; set; }


        [JsonProperty(PropertyName = "postal_code")]
        public string PostalCode { get; set; }


        [JsonProperty(PropertyName = "postal_place")]
        public string PostalPlace { get; set; }


        [JsonProperty(PropertyName = "country")]
        public string Country { get; set; }
    }
}