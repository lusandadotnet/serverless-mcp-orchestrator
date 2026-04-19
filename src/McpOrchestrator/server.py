"""ZAR-Flow MCP server: tools for South African macro indicators (FastMCP)."""

from __future__ import annotations

import logging
import os
from typing import Any

import httpx
from fastmcp import FastMCP
from fastmcp.server.http import create_streamable_http_app

logging.basicConfig(level=logging.INFO)
logger = logging.getLogger("mcp-zar-flow")

mcp = FastMCP("ZAR-Flow")

SARB_LOWER = 3.0
SARB_UPPER = 6.0


def _api_base_url() -> str:
    return os.getenv("API_BASE_URL", "").rstrip("/")


def _build_http_app():
    """Streamable HTTP app for Azure Functions (ASGI) or local `mcp.run`."""
    return create_streamable_http_app(
        mcp,
        streamable_http_path="/mcp",
        stateless_http=True,
    )


http_app = _build_http_app()


@mcp.tool()
async def get_exchange_rate(base_currency: str = "USD", target_currency: str = "ZAR") -> str:
    """Fetches the latest ZAR exchange rate and CPI context from the ASP.NET Core API."""
    api_url = _api_base_url()
    if not api_url:
        logger.error("API_BASE_URL environment variable is missing!")
        return "Error: API_BASE_URL is not configured."

    summary_path = "/api/v1/indicators/summary"
    legacy_path = "/api/indicators"
    logger.info("Fetching indicators from %s", api_url)

    try:
        async with httpx.AsyncClient(timeout=15.0) as client:
            response = await client.get(f"{api_url}{summary_path}")
            if response.status_code == 404:
                response = await client.get(f"{api_url}{legacy_path}")
            response.raise_for_status()
            data: dict[str, Any] = response.json()
    except httpx.HTTPStatusError as e:
        logger.error("API HTTP error: %s", e.response.status_code)
        return f"Error: API returned HTTP {e.response.status_code}."
    except Exception as e:
        logger.exception("API request failed")
        return f"Error: could not reach API ({e!s})."

    rate = data.get("zarUsd")
    if rate is None:
        rate = data.get("ZAR_USD") or data.get("zar_USD")
    cpi = data.get("cpiYearOnYear", data.get("Inflation"))
    status = data.get("status", "unknown")
    within = data.get("withinSarbBand")

    return (
        "--- ZAR-Flow live data ---\n"
        f"Pair: {base_currency}/{target_currency}\n"
        f"ZAR/USD (implied spot): {rate}\n"
        f"CPI (y/y %): {cpi}\n"
        f"Within SARB 3–6% band: {within}\n"
        f"Source: {status}"
    )


@mcp.tool()
def analyze_inflation_impact(
    current_cpi: float,
    lower_threshold: float = SARB_LOWER,
    upper_threshold: float = SARB_UPPER,
) -> str:
    """Checks headline CPI against the SARB 3–6% target band."""
    logger.info("Analyzing CPI %s vs band %s–%s", current_cpi, lower_threshold, upper_threshold)

    if current_cpi > upper_threshold:
        return (
            f"ALERT: CPI ({current_cpi}%) is ABOVE the SARB upper threshold ({upper_threshold}%). "
            "This may warrant a hawkish monetary policy stance."
        )
    if current_cpi < lower_threshold:
        return (
            f"NOTE: CPI ({current_cpi}%) is BELOW the SARB lower bound ({lower_threshold}%). "
            "There may be room for accommodation depending on the output gap."
        )
    return (
        f"WITHIN MANDATE: CPI ({current_cpi}%) sits inside the SARB target band "
        f"({lower_threshold}–{upper_threshold}%)."
    )


@mcp.tool()
def inflation_variance_vs_target(
    current_cpi: float,
    target_midpoint: float = 4.5,
) -> str:
    """Computes variance of CPI from the midpoint of the 3–6% SARB inflation target range."""
    variance = round(float(current_cpi) - float(target_midpoint), 4)
    return (
        f"SARB band midpoint (default): {target_midpoint}% (implicit in 3–6% range). "
        f"Variance from midpoint: {variance:+.4f} percentage points."
    )


@mcp.tool()
def summarize_economic_news(query_topic: str) -> str:
    """Placeholder for summarizing South African financial news on a topic."""
    logger.info("News query: %s", query_topic)
    return (
        f"Summary: Sentiment on '{query_topic}' is mixed; "
        "watch the next MPC statement and local electricity availability."
    )


if __name__ == "__main__":
    mcp.run(transport="streamable-http", host="127.0.0.1", port=8765, path="/mcp")
