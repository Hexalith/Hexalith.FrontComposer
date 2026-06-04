import { defineConfig, devices } from '@playwright/test';

const BASE_URL = process.env.BASE_URL ?? 'http://127.0.0.1:5070';
const IS_CI = !!process.env.CI;
const STORY_3_6_CONFIRM_DELAY_MS = process.env.FC_E2E_STORY_3_6_CONFIRM_DELAY_MS ?? '6500';

export default defineConfig({
  testDir: './specs',
  fullyParallel: true,
  forbidOnly: IS_CI,
  retries: IS_CI ? 2 : 0,
  workers: IS_CI ? '50%' : undefined,
  timeout: 60_000,
  expect: {
    timeout: 10_000,
  },
  reporter: [
    ['list'],
    ['html', { outputFolder: 'playwright-report', open: 'never' }],
    ['junit', { outputFile: 'test-results/junit.xml' }],
  ],
  use: {
    baseURL: BASE_URL,
    actionTimeout: 15_000,
    navigationTimeout: 30_000,
    trace: 'retain-on-failure',
    screenshot: 'only-on-failure',
    video: 'retain-on-failure',
    ignoreHTTPSErrors: true,
    testIdAttribute: 'data-testid',
  },
  projects: [
    {
      name: 'chromium',
      use: { ...devices['Desktop Chrome'] },
    },
    {
      name: 'firefox',
      use: { ...devices['Desktop Firefox'] },
    },
    {
      name: 'webkit',
      use: { ...devices['Desktop Safari'] },
    },
  ],
  outputDir: 'test-results',
  webServer: process.env.PLAYWRIGHT_SKIP_WEBSERVER
    ? undefined
    : {
        command: 'dotnet run --project ../../samples/Counter/Counter.Web/Counter.Web.csproj --configuration Release --no-build --no-launch-profile --urls http://127.0.0.1:5070',
        url: BASE_URL,
        reuseExistingServer: !IS_CI,
        timeout: 120_000,
        env: {
          ASPNETCORE_ENVIRONMENT: 'Test',
          Hexalith__FrontComposer__Specimens__Enabled: 'true',
          Hexalith__Shell__TimeoutActionThresholdMs: '5000',
          Hexalith__FrontComposer__StubCommandService__ConfirmDelayMs: STORY_3_6_CONFIRM_DELAY_MS,
        },
      },
});
