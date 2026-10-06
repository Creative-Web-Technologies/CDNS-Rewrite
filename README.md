# CDNS Rewrite

Monorepo for the CDNS rewrite. Backend is .NET 10 (Clean Architecture); frontend is Vite + React + TypeScript.

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Node.js 22+](https://nodejs.org/)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/)

## Running locally

### 1. Environment variables

```bash
cp .env.example .env
# Edit .env and set POSTGRES_PASSWORD (and any other values)
```

### 2. Start Postgres

```bash
docker compose up -d
```

### 3. Backend

```bash
cd backend
dotnet run --project src/Cdns.Api
```

The API will be available at `https://localhost:5001` (or the port shown in the console).
Health check: `GET /health`

### 4. Frontend

```bash
cd frontend
npm install   # first time only
npm run dev
```

Vite dev server runs at `http://localhost:5173` by default.

## Running backend tests

```bash
cd backend
dotnet test
```

## Project structure

```
.
├── backend/
│   ├── Cdns.slnx
│   ├── src/
│   │   ├── Cdns.Api            # ASP.NET Core Web API
│   │   ├── Cdns.Application    # Application layer (use cases)
│   │   ├── Cdns.Domain         # Domain entities and interfaces
│   │   ├── Cdns.Infrastructure # EF Core, Postgres, external services
│   │   └── Cdns.Worker         # Background worker service
│   └── tests/
│       ├── Cdns.Domain.Tests
│       └── Cdns.Application.Tests
├── frontend/                   # Vite + React + TypeScript
├── docs/legacy/                # Legacy documentation (archived)
└── docker-compose.yml
```
