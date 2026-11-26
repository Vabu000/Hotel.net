using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace HotelCatalog.Api.Metrics;

public sealed class CatalogMetrics
{
    private long _activeOperations;
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
        meter.CreateObservableGauge(
            "hotel.catalog.cache.operations.active",
            () => Interlocked.Read(ref _activeOperations),
            unit: "operations",
            description: "Number of in-flight catalog cache operations");
    }

    public IDisposable TrackOperation()
    {
        Interlocked.Increment(ref _activeOperations);
        return new OperationScope(() => Interlocked.Decrement(ref _activeOperations));
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

    private sealed class OperationScope(Action onDispose) : IDisposable
    {
        private Action? _onDispose = onDispose;

        public void Dispose() => Interlocked.Exchange(ref _onDispose, null)?.Invoke();
    }
}
