# Kubernetes resources

These manifests are kept under the output path expected for the AppHost deployment lab. They define the platform services, backing containers, persistent claims, readiness/liveness probes, and ClusterIP service discovery. The Aspirate CLI is not installed in the current environment, so regenerate this directory with `aspirate generate` after installing it and review the result before applying.

Create `platform-secrets` out-of-band before applying the resources. Required keys include `postgres-user`, `postgres-password`, `sqlserver-password`, `rabbitmq-user`, `rabbitmq-password`, `KC_BOOTSTRAP_ADMIN_PASSWORD`, `Jwt__SecretKey`, `ConnectionStrings__HotelReservationsDB`, `ConnectionStrings__HotelCatalogDB`, `ConnectionStrings__IdentityDb`, `ConnectionStrings__IdentityServerDb`, `ConnectionStrings__IdentityServerConfigDb`, `ConnectionStrings__IdentityServerGrantsDb`, `ConnectionStrings__rabbitmq`, `MongoDbSettings__ConnectionString`, and `IdentityServer__Clients__AggregatorSecret`.

```bash
kubectl create secret generic platform-secrets -n hotel-platform \
  --from-literal=postgres-user="$POSTGRES_USER" \
  --from-literal=postgres-password="$POSTGRES_PASSWORD" \
  --from-literal=sqlserver-password="$SQLSERVER_PASSWORD" \
  --from-literal=rabbitmq-user="$RABBITMQ_USER" \
  --from-literal=rabbitmq-password="$RABBITMQ_PASSWORD" \
  --from-literal=KC_BOOTSTRAP_ADMIN_PASSWORD="$KEYCLOAK_ADMIN_PASSWORD" \
  --from-literal=Jwt__SecretKey="$JWT_SECRET" \
  --from-literal=ConnectionStrings__HotelReservationsDB="$RESERVATIONS_DB" \
  --from-literal=ConnectionStrings__HotelCatalogDB="$CATALOG_DB" \
  --from-literal=ConnectionStrings__IdentityDb="$IDENTITY_DB" \
  --from-literal=ConnectionStrings__IdentityServerDb="$IDENTITY_SERVER_DB" \
  --from-literal=ConnectionStrings__IdentityServerConfigDb="$IDENTITY_SERVER_CONFIG_DB" \
  --from-literal=ConnectionStrings__IdentityServerGrantsDb="$IDENTITY_SERVER_GRANTS_DB" \
  --from-literal=ConnectionStrings__rabbitmq="$RABBITMQ_CONNECTION" \
  --from-literal=MongoDbSettings__ConnectionString="$MONGODB_CONNECTION" \
  --from-literal=IdentityServer__Clients__AggregatorSecret="$AGGREGATOR_CLIENT_SECRET"
kubectl apply -k .
```
