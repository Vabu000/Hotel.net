namespace HotelCatalog.Bll.Caching;

/// <summary>
/// Centralized cache key and TTL definitions for two-level caching (L1 Memory &lt; L2 Redis).
/// </summary>
public static class CatalogCachePolicy
{
    public static readonly TimeSpan L1Ttl = TimeSpan.FromMinutes(1);
    public static readonly TimeSpan L2Ttl = TimeSpan.FromMinutes(10);

    public static string ServiceById(int id) => $"catalog:service:{id}";
    public static string AllServices() => "catalog:services:all";
    public static string RoomCategoryById(int id) => $"catalog:room-category:{id}";
    public static string RoomCategoryPaged(int page, int pageSize) => $"catalog:room-categories:p{page}:s{pageSize}";
}
