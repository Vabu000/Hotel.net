using Microsoft.Extensions.Logging;
using Npgsql;
using Platform.EventBus.IntegrationEvents;

namespace HotelReservation.Api.IntegrationEvents;

public sealed class ProductCatalogChangedHandler(
    string connectionString,
    ILogger<ProductCatalogChangedHandler> logger)
{
    public async Task HandleAsync(ProductPriceChangedEvent eventData, CancellationToken cancellationToken)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        await using (var command = new NpgsqlCommand(
            """
            INSERT INTO processed_events (event_id, processed_at)
            VALUES (@EventId, NOW())
            ON CONFLICT (event_id) DO NOTHING;
            """,
            connection,
            transaction))
        {
            command.Parameters.AddWithValue("EventId", eventData.EventId);
            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        await using (var command = new NpgsqlCommand(
            """
            UPDATE room_services
            SET name_snapshot = @Name,
                price_snapshot = @Price,
                updatedat = NOW()
            WHERE service_id = @ProductId;
            """,
            connection,
            transaction))
        {
            command.Parameters.AddWithValue("ProductId", eventData.ProductId);
            command.Parameters.AddWithValue("Name", eventData.ProductName);
            command.Parameters.AddWithValue("Price", eventData.NewPrice);
            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);
        logger.LogInformation(
            "Applied catalog event {EventId} to reservation read model",
            eventData.EventId);
    }
}
