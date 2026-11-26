using Microsoft.Extensions.Diagnostics.HealthChecks;
using Npgsql;

namespace HotelReservation.Api.HealthChecks;

public sealed class ReservationDatabaseHealthCheck : IHealthCheck
{
    private readonly string _connectionString;

    public ReservationDatabaseHealthCheck(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("HotelReservationsDB")
            ?? throw new InvalidOperationException("Connection string 'HotelReservationsDB' is missing");
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);
            return HealthCheckResult.Healthy("Reservation PostgreSQL database is reachable");
        }
        catch (NpgsqlException ex)
        {
            return HealthCheckResult.Unhealthy("Reservation PostgreSQL database is not reachable", ex);
        }
    }
}
