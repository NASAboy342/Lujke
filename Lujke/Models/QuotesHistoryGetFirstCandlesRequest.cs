using System;
using Newtonsoft.Json;

namespace Lujke.Models;

/// <summary>
/// Inner message for the ONE-SHOT historical fetch. Verified 2026-08-19 — the web client
/// uses "quotes-history.get-first-candles" (NOT "get-candles") to load a chart.
/// Wire format: {"name":"quotes-history.get-first-candles","version":"1.0","body":{"active_id":1912}}
/// Response:    {"request_id":"request_49","name":"candles","msg":{"candles":[...]},"status":2000}
/// Only "active_id" is required — no from_id/tail id needed (this is why v2.0 get-candles
/// with from_id=0 returned an empty list).
/// </summary>
public class QuotesHistoryGetFirstCandlesRequest
{
    [JsonProperty("name")]
    public string Name { get; set; } = "quotes-history.get-first-candles";

    [JsonProperty("version")]
    public string Version { get; set; } = "1.0";

    [JsonProperty("body")]
    public QuotesHistoryGetFirstCandlesBody Body { get; set; } = new();

    public QuotesHistoryGetFirstCandlesRequest(int activeId) => Body.ActiveId = activeId;
}

public class QuotesHistoryGetFirstCandlesBody
{
    [JsonProperty("active_id")]
    public int ActiveId { get; set; }
}
