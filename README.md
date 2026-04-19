# Serverless MCP Orchestrator (ZAR-Flow)

Proof of concept for a cloud-native stack that connects AI models to South African macro data: a **ASP.NET Core 10** REST API (Azure App Service, Azure SQL) plus a **FastMCP** Python layer on **Azure Functions** (streamable HTTP) for LLM tools.

## Overview

This project explores connecting AI models to economic data through a layered architecture, with infrastructure defined in Bicep and deployed via the [Azure Developer CLI](https://learn.microsoft.com/azure/developer/azure-developer-cli/) (`azd`).

## Architecture

| Layer | Stack | Hosting | Role |
|-------|--------|---------|------|
| **Data** | C# (.NET 10), EF Core | Azure App Service | REST API and persistent storage in Azure SQL |
| **Intelligence** | Python, MCP | Azure Functions | MCP tools over HTTP to the API |

Infrastructure details: [`infra/main.bicep`](infra/main.bicep) (SQL Basic, Linux App Service, Consumption Functions, Storage, Log Analytics, Application Insights).

## Run locally

**API (SQL Server / LocalDB):** set `ConnectionStrings:ZarFlowDb`, then `dotnet run --project src/EconomicDataService`.

**MCP:** `cd src/McpOrchestrator`, set `API_BASE_URL` to the API base URL, then `python server.py` (listens on `http://127.0.0.1:8765/mcp`).

## Endpoints

### Azure (deployed)

Run **`azd show`** for the current **api** and **mcp-server** HTTPS bases and an Azure Portal link to the resource group. Host names include a unique suffix per resource group.

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

Deployments use [`azure.yaml`](azure.yaml) and `azd`.

### Prerequisites

- Azure subscription with permissions to create the resources in [`infra/main.bicep`](infra/main.bicep)
- [Azure Developer CLI](https://learn.microsoft.com/azure/developer/azure-developer-cli/install-azd) — `azd auth login`
- .NET 10 SDK and Python 3.11+ for local builds

### First-time provision

From the repository root:

1. `azd init` if you do not yet have a local `azd` environment (choose a name you will reuse).
2. Set the Bicep SQL administrator password: `azd env set adminPassword "<your-strong-password>"`
3. Run **`azd up`** — provisions infrastructure, deploys the API and MCP Function app, and injects settings. EF Core migrations run on API startup against the deployed database.

### Deploy application updates only

```bash
azd deploy --no-prompt
```

Show endpoints and portal link:

```bash
azd show
```

### GitHub Actions

Workflow [`.github/workflows/azure-dev.yml`](.github/workflows/azure-dev.yml) runs `azd deploy` on push to `main` and **workflow_dispatch**. Set repository variable **`AZURE_ENV_NAME`** to your `azd` environment name, and secrets **`AZURE_CLIENT_ID`**, **`AZURE_TENANT_ID`**, **`AZURE_SUBSCRIPTION_ID`** for [azure/login](https://github.com/Azure/login) (OIDC).

CI: [`.github/workflows/ci.yml`](.github/workflows/ci.yml) (`dotnet test`, `pytest`).

### Security and configuration

- No secrets committed: use `.gitignore`, user secrets locally, and `azd` / Key Vault references for deployment (see `infra` and environment config).
- Connection strings for the API are applied via App Service settings from Bicep.

### Troubleshooting

- **`403 Site Disabled` during API deploy** — Check the **api** App Service in the portal (**Overview → Status**). If **Stopped**, click **Start** and retry `azd deploy`. If **QuotaExceeded**, Free (F1) quota is exhausted in that region—**scale up** the plan (e.g. B1), reduce other Free-tier usage, or use another subscription.
- **`az` vs `azd`** — `azd` uses its own login; the Azure CLI needs `az login` separately.

## Tests

- `dotnet test`
- `cd src/McpOrchestrator && pytest`

## Project structure

```
serverless-mcp-orchestrator/
├── infra/main.bicep          # Azure resources
├── azure.yaml                # azd services
├── src/EconomicDataService/  # .NET API
├── src/McpOrchestrator/      # Python Functions + MCP
└── src/EconomicDataService.Tests/
```
