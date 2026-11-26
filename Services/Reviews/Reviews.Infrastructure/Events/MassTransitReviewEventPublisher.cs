using HotelReviews.Application.Common.Interfaces;
using HotelReviews.Application.Events;
using MassTransit;

namespace HotelReviews.Infrastructure.Events;

public sealed class MassTransitReviewEventPublisher : IReviewEventPublisher
{
    private readonly IPublishEndpoint _publishEndpoint;

    public MassTransitReviewEventPublisher(IPublishEndpoint publishEndpoint)
    {
        _publishEndpoint = publishEndpoint;
    }

    public Task PublishAsync(
        ReviewAddedEvent integrationEvent,
        CancellationToken cancellationToken = default) =>
        _publishEndpoint.Publish(integrationEvent, cancellationToken);
}
