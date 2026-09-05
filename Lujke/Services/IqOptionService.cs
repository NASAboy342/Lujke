using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Lujke.Vue.Enums;
using Lujke.Vue.Models;
using Newtonsoft.Json;

namespace Lujke.Vue.Services;

// ═══════════════════════════════════════════════════════════════════════════════
// Scraper
// ═══════════════════════════════════════════════════════════════════════════════

/// <summary>
/// Connects to the IQ Option PWA WebSocket (wss://ws.km.iqoption.com/echo/websocket)
/// and reads candle data. Full protocol verified live 2026-08-19:
///
///   1. Connect — NO token in URL. "identity" cookie in the handshake header is
///      OPTIONAL (server authenticates with just the ssid below).
///   2. Send {"name":"authenticate","msg":{"ssid":"...","protocol":3}} →
///      server replies {"name":"authenticated","msg":true}.
///   3. Historical: {"name":"sendMessage","msg":{"name":"quotes-history.get-first-candles",
///      "version":"1.0","body":{"active_id":1912}}}
///      → replies {"request_id":"...","name":"candles","msg":{"candles":[...]}}.
///      NOTE: the old "quotes-history.get-candles" (v2.0) needs a real from_id/tail id
///      and returns empty when given 0 — the web client uses get-first-candles instead.
///   4. Live stream: {"name":"subscribeMessage","msg":{"name":"quotes.candle-generated",
///      "params":{"routingFilters":{"active_id":1912,"size":5}},"version":"1.0"}}
///      → server pushes {"name":"candle-generated","msg":{"active_id":...,...}} per tick.
///   5. Keep-alive: {"resource":"ping","timestamp":ms} every ~5s.
///
/// ActiveId 1912 = Gold (spot ~4493), 1861 = EUR/USD (observed in live traffic).
/// Paste your live "ssid" cookie (DevTools → Application → Cookies on km.iqoption.com)
/// into Ssid. It rotates when the session changes.
/// </summary>
public class IqOptionService
{
    // ── Configuration ──────────────────────────────────────────────────────────
    private const string WebSocketUrl = "wss://ws.km.iqoption.com/echo/websocket";

    /// <summary>
    /// The "ssid" cookie from km.iqoption.com (DevTools → Application → Cookies).
    /// Used in the post-connect "authenticate" frame.
    /// </summary>
    private string _ssid = "";

    /// <summary>
    /// OPTIONAL. The "identity" cookie from km.iqoption.com, sent in the WS handshake
    /// Cookie header. Verified 2026-08-19: authentication succeeds with only the ssid,
    /// so this can be left empty. Kept here in case the server tightens its checks.
    /// </summary>
    private string _identityCookie = ""; // ← paste the full identity cookie value here

    private const int AuthProtocolVersion = 3;
    private const int PingIntervalSeconds = 5;

    // Asset to subscribe to
    private int _activeId = (int)EnumMarketAssetId.Gold; // Gold (1861 = EUR/USD, observed in live traffic)
    private const EnumIQOptionCandleSize CandleSize = EnumIQOptionCandleSize.FiveSeconds;
    private const int HistoricalCandleCount = 30; // how many history candles to keep printing
    private const int AuthWaitSeconds = 20;       // server sometimes replies a few seconds late

    private static long _localTimeOrigin = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    private int _requestSeq;
    private TaskCompletionSource<bool>? _authTcs;

    public EventHandler<IQOptionCandle>? OnCandleUpdated;

    public List<IQOptionCandle> LiveCandles { get; set; } = new();

    public EventHandler<string>? OnError;

    public EventHandler<string>? OnLogMessage;



    internal async Task Run()
    {
        ValidateSSID();
        ValidateIdentityCookie();
        using var ws = new ClientWebSocket();
        SetIdentityCookie(ws);
        var isConnected = await Connecting(ws);
        if (!isConnected) return;

        using var cts = new CancellationTokenSource();
        var pingLoop = Task.Run(() => StartPingLoopAsync(ws, cts.Token));

        try
        {
            await Authenticate(ws);
            await HistoricalCandle(ws);
            await SubscribeToTheLiveCandleStream(ws);
            await ReceiveLoopAsync(ws);
        }
        finally
        {
            await CloseConnections(ws, cts);
        }
    }

    private void ValidateIdentityCookie()
    {
        if (string.IsNullOrEmpty(_identityCookie))
        {
            throw new InvalidOperationException("Identity cookie is not set.");
        }
    }

    public void SetActiveId(int activeId)
    {
        _activeId = activeId;
    }
    public void SetSsid(string ssid)
    {
        _ssid = ssid;
    }
    
    public void SetIdentityCookie(string identityCookie)
    {
        _identityCookie = identityCookie;
    }

    private static async Task CloseConnections(ClientWebSocket ws, CancellationTokenSource cts)
    {
        cts.Cancel();
        if (ws.State == WebSocketState.Open)
        {
            await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "done", CancellationToken.None);
        }
    }

    private async Task SubscribeToTheLiveCandleStream(ClientWebSocket ws)
    {
        var request = CreateLiveCandleSubscription();
        await SendRawAsync(ws, JsonConvert.SerializeObject(request));
        Log($"Subscribed to live candle stream: active_id={_activeId}, size={(int)CandleSize}s");
    }

    private SubscribeMessageEnvelope CreateLiveCandleSubscription()
    {
        return new SubscribeMessageEnvelope
        {
            Msg = new SubscribeMessageBody
            {
                Name = "quotes.candle-generated",
                Params = new SubscribeParams
                {
                    RoutingFilters = new CandleRoutingFilters
                    {
                        ActiveId = _activeId,
                        Size = (int)CandleSize
                    }
                },
                Version = "1.0"
            },
            RequestId = NextRequestId(),
            LocalTime = LocalTimeMs()
        };
    }

    private async Task HistoricalCandle(ClientWebSocket ws)
    {
        // ── 1. Historical candles (get-first-candles: needs ONLY active_id) ───────
        var firstCandles = new QuotesHistoryGetFirstCandlesRequest(_activeId);
        var firstCandlesId = NextRequestId();
        await SendRawAsync(ws, JsonConvert.SerializeObject(
            SocketSendMessageEnvelope.Wrap(firstCandles, firstCandlesId, LocalTimeMs())));
        Log($"Sent quotes-history.get-first-candles for active_id={_activeId}");
    }

    private async Task Authenticate(ClientWebSocket ws)
    {
        // ── 0. Authenticate (required before the server will stream real data) ──
        var authTcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        _authTcs = authTcs;
        await SendAuthAsync(ws);


        var authenticated = (await Task.WhenAny(authTcs.Task, Task.Delay(AuthWaitSeconds * 1000))) == authTcs.Task
            && await authTcs.Task;
        Log(authenticated
            ? "✔ Authenticated successfully."
            : $"[!] No 'authenticated' reply within {AuthWaitSeconds}s — candle stream may be empty.");
    }

    private async Task<bool> Connecting(ClientWebSocket ws)
    {
        Log($"Connecting to {WebSocketUrl} ...");
        var connectTask = ws.ConnectAsync(new Uri(WebSocketUrl), CancellationToken.None);
        if (!connectTask.Wait(TimeSpan.FromSeconds(15)))
        {
            Log("Connection timed out.");
            return false;
        }
        await connectTask;
        Log($"Connected. State: {ws.State}");
        return true;
    }

    private void SetIdentityCookie(ClientWebSocket ws)
    {
        // .NET 8 ClientWebSocket cannot set arbitrary request headers (Origin/User-Agent),
        // BUT it CAN set the Cookie header, which is what IQ Option actually checks.
        if (!string.IsNullOrEmpty(_identityCookie))
        {
            ws.Options.SetRequestHeader("Cookie", $"identity={_identityCookie}");
            ws.Options.SetRequestHeader("User-Agent",
                "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/126.0 Safari/537.36");
        }
    }

    private void ValidateSSID()
    {
        if (string.IsNullOrEmpty(_ssid))
        {
            Log("[!] Ssid is empty — authentication will fail.");
            Log("    Paste the 'ssid' cookie value from km.iqoption.com into the const.");
        }
    }

    // ── Helpers ────────────────────────────────────────────────────────────────
    private async Task SendAuthAsync(ClientWebSocket ws)
    {
        var auth = new SocketAuthenticateEnvelope
        {
            Msg = new SocketAuthenticateBody { Ssid = _ssid, Protocol = AuthProtocolVersion },
            RequestId = "request_2",
            LocalTime = LocalTimeMs()
        };
        await SendRawAsync(ws, JsonConvert.SerializeObject(auth));
        Log($"Sent authenticate frame (ssid={_ssid}).");
    }
    private static async Task StartPingLoopAsync(ClientWebSocket ws, CancellationToken token)
    {
        while (!token.IsCancellationRequested && ws.State == WebSocketState.Open)
        {
            try
            {
                await SendRawAsync(ws, JsonConvert.SerializeObject(SocketPingFrame.Create()));
            }
            catch
            {
                break;
            }
            await Task.Delay(TimeSpan.FromSeconds(PingIntervalSeconds), token);
        }
    }

    private async Task ReceiveLoopAsync(ClientWebSocket ws)
    {
        // ── 3. Listen loop: print frames, parse candle responses ─────────
        var buffer = new byte[64 * 1024];
        var ms = new MemoryStream();

        while (ws.State == WebSocketState.Open)
        {
            ms.SetLength(0);
            WebSocketReceiveResult result;
            do
            {
                result = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                if (result.MessageType == WebSocketMessageType.Close)
                {
                    Log($"Server closed the connection: {result.CloseStatus}");
                    return;
                }
                ms.Write(buffer, 0, result.Count);
            } while (!result.EndOfMessage);

            HandleIncomingFrame(Encoding.UTF8.GetString(ms.GetBuffer(), 0, (int)ms.Length));
        }
    }

    private void HandleIncomingFrame(string raw)
    {
        if (IsIgnorableFrame(raw))
            return;

        if (raw.Contains("\"name\":\"authenticated\""))
        {
            HandleAuthenticationResponse(raw);
            return;
        }

        Log($"[recv] {Truncate(raw, 500)}");

        try
        {
            var envelope = JsonConvert.DeserializeObject<SocketEnvelopeGeneric>(raw);
            RouteIncomingEnvelope(envelope);
        }
        catch (JsonException ex)
        {
            Error($"Failed to parse incoming frame: {ex.Message}");
        }
    }

    private static bool IsIgnorableFrame(string raw) =>
        raw.Contains("\"resource\":\"events") ||
        raw.Contains("\"resource\":\"ping") ||
        raw.Contains("\"name\":\"timeSync\"");

    private void HandleAuthenticationResponse(string raw)
    {
        Log($"[auth] server replied: {Truncate(raw, 200)}");
        _authTcs?.TrySetResult(raw.Contains("\"msg\":true"));
    }

    private void RouteIncomingEnvelope(SocketEnvelopeGeneric? envelope)
    {
        if (envelope?.Msg is null)
            return;

        switch (envelope.Name)
        {
            case "candle-generated":
                HandleLiveCandle(envelope.Msg);
                break;
            case "candles":
                HandleHistoricalCandles(envelope.Msg);
                break;
        }
    }

    private void HandleLiveCandle(object message)
    {
        var candle = DeserializeMessage<IQOptionCandle>(message);
        if (candle is not null)
            UpdateLiveCandle(candle);
    }

    private void HandleHistoricalCandles(object message)
    {
        var response = DeserializeMessage<GetCandlesResponse>(message);
        var candles = response?.Candles;
        if (candles is not { Count: > 0 })
        {
            Log($"── history reply: {candles?.Count ?? 0} candle(s) (empty?) ──");
            return;
        }

        Log($"── history (get-first-candles): {candles.Count} candle(s) ──");
        foreach (var candle in candles.Take(HistoricalCandleCount))
            UpdateLiveCandle(candle);

        if (candles.Count > HistoricalCandleCount)
            Log($"    … {candles.Count - HistoricalCandleCount} more");
    }

    private static T? DeserializeMessage<T>(object message) =>
        JsonConvert.DeserializeObject<T>(JsonConvert.SerializeObject(message));

    private void Log(string message)
    {
        OnLogMessage?.Invoke(this, message);
    }

    private void Error(string message)
    {
        OnError?.Invoke(this, message);
    }

    private void UpdateLiveCandle(IQOptionCandle c)
    {
        var existingIndex = LiveCandles.FindIndex(candle => candle.Id == c.Id);
        if (existingIndex >= 0)
        {
            LiveCandles[existingIndex] = c;
        }
        else
        {
            LiveCandles.Add(c);
        }

        OnCandleUpdated?.Invoke(this, c);
    }

    private string NextRequestId() => $"request_{Interlocked.Increment(ref _requestSeq)}";

    private static long LocalTimeMs() => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - _localTimeOrigin;

    private static string Truncate(string s, int max) => s.Length <= max ? s : s[..max] + "…";

    private static async Task SendRawAsync(ClientWebSocket ws, string json)
    {
        var bytes = Encoding.UTF8.GetBytes(json);
        await ws.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, CancellationToken.None);
    }
}