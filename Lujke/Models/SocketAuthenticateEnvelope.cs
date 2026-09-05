using Newtonsoft.Json;

namespace Lujke.Vue.Models;

/// <summary>
/// Post-connect authentication frame. Sent once right after the socket opens.
/// Wire format (observed 2026-08-19):
/// {"name":"authenticate","msg":{"ssid":"24c1...f9ch","protocol":3},"request_id":"request_2","local_time":2228}
/// The server answers {"name":"authenticated","msg":true,...}.
/// </summary>
public class SocketAuthenticateEnvelope
{
    [JsonProperty("name")]
    public string Name { get; set; } = "authenticate";

    [JsonProperty("msg")]
    public SocketAuthenticateBody Msg { get; set; } = new();

    [JsonProperty("request_id")]
    public string RequestId { get; set; } = "request_2";

    [JsonProperty("local_time")]
    public long LocalTime { get; set; }
}
