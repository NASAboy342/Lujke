using System;
using Newtonsoft.Json;

namespace Lujke.Models;

/// <summary>
/// Inner message to open the LIVE candle stream. Verified 2026-08-19.
/// Wire format: {"name":"quotes.candle-generated","params":{"routingFilters":{"active_id":1912,"size":5}},"version":"1.0"}
/// The server then pushes {"name":"candle-generated","msg":{...}} frames every tick.
/// </summary>
public class SubscribeMessageBody
{
    [JsonProperty("name")]
    public string Name { get; set; } = "quotes.candle-generated";

    [JsonProperty("params")]
    public SubscribeParams Params { get; set; } = new();

    [JsonProperty("version")]
    public string Version { get; set; } = "1.0";
}