using System;
using Newtonsoft.Json;

namespace Lujke.Models;

public class SubscribeParams
{
    [JsonProperty("routingFilters")]
    public CandleRoutingFilters RoutingFilters { get; set; } = new();
}