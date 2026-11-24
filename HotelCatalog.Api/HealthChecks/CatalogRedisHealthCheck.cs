using Microsoft.Extensions.Diagnostics.HealthChecks;
using StackExchange.Redis;

namespace HotelCatalog.Api.HealthChecks;

public sealed class CatalogRedisHealthCheck : IHealthCheck
{
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<CatalogRedisHealthCheck> _logger;

    public CatalogRedisHealthCheck(
        IConnectionMultiplexer redis,
        ILogger<CatalogRedisHealthCheck> logger)
    {
        _redis = redis;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _redis.GetDatabase().PingAsync()
                .WaitAsync(TimeSpan.FromSeconds(2), cancellationToken);
            return HealthCheckResult.Healthy("Catalog Redis cache is reachable");
        }
        catch (Exception ex) when (ex is RedisException or TimeoutException)
        {
            _logger.LogWarning(ex, "Catalog Redis cache health check failed");
            return HealthCheckResult.Degraded("Catalog is operating without its Redis cache", ex);
        }
    }
}
