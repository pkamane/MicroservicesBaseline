# EKS Octopus microservices baseline

Reference architecture for deploying .NET 8 microservices to Amazon EKS with Helm, GitHub Actions, and Octopus Deploy.

| Layer | Name |
| --- | --- |
| GitHub repository | `eks-octopus-microservices-baseline` |
| .NET solution | `MicroservicesBaseline.sln` |
| Workloads | `ProductApi`, `OrderApi` |

The repository name is the delivery architecture. The solution and namespaces are the workload. They are not the same on purpose.

## Current baseline (Phase 1)

- Two ASP.NET Core minimal APIs
- In-memory data (no database)
- Swagger
- Health endpoint at `/health` (Kubernetes probes later)
- xUnit tests
- Dockerfiles and `docker-compose.yml`
- Kubernetes manifests under `deploy/kubernetes` (local cluster)

Helm, GitHub Actions, Octopus, and EKS are not in this phase yet.

## Layout

```text
MicroservicesBaseline.sln
src/ProductApi
src/OrderApi
tests/ProductApi.Tests
tests/OrderApi.Tests
docs/workload-spec.md
deploy/kubernetes
```

## APIs

| Service | Local URL | Docker URL | Endpoints |
| --- | --- | --- | --- |
| Product API | http://localhost:5035 | http://localhost:8081 | `GET /products`, `GET /products/{id}`, `POST /products`, `GET /health` |
| Order API | http://localhost:5190 | http://localhost:8082 | `GET /orders`, `GET /orders/{id}`, `POST /orders`, `GET /health` |

The APIs do not call each other yet. `OrderApi` stores a `ProductId` only.

Swagger UI:

- Product API: http://localhost:5035/swagger
- Order API: http://localhost:5190/swagger

## Run locally

```bash
dotnet test MicroservicesBaseline.sln
dotnet run --project src/ProductApi
dotnet run --project src/OrderApi
```

## Run with Docker

```bash
docker compose up --build
```

- Product API: http://localhost:8081/swagger
- Order API: http://localhost:8082/swagger
- Health: http://localhost:8081/health and http://localhost:8082/health

Compose and local Kubernetes share these image tags: `product-api:local` and `order-api:local`.

## Run on local Kubernetes

1. Enable Kubernetes in Docker Desktop (Settings → Kubernetes → Enable), wait until it is green.
2. Confirm the cluster: `kubectl get nodes`
3. Build local images (if you have not already): `docker compose build`
4. Apply manifests:

```bash
kubectl apply -f deploy/kubernetes
```

5. Forward ports (two terminals):

```bash
kubectl port-forward -n microservices-baseline svc/product-api 8081:80
kubectl port-forward -n microservices-baseline svc/order-api 8082:80
```

Then use the same URLs as Compose: http://localhost:8081/swagger and http://localhost:8082/swagger

`imagePullPolicy: Never` means Kubernetes uses images already on your machine. It will not pull from Docker Hub yet.

## Delivery sequence

Keep the workload frozen. Add architecture around it:

1. .NET 8 + Docker (done)
2. Kubernetes manifests (Deployment, Service, probes on `/health`)
3. Helm chart
4. GitHub Actions: restore, test, build images, push to a registry
5. Octopus: promote the same image/chart through Dev → Test → Prod
6. AWS EKS: run the chart on the cluster

Workload contract: [docs/workload-spec.md](docs/workload-spec.md)
