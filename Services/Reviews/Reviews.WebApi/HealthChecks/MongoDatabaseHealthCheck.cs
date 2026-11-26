using HotelReviews.Infrastructure.Persistence;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace HotelReviews.WebApi.HealthChecks;

public sealed class MongoDatabaseHealthCheck : IHealthCheck
{
    private readonly MongoDbContext _context;

    public MongoDatabaseHealthCheck(MongoDbContext context)
    {
        _context = context;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var isConnected = await _context.CheckConnectionAsync(cancellationToken);
        return isConnected
            ? HealthCheckResult.Healthy("MongoDB connected")
            : HealthCheckResult.Unhealthy("MongoDB not connected");
    }
}