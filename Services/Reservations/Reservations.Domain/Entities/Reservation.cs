using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HotelReservation.Domain.Entities
{
    public class Reservation : IVersionedEntity
    {
        public int ReservationId { get; set; }
        public int ClientId { get; set; }
        public int RoomId { get; set; }
        /// <summary>Nightly price captured when the reservation was created; null for historical rows without a known rate.</summary>
        public decimal? RoomRateSnapshot { get; set; }
        public DateTime CheckInDate { get; set; }
        public DateTime CheckOutDate { get; set; }
        public ReservationStatus Status { get; set; }
        public int Version { get; set; } = 1;

        public Client Client { get; set; } = null!;
        public Room Room { get; set; } = null!;
        public ICollection<Payment> Payments { get; set; } = new List<Payment>();
    }

    public enum ReservationStatus
    {
        Pending = 0,
        Confirmed = 1,
        CheckedIn = 2,
        CheckedOut = 3,
        Cancelled = 4
    }
}
