using Newtonsoft.Json;

namespace Lujke.Vue.Models;

/// <summary>
/// Response envelope for "quotes-history.get-candles". The historical batch, when
/// returned, arrives wrapped in the standard {name, msg:{...}} envelope; msg is
/// expected to carry {active_id, size, candles:[...]} (confirm exact nesting once a
/// real get-candles reply is captured — the live stream is confirmed, the batch is not).
/// </summary>
public class GetCandlesResponse
{
    [JsonProperty("candles")]
    public List<IQOptionCandle>? Candles { get; set; }
}
