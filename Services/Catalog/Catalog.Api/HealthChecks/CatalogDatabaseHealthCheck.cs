using HotelCatalog.Dal.Data;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace HotelCatalog.Api.HealthChecks;

public sealed class CatalogDatabaseHealthCheck : IHealthCheck
{
    private readonly HotelCatalogDbContext _dbContext;

    public CatalogDatabaseHealthCheck(HotelCatalogDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var canConnect = await _dbContext.Database.CanConnectAsync(cancellationToken);
        return canConnect
            ? HealthCheckResult.Healthy("Catalog PostgreSQL database is reachable")
            : HealthCheckResult.Unhealthy("Catalog PostgreSQL database is not reachable");
    }
}
