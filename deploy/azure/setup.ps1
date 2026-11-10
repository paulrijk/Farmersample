<#
.SYNOPSIS
  Creates the FarmerSample TEST site on Azure, behind one shared site password.

.DESCRIPTION
  Run once from the repository root in PowerShell, after `az login`:

      ./deploy/azure/setup.ps1 -SubscriptionId <id>

  It asks for two passwords (site password, back-office password), builds the images in Azure
  (no local Docker needed), creates everything, runs the database migration and prints the site address.
  Running it again updates the same environment.

  Add -ConnectGitHub to also let GitHub deploy automatically on every push to main
  (needs rights to create an app registration in your Azure tenant).
#>
param(
  [Parameter(Mandatory)] [string] $SubscriptionId,
  [string] $ResourceGroup = 'farmersample-test',
  [string] $Location = 'westeurope',
  [string] $CustomDomain = '',
  [string] $GitHubRepo = 'paulrijk/Farmersample',
  [switch] $ConnectGitHub
)
$ErrorActionPreference = 'Stop'
function Step($text) { Write-Host "`n== $text" -ForegroundColor Green }
function Check { if ($LASTEXITCODE -ne 0) { throw "The last az command failed. Fix the error above and run the script again." } }
function Plain([securestring] $s) { [System.Net.NetworkCredential]::new('', $s).Password }
function NewSecret([int] $length) { -join ((48..57) + (65..90) + (97..122) | Get-Random -Count $length | ForEach-Object { [char] $_ }) }

az account set --subscription $SubscriptionId; Check
foreach ($ns in 'Microsoft.App', 'Microsoft.ContainerRegistry', 'Microsoft.DBforPostgreSQL', 'Microsoft.OperationalInsights', 'Microsoft.Storage') {
  az provider register --namespace $ns --wait | Out-Null
}

Step "Passwords"
$sitePassword  = Plain (Read-Host "Shared SITE password (for the EDB, Saj, invited buyers)" -AsSecureString)
$staffPassword = Plain (Read-Host "BACK-OFFICE password for admin@/qc@/field@farmersample.local (min. 12 characters)" -AsSecureString)
if ($sitePassword.Length -lt 8)   { throw "Choose a site password of at least 8 characters." }
if ($staffPassword.Length -lt 12) { throw "The back-office password needs at least 12 characters." }

# The database password is generated once and kept in a local file that git ignores.
$secretFile = Join-Path $PSScriptRoot '.db-password.local'
if (Test-Path $secretFile) { $dbPassword = (Get-Content $secretFile -Raw).Trim() }
else { $dbPassword = NewSecret 32; Set-Content -Path $secretFile -Value $dbPassword -NoNewline }

$common = @(
  '--resource-group', $ResourceGroup, '--template-file', (Join-Path $PSScriptRoot 'main.bicep'),
  '--parameters', "dbPassword=$dbPassword", "sitePassword=$sitePassword", "staffPassword=$staffPassword", "customDomain=$CustomDomain"
)

Step "1/5 Resource group, registry, database, storage (about 10 minutes the first time)"
az group create --name $ResourceGroup --location $Location | Out-Null; Check
$infra = az deployment group create @common --name infra --parameters imageTag= --query properties.outputs -o json | ConvertFrom-Json; Check
$registry = $infra.registryName.value

Step "2/5 Building the four images in Azure"
$tag = (git rev-parse --short HEAD); if (-not $tag) { $tag = Get-Date -Format 'yyyyMMddHHmm' }
foreach ($p in @(@('api', 'FarmerSample.Api'), @('worker', 'FarmerSample.Worker'), @('migrator', 'FarmerSample.DbMigrator'))) {
  az acr build --registry $registry --image "farmersample-$($p[0]):$tag" --file deploy/dotnet.Dockerfile --build-arg "PROJECT=$($p[1])" . --no-logs; Check
}
az acr build --registry $registry --image "farmersample-gateway:$tag" --file deploy/web.Dockerfile . --no-logs; Check

Step "3/5 Creating the site"
$apps = az deployment group create @common --name apps --parameters "imageTag=$tag" --query properties.outputs -o json | ConvertFrom-Json; Check

Step "4/5 Database migration and demo data"
$execution = az containerapp job start --name $apps.migratorName.value --resource-group $ResourceGroup --query name -o tsv; Check
do {
  Start-Sleep -Seconds 10
  $status = az containerapp job execution show --name $apps.migratorName.value --resource-group $ResourceGroup --job-execution-name $execution --query properties.status -o tsv
  Write-Host "   migration: $status"
} while ($status -eq 'Running' -or $status -eq 'Processing' -or -not $status)
if ($status -ne 'Succeeded') { throw "The migration job ended as '$status'. See its logs in the Azure portal (Container Apps > Jobs > $($apps.migratorName.value))." }
# The API started before the tables existed; restart it so it comes up clean.
$revision = az containerapp show --name $apps.siteName.value --resource-group $ResourceGroup --query properties.latestRevisionName -o tsv
az containerapp revision restart --name $apps.siteName.value --resource-group $ResourceGroup --revision $revision | Out-Null

Step "5/5 Done"
Write-Host "Site:        $($apps.siteUrl.value)   (browser asks for user 'farmersample' + the site password)"
Write-Host "Back office: $($apps.siteUrl.value)/admin   (admin@farmersample.local + the back-office password; sets up two-factor on first sign-in)"
if ($CustomDomain) { Write-Host "Custom domain: point a CNAME for $CustomDomain to $($apps.defaultHostName.value), then add it under Custom domains in the portal." }

if ($ConnectGitHub) {
  Step "Connecting GitHub (automatic deployment on every push to main)"
  $tenant = az account show --query tenantId -o tsv
  $appId = az ad app create --display-name "farmersample-github-deploy" --query appId -o tsv; Check
  az ad sp create --id $appId | Out-Null
  $scope = az group show --name $ResourceGroup --query id -o tsv
  az role assignment create --assignee $appId --role Contributor --scope $scope | Out-Null; Check
  $cred = @{ name = 'github-main'; issuer = 'https://token.actions.githubusercontent.com'; subject = "repo:${GitHubRepo}:ref:refs/heads/main"; audiences = @('api://AzureADTokenExchange') } | ConvertTo-Json -Compress
  $credFile = New-TemporaryFile; Set-Content -Path $credFile -Value $cred
  az ad app federated-credential create --id $appId --parameters "@$credFile" | Out-Null; Remove-Item $credFile
  Write-Host "`nAdd these in GitHub > $GitHubRepo > Settings > Secrets and variables > Actions > Variables:"
  Write-Host "  AZURE_CLIENT_ID        = $appId"
  Write-Host "  AZURE_TENANT_ID        = $tenant"
  Write-Host "  AZURE_SUBSCRIPTION_ID  = $SubscriptionId"
  Write-Host "  AZURE_RESOURCE_GROUP   = $ResourceGroup"
  Write-Host "  AZURE_REGISTRY         = $registry"
  Write-Host "(These are identifiers, not passwords. GitHub signs in with a short-lived token, so no secret is stored.)"
}
