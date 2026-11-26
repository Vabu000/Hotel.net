using Microsoft.Extensions.Diagnostics.HealthChecks;
using StackExchange.Redis;

namespace HotelReviews.WebApi.HealthChecks;

public sealed class ReviewsRedisHealthCheck(
    IConnectionMultiplexer redis,
    ILogger<ReviewsRedisHealthCheck> logger) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await redis.GetDatabase().PingAsync().WaitAsync(TimeSpan.FromSeconds(3), cancellationToken);
            return HealthCheckResult.Healthy("Reviews Redis cache is reachable");
        }
        catch (Exception ex) when (ex is RedisException or TimeoutException)
        {
            logger.LogWarning(ex, "Reviews Redis cache is unavailable; continuing with MongoDB and memory cache");
            return HealthCheckResult.Degraded("Reviews is operating without its Redis cache", ex);
        }
    }
}
