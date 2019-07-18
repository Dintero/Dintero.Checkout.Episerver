using Newtonsoft.Json;
using System.Collections.Generic;

namespace Dintero.Checkout.Episerver.Models
{
    public class DinteroResponseError
    {
        [JsonProperty(PropertyName = "code")]
        public string Code { get; set; }

        [JsonProperty(PropertyName = "errors")]
        public List<object> Errors { get; set; }

        [JsonProperty(PropertyName = "message")]
        public string Message { get; set; }
    }
}