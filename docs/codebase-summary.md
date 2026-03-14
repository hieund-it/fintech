# VnStock Platform — Codebase Summary

**Last Updated:** 2026-03-14
**Status:** Phase 3 User Features Complete — MVP Ready
**Version:** v1.0.0-mvp

---

## Executive Overview

VnStock is a full-stack Vietnamese stock market analytics and portfolio management platform built with:
- **.NET 8** backend (Clean Architecture)
- **Python 3.11** FastAPI data service (TCBS market data)
- **React 18 + TypeScript** frontend with Vite
- **PostgreSQL 16** (partitioned for time-series data)
- **Redis 7** (pub/sub, caching)
- **Docker Compose** orchestration

Phase 3 (User Features) is complete with watchlist management, portfolio tracking, P&L calculations, price alerts, and dashboard.

---

## Directory Structure

```
fintech/
├── README.md                          # Project overview
├── CLAUDE.md                          # Development workflow
├── Makefile                           # Docker shortcuts (up, down, logs, db-shell)
├── docker-compose.yml                 # Service orchestration (5 services)
├── .env.example                       # Environment variable reference
│
├── src/                               # .NET 8 Backend (Clean Architecture)
│   ├── VnStock.sln                    # Solution file
│   ├── VnStock.Domain/
│   │   └── Entities/
│   │       ├── ApplicationUser.cs      # IdentityUser<Guid> + custom fields
│   │       ├── RefreshToken.cs         # Token rotation, expiry tracking
│   │       ├── Stock.cs                # Market metadata (symbol, name, exchange, sector)
│   │       ├── OhlcvDaily.cs           # Daily OHLCV bars (Open/High/Low/Close/Volume)
│   │       ├── WatchlistItem.cs        # User watchlist entries (Phase 3 ✓)
│   │       ├── Portfolio.cs            # Portfolio container (Phase 3 ✓)
│   │       ├── Transaction.cs          # Buy/sell order history (Phase 3 ✓)
│   │       └── PriceAlert.cs           # Price alert definitions (Phase 3 ✓)
│   │
│   ├── VnStock.Application/
│   │   ├── Auth/
│   │   │   ├── Dtos/
│   │   │   │   ├── RegisterRequest.cs
│   │   │   │   ├── LoginRequest.cs
│   │   │   │   ├── RefreshTokenRequest.cs
│   │   │   │   └── AuthResponse.cs
│   │   │   └── Services/
│   │   │       ├── AuthService.cs      # Register, login, refresh, logout
│   │   │       └── TokenService.cs     # JWT generation, validation
│   │   ├── Market/
│   │   │   ├── DTOs/
│   │   │   │   ├── StockDto.cs
│   │   │   │   └── OhlcvDto.cs
│   │   │   ├── Services/
│   │   │   │   ├── IMarketDataService.cs
│   │   │   │   └── MarketDataService.cs  # Query stocks, OHLCV, sectors
│   │   │   └── Interfaces/
│   │   │       └── IMarketDbContext.cs
│   │   ├── User/
│   │   │   ├── Dtos/
│   │   │   │   ├── WatchlistItemDto.cs
│   │   │   │   ├── PortfolioDto.cs
│   │   │   │   ├── TransactionDto.cs
│   │   │   │   ├── PriceAlertDto.cs
│   │   │   │   └── PortfolioSummaryDto.cs
│   │   │   └── Services/
│   │   │       ├── WatchlistService.cs    # Add/remove watchlist items (Phase 3)
│   │   │       ├── PortfolioService.cs    # CRUD portfolios + P&L calc (Phase 3)
│   │   │       ├── TransactionService.cs  # Transaction CRUD (Phase 3)
│   │   │       ├── PriceAlertService.cs   # Alert CRUD (Phase 3)
│   │   │       └── PortfolioPLEngine.cs   # Weighted-avg cost basis + P&L (Phase 3)
│   │   └── Interfaces/
│   │       └── IAuthDbContext.cs
│   │
│   ├── VnStock.Infrastructure/
│   │   ├── Data/
│   │   │   ├── AppDbContext.cs         # IdentityDbContext + all tables
│   │   │   └── DependencyInjection.cs  # Service registration
│   │   ├── BackgroundServices/
│   │   │   └── AlertEngineService.cs   # Background service: monitors prices, fires alerts (Phase 3)
│   │   ├── Email/
│   │   │   └── SmtpEmailService.cs     # MailKit SMTP client (Phase 3)
│   │   └── Migrations/
│   │       ├── 20260309094700_InitialAuth/
│   │       ├── 20260313081536_AddMarketTables/
│   │       └── 20260314_AddUserFeatures/   # Watchlists, Portfolios, Transactions, PriceAlerts
│   │
│   └── VnStock.API/
│       ├── Controllers/
│       │   ├── AuthController.cs       # REST endpoints: /api/auth/*
│       │   ├── StocksController.cs     # REST endpoints: /api/stocks/*
│       │   ├── WatchlistController.cs  # REST endpoints: /api/watchlist/* (Phase 3)
│       │   ├── PortfoliosController.cs # REST endpoints: /api/portfolios/* (Phase 3)
│       │   └── AlertsController.cs     # REST endpoints: /api/alerts/* (Phase 3)
│       ├── Hubs/
│       │   └── MarketHub.cs            # SignalR real-time ticks (JWT auth)
│       ├── Services/
│       │   └── RedisMarketDataSubscriber.cs  # Redis → SignalR bridge
│       ├── Middleware/
│       │   └── (JWT validation, CORS)
│       ├── Program.cs                  # Startup configuration
│       ├── appsettings.json            # Configuration
│       └── Dockerfile                  # Multi-stage build
│
├── client/                             # React 18 + Vite Frontend
│   ├── src/
│   │   ├── App.tsx                     # Root component, routing
│   │   ├── main.tsx                    # Vite entry point
│   │   │
│   │   ├── pages/
│   │   │   ├── login-page.tsx          # Login form
│   │   │   ├── register-page.tsx       # Registration form
│   │   │   ├── dashboard-page.tsx      # User dashboard with 3-panel layout (Phase 3)
│   │   │   └── market-page.tsx         # Market data + price board
│   │   │
│   │   ├── routes/
│   │   │   └── protected-route.tsx     # ProtectedRoute wrapper
│   │   │
│   │   ├── stores/
│   │   │   ├── auth-store.ts           # Zustand auth (login, logout, token)
│   │   │   ├── market-store.ts         # Zustand market (real-time ticks)
│   │   │   ├── watchlist-store.ts      # Zustand watchlist (Phase 3)
│   │   │   └── portfolio-store.ts      # Zustand portfolio (Phase 3)
│   │   │
│   │   ├── services/
│   │   │   ├── api-client.ts           # Axios with JWT interceptor
│   │   │   ├── signalr-connection.ts   # SignalR WebSocket client
│   │   │   ├── market-api.ts           # HTTP client for stocks, OHLCV
│   │   │   ├── watchlist-api.ts        # HTTP client for watchlist (Phase 3)
│   │   │   ├── portfolio-api.ts        # HTTP client for portfolio (Phase 3)
│   │   │   └── alerts-api.ts           # HTTP client for alerts (Phase 3)
│   │   │
│   │   ├── components/
│   │   │   ├── price-board/            # Virtualized stock price grid
│   │   │   ├── chart/                  # TradingView chart wrapper
│   │   │   ├── watchlist-panel/        # Watchlist UI with real-time prices (Phase 3)
│   │   │   ├── portfolio-panel/        # Portfolio + P&L table (Phase 3)
│   │   │   ├── alerts-panel/           # Price alert management (Phase 3)
│   │   │   └── (shadcn/ui + custom UI)
│   │   │
│   │   └── lib/
│   │       └── (Utilities, constants)
│   │
│   ├── vite.config.ts                  # Vite configuration
│   ├── tsconfig.json                   # TypeScript settings
│   ├── package.json                    # Dependencies
│   ├── vitest.config.ts                # Test configuration
│   └── Dockerfile                      # Multi-stage build
│
├── services/vnstock-service/           # Python FastAPI Data Service
│   ├── main.py                         # FastAPI app entry point
│   ├── config.py                       # Environment configuration
│   ├── market_data_fetcher.py          # TCBS polling logic
│   ├── tick_normalizer.py              # Raw tick → standardized format
│   ├── redis_publisher.py              # Pub/sub to Redis channels
│   ├── postgres_writer.py              # Batch write to PostgreSQL
│   ├── requirements.txt                # Python dependencies (fastapi, vnstock, asyncpg, redis)
│   ├── Dockerfile                      # Python 3.11-slim image
│   ├── tests/
│   │   ├── test_tick_normalizer.py     # Normalization unit tests
│   │   ├── test_redis_publisher.py     # Redis pub/sub tests
│   │   └── test_postgres_writer.py     # Database write tests
│   └── .env.example                    # Service environment variables
│
├── db/
│   └── init.sql                        # PostgreSQL schema + seed data
│
├── nginx/
│   └── nginx.conf                      # Reverse proxy configuration
│
├── docs/                               # Project Documentation
│   ├── project-overview-pdr.md         # High-level project overview & PDR
│   ├── code-standards.md               # Code standards & architecture patterns
│   ├── codebase-summary.md             # This file
│   ├── system-architecture.md          # Detailed system architecture
│   ├── development-roadmap.md          # Phase roadmap & timeline
│   └── project-changelog.md            # Version history & changes
│
└── plans/                              # Implementation Plans
    └── 260308-2208-vnstock-platform/   # Phase 1 detailed plan
        ├── phase-01-foundation/
        │   ├── phase.md
        │   ├── task-01-docker-setup.md
        │   ├── task-02-dotnet-auth.md
        │   ├── task-03-python-service.md
        │   └── task-04-react-frontend.md
        └── reports/
            └── (Research and implementation reports)
```

---

## Technology Stack

### Backend — .NET 8 (Clean Architecture)

| Layer | Technology | Purpose |
|-------|-----------|---------|
| **API** | ASP.NET Core 8 | HTTP/REST endpoints, SignalR hubs |
| **Authentication** | ASP.NET Identity + JWT | User registration, login, token management |
| **ORM** | Entity Framework Core | Database queries and schema management |
| **Cache/PubSub** | StackExchange.Redis | In-memory caching, real-time message delivery |
| **Validation** | FluentValidation | Request validation, business logic |
| **Logging** | Serilog (ready for Phase 2) | Structured logging |

**Architecture Pattern:** Clean Architecture (4-project layout)
- **Domain:** Core entities, business rules
- **Application:** Services, DTOs, business logic
- **Infrastructure:** Database, external services, DI
- **API:** Controllers, middleware, startup

### Frontend — React 18 + Vite

| Technology | Purpose |
|-----------|---------|
| **React 18 + TypeScript** | Component library, type safety |
| **Vite** | Fast build tool, HMR development |
| **React Router v6** | Client-side routing, protected routes |
| **Zustand** | Lightweight state management with persistence |
| **Axios** | HTTP client with JWT interceptor |
| **TanStack Query** | Server state management, caching |
| **TailwindCSS v4** | Utility-first styling |
| **shadcn/ui** | Component library (Radix UI based) |
| **Vitest** | Unit testing framework |

### Data Service — Python 3.11 + FastAPI

| Technology | Purpose |
|-----------|---------|
| **FastAPI** | Async web framework |
| **vnstock 0.3.0** | TCBS market data library |
| **asyncpg** | Async PostgreSQL driver |
| **aioredis** | Async Redis client |
| **pytest** | Unit testing |

### Infrastructure

| Technology | Role |
|-----------|------|
| **PostgreSQL 16** | Primary data store (partitioned time-series) |
| **Redis 7** | Cache, pub/sub, rate limiting |
| **Nginx** | Reverse proxy, static file serving |
| **Docker Compose** | Service orchestration |

---

## Core Entities & Data Model

### Authentication (Implemented ✓)

```
ApplicationUser (IdentityUser<Guid>)
  ├── Id: Guid
  ├── Email, UserName, PasswordHash
  ├── RefreshTokens: List<RefreshToken>
  ├── Watchlists: List<WatchlistItem>
  ├── Portfolios: List<Portfolio>
  └── PriceAlerts: List<PriceAlert>

RefreshToken
  ├── Id: Guid
  ├── UserId: Guid (FK)
  ├── Token: string
  ├── ExpiresAt: DateTime
  ├── IsRevoked: bool
  └── RevokedAt: DateTime?
```

### Market Data (Phase 2 ✓)

```
Stock
  ├── Symbol: string (PK, e.g., "VCB")
  ├── Name: string
  ├── Exchange: string ("HOSE" | "HNX" | "UPCOM")
  ├── Sector: string
  └── OhlcvHistory: List<OhlcvDaily>

OhlcvDaily
  ├── Id: int
  ├── Symbol: string (FK)
  ├── Date: DateOnly
  ├── Open, High, Low, Close: decimal
  ├── Volume: long
  └── Index: (symbol, date DESC)

Tick (Partitioned by month)
  ├── Symbol: string (FK)
  ├── Timestamp: DateTime
  ├── Price: Decimal(12,2)
  ├── Volume: BigInt
  └── ChangePct: Decimal(8,4)
```

### User Features (Schema Ready for Phase 2-3)

```
WatchlistItem
  ├── Id: Guid (PK)
  ├── UserId: Guid (FK, unique with Symbol)
  ├── Symbol: string (FK)
  └── AddedAt: DateTime

Portfolio
  ├── Id: Guid (PK)
  ├── UserId: Guid (FK)
  ├── Name: string
  ├── Transactions: List<Transaction>
  └── CreatedAt: DateTime

Transaction
  ├── Id: Guid (PK)
  ├── PortfolioId: Guid (FK)
  ├── Symbol: string (FK)
  ├── Type: enum (Buy, Sell, Dividend)
  ├── Quantity: Decimal
  ├── Price: Decimal(12,2)
  ├── Cost: Decimal (calculated)
  └── ExecutedAt: DateTime

PriceAlert
  ├── Id: Guid (PK)
  ├── UserId: Guid (FK)
  ├── Symbol: string (FK)
  ├── TargetPrice: Decimal(12,2)
  ├── Condition: enum (Above, Below)
  ├── IsActive: bool
  └── CreatedAt: DateTime
```

---

## API Endpoints (Phase 1-3)

### Authentication Routes (Phase 1 ✓)

| Method | Endpoint | Request | Response | Status |
|--------|----------|---------|----------|--------|
| **POST** | `/api/auth/register` | `{ email, password, confirmPassword }` | `{ accessToken, refreshToken }` | ✓ |
| **POST** | `/api/auth/login` | `{ email, password }` | `{ accessToken, refreshToken }` | ✓ |
| **POST** | `/api/auth/refresh` | `{ refreshToken }` | `{ accessToken }` | ✓ |
| **POST** | `/api/auth/logout` | `{}` | `{ success }` | ✓ |
| **GET** | `/api/auth/me` | (requires JWT) | `{ userId, email, createdAt }` | ✓ |

### Market Data Routes (Phase 2 ✓)

| Method | Endpoint | Query Params | Response | Cache |
|--------|----------|--------------|----------|-------|
| **GET** | `/api/stocks` | `exchange`, `q`, `sector` | `[StockDto]` | 5 min |
| **GET** | `/api/stocks/{symbol}` | — | `StockDto` | None |
| **GET** | `/api/stocks/{symbol}/ohlcv` | `from`, `to` (DateOnly) | `[OhlcvDto]` | None |
| **GET** | `/api/stocks/sectors` | — | `[string]` | 1 hour |

### WebSocket (SignalR) — Phase 2 ✓

| Endpoint | Auth | Method | Purpose |
|----------|------|--------|---------|
| `/hubs/market` | JWT | SubscribeSymbol | Subscribe to real-time ticks |
| `/hubs/market` | JWT | UnsubscribeSymbol | Unsubscribe from symbol |

### Watchlist Routes (Phase 3 ✓)

| Method | Endpoint | Request/Query | Response | Auth |
|--------|----------|---------------|----------|------|
| **GET** | `/api/watchlist` | — | `[WatchlistItemDto]` | JWT |
| **POST** | `/api/watchlist` | `{ symbol }` | `WatchlistItemDto` | JWT |
| **DELETE** | `/api/watchlist/{symbol}` | — | `{ success }` | JWT |

### Portfolio Routes (Phase 3 ✓)

| Method | Endpoint | Request/Query | Response | Auth |
|--------|----------|---------------|----------|------|
| **GET** | `/api/portfolios` | — | `[PortfolioDto]` | JWT |
| **POST** | `/api/portfolios` | `{ name }` | `PortfolioDto` | JWT |
| **GET** | `/api/portfolios/{portfolioId}` | — | `PortfolioSummaryDto` | JWT |
| **POST** | `/api/portfolios/{portfolioId}/transactions` | `{ symbol, type, qty, price }` | `TransactionDto` | JWT |
| **GET** | `/api/portfolios/{portfolioId}/transactions` | — | `[TransactionDto]` | JWT |
| **DELETE** | `/api/portfolios/{portfolioId}/transactions/{txnId}` | — | `{ success }` | JWT |

### Price Alerts Routes (Phase 3 ✓)

| Method | Endpoint | Request/Query | Response | Auth |
|--------|----------|---------------|----------|------|
| **GET** | `/api/alerts` | `isActive` (optional) | `[PriceAlertDto]` | JWT |
| **POST** | `/api/alerts` | `{ symbol, targetPrice, condition }` | `PriceAlertDto` | JWT |
| **PUT** | `/api/alerts/{alertId}` | `{ isActive }` | `PriceAlertDto` | JWT |
| **DELETE** | `/api/alerts/{alertId}` | — | `{ success }` | JWT |

**Security:**
- JWT: 15-minute access token (HS256)
- Refresh: 7-day refresh token with rotation
- Cookies: HttpOnly, Secure, SameSite=Strict
- Password: bcrypt hashing via ASP.NET Identity
- Rate Limiting: 5 attempts/minute/IP on login
- SignalR: JWT auth on connection, max 50 symbols/connection

---

## Service Architecture

### .NET API Service

**Startup Configuration** (Program.cs)
- Entity Framework Core with PostgreSQL
- ASP.NET Identity (custom User entity)
- JWT Bearer authentication
- CORS for frontend (localhost:5173 in dev)
- Swagger/OpenAPI documentation
- Health check endpoints

**Controller Layer**
- `AuthController` — Register, login, refresh, logout, me
- Error handling with ProblemDetails middleware

**Middleware Stack**
- JWT validation middleware
- CORS middleware
- Exception handling (ProblemDetails format)

### Python Data Service

**Architecture**
- FastAPI app with async/await throughout
- TCBS market data polling (3-second intervals)
- Redis pub/sub publisher (`ticks:{symbol}` channels)
- PostgreSQL batch writer (5-second flush intervals)
- Exponential backoff reconnect on failures

**Health Check**
- GET `/health` → `{ status: "healthy" }`

**Data Flow**
1. Fetch latest tick from TCBS via vnstock
2. Normalize to standardized TickData format
3. Publish to Redis channel (real-time subscribers)
4. Batch write to PostgreSQL ticks table (5s flush)

### React Frontend

**State Management**
- Zustand auth store (email, token, isAuthenticated)
- Persisted to localStorage (survives page refresh)
- TanStack Query for server-state caching

**Routing**
- React Router v6 with ProtectedRoute wrapper
- Public: `/login`, `/register`
- Protected: `/dashboard`, `/market`
- Redirect: Unauthorized → `/login`

**HTTP Client**
- Axios interceptor adds JWT to requests
- Auto-refresh on 401 response
- Error handling with user feedback

### Nginx Reverse Proxy

**Routing**
- `/api/*` → .NET API (port 5000)
- `/hubs/*` → SignalR WebSocket (port 5000)
- `/` → React SPA (static files)

**Features**
- Upgrade header for WebSocket
- X-Real-IP and X-Forwarded-For headers
- SPA fallback to index.html

---

## Development Environment

### Local Setup

```bash
# Prerequisites
Docker, Docker Compose, Git, .NET 8, Node.js 20+, Python 3.11

# Clone & configure
git clone <repo>
cd fintech
cp .env.example .env
# Edit .env with your values

# Start all services
make up

# Access services
http://localhost:3000      # Frontend
http://localhost:5000      # API docs (Swagger)
http://localhost:8000      # Python service (health check)
http://localhost:6379      # Redis (direct)
http://localhost:5432      # PostgreSQL (direct)
```

### Makefile Shortcuts

| Command | Purpose |
|---------|---------|
| `make up` | Start all services |
| `make down` | Stop all services |
| `make logs` | View all service logs |
| `make ps` | List running containers |
| `make build` | Rebuild Docker images |
| `make db-shell` | Connect to PostgreSQL |
| `make redis-shell` | Connect to Redis CLI |
| `make clean` | Remove volumes & orphans |
| `make api-logs` | API service logs only |
| `make vnstock-logs` | Data service logs only |

### Environment Variables

**Required** (copy from .env.example)
- `POSTGRES_DB`, `POSTGRES_USER`, `POSTGRES_PASSWORD`
- `JWT_SECRET` (minimum 32 characters)
- `REDIS_URL` (redis://redis:6379)
- `ASPNETCORE_ENVIRONMENT` (Development/Production)

---

## Testing

### Unit Tests Summary

| Service | Framework | Tests | Status |
|---------|-----------|-------|--------|
| **.NET Auth** | xUnit | Auth flow tests | ✓ Pass |
| **Python Service** | pytest | 11 tests | ✓ 11/11 Pass |
| **React Frontend** | Vitest | 4 tests | ✓ 4/4 Pass |

**Coverage Areas**
- Authentication (register, login, refresh, logout)
- Token generation and validation
- Tick normalization and data transformation
- Redis pub/sub message publishing
- PostgreSQL batch write logic
- Protected route guards
- Auth store persistence

### Running Tests

```bash
# .NET backend
cd src && dotnet test

# Python data service
cd services/vnstock-service && pytest tests/

# React frontend
cd client && npm test
```

### Docker Integration Tests

```bash
# Start all services (health checks enabled)
make up
make ps  # All services should be healthy

# Test auth flow (from frontend browser)
# 1. Register: http://localhost:3000/register
# 2. Login: http://localhost:3000/login
# 3. Dashboard: http://localhost:3000/dashboard (protected)
```

---

## Database Schema

### Partitioning Strategy

**Ticks Table** (Monthly partitions)
- Partition 1: 2026-03-01 to 2026-04-01
- Partition 2: 2026-04-01 to 2026-05-01
- Partition 3: 2026-05-01 to 2026-06-01
- New partitions created automatically by PostgreSQL

**Rationale**
- Ticks accumulate ~1GB/month per 3000 symbols
- Monthly partitions enable fast purging of old data
- Query performance remains constant as table grows

### Indexes

| Table | Index | Purpose |
|-------|-------|---------|
| **ticks** | `(symbol, timestamp DESC)` | Queries by symbol over time range |
| **ohlcv_daily** | `(symbol, date DESC)` | Daily candle lookups |
| **watchlists** | `(user_id, symbol)` unique | Prevent duplicates |
| **portfolios** | `(user_id)` | User's portfolio collections |
| **price_alerts** | `(user_id, is_active)` partial | Active alerts only |

### Seed Data

15 Vietnamese stocks pre-loaded (banking, real estate, retail, technology, oil & gas):
- VCB, VIC, VHM, HPG, BID, CTG, MBB, TCB, ACB, VPB, FPT, MWG, GAS, SAB, PLX

---

## Security Considerations

### Authentication & Authorization

- **Password Storage:** bcrypt (ASP.NET Identity default)
- **JWT Claims:** UserId, Email, IssuedAt, ExpiresAt
- **Token Lifetime:** 15 min (access), 7 days (refresh)
- **Token Refresh:** Rotation strategy (old tokens invalidated)
- **Refresh Token Storage:** HttpOnly cookie (secure by default)

### API Security

- **CORS:** Configured for localhost:5173 (dev only)
- **Rate Limiting:** 5 login attempts/minute/IP
- **SQL Injection:** Prevented via EF Core parameterized queries
- **CSRF:** SameSite cookie attribute (Strict)

### Data Protection

- **Environment Variables:** All secrets (JWT_SECRET) in .env
- **No Hardcoding:** No credentials in source code
- **Database User:** Limited permissions (no DROP TABLE)

### Future Security (Phase 4)

- HTTPS/TLS in production
- API key authentication for data service
- Audit logging (Serilog)
- OWASP compliance checks

---

## Performance Characteristics

### Database

- **Tick Write Latency:** ~50ms (batch of 100)
- **Query Latency:** <10ms (indexed lookups)
- **Partition Pruning:** Only relevant month scanned
- **Connection Pooling:** 20 default (configurable)

### API Response Times

- **Auth Endpoints:** <100ms (token generation)
- **Data Retrieval:** <50ms (indexed queries)
- **Real-time Updates:** <500ms (Redis pub/sub)

### Frontend

- **Initial Load:** ~2s (React bundle + assets)
- **Token Refresh:** <100ms (background)
- **Route Transitions:** <500ms (code splitting)

### Scalability

- **Horizontal:** Services can be replicated (Nginx load balancing)
- **Vertical:** PostgreSQL tuning for larger data volumes
- **Redis:** Single instance (scale to cluster in Phase 4)

---

## Deployment & Docker

### Image Sizes (Optimized)

| Service | Base Image | Size | Optimization |
|---------|-----------|------|--------------|
| **.NET API** | aspnet:8.0 | ~200MB | Multi-stage build |
| **Python Service** | python:3.11-slim | ~150MB | Minimal dependencies |
| **Nginx** | nginx:alpine | ~40MB | Static files only |

### Health Checks

All services have health checks configured in docker-compose.yml:

```
PostgreSQL: pg_isready -U user -d db
Redis: redis-cli ping
.NET API: GET /health
Python Service: GET /health
```

### Container Dependencies

- `api` depends on `postgres` and `redis` (healthy)
- `vnstock-service` depends on `postgres` and `redis` (healthy)
- `nginx` depends on `api` (started)

---

## Code Quality Standards

### .NET Backend

- **Architecture:** Clean Architecture enforced via project structure
- **Naming:** PascalCase for classes, properties; camelCase for parameters
- **Error Handling:** try-catch with structured logging
- **Async/Await:** Throughout service layer
- **Validation:** FluentValidation for input DTOs

### Python Data Service

- **Style:** PEP 8 with 88-character line limit (Black)
- **Type Hints:** Full type annotations on functions
- **Async:** Full async/await with proper exception handling
- **Logging:** Structured logging with context
- **Testing:** Unit tests with 80%+ coverage

### React Frontend

- **Components:** Functional components with hooks only
- **State:** Zustand for auth, TanStack Query for server state
- **Styling:** TailwindCSS utility classes
- **TypeScript:** Strict mode, no `any` types
- **Testing:** Vitest + @testing-library/react

---

## Completed Features by Phase

### Phase 1 ✓
- Docker Compose orchestration
- JWT authentication + refresh token rotation
- PostgreSQL with table partitioning
- Python TCBS market data polling
- React SPA with routing
- Zustand state management

### Phase 2 ✓
- SignalR Hub with JWT auth and Redis backplane
- Real-time price board (virtualized for 3000+ symbols)
- OHLCV REST API with date range queries
- TradingView Lightweight Charts v5 integration
- cmdk symbol search with sector filtering
- Stock and OhlcvDaily entities with proper indexing

### Phase 3 ✓
- Watchlist CRUD API with real-time SignalR updates
- Portfolio + Transaction management (add BUY/SELL transactions)
- P&L Engine (weighted-average cost basis, realized + unrealized P&L)
- Price Alert system with ABOVE/BELOW conditions
- AlertEngineService (background service monitoring Redis ticks, fires email alerts)
- SmtpEmailService (MailKit SMTP integration for notifications)
- Dashboard with 3-panel layout (watchlist, portfolio, alerts)

## Known Limitations & Future Work

### Current Limitations

- Single Python service instance (no redundancy)
- No monitoring/alerting (Phase 4)
- Limited to Vietnam exchanges (Phase 5: International)

### Planned Improvements

| Phase | Feature |
|-------|---------|
| **Phase 3** | Watchlist, Portfolio, P&L engine, Price alerts, Dashboard |
| **Phase 4** | Mobile responsive UI, Performance optimization, CI/CD, Logging |
| **Phase 5** | International exchanges, Multi-currency support |

---

## Getting Help

1. **Setup Issues:** Check `README.md` quickstart section
2. **API Documentation:** http://localhost:5000/swagger (when running)
3. **Database Schema:** Review `db/init.sql`
4. **Architecture:** See `docs/system-architecture.md`
5. **Development Workflow:** Read `.claude/rules/development-rules.md`

---

## Contributors

- Backend Development (.NET 8, Clean Architecture)
- Frontend Development (React 18, TypeScript)
- Data Service (Python FastAPI, TCBS integration)
- DevOps & Infrastructure (Docker, PostgreSQL, Redis)

---

**Last Updated:** 2026-03-14 | **Status:** Phase 3 Complete — MVP (v1.0.0-mvp)
