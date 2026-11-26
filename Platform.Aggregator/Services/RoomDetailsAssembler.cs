using HotelPlatform.Aggregator.DTOs;

namespace HotelPlatform.Aggregator.Services;

public sealed record AggregationDependencyResult<T>(T? Value, bool IsAvailable);

public static class RoomDetailsAssembler
{
    public static RoomDetailsDto Assemble(
        RoomDetailsDto room,
        AggregationDependencyResult<RoomCategoryInfo> category,
        AggregationDependencyResult<List<ReviewInfo>> reviews,
        AggregationDependencyResult<List<ReservationInfo>> reservations)
    {
        var reviewItems = reviews.Value ?? new List<ReviewInfo>();
        var reservationItems = reservations.Value ?? new List<ReservationInfo>();
        var unavailableSections = new List<string>();

        if (!category.IsAvailable)
        {
            unavailableSections.Add("category");
        }
        if (!reviews.IsAvailable)
        {
            unavailableSections.Add("reviews");
        }
        if (!reservations.IsAvailable)
        {
            unavailableSections.Add("reservations");
        }

        return new RoomDetailsDto
        {
            RoomId = room.RoomId,
            RoomNumber = room.RoomNumber,
            Capacity = room.Capacity,
            Price = room.Price,
            Category = category.Value,
            Reviews = reviewItems,
            AverageRating = reviewItems.Count == 0 ? 0 : Math.Round(reviewItems.Average(review => review.Rating), 2),
            TotalReviews = reviewItems.Count,
            Reservations = reservationItems,
            IsAvailable = reservations.IsAvailable && !reservationItems.Any(reservation =>
                reservation.Status is "Confirmed" or "CheckedIn"),
            AvailabilityKnown = reservations.IsAvailable,
            IsPartial = unavailableSections.Count > 0,
            UnavailableSections = unavailableSections
        };
    }
}
