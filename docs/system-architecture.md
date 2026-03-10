# VnStock Platform — System Architecture

**Last Updated:** 2026-03-09
**Phase:** Foundation (v0.1.0)
**Status:** Complete & Operational

---

## Architecture Overview

VnStock is a microservice-oriented platform with three independent services orchestrated by Docker Compose, communicating through PostgreSQL and Redis, with Nginx routing HTTP/WebSocket traffic from the browser.

```
┌──────────────────────────────────────────────────────────┐
│                      Browser (Client)                     │
│          React 18 + TypeScript + Vite                    │
│     TailwindCSS · Zustand · TanStack Query               │
└────────────────────┬─────────────────────────────────────┘
                     │
                HTTP & WebSocket (port 80)
                     │
        ┌────────────▼─────────────────┐
        │      Nginx (Reverse Proxy)   │
        │      • Port 80 (HTTP)        │
        │      • /api → .NET API       │
        │      • /hubs → SignalR WS    │
        │      • / → React SPA         │
        └────────────┬──────────────────┘
                     │
        ┌────────────▼─────────────────────────┐
        │  .NET 8 Web API (Clean Architecture) │
        │  • REST/JSON endpoints               │
        │  • JWT authentication                │
        │  • SignalR hubs (Phase 2)            │
        │  • Port 5000                         │
        └────┬────────────────────────┬────────┘
             │                        │
    ┌────────▼──────────┐    ┌───────▼───────────┐
    │  PostgreSQL 16    │    │   Redis 7 (Pub/   │
    │  (Persistent)     │    │   Sub & Cache)    │
    │  • Partitioned    │    │   • Port 6379     │
    │    ticks table    │    │   • Channels:     │
    │  • Auth, users    │    │     ticks:symbol  │
    │  • Watchlist, P/L │    └───────┬───────────┘
    │  • Port 5432      │            │
    └──────────────────┘             │
                                     │
                ┌────────────────────▼──────────────┐
                │   Python Data Service (FastAPI)   │
                │   • TCBS polling (3s interval)    │
                │   • Tick normalization            │
                │   • Redis publisher               │
                │   • PostgreSQL batch writer       │
                │   • Port 8000                     │
                └───────────────────────────────────┘
```

---

## Service Decomposition

### 1. Frontend Service (React)

**Purpose:** User interface for stock market analytics

**Technology Stack**
- React 18 + TypeScript (type-safe components)
- Vite (fast build, HMR development)
- React Router v6 (SPA routing)
- Zustand (lightweight state management)
- TanStack Query (server state, caching)
- Axios (HTTP client, JWT interceptor)
- TailwindCSS v4 + shadcn/ui (styling)

**Communication**
- **Outbound:** REST/JSON to .NET API (via Nginx)
- **Inbound:** WebSocket from .NET API for real-time updates (Phase 2)
- **Storage:** localStorage (auth tokens, preferences)

**Responsibilities**
- User authentication (login, register, logout)
- Protected route enforcement
- API requests with automatic JWT refresh
- Display market data, watchlists, portfolios (Phase 2+)

**Deployment**
- Docker: Nginx serves compiled React app
- Build: Multi-stage Dockerfile (optimize image size)
- Entry Point: `/index.html` → React app

---

### 2. API Service (.NET 8)

**Purpose:** Core business logic, data access, user management

**Architecture:** Clean Architecture (4-layer)

```
┌─────────────────────────────────────┐
│    Controllers (HTTP Routing)       │  ← HTTP requests in
│  • AuthController (/api/auth/*)    │
└──────────────────┬──────────────────┘
                   │
┌──────────────────▼──────────────────┐
│  Application Layer (Services)       │  ← Business logic
│  • AuthService (register, login)    │
│  • TokenService (JWT generation)    │
│  • WatchlistService (Phase 3)       │
│  • PortfolioService (Phase 3)       │
└──────────────────┬──────────────────┘
                   │
┌──────────────────▼──────────────────┐
│  Infrastructure Layer (I/O)         │  ← External resources
│  • AppDbContext (EF Core)           │
│  • Redis client                     │
│  • Dependency injection             │
└──────────────────┬──────────────────┘
                   │
┌──────────────────▼──────────────────┐
│  Domain Layer (Core Entities)       │  ← Data models
│  • ApplicationUser                  │
│  • RefreshToken                     │
│  • Stock, Watchlist, Portfolio, etc │
└─────────────────────────────────────┘
```

**Communication**
- **Inbound:** HTTP/REST from Nginx (CORS enabled for localhost:5173)
- **Outbound:** PostgreSQL (EF Core), Redis (cache)
- **Real-time:** WebSocket for price updates (Phase 2: SignalR Hub)

**Responsibilities**
- User authentication and session management
- Authorization (JWT validation)
- Data persistence via EF Core
- Business logic (P&L calculation, alerts)
- SignalR Hub for real-time market data (Phase 2)

**Key Endpoints (Phase 1)**

| Endpoint | Method | Purpose | Auth |
|----------|--------|---------|------|
| `/api/auth/register` | POST | Create user account | No |
| `/api/auth/login` | POST | Authenticate user | No |
| `/api/auth/refresh` | POST | Get new access token | Refresh token |
| `/api/auth/logout` | POST | Invalidate session | JWT |
| `/api/auth/me` | GET | Current user profile | JWT |
| `/health` | GET | Service health check | No |

---

### 3. Data Service (Python FastAPI)

**Purpose:** Real-time market data polling, normalization, persistence

**Architecture:** Event-driven async service

```
TCBS Market Data Feed (3s polling)
         │
         ▼
┌────────────────────────┐
│ market_data_fetcher.py │  ← Polling thread
│ • vnstock.Investor     │
│ • Fetch latest ticks   │
└────────┬───────────────┘
         │ (raw tick data)
         ▼
┌────────────────────────┐
│ tick_normalizer.py     │  ← Transform
│ • Normalize field names│
│ • Type conversion      │
│ • Validation           │
└────────┬───────────────┘
         │ (standardized)
         ▼
    Two parallel writes:
    │
    ├──▶ redis_publisher.py   ── (ticks:symbol)
    │    └─ Immediate pub/sub
    │       (real-time subscribers)
    │
    └──▶ postgres_writer.py   ── (batch)
         └─ Accumulate 100 ticks
         └─ Flush every 5 seconds
         └─ SQL INSERT
```

**Communication**
- **Inbound:** PostgreSQL connection string (POSTGRES_DSN), Redis URL
- **Outbound:** TCBS via vnstock library (HTTP polling)
- **Data Distribution:** Redis pub/sub (ticks:{symbol} channel)

**Responsibilities**
- Continuous market data polling (3-second intervals)
- Tick data normalization and validation
- Real-time pub/sub to Redis
- Batch persistence to PostgreSQL
- Graceful reconnection on failures (exponential backoff)

**Health Check**
- GET `/health` → `{ "status": "healthy" }` (200 OK)

**Data Guarantees**
- At-least-once delivery (no data loss)
- TCBS polling continues even if Redis/PostgreSQL down
- Batch writes ensure transactional consistency
- Exponential backoff prevents overwhelming services

---

## Data Flow Scenarios

### Scenario 1: User Registration & Login

```
Browser                    Nginx              API                   PostgreSQL
  │                         │                  │                         │
  ├─POST /register ────────▶│                  │                         │
  │                         ├─POST /api/auth/  │                         │
  │                         │    register ────▶│                         │
  │                         │                  ├─Hash password ────────▶ │
  │                         │                  │                         │
  │                         │                  │◀─Insert user ──────────┤
  │                         │                  │                         │
  │                         │◀─JWT + refresh ──┤                         │
  │◀─200 + token cookie ────┤                  │                         │
  │                         │                  │                         │
```

**Data Stores:**
- User email/password hash stored in `AspNetUsers` table
- Refresh token stored in `RefreshTokens` table
- JWT issued to client (15-min expiry, stored in memory)

---

### Scenario 2: Real-Time Tick Data (Phase 2 Full Integration)

```
TCBS Market                Python Service          Redis              PostgreSQL
   Data Feed                 (FastAPI)            (Pub/Sub)              (Store)
     │                           │                   │                      │
     ├─Tick: VCB=180 ───────────▶│                   │                      │
     │                           │                   │                      │
     │                           ├─Normalize         │                      │
     │                           ├─Format            │                      │
     │                           │                   │                      │
     │                           ├─Publish ─────────▶│                      │
     │                           │ (ticks:VCB)       │                      │
     │                           │                   ├─Subscribe ──┐        │
     │                           │                   │             │        │
     │                           ├─Batch write ──────────────────┼────────▶│
     │                           │ (5s flush)        │             │        │
     │                           │                   │             │        │
     │                           │ [queue 100 rows]  │             │        │
     │                           │                   │             │        │
     │                           ├─PostgreSQL INSERT─────────────▶│        │
     │                           │ (all 100 ticks)   │             │        │
     │                           │                   │             │        │
```

**Real-time Path (Sub-second):**
- Python fetches tick → normalizes → publishes to Redis
- Browser subscribed via SignalR (Phase 2) receives tick immediately
- Update UI with new price

**Storage Path (5-second batch):**
- Python accumulates normalized ticks (max 100 or 5s timeout)
- Issues single SQL INSERT statement
- PostgreSQL persists to `ticks` table (partition pruning)

---

### Scenario 3: Portfolio P&L Calculation (Phase 3)

```
Browser              API              PostgreSQL              Redis
  │                  │                    │                    │
  ├─GET /portfolio ──▶│                    │                    │
  │                  ├─Fetch txns ───────▶│                    │
  │                  │                    │                    │
  │                  │◀─Transactions ─────┤                    │
  │                  │                    │                    │
  │                  ├─Fetch latest ticks │                    │
  │                  │    (from cache) ──────────────────────▶│
  │                  │                    │                    │
  │                  │◀─Cached prices ────────────────────────┤
  │                  │                    │                    │
  │                  ├─Calculate P&L      │                    │
  │                  │ (cost basis, qty,  │                    │
  │                  │  current price)    │                    │
  │                  │                    │                    │
  │◀─Portfolio JSON ──┤                    │                    │
  │                  │                    │                    │
```

**Data Access Patterns:**
1. **Transactions (slow, cached):** Query PostgreSQL → store in Redis cache (TTL 1 hour)
2. **Current Prices (real-time):** Subscribe to Redis ticks channel (Phase 2 SignalR)
3. **Calculation (in-memory):** PortfolioService calculates P&L on API server

**Cache Key:** `portfolio:{userId}:{portfolioId}` (invalidated on transaction)

---

## Communication Protocols

### REST/JSON (Synchronous)

**Frontend ↔ API:**
```
Request:
POST /api/auth/login
Content-Type: application/json
{
  "email": "user@example.com",
  "password": "secure123"
}

Response:
200 OK
{
  "accessToken": "eyJhbGciOiJIUzI1NiIs...",
  "expiresIn": 900,
  "tokenType": "Bearer"
}

Set-Cookie: refreshToken=...; HttpOnly; Secure; SameSite=Strict
```

### Redis Pub/Sub (Event-driven, Real-time)

**Python → Browser (Phase 2 via SignalR Hub):**
```
Redis Channel: ticks:VCB
Message:
{
  "symbol": "VCB",
  "price": 180.5,
  "volume": 5000000,
  "timestamp": "2026-03-09T10:30:45.123Z",
  "changePercent": 1.25
}
```

**Latency:** <100ms from tick arrival to browser update

### PostgreSQL (Transactional)

**API ↔ Database:**
```
Query: SELECT * FROM AspNetUsers WHERE Id = @userId
INSERT INTO RefreshTokens (UserId, Token, ExpiresAt) VALUES (@userId, @token, @expiresAt)
BEGIN TRANSACTION ... COMMIT
```

---

## Authentication & Authorization Flow

### JWT Token Structure

```
Header: { "alg": "HS256", "typ": "JWT" }

Payload:
{
  "sub": "user-id-guid",
  "email": "user@example.com",
  "iat": 1710000000,
  "exp": 1710000900,
  "iss": "vnstock-api",
  "aud": "vnstock-client"
}

Signature: HMAC-SHA256(Header.Payload, JWT_SECRET)
```

**Tokens:**
- **Access Token:** 15 minutes (in-memory, sent with each request)
- **Refresh Token:** 7 days (HttpOnly cookie, backend-only)

### Authorization Middleware

```
Request with JWT:
  ↓
Middleware validates signature
  ↓
Extract UserId from claims
  ↓
Check if token expired
  ↓
Inject UserId into request context
  ↓
Controller checks [Authorize] attribute
  ↓
200 OK OR 401 Unauthorized
```

---

## Data Consistency & ACID

### Database Transactions

**Registration Flow (Atomic):**
```sql
BEGIN TRANSACTION
  INSERT INTO AspNetUsers (Id, Email, PasswordHash, ...)
  INSERT INTO RefreshTokens (UserId, Token, ExpiresAt, ...)
COMMIT
```

**Guarantees:**
- All-or-nothing semantics (no orphaned tokens)
- Consistent state after each transaction
- Isolation level: READ_COMMITTED (default PostgreSQL)

### Time-Series Data (Tick Partitions)

**Partitioning for Consistency:**
```
Ticks table (partitioned by month)
  ├─ ticks_2026_03 (auto-created, 3/1-4/1)
  ├─ ticks_2026_04 (auto-created, 4/1-5/1)
  ├─ ticks_2026_05 (auto-created, 5/1-6/1)
  └─ (future partitions auto-created)
```

**Benefits:**
- Queries on specific months are extremely fast
- Deleting old data (e.g., >5 years) drops entire partitions
- Parallel inserts on different partitions (horizontal scaling)

---

## Error Handling & Resilience

### API Error Responses (ProblemDetails RFC 7807)

```json
{
  "type": "https://api.example.com/errors/validation",
  "title": "Invalid Request",
  "status": 400,
  "detail": "Email is required",
  "instance": "/api/auth/register"
}
```

**Status Codes:**
- `200 OK` — Success
- `400 Bad Request` — Validation error
- `401 Unauthorized` — Invalid credentials or expired token
- `403 Forbidden` — Insufficient permissions
- `404 Not Found` — Resource not found
- `429 Too Many Requests` — Rate limited (5 attempts/min)
- `500 Internal Server Error` — Unexpected server error

### Python Service Resilience

**Exponential Backoff on Failures:**
```
Connection lost
  ↓
Wait 1 second
  ↓ (retry)
Failed again
  ↓
Wait 2 seconds (exponential)
  ↓ (retry)
Failed again
  ↓
Wait 4 seconds
  ↓ (retry) ... up to 60 seconds
```

**Graceful Degradation:**
- TCBS polling continues even if Redis/PostgreSQL down
- Queued ticks retained in memory (up to 1000)
- Automatic flush when services recover

### Frontend Error Handling

**Token Refresh Retry:**
```
API returns 401
  ↓
Axios interceptor triggers refresh
  ↓
POST /api/auth/refresh
  ↓
New JWT received
  ↓
Original request retried with new token
  ↓
200 OK (user experience: seamless)
```

---

## Deployment Architecture

### Container Layout

```
Docker Network: vnstock-net
  ├─ postgres:16-alpine
  │   ├─ Volumes: /var/lib/postgresql/data
  │   ├─ Ports: 5432 (internal)
  │   └─ Health: pg_isready
  │
  ├─ redis:7-alpine
  │   ├─ Ports: 6379 (internal)
  │   └─ Health: redis-cli ping
  │
  ├─ api (.NET 8)
  │   ├─ Depends on: postgres, redis (healthy)
  │   ├─ Ports: 5000 (internal)
  │   └─ Health: GET /health
  │
  ├─ vnstock-service (Python)
  │   ├─ Depends on: postgres, redis (healthy)
  │   ├─ Ports: 8000 (internal)
  │   └─ Health: GET /health
  │
  └─ nginx (Reverse Proxy)
      ├─ Ports: 80 → 80 (external)
      ├─ Volumes: ./nginx/nginx.conf
      └─ Depends on: api (started)
```

### Service Dependencies

```
Startup Order (enforced by docker-compose):
1. postgres + redis (no dependencies)
   ↓ (wait for health checks)
2. api + vnstock-service (depends on postgres + redis healthy)
   ↓ (wait for start)
3. nginx (depends on api started)
   ↓
All services ready for traffic
```

---

## Scaling Considerations

### Horizontal Scaling (Multiple Instances)

**Phase 2+: Load Balancing**

```
Browser Traffic
  ↓
Nginx Load Balancer
  ├─ api:5000 (instance 1)
  ├─ api:5000 (instance 2)
  └─ api:5000 (instance 3)
         ↓
    PostgreSQL (single source of truth)
    Redis Cluster (pub/sub backplane)
```

**Requirements:**
- Nginx upstream with multiple api servers
- Redis backplane for SignalR session sharing (Phase 2)
- Session affinity for stateful connections (optional)

### Vertical Scaling (Larger Instances)

**Database Optimization:**
- Connection pool size: Increase from 20 to 50+
- Work memory: Increase for sorting/hashing operations
- Shared buffers: 25% of system RAM (up to 40GB)

**Python Service:**
- Workers: Increase from 1 to 4+ (via Uvicorn workers)
- Batch size: Increase from 100 to 1000 ticks
- Flush interval: Adjust based on volume (3s, 5s, or 10s)

### Query Optimization

**Existing Indexes (Phase 1):**
- `ticks(symbol, timestamp DESC)` — O(log N) lookup
- `ohlcv_daily(symbol, date DESC)` — O(log N) lookup

**Future Indexes (Phase 2+):**
- Partial index on `price_alerts(user_id, is_active)` for active alerts
- Composite index on `watchlists(user_id, symbol)` for user lookups

---

## Security Architecture

### Network Isolation

```
External (Internet)
  ↓ [Port 80 HTTP]
Nginx (Reverse Proxy)
  ↓ [Internal network, no external port]
api, vnstock-service, postgres, redis
  [All on internal vnstock-net docker network]
  [No direct external access]
```

**Benefits:**
- Database not exposed to internet
- Redis not exposed to internet
- Python service only accessible internally
- Single entry point (Nginx) for monitoring/logging

### Authentication & Authorization

**Multi-layer:**

1. **Layer 1 — Browser to Nginx:** HTTPS (TLS, Phase 4)
2. **Layer 2 — Nginx to API:** Implicit (same Docker network, no auth needed)
3. **Layer 3 — Browser to API:** JWT in Authorization header
4. **Layer 4 — API to Database:** Database user permissions (restricted)

### Secrets Management

**Phase 1 — Development:**
- Secrets in `.env` file (local only, not committed)
- Read by docker-compose at startup
- Injected as environment variables into containers

**Phase 4+ — Production:**
- Secrets in Docker secrets or external vault
- Rotating secrets without container restart
- Audit logging for secret access

---

## Monitoring & Observability (Phase 4)

### Health Checks (Built-in Phase 1)

```
Endpoint        Interval  Timeout  Healthy Condition
/health         10s       5s       HTTP 200
pg_isready      10s       5s       pg_isready exit 0
redis-cli ping  10s       5s       redis-cli exit 0
```

### Logging Strategy (Phase 4 - Serilog)

**Structured Logs:**
```json
{
  "@timestamp": "2026-03-09T10:30:45.123Z",
  "level": "Information",
  "logger": "VnStock.API.Controllers.AuthController",
  "message": "User login successful",
  "userId": "550e8400-e29b-41d4-a716-446655440000",
  "email": "user@example.com",
  "duration_ms": 45
}
```

**Log Levels:**
- `Debug` — Detailed diagnostics (TCBS polling, cache hits/misses)
- `Information` — User actions (login, portfolio update)
- `Warning` — Degraded behavior (slow queries, retries)
- `Error` — Failures (exceptions, invalid data)
- `Fatal` — System failures (database down, can't recover)

### Metrics & Alerting

**Key Metrics:**
- API response time (p50, p99)
- Database query latency
- Redis pub/sub message rate
- Python service polling health
- JWT token refresh rate
- Login attempt rate (detect brute force)

---

## Technology Rationale

### Why .NET 8?

- **Enterprise-Grade:** Clean Architecture patterns, DI built-in
- **Performance:** Compiled, static typing prevents runtime errors
- **Entity Framework:** LINQ queries, migrations, lazy loading
- **ASP.NET Identity:** Bcrypt hashing, role-based authorization
- **Mature Ecosystem:** Established patterns, large community

### Why Python FastAPI?

- **Simplicity:** Minimal boilerplate for data polling
- **Async/Await:** Non-blocking I/O for polling + Redis pub/sub
- **Type Hints:** Static type checking (mypy)
- **vnstock Integration:** Native Python library for TCBS data

### Why PostgreSQL?

- **ACID Transactions:** Reliable financial data
- **Partitioning:** Sub-second queries on billions of ticks
- **JSON Support:** Flexible schema for Phase 3+ features
- **Proven:** Battle-tested for high-volume time-series data

### Why Redis?

- **Latency:** <1ms pub/sub for real-time ticks
- **Pub/Sub:** Native support for broadcast messaging
- **Caching:** Fast lookups for watchlists, prices
- **Simplicity:** Single process, no replication complexity (Phase 1)

### Why React + Zustand?

- **Component Model:** Declarative UI, easy to reason about
- **Vite:** Fast refresh development experience
- **Zustand:** Minimal state management (vs Redux verbosity)
- **TypeScript:** Catch errors at compile time
- **Community:** Largest React ecosystem for finance apps

---

## API Contract Examples

### Request/Response Examples

**Register User**
```
POST /api/auth/register
Content-Type: application/json

{
  "email": "alice@example.com",
  "password": "SecurePass123!",
  "confirmPassword": "SecurePass123!"
}

---

200 OK
Content-Type: application/json
Set-Cookie: refreshToken=...; HttpOnly

{
  "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "expiresIn": 900,
  "tokenType": "Bearer"
}
```

**Login User**
```
POST /api/auth/login
Content-Type: application/json

{
  "email": "alice@example.com",
  "password": "SecurePass123!"
}

---

200 OK
{
  "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "expiresIn": 900,
  "tokenType": "Bearer"
}
```

**Refresh Token**
```
POST /api/auth/refresh
Content-Type: application/json
Cookie: refreshToken=...

{}

---

200 OK
{
  "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "expiresIn": 900,
  "tokenType": "Bearer"
}
```

---

## Architecture Decision Records (ADRs)

### ADR-1: Clean Architecture for .NET Backend

**Problem:** Monolithic API prone to tight coupling, hard to test

**Decision:** 4-layer Clean Architecture (Domain → Application → Infrastructure → API)

**Rationale:**
- Domain logic independent of frameworks
- Easy to test services in isolation
- Clear separation of concerns
- Easier to add features without regression

**Consequence:** More boilerplate (DTOs, mappers) but better long-term maintainability

---

### ADR-2: Redis Pub/Sub for Real-time Ticks

**Problem:** Broadcasting price updates to 1000+ connected clients

**Decision:** Redis pub/sub channels (`ticks:{symbol}`) with SignalR backplane

**Rationale:**
- Sub-millisecond latency for pub/sub
- Native support for fan-out messaging
- SignalR adds browser WebSocket layer

**Consequence:** Requires Redis instance (single point of failure in Phase 1, cluster in Phase 3)

---

### ADR-3: PostgreSQL Table Partitioning by Month

**Problem:** Ticks table grows ~1GB/month per 3000 symbols (query slowdown)

**Decision:** Monthly partitions with automatic archival

**Rationale:**
- Partition pruning: only relevant month scanned
- Easy data retention: drop old partitions
- Scalability: insert/query performance constant

**Consequence:** Requires partition maintenance scripts (Phase 4)

---

### ADR-4: JWT with Refresh Token Rotation

**Problem:** Lost/compromised JWT tokens can't be revoked; long-lived tokens risky

**Decision:** 15-min access token + 7-day refresh token with rotation

**Rationale:**
- Short-lived access token limits exposure window
- Rotation: each refresh invalidates old token (can't reuse)
- HttpOnly cookies prevent JS theft

**Consequence:** Extra network round-trip on expiry (negligible <100ms)

---

## Next Steps (Phase 2 Architecture)

### SignalR Hub Integration

```
Browser
  ↓ [WebSocket /hubs/market]
SignalR Hub (on API server)
  ├─ Subscribe to Redis: ticks:{symbol}
  ├─ Broadcast to connected clients
  └─ Manage subscriptions per client
```

### Market Data API Endpoints

```
GET /api/market/ohlcv/{symbol}?start=2026-01-01&end=2026-03-09&interval=1d
→ Historical 5-year candlestick data from ohlcv_daily table
```

### Caching Layer

```
API
  ├─ [Cache] Latest price per symbol (TTL 1s)
  ├─ [Cache] OHLCV candles (TTL 1 hour)
  └─ [Cache] User watchlists (TTL 5 min, invalidate on change)
```

---

**Last Updated:** 2026-03-09 | **Status:** Phase 1 Complete (v0.1.0-foundation)
