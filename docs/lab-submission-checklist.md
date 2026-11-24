# Lab submission checklist

Use this list to check the repository against the lab requirements. The lab handouts separately list screenshots as submission files; those can be placed in the repository without an in-person demonstration. Source artifacts and the data ownership/ERD notes are in [data-model.md](data-model.md).

## Repository artifacts

- [x] Reservation SQL schema, five stored procedures, idempotent seed, room-rate snapshot and audit/soft-delete/version upgrade scripts.
- [x] Catalog EF Core model, seed data, migrations, generic asynchronous repository/Unit of Work, filtering/paging/specifications, audit/concurrency fields, and an M:N join field (`AdditionalPrice`).
- [x] Reviews Clean Architecture layers, CQRS handlers, validation behaviors, explicit BSON class maps, Mongo indexes/seeding, and three Mongo collections.
- [x] Aspire AppHost, YARP Gateway, Aggregator, OpenTelemetry, Serilog with trace fields, gRPC contracts/clients, per-client gRPC read policies, caching and health endpoints.
- [x] Reservation service unit tests, API contract tests, Catalog model tests, and Mongo document-model tests.
- [x] Run the full solution build and test commands locally; build succeeds with 0 errors and all 12 tests pass.
- [x] Apply the prior Catalog migrations and Reservation seed to fresh local databases; verified four Catalog migration IDs, Catalog tables, and one Reservation seed row. The fifth Catalog migration is included in the idempotent SQL export and will apply at service startup.
- [ ] Capture and retain the resulting schema screenshots/SQL output for submission.

## Screenshot files listed by the handouts

1. **Lab 01:** show both SQL ERDs and the Mongo `reviews`, `requests`, and `discussions` documents. Show the five Reservation procedures and the duplicate-data ownership table in `data-model.md`.
2. **Lab 02:** show `ClientRepository` using parameterized Npgsql commands (ADO.NET), at least two Dapper repositories, and a transaction spanning repositories. Call a missing reservation and a conflicting status through the API; show 404/409 ProblemDetails.
3. **Lab 03:** show Catalog migrations and seeded data, then call an endpoint using filtering, sorting and paging. Show a validation error.
4. **Lab 04:** show a Reviews command and query through MediatR, the validation behavior, Mongo document, and indexes.
5. **Lab 05:** in Aspire Dashboard capture Resources, a trace from Gateway through Aggregator to downstream gRPC services, and a structured log filtered by CorrelationId. Stop Reviews and capture a partial Aggregator response.
6. **Lab 06:** capture gRPC spans and cache hit/miss metrics. Stop Redis and demonstrate that Catalog, Reviews and Aggregator still serve requests from their source/local cache.
7. **Lab 07:** capture `/health/ready` with Redis `Degraded`; capture database failure with liveness still healthy and readiness unhealthy; generate transient failures and capture Polly retry/circuit events; create reservations and capture the counter, duration histogram and active-operation gauge.

Save screenshots under `docs/evidence/lab-01` through `docs/evidence/lab-07` and name each image for the scenario shown. Keep personal credentials and connection strings out of screenshots.

## Oral explanation to prepare

- Why each context owns its database and why cross-database foreign keys are absent.
- Which duplicated values are immutable snapshots and which are invalidated/updated.
- How the Reservation Unit of Work commits or rolls back one business operation.
- Why Mongo discussions embed bounded messages while keeping room/review references.
- How gRPC retry differs from HTTP retry and which operations are deliberately not retried.
- Why Redis reports Degraded and how readiness differs from liveness.
- What the Counter, Histogram and ObservableGauge measure in this system.
