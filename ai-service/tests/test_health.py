"""Tests for the /health endpoint."""
from __future__ import annotations

import pytest
from httpx import AsyncClient


@pytest.mark.asyncio
async def test_health_returns_200(client: AsyncClient) -> None:
    response = await client.get("/health")
    assert response.status_code == 200


@pytest.mark.asyncio
async def test_health_status_is_healthy(client: AsyncClient) -> None:
    response = await client.get("/health")
    body = response.json()
    assert body["status"] == "healthy"


@pytest.mark.asyncio
async def test_health_includes_version(client: AsyncClient) -> None:
    response = await client.get("/health")
    body = response.json()
    assert "version" in body
    assert isinstance(body["version"], str)
    assert body["version"]  # non-empty


@pytest.mark.asyncio
async def test_health_content_type_is_json(client: AsyncClient) -> None:
    response = await client.get("/health")
    assert "application/json" in response.headers["content-type"]
