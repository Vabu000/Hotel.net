var builder = DistributedApplication.CreateBuilder(args);

var postgresPassword = builder.AddParameter("postgres-password", secret: true);
var postgres = builder.AddPostgres("postgres", password: postgresPassword)
    .WithDataVolume("postgres-reservations-data");
var reservationsDb = postgres.AddDatabase("HotelReservationsDB");

builder.AddProject<Projects.HotelReservation_Api>("reservations-api")
    .WithReference(reservationsDb)
    .WaitFor(reservationsDb);

builder.Build().Run();
