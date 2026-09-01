namespace Lujke.Enums;

/// <summary>
/// Candle interval in seconds as used by the IQ Option PWA quotes protocol
/// (the "size" field of quotes-history.get-candles).
/// </summary>
public enum EnumIQOptionCandleSize
{
    FiveSeconds = 5,
    TenSeconds = 10,
    FifteenSeconds = 15,
    ThirtySeconds = 30,
    OneMinute = 60,
    TwoMinutes = 120,
    FiveMinutes = 300,
    TenMinutes = 600,
    FifteenMinutes = 900,
    ThirtyMinutes = 1800,
    OneHour = 3600,
    FourHours = 14400,
    OneDay = 86400
}
