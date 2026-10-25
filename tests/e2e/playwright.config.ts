import { randomUUID } from 'node:crypto';
import { resolve } from 'node:path';
import { defineConfig, devices } from '@playwright/test';

process.env.FC_A11Y_EVIDENCE_RUN_ID ??= randomUUID();

const BASE_URL = process.env.BASE_URL ?? 'http://127.0.0.1:5070';
const IS_CI = !!process.env.CI;
const STORY_3_6_CONFIRM_DELAY_MS = process.env.FC_E2E_STORY_3_6_CONFIRM_DELAY_MS ?? '6500';
const PROJECTION_RECONNECTED_NOTICE_DURATION_MS = '30000';
const OUTPUT_DIR = process.env.FC_E2E_OUTPUT_DIR ?? 'test-results';
const HTML_REPORT_DIR = process.env.FC_E2E_HTML_REPORT_DIR ?? 'playwright-report';
const JSON_PATH = process.env.FC_E2E_JSON_PATH ?? `${OUTPUT_DIR}/playwright.json`;
const JUNIT_PATH = process.env.FC_E2E_JUNIT_PATH ?? `${OUTPUT_DIR}/junit.xml`;
// The approved Firefox/WebKit Linux images use DejaVu fallbacks. Ubuntu 26.04
// prefers Noto instead; keep the native engines' font environment reproducible.
const nativeLinuxLaunchOptions = process.platform === 'linux'
  ? { env: { ...process.env, FONTCONFIG_FILE: process.env.FONTCONFIG_FILE ?? resolve(import.meta.dirname, 'fonts/linux-fontconfig.conf') } }
  : {};

export default defineConfig({
  metadata: { a11yEvidenceRunId: process.env.FC_A11Y_EVIDENCE_RUN_ID, testHostOptions: { ProjectionReconnectedNoticeDurationMs: Number(PROJECTION_RECONNECTED_NOTICE_DURATION_MS) } },
  testDir: './specs',
  fullyParallel: true,
  forbidOnly: IS_CI,
  retries: IS_CI ? 2 : 0,
  updateSnapshots: 'none',
  workers: IS_CI ? '50%' : undefined,
  timeout: 60_000,
  expect: {
    timeout: 10_000,
  },
  reporter: [
    ['list'],
    ['html', { outputFolder: HTML_REPORT_DIR, open: 'never' }],
    ['junit', { outputFile: JUNIT_PATH }],
    ['json', { outputFile: JSON_PATH }],
  ],
  use: {
    baseURL: BASE_URL,
    actionTimeout: 15_000,
    navigationTimeout: 30_000,
    trace: process.env.FC_E2E_TRACE === 'on' ? 'on' : 'retain-on-failure',
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
      use: { ...devices['Desktop Firefox'], launchOptions: nativeLinuxLaunchOptions },
    },
    {
      name: 'webkit',
      use: { ...devices['Desktop Safari'], launchOptions: nativeLinuxLaunchOptions },
    },
  ],
  outputDir: OUTPUT_DIR,
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
          Hexalith__Shell__FormAbandonmentThresholdSeconds: '5',
          Hexalith__Shell__TimeoutActionThresholdMs: '5000',
          // Keep genuine recovery paint available for both media-mode inspections.
          Hexalith__Shell__ProjectionReconnectedNoticeDurationMs: PROJECTION_RECONNECTED_NOTICE_DURATION_MS,
          Hexalith__FrontComposer__StubCommandService__ConfirmDelayMs: STORY_3_6_CONFIRM_DELAY_MS,
        },
      },
});
