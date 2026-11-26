# Hotel Reservation System

A .NET 8 microservices application for reservations, catalog data, guest reviews and requests. PostgreSQL is used by Reservation and Catalog, MongoDB by Reviews, and Redis and RabbitMQ support caching and messaging.

Guests reserve hotel rooms and optional services. The Reservation context owns clients, rooms, bookings and payments; Catalog owns room categories and services; Reviews owns reviews, guest requests and discussions. Each context has its own database, and cross-context links are identifiers or explicitly documented booking snapshots.

## Run locally

The recommended entry point is the Aspire AppHost. Use .NET SDK 9.0.200 or newer and make Docker available:

```powershell
dotnet workload install aspire
dotnet user-secrets set "Parameters:postgres-password" "<local secret>" --project Platform.AppHost/HotelPlatform.AppHost.csproj
dotnet user-secrets set "Parameters:sqlserver-password" "<SQL Server SA password>" --project Platform.AppHost/HotelPlatform.AppHost.csproj
dotnet user-secrets set "Parameters:jwt-signing-key" "<at least 32 characters>" --project Platform.AppHost/HotelPlatform.AppHost.csproj
dotnet user-secrets set "Parameters:keycloak-admin-password" "<local secret>" --project Platform.AppHost/HotelPlatform.AppHost.csproj
dotnet user-secrets set "Parameters:identityserver-aggregator-secret" "<local secret>" --project Platform.AppHost/HotelPlatform.AppHost.csproj
dotnet run --project Platform.AppHost/HotelPlatform.AppHost.csproj --launch-profile http
```

The AppHost starts PostgreSQL, SQL Server, MongoDB, Redis, RabbitMQ, Keycloak, and the services with persistent data volumes. Reservation, Catalog, Auth, and IdentityServer currently use PostgreSQL; SQL Server is provisioned for the lab infrastructure but is not yet connected to an application data context. Reservation applies its SQL scripts, Catalog applies EF Core migrations, and Reviews creates MongoDB indexes and idempotent seed documents at startup. Local credentials are read from user secrets and are not stored in the repository.

If running a service outside Aspire, configure its connection strings in user secrets or environment variables. Do not commit machine-specific credentials.

## Database scripts

The service ownership, duplication policy, relational ERDs, MongoDB document examples, and database recreation order are documented in [docs/data-model.md](./docs/data-model.md). Kubernetes instructions are in [deploy/k8s/README.md](./deploy/k8s/README.md).

Reservation schema, stored procedures, and idempotent sample data are in [db/p1](./db/p1). The API applies its embedded scripts in filename order at startup. They can also be run manually with `psql` against the `HotelReservationsDB` database.

Catalog schema changes are managed by EF Core migrations in [Services/Catalog/Catalog.Dal/Migrations](./Services/Catalog/Catalog.Dal/Migrations). To produce a SQL script for review or deployment, install the `dotnet-ef` tool matching the project EF Core major version, then run:

```powershell
dotnet ef migrations script --idempotent --project Services/Catalog/Catalog.Dal/HotelCatalog.Dal.csproj --startup-project Services/Catalog/Catalog.Api/HotelCatalog.Api.csproj --output Services/Catalog/Catalog.Dal/Migrations/catalog-idempotent.sql
```

The checked-in [Catalog SQL export](./Services/Catalog/Catalog.Dal/Migrations/catalog-idempotent.sql) is generated from those migrations. Use the script **or** the API's migration startup, not both against the same fresh database.

`RoomCategory` has an optional one-to-one `RoomCategoryDetails` dependent record. Its primary key is also its foreign key to `roomcategories`, so a category can have at most one details record. The migration seeds example occupancy and area values.

`RoomService` is the Catalog M:N join entity and stores a category-specific `AdditionalPrice` in addition to its two foreign keys. The migration and seed data include this relationship field.

The Reviews API seeds its configured Mongo database at startup. An optional idempotent `mongosh` example is in [Services/Reviews/Reviews.Infrastructure/Seeding/seed.example.js](./Services/Reviews/Reviews.Infrastructure/Seeding/seed.example.js).

## Data ownership and duplication policy

| Context/database | Data owned by the context | Cross-context fields and policy |
| --- | --- | --- |
| Reservation / PostgreSQL | Clients, rooms, reservations, reservation details, payments, status history, reservation service snapshots | `catalog_service_id` is a logical reference to Catalog (no cross-database FK). Service name and rate are captured in `catalog_service_snapshots`; the booking-time room rate is stored as `room_rate_snapshot`. These are immutable booking snapshots. |
| Catalog / PostgreSQL | Room categories, category details, services, room-service links, discount categories, client discounts | Catalog owns names, descriptions, service prices, and category definitions. Other contexts refer to its IDs and must not edit Catalog-owned fields. |
| Reviews / MongoDB | Reviews, guest requests, discussions | `clientId`, `roomId`, and `reviewId` are logical references. Discussion messages are embedded in a bounded aggregate; discussion metadata may evolve without a relational migration. |

The principal SQL relationships are: Reservation `clients` 1:N `reservations`, `reservations` 1:1 `reservation_details`, and `reservations` M:N Catalog service snapshots through `reservation_services`; Catalog `roomcategories` 1:1 `roomcategorydetails`, `roomcategories` 1:N `roomservices`, and `roomcategories` M:N `services` through `roomservices`. Every FK remains inside its own database.

MongoDB stores `reviews`, `requests`, and `discussions`, plus a Catalog category projection maintained by RabbitMQ events. Reviews and requests are independent aggregates; discussions embed a bounded message list and reference room/review IDs. `Discussion.AdditionalData` preserves evolving optional fields. The seeder creates representative records and the example script includes indexes and an optional JSON Schema validator.

Reservation schema scripts are applied in numeric order. `004_room_rate_snapshot.sql` is an idempotent upgrade for existing databases; old bookings keep a null snapshot because their historical rate cannot be reconstructed reliably. New bookings capture the selected room rate before commit.

## API behavior and diagnostics

API error responses use `application/problem+json` with a trace identifier. Aggregator responses include `isPartial` and `unavailableSections` when optional Catalog, Reviews, or Reservation lookups fail. If Reservation data is unavailable, `availabilityKnown` is `false`; the response does not imply the room is available.

Useful API routes (open each service's `/swagger` for schemas and bodies):

| Request | Purpose |
| --- | --- |
| `GET /api/RoomCategory/paged?page=2&pageSize=5&searchTerm=deluxe&sortBy=name&sortOrder=asc` | Catalog filtering, sorting and paging. |
| `POST /api/RoomCategory/assign-service` | Assign a service and set its `additionalPrice` on the M:N relationship. |
| `GET /api/RoomAggregator/room/{roomId}` | Aggregated room details from downstream services. |
| `GET /health/ready` | Readiness details, including optional Redis status. |

`/health` reports all checks, `/health/live` and `/alive` are liveness endpoints, and `/health/ready` reports checks tagged `ready`. Redis is an optional cache and reports `Degraded` when unavailable; it does not prevent a service from remaining live. Aspire resource health checks use `/health/ready`.

Shared HTTP clients use bounded timeouts, retries, and a circuit breaker. OpenTelemetry traces ASP.NET Core, HTTP/gRPC clients, PostgreSQL (`Npgsql`), and MongoDB driver commands; it exports the Catalog cache, Aggregator, Reservation, ASP.NET Core, HTTP-client, and runtime metrics when `OTEL_EXPORTER_OTLP_ENDPOINT` is configured. Serilog events carry `ServiceName`, `CorrelationId` (on requests), `TraceId`, `SpanId`, `Environment`, and `MachineName` where available.

See [docs/resilience-policies.md](./docs/resilience-policies.md) for the client policies and the HTTP/gRPC comparison procedure.

## Build and tests

```powershell
dotnet build Platform.slnx
dotnet test Services/Reservations/Reservations.Tests/HotelReservation.Tests.csproj
```

The complete solution also includes the Aspire AppHost, which requires the Aspire workload for the installed .NET SDK.
