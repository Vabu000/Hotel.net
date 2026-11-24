using Dapper;
using Npgsql;
using Microsoft.Extensions.Logging;
using HotelReservation.Domain.Entities;
using HotelReservation.Dal.Interfaces;

namespace HotelReservation.Dal.Repositories
{
    public class RoomRepository : BaseDapperRepository<Room>, IRoomRepository
    {
        private readonly ILogger<RoomRepository> _logger;

        public RoomRepository(
            NpgsqlConnection connection,
            Func<NpgsqlTransaction?> currentTransaction,
            ILogger<RoomRepository> logger)
            : base(connection, currentTransaction)
        {
            _logger = logger;
        }

        protected override string TableName => "rooms";
        protected override string IdColumn => "roomid";
        protected override string SelectColumns => "roomid, roomnumber, capacity, price, version AS Version";
        protected override string InsertSql => @"
            INSERT INTO rooms (roomnumber, capacity, price)
            VALUES (@RoomNumber, @Capacity, @Price)
            RETURNING roomid";
        protected override string UpdateSql => @"
            UPDATE rooms
            SET roomnumber = @RoomNumber, capacity = @Capacity, price = @Price,
                updatedat = now(), version = version + 1
            WHERE roomid = @RoomId AND isdeleted = false AND version = @Version";

        protected override object GetInsertParameters(Room entity) => new
        {
            entity.RoomNumber,
            entity.Capacity,
            entity.Price,
            entity.Version
        };

        protected override object GetUpdateParameters(Room entity) => new
        {
            entity.RoomId,
            entity.RoomNumber,
            entity.Capacity,
            entity.Price,
            entity.Version
        };

        public async Task<IEnumerable<Room>> GetAvailableRoomsAsync(
            DateTime checkIn,
            DateTime checkOut,
            CancellationToken cancellationToken = default)
        {
            await EnsureOpenAsync(cancellationToken);
            const string sql = @"
                SELECT r.roomid, r.roomnumber, r.capacity, r.price, r.version AS Version
                FROM rooms r
                WHERE r.isdeleted = false AND r.roomid NOT IN (
                    SELECT res.roomid
                    FROM reservations res
                    WHERE res.status NOT IN (3, 4) AND res.isdeleted = false
                    AND res.checkindate <= @CheckOut AND res.checkoutdate >= @CheckIn
                )
                ORDER BY r.roomnumber";

            return await Connection.QueryAsync<Room>(
                new CommandDefinition(sql, new { CheckIn = checkIn, CheckOut = checkOut },
                    Transaction, cancellationToken: cancellationToken));
        }

        public async Task<Room?> GetByRoomNumberAsync(
            string roomNumber,
            CancellationToken cancellationToken = default)
        {
            await EnsureOpenAsync(cancellationToken);
            const string sql = @"
                SELECT roomid, roomnumber, capacity, price, version AS Version
                FROM rooms
                WHERE roomnumber = @RoomNumber AND isdeleted = false";

            return await Connection.QueryFirstOrDefaultAsync<Room>(
                new CommandDefinition(sql, new { RoomNumber = roomNumber },
                    Transaction, cancellationToken: cancellationToken));
        }
    }
}
