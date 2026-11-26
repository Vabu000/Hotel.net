using System.Security.Cryptography;

namespace HotelReviews.Domain.Common;

public abstract class BaseEntity
{
    public string Id { get; protected set; } = Convert.ToHexString(RandomNumberGenerator.GetBytes(12)).ToLowerInvariant();
    public DateTime CreatedAt { get; protected set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; protected set; }

    protected void UpdateTimestamp()
    {
        UpdatedAt = DateTime.UtcNow;
    }
    public bool IsNew => string.IsNullOrEmpty(Id);
}