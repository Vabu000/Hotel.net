using Dapper;
using Npgsql;

namespace HotelReservation.Dal.Repositories;

public abstract class BaseDapperRepository<T> where T : class
{
    protected readonly NpgsqlConnection Connection;
    private readonly Func<NpgsqlTransaction?> _currentTransaction;

    protected BaseDapperRepository(
        NpgsqlConnection connection,
        Func<NpgsqlTransaction?> currentTransaction)
    {
        Connection = connection;
        _currentTransaction = currentTransaction;
    }

    protected NpgsqlTransaction? Transaction => _currentTransaction();
    protected abstract string TableName { get; }
    protected abstract string IdColumn { get; }
    protected abstract string SelectColumns { get; }
    protected abstract string InsertSql { get; }
    protected abstract string UpdateSql { get; }
    protected abstract object GetInsertParameters(T entity);
    protected abstract object GetUpdateParameters(T entity);

    protected async Task EnsureOpenAsync(CancellationToken cancellationToken)
    {
        if (Connection.State != System.Data.ConnectionState.Open)
        {
            await Connection.OpenAsync(cancellationToken);
        }
    }

    public virtual async Task<T?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        await EnsureOpenAsync(cancellationToken);
        var sql = $"SELECT {SelectColumns} FROM {TableName} WHERE {IdColumn} = @Id AND isdeleted = false";
        return await Connection.QueryFirstOrDefaultAsync<T>(
            new CommandDefinition(sql, new { Id = id }, Transaction, cancellationToken: cancellationToken));
    }

    public virtual async Task<IEnumerable<T>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        await EnsureOpenAsync(cancellationToken);
        var sql = $"SELECT {SelectColumns} FROM {TableName} WHERE isdeleted = false";
        return await Connection.QueryAsync<T>(
            new CommandDefinition(sql, transaction: Transaction, cancellationToken: cancellationToken));
    }

    public virtual async Task<int> CreateAsync(T entity, CancellationToken cancellationToken = default)
    {
        await EnsureOpenAsync(cancellationToken);
        return await Connection.QuerySingleAsync<int>(
            new CommandDefinition(InsertSql, GetInsertParameters(entity), Transaction, cancellationToken: cancellationToken));
    }

    public virtual async Task<bool> UpdateAsync(T entity, CancellationToken cancellationToken = default)
    {
        await EnsureOpenAsync(cancellationToken);
        var rowsAffected = await Connection.ExecuteAsync(
            new CommandDefinition(UpdateSql, GetUpdateParameters(entity), Transaction, cancellationToken: cancellationToken));
        if (rowsAffected > 0 && entity is HotelReservation.Domain.Entities.IVersionedEntity versioned)
        {
            versioned.Version++;
        }
        return rowsAffected > 0;
    }

    public virtual async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        await EnsureOpenAsync(cancellationToken);
        var sql = $"UPDATE {TableName} SET isdeleted = true, updatedat = now(), version = version + 1 " +
                  $"WHERE {IdColumn} = @Id AND isdeleted = false";
        var rowsAffected = await Connection.ExecuteAsync(
            new CommandDefinition(sql, new { Id = id }, Transaction, cancellationToken: cancellationToken));
        return rowsAffected > 0;
    }
}
