using System.Data;
using Npgsql;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using HotelReservation.Domain.Entities;
using HotelReservation.Dal.Interfaces;

namespace HotelReservation.Dal.Repositories
{
    public class ClientRepository : IClientRepository
    {
        private readonly NpgsqlConnection _connection;
        private readonly Func<NpgsqlTransaction?> _currentTransaction;
        private readonly ILogger<ClientRepository> _logger;

        public ClientRepository(
            NpgsqlConnection connection,
            Func<NpgsqlTransaction?> currentTransaction,
            ILogger<ClientRepository> logger)
        {
            _connection = connection;
            _currentTransaction = currentTransaction;
            _logger = logger;
        }

        public async Task<Client?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            const string sql = @"
                SELECT clientid, fullname, passportdata, phone, email, createdat, version
                FROM clients
                WHERE clientid = @Id AND isdeleted = false";

            await EnsureOpenAsync(cancellationToken);

            await using var command = new NpgsqlCommand(sql, _connection, _currentTransaction());
            command.Parameters.AddWithValue("@Id", id);

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);

            if (await reader.ReadAsync(cancellationToken))
            {
                return MapClientFromReader(reader);
            }

            return null;
        }

        public async Task<IEnumerable<Client>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            const string sql = @"
                SELECT clientid, fullname, passportdata, phone, email, createdat, version
                FROM clients
                WHERE isdeleted = false
                ORDER BY fullname";

            var clients = new List<Client>();

            await EnsureOpenAsync(cancellationToken);

            await using var command = new NpgsqlCommand(sql, _connection, _currentTransaction());
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);

            while (await reader.ReadAsync(cancellationToken))
            {
                clients.Add(MapClientFromReader(reader));
            }

            return clients;
        }

        public async Task<int> CreateAsync(Client entity, CancellationToken cancellationToken = default)
        {
            const string sql = @"
                INSERT INTO clients (fullname, passportdata, phone, email, createdat)
                VALUES (@FullName, @PassportData, @Phone, @Email, @CreatedAt)
                RETURNING clientid";

            await EnsureOpenAsync(cancellationToken);

            await using var command = new NpgsqlCommand(sql, _connection, _currentTransaction());
            command.Parameters.AddWithValue("@FullName", entity.FullName);
            command.Parameters.AddWithValue("@PassportData", entity.PassportData);
            command.Parameters.AddWithValue("@Phone", entity.Phone);
            command.Parameters.AddWithValue("@Email", entity.Email);
            command.Parameters.AddWithValue("@CreatedAt", entity.CreatedAt);

            var result = await command.ExecuteScalarAsync(cancellationToken);
            return Convert.ToInt32(result);
        }

        public async Task<bool> UpdateAsync(Client entity, CancellationToken cancellationToken = default)
        {
            const string sql = @"
                UPDATE clients
                SET fullname = @FullName,
                    passportdata = @PassportData,
                    phone = @Phone,
                    email = @Email,
                    updatedat = now(), version = version + 1
                WHERE clientid = @ClientId AND isdeleted = false AND version = @Version";

            await EnsureOpenAsync(cancellationToken);

            await using var command = new NpgsqlCommand(sql, _connection, _currentTransaction());
            command.Parameters.AddWithValue("@ClientId", entity.ClientId);
            command.Parameters.AddWithValue("@FullName", entity.FullName);
            command.Parameters.AddWithValue("@PassportData", entity.PassportData);
            command.Parameters.AddWithValue("@Phone", entity.Phone);
            command.Parameters.AddWithValue("@Email", entity.Email);
            command.Parameters.AddWithValue("@Version", entity.Version);

            var rowsAffected = await command.ExecuteNonQueryAsync(cancellationToken);
            if (rowsAffected > 0) entity.Version++;
            return rowsAffected > 0;
        }

        public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
        {
            const string sql = "UPDATE clients SET isdeleted = true, updatedat = now(), version = version + 1 WHERE clientid = @Id AND isdeleted = false";

            await EnsureOpenAsync(cancellationToken);

            await using var command = new NpgsqlCommand(sql, _connection, _currentTransaction());
            command.Parameters.AddWithValue("@Id", id);

            var rowsAffected = await command.ExecuteNonQueryAsync(cancellationToken);
            return rowsAffected > 0;
        }

        public async Task<Client?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
        {
            const string sql = @"
                SELECT clientid, fullname, passportdata, phone, email, createdat, version
                FROM clients
                WHERE email = @Email AND isdeleted = false";

            await EnsureOpenAsync(cancellationToken);

            await using var command = new NpgsqlCommand(sql, _connection, _currentTransaction());
            command.Parameters.AddWithValue("@Email", email);

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);

            if (await reader.ReadAsync(cancellationToken))
            {
                return MapClientFromReader(reader);
            }

            return null;
        }

        public async Task<IEnumerable<Client>> SearchAsync(string searchTerm, CancellationToken cancellationToken = default)
        {
            const string sql = @"
                SELECT clientid, fullname, passportdata, phone, email, createdat, version
                FROM clients
                WHERE isdeleted = false AND (fullname ILIKE @SearchTerm OR email ILIKE @SearchTerm)
                ORDER BY fullname";

            var clients = new List<Client>();

            await EnsureOpenAsync(cancellationToken);

            await using var command = new NpgsqlCommand(sql, _connection, _currentTransaction());
            command.Parameters.AddWithValue("@SearchTerm", $"%{searchTerm}%");

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);

            while (await reader.ReadAsync(cancellationToken))
            {
                clients.Add(MapClientFromReader(reader));
            }

            return clients;
        }

        private static Client MapClientFromReader(IDataReader reader)
        {
            return new Client
            {
                ClientId = reader.GetInt32(reader.GetOrdinal("clientid")),
                FullName = reader.GetString(reader.GetOrdinal("fullname")),
                PassportData = reader.GetString(reader.GetOrdinal("passportdata")),
                Phone = reader.GetString(reader.GetOrdinal("phone")),
                Email = reader.GetString(reader.GetOrdinal("email")),
                CreatedAt = reader.GetDateTime(reader.GetOrdinal("createdat")),
                Version = reader.GetInt32(reader.GetOrdinal("version"))
            };
        }

        private async Task EnsureOpenAsync(CancellationToken cancellationToken)
        {
            if (_connection.State != ConnectionState.Open)
            {
                await _connection.OpenAsync(cancellationToken);
            }
        }
    }
}
