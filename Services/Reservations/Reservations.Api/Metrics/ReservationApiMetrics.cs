using System.Diagnostics.Metrics;

namespace HotelReservation.Api.Metrics;

/// <summary>
/// Business metrics for the Hotel Reservations API exported via OpenTelemetry.
/// </summary>
public sealed class ReservationApiMetrics
{
    private readonly Counter<long> _reservationsCreated;
    private readonly Histogram<double> _operationDuration;
    private long _activeOperations;

    public ReservationApiMetrics(IMeterFactory meterFactory)
    {
        var meter = meterFactory.Create("HotelPlatform.Reservation");
        _reservationsCreated = meter.CreateCounter<long>(
            "reservations.created",
            unit: "{reservation}",
            description: "Number of successfully created hotel reservations");
        _operationDuration = meter.CreateHistogram<double>(
            "reservations.operation.duration",
            unit: "ms",
            description: "Duration of reservation business operations");
        meter.CreateObservableGauge(
            "reservations.operations.active",
            () => Interlocked.Read(ref _activeOperations),
            unit: "{operation}",
            description: "Reservation business operations currently in progress");
    }

    public void IncrementActive() => Interlocked.Increment(ref _activeOperations);
    public void DecrementActive() => Interlocked.Decrement(ref _activeOperations);

    public void RecordCreated(string status) =>
        _reservationsCreated.Add(1, new KeyValuePair<string, object?>("reservation.status", status));

    public void RecordDuration(string operation, bool success, double durationMs) =>
        _operationDuration.Record(durationMs,
            new KeyValuePair<string, object?>("operation", operation),
            new KeyValuePair<string, object?>("status", success ? "success" : "failure"));
}
