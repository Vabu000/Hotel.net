# Resilience and performance notes

## HTTP resilience defaults

`HotelPlatform.ServiceDefaults.AddServiceDefaults` registers one standard resilience handler for `HttpClientFactory` clients. It applies a 15-second total timeout, a 5-second per-attempt timeout, up to two retries with a 200 ms base delay and jitter, and a circuit breaker with a 30-second sampling window, minimum throughput of five calls, and a 10-second break. The same defaults also configure service discovery and CorrelationId propagation.

## Aggregator gRPC read policies

Only read methods are listed in each gRPC `ServiceConfig`. Mutating operations are not retried. The gRPC retry policy handles `Unavailable`; the per-method deadline bounds the total time spent on the call.

| Aggregator client | Retryable methods | Maximum attempts | Backoff | Per-method timeout | Reason |
| --- | --- | ---: | --- | ---: | --- |
| Catalog | `GetAllServices`, `GetServiceById`, `GetRoomCategoryByRoomId`, `GetServicesByRoomCategory` | 2 | 100 to 400 ms, multiplier 2 | 3 s | Catalog enriches the response but is optional; keep its retry budget short so it cannot hold up a partial response. |
| Reservation | `GetAllReservations`, `GetReservationById`, `GetReservationsByHotel`, `GetReservationsByRoom`, `GetReservationsByClient`, `GetRoomById` | 3 | 200 ms to 1 s, multiplier 2 | 4 s | Reservation determines room and availability data, so it gets the largest bounded retry budget. |
| Reviews | `GetReviewsByRoom`, `GetReviewById` | 2 | 100 ms to 400 ms, multiplier 2 | 2 s | Reviews are optional to the aggregate response; keep their delay short so the aggregator can return partial data. |

`Platform.ServiceDefaults` registers the standard HTTP resilience handler once for `HttpClientFactory` clients. The Aggregator uses gRPC `ServiceConfig` retry rules for only idempotent read methods and a per-client deadline; it does not add a second HTTP retry policy per gRPC client. Mutating gRPC operations are not retried.

## HTTP versus gRPC latency measurement

The repository contains both the Gateway-to-Aggregator HTTP route and gRPC downstream clients, but no latency measurements are checked in. To produce a comparison, send the same room-detail request through the HTTP baseline and the gRPC aggregator after clearing or warming the same cache, collect at least 20 samples for each path, and record median and p95 latency with the cache state and sample count. Do not compare a cold HTTP result with a warm gRPC result.

| Path | Cache state | Samples | Median | p95 |
| --- | --- | ---: | ---: | ---: |
| HTTP baseline | to be measured | — | — | — |
| gRPC aggregator | to be measured | — | — | — |
