using System.Text.Json;
using Dapper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Npgsql;
using Platform.EventBus;
using Platform.EventBus.IntegrationEvents;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace HotelReservation.Api.IntegrationEvents;

/// <summary>
/// Consumes catalog price/name change events and updates the local Catalog read model
/// idempotently while keeping existing reservation_services booking snapshots untouched.
/// </summary>
public sealed class CatalogSyncConsumer(
    IConnection connection,
    IConfiguration configuration,
    ILogger<CatalogSyncConsumer> logger)
    : RabbitMqConsumerBase(connection, logger, EventTopology.ReservationsSyncQueue)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly string _connectionString = configuration.GetConnectionString("HotelReservationsDB")
        ?? throw new InvalidOperationException("Connection string 'HotelReservationsDB' is missing");

    protected override async Task ProcessMessageAsync(BasicDeliverEventArgs ea, CancellationToken cancellationToken)
    {
        var evt = JsonSerializer.Deserialize<CatalogServicePriceChangedEvent>(ea.Body.Span, JsonOptions)
            ?? throw new JsonException("Empty CatalogServicePriceChangedEvent payload");

        await using var db = new NpgsqlConnection(_connectionString);
        await db.OpenAsync(cancellationToken);
        await using var tx = await db.BeginTransactionAsync(cancellationToken);

        var inserted = await db.ExecuteAsync(
            new CommandDefinition(
                """
                INSERT INTO processed_events (event_id, event_type)
                VALUES (@EventId, @EventType)
                ON CONFLICT (event_id) DO NOTHING
                """,
                new { evt.EventId, EventType = nameof(CatalogServicePriceChangedEvent) },
                tx,
                cancellationToken: cancellationToken));

        if (inserted == 0)
        {
            logger.LogInformation("Duplicate event {EventId} skipped by Reservations", evt.EventId);
            await tx.RollbackAsync(cancellationToken);
            return;
        }

        await db.ExecuteAsync(
            new CommandDefinition(
                """
                INSERT INTO catalog_services (catalog_service_id, name, price, updatedat)
                VALUES (@ServiceId, @ServiceName, @NewPrice, now())
                ON CONFLICT (catalog_service_id) DO UPDATE
                SET name = EXCLUDED.name,
                    price = EXCLUDED.price,
                    updatedat = now()
                """,
                new { evt.ServiceId, evt.ServiceName, evt.NewPrice },
                tx,
                cancellationToken: cancellationToken));

        await tx.CommitAsync(cancellationToken);

        logger.LogInformation(
            "Replicated catalog service {ServiceId} ({ServiceName}, {NewPrice}) via event {EventId}",
            evt.ServiceId, evt.ServiceName, evt.NewPrice, evt.EventId);
    }
}
