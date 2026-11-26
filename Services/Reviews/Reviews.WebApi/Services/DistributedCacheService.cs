using HotelReviews.Application.Common.Interfaces;
using Microsoft.Extensions.Caching.Distributed;
using StackExchange.Redis;
using System.Text.Json;

namespace HotelReviews.WebApi.Services;

public class DistributedCacheService : IDistributedCacheService
{
    private readonly IConnectionMultiplexer _redis;
    private readonly IDistributedCache _cache;
    private readonly ILogger<DistributedCacheService> _logger;
    private readonly IDatabase _database;

    public DistributedCacheService(
        IConnectionMultiplexer redis,
        IDistributedCache cache,
        ILogger<DistributedCacheService> logger)
    {
        _redis = redis;
        _cache = cache;
        _logger = logger;
        _database = _redis.GetDatabase();
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            var value = await _cache.GetStringAsync(key, cancellationToken);

            if (string.IsNullOrEmpty(value))
            {
                _logger.LogInformation("Redis Cache MISS: {CacheKey}", key);
                return default;
            }

            _logger.LogInformation("Redis Cache HIT: {CacheKey}", key);
            return JsonSerializer.Deserialize<T>(value);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Redis GET error for key: {CacheKey}", key);
            return default;
        }
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var ttl = expiration ?? TimeSpan.FromMinutes(10);

            await _cache.SetStringAsync(
                key,
                JsonSerializer.Serialize(value),
                new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = ttl },
                cancellationToken);

            _logger.LogInformation("Redis Cache SET: {CacheKey}, Expiration: {Expiration}", key, ttl);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Redis SET error for key: {CacheKey}", key);
        }
    }

    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            await _cache.RemoveAsync(key, cancellationToken);
            _logger.LogInformation("Redis Cache REMOVE: {CacheKey}", key);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Redis REMOVE error for key: {CacheKey}", key);
        }
    }

    public async Task RemoveByPrefixAsync(string prefix, CancellationToken cancellationToken = default)
    {
        try
        {
            var server = _redis.GetServer(_redis.GetEndPoints().First());
            var keys = server.Keys(pattern: $"hotel:reviews:distributed:{prefix}*").ToArray();

            if (keys.Length > 0)
            {
                await _database.KeyDeleteAsync(keys);
                _logger.LogInformation("Redis Cache REMOVE BY PREFIX: {Prefix}, Count: {Count}", prefix, keys.Length);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Redis REMOVE BY PREFIX error for: {Prefix}", prefix);
        }
    }
}
