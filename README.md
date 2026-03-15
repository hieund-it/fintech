# fintech

> A full-stack Vietnamese stock market analytics and portfolio management platform built with .NET 8, FastAPI, and React.

[![Status](https://img.shields.io/badge/Status-Production--Ready-brightgreen)](#roadmap)
[![.NET](https://img.shields.io/badge/.NET-8.0-512BD4)](https://dotnet.microsoft.com/)
[![FastAPI](https://img.shields.io/badge/FastAPI-0.110-009688)](https://fastapi.tiangolo.com/)
[![React](https://img.shields.io/badge/React-18-61DAFB)](https://react.dev/)
[![Docker](https://img.shields.io/badge/Docker-Compose-2496ED)](https://www.docker.com/)
[![License](https://img.shields.io/badge/License-MIT-green)](#license)

---

## Overview

**fintech** is a real-time Vietnamese stock market platform that provides live price feeds, interactive charting, portfolio tracking, and watchlist management for VN-Index, HNX, and UPCOM markets. Powered by the [vnstock](https://github.com/thinh-vu/vnstock) data library and built on a clean three-service architecture.

> **Status:** All 4 phases implemented. Platform is production-ready with real-time price board, portfolio management, watchlist, and responsive UI.

---

## Features

- **Real-time Price Board** — Live tick data for VN-Index, HNX, UPCOM via SignalR WebSocket
- **Interactive Charts** — OHLCV candlestick charts powered by TradingView Lightweight Charts
- **Portfolio Management** — Track holdings, P&L, cost basis, and performance over time
- **Watchlist** — Personalized stock watchlists with custom price alerts
- **Secure Auth** — JWT-based authentication with ASP.NET Core Identity
- **Responsive UI** — Mobile-first design with TailwindCSS + shadcn/ui

---

## Architecture

```
┌──────────────────────────────────────────────────┐
│                   Browser                        │
│          React 18 + TypeScript + Vite            │
│     TradingView Charts · Zustand · TanStack      │
└──────────────────┬───────────────────────────────┘
                   │  REST + WebSocket (SignalR)
┌──────────────────▼───────────────────────────────┐
│             .NET 8 Web API                       │
│      Clean Architecture · JWT Auth · Redis       │
└──────────┬───────────────────┬───────────────────┘
           │                   │
     PostgreSQL 16        ┌────▼──────────────────┐
     (partitioned)        │  Python Data Service  │
                          │  FastAPI · vnstock    │
                          └───────────────────────┘
```

---

## Tech Stack

| Layer | Technology |
|-------|------------|
| **Frontend** | React 18, TypeScript, Vite, TailwindCSS, shadcn/ui |
| **Charts** | TradingView Lightweight Charts v4 |
| **State** | Zustand, TanStack Query |
| **Real-time** | @microsoft/signalr (WebSocket) |
| **Backend API** | .NET 8 Web API, ASP.NET Core Identity |
| **Data Service** | Python 3.11, FastAPI, vnstock |
| **Database** | PostgreSQL 16 (partitioned) |
| **Cache / PubSub** | Redis 7 |
| **Infrastructure** | Docker Compose, Nginx |

---

## Quick Start

**Prerequisites:** Docker & Docker Compose, Node.js 18+, Git

```bash
# Clone repository
git clone <repo-url>
cd fintech

# Copy environment config
cp .env.example .env
# Edit .env — set POSTGRES_PASSWORD and JWT_SECRET at minimum

# Start backend services (PostgreSQL, Redis, .NET API, Python data service)
docker compose up -d

# Start frontend dev server
cd client
npm install
npm run dev
```

**Service URLs:**

| Service | URL |
|---------|-----|
| Frontend (dev) | http://localhost:5173 |
| .NET API | http://localhost:5000 |
| API Docs (Swagger) | http://localhost:5000/swagger |
| PostgreSQL | localhost:5432 |
| Redis | localhost:6379 |

> **Production:** `docker compose --profile prod up -d` starts Nginx at port 80 serving the built frontend.

---

## Development Workflow

```bash
# Terminal 1 — backend stack (hot-reload enabled via docker-compose.override.yml)
docker compose up -d

# Terminal 2 — frontend Vite dev server (proxies /api and /hubs to localhost:5000)
cd client && npm run dev
```

**Useful commands (via Makefile):**

```bash
make up           # Start all services
make down         # Stop all services
make logs         # Tail all service logs
make api-logs     # Tail .NET API logs only
make vnstock-logs # Tail Python data service logs
make db-shell     # Open psql shell in postgres container
make redis-shell  # Open redis-cli in redis container
make clean        # Stop & remove all containers + volumes
make build        # Rebuild Docker images
```

---

## Project Structure

```
fintech/
├── client/            # React 19 + TypeScript + Vite frontend
├── src/               # .NET 8 Web API (Clean Architecture)
│   ├── VnStock.Domain/
│   ├── VnStock.Application/
│   ├── VnStock.Infrastructure/
│   └── VnStock.API/
├── services/
│   └── vnstock-service/  # Python FastAPI + vnstock data service
├── db/                # PostgreSQL init scripts
├── nginx/             # Nginx config (production)
├── tests/             # Unit & integration tests
└── docs/              # Project documentation
```

---

## Roadmap

| Phase | Description | Status |
|-------|-------------|--------|
| **Phase 1** | Foundation — Auth, Docker, API skeleton, React scaffold | ✅ Complete |
| **Phase 2** | Core Features — Real-time prices, charting, search | ✅ Complete |
| **Phase 3** | User Features — Portfolio, watchlist, alerts, dashboard | ✅ Complete |
| **Phase 4** | Polish & Production — Mobile UI, CI/CD, monitoring | ✅ Complete |
| **Phase 5** | International — Multi-market support (post-MVP) | 🔭 Future |

---

## Contributing

1. Fork the repository
2. Create a feature branch: `git checkout -b feat/your-feature`
3. Commit with conventional commits: `git commit -m "feat: add price alert"`
4. Push and open a Pull Request

See [development rules](.claude/rules/development-rules.md) for coding standards and conventions.

---

## License

MIT © fintech contributors
