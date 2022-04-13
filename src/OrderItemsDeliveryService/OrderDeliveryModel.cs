using System.Collections.Generic;
using Newtonsoft.Json;

namespace OrderItemsDeliveryService;
public class OrderDeliveryModel
{
    [JsonProperty(PropertyName = "id")]
    public string Id { get; set; }
    public string ShippingAddress { get; set; }
    public Dictionary<string,int> ListOfItems { get; set; }
    public decimal FinalPrice { get; set; }
}
