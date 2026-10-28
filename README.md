# CDNS Rewrite

Monorepo for the CDNS rewrite. Backend is .NET 10 (Clean Architecture); frontend is Vite + React + JavaScript.

---

## Getting Started (Local Development)

This section walks a new teammate through running CDNS on a local machine.
All commands work on Windows (Command Prompt, PowerShell, or Git Bash), macOS, and Linux.

### 1. Prerequisites

| Tool | Minimum version | Version check |
|---|---|---|
| Docker Desktop (Windows / macOS) or Docker Engine + Compose v2 (Linux) | Docker 25 / Compose 2.x | `docker compose version` |
| .NET 10 SDK | 10.x | `dotnet --version` |
| Node.js LTS | 22+ | `node --version` |
| Git | any recent | `git --version` |

On Windows and macOS, install [Docker Desktop](https://www.docker.com/products/docker-desktop/).
On Linux, install [Docker Engine](https://docs.docker.com/engine/install/) and the [Compose plugin](https://docs.docker.com/compose/install/linux/).

### 2. Clone and configure

Clone the repository and move into the project root:

```shell
git clone <repo-url>
cd CDNS-Rewrite
```

Copy the example environment file:

```shell
# macOS / Linux / Git Bash
cp .env.example .env
```

```shell
# Windows Command Prompt
copy .env.example .env
```

```shell
# Windows PowerShell
Copy-Item .env.example .env
```

Open `.env` in any editor and set the four variables:

```dotenv
POSTGRES_DB=cdns_dev
POSTGRES_USER=cdns
POSTGRES_PASSWORD=<strong-password>
POSTGRES_PORT=5432
```

If you already have a native PostgreSQL server running on port 5432, change `POSTGRES_PORT` to a free port (e.g. `5433`) to avoid a conflict.

> **Important — read before you start the container.**
> The `POSTGRES_DB`, `POSTGRES_USER`, and `POSTGRES_PASSWORD` values are written into the Docker volume only the first time the container starts on an empty volume.
> If you need to change any of them after the first start, you must destroy the volume first.
> This permanently deletes all local database data:
>
> ```shell
> docker compose down -v
> docker compose up -d
> ```
>
> `.env` is listed in `.gitignore`. Never commit it.

### 3. Start the database

```shell
docker compose up -d
```

Check that the container is healthy:

```shell
docker compose ps
```

Expected output (abbreviated):

```
NAME      IMAGE        STATUS
postgres  postgres:16  Up X seconds (healthy)
```

The `(healthy)` label confirms the built-in healthcheck passed.
If the status shows `(starting)`, wait a few seconds and run `docker compose ps` again.
If it stays `(unhealthy)`, see the troubleshooting table below.

The database runs the official `postgres:16` image (Debian/glibc).
The Alpine variant is deliberately avoided to prevent collation differences between developer machines.

### 4. Verify the connection

Run a query directly inside the container to confirm the database is reachable:

```shell
docker compose exec postgres psql -U cdns -d cdns_dev -c "select version();"
```

Replace `cdns` and `cdns_dev` with the values you set in `.env`.
A successful response prints the PostgreSQL version string and exits with code 0.

### 5. Connect with DBeaver

[DBeaver Community](https://dbeaver.io/) is a free cross-platform GUI client.

1. Open DBeaver and click **New Database Connection**.
2. Select **PostgreSQL** and click **Next**.
3. Fill in the connection fields using your `.env` values:

| Field | Value |
|---|---|
| Host | `localhost` |
| Port | `POSTGRES_PORT` from `.env` (default `5432`) |
| Database | `POSTGRES_DB` from `.env` (e.g. `cdns_dev`) |
| Username | `POSTGRES_USER` from `.env` (e.g. `cdns`) |
| Password | `POSTGRES_PASSWORD` from `.env` |

4. Click **Test Connection**. DBeaver may prompt you to download the PostgreSQL JDBC driver — allow it.
5. Click **Finish**.

### 6. Set connection strings (user-secrets)

The application connection string is not stored in git.
Each developer stores it locally using .NET user-secrets, which writes to an OS-managed directory outside the repository and is never included in builds.

Run both commands from the repository root, substituting your `.env` values:

**Cdns.Api**

```shell
dotnet user-secrets set "ConnectionStrings:Default" "Host=localhost;Port=5432;Database=cdns_dev;Username=cdns;Password=<your-password>" --project backend/src/Cdns.Api
```

**Cdns.Worker**

```shell
dotnet user-secrets set "ConnectionStrings:Default" "Host=localhost;Port=5432;Database=cdns_dev;Username=cdns;Password=<your-password>" --project backend/src/Cdns.Worker
```

Replace `5432`, `cdns_dev`, `cdns`, and `<your-password>` with your actual `.env` values.

In non-local environments (CI, staging, production), supply the same key as an environment variable instead.
Note the double underscore (`__`), which is the .NET environment-variable hierarchy separator:

```
ConnectionStrings__Default=Host=...;Port=...;Database=...;Username=...;Password=...
```

### 7. Apply database migrations and run the apps

**Migrations — coming soon.**
`CdnsDbContext` and the baseline EF Core migration have not been created yet.
Once they exist, install the EF Core CLI tool (first time only) and apply the migration:

```shell
dotnet tool install --global dotnet-ef
dotnet ef database update --project backend/src/Cdns.Infrastructure --startup-project backend/src/Cdns.Api
```

The schema uses snake_case column and table names (EFCore.NamingConventions).
Each developer runs migrations against their own local database.

**Backend API**

```shell
cd backend
dotnet run --project src/Cdns.Api
```

Health check: `GET http://localhost:<port>/health`

**Frontend**

```shell
cd frontend
npm install
npm run dev
```

Vite dev server: `http://localhost:5173`

### 8. Troubleshooting

| Symptom | Likely cause | Fix |
|---|---|---|
| `Error response from daemon: Ports are not available` or `port is already allocated` | A process (native Postgres or another container) is already using `POSTGRES_PORT` | Set `POSTGRES_PORT` to a free port in `.env`, then `docker compose up -d` |
| `FATAL: role "cdns" does not exist` | Volume was created with different `POSTGRES_USER` credentials | `docker compose down -v && docker compose up -d` — this deletes local data |
| `FATAL: password authentication failed for user "cdns"` | `.env` password does not match the password stored in the existing volume | `docker compose down -v && docker compose up -d` — this deletes local data |
| Container stays `(starting)` / goes `(unhealthy)` | Postgres failed to initialise | `docker compose logs postgres` to read the error |
| `\r` errors or `Permission denied` on scripts (Linux / macOS) | Files checked out with Windows line endings | `git config core.autocrlf input` and re-checkout the affected files |

### 9. Useful commands

```shell
# Stop the database without removing the volume
docker compose stop

# Start a stopped database
docker compose start

# Stream live logs
docker compose logs -f postgres

# Open an interactive psql shell inside the container
docker compose exec postgres psql -U cdns -d cdns_dev

# Destroy the container and its volume — all local data is lost
docker compose down -v
```

---

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Node.js 22+](https://nodejs.org/)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/)

## Running locally

### 1. Environment variables

```shell
cp .env.example .env
# Edit .env and set POSTGRES_PASSWORD (and any other values)
```

### 2. Start Postgres

```shell
docker compose up -d
```

### 3. Backend

```shell
cd backend
dotnet run --project src/Cdns.Api
```

The API will be available at `https://localhost:5001` (or the port shown in the console).
Health check: `GET /health`

### 4. Frontend

```shell
cd frontend
npm install   # first time only
npm run dev
```

Vite dev server runs at `http://localhost:5173` by default.

## Running backend tests

```shell
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
├── frontend/                   # Vite + React + JavaScript
├── docs/legacy/                # Legacy documentation (archived)
└── docker-compose.yml
```
