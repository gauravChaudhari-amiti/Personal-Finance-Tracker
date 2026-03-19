import { defineConfig, devices } from "@playwright/test";

export default defineConfig({
  testDir: "./tests/e2e",
  fullyParallel: false,
  retries: 0,
  workers: 1,
  timeout: 120_000,
  expect: {
    timeout: 12_000
  },
  use: {
    baseURL: "http://localhost:5173",
    trace: "on-first-retry",
    screenshot: "only-on-failure",
    video: "retain-on-failure"
  },
  projects: [
    {
      name: "chromium",
      use: {
        ...devices["Desktop Chrome"]
      }
    }
  ],
  webServer: [
    {
      command: "dotnet run --urls http://localhost:5000",
      cwd: "../backend/PersonalFinanceTracker.Api",
      url: "http://localhost:5000/swagger/v1/swagger.json",
      timeout: 120_000,
      reuseExistingServer: true
    },
    {
      command: "npm run dev -- --host localhost --port 5173",
      cwd: ".",
      url: "http://localhost:5173/login",
      timeout: 120_000,
      reuseExistingServer: true
    }
  ]
});
