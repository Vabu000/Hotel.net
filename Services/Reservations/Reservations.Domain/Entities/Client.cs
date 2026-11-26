using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HotelReservation.Domain.Entities
{
    public class Client : IVersionedEntity
    {
        public int ClientId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string PassportData { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public int Version { get; set; } = 1;

        public ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();
    }
}
