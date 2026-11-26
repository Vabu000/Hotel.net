namespace HotelReviews.Domain.Common;

public abstract class BaseEntity
{
    public string Id { get; protected set; } = Guid.NewGuid().ToString("N")[..24];
    public DateTime CreatedAt { get; protected set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; protected set; }
    public int Version { get; private set; }

    protected void UpdateTimestamp()
    {
        UpdatedAt = DateTime.UtcNow;
    }

    public void BumpVersion()
    {
        Version++;
    }

    public bool IsNew => string.IsNullOrEmpty(Id);
}
