param location string = resourceGroup().location
param sqlServerName string = 'sql-zarflow-${uniqueString(resourceGroup().id)}'

@secure()
param adminPassword string

resource sqlServer 'Microsoft.Sql/servers@2023-05-01-preview' = {
  name: sqlServerName
  location: location
  properties: {
    administratorLogin: 'zaradmin'
    administratorLoginPassword: adminPassword 
  }
}

resource sqlDatabase 'Microsoft.Sql/servers/databases@2023-05-01-preview' = {
  parent: sqlServer
  name: 'ZarFlowDb'
  location: location
  sku: {
    name: 'Basic' 
    tier: 'Basic'
  }
}

resource allowLocalIp 'Microsoft.Sql/servers/firewallRules@2023-05-01-preview' = {
  parent: sqlServer
  name: 'AllowAllAzureIps'
  properties: {
    startIpAddress: '0.0.0.0'
    endIpAddress: '0.0.0.0'
  }
}

output sqlServerName string = sqlServer.properties.fullyQualifiedDomainName
output databaseName string = sqlDatabase.name


// --- NEW: App Service (The Web Server) ---
param webAppName string = 'api-zarflow-${uniqueString(resourceGroup().id)}'

// 1. The Server Farm (F1 Free Tier for Students)
resource appServicePlan 'Microsoft.Web/serverfarms@2022-09-01' = {
  name: 'asp-zarflow'
  location: location
  sku: {
    name: 'F1' 
    tier: 'Free'
  }
  properties: {
    reserved: true // Required for Linux hosting
  }
}

// 2. The Web App
resource webApp 'Microsoft.Web/sites@2022-09-01' = {
  name: webAppName
  location: location
  tags: { 'azd-service-name': 'api' } // <-- The crucial link for Azure Developer CLI
  properties: {
    serverFarmId: appServicePlan.id
    siteConfig: {
      linuxFxVersion: 'DOTNETCORE|8.0' 
      appSettings: [
        {
          name: 'ConnectionStrings__ZarFlowDb'
          value: 'Server=tcp:${sqlServer.properties.fullyQualifiedDomainName},1433;Initial Catalog=${sqlDatabase.name};Persist Security Info=False;User ID=zaradmin;Password=${adminPassword};MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;'
        }
      ]
    }
  }
}

output apiEndpoint string = 'https://${webApp.properties.defaultHostName}'



// 1. Storage Account (Required for Azure Functions to store execution state)
param storageAccountName string = 'stzarflow${uniqueString(resourceGroup().id)}'

resource storageAccount 'Microsoft.Storage/storageAccounts@2022-09-01' = {
  name: storageAccountName
  location: location
  sku: {
    name: 'Standard_LRS' 
  }
  kind: 'StorageV2'
}

// 2. Serverless Function Plan (Y1 Dynamic = Consumption/Serverless)
resource functionPlan 'Microsoft.Web/serverfarms@2022-09-01' = {
  name: 'plan-mcp-function'
  location: location
  sku: {
    name: 'Y1' 
    tier: 'Dynamic'
  }
  properties: {
    reserved: true 
  }
}

// 3. The Python Function App
param functionAppName string = 'func-mcp-server-${uniqueString(resourceGroup().id)}'

resource functionApp 'Microsoft.Web/sites@2022-09-01' = {
  name: functionAppName
  location: location
  kind: 'functionapp,linux'
  tags: { 'azd-service-name': 'mcp-server' } 
  properties: {
    serverFarmId: functionPlan.id
    siteConfig: {
      linuxFxVersion: 'python|3.11' // Ensures it runs a Python environment
      appSettings: [
        {
          name: 'AzureWebJobsStorage'
          value: 'DefaultEndpointsProtocol=https;AccountName=${storageAccount.name};AccountKey=${storageAccount.listKeys().keys[0].value};EndpointSuffix=core.windows.net'
        }
        {
          name: 'FUNCTIONS_EXTENSION_VERSION'
          value: '~4'
        }
        {
          name: 'FUNCTIONS_WORKER_RUNTIME'
          value: 'python'
        }
        {
          
          name: 'API_BASE_URL'
          value: 'https://${webApp.properties.defaultHostName}' 
        }
      ]
    }
  }
}

output functionEndpoint string = 'https://${functionApp.properties.defaultHostName}'



resource logAnalytics 'Microsoft.OperationalInsights/workspaces@2021-06-01' = {
  name: 'log-zarflow-${uniqueString(resourceGroup().id)}'
  location: location
  properties: {
    sku: {
      name: 'PerGB2018'
    }
  }
}

resource appInsights 'Microsoft.Insights/components@2020-02-02' = {
  name: 'ai-zarflow-${uniqueString(resourceGroup().id)}'
  location: location
  kind: 'web'
  properties: {
    Application_Type: 'web'
    WorkspaceResourceId: logAnalytics.id
  }
}
