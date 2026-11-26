using System.Data;
using Npgsql;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using HotelReservation.Dal.Interfaces;

namespace HotelReservation.Dal.Repositories
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly NpgsqlConnection _connection;
        private NpgsqlTransaction? _transaction;
        private bool _disposed;

        public UnitOfWork(
            IConfiguration configuration,
            ILoggerFactory loggerFactory)
        {
            var connectionString = configuration.GetConnectionString("HotelReservationsDB")
                ?? throw new InvalidOperationException("Connection string 'HotelReservationsDB' is missing");
            _connection = new NpgsqlConnection(connectionString);
            Clients = new ClientRepository(_connection, () => _transaction, loggerFactory.CreateLogger<ClientRepository>());
            Reservations = new ReservationRepository(_connection, () => _transaction, loggerFactory.CreateLogger<ReservationRepository>());
            Rooms = new RoomRepository(_connection, () => _transaction, loggerFactory.CreateLogger<RoomRepository>());
            Payments = new PaymentRepository(_connection, () => _transaction, loggerFactory.CreateLogger<PaymentRepository>());
        }

        public IClientRepository Clients { get; }
        public IReservationRepository Reservations { get; }
        public IRoomRepository Rooms { get; }
        public IPaymentRepository Payments { get; }

        public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
        {
            if (_transaction != null)
            {
                throw new InvalidOperationException("Transaction already started");
            }

            await EnsureOpenAsync(cancellationToken);
            _transaction = await _connection.BeginTransactionAsync(cancellationToken);
        }

        public async Task CommitAsync(CancellationToken cancellationToken = default)
        {
            if (_transaction == null)
            {
                throw new InvalidOperationException("No transaction to commit");
            }

            var transaction = _transaction;
            try
            {
                await transaction.CommitAsync(cancellationToken);
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
            finally
            {
                await transaction.DisposeAsync();
                _transaction = null;
            }
        }

        public async Task RollbackAsync(CancellationToken cancellationToken = default)
        {
            if (_transaction == null)
            {
                return;
            }

            var transaction = _transaction;
            try
            {
                await transaction.RollbackAsync(cancellationToken);
            }
            finally
            {
                await transaction.DisposeAsync();
                _transaction = null;
            }
        }

        private async Task EnsureOpenAsync(CancellationToken cancellationToken)
        {
            if (_connection.State != ConnectionState.Open)
            {
                await _connection.OpenAsync(cancellationToken);
            }
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed && disposing)
            {
                _transaction?.Dispose();
                _connection.Dispose();
                _disposed = true;
            }
        }
    }
}