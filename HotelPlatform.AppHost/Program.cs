Environment.SetEnvironmentVariable("ASPIRE_ALLOW_UNSECURED_TRANSPORT", "true");
var builder = DistributedApplication.CreateBuilder(args);

var mongodb = builder.AddMongoDB("mongo")
    .WithDataVolume("mongodb-data-v2");

var postgres = builder.AddPostgres("postgres")
    .WithDataVolume("postgres-data-v2")
    .WithPgAdmin();

var redis = builder.AddRedis("redis")
    .WithDataVolume("redis-data")
    .WithRedisCommander();

var rabbitmq = builder.AddRabbitMQ("rabbitmq")
    .WithDataVolume("rabbitmq-data")
    .WithManagementPlugin();

var reservationDb = postgres.AddDatabase("HotelReservationsDB");
var catalogDb = postgres.AddDatabase("HotelCatalogDB");
var reviewsDb = mongodb.AddDatabase("mongodb");

var reviewsService = builder.AddProject<Projects.HotelReviews_WebApi>("reviews-service")
    .WithReference(reviewsDb)
    .WithReference(redis)
    .WithReference(rabbitmq)
    .WithHttpHealthCheck("/health/ready");

var reservationService = builder.AddProject<Projects.HotelReservation_Api>("reservation-service")
    .WithReference(reservationDb)
    .WithReference(rabbitmq)
    .WithHttpHealthCheck("/health/ready");

var catalogService = builder.AddProject<Projects.HotelCatalog_Api>("catalog-service")
    .WithReference(catalogDb)
    .WithReference(redis)
    .WithReference(rabbitmq)
    .WithHttpHealthCheck("/health/ready");

var aggregator = builder.AddProject<Projects.HotelPlatform_Aggregator>("aggregator")
    .WithReference(reviewsService)
    .WithReference(reservationService)
    .WithReference(catalogService)
    .WithReference(redis)
    .WithHttpHealthCheck("/health/ready");

var gateway = builder.AddProject<Projects.HotelPlatform_ApiGateway>("gateway")
    .WithReference(reviewsService)
    .WithReference(reservationService)
    .WithReference(catalogService)
    .WithReference(aggregator)
    .WithHttpEndpoint(name: "public")
    .WithHttpHealthCheck("/health/ready");

builder.Build().Run();
