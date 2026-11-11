<#
.SYNOPSIS
  Lets the GitHub workflow "Deploy test site" sign in to Azure.

.DESCRIPTION
  GitHub presents an identity ("subject") when it signs in to Azure. Newer repositories use a form that
  includes ID numbers, e.g. repo:paulrijk@254931689/Farmersample@1451541256:ref:refs/heads/main.
  Azure only accepts a subject that is registered exactly. This script registers the subject shown in
  the workflow's error message (AADSTS700213 ... presented assertion subject '...').

  Run after `az login`:
      powershell -ExecutionPolicy Bypass -File deploy\azure\fix-github-login.ps1
#>
param(
  [string] $AppId = '16f4ba9a-d168-4e82-9e2f-0b1967c484f8',
  [string] $Subject = 'repo:paulrijk@254931689/Farmersample@1451541256:ref:refs/heads/main',
  [string] $Name = 'github-main-ids'
)
$ErrorActionPreference = 'Stop'
$cred = @{ name = $Name; issuer = 'https://token.actions.githubusercontent.com'; subject = $Subject; audiences = @('api://AzureADTokenExchange') } | ConvertTo-Json -Compress
$file = New-TemporaryFile
Set-Content -Path $file -Value $cred
az ad app federated-credential create --id $AppId --parameters "@$file" | Out-Null
$ok = $LASTEXITCODE -eq 0
Remove-Item $file
if (-not $ok) { throw "Azure refused the change. If it says the credential already exists, it is already registered." }
Write-Host "Registered. Credentials on this app now:" -ForegroundColor Green
az ad app federated-credential list --id $AppId --query "[].{name:name, subject:subject}" -o table
Write-Host "`nNow re-run the failed 'Deploy test site' run in GitHub > Actions (Re-run jobs)."
