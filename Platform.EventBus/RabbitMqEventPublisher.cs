using System.Diagnostics;
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
        await EventTopology.DeclareAsync(channel, cancellationToken);
        using var activity = TracePropagation.Source.StartActivity(
            $"{typeof(TEvent).Name} publish",
            ActivityKind.Producer);
        activity?.SetTag("messaging.system", "rabbitmq");
        activity?.SetTag("messaging.destination.name", exchange);
        activity?.SetTag("messaging.destination.routing_key", routingKey);
        activity?.SetTag("messaging.message.id", integrationEvent.EventId.ToString());
        activity?.SetTag("messaging.operation.name", "publish");
        activity?.SetTag("messaging.operation.type", "publish");

        var properties = new BasicProperties
        {
            Persistent = true,
            ContentType = "application/json",
            MessageId = integrationEvent.EventId.ToString(),
            Type = typeof(TEvent).Name,
            Headers = new Dictionary<string, object?>
            {
                ["schema-version"] = "1"
            }
        };

        if (!string.IsNullOrWhiteSpace(integrationEvent.CorrelationId))
        {
            properties.Headers["x-correlation-id"] = integrationEvent.CorrelationId;
        }

        TracePropagation.Inject(properties);

        var body = JsonSerializer.SerializeToUtf8Bytes(integrationEvent, JsonOptions);
        try
        {
            await channel.BasicPublishAsync(
                exchange,
                routingKey,
                mandatory: false,
                properties,
                body,
                cancellationToken);
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            throw;
        }

        logger.LogInformation(
            "Published {EventType} {EventId} with CorrelationId {CorrelationId} to {Exchange}/{RoutingKey}",
            typeof(TEvent).Name,
            integrationEvent.EventId,
            integrationEvent.CorrelationId,
            exchange,
            routingKey);
    }
}
