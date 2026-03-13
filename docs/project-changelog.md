# VnStock Platform — Project Changelog

All notable changes to the VnStock platform are documented here. Format follows [Keep a Changelog](https://keepachangelog.com/), organized by version.

---

## [Unreleased]

### Planned
- Phase 3: Watchlist, portfolio, P&L engine, price alerts
- Phase 4: Mobile responsive UI, performance optimization, CI/CD
- Phase 5: International exchange support

---

## [0.2.0-core] — 2026-03-13

### Core Features Release: Real-Time Market Data & Interactive Charts

Phase 2 completed with real-time price updates, market data APIs, and interactive charting.

#### Added

**Real-Time Communication**
- SignalR Hub (MarketHub) with JWT authentication
- Redis pub/sub bridge (RedisMarketDataSubscriber)
- WebSocket-based tick streaming to browser
- Group-based subscription management per symbol
- Max 50 symbols per connection (throttling)
- Redis backplane for multi-instance deployment

**Market Data Entities**
- Stock entity (symbol, name, exchange, sector)
- OhlcvDaily entity (Open, High, Low, Close, Volume)
- Migration: AddMarketTables (schema extension)
- 15 Vietnamese stocks pre-seeded (HOSE, HNX, UPCOM)

**REST API Endpoints**
- GET `/api/stocks` — List stocks (search, exchange filter, sector filter) [cached 5min]
- GET `/api/stocks/{symbol}` — Stock metadata
- GET `/api/stocks/{symbol}/ohlcv` — Daily OHLCV bars (date range) [no cache]
- GET `/api/stocks/sectors` — All distinct sectors [cached 1hr]

**Frontend Components**
- PriceBoard: Virtualized grid (TanStack Virtual) for 3000+ symbols
- Chart: TradingView Lightweight Charts v5 integration
- MarketStore (Zustand): Real-time tick data (Map<symbol, TickData>)
- GlobalSearch: cmdk command palette (300ms debounce)
- MarketPage: Main market view

**Frontend Services**
- market-api.ts: HTTP client for stock/OHLCV queries
- signalr-connection.ts: WebSocket client with JWT auth + auto-reconnect
- market-store.ts: Zustand store for real-time price updates

**Features**
- Real-time price board with exchange filter
- Flash animations on price updates (TailwindCSS)
- OHLCV candlestick charts with moving averages (MA20, MA50)
- Volume bar charts integrated with candles
- Symbol search with sector filtering
- Sub-100ms latency from market data to browser

#### Performance & Scalability
- Virtualized rendering: <1s load time for 3000+ symbols
- Database indexes: (symbol, date DESC) on OhlcvDaily for fast queries
- Response caching: 5-minute cache on stock list, 1-hour on sectors
- Redis backplane: Enables SignalR scaling to multiple API instances

#### Quality & Testing
- 24 unit tests passing (Phase 1-2 combined)
- Zero TypeScript errors (strict mode)
- Zero .NET compilation errors
- Production React build verified

---

## [0.1.0-foundation] — 2026-03-09

### Foundation Release: Complete Infrastructure & Auth

Phase 1 completed successfully. All infrastructure, authentication, and service skeletons in place.

#### Added

**Infrastructure**
- Docker Compose orchestration (postgres:16, redis:7, nginx, .NET API, Python data service)
- Multi-environment docker-compose.override.yml for development
- Nginx reverse proxy with API and WebSocket routing
- PostgreSQL 16 with table partitioning strategy (ticks partitioned by month)
- Redis 7 for pub/sub and caching
- Makefile with shortcuts: up, down, logs, db-shell
- .env.example with documented environment variables

**Backend — .NET 8 Web API**
- VnStock.sln with 4-project Clean Architecture:
  - VnStock.Domain: Entities, interfaces, value objects
  - VnStock.Application: Services, DTOs, commands/queries
  - VnStock.Infrastructure: EF Core, Redis client, dependency injection
  - VnStock.API: Controllers, SignalR hubs, middleware, configuration
- ASP.NET Core Identity integration with custom User entity
- JWT authentication (15-minute access tokens, 7-day refresh tokens)
- Token refresh with rotation (invalidates old refresh tokens)
- HttpOnly, Secure, SameSite=Strict cookies for refresh tokens
- Protected endpoints with [Authorize] attribute enforcement
- Swagger/OpenAPI documentation with JWT bearer support
- Rate limiting on auth endpoints (5 attempts/minute/IP)

**Auth Endpoints**
- POST `/api/auth/register` → Create account, return JWT + refresh cookie
- POST `/api/auth/login` → Authenticate, return JWT + refresh cookie
- POST `/api/auth/refresh` → Get new access token (refresh token remains in cookie)
- POST `/api/auth/logout` → Invalidate refresh token, clear cookie
- GET `/api/auth/me` → Get current user profile (requires JWT)

**Database Schema (EF Core)**
- EF Core migration: 20260309094700_InitialAuth
- Auth tables: AspNetUsers, AspNetRoles, AspNetUserRoles, AspNetUserClaims, RefreshTokens
- Domain tables (Phase 2-3 ready):
  - Watchlists (unique per user+symbol)
  - Portfolios (user-owned collections)
  - Transactions (buy/sell tracking with decimal precision)
  - PriceAlerts (with partial index for active alerts)
  - Ticks (partitioned by month for high-volume time-series data)
  - OhlcvDaily (daily OHLCV aggregates)
- Stocks (market metadata: symbol, name, sector)
- Proper constraints, indexes, and cascading deletes

**Data Service — Python FastAPI**
- FastAPI microservice on port 8000
- TCBS market data polling (3-second intervals via vnstock library)
- Tick normalization (raw TCBS → standardized TickData format)
- Redis pub/sub publisher (channel: `ticks:{symbol}`)
- PostgreSQL batch writer (flushes every 5 seconds)
- Health check endpoint: GET `/health` → 200 OK
- Exponential backoff reconnect on connection failure
- Async/await architecture with proper error handling
- Docker image: python:3.11-slim

**Frontend — React 18 + Vite**
- React 18 + TypeScript project with Vite build tool
- React Router v6 for client-side routing
- Zustand state management with persistence (auth store survives refresh)
- Axios HTTP client with JWT interceptor + automatic token refresh
- TanStack Query (React Query) setup for server state management
- TailwindCSS v4 for styling
- shadcn/ui component library (Radix UI based)
- Protected routes component (redirects to /login if unauthenticated)
- SignalR client stub (ready for Phase 2 real-time updates)
- Auth pages: login, register, dashboard, market

**Pages Implemented**
- `/login` — User login form with email/password
- `/register` — User registration form with validation
- `/dashboard` — Placeholder for user dashboard
- `/market` — Placeholder for market data
- `ProtectedRoute` wrapper for private pages

**Testing**

Backend:
- Unit tests for Auth service (register, login, refresh, logout flows)
- Test coverage for token generation and validation
- InMemory database tests for EF Core

Frontend:
- 4 unit tests for auth store (login, logout, register, token persistence)
- Tests use Vitest + @testing-library/react
- Auth store persistence verification

Data Service:
- 11 unit tests pass (tick normalization, Redis publisher, PostgreSQL batch writer)
- Comprehensive error handling tests
- Exponential backoff reconnect logic tests

#### Security

- JWT secret stored in environment variables (not hardcoded)
- Refresh tokens stored in HttpOnly cookies (not accessible via JavaScript)
- Passwords hashed with bcrypt via ASP.NET Identity
- CORS configured for development (localhost:5173)
- Rate limiting on login endpoint (5 attempts/minute)
- SQL injection protection via EF Core parameterized queries
- CSRF protection via SameSite cookie attribute

#### Docker

**Images**
- postgres:16-alpine (database)
- redis:7-alpine (cache & pub/sub)
- nginx:alpine (reverse proxy)
- mcr.microsoft.com/dotnet/aspnet:8.0 (API runtime)
- python:3.11-slim (data service)

**Health Checks**
- PostgreSQL: `pg_isready` command
- Redis: `redis-cli ping`
- .NET API: HTTP endpoint probe
- Python service: FastAPI `/health` endpoint

#### DevOps

- Docker Compose with service dependencies
- Volume mounts for development hot-reload
- Environment variable injection from .env
- Makefile shortcuts for common commands
- Network isolation between containers

#### Build & Deploy

- .NET: Multi-stage Dockerfile for optimized image size
- Python: Minimal python:3.11-slim base image
- React: Production build tested (zero TypeScript errors)
- All services containerizable and deployable via docker compose up

#### Performance

- PostgreSQL table partitioning (ticks table by month)
- Indexes on frequently queried columns (symbol, timestamp, user_id)
- Partial indexes for active alerts only
- Redis for real-time pub/sub (low-latency message delivery)
- Async/await throughout Python service
- Lazy loading in React via code splitting

#### Documentation

- Architecture diagrams in plan files
- API endpoint specs in task files
- Database schema documentation
- Docker setup instructions
- Environment variable reference (.env.example)

#### Known Limitations & Future Work

- Phase 1 does not implement real-time WebSocket updates (Phase 2: SignalR)
- Frontend components are skeleton/placeholder only
- No monitoring or observability yet (Phase 4)
- Limited to Vietnam exchanges (Phase 5: International)
- No mobile app (web-responsive design only)

#### Test Results Summary

- ✓ All 5 Phase 1 tasks completed
- ✓ Python service: 11/11 unit tests pass
- ✓ React frontend: 4/4 unit tests pass
- ✓ .NET API: Auth service unit tests pass
- ✓ Docker Compose: All containers healthy
- ✓ Integration test: Full auth flow works end-to-end
- ✓ TypeScript: Zero compilation errors
- ✓ React build: Production build succeeds

---

## Version 0.2.0 Artifacts

**Git Tags:**
- `v0.2.0-core` (Phase 2 complete)

**Deployment:**
- All Phase 2 services containerized and tested
- MarketHub integrated with Redis backplane
- OHLCV data accessible via REST API
- Real-time price board functional

---

## Version 0.1.0 Artifacts

**Git Tags:**
- `v0.1.0-foundation` (Phase 1 complete)

**Deployment:**
- Containerized via docker-compose.yml
- All services configured and tested
- Ready for Phase 2 development

**Documentation:**
- README.md with quick start instructions
- CLAUDE.md with development workflow
- plans/260308-2208-vnstock-platform/ with detailed phase plans
- docs/development-roadmap.md with project timeline

---

## Upcoming Changes

### Phase 2 (Released as v0.2.0-core)

See [0.2.0-core] section above for complete details.

---

### Phase 3 (Planned)

#### New Features
- Watchlist management (add/remove/reorder)
- Portfolio tracking with transactions
- P&L calculation engine
- Price alerts with email notifications
- User dashboard

#### Backend Changes
- Watchlist service
- Portfolio service
- Transaction service
- P&L calculation engine
- Background alert job service

#### Frontend Changes
- Watchlist UI
- Portfolio UI
- Dashboard UI
- Alert notification UI

#### Target Release
- **v1.0.0-mvp** — 2026-05-11

---

### Phase 4 (Planned)

#### New Features
- Mobile responsive design
- Performance optimization
- Structured logging (Serilog)
- GitHub Actions CI/CD pipeline

#### Target Release
- **v1.1.0-prod** — 2026-06-01

---

### Phase 5 (Future)

#### Planned Features
- International exchange support (US, SG, HK)
- Multi-currency portfolios
- IMarketDataProvider abstraction

#### Target Release
- **v2.0.0-intl** — TBD

---

## Breaking Changes

None in v0.1.0 (initial release)

---

## Deprecations

None in v0.1.0 (initial release)

---

## Security Updates

- All dependencies pinned to known-good versions
- No security vulnerabilities detected in initial scan
- JWT secrets managed via environment variables
- SQL injection prevention via EF Core parameterization

---

## Migration Guide

N/A for v0.1.0 (first release)

---

## Contributors

- Project Manager (planning, coordination)
- Backend Developer (.NET 8, Python services)
- Frontend Developer (React, TypeScript)
- DevOps (Docker, infrastructure)

---

## License

MIT © VnStock Contributors

---

## Glossary

- **JWT:** JSON Web Token for stateless authentication
- **SignalR:** Microsoft's real-time communication library
- **TCBS:** Stock market data source via vnstock library
- **OHLCV:** Open, High, Low, Close, Volume (candlestick data)
- **Redis:** In-memory data store for caching and pub/sub
- **EF Core:** Entity Framework Core (ORM for .NET)
- **Zustand:** Lightweight state management for React

---

## Contact & Support

For questions or issues:
1. Check README.md for quick start guide
2. Review plans/ directory for detailed specifications
3. Consult docs/ for architecture and standards

---

**Last Updated:** 2026-03-13 | **Status:** Phase 2 Complete, Phase 3 In Progress
