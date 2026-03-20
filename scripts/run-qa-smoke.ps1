[CmdletBinding()]
param(
    [string]$ApiBaseUrl = "http://127.0.0.1:5086",
    [switch]$UseExistingApi,
    [switch]$SkipBackendBuild,
    [switch]$SkipFrontendBuild,
    [switch]$KeepServerRunning
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $PSScriptRoot
$backendProjectDir = Join-Path $root "backend\PersonalFinanceTracker.Api"
$frontendDir = Join-Path $root "frontend"
$serverProcess = $null
$serverStdOut = $null
$serverStdErr = $null
$originalAspNetCoreEnvironment = $env:ASPNETCORE_ENVIRONMENT

function Write-Step {
    param([string]$Message)
    Write-Host ""
    Write-Host "==> $Message" -ForegroundColor Cyan
}

function Write-Pass {
    param([string]$Message)
    Write-Host "[PASS] $Message" -ForegroundColor Green
}

function Assert-True {
    param(
        [bool]$Condition,
        [string]$Message
    )

    if (-not $Condition) {
        throw $Message
    }
}

function Assert-Equal {
    param(
        $Actual,
        $Expected,
        [string]$Message
    )

    if ($Actual -ne $Expected) {
        throw "$Message Expected '$Expected' but got '$Actual'."
    }
}

function ConvertTo-UtcIso {
    param([datetime]$Date)
    return $Date.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.fffZ")
}

function Ensure-Array {
    param($Value)

    if ($null -eq $Value) {
        return @()
    }

    $items = New-Object System.Collections.Generic.List[object]
    foreach ($item in $Value) {
        $items.Add($item)
    }

    return ,($items.ToArray())
}

function Get-MessageText {
    param($Value)

    if ($null -eq $Value) {
        return $null
    }

    if ($Value -is [string]) {
        return $Value
    }

    $messageProperty = $Value.PSObject.Properties["message"]
    if ($null -ne $messageProperty) {
        return [string]$messageProperty.Value
    }

    return [string]$Value
}

function Get-ParsedErrorBody {
    param($ErrorRecord)

    if ($ErrorRecord.ErrorDetails -and $ErrorRecord.ErrorDetails.Message) {
        try {
            return $ErrorRecord.ErrorDetails.Message | ConvertFrom-Json
        }
        catch {
            return $ErrorRecord.ErrorDetails.Message
        }
    }

    $response = $ErrorRecord.Exception.Response
    if ($null -eq $response) {
        return $null
    }

    try {
        $stream = $response.GetResponseStream()
        if ($null -eq $stream) {
            return $null
        }

        $reader = New-Object System.IO.StreamReader($stream)
        $rawBody = $reader.ReadToEnd()
        if ([string]::IsNullOrWhiteSpace($rawBody)) {
            return $null
        }

        try {
            return $rawBody | ConvertFrom-Json
        }
        catch {
            return $rawBody
        }
    }
    catch {
        return $null
    }
}

function Get-TokenFromPreviewUrl {
    param([string]$PreviewUrl)

    if ([string]::IsNullOrWhiteSpace($PreviewUrl)) {
        return $null
    }

    if ($PreviewUrl -match '[?&]token=([^&]+)') {
        return [Uri]::UnescapeDataString($Matches[1])
    }

    return $null
}

function Invoke-JsonApi {
    param(
        [ValidateSet("GET", "POST", "PUT", "DELETE")]
        [string]$Method,
        [string]$Path,
        $Body = $null,
        [hashtable]$Headers = @{},
        [Microsoft.PowerShell.Commands.WebRequestSession]$WebSession = $null
    )

    $uri = "{0}{1}" -f $ApiBaseUrl.TrimEnd("/"), $Path
    $params = @{
        Method      = $Method
        Uri         = $uri
        Headers     = $Headers
        TimeoutSec  = 90
        ErrorAction = "Stop"
    }

    if ($null -ne $Body) {
        $params.ContentType = "application/json"
        $params.Body = $Body | ConvertTo-Json -Depth 12
    }

    if ($null -ne $WebSession) {
        $params.WebSession = $WebSession
    }

    return Invoke-RestMethod @params
}

function Invoke-ApiExpectFailure {
    param(
        [ValidateSet("GET", "POST", "PUT", "DELETE")]
        [string]$Method,
        [string]$Path,
        [int]$ExpectedStatusCode,
        $Body = $null,
        [hashtable]$Headers = @{},
        [Microsoft.PowerShell.Commands.WebRequestSession]$WebSession = $null
    )

    try {
        $null = Invoke-JsonApi -Method $Method -Path $Path -Body $Body -Headers $Headers -WebSession $WebSession
        throw "Expected HTTP $ExpectedStatusCode for $Method $Path, but the request succeeded."
    }
    catch {
        $response = $_.Exception.Response
        if ($null -eq $response) {
            throw
        }

        $actualStatusCode = [int]$response.StatusCode
        if ($actualStatusCode -ne $ExpectedStatusCode) {
            throw "Expected HTTP $ExpectedStatusCode for $Method $Path, but received $actualStatusCode."
        }

        $parsedBody = Get-ParsedErrorBody $_

        return [pscustomobject]@{
            StatusCode = $actualStatusCode
            Body       = $parsedBody
        }
    }
}

function Wait-ForApi {
    param(
        [string]$HealthPath = "/swagger/v1/swagger.json",
        [int]$TimeoutSeconds = 120
    )

    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    $uri = "{0}{1}" -f $ApiBaseUrl.TrimEnd("/"), $HealthPath

    while ((Get-Date) -lt $deadline) {
        if ($serverProcess -and $serverProcess.HasExited) {
            $stdout = if ($serverStdOut -and (Test-Path $serverStdOut)) { Get-Content $serverStdOut -Raw } else { "" }
            $stderr = if ($serverStdErr -and (Test-Path $serverStdErr)) { Get-Content $serverStdErr -Raw } else { "" }
            throw "Backend exited before becoming ready.`nSTDOUT:`n$stdout`nSTDERR:`n$stderr"
        }

        try {
            $null = Invoke-WebRequest -Uri $uri -UseBasicParsing -TimeoutSec 10 -ErrorAction Stop
            return
        }
        catch {
            Start-Sleep -Seconds 2
        }
    }

    throw "Timed out waiting for API readiness at $uri."
}

try {
    if (-not $SkipBackendBuild) {
        Write-Step "Building backend"
        & dotnet build $backendProjectDir
        if ($LASTEXITCODE -ne 0) {
            throw "Backend build failed."
        }
        Write-Pass "Backend build completed"
    }

    if (-not $SkipFrontendBuild) {
        Write-Step "Building frontend"
        Push-Location $frontendDir
        try {
            & npm run build
            if ($LASTEXITCODE -ne 0) {
                throw "Frontend build failed."
            }
        }
        finally {
            Pop-Location
        }
        Write-Pass "Frontend build completed"
    }

    if (-not $UseExistingApi) {
        Write-Step "Starting backend API"
        $env:ASPNETCORE_ENVIRONMENT = "Development"
        $runId = [guid]::NewGuid().ToString("N")
        $serverStdOut = Join-Path $env:TEMP "personal-finance-qa-$runId.out.log"
        $serverStdErr = Join-Path $env:TEMP "personal-finance-qa-$runId.err.log"
        $serverProcess = Start-Process `
            -FilePath "dotnet" `
            -ArgumentList @("run", "--urls", $ApiBaseUrl) `
            -WorkingDirectory $backendProjectDir `
            -RedirectStandardOutput $serverStdOut `
            -RedirectStandardError $serverStdErr `
            -PassThru

        Wait-ForApi
        Write-Pass "Backend API is ready at $ApiBaseUrl"
    }
    else {
        Write-Step "Using existing API"
        Wait-ForApi
        Write-Pass "Existing API is reachable at $ApiBaseUrl"
    }

    $stamp = Get-Date -Format "yyyyMMddHHmmss"
    $email = "qa+$stamp@finance.local"
    $password = "QaUser@$stamp"
    $displayName = "QA Runner $stamp"
    $todayUtc = [DateTime]::UtcNow.Date
    $month = $todayUtc.Month
    $year = $todayUtc.Year
    $nextMonthDate = $todayUtc.AddMonths(1)
    $nextMonth = $nextMonthDate.Month
    $nextMonthYear = $nextMonthDate.Year

    Write-Step "Running QA API coverage for isolated user $email"

    $unauthorized = Invoke-ApiExpectFailure -Method "GET" -Path "/api/accounts" -ExpectedStatusCode 401
    Write-Pass "Unauthorized request is rejected"

    $register = Invoke-JsonApi -Method "POST" -Path "/api/auth/register" -Body @{
        displayName = $displayName
        email = $email
        password = $password
    }
    $verifyToken = Get-TokenFromPreviewUrl $register.previewUrl
    Assert-True (-not [string]::IsNullOrWhiteSpace($verifyToken)) "Registration did not return a local preview verification link."
    Write-Pass "User registration succeeded"

    $unverifiedLogin = Invoke-ApiExpectFailure -Method "POST" -Path "/api/auth/login" -ExpectedStatusCode 400 -Body @{
        email = $email
        password = $password
    }
    Assert-Equal (Get-MessageText $unverifiedLogin.Body) "Verify your email before logging in." "Unverified login should be blocked."
    Write-Pass "Unverified login is blocked"

    $verified = Invoke-JsonApi -Method "POST" -Path "/api/auth/verify-email" -Body @{
        token = $verifyToken
    }
    Assert-Equal $verified.message "Your email has been verified. You can log in now." "Email verification did not complete."
    Write-Pass "Email verification flow works"

    $session = New-Object Microsoft.PowerShell.Commands.WebRequestSession
    $login = Invoke-JsonApi -Method "POST" -Path "/api/auth/login" -Body @{
        email = $email
        password = $password
    } -WebSession $session
    Assert-Equal $login.email $email.ToLowerInvariant() "Login email mismatch."
    Assert-True ($login.userNumber -ge 1) "Login did not return a valid user number."
    Write-Pass "Login succeeded"

    $badLogin = Invoke-ApiExpectFailure -Method "POST" -Path "/api/auth/login" -ExpectedStatusCode 401 -Body @{
        email = $email
        password = "WrongPass123"
    }
    Write-Pass "Invalid login is rejected"

    $me = Invoke-JsonApi -Method "GET" -Path "/api/auth/me" -WebSession $session
    Assert-Equal $me.email $login.email "Session restore after login failed."
    Write-Pass "Cookie session is active"

    $expenseCategories = Ensure-Array (Invoke-JsonApi -Method "GET" -Path "/api/categories?type=expense" -WebSession $session)
    $incomeCategories = Ensure-Array (Invoke-JsonApi -Method "GET" -Path "/api/categories?type=income" -WebSession $session)
    Assert-True ($expenseCategories.Count -gt 0) "Expected seeded expense categories."
    Assert-True ($incomeCategories.Count -gt 0) "Expected seeded income categories."
    Write-Pass "Seeded categories are available"

    $customExpenseCategory = Invoke-JsonApi -Method "POST" -Path "/api/categories" -WebSession $session -Body @{
        name = "QA Travel $stamp"
        type = "expense"
        color = "#F97316"
        icon = "plane"
    }
    $customIncomeCategory = Invoke-JsonApi -Method "POST" -Path "/api/categories" -WebSession $session -Body @{
        name = "QA Bonus $stamp"
        type = "income"
        color = "#10B981"
        icon = "coins"
    }
    $archiveCategory = Invoke-JsonApi -Method "POST" -Path "/api/categories" -WebSession $session -Body @{
        name = "QA Archive $stamp"
        type = "expense"
        color = "#64748B"
        icon = "archive"
    }
    Assert-Equal $customExpenseCategory.type "expense" "Custom expense category type mismatch."
    Assert-Equal $customIncomeCategory.type "income" "Custom income category type mismatch."
    Write-Pass "Custom categories can be created"

    $updatedArchiveCategory = Invoke-JsonApi -Method "PUT" -Path "/api/categories/$($archiveCategory.id)" -WebSession $session -Body @{
        name = "QA Archive Updated $stamp"
        type = "expense"
        color = "#334155"
        icon = "archive"
    }
    Assert-Equal $updatedArchiveCategory.name "QA Archive Updated $stamp" "Category update failed."
    $archivedCategory = Invoke-JsonApi -Method "POST" -Path "/api/categories/$($archiveCategory.id)/archive?isArchived=true" -WebSession $session
    Assert-True ([bool]$archivedCategory.isArchived) "Category archive failed."
    $restoredCategory = Invoke-JsonApi -Method "POST" -Path "/api/categories/$($archiveCategory.id)/archive?isArchived=false" -WebSession $session
    Assert-True (-not [bool]$restoredCategory.isArchived) "Category unarchive failed."
    Write-Pass "Category update and archive flow works"

    $bankAccount = Invoke-JsonApi -Method "POST" -Path "/api/accounts" -WebSession $session -Body @{
        name = "QA Primary Bank $stamp"
        type = "Savings Account"
        openingBalance = 20000
        institutionName = "QA Bank"
    }
    $fundAccount = Invoke-JsonApi -Method "POST" -Path "/api/accounts" -WebSession $session -Body @{
        name = "QA Travel Fund $stamp"
        type = "Fund"
        categoryId = $customExpenseCategory.id
        openingBalance = 4000
        institutionName = "Goal Bucket"
    }
    $creditCard = Invoke-JsonApi -Method "POST" -Path "/api/accounts" -WebSession $session -Body @{
        name = "QA Credit Card $stamp"
        type = "Credit Card"
        creditLimit = 10000
        institutionName = "QA Cards"
    }
    $tempAccount = Invoke-JsonApi -Method "POST" -Path "/api/accounts" -WebSession $session -Body @{
        name = "QA Temp Account $stamp"
        type = "Cash Wallet"
        openingBalance = 250
        institutionName = "Wallet"
    }
    Assert-Equal $fundAccount.categoryId $customExpenseCategory.id "Fund account category link mismatch."
    Assert-True ($creditCard.creditLimit -eq 10000) "Credit card limit mismatch."
    Write-Pass "Accounts can be created"

    $creditCardUpdated = Invoke-JsonApi -Method "PUT" -Path "/api/accounts/$($creditCard.id)" -WebSession $session -Body @{
        name = "QA Credit Card $stamp"
        type = "Credit Card"
        creditLimit = 12000
        institutionName = "QA Cards"
    }
    Assert-True ($creditCardUpdated.creditLimit -eq 12000) "Credit card update failed."

    $deletedTempAccount = Invoke-JsonApi -Method "DELETE" -Path "/api/accounts/$($tempAccount.id)" -WebSession $session
    Assert-True ((Get-MessageText $deletedTempAccount) -like "*deleted*") "Temp account delete failed."
    Write-Pass "Account update and delete flow works"

    $accounts = Ensure-Array (Invoke-JsonApi -Method "GET" -Path "/api/accounts" -WebSession $session)
    Assert-True ($accounts.Count -ge 3) "Expected three persisted accounts."

    $incomeTransaction = Invoke-JsonApi -Method "POST" -Path "/api/transactions" -WebSession $session -Body @{
        accountId = $bankAccount.id
        categoryId = $customIncomeCategory.id
        type = "income"
        amount = 5000
        date = ConvertTo-UtcIso $todayUtc
        merchant = "QA Employer"
        note = "Salary style income"
        paymentMethod = "Bank Transfer"
        tags = @("qa", "income")
    }
    $bankExpenseTransaction = Invoke-JsonApi -Method "POST" -Path "/api/transactions" -WebSession $session -Body @{
        accountId = $bankAccount.id
        categoryId = $customExpenseCategory.id
        type = "expense"
        amount = 1200
        date = ConvertTo-UtcIso $todayUtc
        merchant = "QA Market"
        note = "Travel expense from bank"
        paymentMethod = "UPI"
        tags = @("qa", "expense")
    }
    $creditExpenseTransaction = Invoke-JsonApi -Method "POST" -Path "/api/transactions" -WebSession $session -Body @{
        accountId = $creditCard.id
        categoryId = $customExpenseCategory.id
        type = "expense"
        amount = 3000
        date = ConvertTo-UtcIso $todayUtc
        merchant = "QA Airline"
        note = "Travel expense on card"
        paymentMethod = "Card"
        tags = @("qa", "card")
    }
    $fundExpenseTransaction = Invoke-JsonApi -Method "POST" -Path "/api/transactions" -WebSession $session -Body @{
        accountId = $fundAccount.id
        categoryId = $customExpenseCategory.id
        type = "expense"
        amount = 700
        date = ConvertTo-UtcIso $todayUtc
        merchant = "QA Fund Spend"
        note = "Travel expense from fund"
        paymentMethod = "Fund"
        tags = @("qa", "fund")
    }
    $tempDeleteTransaction = Invoke-JsonApi -Method "POST" -Path "/api/transactions" -WebSession $session -Body @{
        accountId = $bankAccount.id
        categoryId = $customExpenseCategory.id
        type = "expense"
        amount = 100
        date = ConvertTo-UtcIso $todayUtc
        merchant = "QA Delete Me"
        note = "Temporary transaction"
        paymentMethod = "Cash"
        tags = @("qa", "delete")
    }
    Assert-True ($incomeTransaction.transactionNumber -ge 1) "Transaction number was not generated."
    Write-Pass "Transactions can be created across bank, fund, and credit card sources"

    $updatedBankExpense = Invoke-JsonApi -Method "PUT" -Path "/api/transactions/$($bankExpenseTransaction.id)" -WebSession $session -Body @{
        accountId = $bankAccount.id
        categoryId = $customExpenseCategory.id
        type = "expense"
        amount = 1000
        date = ConvertTo-UtcIso $todayUtc
        merchant = "QA Market"
        note = "Travel expense updated"
        paymentMethod = "UPI"
        tags = @("qa", "expense", "updated")
    }
    Assert-True ([decimal]$updatedBankExpense.amount -eq 1000) "Transaction update failed."

    $deletedTransaction = Invoke-JsonApi -Method "DELETE" -Path "/api/transactions/$($tempDeleteTransaction.id)" -WebSession $session
    Assert-True ((Get-MessageText $deletedTransaction) -like "*deleted*") "Transaction delete failed."

    $searchedTransactions = Ensure-Array (Invoke-JsonApi -Method "GET" -Path "/api/transactions?search=QA%20Market" -WebSession $session)
    Assert-True ($searchedTransactions.Count -ge 1) "Transaction search failed."
    $fundTransactions = Ensure-Array (Invoke-JsonApi -Method "GET" -Path "/api/transactions?accountId=$($fundAccount.id)" -WebSession $session)
    Assert-True ($fundTransactions.Count -eq 1) "Fund account filter failed."
    Write-Pass "Transaction update, delete, filter, and search work"

    $budget = Invoke-JsonApi -Method "POST" -Path "/api/budgets" -WebSession $session -Body @{
        categoryId = $customExpenseCategory.id
        month = $month
        year = $year
        amount = 10000
        alertThresholdPercent = 70
    }
    Assert-Equal $budget.categoryId $customExpenseCategory.id "Budget category mismatch."
    $nextMonthBudgets = Ensure-Array (Invoke-JsonApi -Method "POST" -Path "/api/budgets/duplicate" -WebSession $session -Body @{
        sourceMonth = $month
        sourceYear = $year
        targetMonth = $nextMonth
        targetYear = $nextMonthYear
    })
    Assert-True ($nextMonthBudgets.Count -ge 1) "Budget duplication failed."
    $duplicatedBudget = $nextMonthBudgets | Where-Object { $_.categoryId -eq $customExpenseCategory.id } | Select-Object -First 1
    Assert-True ($null -ne $duplicatedBudget) "Duplicated budget for target month not found."
    Write-Pass "Budget create and duplicate flow works"

    $goal = Invoke-JsonApi -Method "POST" -Path "/api/goals" -WebSession $session -Body @{
        name = "QA Travel Goal $stamp"
        targetAmount = 5000
        targetDate = ConvertTo-UtcIso ($todayUtc.AddMonths(6))
        categoryId = $customExpenseCategory.id
        linkedAccountId = $bankAccount.id
        icon = "plane"
        color = "#2563EB"
    }
    $tempGoal = Invoke-JsonApi -Method "POST" -Path "/api/goals" -WebSession $session -Body @{
        name = "QA Empty Goal $stamp"
        targetAmount = 1000
        targetDate = ConvertTo-UtcIso ($todayUtc.AddMonths(2))
        categoryId = $customExpenseCategory.id
        icon = "target"
        color = "#8B5CF6"
    }
    $goalContribution = Invoke-JsonApi -Method "POST" -Path "/api/goals/$($goal.id)/contribute" -WebSession $session -Body @{
        amount = 2500
        accountId = $bankAccount.id
        note = "QA contribution"
    }
    Assert-True ([decimal]$goalContribution.currentAmount -eq 2500) "Goal contribution failed."

    $goalFundExpense = Invoke-JsonApi -Method "POST" -Path "/api/transactions" -WebSession $session -Body @{
        goalId = $goal.id
        categoryId = $customExpenseCategory.id
        type = "expense"
        amount = 600
        date = ConvertTo-UtcIso $todayUtc
        merchant = "QA Travel Booking"
        note = "Expense paid from goal fund"
        paymentMethod = "Goal Fund"
        tags = @("qa", "goal")
    }
    Assert-Equal $goalFundExpense.goalId $goal.id "Goal-funded expense did not use the goal source."

    $goalWithdrawal = Invoke-JsonApi -Method "POST" -Path "/api/goals/$($goal.id)/withdraw" -WebSession $session -Body @{
        amount = 400
        accountId = $bankAccount.id
        note = "QA withdrawal"
    }
    Assert-True ([decimal]$goalWithdrawal.currentAmount -eq 1500) "Goal withdrawal failed."

    $deletedGoal = Invoke-JsonApi -Method "DELETE" -Path "/api/goals/$($tempGoal.id)" -WebSession $session
    Assert-True ((Get-MessageText $deletedGoal) -like "*deleted*") "Empty goal delete failed."
    Write-Pass "Goals can be created, funded, spent from, withdrawn from, and deleted when empty"

    $bankToCardTransfer = Invoke-JsonApi -Method "POST" -Path "/api/accounts/transfer" -WebSession $session -Body @{
        sourceAccountId = $bankAccount.id
        destinationAccountId = $creditCard.id
        amount = 2000
        date = ConvertTo-UtcIso $todayUtc
        note = "QA card payment"
    }
    $bankToCardTransferMessage = Get-MessageText $bankToCardTransfer
    Assert-True ($bankToCardTransferMessage -like "*payment*" -or $bankToCardTransferMessage -like "*paid off*") "Bank to card settlement failed."

    $creditToBankFailure = Invoke-ApiExpectFailure -Method "POST" -Path "/api/accounts/transfer" -ExpectedStatusCode 400 -WebSession $session -Body @{
        sourceAccountId = $creditCard.id
        destinationAccountId = $bankAccount.id
        amount = 100
        date = ConvertTo-UtcIso $todayUtc
        note = "Invalid reverse transfer"
    }
    $creditToBankFailureMessage = Get-MessageText $creditToBankFailure.Body
    if (-not [string]::IsNullOrWhiteSpace($creditToBankFailureMessage)) {
        Assert-True ($creditToBankFailureMessage -like "*not supported*") "Expected credit card reverse transfer guard did not fire."
    }
    Write-Pass "Transfers and credit-card rules behave correctly"

    $currentBudgets = Ensure-Array (Invoke-JsonApi -Method "GET" -Path "/api/budgets?month=$month&year=$year" -WebSession $session)
    $currentBudget = $currentBudgets | Where-Object { $_.id -eq $budget.id } | Select-Object -First 1
    Assert-True ($null -ne $currentBudget) "Current budget was not returned."
    Assert-True ([decimal]$currentBudget.spentAmount -eq 5300) "Budget spent amount did not reflect transaction activity."
    Assert-True ([string]$currentBudget.status -eq "safe") "Unexpected budget status before threshold."
    Write-Pass "Budget aggregates respond to transaction activity"

    $futureDueDate = $todayUtc.AddDays(5)
    $recurring = Invoke-JsonApi -Method "POST" -Path "/api/recurring" -WebSession $session -Body @{
        title = "QA Rent $stamp"
        type = "expense"
        amount = 1600
        categoryId = $customExpenseCategory.id
        accountId = $bankAccount.id
        frequency = "monthly"
        startDate = ConvertTo-UtcIso $futureDueDate
        nextRunDate = ConvertTo-UtcIso $futureDueDate
        autoCreateTransaction = $true
        isPaused = $false
    }
    $updatedRecurring = Invoke-JsonApi -Method "PUT" -Path "/api/recurring/$($recurring.id)" -WebSession $session -Body @{
        title = "QA Rent Updated $stamp"
        type = "expense"
        amount = 1700
        categoryId = $customExpenseCategory.id
        accountId = $bankAccount.id
        frequency = "monthly"
        startDate = ConvertTo-UtcIso $futureDueDate
        nextRunDate = ConvertTo-UtcIso $futureDueDate
        autoCreateTransaction = $true
        isPaused = $false
    }
    Assert-True ([decimal]$updatedRecurring.amount -eq 1700) "Recurring update failed."
    $recurringItems = Ensure-Array (Invoke-JsonApi -Method "GET" -Path "/api/recurring" -WebSession $session)
    Assert-True ((@($recurringItems | Where-Object { $_.id -eq $updatedRecurring.id })).Count -eq 1) "Recurring item lookup failed."
    Write-Pass "Recurring create, update, and list work"

    $dashboard = Invoke-JsonApi -Method "GET" -Path "/api/dashboard/summary" -WebSession $session
    $upcomingBills = Ensure-Array $dashboard.upcomingBills
    Assert-True ([decimal]$dashboard.currentMonthIncome -eq 5400) "Dashboard income aggregate mismatch."
    Assert-True ([decimal]$dashboard.currentMonthExpense -eq 7800) "Dashboard expense aggregate mismatch."
    Assert-True ($upcomingBills.Count -ge 1) "Dashboard upcoming bills is empty."
    Assert-True ((@($upcomingBills | Where-Object { $_.title -eq "QA Rent Updated $stamp" })).Count -eq 1) "Recurring item did not appear in upcoming bills."
    Write-Pass "Dashboard summary returns live aggregates and upcoming bills"

    $from = ConvertTo-UtcIso (Get-Date -Date (Get-Date -Year $year -Month $month -Day 1).ToUniversalTime())
    $to = ConvertTo-UtcIso $todayUtc
    $report = Invoke-JsonApi -Method "GET" -Path "/api/reports/summary?from=$([uri]::EscapeDataString($from))&to=$([uri]::EscapeDataString($to))" -WebSession $session
    $reportCategorySpend = Ensure-Array $report.categorySpend
    Assert-True ([decimal]$report.summary.totalIncome -eq 5400) "Report income total mismatch."
    Assert-True ([decimal]$report.summary.totalExpense -eq 7800) "Report expense total mismatch."
    Assert-True ($report.summary.transactionCount -ge 7) "Report transaction count is lower than expected."
    Assert-True ((@($reportCategorySpend | Where-Object { $_.categoryName -eq $customExpenseCategory.name })).Count -ge 1) "Category spend report is missing the custom expense category."

    $csvResponse = Invoke-WebRequest `
        -Uri ("{0}/api/reports/export/csv?from={1}&to={2}" -f $ApiBaseUrl.TrimEnd("/"), [uri]::EscapeDataString($from), [uri]::EscapeDataString($to)) `
        -WebSession $session `
        -UseBasicParsing `
        -TimeoutSec 90 `
        -ErrorAction Stop
    $csvFirstLine = ($csvResponse.Content -split "`n")[0].Trim("`r").Trim([char]0xFEFF)
    Assert-Equal $csvFirstLine 'Date,Account,Type,Category,Merchant,Amount,PaymentMethod,Note,Tags' "CSV header mismatch."
    Assert-True ($csvResponse.Content -like "*QA Travel Booking*") "CSV export is missing goal-funded expense data."
    Write-Pass "Reports summary and CSV export work"

    $deletedRecurring = Invoke-JsonApi -Method "DELETE" -Path "/api/recurring/$($updatedRecurring.id)" -WebSession $session
    Assert-True ((Get-MessageText $deletedRecurring) -like "*deleted*") "Recurring delete failed."
    $deletedBudget = Invoke-JsonApi -Method "DELETE" -Path "/api/budgets/$($duplicatedBudget.id)" -WebSession $session
    Assert-True ((Get-MessageText $deletedBudget) -like "*deleted*") "Duplicated budget delete failed."
    Write-Pass "Delete flows work for recurring items and budgets"

    $finalAccounts = Ensure-Array (Invoke-JsonApi -Method "GET" -Path "/api/accounts" -WebSession $session)
    $bankFinal = $finalAccounts | Where-Object { $_.id -eq $bankAccount.id } | Select-Object -First 1
    $fundFinal = $finalAccounts | Where-Object { $_.id -eq $fundAccount.id } | Select-Object -First 1
    $cardFinal = $finalAccounts | Where-Object { $_.id -eq $creditCard.id } | Select-Object -First 1
    Assert-True ([decimal]$bankFinal.currentBalance -eq 19900) "Unexpected final bank balance."
    Assert-True ([decimal]$fundFinal.currentBalance -eq 3300) "Unexpected final fund balance."
    Assert-True ([decimal]$cardFinal.currentBalance -eq 1000) "Unexpected final credit-card balance."
    Write-Pass "Final account balances match expected state"

    Write-Host ""
    Write-Host "QA smoke test completed successfully." -ForegroundColor Green
    Write-Host "User: $email"
    Write-Host "API:  $ApiBaseUrl"
}
finally {
    $env:ASPNETCORE_ENVIRONMENT = $originalAspNetCoreEnvironment
    if ($serverProcess -and -not $KeepServerRunning) {
        if (-not $serverProcess.HasExited) {
            Stop-Process -Id $serverProcess.Id -Force
        }
    }
}
