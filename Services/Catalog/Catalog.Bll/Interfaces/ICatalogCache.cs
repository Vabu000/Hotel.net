namespace HotelCatalog.Bll.Interfaces;

public interface ICatalogCache
{
    Task<T?> GetOrCreateAsync<T>(
        string key,
        Func<Task<T?>> factory,
        CancellationToken cancellationToken = default);

    Task InvalidateRoomCatalogAsync(CancellationToken cancellationToken = default);
}
