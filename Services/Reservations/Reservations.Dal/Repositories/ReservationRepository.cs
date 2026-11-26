using Dapper;
using Npgsql;
using Microsoft.Extensions.Logging;
using HotelReservation.Domain.Entities;
using HotelReservation.Dal.Interfaces;
using HotelReservation.Domain.Exceptions;

namespace HotelReservation.Dal.Repositories
{
    public class ReservationRepository : BaseDapperRepository<Reservation>, IReservationRepository
    {
        private readonly ILogger<ReservationRepository> _logger;

        public ReservationRepository(
            NpgsqlConnection connection,
            Func<NpgsqlTransaction?> currentTransaction,
            ILogger<ReservationRepository> logger)
            : base(connection, currentTransaction)
        {
            _logger = logger;
        }

        protected override string TableName => "reservations";
        protected override string IdColumn => "reservationid";
        protected override string SelectColumns =>
            "reservationid, clientid, roomid, room_rate_snapshot AS RoomRateSnapshot, checkindate, checkoutdate, status, version AS Version";
        protected override string InsertSql => @"
            INSERT INTO reservations (clientid, roomid, room_rate_snapshot, checkindate, checkoutdate, status)
            VALUES (@ClientId, @RoomId, @RoomRateSnapshot, @CheckInDate, @CheckOutDate, @Status)
            RETURNING reservationid";
        protected override string UpdateSql => @"
            UPDATE reservations
            SET clientid = @ClientId, roomid = @RoomId,
                checkindate = @CheckInDate, checkoutdate = @CheckOutDate, status = @Status,
                updatedat = now(), version = version + 1
            WHERE reservationid = @ReservationId AND isdeleted = false AND version = @Version";

        protected override object GetInsertParameters(Reservation entity) => new
        {
            entity.ClientId,
            entity.RoomId,
            entity.RoomRateSnapshot,
            entity.CheckInDate,
            entity.CheckOutDate,
            Status = (int)entity.Status,
            entity.Version
        };

        protected override object GetUpdateParameters(Reservation entity) => new
        {
            entity.ReservationId,
            entity.ClientId,
            entity.RoomId,
            entity.CheckInDate,
            entity.CheckOutDate,
            Status = (int)entity.Status,
            entity.Version
        };

        public async Task<IEnumerable<Reservation>> GetByClientIdAsync(
            int clientId,
            CancellationToken cancellationToken = default)
        {
            await EnsureOpenAsync(cancellationToken);
            const string sql = @"
                SELECT reservationid, clientid, roomid, room_rate_snapshot AS RoomRateSnapshot,
                       checkindate, checkoutdate, status, version AS Version
                FROM reservations
                WHERE clientid = @ClientId AND isdeleted = false
                ORDER BY checkindate DESC";

            return await Connection.QueryAsync<Reservation>(
                new CommandDefinition(sql, new { ClientId = clientId }, Transaction, cancellationToken: cancellationToken));
        }

        public async Task<Reservation?> GetWithDetailsAsync(
            int reservationId,
            CancellationToken cancellationToken = default)
        {
            await EnsureOpenAsync(cancellationToken);
            const string sql = @"
                SELECT
                    r.reservationid, r.clientid, r.roomid, r.room_rate_snapshot AS RoomRateSnapshot,
                    r.checkindate, r.checkoutdate, r.status, r.version AS Version,
                    c.clientid, c.fullname, c.passportdata, c.phone, c.email, c.createdat,
                    rm.roomid, rm.roomnumber, rm.capacity, rm.price
                FROM reservations r
                INNER JOIN clients c ON r.clientid = c.clientid
                INNER JOIN rooms rm ON r.roomid = rm.roomid
                WHERE r.reservationid = @ReservationId
                  AND r.isdeleted = false AND c.isdeleted = false AND rm.isdeleted = false";

            var reservationDictionary = new Dictionary<int, Reservation>();
            var result = await Connection.QueryAsync<Reservation, Client, Room, Reservation>(
                new CommandDefinition(sql, new { ReservationId = reservationId }, Transaction, cancellationToken: cancellationToken),
                (reservation, client, room) =>
                {
                    if (!reservationDictionary.TryGetValue(reservation.ReservationId, out var reservationEntry))
                    {
                        reservationEntry = reservation;
                        reservationEntry.Client = client;
                        reservationEntry.Room = room;
                        reservationDictionary.Add(reservationEntry.ReservationId, reservationEntry);
                    }

                    return reservationEntry;
                },
                splitOn: "clientid,roomid");

            return result.FirstOrDefault();
        }

        public async Task<bool> UpdateStatusAsync(
            int reservationId,
            ReservationStatus status,
            CancellationToken cancellationToken = default)
        {
            await EnsureOpenAsync(cancellationToken);

            if (status is ReservationStatus.Confirmed or ReservationStatus.Cancelled)
            {
                var procedure = status == ReservationStatus.Confirmed
                    ? "usp_confirm_reservation"
                    : "usp_cancel_reservation";

                try
                {
                    await Connection.ExecuteAsync(
                        new CommandDefinition(
                            $"CALL {procedure}(@ReservationId)",
                            new { ReservationId = reservationId },
                            Transaction,
                            cancellationToken: cancellationToken));
                    return true;
                }
                catch (PostgresException ex) when (ex.SqlState == "P0002")
                {
                    throw new NotFoundException($"Reservation with ID {reservationId} not found");
                }
                catch (PostgresException ex) when (ex.SqlState == "P0001")
                {
                    throw new BusinessConflictException(
                        $"Cannot change reservation {reservationId} to status {status}");
                }
            }

            const string sql = @"
                WITH current_reservation AS (
                    SELECT reservationid, status AS oldstatus
                    FROM reservations
                    WHERE reservationid = @ReservationId AND isdeleted = false
                    FOR UPDATE
                ),
                changed_reservation AS (
                    UPDATE reservations AS r
                    SET status = @Status, updatedat = now(), version = version + 1
                    FROM current_reservation
                    WHERE r.reservationid = current_reservation.reservationid
                      AND ((@Status = 2 AND current_reservation.oldstatus = 1)
                        OR (@Status = 3 AND current_reservation.oldstatus = 2))
                    RETURNING r.reservationid, r.status AS newstatus
                )
                INSERT INTO reservation_status_history (reservationid, oldstatus, newstatus, changedby)
                SELECT changed.reservationid, current_reservation.oldstatus, changed.newstatus, current_user
                FROM changed_reservation AS changed
                INNER JOIN current_reservation USING (reservationid)";

            var rowsAffected = await Connection.ExecuteAsync(
                new CommandDefinition(sql, new { ReservationId = reservationId, Status = (int)status },
                    Transaction, cancellationToken: cancellationToken));
            return rowsAffected > 0;
        }

        public async Task<IEnumerable<Reservation>> GetByDateRangeAsync(
            DateTime from,
            DateTime to,
            CancellationToken cancellationToken = default)
        {
            await EnsureOpenAsync(cancellationToken);
            const string sql = @"
                SELECT reservationid, clientid, roomid, room_rate_snapshot AS RoomRateSnapshot,
                       checkindate, checkoutdate, status, version AS Version
                FROM reservations
                WHERE checkindate >= @From AND checkoutdate <= @To AND isdeleted = false
                ORDER BY checkindate";

            return await Connection.QueryAsync<Reservation>(
                new CommandDefinition(sql, new { From = from, To = to }, Transaction, cancellationToken: cancellationToken));
        }
    }
}
