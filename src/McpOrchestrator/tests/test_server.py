"""pytest suite for ZAR-Flow MCP tools."""

from __future__ import annotations

import os

import httpx
import pytest
import respx

from server import (
    analyze_inflation_impact,
    get_exchange_rate,
    http_app,
    inflation_variance_vs_target,
)


def test_analyze_inflation_impact_within_band() -> None:
    text = analyze_inflation_impact(5.3)
    assert "WITHIN MANDATE" in text


def test_analyze_inflation_impact_above_band() -> None:
    text = analyze_inflation_impact(7.2)
    assert "ABOVE" in text


def test_analyze_inflation_impact_below_band() -> None:
    text = analyze_inflation_impact(2.1)
    assert "BELOW" in text


def test_inflation_variance_vs_target() -> None:
    text = inflation_variance_vs_target(5.3, 4.5)
    assert "0.8" in text or "+0.8" in text


@pytest.mark.asyncio
@respx.mock
async def test_get_exchange_rate_reads_summary_json() -> None:
    os.environ["API_BASE_URL"] = "https://example.test"
    respx.get("https://example.test/api/v1/indicators/summary").mock(
        return_value=httpx.Response(
            200,
            json={
                "zarUsd": 18.45,
                "cpiYearOnYear": 5.3,
                "withinSarbBand": True,
                "status": "Live from Azure SQL",
            },
        )
    )

    out = await get_exchange_rate("USD", "ZAR")
    assert "18.45" in out
    assert "ZAR" in out


@pytest.mark.asyncio
async def test_mcp_http_app_exposes_mcp_path() -> None:
    transport = httpx.ASGITransport(app=http_app)
    async with httpx.AsyncClient(transport=transport, base_url="http://test") as client:
        r = await client.get("/mcp")
        assert r.status_code in (200, 400, 405, 406)
