"""
Health check endpoint.

GET /health — returns 200 when the service is alive.
Used by Docker HEALTHCHECK, Kubernetes liveness probes, and load balancers.
Does NOT require authentication.
"""
from __future__ import annotations

from fastapi import APIRouter

from app.models.common import HealthResponse

router = APIRouter()


@router.get(
    "/health",
    response_model=HealthResponse,
    tags=["Health"],
    summary="Service health check",
    description="Returns 200 with status=healthy when the service is running.",
)
async def health_check() -> HealthResponse:
    # TODO (Phase 3): Add readiness checks here (e.g. vector store connectivity)
    return HealthResponse(status="healthy")
