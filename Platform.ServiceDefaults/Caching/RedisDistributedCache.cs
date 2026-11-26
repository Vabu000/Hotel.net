using Microsoft.Extensions.Caching.Distributed;
using StackExchange.Redis;

namespace HotelPlatform.ServiceDefaults.Caching;

public sealed class RedisDistributedCache : IDistributedCache
{
    private readonly IDatabase _database;

    public RedisDistributedCache(IConnectionMultiplexer connection)
    {
        _database = connection.GetDatabase();
    }

    public byte[]? Get(string key)
    {
        var value = _database.StringGet(key);
        return value.IsNull ? null : (byte[]?)value;
    }

    public async Task<byte[]?> GetAsync(string key, CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
        var value = await _database.StringGetAsync(key).ConfigureAwait(false);
        return value.IsNull ? null : (byte[]?)value;
    }

    public void Refresh(string key)
    {
        // Entries use absolute expiration; no sliding refresh is required.
    }

    public Task RefreshAsync(string key, CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
        return Task.CompletedTask;
    }

    public void Remove(string key) => _database.KeyDelete(key);

    public async Task RemoveAsync(string key, CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
        await _database.KeyDeleteAsync(key).ConfigureAwait(false);
    }

    public void Set(string key, byte[] value, DistributedCacheEntryOptions options)
    {
        _database.StringSet(key, value, GetExpiration(options));
    }

    public async Task SetAsync(
        string key,
        byte[] value,
        DistributedCacheEntryOptions options,
        CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
        await _database.StringSetAsync(key, value, GetExpiration(options)).ConfigureAwait(false);
    }

    private static TimeSpan? GetExpiration(DistributedCacheEntryOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (options.AbsoluteExpirationRelativeToNow is { } relative)
        {
            return relative;
        }

        if (options.AbsoluteExpiration is { } absolute)
        {
            return absolute - DateTimeOffset.UtcNow;
        }

        return options.SlidingExpiration;
    }
}
