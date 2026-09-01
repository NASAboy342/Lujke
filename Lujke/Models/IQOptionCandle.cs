using System;
using Newtonsoft.Json;

namespace Lujke.Models;

/// <summary>
/// A single candle. Confirmed live shape from a "candle-generated" frame (2026-08-19):
/// {"active_id":1912,"size":5,"at":1787154461000000000,"from":1787154460,"to":1787154465,
///  "id":10976910,"open":4493.205,"close":4493.035,"min":4492.995,"max":4493.325,
///  "ask":4493.16,"bid":4492.91,"volume":26,"phase":"T"}
/// NOTE: "at" is in NANOseconds; "min"/"max" are the low/high; "bid"/"ask" are the live quotes.
/// </summary>
public class IQOptionCandle
{
    [JsonProperty("active_id")]
    public int ActiveId { get; set; }

    [JsonProperty("size")]
    public int Size { get; set; }

    /// <summary>Candle open time in NANOseconds since epoch.</summary>
    [JsonProperty("at")]
    public long At { get; set; }

    [JsonProperty("from")]
    public long From { get; set; }

    [JsonProperty("to")]
    public long To { get; set; }

    [JsonProperty("id")]
    public long Id { get; set; }

    [JsonProperty("open")]
    public decimal Open { get; set; }

    [JsonProperty("close")]
    public decimal Close { get; set; }

    [JsonProperty("min")]
    public decimal Min { get; set; }

    [JsonProperty("max")]
    public decimal Max { get; set; }

    [JsonProperty("bid")]
    public decimal Bid { get; set; }

    [JsonProperty("ask")]
    public decimal Ask { get; set; }

    [JsonProperty("volume")]
    public long Volume { get; set; }

    [JsonProperty("phase")]
    public string? Phase { get; set; }
}