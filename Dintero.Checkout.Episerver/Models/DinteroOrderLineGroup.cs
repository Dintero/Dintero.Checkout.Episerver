using Newtonsoft.Json;

namespace Dintero.Checkout.Episerver.Models
{
    public class DinteroOrderLineGroup
    {
        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; }


        [JsonProperty(PropertyName = "name")]
        public string Name { get; set; }
    }
}