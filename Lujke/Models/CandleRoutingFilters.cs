using Newtonsoft.Json;

namespace Lujke.Vue.Models;

public class CandleRoutingFilters
{
    [JsonProperty("active_id")]
    public int ActiveId { get; set; }

    [JsonProperty("size")]
    public int Size { get; set; }
}
