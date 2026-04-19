"""Azure Functions (Python v2) — exposes FastMCP streamable HTTP via ASGI."""

import azure.functions as func

from server import http_app

app = func.FunctionApp(http_auth_level=func.AuthLevel.ANONYMOUS)

_middleware = func.AsgiMiddleware(http_app)


@app.route(
    route="{*path}",
    methods=["GET", "POST", "PUT", "DELETE", "OPTIONS", "PATCH"],
    auth_level=func.AuthLevel.ANONYMOUS,
)
async def mcp_streamable_http(req: func.HttpRequest, context: func.Context) -> func.HttpResponse:
    return await _middleware.handle_async(req, context)
