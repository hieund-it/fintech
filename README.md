# fintech

> A full-stack Vietnamese stock market analytics and portfolio management platform built with .NET 8, FastAPI, and React.

[![Status](https://img.shields.io/badge/Status-Planning-yellow)](#roadmap)
[![.NET](https://img.shields.io/badge/.NET-8.0-512BD4)](https://dotnet.microsoft.com/)
[![FastAPI](https://img.shields.io/badge/FastAPI-0.110-009688)](https://fastapi.tiangolo.com/)
[![React](https://img.shields.io/badge/React-18-61DAFB)](https://react.dev/)
[![Docker](https://img.shields.io/badge/Docker-Compose-2496ED)](https://www.docker.com/)
[![License](https://img.shields.io/badge/License-MIT-green)](#license)

---

## Overview

**fintech** is a real-time Vietnamese stock market platform that provides live price feeds, interactive charting, portfolio tracking, and watchlist management for VN-Index, HNX, and UPCOM markets. Powered by the [vnstock](https://github.com/thinh-vu/vnstock) data library and built on a clean three-service architecture.

> **Status:** Project is currently in the planning phase. Implementation begins in Phase 1 shortly. See [Roadmap](#roadmap) for progress.

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
┌─────────────────────────────────────────────────┐
│                   Browser                        │
│          React 18 + TypeScript + Vite            │
│     TradingView Charts · Zustand · TanStack       │
└──────────────────┬──────────────────────────────┘
                   │  REST + WebSocket (SignalR)
┌──────────────────▼──────────────────────────────┐
│             .NET 8 Web API                       │
│      Clean Architecture · JWT Auth · Redis       │
└──────────┬───────────────────┬──────────────────┘
           │                   │
     PostgreSQL 16        ┌────▼─────────────────┐
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

> **Note:** Implementation is in progress. These steps will work once Phase 1 is complete.

**Prerequisites:** Docker & Docker Compose, Git

```bash
# Clone repository
git clone <repo-url>
cd fintech

# Copy environment config
cp .env.example .env
# Edit .env with your credentials

# Start all services
docker compose up -d

# Access the app
open http://localhost:3000
```

**Service URLs:**

| Service | URL |
|---------|-----|
| Frontend | http://localhost:3000 |
| .NET API | http://localhost:5000 |
| Python Data Service | http://localhost:8000 |
| API Docs (Swagger) | http://localhost:5000/swagger |

---

## Project Structure

```
fintech/
├── frontend/          # React + TypeScript application
├── backend/           # .NET 8 Web API (Clean Architecture)
│   └── src/
│       ├── Domain/
│       ├── Application/
│       ├── Infrastructure/
│       └── API/
├── data-service/      # Python FastAPI + vnstock data service
├── docker/            # Docker Compose & Nginx config
└── docs/              # Project documentation
```

---

## Roadmap

| Phase | Description | Status |
|-------|-------------|--------|
| **Phase 1** | Foundation — Auth, Docker, API skeleton, React scaffold | 🔄 Planning |
| **Phase 2** | Core Features — Real-time prices, charting, search | ⏳ Pending |
| **Phase 3** | User Features — Portfolio, watchlist, alerts, dashboard | ⏳ Pending |
| **Phase 4** | Polish & Production — Mobile UI, CI/CD, monitoring | ⏳ Pending |
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
