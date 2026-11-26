using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace HotelCatalog.Api.Services;

public sealed class CatalogMetrics
{
    private readonly Counter<long> _cacheOperations;
    private readonly Histogram<double> _cacheOperationDuration;

    public CatalogMetrics(IMeterFactory meterFactory)
    {
        var meter = meterFactory.Create("HotelCatalog");
        _cacheOperations = meter.CreateCounter<long>(
            "hotel.catalog.cache.operations",
            unit: "operations",
            description: "Catalog cache reads, misses, fallbacks and invalidations");
        _cacheOperationDuration = meter.CreateHistogram<double>(
            "hotel.catalog.cache.duration",
            unit: "ms",
            description: "Duration of catalog cache reads");
    }

    public void RecordCacheOperation(string level, string outcome, double durationMilliseconds)
    {
        var tags = new TagList
        {
            { "cache.level", level },
            { "outcome", outcome }
        };
        _cacheOperations.Add(1, tags);
        _cacheOperationDuration.Record(durationMilliseconds, tags);
    }
}
