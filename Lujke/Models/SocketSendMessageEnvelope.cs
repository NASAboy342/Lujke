using Newtonsoft.Json;

namespace Lujke.Vue.Models;

/// <summary>
/// Outer envelope every application-level message is wrapped in before hitting the socket.
/// Wire format (observed 2026-08-19):
/// {"name":"sendMessage","msg":{...actual message...},"request_id":"request_433","local_time":2722686}
/// </summary>
public class SocketSendMessageEnvelope
{
    [JsonProperty("name")]
    public string Name { get; set; } = "sendMessage";

    [JsonProperty("msg")]
    public object Msg { get; set; } = null!;

    /// <summary>Client-generated correlation id, e.g. "request_433".</summary>
    [JsonProperty("request_id")]
    public string RequestId { get; set; } = string.Empty;

    /// <summary>Client time in ms (app-relative timestamp is what the web client sends).</summary>
    [JsonProperty("local_time")]
    public long LocalTime { get; set; }

    public static SocketSendMessageEnvelope Wrap(object msg, string requestId, long localTime)
        => new() { Msg = msg, RequestId = requestId, LocalTime = localTime };
}
