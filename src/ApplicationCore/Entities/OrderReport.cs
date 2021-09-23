using Microsoft.eShopWeb.ApplicationCore.Entities.OrderAggregate;
using Newtonsoft.Json;
using System.Linq;

namespace Microsoft.eShopWeb.ApplicationCore.Entities
{
    public class OrderReport
    {
        [JsonProperty("Shipping address")]
        public string ShippingAddress { get; set; }
        [JsonProperty("List of items")]
        public string [] ListOfItems { get; set; }
        [JsonProperty("Final Price")]
        public decimal FinalPrice { get; set; }

        private OrderReport()
        {

        }

        public OrderReport(string address, OrderItem[] items)
        {
            this.ShippingAddress = address;
            this.ListOfItems = items.Select(t => t.ItemOrdered.ProductName).ToArray<string>();
            this.FinalPrice = items.Sum(t => t.UnitPrice * t.Units);
        }

        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);
        }
    }
}
