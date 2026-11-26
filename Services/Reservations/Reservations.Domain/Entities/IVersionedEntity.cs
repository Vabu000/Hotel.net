namespace HotelReservation.Domain.Entities;

public interface IVersionedEntity
{
    int Version { get; set; }
}
