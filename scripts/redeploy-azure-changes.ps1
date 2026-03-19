param(
    [ValidateSet("backend", "frontend", "both")]
    [string]$Target = "both",

    [string]$ResourceGroup = "pft-rg",
    [string]$AcrName = "pftacrgauravchaudhari01",
    [string]$BackendAppName = "pft-api",
    [string]$FrontendAppName = "pft-web",
    [string]$Tag = "",
    [string]$BackendApiBaseUrl = ""
)

$ErrorActionPreference = "Stop"

function Require-Command {
    param([string]$Name)

    if (-not (Get-Command $Name -ErrorAction SilentlyContinue)) {
        throw "Required command '$Name' was not found in PATH."
    }
}

function Ensure-AzureLogin {
    try {
        az account show | Out-Null
    }
    catch {
        Write-Host "==> Logging into Azure"
        az login | Out-Null
    }
}

Require-Command az
Require-Command podman

if ([string]::IsNullOrWhiteSpace($Tag)) {
    $Tag = Get-Date -Format "yyyyMMddHHmmss"
}

$repoRoot = Split-Path -Parent $PSScriptRoot

Ensure-AzureLogin
az extension add --name containerapp --upgrade | Out-Null

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
$acrPassword | podman login $loginServer --username $acrUsername --password-stdin | Out-Null

if ($Target -in @("backend", "both")) {
    $backendImage = "$loginServer/pft-api:$Tag"

    Write-Host "==> Building backend image $backendImage"
    podman build `
        --file "$repoRoot\backend\PersonalFinanceTracker.Api\Containerfile" `
        --tag $backendImage `
        $repoRoot

    Write-Host "==> Pushing backend image"
    podman push $backendImage

    Write-Host "==> Updating backend Container App"
    az containerapp update `
        --name $BackendAppName `
        --resource-group $ResourceGroup `
        --image $backendImage | Out-Null
}

if ($Target -in @("frontend", "both")) {
    if ([string]::IsNullOrWhiteSpace($BackendApiBaseUrl)) {
        $backendFqdn = az containerapp show `
            --name $BackendAppName `
            --resource-group $ResourceGroup `
            --query properties.configuration.ingress.fqdn `
            -o tsv

        if ([string]::IsNullOrWhiteSpace($backendFqdn)) {
            throw "Backend Container App FQDN could not be resolved. Pass -BackendApiBaseUrl if needed."
        }

        $BackendApiBaseUrl = "https://$backendFqdn/api"
    }

    $frontendImage = "$loginServer/pft-web:$Tag"

    Write-Host "==> Building frontend image $frontendImage"
    Write-Host "==> Using backend API $BackendApiBaseUrl"
    podman build `
        --file "$repoRoot\frontend\Containerfile" `
        --build-arg "VITE_API_BASE_URL=$BackendApiBaseUrl" `
        --tag $frontendImage `
        $repoRoot

    Write-Host "==> Pushing frontend image"
    podman push $frontendImage

    Write-Host "==> Updating frontend Container App"
    az containerapp update `
        --name $FrontendAppName `
        --resource-group $ResourceGroup `
        --image $frontendImage | Out-Null
}

$finalBackendFqdn = az containerapp show `
    --name $BackendAppName `
    --resource-group $ResourceGroup `
    --query properties.configuration.ingress.fqdn `
    -o tsv

$finalFrontendFqdn = az containerapp show `
    --name $FrontendAppName `
    --resource-group $ResourceGroup `
    --query properties.configuration.ingress.fqdn `
    -o tsv

Write-Host ""
Write-Host "Redeploy complete."
Write-Host "Target:   $Target"
Write-Host "Tag:      $Tag"
Write-Host "Frontend: https://$finalFrontendFqdn"
Write-Host "Backend:  https://$finalBackendFqdn"
Write-Host "Swagger:  https://$finalBackendFqdn/swagger/index.html"
