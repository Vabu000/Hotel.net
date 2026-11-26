using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text.Json;
using HotelCatalog.Bll.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using StackExchange.Redis;

namespace HotelCatalog.Api.Services;

public sealed class CatalogCacheService : ICatalogCache, IHostedService
{
    private const string CachePrefix = "catalog:room-";
    private const string InvalidationChannel = "catalog:invalidate";
    private readonly IMemoryCache _memoryCache;
    private readonly IDatabase _database;
    private readonly ISubscriber _subscriber;
    private readonly ILogger<CatalogCacheService> _logger;
    private readonly CatalogMetrics _metrics;
    private readonly ConcurrentDictionary<string, byte> _localKeys = new();

    public CatalogCacheService(
        IMemoryCache memoryCache,
        IConnectionMultiplexer redis,
        CatalogMetrics metrics,
        ILogger<CatalogCacheService> logger)
    {
        _memoryCache = memoryCache;
        _database = redis.GetDatabase();
        _subscriber = redis.GetSubscriber();
        _metrics = metrics;
        _logger = logger;

    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _subscriber.SubscribeAsync(RedisChannel.Literal(InvalidationChannel), (_, value) =>
            {
                if (!value.IsNullOrEmpty && value.ToString() != Environment.MachineName)
                {
                    RemoveLocalEntries();
                }
            });
        }
        catch (Exception ex) when (ex is RedisConnectionException or RedisTimeoutException)
        {
            _logger.LogWarning(ex, "Redis unavailable; catalog L1 invalidation notifications are not subscribed");
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _subscriber.UnsubscribeAsync(RedisChannel.Literal(InvalidationChannel));
        }
        catch (Exception ex) when (ex is RedisConnectionException or RedisTimeoutException)
        {
            _logger.LogDebug(ex, "Could not unsubscribe catalog cache invalidation notifications");
        }
    }

    public async Task<T?> GetOrCreateAsync<T>(
        string key,
        Func<Task<T?>> factory,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        if (_memoryCache.TryGetValue(key, out T? memoryValue))
        {
            _metrics.RecordCacheOperation("L1", "hit", stopwatch.Elapsed.TotalMilliseconds);
            _logger.LogDebug("Catalog cache L1 hit for {CacheKey}", key);
            return memoryValue;
        }

        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            var cachedJson = await _database.StringGetAsync(key);
            if (!cachedJson.IsNullOrEmpty)
            {
                var redisValue = JsonSerializer.Deserialize<T>(cachedJson.ToString());
                if (redisValue is not null)
                {
                    StoreLocally(key, redisValue);
                    _metrics.RecordCacheOperation("L2", "hit", stopwatch.Elapsed.TotalMilliseconds);
                    _logger.LogDebug("Catalog cache L2 hit for {CacheKey}", key);
                    return redisValue;
                }
            }
        }
        catch (Exception ex) when (ex is RedisConnectionException or RedisTimeoutException)
        {
            _metrics.RecordCacheOperation("L2", "fallback", stopwatch.Elapsed.TotalMilliseconds);
            _logger.LogWarning(ex, "Redis unavailable; loading catalog value from database for {CacheKey}", key);
        }

        var value = await factory();
        _metrics.RecordCacheOperation("database", "miss", stopwatch.Elapsed.TotalMilliseconds);
        if (value is null)
        {
            return default;
        }

        StoreLocally(key, value);
        try
        {
            await _database.StringSetAsync(key, JsonSerializer.Serialize(value), TimeSpan.FromMinutes(10));
        }
        catch (Exception ex) when (ex is RedisConnectionException or RedisTimeoutException)
        {
            _logger.LogWarning(ex, "Redis unavailable; catalog value not shared for {CacheKey}", key);
        }

        return value;
    }

    public async Task InvalidateRoomCatalogAsync(CancellationToken cancellationToken = default)
    {
        RemoveLocalEntries();
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            var endpoints = _subscriber.Multiplexer.GetEndPoints();
            if (endpoints.Length > 0)
            {
                var server = _subscriber.Multiplexer.GetServer(endpoints[0]);
                var keys = server.Keys(pattern: $"{CachePrefix}*").ToArray();
                if (keys.Length > 0)
                {
                    await _database.KeyDeleteAsync(keys);
                }
            }

            await _subscriber.PublishAsync(RedisChannel.Literal(InvalidationChannel), Environment.MachineName);
            _metrics.RecordCacheOperation("all", "invalidation", 0);
        }
        catch (Exception ex) when (ex is RedisConnectionException or RedisTimeoutException)
        {
            _logger.LogWarning(ex, "Redis unavailable while invalidating catalog cache");
        }
    }

    private void StoreLocally<T>(string key, T value)
    {
        _memoryCache.Set(key, value, new MemoryCacheEntryOptions
        {
            Size = 1,
            SlidingExpiration = TimeSpan.FromSeconds(30),
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(2)
        });
        _localKeys.TryAdd(key, 0);
    }

    private void RemoveLocalEntries()
    {
        foreach (var key in _localKeys.Keys)
        {
            _memoryCache.Remove(key);
            _localKeys.TryRemove(key, out _);
        }
    }
}
