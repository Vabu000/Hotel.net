using RabbitMQ.Client;

namespace Platform.EventBus;

public static class EventTopology
{
    public static async Task DeclareAsync(IChannel channel, CancellationToken cancellationToken)
    {
        await channel.ExchangeDeclareAsync(
            "catalog-events",
            ExchangeType.Topic,
            durable: true,
            cancellationToken: cancellationToken);
        await channel.ExchangeDeclareAsync(
            "catalog-events.dlx",
            ExchangeType.Fanout,
            durable: true,
            cancellationToken: cancellationToken);

        await DeclareConsumerQueueAsync(channel, "orders.catalog-sync", cancellationToken);
        await DeclareConsumerQueueAsync(channel, "reviews.catalog-sync", cancellationToken);
    }

    private static async Task DeclareConsumerQueueAsync(
        IChannel channel,
        string queueName,
        CancellationToken cancellationToken)
    {
        await channel.QueueDeclareAsync(
            queueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: new Dictionary<string, object?>
            {
                ["x-dead-letter-exchange"] = "catalog-events.dlx"
            },
            cancellationToken: cancellationToken);
        await channel.QueueBindAsync(
            queueName,
            "catalog-events",
            "product.*",
            cancellationToken: cancellationToken);

        var dlqName = $"{queueName}.dlq";
        await channel.QueueDeclareAsync(
            dlqName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            cancellationToken: cancellationToken);
        await channel.QueueBindAsync(
            dlqName,
            "catalog-events.dlx",
            "",
            cancellationToken: cancellationToken);
    }
}
