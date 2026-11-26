using Grpc.Core;
using HotelPlatform.Aggregator.DTOs;
using HotelReviews.WebApi.Services;

namespace HotelPlatform.Aggregator.Clients;

public sealed class ReviewsClient : IReviewsClient
{
    private readonly ReviewsService.ReviewsServiceClient _client;
    private readonly ILogger<ReviewsClient> _logger;

    public ReviewsClient(ReviewsService.ReviewsServiceClient client, ILogger<ReviewsClient> logger)
    {
        _client = client;
        _logger = logger;
    }

    public async Task<List<ReviewDto>> GetReviewsByRoomAsync(
        int roomId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _client.GetReviewsByRoomAsync(
                new GetReviewsByRoomRequest { RoomId = roomId, Page = 1, PageSize = 100 },
                cancellationToken: cancellationToken);

            return response.Items.Select(review => new ReviewDto
            {
                Id = review.Id,
                ClientId = review.ClientId,
                RoomId = review.RoomId,
                Rating = review.Rating,
                Comment = review.Comment,
                IsVerified = review.IsVerified,
                IsApproved = review.IsApproved,
                CreatedAt = DateTime.TryParse(review.CreatedAt, out var createdAt)
                    ? createdAt
                    : DateTime.MinValue
            }).ToList();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Reviews gRPC call failed for room {RoomId}", roomId);
            return [];
        }
    }
}
