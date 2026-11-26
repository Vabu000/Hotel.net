using Microsoft.Extensions.Caching.Distributed;
using StackExchange.Redis;

namespace HotelReviews.WebApi.Services;

/// <summary>
/// IDistributedCache adapter backed by the Redis connection supplied by Aspire.
/// Hash fields keep sliding and absolute expiration metadata beside the cached bytes.
/// </summary>
public sealed class RedisDistributedCache(IConnectionMultiplexer redis) : IDistributedCache
{
    private const string ValueField = "value";
    private const string SlidingExpirationField = "sliding-expiration-ms";
    private const string AbsoluteExpirationField = "absolute-expiration-unix-ms";
    private const string KeyPrefix = "hotel:reviews:distributed:";
    private readonly IDatabase _database = redis.GetDatabase();

    public byte[]? Get(string key) => GetAsync(key).GetAwaiter().GetResult();

    public async Task<byte[]?> GetAsync(string key, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var entries = await _database.HashGetAllAsync(ToRedisKey(key)).ConfigureAwait(false);
        if (entries.Length == 0)
        {
            return null;
        }

        var value = entries.FirstOrDefault(entry => entry.Name == ValueField).Value;
        if (value.IsNull)
        {
            return null;
        }

        var slidingMilliseconds = ReadLong(entries, SlidingExpirationField);
        var absoluteExpirationUnixMilliseconds = ReadLong(entries, AbsoluteExpirationField);
        var expiration = GetExpiration(slidingMilliseconds, absoluteExpirationUnixMilliseconds);
        if (expiration is { } ttl)
        {
            if (ttl <= TimeSpan.Zero)
            {
                await _database.KeyDeleteAsync(ToRedisKey(key)).ConfigureAwait(false);
                return null;
            }

            await _database.KeyExpireAsync(ToRedisKey(key), ttl).ConfigureAwait(false);
        }

        return (byte[]?)value;
    }

    public void Refresh(string key) => RefreshAsync(key).GetAwaiter().GetResult();

    public async Task RefreshAsync(string key, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var metadata = await _database.HashGetAsync(
            ToRedisKey(key),
            [SlidingExpirationField, AbsoluteExpirationField]).ConfigureAwait(false);
        if (metadata.Length != 2)
        {
            return;
        }

        var expiration = GetExpiration(
            ReadLong(metadata[0]),
            ReadLong(metadata[1]));
        if (expiration is { } ttl)
        {
            if (ttl <= TimeSpan.Zero)
            {
                await _database.KeyDeleteAsync(ToRedisKey(key)).ConfigureAwait(false);
                return;
            }

            await _database.KeyExpireAsync(ToRedisKey(key), ttl).ConfigureAwait(false);
        }
    }

    public void Remove(string key) => RemoveAsync(key).GetAwaiter().GetResult();

    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await _database.KeyDeleteAsync(ToRedisKey(key)).ConfigureAwait(false);
    }

    public void Set(string key, byte[] value, DistributedCacheEntryOptions options) =>
        SetAsync(key, value, options).GetAwaiter().GetResult();

    public async Task SetAsync(
        string key,
        byte[] value,
        DistributedCacheEntryOptions options,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(value);
        ArgumentNullException.ThrowIfNull(options);
        cancellationToken.ThrowIfCancellationRequested();

        var absoluteExpirationUnixMilliseconds = GetAbsoluteExpiration(options)?.ToUnixTimeMilliseconds() ?? 0;
        var slidingMilliseconds = (long)(options.SlidingExpiration?.TotalMilliseconds ?? 0);
        await _database.HashSetAsync(ToRedisKey(key),
        [
            new HashEntry(ValueField, value),
            new HashEntry(SlidingExpirationField, slidingMilliseconds),
            new HashEntry(AbsoluteExpirationField, absoluteExpirationUnixMilliseconds)
        ]).ConfigureAwait(false);

        var expiration = GetExpiration(slidingMilliseconds, absoluteExpirationUnixMilliseconds);
        await _database.KeyExpireAsync(ToRedisKey(key), expiration).ConfigureAwait(false);
    }

    private static long ReadLong(HashEntry[] entries, string field)
    {
        var value = entries.FirstOrDefault(entry => entry.Name == field).Value;
        return ReadLong(value);
    }

    private static long ReadLong(HashEntry entry) => ReadLong(entry.Value);

    private static long ReadLong(RedisValue value) =>
        long.TryParse(value.ToString(), out var parsed) ? parsed : 0;

    private static DateTimeOffset? GetAbsoluteExpiration(DistributedCacheEntryOptions options)
    {
        if (options.AbsoluteExpiration is { } absoluteExpiration)
        {
            return absoluteExpiration;
        }

        return options.AbsoluteExpirationRelativeToNow is { } relativeExpiration
            ? DateTimeOffset.UtcNow.Add(relativeExpiration)
            : null;
    }

    private static TimeSpan? GetExpiration(long slidingMilliseconds, long absoluteExpirationUnixMilliseconds)
    {
        TimeSpan? sliding = slidingMilliseconds > 0
            ? TimeSpan.FromMilliseconds(slidingMilliseconds)
            : null;
        TimeSpan? absolute = absoluteExpirationUnixMilliseconds > 0
            ? DateTimeOffset.FromUnixTimeMilliseconds(absoluteExpirationUnixMilliseconds) - DateTimeOffset.UtcNow
            : null;

        if (sliding is null) return absolute;
        if (absolute is null) return sliding;
        return sliding < absolute ? sliding : absolute;
    }

    private static RedisKey ToRedisKey(string key) => $"{KeyPrefix}{key}";
}
