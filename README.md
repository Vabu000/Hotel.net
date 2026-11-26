# Hotel Reservation System

This repository models a hotel booking domain in three independent data contexts. Reservation owns clients, physical rooms, bookings and payments. Catalog owns room categories, services and discounts. Reviews owns guest reviews, requests and discussions.

## Database layout

Each context has its own database. IDs may be copied between contexts, but there are no cross-database foreign keys.

| Data | Owner | Replicas or snapshots | Policy |
| --- | --- | --- | --- |
| Clients, physical rooms, reservations and payments | Reservation (PostgreSQL) | A reservation keeps its own room and service identifiers and booking-time price snapshots. | Reservation is authoritative; snapshots are immutable booking history. |
| Room categories, service catalog and discounts | Catalog (PostgreSQL) | Reviews stores a category label for display; Reservation stores selected service snapshots. | Catalog is authoritative; consumers update projections from integration events. |
| Reviews, guest requests and discussions | Reviews (MongoDB) | No relational copy. | Reviews is authoritative; documents reference room or category IDs without foreign keys. |

The scripts in `db/` create the relational schemas and MongoDB collections, indexes and sample data. Apply them in this order:

1. Create the Reservation database and apply `db/p1/schema.sql`, `db/p1/procedures.sql`, then `db/p1/seed.sql`.
2. Create the Catalog database and apply `db/p2/schema.sql`, then `db/p2/seed.sql`.
3. Start MongoDB and run `db/p3/collections.js`, then `db/p3/seed.js` with `mongosh`.

Example local infrastructure:

```powershell
docker run -d --name hotel-postgres -e POSTGRES_PASSWORD=local-only -p 5432:5432 postgres:16
docker run -d --name hotel-mongodb -p 27017:27017 mongo:7
```

Use a separate database name for each relational context. Do not use these local example credentials outside a development machine.

## Run the Reservation API

Set `ConnectionStrings__HotelReservationsDB` to the Reservation PostgreSQL connection string, then run:

```powershell
dotnet run --project Services/Reservations/Reservations.Api/HotelReservation.Api.csproj
```

Or run the Aspire host, which starts PostgreSQL and MongoDB with persistent volumes and supplies connection strings to the Reservation, Catalog and Reviews APIs:

```powershell
dotnet user-secrets set "Parameters:postgres-password" "<local password>" --project Platform.AppHost/HotelPlatform.AppHost.csproj
dotnet run --project Platform.AppHost/HotelPlatform.AppHost.csproj
```

The APIs expose Swagger in Development. The `Platform.slnx` file is the solution for this repository.
