# EKS Octopus microservices baseline

Reference architecture for deploying .NET 8 microservices to Amazon EKS with Helm, GitHub Actions, and Octopus Deploy.

| Layer | Name |
| --- | --- |
| GitHub repository | `eks-octopus-microservices-baseline` |
| .NET solution | `MicroservicesBaseline.sln` |
| Workloads | `AuthApi`, `ProductApi`, `OrderApi` |

The repository name is the delivery architecture. The solution and namespaces are the workload. They are not the same on purpose.

## Current baseline (Phase 1)

- Three ASP.NET Core minimal APIs
- JWT authentication: `AuthApi` issues tokens, `ProductApi` and `OrderApi` validate them locally
- In-memory data (no database)
- Swagger, with an **Authorize** button on every API
- Health endpoint at `/health` (anonymous, for Kubernetes probes later)
- xUnit tests, including JWT security tests
- Dockerfiles and `docker-compose.yml` (ProductApi, OrderApi)
- Kubernetes manifests under `deploy/kubernetes` (local cluster)

Helm, GitHub Actions, Octopus, and EKS are not in this phase yet.

## Layout

```text
MicroservicesBaseline.sln
src/AuthApi
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
| Auth API | http://localhost:5250 | not containerised yet | `POST /auth/login`, `GET /auth/me` 🔒, `GET /health` |
| Product API | http://localhost:5035 | http://localhost:8081 | `GET /products` 🔒, `GET /products/{id}` 🔒, `POST /products` 🔒, `GET /security/me` 🔒, `GET /version`, `GET /health` |
| Order API | http://localhost:5190 | http://localhost:8082 | `GET /orders` 🔒, `GET /orders/{id}` 🔒, `POST /orders` 🔒, `GET /security/me` 🔒, `GET /health` |

🔒 = requires a valid JWT (`Authorization: Bearer <token>`). Without one the response is `401 Unauthorized`.

The APIs do not call each other. `OrderApi` stores a `ProductId` only.

Swagger UI:

- Auth API: http://localhost:5250/swagger
- Product API: http://localhost:5035/swagger
- Order API: http://localhost:5190/swagger

## Authentication and authorization

### Architecture

```text
                 Client
                   |
                   | POST /auth/login (username/password)
                   v
              +-----------+
              |  AuthApi  |   issues JWT (HS256)
              +-----------+
                   |
                   | JWT access token
             +-----+------+
             |            |
             v            v
       ProductApi      OrderApi
       validate JWT    validate JWT     (locally, no call to AuthApi)
```

- **AuthApi is the only service that issues tokens.** ProductApi and OrderApi are resource APIs: they have no token service and never call AuthApi.
- Each resource API validates the token **in-process** using the shared issuer, audience and signing key. AuthApi can be down and existing tokens still work until they expire.

### AuthApi

| Item | Detail |
| --- | --- |
| Login | `POST /auth/login` with `{ "username": "...", "password": "..." }` |
| Response | `{ "accessToken": "...", "tokenType": "Bearer", "expiresIn": 3600 }` |
| Users | `IUserStore` → `InMemoryUserStore` (**POC only**) |
| Passwords | Stored as hashes only (ASP.NET Core `PasswordHasher`, PBKDF2). No plaintext passwords in code. |
| Tokens | `ITokenService` → `TokenService`, signed with HS256 |
| Failed login | Always `401` with no detail, and the same timing for unknown user and wrong password (no username discovery) |

Test users (both with password `password`):

| Username | Role | `sub` |
| --- | --- | --- |
| `admin` | `Admin` | `1` |
| `user1` | `User` | `2` |

> **POC only.** A production system uses a persistent identity store (or an external identity provider) with user management, lockout, MFA, password policy and auditing.

### Token contents

| Claim | Example | Meaning |
| --- | --- | --- |
| `sub` | `1` | User ID |
| `username` | `admin` | Username (`User.Identity.Name`) |
| `role` | `Admin` | Role (`User.IsInRole(...)`) |
| `jti` | GUID | Unique token ID |
| `iss` | `AuthApi` | Issuer |
| `aud` | `Microservices` | Audience |
| `iat` / `nbf` / `exp` | Unix time | Issued at / not before / expiry |

The token is **signed, not encrypted**. Anyone can read the claims (for example on jwt.io), so never put sensitive data in them.

### Validation rules (identical in all three APIs)

| Check | Rule |
| --- | --- |
| Signature | HMAC-SHA256 with `Jwt:SecretKey` |
| Allowed algorithm | `HS256` only |
| Issuer | must be `AuthApi` |
| Audience | must be `Microservices` |
| Lifetime | `exp` / `nbf` enforced, 30-second clock skew |
| Claim mapping | `MapInboundClaims = false`, `NameClaimType = "username"`, `RoleClaimType = "role"` |

Authentication answers "does this request carry a valid JWT?". Authorization currently answers only "is this endpoint protected?" (`.RequireAuthorization()`). Role and policy-based rules come next.

Anonymous by design: `/health` (probes), `/`, Swagger, and ProductApi `/version`.

### Configuration

All three APIs use the same strongly typed `JwtOptions` (Options pattern with `ValidateDataAnnotations()` and `ValidateOnStart()`):

```json
"Jwt": {
  "Issuer": "AuthApi",
  "Audience": "Microservices",
  "SecretKey": "",
  "AccessTokenLifetimeMinutes": 60
}
```

| Where | `Jwt:SecretKey` |
| --- | --- |
| `appsettings.json` (committed) | empty, on purpose |
| `appsettings.Development.json` (committed) | `DEV-ONLY-...` placeholder, identical in all three APIs. Not a real secret. |
| Any other environment | must be supplied, e.g. environment variable `Jwt__SecretKey` |

If the key is missing or shorter than 32 characters, the API **refuses to start** with an `OptionsValidationException` (fail fast).

To avoid the committed placeholder locally, use user-secrets (run in each of the three project folders, with the same key):

```bash
dotnet user-secrets init
dotnet user-secrets set "Jwt:SecretKey" "<a key of at least 32 characters>"
```

### Code map

```text
src/AuthApi/
  Configuration/JwtOptions.cs
  Models/LoginRequest.cs, LoginResponse.cs, UserAccount.cs
  Services/IUserStore.cs, InMemoryUserStore.cs      (POC only)
  Services/ITokenService.cs, TokenService.cs, AuthClaimTypes.cs
src/ProductApi/ and src/OrderApi/
  Configuration/JwtOptions.cs
  Security/AuthClaimTypes.cs
  Security/SecurityServiceCollectionExtensions.cs    (AddJwtBearerAuthentication, AddSwaggerGenWithJwtBearer)
tests/*/TestJwt.cs          test-only token minting + JwtApiFactory (test signing key)
tests/*/SecurityTests.cs    security scenarios 1-7, claims, anonymous endpoints
```

## Run locally

```bash
dotnet build MicroservicesBaseline.sln
dotnet test MicroservicesBaseline.sln
```

Start the three services (three terminals):

```bash
dotnet run --project src/AuthApi    --launch-profile http
dotnet run --project src/ProductApi --launch-profile http
dotnet run --project src/OrderApi   --launch-profile http
```

The `http` launch profile sets `ASPNETCORE_ENVIRONMENT=Development`, which loads the development signing key.

### Test with Swagger

1. AuthApi Swagger → `POST /auth/login` with `admin` / `password` → copy `accessToken` (without quotes).
2. ProductApi Swagger → **Authorize** (top right) → paste the token **without** `Bearer ` → Authorize → Close.
3. Call `GET /products` → `200`. Call `GET /security/me` → `{"userId":"1","username":"admin","role":"Admin"}`.
4. Repeat steps 2–3 in OrderApi Swagger (each Swagger page keeps its own token).

If the Authorize button is missing, hard-refresh the page (Ctrl+F5) or stop old running instances.

### Test with curl (Windows cmd)

```bash
curl -X POST http://localhost:5250/auth/login -H "Content-Type: application/json" -d "{\"username\":\"admin\",\"password\":\"password\"}"
curl http://localhost:5035/products -H "Authorization: Bearer <token>"
curl http://localhost:5190/security/me -H "Authorization: Bearer <token>"
```

PowerShell:

```powershell
$t = (Invoke-RestMethod -Method Post http://localhost:5250/auth/login -ContentType "application/json" -Body '{"username":"admin","password":"password"}').accessToken
Invoke-RestMethod http://localhost:5035/products -Headers @{ Authorization = "Bearer $t" }
```

The `.http` files in each project contain the same requests for Visual Studio.

### Expected security behaviour

| # | Scenario | Result |
| --- | --- | --- |
| 1 | No token | `401` |
| 2 | Random / malformed token | `401` |
| 3 | Valid AuthApi token | `200` |
| 4 | Wrong issuer | `401` |
| 5 | Wrong audience | `401` |
| 6 | Expired token | `401` |
| 7 | Signed with a different key | `401` |

The reason for each `401` is in the `WWW-Authenticate` response header (for example `The issuer 'EvilIssuer' is invalid`). All seven scenarios are covered by `SecurityTests` in both test projects.

To reproduce 4–7 manually with the real AuthApi, restart it with an override, log in, and use that token:

```bash
dotnet run --project src/AuthApi --launch-profile http -- --Jwt:Issuer=EvilIssuer
dotnet run --project src/AuthApi --launch-profile http -- --Jwt:Audience=SomeOtherApi
dotnet run --project src/AuthApi --launch-profile http -- --Jwt:AccessTokenLifetimeMinutes=1
dotnet run --project src/AuthApi --launch-profile http -- --Jwt:SecretKey=another-completely-different-key-0123456789
```

For the expiry test, wait about 2 minutes (60 s lifetime + 30 s clock skew).

## Run with Docker

> **Note:** since JWT validation was added, containers run as `Production` and need `Jwt__SecretKey`. Without it ProductApi and OrderApi stop at startup. The compose file does not set it yet, and AuthApi has no Dockerfile yet.

```bash
docker compose up --build
```

- Product API: http://localhost:8081/swagger
- Order API: http://localhost:8082/swagger
- Health: http://localhost:8081/health and http://localhost:8082/health

Compose and local Kubernetes share these image tags: `product-api:local` and `order-api:local`.

## Run on local Kubernetes

> **Note:** the same `Jwt__SecretKey` requirement applies. Rebuilt images without the key will end in `CrashLoopBackOff` until a Kubernetes Secret is added (planned).

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

## Security roadmap

| Now (POC) | Later |
| --- | --- |
| In-memory users | Persistent identity store or external identity provider |
| HS256 shared secret (every API holds the signing key) | RS256/ES256: AuthApi keeps the private key, APIs fetch the public key via JWKS |
| `.RequireAuthorization()` only | `.RequireRole("Admin")`, then policy-based authorization |
| Dev key in `appsettings.Development.json` | Kubernetes Secret, synced from AWS Secrets Manager |
| `JwtOptions` duplicated in three projects | Shared library |
| No refresh tokens, no revocation | Short-lived access tokens + refresh tokens |

## Delivery sequence

Keep the workload frozen. Add architecture around it:

1. .NET 8 + Docker (done)
2. Kubernetes manifests (Deployment, Service, probes on `/health`)
3. Helm chart
4. GitHub Actions: restore, test, build images, push to a registry
5. Octopus: promote the same image/chart through Dev → Test → Prod
6. AWS EKS: run the chart on the cluster

Workload contract: [docs/workload-spec.md](docs/workload-spec.md)
