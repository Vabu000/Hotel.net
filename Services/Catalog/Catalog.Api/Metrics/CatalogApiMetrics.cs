using System.Diagnostics.Metrics;

namespace HotelCatalog.Api.Metrics;

/// <summary>
/// Business and cache metrics for the Hotel Catalog API.
/// </summary>
public sealed class CatalogApiMetrics
{
    private readonly Counter<long> _cacheHits;
    private readonly Counter<long> _cacheMisses;
    private readonly Counter<long> _catalogMutations;
    private readonly Histogram<double> _operationDuration;

    public CatalogApiMetrics(IMeterFactory meterFactory)
    {
        var meter = meterFactory.Create("HotelCatalog.Api");
        _cacheHits = meter.CreateCounter<long>("catalog.cache.hits", unit: "{hit}", description: "Catalog cache hits");
        _cacheMisses = meter.CreateCounter<long>("catalog.cache.misses", unit: "{miss}", description: "Catalog cache misses");
        _catalogMutations = meter.CreateCounter<long>("catalog.mutations", unit: "{operation}", description: "Catalog write operations");
        _operationDuration = meter.CreateHistogram<double>("catalog.operation.duration", unit: "ms", description: "Duration of catalog operations");
    }

    public void RecordCacheHit(string level) =>
        _cacheHits.Add(1, new KeyValuePair<string, object?>("cache.level", level));

    public void RecordCacheMiss(string level) =>
        _cacheMisses.Add(1, new KeyValuePair<string, object?>("cache.level", level));

    public void RecordMutation(string entity, string action) =>
        _catalogMutations.Add(1,
            new KeyValuePair<string, object?>("entity", entity),
            new KeyValuePair<string, object?>("action", action));

    public void RecordDuration(string operation, bool success, double durationMs) =>
        _operationDuration.Record(durationMs,
            new KeyValuePair<string, object?>("operation", operation),
            new KeyValuePair<string, object?>("status", success ? "success" : "failure"));
}
