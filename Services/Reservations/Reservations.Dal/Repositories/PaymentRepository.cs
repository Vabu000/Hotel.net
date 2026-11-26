using Dapper;
using Npgsql;
using Microsoft.Extensions.Logging;
using HotelReservation.Domain.Entities;
using HotelReservation.Dal.Interfaces;

namespace HotelReservation.Dal.Repositories
{
    public class PaymentRepository : BaseDapperRepository<Payment>, IPaymentRepository
    {
        private readonly ILogger<PaymentRepository> _logger;

        public PaymentRepository(
            NpgsqlConnection connection,
            Func<NpgsqlTransaction?> currentTransaction,
            ILogger<PaymentRepository> logger)
            : base(connection, currentTransaction)
        {
            _logger = logger;
        }

        protected override string TableName => "payments";
        protected override string IdColumn => "paymentid";
        protected override string SelectColumns => "paymentid, reservationid, amount, paymentdate, method, version AS Version";
        protected override string InsertSql => @"
            INSERT INTO payments (reservationid, amount, paymentdate, method)
            VALUES (@ReservationId, @Amount, @PaymentDate, @Method)
            RETURNING paymentid";
        protected override string UpdateSql => @"
            UPDATE payments
            SET reservationid = @ReservationId, amount = @Amount,
                paymentdate = @PaymentDate, method = @Method, updatedat = now(), version = version + 1
            WHERE paymentid = @PaymentId AND isdeleted = false AND version = @Version";

        protected override object GetInsertParameters(Payment entity) => new
        {
            entity.ReservationId,
            entity.Amount,
            entity.PaymentDate,
            entity.Method,
            entity.Version
        };

        protected override object GetUpdateParameters(Payment entity) => new
        {
            entity.PaymentId,
            entity.ReservationId,
            entity.Amount,
            entity.PaymentDate,
            entity.Method,
            entity.Version
        };

        public async Task<IEnumerable<Payment>> GetByReservationIdAsync(
            int reservationId,
            CancellationToken cancellationToken = default)
        {
            await EnsureOpenAsync(cancellationToken);
            const string sql = @"
                SELECT paymentid, reservationid, amount, paymentdate, method, version AS Version
                FROM payments
                WHERE reservationid = @ReservationId AND isdeleted = false
                ORDER BY paymentdate DESC";

            return await Connection.QueryAsync<Payment>(
                new CommandDefinition(sql, new { ReservationId = reservationId },
                    Transaction, cancellationToken: cancellationToken));
        }
    }
}
