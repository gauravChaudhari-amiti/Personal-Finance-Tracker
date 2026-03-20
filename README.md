# Personal Finance Tracker

Personal Finance Tracker is a full-stack finance app with:

- ASP.NET Core backend
- React + Vite frontend
- PostgreSQL database
- budgets, goals, recurring items, reports, accounts, and transactions

## Project structure

- `backend/PersonalFinanceTracker.Api` -> API, EF Core, PostgreSQL, auth, business logic
- `frontend` -> React app
- `scripts` -> deployment and QA scripts

## Local development

Backend:

```powershell
cd .\backend\PersonalFinanceTracker.Api
dotnet run
```

Frontend:

```powershell
cd .\frontend
npm install
npm run dev
```

## QA

API smoke run from repo root:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\run-qa-smoke.ps1
```
More detail is in `QA-TESTING.md`.

## Deployment

Azure + Podman deployment details, exact commands, troubleshooting notes, and redeploy examples are in:

- `DEPLOY-AZURE-PODMAN.md`

Live app:

- The app is currently deployed at `https://pft-web.agreeablewave-6fe11347.centralindia.azurecontainerapps.io`
- Use this link when you want to quickly open the latest Azure-hosted version of the app without running it locally

Quick redeploy commands from the repo root in `cmd`:

```cmd
scripts\redeploy.cmd
scripts\redeploy.cmd backend
scripts\redeploy.cmd frontend
scripts\redeploy.cmd both -GoogleClientId "YOUR_GOOGLE_CLIENT_ID"
scripts\redeploy.cmd both -EmailSmtpHost "smtp.example.com" -EmailSmtpPort "587" -EmailSmtpUsername "user@example.com" -EmailSmtpPassword "APP_PASSWORD" -EmailFromEmail "user@example.com"
```

Quick redeploy command from PowerShell:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\redeploy-azure-changes.ps1 -Target both
powershell -ExecutionPolicy Bypass -File .\scripts\redeploy-azure-changes.ps1 -Target both -GoogleClientId "YOUR_GOOGLE_CLIENT_ID"
powershell -ExecutionPolicy Bypass -File .\scripts\redeploy-azure-changes.ps1 -Target both -EmailSmtpHost "smtp.example.com" -EmailSmtpPort "587" -EmailSmtpUsername "user@example.com" -EmailSmtpPassword "APP_PASSWORD" -EmailFromEmail "user@example.com"
```

Notes:

- `Resend email verification link` is for users who signed up with email and password but did not receive or click the verification link.
- Real Google sign-in appears only after `GoogleClientId` is configured in the frontend build and backend Container App settings.
- Real email delivery appears only after SMTP settings are passed during deploy/redeploy.

That runbook includes:

- tool installation
- WSL and Podman setup
- Azure provider registration
- one-shot deployment script usage
- quick redeploy script usage
- PostgreSQL firewall rules
- local container debugging
- backend/frontend rebuild and redeploy commands

## Deployment files

- `backend/PersonalFinanceTracker.Api/Containerfile`
- `frontend/Containerfile`
- `frontend/nginx.conf`
- `.dockerignore`
- `scripts/deploy-azure-container-apps.ps1`
- `scripts/redeploy-azure-changes.ps1`
- `scripts/redeploy.cmd`
