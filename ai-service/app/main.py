"""
FastAPI application factory and entrypoint.

Run locally:
    uvicorn app.main:app --reload --host 0.0.0.0 --port 8000

Run in Docker:
    uvicorn app.main:app --host 0.0.0.0 --port 8000 --workers 4
"""
from __future__ import annotations

import logging
from collections.abc import AsyncIterator
from contextlib import asynccontextmanager

from fastapi import FastAPI, Request
from fastapi.middleware.cors import CORSMiddleware
from fastapi.responses import JSONResponse

from app.api.router import api_router
from app.core.config import settings
from app.core.logging import configure_logging

logger = logging.getLogger(__name__)


@asynccontextmanager
async def lifespan(app: FastAPI) -> AsyncIterator[None]:
    """
    Application lifecycle manager.
    Startup: configure logging, warm up provider connections, load models.
    Shutdown: release resources, close connections.
    """
    configure_logging(debug=settings.debug)
    logger.info("Starting %s v%s", settings.app_name, settings.app_version)
    logger.info("Default LLM provider: %s", settings.ai_default_provider)

    # TODO (Phase 2): Initialize provider pool
    # TODO (Phase 2): Pre-load embedding model if using local provider
    # TODO (Phase 3): Connect to vector store

    yield  # ← application is running

    logger.info("Shutting down %s", settings.app_name)
    # TODO (Phase 2): Close provider HTTP sessions
    # TODO (Phase 3): Disconnect from vector store


def create_app() -> FastAPI:
    """
    Create and configure the FastAPI application.
    Docs (Swagger UI & ReDoc) are only exposed in debug mode.
    """
    app = FastAPI(
        title=settings.app_name,
        version=settings.app_version,
        description="AI tutoring, RAG, scoring, and learner insights service.",
        # Never expose API schema in production — it leaks endpoint structure
        docs_url="/docs" if settings.debug else None,
        redoc_url="/redoc" if settings.debug else None,
        openapi_url="/openapi.json" if settings.debug else None,
        lifespan=lifespan,
    )

    # ── CORS ────────────────────────────────────────────────────────────────
    # cors_allowed_origins is validated in Settings — never contains "*"
    app.add_middleware(
        CORSMiddleware,
        allow_origins=settings.cors_allowed_origins,
        allow_credentials=True,
        allow_methods=["*"],
        allow_headers=["*"],
    )

    # ── Global exception handler ─────────────────────────────────────────
    @app.exception_handler(Exception)
    async def unhandled_exception_handler(request: Request, exc: Exception) -> JSONResponse:
        logger.error(
            "Unhandled exception: %s %s — %r",
            request.method,
            request.url.path,
            exc,
            exc_info=True,
        )
        # Never expose exc details — only a generic message reaches the client
        return JSONResponse(
            status_code=500,
            content={"detail": "An unexpected error occurred.", "success": False},
        )

    # ── Routers ──────────────────────────────────────────────────────────
    app.include_router(api_router)

    return app


app = create_app()
