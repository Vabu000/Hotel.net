using Grpc.Core;
using HotelCatalog.Api.Grpc;
using HotelPlatform.Aggregator.DTOs;

namespace HotelPlatform.Aggregator.Clients;

public sealed class CatalogClient : ICatalogClient
{
    private readonly CatalogService.CatalogServiceClient _client;
    private readonly ILogger<CatalogClient> _logger;

    public CatalogClient(CatalogService.CatalogServiceClient client, ILogger<CatalogClient> logger)
    {
        _client = client;
        _logger = logger;
    }

    public async Task<RoomCategoryDto?> GetRoomCategoryAsync(
        int roomId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _client.GetRoomCategoryByIdAsync(
                new GetRoomCategoryByIdRequest { Id = roomId },
                cancellationToken: cancellationToken);

            var category = response.Category;
            return new RoomCategoryDto
            {
                Id = category.CategoryId,
                Name = category.Name,
                Description = category.Description,
                BasePrice = 0,
                Capacity = 0
            };
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.NotFound)
        {
            _logger.LogInformation("No catalog category found for room/category {RoomId}", roomId);
            return null;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Catalog gRPC call failed for room/category {RoomId}", roomId);
            return null;
        }
    }
}
