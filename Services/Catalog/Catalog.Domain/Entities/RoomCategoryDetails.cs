namespace HotelCatalog.Domain.Entities;

public class RoomCategoryDetails
{
    public int CategoryId { get; set; }
    public int MaxOccupancy { get; set; }
    public decimal AreaSquareMeters { get; set; }
    public RoomCategory RoomCategory { get; set; } = null!;
}
