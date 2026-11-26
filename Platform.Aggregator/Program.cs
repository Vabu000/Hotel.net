using HotelPlatform.Aggregator.Clients;
using HotelCatalog.Api.Grpc;
using HotelPlatform.ServiceDefaults.Caching;
using HotelReservation.Api.Grpc;
using HotelReviews.WebApi.Services;
using Microsoft.Extensions.Caching.Distributed;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddRedisClient("redis");
builder.Services.AddSingleton<IDistributedCache, RedisDistributedCache>();
builder.Services.AddControllers();

builder.Services.AddGrpcClient<ReviewsService.ReviewsServiceClient>(client =>
{
    client.Address = new Uri("http://reviews-service");
});

builder.Services.AddGrpcClient<ReservationsService.ReservationsServiceClient>(client =>
{
    client.Address = new Uri("http://reservation-service");
});

builder.Services.AddGrpcClient<CatalogService.CatalogServiceClient>(client =>
{
    client.Address = new Uri("http://catalog-service");
});

builder.Services.AddTransient<IReviewsClient, ReviewsClient>();
builder.Services.AddTransient<IReservationsClient, ReservationsClient>();
builder.Services.AddTransient<ICatalogClient, CatalogClient>();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Hotel Aggregator API",
        Version = "v1",
        Description = "Aggregates data from multiple hotel microservices"
    });
});
var app = builder.Build();

app.UseServiceDefaults();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();
app.Run();
