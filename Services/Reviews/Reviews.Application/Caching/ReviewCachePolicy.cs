namespace HotelReviews.Application.Caching;

public static class ReviewCachePolicy
{
    public static TimeSpan L1Expiration { get; } = TimeSpan.FromMinutes(2);
    public static TimeSpan L2Expiration { get; } = TimeSpan.FromMinutes(10);

    public static string ReviewById(string reviewId) => $"review:id:{reviewId}";

    public static string ReviewsByRoomPrefix(int roomId) => $"reviews:room:{roomId}";

    public static string ReviewsByRoomQuery(
        int roomId,
        int page,
        int pageSize,
        int? minRating,
        bool? isVerified,
        bool? isApproved) =>
        $"{ReviewsByRoomPrefix(roomId)}:page:{page}:size:{pageSize}:rating:{minRating}:verified:{isVerified}:approved:{isApproved}";
}