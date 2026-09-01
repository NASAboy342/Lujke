using System;
using Newtonsoft.Json;

namespace Lujke.Models;

/// <summary>
/// Keep-alive frame the server expects roughly every 5 seconds, otherwise the connection drops.
/// Wire format: {"resource":"ping","timestamp":1787152222559}
/// </summary>
public class SocketPingFrame
{
    [JsonProperty("resource")]
    public string Resource { get; set; } = "ping";

    [JsonProperty("timestamp")]
    public long Timestamp { get; set; } = DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond;

    public static SocketPingFrame Create() => new();
}