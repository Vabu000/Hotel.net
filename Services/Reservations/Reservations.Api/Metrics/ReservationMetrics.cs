using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace HotelReservation.Api.Metrics;

public sealed class ReservationApiMetrics
{
    private readonly Counter<long> _requests;
    private readonly Counter<long> _reservationsCreated;
    private readonly Counter<long> _statusChanges;
    private readonly Histogram<double> _requestDuration;
    private long _activeOperations;

    public ReservationApiMetrics(IMeterFactory meterFactory)
    {
        var meter = meterFactory.Create("HotelPlatform.Reservation");
        _requests = meter.CreateCounter<long>(
            "hotel.reservation.api.requests",
            unit: "requests",
            description: "Reservation API requests by route and status class");
        _reservationsCreated = meter.CreateCounter<long>(
            "hotel.reservation.created",
            unit: "reservations",
            description: "Reservations accepted by the API");
        _statusChanges = meter.CreateCounter<long>(
            "hotel.reservation.status.changes",
            unit: "changes",
            description: "Reservation status change operations");
        _requestDuration = meter.CreateHistogram<double>(
            "hotel.reservation.api.duration",
            unit: "ms",
            description: "Reservation API request duration");
        meter.CreateObservableGauge(
            "hotel.reservation.api.operations.active",
            () => Interlocked.Read(ref _activeOperations),
            unit: "operations",
            description: "Number of active Reservation API operations");
    }

    public IDisposable TrackActiveOperation()
    {
        Interlocked.Increment(ref _activeOperations);
        return new OperationScope(() => Interlocked.Decrement(ref _activeOperations));
    }

    public void RecordRequest(
        string routeTemplate,
        string method,
        string statusClass,
        double durationMilliseconds)
    {
        var tags = new TagList
        {
            { "route", routeTemplate },
            { "method", method },
            { "status.class", statusClass }
        };
        _requests.Add(1, tags);
        _requestDuration.Record(durationMilliseconds, tags);
    }

    public void RecordCreated(string statusClass) =>
        _reservationsCreated.Add(1, new TagList { { "status.class", statusClass } });

    public void RecordStatusChange(string statusClass) =>
        _statusChanges.Add(1, new TagList { { "status.class", statusClass } });

    private sealed class OperationScope(Action onDispose) : IDisposable
    {
        private Action? _onDispose = onDispose;

        public void Dispose() => Interlocked.Exchange(ref _onDispose, null)?.Invoke();
    }
}
