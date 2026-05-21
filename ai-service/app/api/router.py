"""
Root API router.

Structure:
  /health           — liveness / readiness probe (no auth required)
  /api/v1/...       — versioned application endpoints (Phase 2+)

Adding a new feature module:
  1. Create app/api/v1/<feature>.py with its APIRouter
  2. Import and include it here under the v1_router with a prefix
"""
from __future__ import annotations

from fastapi import APIRouter

from app.api.v1.health import router as health_router
from app.api.v1.tutor import router as tutor_router

# ── Top-level unversioned routes ─────────────────────────────────────────────
# /health is intentionally NOT under /api/v1 so Kubernetes probes and load
# balancers can reach it without the API path prefix.
api_router = APIRouter()
api_router.include_router(health_router)

# ── Versioned routes (/api/v1/...) ───────────────────────────────────────────
v1_router = APIRouter(prefix="/api/v1")

# Section 11 Part 1: Tutor endpoint stub (returns 501 until Part 2 wires LLM)
v1_router.include_router(tutor_router, prefix="/tutor", tags=["Tutor"])

# TODO (Phase 2+): Add remaining feature routers here, e.g.:
#
# from app.api.v1.scoring import router as scoring_router
# v1_router.include_router(scoring_router, prefix="/scoring", tags=["Scoring"])
#
# from app.api.v1.insights import router as insights_router
# v1_router.include_router(insights_router, prefix="/insights", tags=["Insights"])
#
# from app.api.v1.recommendations import router as recommendations_router
# v1_router.include_router(
#     recommendations_router, prefix="/recommendations", tags=["Recommendations"]
# )

api_router.include_router(v1_router)
