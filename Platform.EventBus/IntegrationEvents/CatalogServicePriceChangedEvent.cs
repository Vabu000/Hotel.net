namespace Platform.EventBus.IntegrationEvents;

public sealed record CatalogServicePriceChangedEvent(
    int ServiceId,
    string ServiceName,
    decimal OldPrice,
    decimal NewPrice) : IntegrationEvent;

public sealed record RoomCategoryRenamedEvent(
    int CategoryId,
    string OldName,
    string NewName) : IntegrationEvent;

public sealed record RoomCategoryCreatedIntegrationEvent(
    int CategoryId,
    string Name,
    string? Description) : IntegrationEvent;
