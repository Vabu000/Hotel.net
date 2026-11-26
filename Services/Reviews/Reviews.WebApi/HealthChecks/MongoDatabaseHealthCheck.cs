using HotelReviews.Infrastructure.Persistence;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using MongoDB.Bson;
using MongoDB.Driver;

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
        try
        {
            await _context.Database.RunCommandAsync(
                (Command<BsonDocument>)"{ping:1}",
                cancellationToken: cancellationToken);
            return HealthCheckResult.Healthy("Reviews MongoDB database is reachable");
        }
        catch (MongoException ex)
        {
            return HealthCheckResult.Unhealthy("Reviews MongoDB database is not reachable", ex);
        }
    }
}
