using System.Diagnostics.Metrics;

namespace HotelPlatform.ServiceDefaults.Metrics;

public static class CacheMetrics
{
    public const string MeterName = "HotelPlatform.Caching";
    private static readonly Meter Meter = new(MeterName);
    private static readonly Counter<long> Hits = Meter.CreateCounter<long>("cache.hits");
    private static readonly Counter<long> Misses = Meter.CreateCounter<long>("cache.misses");

    public static void RecordHit(string cacheName, string level) =>
        Hits.Add(1,
            new KeyValuePair<string, object?>("cache.name", cacheName),
            new KeyValuePair<string, object?>("cache.level", level));

    public static void RecordMiss(string cacheName, string level) =>
        Misses.Add(1,
            new KeyValuePair<string, object?>("cache.name", cacheName),
            new KeyValuePair<string, object?>("cache.level", level));
}
