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

Browser E2E from `frontend`:

```powershell
npm run test:e2e
```

More detail is in `QA-TESTING.md`.

## Deployment

Azure + Podman deployment details, exact commands, troubleshooting notes, and redeploy examples are in:

- `DEPLOY-AZURE-PODMAN.md`

Quick redeploy commands from the repo root in `cmd`:

```cmd
scripts\redeploy.cmd
scripts\redeploy.cmd backend
scripts\redeploy.cmd frontend
```

Quick redeploy command from PowerShell:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\redeploy-azure-changes.ps1 -Target both
```

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
