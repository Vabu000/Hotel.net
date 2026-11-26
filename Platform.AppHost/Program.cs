var builder = DistributedApplication.CreateBuilder(args);

var postgresPassword = builder.AddParameter("postgres-password", secret: true);
var postgres = builder.AddPostgres("postgres", password: postgresPassword)
    .WithDataVolume("postgres-data");
var reservationsDb = postgres.AddDatabase("HotelReservationDb");
var catalogDb = postgres.AddDatabase("HotelCatalogDb");

var mongodb = builder.AddMongoDB("mongodb")
    .WithDataVolume("mongodb-data");
var reviewsDb = mongodb.AddDatabase("hotel-reviews-dev");

var reservationService = builder.AddProject<Projects.HotelReservation_Api>("reservation-service")
    .WithReference(reservationsDb)
    .WaitFor(reservationsDb);
var catalogService = builder.AddProject<Projects.HotelCatalog_Api>("catalog-service")
    .WithReference(catalogDb)
    .WaitFor(catalogDb);
var reviewsService = builder.AddProject<Projects.HotelReviews_WebApi>("reviews-service")
    .WithReference(reviewsDb)
    .WaitFor(reviewsDb);

var aggregator = builder.AddProject<Projects.HotelPlatform_Aggregator>("aggregator")
    .WithReference(reservationService)
    .WithReference(catalogService)
    .WithReference(reviewsService)
    .WaitFor(reservationService)
    .WaitFor(catalogService)
    .WaitFor(reviewsService);

builder.AddProject<Projects.HotelPlatform_ApiGateway>("gateway")
    .WithExternalHttpEndpoints()
    .WithReference(reservationService)
    .WithReference(catalogService)
    .WithReference(reviewsService)
    .WithReference(aggregator)
    .WaitFor(reservationService)
    .WaitFor(catalogService)
    .WaitFor(reviewsService)
    .WaitFor(aggregator);

builder.Build().Run();