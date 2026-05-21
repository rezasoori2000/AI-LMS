# Local Development Guide

This guide walks through running the AI-LMS stack on your local machine — both the
fast path (Docker Compose) and the manual path (each service run natively).

---

## Prerequisites

| Tool | Required version | Install |
|------|-----------------|---------|
| Docker + Docker Compose | Docker Desktop ≥ 4.25 / Compose v2 | https://docs.docker.com/get-docker/ |
| Node.js | ≥ 20.x | https://nodejs.org or `nvm` / `fnm` |
| .NET SDK | 8.0.x (pinned in `backend/global.json`) | https://dotnet.microsoft.com/download |
| Python | 3.12.x (pinned in `ai-service/.python-version`) | https://python.org or `pyenv` |

---

## Quick Start — Docker Compose (recommended)

1. **Copy the environment file:**

   ```bash
   cp .env.example .env
   ```

   Edit `.env` for your machine (API keys, ports). The defaults work for local dev
   with no external AI providers.

2. **Start all services with hot-reload:**

   ```bash
   docker compose up
   ```

   Docker Compose automatically merges `docker-compose.override.yml`, which enables
   file-watching and hot-reload for every service.

3. **Access the services:**

   | Service | URL |
   |---------|-----|
   | Frontend (Vite dev server) | http://localhost:5173 |
   | Backend API + Swagger UI | http://localhost:5000/swagger |
   | Backend health check | http://localhost:5000/health |
   | AI Service docs | http://localhost:8000/docs |
   | AI Service health | http://localhost:8000/health |

4. **Stop:**

   ```bash
   docker compose down
   ```

   Add `--volumes` to also wipe named volumes (database data, etc.).

---

## Running Services Natively

### Frontend

```bash
cd frontend
npm install
npm run dev
```

The Vite dev server starts at `http://localhost:5173` and proxies `/api` calls to
`http://localhost:5000` (see `vite.config.ts`).

Other useful scripts:

```bash
npm run build          # production build → dist/
npm run type-check     # tsc --noEmit
npm run lint           # ESLint
npm run test           # Vitest (watch mode)
npm run test:coverage  # Vitest with coverage report → coverage/
```

### Backend (ASP.NET Core)

```bash
cd backend
dotnet run --project src/LMS.Api/LMS.Api.csproj
```

For hot-reload during development:

```bash
dotnet watch --project src/LMS.Api/LMS.Api.csproj run
```

The API listens on `http://localhost:5000`. Swagger UI is available at
`http://localhost:5000/swagger` when `ASPNETCORE_ENVIRONMENT=Development`.

Running tests:

```bash
dotnet test                          # all projects
dotnet test tests/LMS.Api.Tests      # API integration tests only
dotnet test tests/LMS.Domain.Tests   # domain unit tests only
```

Database migrations (requires PostgreSQL running):

```bash
# Apply all pending migrations
dotnet ef database update --project src/LMS.Infrastructure --startup-project src/LMS.Api

# Add a new migration
dotnet ef migrations add <Name> --project src/LMS.Infrastructure --startup-project src/LMS.Api
```

Integration tests use an InMemory database and do not require PostgreSQL.

### AI Service (FastAPI / Python)

```bash
cd ai-service

# Create a virtual environment (first time only)
python -m venv .venv

# Activate
# Windows:
.venv\Scripts\activate
# macOS / Linux:
source .venv/bin/activate

# Install dependencies
pip install -r requirements-dev.txt

# Run with hot-reload
uvicorn app.main:app --host 0.0.0.0 --port 8000 --reload
```

Interactive API docs (dev only): `http://localhost:8000/docs`

Running tests and linting:

```bash
pytest                             # all tests
pytest --cov=app --cov-report=term-missing   # with coverage
ruff check .                       # lint
ruff format .                      # format
```

---

## Environment Variables

All configuration is driven by environment variables. Copy `.env.example` to `.env` and
fill in the values you need. The `.env` file is **gitignored** — never commit secrets.

Key variables:

| Variable | Default | Purpose |
|----------|---------|---------|
| `FRONTEND_PORT` | `5173` | Host port for the frontend |
| `BACKEND_PORT` | `5000` | Host port for the backend API |
| `AI_SERVICE_PORT` | `8000` | Host port for the AI service |
| `AI_DEFAULT_PROVIDER` | `ollama` | Which LLM provider the AI service uses |
| `OLLAMA_BASE_URL` | `http://localhost:11434` | Ollama endpoint |
| `OPENAI_API_KEY` | *(empty)* | OpenAI API key (Phase 2+) |

> **JWT secret requirement**: In `Production` and `Staging` environments,
> `BACKEND_JWT_SECRET` must be present and at least 32 characters long.
> The application will refuse to start if this condition is not met.
> In `Development`, a warning is logged but startup continues.
> The default value in `.env.example` is a placeholder — replace it before any deployment.

See `.env.example` for the full list.

---

## Useful Docker Commands

```bash
# Rebuild a single service without restarting others
docker compose up --build frontend

# View logs for one service
docker compose logs -f backend

# Open a shell inside a running container
docker compose exec ai-service sh

# Run backend tests inside the container image (CI simulation)
docker compose run --rm backend dotnet test

# Check service health status
docker compose ps
```

---

## Troubleshooting

### Port already in use

Change the host port in `.env`:

```bash
FRONTEND_PORT=3000
BACKEND_PORT=7000
```

### Frontend changes not reflecting

The Vite dev server's HMR works via the bind-mounted volume. If changes still don't
appear, check that the container started with the override file:

```bash
docker compose config | grep -A3 frontend
```

### Backend health check failing on startup

`dotnet watch` can take 20–30 seconds on first run. The `depends_on` health check in
`docker-compose.yml` will retry automatically. Check backend logs:

```bash
docker compose logs -f backend
```

### AI service import errors after adding a package

Rebuild the `ai-service` image to re-run `pip install`:

```bash
docker compose up --build ai-service
```
