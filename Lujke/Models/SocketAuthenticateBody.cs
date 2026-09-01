using System;
using Newtonsoft.Json;

namespace Lujke.Models;

public class SocketAuthenticateBody
{
    [JsonProperty("ssid")]
    public string Ssid { get; set; } = string.Empty;

    [JsonProperty("protocol")]
    public int Protocol { get; set; } = 3;
}