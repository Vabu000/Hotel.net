using RabbitMQ.Client;

namespace Platform.EventBus;

public static class EventTopology
{
    public const string CatalogExchange = "catalog-events";
    public const string CatalogDlxExchange = "catalog-events.dlx";
    public const string ReservationsSyncQueue = "reservations.catalog-sync";
    public const string ReviewsSyncQueue = "reviews.catalog-sync";
    public const string ServicePriceChangedRoutingKey = "catalog.service-price-changed";
    public const string RoomCategoryCreatedRoutingKey = "catalog.room-category-created";
    public const string RoomCategoryRenamedRoutingKey = "catalog.room-category-renamed";

    public static async Task DeclareAsync(IChannel channel, CancellationToken cancellationToken)
    {
        await channel.ExchangeDeclareAsync(
            CatalogExchange,
            ExchangeType.Topic,
            durable: true,
            cancellationToken: cancellationToken);

        await channel.ExchangeDeclareAsync(
            CatalogDlxExchange,
            ExchangeType.Direct,
            durable: true,
            cancellationToken: cancellationToken);

        await DeclareConsumerQueueAsync(
            channel,
            ReservationsSyncQueue,
            cancellationToken,
            ServicePriceChangedRoutingKey);
        await DeclareConsumerQueueAsync(
            channel,
            ReviewsSyncQueue,
            cancellationToken,
            RoomCategoryCreatedRoutingKey,
            RoomCategoryRenamedRoutingKey);
    }

    private static async Task DeclareConsumerQueueAsync(
        IChannel channel,
        string queueName,
        CancellationToken cancellationToken,
        params string[] routingKeys)
    {
        await channel.QueueDeclareAsync(
            queueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: new Dictionary<string, object?>
            {
                ["x-dead-letter-exchange"] = CatalogDlxExchange,
                ["x-dead-letter-routing-key"] = $"{queueName}.dlq"
            },
            cancellationToken: cancellationToken);

        foreach (var routingKey in routingKeys)
        {
            await channel.QueueBindAsync(
                queueName,
                CatalogExchange,
                routingKey,
                cancellationToken: cancellationToken);
        }

        var dlqName = $"{queueName}.dlq";
        await channel.QueueDeclareAsync(
            dlqName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            cancellationToken: cancellationToken);

        await channel.QueueBindAsync(
            dlqName,
            CatalogDlxExchange,
            dlqName,
            cancellationToken: cancellationToken);
    }
}
