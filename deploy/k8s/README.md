# Лабораторна № 10 — Розгортання в Kubernetes

## Опис
Розгортання платформи Hotel Reservation System у локальному кластері Kubernetes двома способами:
1. Автоматична генерація маніфестів через Aspir8 (aspirate) з AppHost
2. Ручне створення YAML-маніфестів

## Структура
- `Dockerfile` — багатоетапний Dockerfile для кожного сервісу (SDK build → publish → aspnet runtime)
- `namespace.yaml` — Namespace `hotel-platform`
- `configmap.yaml` — ConfigMap з адресами сервісів для Gateway
- `secrets.yaml` — Secret з рядками підключення та ключем JWT
- `*-deployment.yaml` — Deployment з 2 репліками, liveness/readiness probes
- `*-service.yaml` — ClusterIP Service для кожного сервісу
- `aspirate-output/` — директорія для маніфестів, згенерованих aspirate

## Сервіси
| Сервіс | Образ | Port | Health endpoint |
|--------|-------|------|-----------------|
| auth-server | auth-server:v1 | 8080 | /health |
| catalog-api | catalog-api:v1 | 8080 | /health/live, /health/ready |
| reservation-api | reservation-api:v1 | 8080 | /health/live, /health/ready |
| reviews-api | reviews-api:v1 | 8080 | /health/live, /health/ready |
| aggregator | aggregator:v1 | 8080 | /health/live, /health/ready |
| gateway | gateway:v1 | 8080 | /health/live, /health/ready |

## Розгортання

### Крок 1: Увімкнути Kubernetes в Docker Desktop
```bash
kubectl config use-context docker-desktop
kubectl version --client
```

### Крок 2: Встановити aspirate
```bash
dotnet tool install -g aspirate
```

### Крок 3: Згенерувати маніфести через Aspirate
```bash
cd Platform.AppHost
aspirate init
aspirate generate
aspirate apply
```

### Крок 4: Ручне розгортання ключових сервісів
```bash
kubectl apply -f deploy/k8s/namespace.yaml
kubectl apply -f deploy/k8s/configmap.yaml
kubectl apply -f deploy/k8s/secrets.yaml
kubectl apply -f deploy/k8s/catalog-api-deployment.yaml -f deploy/k8s/catalog-api-service.yaml
kubectl apply -f deploy/k8s/gateway-deployment.yaml -f deploy/k8s/gateway-service.yaml
```

### Крок 5: Перевірка
```bash
kubectl get pods -n hotel-platform
kubectl get services -n hotel-platform
kubectl port-forward service/gateway 8080:80 -n hotel-platform
curl http://localhost:8080/api/roomcategory/paged?page=1&pageSize=5
```

## Контрольні точки
- Усі поди в стані Running
- Запит через gateway повертає дані
- Після видалення поду Deployment створює новий
- При недоступній базі под стає NotReady (readiness), але не перезапускається (liveness)