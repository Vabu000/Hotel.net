using HotelReviews.Domain.Common;

namespace HotelReviews.Domain.Entities;

/// <summary>
/// A discussion is a document aggregate: a bounded set of recent messages is embedded,
/// while room/review ownership is represented by logical references only.
/// </summary>
public sealed class Discussion : BaseEntity
{
    public int RoomId { get; private set; }

    public string? ReviewId { get; private set; }

    public string Subject { get; private set; } = string.Empty;

    public List<DiscussionMessage> Messages { get; private set; } = [];

    public Dictionary<string, string> AdditionalData { get; private set; } = new(StringComparer.Ordinal);

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

    public void SetMetadata(string key, string value)
    {
        if (string.IsNullOrWhiteSpace(key)) throw new ArgumentException("Metadata key is required.", nameof(key));
        if (key is "roomId" or "reviewId" or "subject" or "messages" or "_id")
            throw new ArgumentException($"'{key}' is a reserved discussion field.", nameof(key));
        if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("Metadata value is required.", nameof(value));

        AdditionalData[key] = value;
        BumpVersion();
    }
}

public sealed class DiscussionMessage
{
    public int ClientId { get; private set; }

    public string Text { get; private set; } = string.Empty;

    public DateTime CreatedAt { get; private set; }

    private DiscussionMessage() { }

    internal DiscussionMessage(int clientId, string text, DateTime createdAt)
    {
        ClientId = clientId;
        Text = text;
        CreatedAt = createdAt;
    }
}
