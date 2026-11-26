# Kubernetes Deployment (Lab 10)

## 1. Automated Deployment via Aspirate
```bash
cd Platform.AppHost
aspirate init
aspirate generate
kubectl apply -k ./aspirate-output
```

## 2. Manual Deployment (`deploy/k8s/`)

Build the local service images from the repository root:
```bash
docker build -t catalog-api:latest -f Services/Catalog/Catalog.Api/Dockerfile .
docker build -t reservation-api:latest -f Services/Reservations/Reservations.Api/Dockerfile .
docker build -t reviews-api:latest -f Services/Reviews/Reviews.WebApi/Dockerfile .
docker build -t auth-api:latest -f Services/Auth/Auth.Api/Dockerfile .
docker build -t payments-api:latest -f Services/Payments/Payments.Api/Dockerfile .
docker build -t identityserver:latest -f Platform.IdentityServer/Dockerfile .
docker build -t aggregator:latest -f Platform.Aggregator/Dockerfile .
docker build -t gateway:latest -f Platform.ApiGateway/Dockerfile .
```

Create namespace and ConfigMap:
```bash
kubectl apply -f deploy/k8s/namespace.yaml
kubectl apply -f deploy/k8s/configmap.yaml
```

The manifests expect PostgreSQL, MongoDB, Redis, RabbitMQ and Keycloak to be reachable in the cluster. Supply database URLs and credentials out-of-band; never commit real values to git. The Secret must include:
```bash
kubectl create secret generic platform-secrets -n hotel-platform --dry-run=client -o yaml \
  --from-literal=ConnectionStrings__HotelCatalogDB="$CATALOG_DB" \
  --from-literal=ConnectionStrings__HotelReservationsDB="$RESERVATIONS_DB" \
  --from-literal=ConnectionStrings__IdentityDb="$IDENTITY_DB" \
  --from-literal=ConnectionStrings__IdentityServerDb="$IDENTITY_SERVER_DB" \
  --from-literal=ConnectionStrings__IdentityServerConfigDb="$IDENTITY_SERVER_CONFIG_DB" \
  --from-literal=ConnectionStrings__IdentityServerGrantsDb="$IDENTITY_SERVER_GRANTS_DB" \
  --from-literal=ConnectionStrings__rabbitmq="$RABBITMQ_CONNECTION" \
  --from-literal=MongoDbSettings__ConnectionString="$MONGODB_CONNECTION" \
  --from-literal=Jwt__SecretKey="$JWT_SECRET" \
  --from-literal=IdentityServer__Clients__AggregatorSecret="$AGGREGATOR_CLIENT_SECRET" \
  --from-literal=KC_BOOTSTRAP_ADMIN_PASSWORD="$KEYCLOAK_ADMIN_PASSWORD" | kubectl apply -f -
```

Apply Deployments and ClusterIP Services:
```bash
kubectl apply -k deploy/k8s/
kubectl port-forward -n hotel-platform service/gateway 8080:80
curl http://localhost:8080/health/ready
```
