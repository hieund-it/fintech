# VnStock Platform — Development Roadmap

**Last Updated:** 2026-03-13

---

## Executive Summary

VnStock is a full-stack Vietnamese stock market analytics and portfolio management platform. Built on .NET 8 (backend), Python (data service), and React 18 (frontend), with PostgreSQL and Redis infrastructure.

**Current Status:** Phase 2 Core Features COMPLETE ✓
**Overall Progress:** 40% (Phase 1-2 of 5 complete)
**Target MVP:** End of Phase 3 (v1.0.0 expected Q2 2026)

---

## Phase Overview

| Phase | Name | Status | Progress | Target End | Key Deliverables |
|-------|------|--------|----------|------------|------------------|
| **1** | Foundation | ✓ COMPLETE | 100% | 2026-03-09 | Docker, .NET Auth, Python Data Service, React Skeleton |
| **2** | Core Features | ✓ COMPLETE | 100% | 2026-03-13 | SignalR Hub, Real-time Price Board, OHLCV API, Market Data |
| **3** | User Features | ⏳ In Progress | 0% | 2026-05-11 | Watchlist, Portfolio, P&L, Alerts, Dashboard |
| **4** | Polish & Production | ⏳ Pending | 0% | 2026-06-01 | Mobile UI, Performance, Logging, CI/CD |
| **5** | International (Post-MVP) | 🔭 Future | 0% | TBD | Multi-market Support, International Exchanges |

---

## Phase 1: Foundation — COMPLETE ✓

**Status:** ✓ COMPLETE (2026-03-09)
**Effort:** 3-4 weeks (completed on schedule)
**Progress:** 100% (5/5 tasks)

### Completed Tasks

#### Task 01: Docker Compose Setup ✓
- **Deliverables:**
  - `docker-compose.yml` with 5 services: postgres:16, redis:7, nginx, .NET API, Python data service
  - `docker-compose.override.yml` for dev environment (hot-reload volumes)
  - `.env.example` with all required environment variables
  - `Makefile` with shortcuts: `make up`, `make down`, `make logs`, `make db-shell`
  - `nginx/nginx.conf` with reverse proxy rules
  - `db/init.sql` with complete schema + partitions
- **Status:** ✓ Complete
- **Tests:** All containers healthy on `docker compose up`

#### Task 02: .NET 8 Web API + ASP.NET Identity + JWT Auth ✓
- **Deliverables:**
  - VnStock.sln with 4-project Clean Architecture:
    - VnStock.Domain (entities, interfaces)
    - VnStock.Application (services, DTOs, commands)
    - VnStock.Infrastructure (EF Core, Redis, DI)
    - VnStock.API (controllers, hubs, middleware)
  - AuthService with register, login, logout, refresh logic
  - TokenService generating JWT (15min) + refresh tokens (7days)
  - EF Core initial migration for Identity tables
- **Status:** ✓ Complete
- **Endpoints Implemented:**
  - POST `/api/auth/register` → 200 with JWT + HttpOnly cookie
  - POST `/api/auth/login` → 200 with JWT + refresh token
  - POST `/api/auth/refresh` → new access token
  - POST `/api/auth/logout` → clear refresh token
  - GET `/api/auth/me` → current user (protected)

#### Task 02b: Domain Entities + Full DB Schema ✓
- **Deliverables:**
  - Domain entities: WatchlistItem, Portfolio, Transaction, PriceAlert
  - EF Core migration: 20260309094700_InitialAuth
  - Full AppDbContext configuration with constraints, indexes, enums
  - PostgreSQL schema with all Phase 2-3 tables ready
- **Status:** ✓ Complete
- **Schema Created:**
  - Watchlists (with unique UserId+Symbol index)
  - Portfolios (user-owned collections)
  - Transactions (with decimal precision for financial values)
  - PriceAlerts (with partial index for active alerts)
  - Foreign keys with cascade delete for data integrity

#### Task 03: Python FastAPI vnstock Service ✓
- **Deliverables:**
  - FastAPI microservice with health endpoint
  - TCBS market data polling (3-second intervals)
  - Tick normalization (raw TCBS → standardized TickData)
  - Redis pub/sub publisher (channel: `ticks:{symbol}`)
  - PostgreSQL batch writer (5-second flush intervals)
  - Async/await architecture with exponential backoff reconnect
  - Docker image: python:3.11-slim
- **Status:** ✓ Complete
- **Tests:** 11/11 unit tests pass
  - Tick normalization validation
  - Redis publisher mock tests
  - PostgreSQL batch write tests
- **Data Flow:**
  - TCBS polling → normalize ticks → publish to Redis → batch write to PostgreSQL

#### Task 04: React 18 + Vite Frontend Skeleton ✓
- **Deliverables:**
  - React 18 + TypeScript + Vite build
  - React Router v6 with protected routes
  - Zustand auth store with persistence
  - Axios API client with JWT interceptor + auto-refresh
  - TanStack Query (React Query) setup
  - TailwindCSS v4 + shadcn/ui components
  - SignalR stub connection (ready for Phase 2)
  - Auth pages: login, register, dashboard, market
- **Status:** ✓ Complete
- **Tests:** 4/4 unit tests pass
  - Auth store (login, logout, register flows)
  - Protected route guard logic
  - JWT token persistence
- **Build:** Zero TypeScript errors, production build succeeds

### Key Metrics

- **Code Quality:**
  - Unit test coverage: Python 11/11, React 4/4
  - TypeScript: Zero errors
  - .NET: Clean Architecture patterns enforced
- **Infrastructure:**
  - 5 containerized services
  - PostgreSQL with table partitioning (ready for high-volume tick data)
  - Redis for real-time pub/sub
- **Security:**
  - JWT-based auth with refresh token rotation
  - HttpOnly, Secure, SameSite cookies
  - ASP.NET Identity with bcrypt hashing
  - Environment variables for secrets (no hardcoding)

### Integration Test Results

- ✓ All containers start and pass health checks
- ✓ Auth endpoints return correct JWT + refresh tokens
- ✓ Python service connects to TCBS, publishes to Redis
- ✓ PostgreSQL persists tick data
- ✓ React app loads, auth flow works end-to-end
- ✓ Protected routes enforce authentication

---

## Phase 2: Core Features — COMPLETE ✓

**Completed:** 2026-03-13
**Estimated Effort:** 1-2 weeks (accelerated delivery)
**Progress:** 100% (5/5 tasks completed)

### Completed Tasks

| # | Task | Status |
|---|------|--------|
| 05 | SignalR Hub + Redis backplane | ✓ Complete |
| 06 | Real-time price board (virtualized) | ✓ Complete |
| 07 | OHLCV historical data API | ✓ Complete |
| 08 | TradingView chart + stock detail | ✓ Complete |
| 09 | Symbol search + sector filter | ✓ Complete |

### Implementation Summary

**SignalR Real-Time Hub (Task 05)**
- MarketHub with JWT authentication
- Symbol subscription management (max 50 per connection)
- Redis backplane for multi-instance broadcasting
- Group-based message delivery to subscribed clients
- Sub-100ms latency from Python service to browser

**Price Board Component (Task 06)**
- TanStack Virtual for 3000+ symbol virtualization
- Flash animations on price updates
- Exchange filter (HOSE, HNX, UPCOM)
- Real-time Zustand store (Map<symbol, TickData>)
- Responsive grid layout

**OHLCV API & Market Data (Task 07)**
- `GET /api/stocks` — List stocks with search, exchange, sector filters (cached 5min)
- `GET /api/stocks/{symbol}` — Stock metadata
- `GET /api/stocks/{symbol}/ohlcv?from=...&to=...` — Daily OHLCV bars
- `GET /api/stocks/sectors` — All distinct sectors (cached 1hr)
- Stock and OhlcvDaily entities with proper indexing

**Chart Integration (Task 08)**
- TradingView Lightweight Charts v5 integration
- Candlestick + volume rendering
- MA20/MA50 moving averages
- Responsive chart container
- Date range selection for OHLCV data

**Symbol Search (Task 09)**
- cmdk command palette integration
- 300ms debounce for search input
- Full-text stock search (symbol, name)
- Sector filtering dropdown
- Real-time market data sync

### Deliverables

**Backend**
- MarketHub.cs (SignalR real-time communication)
- StocksController.cs (REST API endpoints)
- MarketDataService.cs (business logic)
- Stock.cs entity (market metadata)
- OhlcvDaily.cs entity (OHLCV bars)
- Migration: AddMarketTables (schema update)
- RedisMarketDataSubscriber (pub/sub bridge)

**Frontend**
- market-api.ts (HTTP service layer)
- signalr-connection.ts (WebSocket client)
- market-store.ts (Zustand real-time store)
- price-board component (virtualized grid)
- chart component wrapper (TradingView)
- market-page.tsx (main market view)

**Database**
- Stock table (15 pre-seeded Vietnamese stocks)
- OhlcvDaily table with (symbol, date DESC) index
- Foreign key constraints and cascading deletes

### Success Criteria Met

✓ Real-time price updates via SignalR WebSocket (<100ms latency)
✓ Virtualized price board for 3000+ symbols
✓ Daily OHLCV candlestick data with date range queries
✓ TradingView Lightweight Charts v5 fully integrated
✓ cmdk search + sector filtering implemented

### Build Quality

- **.NET:** 0 compilation errors, Clean Architecture maintained
- **React:** 0 TypeScript errors, production build successful
- **Tests:** 24 unit tests passing (all Phase 1-2 tests)
- **Performance:** Virtualization renders <1s for 3000 symbols

---

## Phase 3: User Features — IN PROGRESS

**Target Start:** 2026-03-14
**Target End:** 2026-05-11
**Estimated Effort:** 4-6 weeks
**Progress:** 0% (0/5 tasks started, queue ready)

### Overview

User-specific features: watchlists, portfolio tracking, P&L calculations, price alerts, and dashboard.

### Tasks

| # | Task | Branch | Effort | Status |
|---|------|--------|--------|--------|
| 10 | Watchlist (add/remove, real-time) | `feature/phase03-watchlist` | 2-3d | ⏳ Pending |
| 11 | Portfolio + transactions CRUD | `feature/phase03-portfolio` | 3-4d | ⏳ Pending |
| 12 | P&L calculation engine | `feature/phase03-pnl-engine` | 2-3d | ⏳ Pending |
| 13 | Price alerts (background service) | `feature/phase03-price-alerts` | 3-4d | ⏳ Pending |
| 14 | Dashboard (portfolio + watchlist) | `feature/phase03-dashboard` | 2-3d | ⏳ Pending |

### Success Criteria

- Add/remove stocks to watchlist in real-time
- Create multiple portfolios with transactions
- Calculate P&L, cost basis, performance metrics
- Trigger price alerts via email/in-app notifications
- Dashboard shows portfolio overview + watchlist ticker

---

## Phase 4: Polish & Production — PENDING

**Target Start:** 2026-05-12
**Target End:** 2026-06-01
**Estimated Effort:** 2-3 weeks
**Progress:** 0% (0/4 tasks started)

### Overview

Performance optimization, responsive design, monitoring, and CI/CD automation.

### Tasks

| # | Task | Branch | Effort | Status |
|---|------|--------|--------|--------|
| 15 | Mobile responsive UI | `feature/phase04-mobile-ui` | 2-3d | ⏳ Pending |
| 16 | Performance optimization | `feature/phase04-performance` | 2-3d | ⏳ Pending |
| 17 | Logging + error handling | `feature/phase04-logging` | 1-2d | ⏳ Pending |
| 18 | CI/CD pipeline (GitHub Actions) | `feature/phase04-cicd` | 2-3d | ⏳ Pending |

### Success Criteria

- Responsive design: mobile, tablet, desktop
- Lighthouse score: 90+
- Real-time data sync within 1 second
- Structured logging (Serilog)
- Automated testing + deployment pipeline

---

## Phase 5: International — Future

**Status:** 🔭 Future | **Effort:** TBD | **Timeline:** Post-MVP

### Overview

Expand to international markets and multi-exchange support.

### Planned Features

- Support for US, Singapore, HK exchanges
- IMarketDataProvider abstraction (pluggable data sources)
- Multi-currency portfolio tracking
- International dividend tracking

---

## Dependencies & Critical Path

```
Phase 1 (Foundation)
    ↓
Phase 2 (Core Features) — requires auth + infrastructure
    ↓
Phase 3 (User Features) — requires real-time prices + charting
    ↓
Phase 4 (Polish) — requires all features implemented
    ↓
Phase 5 (International) — independent, post-MVP
```

**Critical Path:** Phase 1 → Phase 2 → Phase 3 → Phase 4
**Time to MVP (Phase 3 complete):** ~14 weeks from start

---

## Resource Allocation

| Role | Allocation | Responsibility |
|------|-----------|-----------------|
| Fullstack Developer | 1 FTE | .NET API, React frontend |
| Backend Developer | 0.5 FTE | Python data service, database optimization |
| DevOps | 0.25 FTE | Docker, Nginx, GitHub Actions, monitoring |

---

## Version Timeline

| Version | Phase | Target | Status |
|---------|-------|--------|--------|
| v0.1.0-foundation | 1 | 2026-03-09 | ✓ RELEASED |
| v0.2.0-core | 1-2 | 2026-04-06 | ⏳ In Planning |
| v1.0.0-mvp | 1-3 | 2026-05-11 | ⏳ Planned |
| v1.1.0-prod | 4 | 2026-06-01 | ⏳ Planned |
| v2.0.0-intl | 5 | TBD | 🔭 Future |

---

## Known Issues & Unresolved Questions

### Phase 1
- ✓ All resolved

### Phase 2 (Upcoming)
- TCBS WebSocket stability needs real-world testing
- Symbol count (starting with 30, scale to 3000+?)
- OHLCV historical data retention policy (5 years? 10 years?)

### General
- SLA targets for real-time data (sub-second? target?)
- Mobile app vs responsive web (web-first approach decided)
- Localization strategy (Vietnamese first, English later)

---

## Success Metrics

### Technical KPIs
- Unit test coverage: >80%
- Build time: <3 minutes
- API response time: <100ms (p99)
- Real-time data latency: <1s
- Uptime: 99.5%

### Business KPIs
- User registrations: N/A (internal project)
- Feature completeness: Phase 3 end = MVP
- Performance: Mobile-friendly Lighthouse 90+
- Data accuracy: 100% tick data fidelity

---

## Next Steps

### Immediate (Week of 2026-03-10)
1. ✓ Phase 1 sync-back and documentation complete
2. Schedule Phase 2 kickoff meeting
3. Finalize SignalR Hub architecture
4. Review price board requirements (virtualization library selection)

### Short Term (March 2026)
1. Begin Phase 2 implementation
2. Set up GitHub Actions CI/CD skeleton
3. Establish monitoring/alerting for production infrastructure

### Medium Term (Q2 2026)
1. Complete Phase 3 (user features)
2. Load testing for real-time price feeds
3. MVP release (v1.0.0)

---

## Document History

| Date | Author | Changes |
|------|--------|---------|
| 2026-03-13 | Documentation Manager | Phase 2 completion: SignalR, OHLCV API, Price Board, Charts, Search |
| 2026-03-09 | Project Manager | Created roadmap; Phase 1 completion sync-back |
