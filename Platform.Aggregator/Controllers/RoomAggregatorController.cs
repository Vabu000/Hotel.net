using HotelPlatform.Aggregator.Clients;
using HotelPlatform.Aggregator.DTOs;
using HotelPlatform.ServiceDefaults.Metrics;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace HotelPlatform.Aggregator.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RoomAggregatorController : ControllerBase
{
    private readonly IReviewsClient _reviewsClient;
    private readonly IReservationsClient _reservationsClient;
    private readonly ICatalogClient _catalogClient;
    private readonly ILogger<RoomAggregatorController> _logger;
    private readonly IDistributedCache _cache;

    public RoomAggregatorController(
        IReviewsClient reviewsClient,
        IReservationsClient reservationsClient,
        ICatalogClient catalogClient,
        ILogger<RoomAggregatorController> logger,
        IDistributedCache cache)
    {
        _reviewsClient = reviewsClient;
        _reservationsClient = reservationsClient;
        _catalogClient = catalogClient;
        _logger = logger;
        _cache = cache;
    }
    [HttpGet("room/{roomId}")]
    public async Task<ActionResult<RoomDetailsDto>> GetRoomDetails(
        int roomId,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Aggregating data for room {RoomId}", roomId);

        var cacheKey = $"room-details:{roomId}";
        try
        {
            var cachedJson = await _cache.GetStringAsync(cacheKey, cancellationToken);
            if (!string.IsNullOrWhiteSpace(cachedJson))
            {
                CacheMetrics.RecordHit("room-aggregation", "redis");
                var cachedResult = JsonSerializer.Deserialize<RoomDetailsDto>(cachedJson);
                if (cachedResult is not null)
                {
                    return Ok(cachedResult);
                }
            }

            CacheMetrics.RecordMiss("room-aggregation", "redis");
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "Redis cache read failed for room {RoomId}; querying services", roomId);
        }

        // Паралельні запити до всіх сервісів
        var reviewsTask = _reviewsClient.GetReviewsByRoomAsync(roomId, cancellationToken);
        var reservationsTask = _reservationsClient.GetReservationsByRoomAsync(roomId, cancellationToken);
        var categoryTask = _catalogClient.GetRoomCategoryAsync(roomId, cancellationToken);
        await Task.WhenAll(reviewsTask, reservationsTask, categoryTask);

        var reviews = await reviewsTask;
        var reservations = await reservationsTask;
        var category = await categoryTask;

        // Обчислення середнього рейтингу
        var averageRating = reviews.Any()
            ? reviews.Average(r => r.Rating)
            : 0;

        var result = new RoomDetailsDto
        {
            RoomId = roomId,
            Category = category,
            Reviews = reviews,
            Reservations = reservations,
            AverageRating = Math.Round(averageRating, 2),
            TotalReviews = reviews.Count
        };

        try
        {
            await _cache.SetStringAsync(
                cacheKey,
                JsonSerializer.Serialize(result),
                new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(30)
                },
                cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "Redis cache write failed for room {RoomId}", roomId);
        }

        _logger.LogInformation(
            "Successfully aggregated data for room {RoomId}: {ReviewCount} reviews, {ReservationCount} reservations",
            roomId,
            reviews.Count,
            reservations.Count);

        return Ok(result);
    }
}
