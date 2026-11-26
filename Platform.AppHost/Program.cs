var builder = DistributedApplication.CreateBuilder(args);
var postgresPassword = builder.AddParameter("postgres-password", secret: true);

var mongodb = builder.AddMongoDB("mongodb")
    .WithDataVolume("mongodb-data")
    .WithMongoExpress();

var postgres = builder.AddPostgres("postgres", password: postgresPassword)
    .WithDataVolume("postgres-data")
    .WithPgAdmin();

var redis = builder.AddRedis("redis")
    .WithDataVolume("redis-data")
    .WithRedisCommander();

var mongoDatabase = mongodb.AddDatabase("hotel-reviews-dev");
var reservationDb = postgres.AddDatabase("HotelReservationsDB");
var catalogDb = postgres.AddDatabase("HotelCatalogDB");

var reviewsService = builder.AddProject<Projects.HotelReviews_WebApi>("reviews-service")
    .WithReference(mongoDatabase)
    .WithReference(redis);

var reservationService = builder.AddProject<Projects.HotelReservation_Api>("reservation-service")
    .WithReference(reservationDb)
    .WithReference(redis);

var catalogService = builder.AddProject<Projects.HotelCatalog_Api>("catalog-service")
    .WithReference(catalogDb)
    .WithReference(redis);

var aggregator = builder.AddProject<Projects.HotelPlatform_Aggregator>("aggregator")
    .WithReference(reviewsService)
    .WithReference(reservationService)
    .WithReference(catalogService)
    .WithReference(redis);

var gateway = builder.AddProject<Projects.HotelPlatform_ApiGateway>("gateway")
    .WithExternalHttpEndpoints()
    .WithReference(reviewsService)
    .WithReference(reservationService)
    .WithReference(catalogService)
    .WithReference(aggregator);

builder.Build().Run();
