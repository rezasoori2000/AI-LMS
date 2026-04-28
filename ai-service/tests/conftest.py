"""
Shared pytest fixtures for the AI service test suite.

Test client uses httpx.AsyncClient with ASGITransport so tests run
entirely in-process — no real network calls, no port binding.
"""
from __future__ import annotations

import pytest
from httpx import ASGITransport, AsyncClient

from app.main import app


@pytest.fixture
async def client() -> AsyncClient:
    """
    Async HTTP test client bound to the FastAPI app.
    Uses ASGITransport so tests run in-process (fast, no network).
    """
    async with AsyncClient(
        transport=ASGITransport(app=app),
        base_url="http://testserver",
    ) as ac:
        yield ac
