using Newtonsoft.Json;

namespace Lujke.Vue.Models;

/// <summary>
/// Generic incoming frame. Every application-level message the server pushes has this
/// shape: {"name":"<event>","msg":{...},"request_id":"...","..."}
/// Used to decode the live "candle-generated" stream and get-candles results.
/// </summary>
public class SocketEnvelopeGeneric
{
    [JsonProperty("name")]
    public string? Name { get; set; }

    [JsonProperty("msg")]
    public object? Msg { get; set; }

    [JsonProperty("request_id")]
    public string? RequestId { get; set; }

    [JsonProperty("microserviceName")]
    public string? MicroserviceName { get; set; }
}
