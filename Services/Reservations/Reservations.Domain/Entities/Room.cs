using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HotelReservation.Domain.Entities
{
    public class Room : IVersionedEntity
    {
        public int RoomId { get; set; }
        public string RoomNumber { get; set; } = string.Empty;
        public int Capacity { get; set; }
        public decimal Price { get; set; }
        public int Version { get; set; } = 1;

        public ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();
    }
}
