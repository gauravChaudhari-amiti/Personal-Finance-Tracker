# Azure + Podman Deployment Runbook

This project is deployed as:

- `frontend` -> Azure Container Apps (`nginx` serving the Vite build)
- `backend` -> Azure Container Apps (`ASP.NET Core`)
- `database` -> Azure Database for PostgreSQL Flexible Server
- `images` -> Azure Container Registry

This file captures the actual steps and fixes used during deployment so you can repeat them without rebuilding the process from terminal history.

## Files used for deployment

- `backend/PersonalFinanceTracker.Api/Containerfile`
- `frontend/Containerfile`
- `frontend/nginx.conf`
- `.dockerignore`
- `scripts/deploy-azure-container-apps.ps1`

## Prerequisites

Install on Windows:

```powershell
winget install RedHat.Podman-Desktop
winget install RedHat.Podman
winget install --id Microsoft.AzureCLI -e
```

If Podman machine setup fails because WSL is missing:

```powershell
wsl --install
```

Restart Windows after WSL installation.

## First-time Podman setup

Run once:

```powershell
podman machine init
podman machine start
podman info
```

## Azure login

```powershell
az login
az account show
az extension add --name containerapp --upgrade
```

## Register required Azure resource providers

If these are not registered, deployment will fail while creating ACR, PostgreSQL, or Container Apps.

```powershell
az provider register --namespace Microsoft.App --wait
az provider register --namespace Microsoft.OperationalInsights --wait
az provider register --namespace Microsoft.ContainerRegistry --wait
az provider register --namespace Microsoft.DBforPostgreSQL --wait
```

## Example deployment values

These are the values used in the successful deployment example:

```text
Resource Group:               pft-rg
Location:                     centralindia
Container Apps Environment:   pft-env
Backend App:                  pft-api
Frontend App:                 pft-web
ACR:                          pftacrgauravchaudhari01
PostgreSQL Server:            pftpggauravchaudhari01
PostgreSQL Database:          personal_finance_db
PostgreSQL Admin User:        pftadmin
```

Use your own strong values for:

- PostgreSQL admin password
- JWT key
- Google client ID if you want Google sign-in on the login page
- SMTP settings if you want verification and reset emails to reach real inboxes

## One-shot deployment script

From the repo root in `cmd`:

```cmd
powershell -ExecutionPolicy Bypass -File .\scripts\deploy-azure-container-apps.ps1 ^
  -ResourceGroup "pft-rg" ^
  -Location "centralindia" ^
  -AcrName "pftacrgauravchaudhari01" ^
  -ContainerAppsEnvironment "pft-env" ^
  -BackendAppName "pft-api" ^
  -FrontendAppName "pft-web" ^
  -PostgresServerName "pftpggauravchaudhari01" ^
  -PostgresDatabaseName "personal_finance_db" ^
  -PostgresAdminUser "pftadmin" ^
  -PostgresAdminPassword "REPLACE_WITH_STRONG_PASSWORD" ^
  -JwtKey "REPLACE_WITH_LONG_RANDOM_JWT_KEY" ^
  -GoogleClientId "YOUR_GOOGLE_CLIENT_ID" ^
  -EmailSmtpHost "smtp.example.com" ^
  -EmailSmtpPort "587" ^
  -EmailSmtpUsername "user@example.com" ^
  -EmailSmtpPassword "APP_PASSWORD" ^
  -EmailFromEmail "user@example.com"
```

From PowerShell:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\deploy-azure-container-apps.ps1 `
  -ResourceGroup "pft-rg" `
  -Location "centralindia" `
  -AcrName "pftacrgauravchaudhari01" `
  -ContainerAppsEnvironment "pft-env" `
  -BackendAppName "pft-api" `
  -FrontendAppName "pft-web" `
  -PostgresServerName "pftpggauravchaudhari01" `
  -PostgresDatabaseName "personal_finance_db" `
  -PostgresAdminUser "pftadmin" `
  -PostgresAdminPassword "REPLACE_WITH_STRONG_PASSWORD" `
  -JwtKey "REPLACE_WITH_LONG_RANDOM_JWT_KEY" `
  -GoogleClientId "YOUR_GOOGLE_CLIENT_ID" `
  -EmailSmtpHost "smtp.example.com" `
  -EmailSmtpPort "587" `
  -EmailSmtpUsername "user@example.com" `
  -EmailSmtpPassword "APP_PASSWORD" `
  -EmailFromEmail "user@example.com"
```

## What the script does

- ensures Azure login
- ensures Azure resource providers are registered
- creates the resource group
- creates Azure Container Registry
- creates PostgreSQL Flexible Server
- creates the app database
- creates the Container Apps environment
- builds and pushes backend image with Podman
- deploys the backend container app
- builds and pushes frontend image with the backend API URL
- deploys the frontend container app
- updates backend CORS, frontend base URL, optional Google auth client ID, and optional SMTP email settings

## Important post-deployment database access step

The backend container will not start if PostgreSQL firewall access is missing.

Check PostgreSQL networking:

```cmd
az postgres flexible-server show ^
  --resource-group pft-rg ^
  --name pftpggauravchaudhari01 ^
  --query "{publicNetworkAccess:network.publicNetworkAccess,delegatedSubnet:network.delegatedSubnetResourceId,privateDnsZone:network.privateDnsZoneArmResourceId}" ^
  -o json
```

For the public-access setup used here, add Azure services:

```cmd
az postgres flexible-server firewall-rule create ^
  --resource-group pft-rg ^
  --name pftpggauravchaudhari01 ^
  --rule-name AllowAzureServices ^
  --start-ip-address 0.0.0.0 ^
  --end-ip-address 0.0.0.0
```

Get your laptop public IP:

```cmd
az rest --method get --url https://api.ipify.org?format=json
```

Then add your laptop IP:

```cmd
az postgres flexible-server firewall-rule create ^
  --resource-group pft-rg ^
  --name pftpggauravchaudhari01 ^
  --rule-name AllowMyLaptop ^
  --start-ip-address YOUR_PUBLIC_IP ^
  --end-ip-address YOUR_PUBLIC_IP
```

List rules:

```cmd
az postgres flexible-server firewall-rule list ^
  --resource-group pft-rg ^
  --name pftpggauravchaudhari01 ^
  -o table
```

## Useful verification commands

Check backend app:

```cmd
az containerapp show -n pft-api -g pft-rg --query properties.runningStatus
az containerapp revision list -n pft-api -g pft-rg -o table
az containerapp logs show -n pft-api -g pft-rg --type system --tail 200
```

Check backend Swagger:

```cmd
curl https://pft-api.agreeablewave-6fe11347.centralindia.azurecontainerapps.io/swagger/index.html
```

## Local backend image test before pushing

If Azure backend is unhealthy, run the exact backend image locally to see the real startup error:

```cmd
podman run --rm -p 8080:8080 ^
  -e ConnectionStrings__DefaultConnection="Host=pftpggauravchaudhari01.postgres.database.azure.com;Port=5432;Database=personal_finance_db;Username=pftadmin;Password=REPLACE_WITH_STRONG_PASSWORD;SSL Mode=Require;Trust Server Certificate=true" ^
  -e Jwt__Issuer="PersonalFinanceTracker" ^
  -e Jwt__Audience="PersonalFinanceTrackerUsers" ^
  -e Jwt__Key="REPLACE_WITH_LONG_RANDOM_JWT_KEY" ^
  pftacrgauravchaudhari01.azurecr.io/pft-api:TAG
```

If the container starts correctly, you should see:

```text
Now listening on: http://[::]:8080
Application started.
```

## Podman login for ACR

Do not rely on `az acr login` unless Docker is installed. For Podman, use ACR admin credentials:

```cmd
for /f %i in ('az acr credential show -n pftacrgauravchaudhari01 --query username -o tsv') do set ACR_USER=%i
for /f %i in ('az acr credential show -n pftacrgauravchaudhari01 --query passwords[0].value -o tsv') do set ACR_PASS=%i
echo %ACR_PASS% | podman login pftacrgauravchaudhari01.azurecr.io --username %ACR_USER% --password-stdin
```

## Backend rebuild and redeploy

Example backend rebuild after a code change:

```cmd
podman build -f .\backend\PersonalFinanceTracker.Api\Containerfile -t pftacrgauravchaudhari01.azurecr.io/pft-api:2 .
podman push pftacrgauravchaudhari01.azurecr.io/pft-api:2
az containerapp update ^
  -n pft-api ^
  -g pft-rg ^
  --image pftacrgauravchaudhari01.azurecr.io/pft-api:2
```

Check the new revision:

```cmd
az containerapp revision list -n pft-api -g pft-rg -o table
```

## Frontend rebuild and redeploy

The frontend must be rebuilt whenever the backend API URL changes, because `VITE_API_BASE_URL` is baked in at build time.

Example frontend rebuild:

```cmd
podman build -f .\frontend\Containerfile --build-arg VITE_API_BASE_URL="https://pft-api.agreeablewave-6fe11347.centralindia.azurecontainerapps.io/api" --build-arg VITE_GOOGLE_CLIENT_ID="YOUR_GOOGLE_CLIENT_ID" -t pftacrgauravchaudhari01.azurecr.io/pft-web:4 .
podman push pftacrgauravchaudhari01.azurecr.io/pft-web:4
az containerapp update ^
  -n pft-web ^
  -g pft-rg ^
  --image pftacrgauravchaudhari01.azurecr.io/pft-web:4
```

## Quick redeploy script for daily changes

If you do not want to type the Podman push and Azure update commands every time, use the redeploy script added in `scripts`.

PowerShell script:

- `scripts/redeploy-azure-changes.ps1`

Simple `cmd` wrapper:

- `scripts/redeploy.cmd`

Run from the repo root in `cmd`:

```cmd
scripts\redeploy.cmd
```

That redeploys both backend and frontend using a fresh timestamp tag.

Backend only:

```cmd
scripts\redeploy.cmd backend
```

Frontend only:

```cmd
scripts\redeploy.cmd frontend
```

Frontend or both with Google sign-in enabled:

```cmd
scripts\redeploy.cmd both -GoogleClientId "YOUR_GOOGLE_CLIENT_ID"
scripts\redeploy.cmd both -EmailSmtpHost "smtp.example.com" -EmailSmtpPort "587" -EmailSmtpUsername "user@example.com" -EmailSmtpPassword "APP_PASSWORD" -EmailFromEmail "user@example.com"
```

If you prefer PowerShell directly:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\redeploy-azure-changes.ps1 -Target both
powershell -ExecutionPolicy Bypass -File .\scripts\redeploy-azure-changes.ps1 -Target both -GoogleClientId "YOUR_GOOGLE_CLIENT_ID"
powershell -ExecutionPolicy Bypass -File .\scripts\redeploy-azure-changes.ps1 -Target both -EmailSmtpHost "smtp.example.com" -EmailSmtpPort "587" -EmailSmtpUsername "user@example.com" -EmailSmtpPassword "APP_PASSWORD" -EmailFromEmail "user@example.com"
```

What it does:

- ensures Azure login
- logs Podman into ACR
- builds a fresh image tag based on the current timestamp
- pushes the image
- updates the matching Azure Container App
- prints the final frontend, backend, and Swagger URLs

Defaults are already set for this project:

- resource group: `pft-rg`
- ACR: `pftacrgauravchaudhari01`
- backend app: `pft-api`
- frontend app: `pft-web`

For frontend redeploys, it automatically reads the current backend FQDN and builds with:

- `VITE_API_BASE_URL=https://<backend-fqdn>/api`

If `-GoogleClientId` is passed, it also:

- builds the frontend with `VITE_GOOGLE_CLIENT_ID`
- updates backend `GoogleAuth__ClientId`
- updates backend `Frontend__BaseUrl`

If SMTP settings are passed, it also:

- updates backend `Email__SmtpHost`
- updates backend `Email__SmtpPort`
- updates backend `Email__FromEmail`
- updates backend `Email__FromName`
- updates backend `Email__UseSsl`
- stores SMTP username/password as Container App secrets

## Current live URLs from the successful deployment

- Frontend: `https://pft-web.agreeablewave-6fe11347.centralindia.azurecontainerapps.io`
- Backend: `https://pft-api.agreeablewave-6fe11347.centralindia.azurecontainerapps.io`
- Swagger: `https://pft-api.agreeablewave-6fe11347.centralindia.azurecontainerapps.io/swagger/index.html`

## Common deployment issues

### `MissingSubscriptionRegistration`

Register the Azure providers listed earlier in this document, then rerun deployment.

### Backend revision is `Unhealthy` with `Replicas = 0`

Check system logs:

```cmd
az containerapp logs show -n pft-api -g pft-rg --type system --tail 200
```

If you see PostgreSQL timeout errors:

- confirm `publicNetworkAccess` is enabled
- add `AllowAzureServices`
- add your laptop IP for local testing

### `az acr login` fails with Docker error

Use the Podman login commands shown above instead.

### Frontend works locally but not after deployment

Rebuild the frontend image with the correct backend URL:

- `--build-arg VITE_API_BASE_URL="https://<backend-fqdn>/api"`

If the Google button is missing:

- rebuild with `--build-arg VITE_GOOGLE_CLIENT_ID="YOUR_GOOGLE_CLIENT_ID"`
- or use `scripts\redeploy.cmd both -GoogleClientId "YOUR_GOOGLE_CLIENT_ID"`

### Backend starts locally but fails in Azure

Check:

- PostgreSQL firewall rules
- backend container app secrets/env vars
- backend revision system logs

## Notes

- The backend runs EF migrations on startup.
- The backend also seeds initial data on startup.
- DataProtection key warnings inside the backend container are expected for now and do not block app startup.
- `Resend email verification link` is only for email/password sign-up accounts. It is not needed for Google sign-in accounts.
