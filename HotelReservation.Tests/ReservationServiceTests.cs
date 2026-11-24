using AutoMapper;
using HotelReservation.Bll.DTOs;
using HotelReservation.Bll.Profiles;
using HotelReservation.Bll.Services;
using HotelReservation.Dal.Database;
using HotelReservation.Dal.Interfaces;
using HotelReservation.Domain.Entities;
using HotelReservation.Domain.Exceptions;
using System.Diagnostics.Metrics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace HotelReservation.Tests;

public class ReservationServiceTests
{
    [Fact]
    public async Task UpdateStatusAsync_ConfirmsPendingReservation()
    {
        var reservation = new Reservation
        {
            ReservationId = 12,
            Status = ReservationStatus.Pending
        };
        var repository = new FakeReservationRepository(reservation);
        var service = CreateService(repository);

        var result = await service.UpdateStatusAsync(new UpdateReservationStatusDto
        {
            ReservationId = reservation.ReservationId,
            Status = ReservationStatus.Confirmed
        });

        Assert.Equal(ReservationStatus.Confirmed, result.Status);
        Assert.Equal(ReservationStatus.Confirmed, repository.UpdatedStatus);
    }

    [Fact]
    public async Task UpdateStatusAsync_RejectsInvalidTransitionBeforeUpdating()
    {
        var reservation = new Reservation
        {
            ReservationId = 12,
            Status = ReservationStatus.Pending
        };
        var repository = new FakeReservationRepository(reservation);
        var service = CreateService(repository);

        await Assert.ThrowsAsync<BusinessConflictException>(() =>
            service.UpdateStatusAsync(new UpdateReservationStatusDto
            {
                ReservationId = reservation.ReservationId,
                Status = ReservationStatus.CheckedOut
            }));

        Assert.Null(repository.UpdatedStatus);
    }

    [Fact]
    public void DatabaseInitializer_EmbedsAllOrderedScripts()
    {
        var scripts = typeof(ReservationDatabaseInitializer).Assembly
            .GetManifestResourceNames()
            .Where(name => name.Contains(".Database.", StringComparison.Ordinal)
                && name.EndsWith(".sql", StringComparison.OrdinalIgnoreCase))
            .OrderBy(name => name, StringComparer.Ordinal)
            .Select(name => name[..^4].Split('.').Last())
            .ToArray();

        Assert.Equal(
            new[] { "001_schema", "002_procedures", "003_seed", "004_room_rate_snapshot", "005_audit_and_concurrency" },
            scripts);
    }

    private static ReservationService CreateService(FakeReservationRepository repository)
    {
        var unitOfWork = new FakeUnitOfWork(repository);
        var mapper = new MapperConfiguration(configuration =>
            configuration.AddProfile<MappingProfile>()).CreateMapper();
        using var services = new ServiceCollection()
            .AddMetrics()
            .BuildServiceProvider();
        var metrics = new ReservationMetrics(services.GetRequiredService<IMeterFactory>());

        return new ReservationService(
            unitOfWork,
            mapper,
            NullLogger<ReservationService>.Instance,
            null!,
            metrics);
    }

    private sealed class FakeUnitOfWork(IReservationRepository reservations) : IUnitOfWork
    {
        public IClientRepository Clients => null!;
        public IReservationRepository Reservations => reservations;
        public IRoomRepository Rooms => null!;
        public IPaymentRepository Payments => null!;
        public Task BeginTransactionAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task CommitAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task RollbackAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public void Dispose() { }
    }

    private sealed class FakeReservationRepository(Reservation reservation) : IReservationRepository
    {
        public ReservationStatus? UpdatedStatus { get; private set; }

        public Task<Reservation?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
            Task.FromResult<Reservation?>(id == reservation.ReservationId ? reservation : null);

        public Task<bool> UpdateStatusAsync(
            int reservationId,
            ReservationStatus status,
            CancellationToken cancellationToken = default)
        {
            UpdatedStatus = status;
            return Task.FromResult(true);
        }

        public Task<IEnumerable<Reservation>> GetByClientIdAsync(int clientId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IEnumerable<Reservation>>(Array.Empty<Reservation>());

        public Task<Reservation?> GetWithDetailsAsync(int reservationId, CancellationToken cancellationToken = default) =>
            Task.FromResult<Reservation?>(null);

        public Task<IEnumerable<Reservation>> GetByDateRangeAsync(
            DateTime from,
            DateTime to,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IEnumerable<Reservation>>(Array.Empty<Reservation>());

        public Task<IEnumerable<Reservation>> GetAllAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IEnumerable<Reservation>>(Array.Empty<Reservation>());

        public Task<int> CreateAsync(Reservation entity, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<bool> UpdateAsync(Reservation entity, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();
    }
}
