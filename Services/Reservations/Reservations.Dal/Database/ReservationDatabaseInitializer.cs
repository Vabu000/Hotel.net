using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace HotelReservation.Dal.Database;

public sealed class ReservationDatabaseInitializer
{
    private readonly string _connectionString;
    private readonly ILogger<ReservationDatabaseInitializer> _logger;

    public ReservationDatabaseInitializer(
        IConfiguration configuration,
        ILogger<ReservationDatabaseInitializer> logger)
    {
        _connectionString = configuration.GetConnectionString("HotelReservationsDB")
            ?? throw new InvalidOperationException("Connection string 'HotelReservationsDB' is missing");
        _logger = logger;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await EnsureDatabaseExistsAsync(cancellationToken);
        await using var connection = await OpenConnectionWithRetryAsync(cancellationToken);

        var scripts = Assembly.GetExecutingAssembly()
            .GetManifestResourceNames()
            .Where(name => (name.Contains(".Database.", StringComparison.Ordinal)
                    || name.Contains(".Migrations.", StringComparison.Ordinal))
                && name.EndsWith(".sql", StringComparison.OrdinalIgnoreCase))
            .OrderBy(GetScriptOrder)
            .ThenBy(name => name, StringComparer.Ordinal);

        foreach (var scriptName in scripts)
        {
            await using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(scriptName)
                ?? throw new InvalidOperationException($"Embedded database script '{scriptName}' was not found");
            using var reader = new StreamReader(stream);
            var sql = await reader.ReadToEndAsync(cancellationToken);

            await using var command = new NpgsqlCommand(sql, connection);
            await command.ExecuteNonQueryAsync(cancellationToken);
        }
    }

    private static int GetScriptOrder(string resourceName)
    {
        var fileName = Path.GetFileNameWithoutExtension(resourceName);
        var sequence = fileName.Split('_', 2)[0];
        return int.TryParse(sequence, out var order) ? order : int.MaxValue;
    }

    private async Task EnsureDatabaseExistsAsync(CancellationToken cancellationToken)
    {
        var target = new NpgsqlConnectionStringBuilder(_connectionString);
        var databaseName = target.Database;
        if (string.IsNullOrWhiteSpace(databaseName))
        {
            throw new InvalidOperationException("Reservation database name is missing from its connection string");
        }

        target.Database = "postgres";
        await using var connection = await OpenConnectionWithRetryAsync(target.ConnectionString, cancellationToken);

        await using var checkCommand = new NpgsqlCommand(
            "SELECT EXISTS (SELECT 1 FROM pg_database WHERE datname = @databaseName)",
            connection);
        checkCommand.Parameters.AddWithValue("databaseName", databaseName);
        if ((bool)(await checkCommand.ExecuteScalarAsync(cancellationToken) ?? false))
        {
            return;
        }

        var quotedDatabaseName = new NpgsqlCommandBuilder().QuoteIdentifier(databaseName);
        var createDatabase = $"CREATE DATABASE {quotedDatabaseName}";
        try
        {
            await using var createCommand = new NpgsqlCommand(createDatabase, connection);
            await createCommand.ExecuteNonQueryAsync(cancellationToken);
        }
        catch (PostgresException ex) when (ex.SqlState == "42P04")
        {
            _logger.LogInformation("Reservation database {DatabaseName} was created by another instance", databaseName);
        }
    }

    private Task<NpgsqlConnection> OpenConnectionWithRetryAsync(CancellationToken cancellationToken) =>
        OpenConnectionWithRetryAsync(_connectionString, cancellationToken);

    private async Task<NpgsqlConnection> OpenConnectionWithRetryAsync(
        string connectionString,
        CancellationToken cancellationToken)
    {
        const int maxAttempts = 10;

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            var connection = new NpgsqlConnection(connectionString);
            try
            {
                await connection.OpenAsync(cancellationToken);
                return connection;
            }
            catch (NpgsqlException ex) when (
                attempt < maxAttempts
                && (ex.IsTransient || ex is PostgresException { SqlState: "57P03" }))
            {
                await connection.DisposeAsync();
                _logger.LogWarning(
                    ex,
                    "Reservation database is not ready; retrying connection ({Attempt}/{MaxAttempts})",
                    attempt,
                    maxAttempts);
                await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
            }
            catch
            {
                await connection.DisposeAsync();
                throw;
            }
        }

        throw new InvalidOperationException("Could not open the Reservation database connection");
    }
}
