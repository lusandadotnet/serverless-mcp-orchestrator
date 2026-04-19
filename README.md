# ZAR-Flow (serverless MCP orchestrator)

Cross-platform pipeline: **ASP.NET Core 10** REST API (South African macro indicators) plus a **FastMCP** Python server (deployed to **Azure Functions**) that exposes agent tools to LLM clients over streamable HTTP.

Infrastructure: **Azure SQL** (Basic), **Linux App Service** (API, .NET 10), **Python 3.11** on a **Consumption** Functions app, plus Storage, Log Analytics, and Application Insights (see [`infra/main.bicep`](infra/main.bicep)).

## Run locally

**API (SQL Server / LocalDB):** set `ConnectionStrings:ZarFlowDb`, then `dotnet run --project src/EconomicDataService`.

**MCP:** `cd src/McpOrchestrator`, set `API_BASE_URL` to the API base URL, then `python server.py` (listens on `http://127.0.0.1:8765/mcp`).

## Endpoints

### Azure (deployed)

Run **`azd show`** for the current **api** and **mcp-server** HTTPS bases and an Azure Portal link to the resource group. Host names include a unique suffix from your resource group; another environment will differ.

Example bases (environment `sarb-orchestrator-99`):

| Service | Base URL |
|---------|----------|
| **REST API** | [https://api-zarflow-nrhuttfhony7o.azurewebsites.net/](https://api-zarflow-nrhuttfhony7o.azurewebsites.net/) |
| **MCP (streamable HTTP)** | [https://func-mcp-server-nrhuttfhony7o.azurewebsites.net/mcp](https://func-mcp-server-nrhuttfhony7o.azurewebsites.net/mcp) |

### REST API paths (`/api/v1`)

| Method | Path |
|--------|------|
| GET | `/api/v1/indicators/summary` |
| GET | `/api/v1/exchange-rates/latest` |
| POST | `/api/v1/exchange-rates` |
| GET | `/api/v1/inflation/latest` |
| POST | `/api/v1/inflation/readings` |

Legacy: `GET /api/indicators` (same summary as `/api/v1/indicators/summary`).

In **Development** only, OpenAPI is mapped at `/openapi/v1.json` (see `Program.cs`).

## Deploy to Azure

Deployments use the [Azure Developer CLI](https://learn.microsoft.com/azure/developer/azure-developer-cli/) (`azd`) with [`azure.yaml`](azure.yaml).

### Prerequisites

- Install [`azd`](https://learn.microsoft.com/azure/developer/azure-developer-cli/install-azd) and sign in: `azd auth login`
- An Azure subscription with permission to create resource groups and the resources in [`infra/main.bicep`](infra/main.bicep)

### First-time provision

From the repository root:

1. `azd init` if you do not yet have a local `azd` environment (pick an environment name you will reuse, for example `sarb-orchestrator-99`).
2. Set the Bicep `adminPassword` parameter (SQL administrator password) before provisioning, for example:  
   `azd env set adminPassword "<your-strong-password>"`
3. Run `azd up` to create the resource group and deploy both **api** (App Service) and **mcp-server** (Functions).

Entity Framework migrations run on API startup against the deployed database.

### Deploy code only

When infrastructure already exists, publish new builds:

```bash
azd deploy --no-prompt
```

Show HTTPS endpoints and a portal link to the resource group:

```bash
azd show
```

### GitHub Actions

The workflow [`.github/workflows/azure-dev.yml`](.github/workflows/azure-dev.yml) runs `azd deploy` on pushes to `main` and on **workflow_dispatch**.

Configure the repository as follows:

| Item | Purpose |
|------|---------|
| Variable `AZURE_ENV_NAME` | Must match your `azd` environment name (the one in `.azure/config.json` / `azd env list`) |
| Secrets `AZURE_CLIENT_ID`, `AZURE_TENANT_ID`, `AZURE_SUBSCRIPTION_ID` | Used by [azure/login](https://github.com/Azure/login) for federated OIDC to Azure |

### Troubleshooting

- **`403 Site Disabled` during API deploy** — Usually the **api** App Service is not accepting SCM traffic. In the [Azure Portal](https://portal.azure.com), open the web app and check **Overview → Status**. If it shows **Stopped**, click **Start** and retry `azd deploy`. If status is **QuotaExceeded** (or usage shows as exceeded), the **Free (F1) plan quota** for your subscription or region is full—you must **scale up** the App Service plan (for example to **B1** Basic), delete or stop other Free-tier apps in that region, or use another subscription. Until quota is resolved, zip deploy and the site will keep returning 403.
- **`az` CLI vs `azd`** — `azd` has its own sign-in. The Azure CLI (`az webapp start`, etc.) requires a separate `az login` if you use it to manage apps.

## Tests

- `dotnet test`
- `cd src/McpOrchestrator && pytest`
