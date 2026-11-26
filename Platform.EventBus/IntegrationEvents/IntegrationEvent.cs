namespace Platform.EventBus.IntegrationEvents;

public abstract record IntegrationEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime OccurredAt { get; init; } = DateTime.UtcNow;
    public string? CorrelationId { get; init; } = System.Diagnostics.Activity.Current?.TraceId.ToString();
}
