using HotelReviews.Domain.Common;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace HotelReviews.Domain.Entities;

/// <summary>
/// A discussion is a document aggregate: a bounded set of recent messages is embedded,
/// while room/review ownership is represented by logical references only.
/// </summary>
[BsonCollection("discussions")]
public sealed class Discussion : BaseEntity
{
    [BsonElement("roomId")]
    public int RoomId { get; private set; }

    [BsonElement("reviewId")]
    [BsonIgnoreIfNull]
    public string? ReviewId { get; private set; }

    [BsonElement("subject")]
    public string Subject { get; private set; } = string.Empty;

    [BsonElement("messages")]
    public List<DiscussionMessage> Messages { get; private set; } = [];

    [BsonExtraElements]
    public BsonDocument? AdditionalData { get; private set; }

    private Discussion() { }

    public static Discussion Create(int roomId, string subject, string? reviewId = null)
    {
        if (roomId <= 0) throw new ArgumentOutOfRangeException(nameof(roomId));
        if (string.IsNullOrWhiteSpace(subject)) throw new ArgumentException("Subject is required.", nameof(subject));

        return new Discussion { RoomId = roomId, Subject = subject.Trim(), ReviewId = reviewId };
    }

    public void AddMessage(int clientId, string text)
    {
        if (clientId <= 0) throw new ArgumentOutOfRangeException(nameof(clientId));
        if (string.IsNullOrWhiteSpace(text)) throw new ArgumentException("Message text is required.", nameof(text));
        if (Messages.Count >= 100) throw new InvalidOperationException("A discussion may contain at most 100 embedded messages.");

        Messages.Add(new DiscussionMessage(clientId, text.Trim(), DateTime.UtcNow));
        BumpVersion();
    }

    public void SetMetadata(string key, BsonValue value)
    {
        if (string.IsNullOrWhiteSpace(key)) throw new ArgumentException("Metadata key is required.", nameof(key));
        if (key is "roomId" or "reviewId" or "subject" or "messages" or "_id")
            throw new ArgumentException($"'{key}' is a reserved discussion field.", nameof(key));

        AdditionalData ??= new BsonDocument();
        AdditionalData[key] = value ?? BsonNull.Value;
        BumpVersion();
    }
}

public sealed class DiscussionMessage
{
    [BsonElement("clientId")]
    public int ClientId { get; private set; }

    [BsonElement("text")]
    public string Text { get; private set; } = string.Empty;

    [BsonElement("createdAt")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime CreatedAt { get; private set; }

    private DiscussionMessage() { }

    internal DiscussionMessage(int clientId, string text, DateTime createdAt)
    {
        ClientId = clientId;
        Text = text;
        CreatedAt = createdAt;
    }
}
