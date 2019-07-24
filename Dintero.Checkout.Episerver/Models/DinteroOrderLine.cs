using Newtonsoft.Json;
using System.Collections.Generic;

namespace Dintero.Checkout.Episerver.Models
{
    public class DinteroOrderLine
    {
        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; }


        [JsonProperty(PropertyName = "groups")]
        public List<DinteroOrderLineGroup> Groups { get; set; }


        [JsonProperty(PropertyName = "line_id")]
        public string LineId { get; set; }


        [JsonProperty(PropertyName = "description")]
        public string Description { get; set; }


        [JsonProperty(PropertyName = "quantity")]
        public decimal Quantity { get; set; }


        [JsonProperty(PropertyName = "amount")]
        public int Amount { get; set; }


        [JsonProperty(PropertyName = "vat_amount")]
        public int VatAmount { get; set; }


        [JsonProperty(PropertyName = "vat")]
        public int Vat { get; set; }
    }
}