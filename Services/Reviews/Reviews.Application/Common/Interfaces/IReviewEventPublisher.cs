using HotelReviews.Application.Events;

namespace HotelReviews.Application.Common.Interfaces;

public interface IReviewEventPublisher
{
    Task PublishAsync(ReviewAddedEvent integrationEvent, CancellationToken cancellationToken = default);
}
