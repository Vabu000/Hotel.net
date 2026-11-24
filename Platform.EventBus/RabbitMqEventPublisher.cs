using System.Text.Json;
using Microsoft.Extensions.Logging;
using Platform.EventBus.IntegrationEvents;
using RabbitMQ.Client;

namespace Platform.EventBus;

public sealed class RabbitMqEventPublisher(
    IConnection connection,
    ILogger<RabbitMqEventPublisher> logger) : IEventPublisher
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public async Task PublishAsync<TEvent>(
        TEvent integrationEvent,
        string exchange,
        string routingKey,
        CancellationToken cancellationToken = default)
        where TEvent : IntegrationEvent
    {
        await using var channel = await connection.CreateChannelAsync(cancellationToken: cancellationToken);
        var properties = new BasicProperties
        {
            Persistent = true,
            ContentType = "application/json",
            MessageId = integrationEvent.EventId.ToString(),
            Type = typeof(TEvent).Name,
            Headers = new Dictionary<string, object?>
            {
                ["x-correlation-id"] = integrationEvent.CorrelationId,
                ["schema-version"] = "1"
            }
        };

        var body = JsonSerializer.SerializeToUtf8Bytes(integrationEvent, JsonOptions);
        await channel.BasicPublishAsync(
            exchange,
            routingKey,
            mandatory: false,
            properties,
            body,
            cancellationToken);

        logger.LogInformation(
            "Published {EventType} {EventId} to {Exchange}/{RoutingKey}",
            typeof(TEvent).Name,
            integrationEvent.EventId,
            exchange,
            routingKey);
    }
}
