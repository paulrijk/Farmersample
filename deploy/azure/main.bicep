// FarmerSample test environment on Azure.
//
// Deployed in two passes by setup.ps1:
//   1. imageTag = ''   → registry, database, storage, logging, Container Apps environment
//   2. imageTag = 'x'  → the site itself (gateway + API + worker in one container app) and the migration job
//
// The site is one container app with three containers that talk over localhost, behind a shared
// site password. Split into separate apps when production needs to scale them independently.

@description('Name prefix for all resources.')
param name string = 'farmersample'

@description('Azure region. Use an EU region: buyer data and the EU entity are in the EU.')
param location string = resourceGroup().location

@description('Image tag to run. Leave empty on the first pass (no images exist yet).')
param imageTag string = ''

@secure()
@description('PostgreSQL administrator password (letters and digits only).')
param dbPassword string

@description('User name for the site-wide password box.')
param siteUser string = 'farmersample'

@secure()
@description('Shared password for the whole site (give this to the EDB, Saj and invited buyers).')
param sitePassword string

@secure()
@description('Password for the seeded back-office accounts (admin@, qc@, field@farmersample.local).')
param staffPassword string

@description('Load the fictional demo farms and lots.')
param seedDemoData bool = true

@description('Optional custom domain, e.g. test.farmersample.com. QR codes and e-mails point here when set.')
param customDomain string = ''

@description('Optional mail server. Leave empty to run without outgoing e-mail.')
param smtpHost string = ''
param smtpPort int = 587
param smtpUser string = ''
@secure()
param smtpPassword string = ''
param smtpFrom string = 'FarmerSample <no-reply@farmersample.com>'
param staffInbox string = ''

var suffix = uniqueString(resourceGroup().id)
var deployApps = !empty(imageTag)
var acrPullRole = subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '7f951dda-4ed3-4680-a7ca-43fe172d538d')

// ---------- identity + registry ----------
resource identity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' = {
  name: '${name}-identity'
  location: location
}

resource registry 'Microsoft.ContainerRegistry/registries@2023-07-01' = {
  name: take('${name}${suffix}', 50)
  location: location
  sku: { name: 'Basic' }
  properties: { adminUserEnabled: false }
}

resource pull 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(registry.id, identity.id, acrPullRole)
  scope: registry
  properties: {
    principalId: identity.properties.principalId
    principalType: 'ServicePrincipal'
    roleDefinitionId: acrPullRole
  }
}

// ---------- logging + Container Apps environment ----------
resource logs 'Microsoft.OperationalInsights/workspaces@2022-10-01' = {
  name: '${name}-logs'
  location: location
  properties: { sku: { name: 'PerGB2018' }, retentionInDays: 30 }
}

resource environment 'Microsoft.App/managedEnvironments@2024-03-01' = {
  name: '${name}-env'
  location: location
  properties: {
    appLogsConfiguration: {
      destination: 'log-analytics'
      logAnalyticsConfiguration: { customerId: logs.properties.customerId, sharedKey: logs.listKeys().primarySharedKey }
    }
  }
}

// ---------- database ----------
resource postgres 'Microsoft.DBforPostgreSQL/flexibleServers@2024-08-01' = {
  name: '${name}-db-${suffix}'
  location: location
  sku: { name: 'Standard_B1ms', tier: 'Burstable' }
  properties: {
    version: '17'
    administratorLogin: 'fsadmin'
    administratorLoginPassword: dbPassword
    storage: { storageSizeGB: 32 }
    backup: { backupRetentionDays: 7, geoRedundantBackup: 'Disabled' }
    highAvailability: { mode: 'Disabled' }
  }

  resource database 'databases' = {
    name: 'farmersample'
  }

  // Test environment: reachable from Azure services only (the container app). Production moves to a private network.
  resource allowAzure 'firewallRules' = {
    name: 'AllowAzureServices'
    properties: { startIpAddress: '0.0.0.0', endIpAddress: '0.0.0.0' }
  }
}

// ---------- blob storage (farm photos; private) ----------
resource storage 'Microsoft.Storage/storageAccounts@2023-01-01' = {
  name: take('${name}st${suffix}', 24)
  location: location
  sku: { name: 'Standard_LRS' }
  kind: 'StorageV2'
  properties: { allowBlobPublicAccess: false, minimumTlsVersion: 'TLS1_2', supportsHttpsTrafficOnly: true }
}

var dbConnection = 'Host=${postgres.properties.fullyQualifiedDomainName};Port=5432;Database=farmersample;Username=fsadmin;Password=${dbPassword};Ssl Mode=Require'
var blobConnection = 'DefaultEndpointsProtocol=https;AccountName=${storage.name};AccountKey=${storage.listKeys().keys[0].value};EndpointSuffix=${az.environment().suffixes.storage}'
var publicBaseUrl = empty(customDomain) ? 'https://${name}.${environment.properties.defaultDomain}' : 'https://${customDomain}'
var registryConfig = [ { server: registry.properties.loginServer, identity: identity.id } ]
var userIdentity = { type: 'UserAssigned', userAssignedIdentities: { '${identity.id}': {} } }

// ---------- migration job: run before each new version takes traffic ----------
resource migrator 'Microsoft.App/jobs@2024-03-01' = if (deployApps) {
  name: '${name}-migrator'
  location: location
  identity: userIdentity
  dependsOn: [ pull ]
  properties: {
    environmentId: environment.id
    configuration: {
      triggerType: 'Manual'
      replicaTimeout: 600
      replicaRetryLimit: 1
      manualTriggerConfig: { parallelism: 1, replicaCompletionCount: 1 }
      registries: registryConfig
      secrets: [
        { name: 'db', value: dbConnection }
        { name: 'staff-password', value: staffPassword }
      ]
    }
    template: {
      containers: [
        {
          name: 'migrator'
          image: '${registry.properties.loginServer}/farmersample-migrator:${imageTag}'
          resources: { cpu: json('0.5'), memory: '1Gi' }
          env: [
            { name: 'ConnectionStrings__Default', secretRef: 'db' }
            { name: 'Seed__DemoData', value: string(seedDemoData) }
            { name: 'Seed__DemoPassword', secretRef: 'staff-password' }
          ]
        }
      ]
    }
  }
}

// ---------- the site: gateway + API + worker ----------
resource site 'Microsoft.App/containerApps@2024-03-01' = if (deployApps) {
  name: name
  location: location
  identity: userIdentity
  dependsOn: [ pull ]
  properties: {
    environmentId: environment.id
    configuration: {
      activeRevisionsMode: 'Single'
      ingress: { external: true, targetPort: 80, transport: 'auto', allowInsecure: false }
      registries: registryConfig
      secrets: [
        { name: 'db', value: dbConnection }
        { name: 'blobs', value: blobConnection }
        { name: 'site-password', value: sitePassword }
        { name: 'smtp-password', value: empty(smtpPassword) ? 'none' : smtpPassword }
      ]
    }
    template: {
      scale: { minReplicas: 1, maxReplicas: 1 }
      containers: [
        {
          name: 'gateway'
          image: '${registry.properties.loginServer}/farmersample-gateway:${imageTag}'
          resources: { cpu: json('0.25'), memory: '0.5Gi' }
          env: [
            { name: 'API_UPSTREAM', value: 'localhost:8080' }
            { name: 'SITE_USER', value: siteUser }
            { name: 'SITE_PASSWORD', secretRef: 'site-password' }
          ]
        }
        {
          name: 'api'
          image: '${registry.properties.loginServer}/farmersample-api:${imageTag}'
          resources: { cpu: json('0.5'), memory: '1Gi' }
          env: [
            { name: 'ASPNETCORE_ENVIRONMENT', value: 'Production' }
            { name: 'ASPNETCORE_HTTP_PORTS', value: '8080' }
            { name: 'ConnectionStrings__Default', secretRef: 'db' }
            { name: 'ConnectionStrings__Blobs', secretRef: 'blobs' }
            { name: 'PublicBaseUrl', value: publicBaseUrl }
            { name: 'Auth__RequireStaffMfa', value: 'true' }
          ]
        }
        {
          name: 'worker'
          image: '${registry.properties.loginServer}/farmersample-worker:${imageTag}'
          resources: { cpu: json('0.25'), memory: '0.5Gi' }
          env: [
            { name: 'DOTNET_ENVIRONMENT', value: 'Production' }
            { name: 'ConnectionStrings__Default', secretRef: 'db' }
            { name: 'PublicBaseUrl', value: publicBaseUrl }
            { name: 'Smtp__Host', value: smtpHost }
            { name: 'Smtp__Port', value: string(smtpPort) }
            { name: 'Smtp__EnableSsl', value: 'true' }
            { name: 'Smtp__UserName', value: smtpUser }
            { name: 'Smtp__Password', secretRef: 'smtp-password' }
            { name: 'Smtp__From', value: smtpFrom }
            { name: 'Smtp__StaffInbox', value: staffInbox }
          ]
        }
      ]
    }
  }
}

output registryName string = registry.name
output siteUrl string = publicBaseUrl
output defaultHostName string = '${name}.${environment.properties.defaultDomain}'
output siteName string = name
output migratorName string = '${name}-migrator'
