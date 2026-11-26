var builder = DistributedApplication.CreateBuilder(args);

var postgresPassword = builder.AddParameter("postgres-password", secret: true);
var postgres = builder.AddPostgres("postgres", password: postgresPassword)
    .WithDataVolume("postgres-reservations-data");
var reservationsDb = postgres.AddDatabase("HotelReservationsDB");
var catalogDb = postgres.AddDatabase("HotelCatalogDB");
var mongodb = builder.AddMongoDB("mongo")
    .WithDataVolume("mongodb-reviews-data");
var reviewsDb = mongodb.AddDatabase("mongodb");

builder.AddProject<Projects.HotelReservation_Api>("reservations-api")
    .WithReference(reservationsDb)
    .WaitFor(reservationsDb);

builder.AddProject<Projects.HotelCatalog_Api>("catalog-api")
    .WithReference(catalogDb)
    .WaitFor(catalogDb);

builder.AddProject<Projects.HotelReviews_WebApi>("reviews-api")
    .WithReference(reviewsDb)
    .WaitFor(reviewsDb);

builder.Build().Run();
