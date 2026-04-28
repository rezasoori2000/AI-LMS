# Phase 1 — Section 1 Readiness Checklist

Use this checklist before starting **Phase 1, Section 2** (Authentication and multi-tenancy).
Every item below should be verified manually in a clean clone of the repo.

---

## Foundation (automated — run these commands)

### Frontend

```bash
cd frontend
npm install
npm run type-check      # must exit 0
npm run lint            # must exit 0
npm run test -- --run   # must pass: 6 tests
npm run build           # must produce dist/ with no TS errors
```

### Backend

```bash
cd backend
dotnet build /p:TreatWarningsAsErrors=true   # must exit 0, 0 warnings
dotnet test --verbosity minimal              # must pass: 7 tests (5 domain + 2 API)
```

### AI Service

```bash
cd ai-service
python -m venv .venv && .venv/Scripts/activate   # Windows
# source .venv/bin/activate                      # macOS/Linux
pip install -r requirements-dev.txt
pytest                                           # must pass: 4 tests
ruff check .                                     # must exit 0
```

### Docker

```bash
# From repo root
cp .env.example .env
docker compose config --quiet    # must exit 0
```

---

## Manual Verification Checklist

### Project structure

- [ ] All source folders exist: `frontend/`, `backend/`, `ai-service/`, `infra/`, `docs/`, `scripts/`
- [ ] No secrets, API keys, or passwords are committed to the repository
- [ ] `.env` is in `.gitignore`; `.env.example` is committed

### Frontend

- [ ] `npm run dev` starts the Vite dev server at `http://localhost:5173`
- [ ] The default route (`/`) renders without console errors
- [ ] An unknown route (`/does-not-exist`) renders a 404/Not Found page
- [ ] Application layout renders in LTR (English default)
- [ ] React Query DevTools panel is visible in dev mode

### Backend

- [ ] `dotnet run --project src/LMS.Api` starts without errors
- [ ] `GET http://localhost:5000/health` returns `200 Healthy`
- [ ] `GET http://localhost:5000/swagger` opens the Swagger UI
- [ ] An unknown route returns a structured Problem Details JSON (not a stack trace)
- [ ] `ASPNETCORE_ENVIRONMENT=Production dotnet run…` does **not** expose Swagger

### AI Service

- [ ] `uvicorn app.main:app --reload` starts at `http://localhost:8000`
- [ ] `GET http://localhost:8000/health` returns `{"status": "healthy"}`
- [ ] `GET http://localhost:8000/docs` opens the Swagger UI (debug on)
- [ ] Setting `DEBUG=false` hides `/docs` and `/redoc`

### Docker Compose

- [ ] `docker compose up` builds and starts all 3 services without errors
- [ ] All service health checks pass (`docker compose ps` shows `(healthy)`)
- [ ] Frontend is accessible at `http://localhost:5173`
- [ ] Backend health at `http://localhost:5000/health`
- [ ] AI service health at `http://localhost:8000/health`
- [ ] Editing `frontend/src/` triggers Vite HMR without container restart
- [ ] Editing `backend/src/` triggers `dotnet watch` recompile
- [ ] Editing `ai-service/app/` triggers uvicorn `--reload`

### Documentation

- [ ] `docs/architecture/architecture-overview.md` accurately reflects the codebase
- [ ] `docs/development/local-development.md` steps work end-to-end on a fresh machine
- [ ] `docs/development/folder-structure.md` matches the actual file tree
- [ ] README Phase 1 Section 1 status is up to date

---

## "Definition of Done" for Section 1

All of the following must be true before committing to Section 2:

| # | Check | Pass? |
|---|-------|-------|
| 1 | Zero build errors or warnings (frontend, backend, AI service) | |
| 2 | All automated tests pass on a clean clone | |
| 3 | `docker compose up` works from `.env.example` with no manual steps | |
| 4 | No secrets or credentials anywhere in the committed code | |
| 5 | Swagger/docs hidden in production mode for all services | |
| 6 | Health endpoints return 200 for all three services | |
| 7 | Architecture overview doc matches the code | |
| 8 | Each developer on the team has run the setup independently | |

---

## What Section 2 Will Build On

Section 2 (Authentication + Multi-Tenancy) requires the following from Section 1:

| Dependency | Status | Location |
|------------|--------|----------|
| `ICurrentUserService` interface | ✅ stubbed | `LMS.Application/Common/Interfaces/` |
| `TenantMiddleware` placeholder | ✅ stubbed | `LMS.Api/Middleware/TenantMiddleware.cs` |
| CORS policy configured from env | ✅ done | `appsettings.json` + `ServiceCollectionExtensions` |
| Auth handlers commented-out in pipeline | ✅ done | `ApplicationBuilderExtensions.cs` lines 35–40 |
| JWT security definition stub in Swagger | ✅ commented | `ServiceCollectionExtensions.cs` TODO comment |
| `BaseLLMProvider` for protected AI routes | ✅ stubbed | `ai-service/app/providers/base.py` |
| Frontend `api-client.ts` for auth headers | ✅ in place | `frontend/src/services/api-client.ts` |
