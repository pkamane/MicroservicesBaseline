# Workload specification

.NET 8 microservices used by the `eks-octopus-microservices-baseline` architecture.

The GitHub repository name describes delivery (EKS + Octopus). This document describes the **workload** only: two APIs that stay independent of cloud and CD tooling.

## Purpose

A small, production-shaped .NET 8 solution that can be:

- containerized
- deployed to Kubernetes
- packaged with Helm
- built and tested in GitHub Actions
- promoted with Octopus Deploy onto Amazon EKS

The workload is deliberately small so the architecture around it stays visible.

---

# Phase 1 scope

Implement the application foundation and Docker support.

Do not implement Kubernetes, Helm, GitHub Actions, Octopus Deploy, AWS/EKS, databases, or authentication in this phase.

---

# 1. Technology

- .NET 8
- C#
- ASP.NET Core Web API
- Minimal APIs
- Swagger / OpenAPI
- Built-in dependency injection
- Built-in logging
- Nullable reference types
- Docker

Avoid unnecessary third-party packages.

---

# 2. Solution structure

```text
eks-octopus-microservices-baseline
│
├── MicroservicesBaseline.sln
│
├── src
│   ├── ProductApi
│   └── OrderApi
│
├── tests
│   ├── ProductApi.Tests
│   └── OrderApi.Tests
│
├── docs
│   └── workload-spec.md
│
├── .gitignore
├── docker-compose.yml
└── README.md
```

Service projects and namespaces stay `ProductApi` and `OrderApi`. Delivery names (EKS, Octopus) do not appear in C# namespaces.

---

# 3. Application behaviour

Keep both services independent for Phase 1. Do not call one API from the other.

Use in-memory storage with seed data so `GET` works immediately.

## ProductApi

- `GET /health`
- `GET /products`
- `GET /products/{id}`
- `POST /products` with `{ "name": "Webcam", "price": 59.99 }`

`Product`: `Id`, `Name`, `Price`

## OrderApi

- `GET /health`
- `GET /orders`
- `GET /orders/{id}`
- `POST /orders` with `{ "productId": 1, "quantity": 2 }`

`Order`: `Id`, `ProductId`, `Quantity`, `CreatedAt`

TLS belongs at the ingress later. The APIs listen on HTTP inside the container (`8080`).

---

# 4. Docker

Each API has a multi-stage Dockerfile.

`docker-compose.yml` runs both APIs locally:

- Product API: `http://localhost:8081`
- Order API: `http://localhost:8082`

---

# Later phases (out of scope for Phase 1)

Keep the workload small. Add delivery around it:

2. Kubernetes manifests and probes
3. Helm
4. GitHub Actions (build, test, publish images)
5. Octopus Deploy (environment promotion)
6. AWS EKS
