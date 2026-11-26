using Grpc.Core;
using HotelPlatform.Aggregator.DTOs;
using HotelReservation.Api.Grpc;

namespace HotelPlatform.Aggregator.Clients;

public sealed class ReservationsClient : IReservationsClient
{
    private readonly ReservationsService.ReservationsServiceClient _client;
    private readonly ILogger<ReservationsClient> _logger;

    public ReservationsClient(
        ReservationsService.ReservationsServiceClient client,
        ILogger<ReservationsClient> logger)
    {
        _client = client;
        _logger = logger;
    }

    public async Task<List<ReservationDto>> GetReservationsByRoomAsync(
        int roomId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _client.GetReservationsByRoomAsync(
                new GetReservationsByRoomRequest { RoomId = roomId, Page = 1, PageSize = 100 },
                cancellationToken: cancellationToken);

            return response.Items.Select(reservation => new ReservationDto
            {
                Id = reservation.Id,
                ClientId = reservation.ClientId,
                RoomId = reservation.RoomId,
                CheckInDate = DateTime.Parse(reservation.CheckInDate),
                CheckOutDate = DateTime.Parse(reservation.CheckOutDate),
                Status = reservation.Status,
                TotalPrice = (decimal)reservation.TotalPrice
            }).ToList();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Reservations gRPC call failed for room {RoomId}", roomId);
            return [];
        }
    }
}
