using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dintero.Checkout.Episerver.Models
{
    public class DinteroStore
    {
        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; }
    }
}
