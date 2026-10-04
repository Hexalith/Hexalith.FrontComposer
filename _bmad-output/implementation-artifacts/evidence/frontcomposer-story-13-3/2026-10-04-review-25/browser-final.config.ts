import base from '/home/administrator/projects/hexalith/frontcomposer/tests/e2e/playwright.config.ts';
export default {
  ...base,
  testDir: '/home/administrator/projects/hexalith/frontcomposer/tests/e2e/specs',
  outputDir: '/tmp/frontcomposer-13-3-browser-results',
  reporter: [['list'], ['junit', { outputFile: '/tmp/frontcomposer-13-3-browser-results/junit.xml' }]],
  use: { ...base.use, baseURL: 'http://127.0.0.1:53849' },
  projects: [{ name: 'chromium', use: { ...base.projects![0].use, launchOptions: { args: ['--disable-gpu', '--disable-software-rasterizer'] } } }],
  webServer: {
    command: 'dotnet /home/administrator/projects/hexalith/frontcomposer/samples/Counter/Counter.Web/bin/Debug/net10.0/Counter.Web.dll --urls http://127.0.0.1:53849',
    cwd: '/home/administrator/projects/hexalith/frontcomposer/samples/Counter/Counter.Web',
    url: 'http://127.0.0.1:53849',
    reuseExistingServer: false,
    timeout: 60_000,
    env: { ASPNETCORE_ENVIRONMENT: 'Test', DOTNET_ENVIRONMENT: 'Test', Hexalith__FrontComposer__Specimens__Enabled: 'true', Hexalith__Shell__FormAbandonmentThresholdSeconds: '5', Hexalith__Shell__TimeoutActionThresholdMs: '5000', Hexalith__FrontComposer__StubCommandService__ConfirmDelayMs: '6500' }
  }
};
