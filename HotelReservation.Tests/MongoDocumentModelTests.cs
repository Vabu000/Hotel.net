using HotelReviews.Domain.Entities;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;

namespace HotelReservation.Tests;

public sealed class MongoDocumentModelTests
{
    [Fact]
    public void DiscussionEmbedsMessagesAndKeepsLogicalReferences()
    {
        var discussion = Discussion.Create(101, "Room question", "review-123");
        discussion.AddMessage(5, "Is breakfast included?");

        var document = discussion.ToBsonDocument();

        Assert.Equal(101, document["roomId"].AsInt32);
        Assert.Equal("review-123", document["reviewId"].AsString);
        Assert.Single(document["messages"].AsBsonArray);
        Assert.Equal("Is breakfast included?", document["messages"][0]["text"].AsString);
    }

    [Fact]
    public void DiscussionPreservesUnmodeledMetadataForFlexibleSchema()
    {
        var id = ObjectId.GenerateNewId();
        var source = new BsonDocument
        {
            ["_id"] = id,
            ["createdAt"] = DateTime.UtcNow,
            ["version"] = 1,
            ["roomId"] = 101,
            ["subject"] = "Accessibility",
            ["messages"] = new BsonArray(),
            ["moderation"] = new BsonDocument("labels", new BsonArray { "accessibility" })
        };

        var discussion = BsonSerializer.Deserialize<Discussion>(source);
        var roundTripped = discussion.ToBsonDocument();

        Assert.True(roundTripped.Contains("moderation"));
        Assert.Equal("accessibility", roundTripped["moderation"]["labels"][0].AsString);
    }
}
