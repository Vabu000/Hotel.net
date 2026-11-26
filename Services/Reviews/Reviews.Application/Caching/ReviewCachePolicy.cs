namespace HotelReviews.Application.Caching;

/// <summary>
/// Centralized cache key and TTL definitions for Reviews L1/L2 caching.
/// </summary>
public static class ReviewCachePolicy
{
    public static readonly TimeSpan L1Ttl = TimeSpan.FromMinutes(1);
    public static readonly TimeSpan L2Ttl = TimeSpan.FromMinutes(5);

    public static string ReviewById(string id) => $"reviews:item:{id}";
    public static string ReviewsByRoom(int roomId) => $"reviews:room:{roomId}";
}
