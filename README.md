# Serverless MCP Orchestrator

<h2>Overview</h2>

This project is a Proof of Concept (PoC) exploring a cloud-native architecture to analyze South African Reserve Bank (SARB) mandates. It demonstrates how to connect AI models to real-time economic data through a layered architecture, experimenting with a zero-secrets security approach and automated infrastructure provisioning.

<h2>Architecture</h2>

The system is composed of two primary layers:

<table>
<tr>
<th>Layer</th>
<th>Technology Stack</th>
<th>Hosting</th>
<th>Purpose</th>
</tr>
<tr>
<td><strong>Data Layer</strong></td>
<td>C# (.NET 10), Entity Framework Core</td>
<td>Azure App Service</td>
<td>RESTful API for SARB economic data with persistent storage in Azure SQL Database</td>
</tr>
<tr>
<td><strong>Intelligence Layer</strong></td>
<td>Python, Model Context Protocol (MCP)</td>
<td>Azure Functions (Serverless)</td>
<td>AI integration layer enabling LLM access to economic data through MCP protocol</td>
</tr>
</table>

<h2>Key Features</h2>

<h3>Security-First Design</h3>

- **Zero-Secrets Policy**: No hardcoded credentials, API keys, or connection strings in source code
- **Infrastructure-Level Secret Injection**: Database connection strings and sensitive configuration injected directly into Azure environment variables via Bicep templates
- **Version Control Hygiene**: Strict `.gitignore` configuration preventing accidental exposure of `appsettings.json` and other sensitive files

<h3>Infrastructure as Code</h3>

- **Bicep Templates**: Complete infrastructure definition including App Service, Azure Functions, SQL Database, and networking components
- **Azure Developer CLI Integration**: Single-command deployment pipeline (`azd up`) for consistent environment provisioning
- **Idempotent Deployments**: Infrastructure updates can be safely re-applied without manual intervention

<h3>Serverless Intelligence</h3>

- **Model Context Protocol (MCP)**: Standards-based interface for AI model integration with external data sources
- **Event-Driven Processing**: Azure Functions-based compute for cost-effective, auto-scaling AI workloads
- **Claude Integration**: Production-ready connection to Anthropic's Claude API for mandate analysis

<h2>Technical Stack</h2>

<table>
<tr>
<th>Component</th>
<th>Technology</th>
<th>Version/Details</th>
</tr>
<tr>
<td>Data API</td>
<td>C# / ASP.NET Core</td>
<td>.NET 10</td>
</tr>
<tr>
<td>ORM</td>
<td>Entity Framework Core</td>
<td>Latest stable</td>
</tr>
<tr>
<td>Database</td>
<td>Azure SQL Database</td>
<td>Managed PaaS</td>
</tr>
<tr>
<td>AI Integration</td>
<td>Python</td>
<td>3.11+</td>
</tr>
<tr>
<td>AI Protocol</td>
<td>Model Context Protocol (MCP)</td>
<td>Latest specification</td>
</tr>
<tr>
<td>Serverless Compute</td>
<td>Azure Functions</td>
<td>Python runtime</td>
</tr>
<tr>
<td>IaC</td>
<td>Bicep</td>
<td>Azure native</td>
</tr>
<tr>
<td>Deployment CLI</td>
<td>Azure Developer CLI (azd)</td>
<td>Latest</td>
</tr>
</table>

<h2>Deployment</h2>

<h3>Prerequisites</h3>

- Azure subscription with appropriate permissions
- Azure Developer CLI installed
- .NET 10 SDK
- Python 3.11 or higher
- Azure CLI (authenticated)

<h3>One-Command Deployment</h3>

```bash
azd up
```

This command orchestrates:
1. Infrastructure provisioning via Bicep templates
2. Database schema deployment through EF Core migrations
3. API deployment to Azure App Service
4. Azure Functions deployment with MCP integration
5. Environment variable injection for all services

<h3>Environment Configuration</h3>

All sensitive configuration is managed through Azure-native mechanisms:
- Connection strings: Injected via Bicep into App Service configuration
- API endpoints: Configured through Azure Functions application settings
- AI model credentials: Stored in Azure Key Vault (referenced by Bicep)

<h2>Use Case: SARB Mandate Analysis</h2>

The system enables AI-powered analysis of South African Reserve Bank mandates by:

1. **Data Ingestion**: C# API retrieves and stores economic indicators, policy statements, and mandate documentation
2. **Contextual Access**: Python MCP server exposes structured data to AI models through standardized protocol
3. **Intelligent Analysis**: Claude or other LLMs query real-time SARB data to generate insights, comparisons, and policy assessments
4. **Scalable Processing**: Serverless architecture handles variable query loads without manual scaling

<h2>Security Considerations</h2>

<h3>Implemented Controls</h3>

- No secrets in source control (enforced via `.gitignore`)
- Managed identities for Azure service-to-service authentication
- Connection string injection at deployment time
- Azure SQL firewall rules limiting access to Azure services only


<h2>Development Workflow</h2>

<h3>Local Development</h3>

1. Clone repository
2. Configure local `appsettings.Development.json` (not committed to Git)
3. Run database migrations: `dotnet ef database update`
4. Start API: `dotnet run` in API project

<h3>CI/CD Pipeline</h3>

The Azure Developer CLI supports integration with GitHub Actions for automated deployments:

```bash
azd pipeline config
```

<h2>Project Structure</h2>

```
serverless-mcp-orchestrator/
├── infra/                          # Bicep infrastructure templates
│   ├── main.bicep                  # Main infrastructure definition
│   └── modules/                    # Modular Bicep components
├── src/
│   ├── EconomicDataService/                    # C# .NET 10 API
│   │   └── Data/                   # EF Core DbContext
│   └── IntelligenceLayer/          # Python Azure Functions
│       ├── server.py            # MCP protocol implementation
├── .gitignore                      # Strict secret exclusion
└── azure.yaml                      # azd configuration
```
