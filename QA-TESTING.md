# QA Testing

Use the automated smoke runner when you want a thorough pass across the core finance flows.

## Command

From the project root:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\run-qa-smoke.ps1
```

## What it covers

- backend build
- frontend build
- API startup
- auth: register, login, invalid login, unauthorized access
- categories: create, update, archive, unarchive
- accounts: create, update, delete, fund account setup, credit card setup
- transactions: create, update, delete, search, filter
- budgets: create, duplicate, live spend aggregation
- goals: create, contribute, spend from goal fund, withdraw, delete empty goal
- transfers: bank to credit-card settlement and invalid reverse transfer guard
- recurring: create, update, list, delete
- dashboard: live totals and upcoming bills
- reports: summary and CSV export
- final balance assertions for key accounts

## Useful switches

Use an already running API:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\run-qa-smoke.ps1 -UseExistingApi -ApiBaseUrl http://127.0.0.1:5000
```

Keep the API running after the test:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\run-qa-smoke.ps1 -KeepServerRunning
```

Skip build steps if you only want the API flow check:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\run-qa-smoke.ps1 -SkipBackendBuild -SkipFrontendBuild
```

## Notes

- The script creates a fresh QA user on every run, so it does not depend on seeded demo users.
- It expects PostgreSQL to be reachable through the app's configured `DefaultConnection`.
- By default it starts the backend on `http://127.0.0.1:5086` to avoid common local port conflicts.
