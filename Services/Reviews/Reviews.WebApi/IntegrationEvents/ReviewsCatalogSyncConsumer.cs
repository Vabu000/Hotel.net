using System.Text.Json;
using HotelReviews.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;
using Platform.EventBus;
using Platform.EventBus.IntegrationEvents;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace HotelReviews.WebApi.IntegrationEvents;

public sealed class ReviewsCatalogSyncConsumer(
    IConnection connection,
    IServiceScopeFactory scopeFactory,
    ILogger<ReviewsCatalogSyncConsumer> logger)
    : RabbitMqConsumerBase(connection, logger, EventTopology.ReviewsSyncQueue)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    protected override async Task ProcessMessageAsync(BasicDeliverEventArgs ea, CancellationToken cancellationToken)
    {
        Guid eventId;
        int categoryId;
        string categoryName;
        string? description = null;
        string eventType;

        if (ea.RoutingKey == EventTopology.RoomCategoryCreatedRoutingKey)
        {
            var evt = JsonSerializer.Deserialize<RoomCategoryCreatedIntegrationEvent>(ea.Body.Span, JsonOptions)
                ?? throw new JsonException("Empty RoomCategoryCreatedIntegrationEvent payload");
            eventId = evt.EventId;
            categoryId = evt.CategoryId;
            categoryName = evt.Name;
            description = evt.Description;
            eventType = nameof(RoomCategoryCreatedIntegrationEvent);
        }
        else if (ea.RoutingKey == EventTopology.RoomCategoryRenamedRoutingKey)
        {
            var evt = JsonSerializer.Deserialize<RoomCategoryRenamedEvent>(ea.Body.Span, JsonOptions)
                ?? throw new JsonException("Empty RoomCategoryRenamedEvent payload");
            eventId = evt.EventId;
            categoryId = evt.CategoryId;
            categoryName = evt.NewName;
            eventType = nameof(RoomCategoryRenamedEvent);
        }
        else
        {
            throw new JsonException($"Unexpected Reviews routing key '{ea.RoutingKey}'");
        }

        await using var scope = scopeFactory.CreateAsyncScope();
        var mongo = scope.ServiceProvider.GetRequiredService<MongoDbContext>();
        var processed = mongo.Database.GetCollection<BsonDocument>("processed_events");

        var alreadyProcessed = await processed
            .Find(Builders<BsonDocument>.Filter.Eq("_id", eventId.ToString()))
            .AnyAsync(cancellationToken);

        if (alreadyProcessed)
        {
            logger.LogInformation("Duplicate event {EventId} skipped by Reviews", eventId);
            return;
        }

        var roomCategories = mongo.Database.GetCollection<BsonDocument>("catalog_room_categories");
        var categoryProjection = new BsonDocument
        {
            ["_id"] = categoryId,
            ["categoryId"] = categoryId,
            ["name"] = categoryName,
            ["updatedAt"] = DateTime.UtcNow,
            ["lastEventId"] = eventId.ToString()
        };
        if (description is not null)
        {
            categoryProjection["description"] = description;
        }

        await roomCategories.ReplaceOneAsync(
            Builders<BsonDocument>.Filter.Eq("_id", categoryId),
            categoryProjection,
            new ReplaceOptions { IsUpsert = true },
            cancellationToken);

        await processed.InsertOneAsync(
            new BsonDocument
            {
                ["_id"] = eventId.ToString(),
                ["eventType"] = eventType,
                ["processedAt"] = DateTime.UtcNow
            },
            cancellationToken: cancellationToken);

        logger.LogInformation(
            "Replicated room category {CategoryId} ({CategoryName}) to Reviews via {EventType} {EventId}",
            categoryId, categoryName, eventType, eventId);
    }
}
