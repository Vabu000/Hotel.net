using System.Net;
using System.Security.Claims;
using System.Text.Encodings.Web;
using HotelReservation.Bll.DTOs;
using HotelReservation.Bll.Services;
using HotelReservation.Domain.Exceptions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HotelReservation.Tests;

public sealed class ReservationApiTests
{
    [Fact]
    public async Task MissingReservationReturnsProblemDetailsNotServerError()
    {
        using var factory = new ReservationApiFactory();
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/api/Reservation/404");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.True(response.Content.Headers.ContentType?.MediaType == "application/problem+json", body);
        Assert.Contains("traceId", body);
    }

    [Fact]
    public async Task BusinessConflictReturnsProblemDetails()
    {
        using var factory = new ReservationApiFactory();
        using var client = factory.CreateClient();

        using var response = await client.PostAsync("/api/Reservation/7/cancel", content: null);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.True(response.Content.Headers.ContentType?.MediaType == "application/problem+json", body);
    }

    private sealed class ReservationApiFactory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IReservationService>();
                services.AddSingleton<IReservationService, TestReservationService>();
                services.AddAuthentication("Test")
                    .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("Test", _ => { });
                services.AddSingleton<IAuthorizationHandler, AllowAllAuthHandler>();
            });
        }
    }

    private sealed class TestAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
    {
        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var identity = new ClaimsIdentity([new Claim(ClaimTypes.Name, "test-user")], "Test");
            var ticket = new AuthenticationTicket(new ClaimsPrincipal(identity), "Test");
            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
    }

    private sealed class AllowAllAuthHandler : IAuthorizationHandler
    {
        public Task HandleAsync(AuthorizationHandlerContext context)
        {
            foreach (var req in context.PendingRequirements.ToList())
                context.Succeed(req);
            return Task.CompletedTask;
        }
    }

    private sealed class TestReservationService : IReservationService
    {
        public Task<ReservationDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default) => Task.FromResult<ReservationDto?>(null);
        public Task<IEnumerable<ReservationDto>> GetAllAsync(CancellationToken cancellationToken = default) => Task.FromResult<IEnumerable<ReservationDto>>([]);
        public Task<ReservationDto> CreateAsync(CreateReservationDto dto, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<ReservationDto> UpdateStatusAsync(UpdateReservationStatusDto dto, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<IEnumerable<ReservationDto>> GetByClientIdAsync(int clientId, CancellationToken cancellationToken = default) => Task.FromResult<IEnumerable<ReservationDto>>([]);
        public Task<ReservationDto?> GetWithDetailsAsync(int id, CancellationToken cancellationToken = default) => Task.FromResult<ReservationDto?>(null);
        public Task<IEnumerable<ReservationDto>> GetByDateRangeAsync(DateTime from, DateTime to, CancellationToken cancellationToken = default) => Task.FromResult<IEnumerable<ReservationDto>>([]);
        public Task<ReservationDto> CheckInAsync(int reservationId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<ReservationDto> CheckOutAsync(int reservationId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<ReservationDto> CancelAsync(int reservationId, CancellationToken cancellationToken = default) =>
            Task.FromException<ReservationDto>(new BusinessConflictException("Reservation cannot be cancelled in its current state"));
    }
}
