using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace HotelReservation.Bll.Services;

public sealed class ReservationMetrics
{
    private readonly Counter<long> _created;
    private readonly Histogram<double> _operationDuration;
    private long _activeOperations;

    public ReservationMetrics(IMeterFactory meterFactory)
    {
        var meter = meterFactory.Create("HotelPlatform.Reservation");
        _created = meter.CreateCounter<long>(
            "orders.created",
            unit: "orders",
            description: "Number of successfully created reservations");
        _operationDuration = meter.CreateHistogram<double>(
            "orders.operation.duration",
            unit: "ms",
            description: "Duration of reservation business operations");
        meter.CreateObservableGauge(
            "orders.operations.active",
            () => Interlocked.Read(ref _activeOperations),
            unit: "operations",
            description: "Reservation business operations currently in progress");
    }

    public IDisposable TrackOperation(string operation)
    {
        Interlocked.Increment(ref _activeOperations);
        return new OperationMeasurement(_operationDuration, operation, () => Interlocked.Decrement(ref _activeOperations));
    }

    public void RecordCreated(string status) =>
        _created.Add(1, new KeyValuePair<string, object?>("order.status", status));

    private sealed class OperationMeasurement : IDisposable
    {
        private readonly Histogram<double> _histogram;
        private readonly string _operation;
        private readonly Action _onDispose;
        private readonly long _startedAt = Stopwatch.GetTimestamp();
        private bool _disposed;

        public OperationMeasurement(Histogram<double> histogram, string operation, Action onDispose)
        {
            _histogram = histogram;
            _operation = operation;
            _onDispose = onDispose;
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _histogram.Record(
                Stopwatch.GetElapsedTime(_startedAt).TotalMilliseconds,
                new KeyValuePair<string, object?>("operation", _operation));
            _onDispose();
        }
    }
}
