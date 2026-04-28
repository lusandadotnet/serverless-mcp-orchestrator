# Serverless MCP Orchestrator

> **Project Status: Successful Proof of Concept (Archived)**
> *Note: The live Azure endpoints for this project have been spun down to conserve cloud costs. The architecture successfully demonstrated the integration of AI models with South African macro data. The repository remains available for local development and architectural reference.*

A cloud-native stack designed to connect AI models seamlessly to South African macroeconomic data. It combines an **ASP.NET Core 10** REST API (hosted on Azure App Service with Azure SQL) with a **FastMCP** Python layer deployed on **Azure Functions** (streamable HTTP) to serve as LLM tools.

## Overview

This project explores a layered, serverless architecture for bridging the gap between raw economic data and artificial intelligence. All infrastructure is defined as code using Bicep and deployed via the [Azure Developer CLI (`azd`)](https://learn.microsoft.com/azure/developer/azure-developer-cli/).

## Architecture

| Layer | Technology Stack | Hosting Environment | Primary Role |
|-------|-------------------|---------------------|--------------|
| **Data** | C# (.NET 10), EF Core | Azure App Service | REST API and persistent storage (Azure SQL) |
| **Intelligence** | Python, FastMCP | Azure Functions | Exposes MCP tools over HTTP to the API |

**Infrastructure Details:** The infrastructure map is located at [`infra/main.bicep`](infra/main.bicep) and provisions the following: 
* SQL Server (Basic tier)
* Linux App Service
* Azure Functions (Consumption plan)
* Azure Storage
* Log Analytics & Application Insights

## Running Locally

To spin up the PoC on your local machine:

**1. API (SQL Server / LocalDB)**
* Ensure your `ConnectionStrings:ZarFlowDb` is set appropriately in your configuration.
* Run the API service:
  ```bash
  dotnet run --project src/EconomicDataService
  ```

**2. MCP Server**
* Navigate to the MCP directory:
  ```bash
  cd src/McpOrchestrator
  ```
* Set the `API_BASE_URL` environment variable to match your local .NET API base URL.
* Start the Python server (listens on `http://127.0.0.1:8765/mcp` by default):
  ```bash
  python server.py
  ```

## API Reference

### Azure Deployments (Historical)

When deployed via **`azd show`**, the CLI provisions unique suffixes per resource group. Below are *example* base URLs from the successful `sarb-orchestrator-99` environment testing phase:

| Service | Example Base URL |
|---------|----------|
| **REST API** | `https://api-zarflow-nrhuttfhony7o.azurewebsites.net/` |
| **MCP Server** | `https://func-mcp-server-nrhuttfhony7o.azurewebsites.net/mcp` |

### REST API Paths (`/api/v1`)

The core .NET service exposes the following endpoints for macro data operations:

| Method | Path | Description |
|--------|------|-------------|
| **GET** | `/api/v1/indicators/summary` | Retrieves a high-level summary of all economic indicators. |
| **GET** | `/api/v1/exchange-rates/latest` | Fetches the most recent ZAR exchange rates. |
| **POST** | `/api/v1/exchange-rates` | Ingests new exchange rate data. |
| **GET** | `/api/v1/inflation/latest` | Fetches the most recent CPI/inflation readings. |
| **POST** | `/api/v1/inflation/readings` | Ingests new inflation data. |

> **Note:** > * **Legacy route:** `GET /api/indicators` maps to the same summary as `/api/v1/indicators/summary`.
> * **OpenAPI/Swagger:** In the `Development` environment only, the OpenAPI specification is mapped at `/openapi/v1.json` (refer to `Program.cs` for implementation details).
