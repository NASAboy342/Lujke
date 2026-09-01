using System;
using Newtonsoft.Json;

namespace Lujke.Models;

/// <summary>
/// Outer envelope for subscription requests (distinct from "sendMessage").
/// Wire format (observed 2026-08-19):
/// {"name":"subscribeMessage","msg":{"name":"quotes.candle-generated","params":{"routingFilters":{"active_id":1912,"size":5}},"version":"1.0"},"request_id":"request_50","local_time":5531}
/// </summary>
public class SubscribeMessageEnvelope
{
    [JsonProperty("name")]
    public string Name { get; set; } = "subscribeMessage";

    [JsonProperty("msg")]
    public SubscribeMessageBody Msg { get; set; } = new();

    [JsonProperty("request_id")]
    public string RequestId { get; set; } = string.Empty;

    [JsonProperty("local_time")]
    public long LocalTime { get; set; }
}