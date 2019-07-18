using Newtonsoft.Json;

namespace Dintero.Checkout.Episerver.Models
{
    public class BaseDinteroResponse
    {

        [JsonProperty(PropertyName = "error")]
        public DinteroResponseError Error
        {
            get; set;
        }
    }
}