using Newtonsoft.Json;

namespace Lujke.Vue.Models;

public class SubscribeParams
{
    [JsonProperty("routingFilters")]
    public CandleRoutingFilters RoutingFilters { get; set; } = new();
}
