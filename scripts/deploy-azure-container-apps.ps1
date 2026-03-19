param(
    [Parameter(Mandatory = $true)]
    [string]$ResourceGroup,

    [Parameter(Mandatory = $true)]
    [string]$Location,

    [Parameter(Mandatory = $true)]
    [string]$AcrName,

    [Parameter(Mandatory = $true)]
    [string]$ContainerAppsEnvironment,

    [Parameter(Mandatory = $true)]
    [string]$BackendAppName,

    [Parameter(Mandatory = $true)]
    [string]$FrontendAppName,

    [Parameter(Mandatory = $true)]
    [string]$PostgresServerName,

    [Parameter(Mandatory = $true)]
    [string]$PostgresDatabaseName,

    [Parameter(Mandatory = $true)]
    [string]$PostgresAdminUser,

    [Parameter(Mandatory = $true)]
    [string]$PostgresAdminPassword,

    [Parameter(Mandatory = $true)]
    [string]$JwtKey,

    [string]$BackendImageTag = "1",
    [string]$FrontendImageTag = "1",
    [string]$PostgresSku = "Standard_B1ms",
    [string]$PostgresTier = "Burstable"
)

$ErrorActionPreference = "Stop"

function Require-Command {
    param([string]$Name)

    if (-not (Get-Command $Name -ErrorAction SilentlyContinue)) {
        throw "Required command '$Name' was not found in PATH."
    }
}

Require-Command az
Require-Command podman

function Ensure-AzureLogin {
    try {
        az account show | Out-Null
    }
    catch {
        Write-Host "==> Logging into Azure"
        az login | Out-Null
    }
}

function Ensure-ProviderRegistered {
    param([string]$Namespace)

    $state = az provider show --namespace $Namespace --query registrationState -o tsv 2>$null
    if ($state -ne "Registered") {
        Write-Host "==> Registering Azure resource provider $Namespace"
        az provider register --namespace $Namespace --wait | Out-Null
    }
}

$repoRoot = Split-Path -Parent $PSScriptRoot

Ensure-AzureLogin
az extension add --name containerapp --upgrade | Out-Null

Ensure-ProviderRegistered "Microsoft.App"
Ensure-ProviderRegistered "Microsoft.OperationalInsights"
Ensure-ProviderRegistered "Microsoft.ContainerRegistry"
Ensure-ProviderRegistered "Microsoft.DBforPostgreSQL"

Write-Host "==> Creating resource group"
az group create --name $ResourceGroup --location $Location | Out-Null

Write-Host "==> Creating Azure Container Registry"
az acr create --resource-group $ResourceGroup --name $AcrName --sku Standard | Out-Null
az acr update --name $AcrName --admin-enabled true | Out-Null

Write-Host "==> Creating PostgreSQL Flexible Server"
az postgres flexible-server create `
    --resource-group $ResourceGroup `
    --name $PostgresServerName `
    --location $Location `
    --admin-user $PostgresAdminUser `
    --admin-password $PostgresAdminPassword `
    --sku-name $PostgresSku `
    --tier $PostgresTier `
    --public-access 0.0.0.0 `
    --storage-size 32 | Out-Null

az postgres flexible-server db create `
    --resource-group $ResourceGroup `
    --server-name $PostgresServerName `
    --database-name $PostgresDatabaseName | Out-Null

Write-Host "==> Creating Container Apps environment"
az containerapp env create `
    --name $ContainerAppsEnvironment `
    --resource-group $ResourceGroup `
    --location $Location | Out-Null

$loginServer = az acr show --name $AcrName --query loginServer -o tsv
$acrUsername = az acr credential show --name $AcrName --query username -o tsv
$acrPassword = az acr credential show --name $AcrName --query passwords[0].value -o tsv

if ([string]::IsNullOrWhiteSpace($loginServer)) {
    throw "Azure Container Registry login server could not be resolved."
}

if ([string]::IsNullOrWhiteSpace($acrUsername) -or [string]::IsNullOrWhiteSpace($acrPassword)) {
    throw "Azure Container Registry credentials could not be resolved."
}

Write-Host "==> Logging Podman into ACR"
$acrPassword | podman login $loginServer --username $acrUsername --password-stdin

$backendImage = "$loginServer/pft-api:$BackendImageTag"
$frontendImage = "$loginServer/pft-web:$FrontendImageTag"

Write-Host "==> Building backend image"
podman build `
    --file "$repoRoot\backend\PersonalFinanceTracker.Api\Containerfile" `
    --tag $backendImage `
    $repoRoot

Write-Host "==> Pushing backend image"
podman push $backendImage

$connectionString = "Host=$PostgresServerName.postgres.database.azure.com;Port=5432;Database=$PostgresDatabaseName;Username=$PostgresAdminUser;Password=$PostgresAdminPassword;SSL Mode=Require;Trust Server Certificate=true"

Write-Host "==> Deploying backend Container App"
az containerapp create `
    --name $BackendAppName `
    --resource-group $ResourceGroup `
    --environment $ContainerAppsEnvironment `
    --image $backendImage `
    --ingress external `
    --target-port 8080 `
    --registry-server $loginServer `
    --registry-username $acrUsername `
    --registry-password $acrPassword `
    --secrets connstr="$connectionString" jwtkey="$JwtKey" `
    --env-vars `
        ConnectionStrings__DefaultConnection=secretref:connstr `
        Jwt__Issuer=PersonalFinanceTracker `
        Jwt__Audience=PersonalFinanceTrackerUsers `
        Jwt__Key=secretref:jwtkey | Out-Null

$backendFqdn = az containerapp show `
    --name $BackendAppName `
    --resource-group $ResourceGroup `
    --query properties.configuration.ingress.fqdn `
    -o tsv

if ([string]::IsNullOrWhiteSpace($backendFqdn)) {
    throw "Backend Container App FQDN could not be resolved."
}

$backendApiBaseUrl = "https://$backendFqdn/api"

Write-Host "==> Building frontend image"
podman build `
    --file "$repoRoot\frontend\Containerfile" `
    --build-arg "VITE_API_BASE_URL=$backendApiBaseUrl" `
    --tag $frontendImage `
    $repoRoot

Write-Host "==> Pushing frontend image"
podman push $frontendImage

Write-Host "==> Deploying frontend Container App"
az containerapp create `
    --name $FrontendAppName `
    --resource-group $ResourceGroup `
    --environment $ContainerAppsEnvironment `
    --image $frontendImage `
    --ingress external `
    --target-port 80 `
    --registry-server $loginServer `
    --registry-username $acrUsername `
    --registry-password $acrPassword | Out-Null

$frontendFqdn = az containerapp show `
    --name $FrontendAppName `
    --resource-group $ResourceGroup `
    --query properties.configuration.ingress.fqdn `
    -o tsv

if ([string]::IsNullOrWhiteSpace($frontendFqdn)) {
    throw "Frontend Container App FQDN could not be resolved."
}

$frontendOrigin = "https://$frontendFqdn"

Write-Host "==> Updating backend CORS to allow frontend origin"
az containerapp update `
    --name $BackendAppName `
    --resource-group $ResourceGroup `
    --set-env-vars "Cors__AllowedOrigins__0=$frontendOrigin" | Out-Null

Write-Host ""
Write-Host "Deployment complete."
Write-Host "Frontend: $frontendOrigin"
Write-Host "Backend:  https://$backendFqdn"
Write-Host "Swagger:  https://$backendFqdn/swagger"
