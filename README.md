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

Secrets are per-machine — every developer must run these commands once on their own machine. A `git pull` does not carry secrets across.

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

Verify the secret was stored correctly:

```shell
dotnet user-secrets list --project backend/src/Cdns.Api
```

If the output is `Could not find the global property 'UserSecretsId'`, the csproj on your checkout is missing the secret ID — run `git pull` to get the latest version, then rerun the `set` command.

In non-local environments (CI, staging, production), supply the same key as an environment variable instead.
Note the double underscore (`__`), which is the .NET environment-variable hierarchy separator:

```
ConnectionStrings__Default=Host=...;Port=...;Database=...;Username=...;Password=...
```

### 7. Apply database migrations and run the apps

**Migrations**

Complete step 6 before running any `dotnet ef` command. The CLI starts `Cdns.Api` to build the `DbContext`; if the user-secret is missing it will fail with `ConnectionStrings:Default is not configured`.

Install the EF Core CLI (first time only):

```shell
dotnet tool install --global dotnet-ef
```

Apply existing migrations (the Postgres container from step 3 must be running):

```shell
dotnet ef database update --project backend/src/Cdns.Infrastructure --startup-project backend/src/Cdns.Api
```

**Creating a new migration**

Always run `git pull` first to make sure you have the latest `CdnsDbContextModelSnapshot.cs` before adding a migration:

```shell
dotnet ef migrations add <MigrationName> --project backend/src/Cdns.Infrastructure --startup-project backend/src/Cdns.Api
```

Rules:
- Never edit or delete a migration that has already been pushed to `main` — add a new one instead.
- If two branches each add a migration, you will get a merge conflict in `CdnsDbContextModelSnapshot.cs`. Resolve it by rebasing your branch on `main` and regenerating your migration.

The schema uses snake_case column and table names (EFCore.NamingConventions).
Each developer runs migrations against their own local database.

**Backend API**

```shell
cd backend
dotnet run --project src/Cdns.Api
```

The API will be available at `https://localhost:5001` (or the port shown in the terminal).
Health check: `GET https://localhost:5001/health`

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
| `ConnectionStrings:Default is not configured` or `Unable to create a 'DbContext'` when running `dotnet ef` | The user-secret from step 6 isn't set on this machine | Run the `user-secrets set` command from step 6, then confirm with `dotnet user-secrets list --project backend/src/Cdns.Api` |
| `Could not find the global property 'UserSecretsId'` when running `dotnet user-secrets` | Checkout is missing the csproj change that added the secret ID | `git pull`, then rerun the `set` command |
| `dotnet ef database update` fails to connect to the database | Port, database name, or password in the secret doesn't match `.env`, or the container isn't running | Compare the secret with `.env`, then run `docker compose ps` |

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
