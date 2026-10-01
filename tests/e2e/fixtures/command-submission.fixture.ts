import { fileURLToPath } from 'node:url';

import { spawnOwnedProcess, stopOwnedProcess, waitForOwnedServer } from '../helpers/owned-process.js';
import { expect, test } from './index.js';

// Only the two tests that assert transient Submitting feedback use this prebuilt host.
// The normal 150ms acknowledgement can expire while a loaded browser completes its click.
export const submissionTest = test.extend<{ submissionBaseUrl: string }>({
  submissionBaseUrl: async ({}, use) => {
    const projectDirectory = new URL('../../../samples/Counter/Counter.Web/', import.meta.url);
    const assembly = fileURLToPath(new URL('bin/Release/net10.0/Counter.Web.dll', projectDirectory));
    const host = spawnOwnedProcess('dotnet', [assembly, '--urls', 'http://127.0.0.1:0'], {
      cwd: projectDirectory,
      env: {
        ...process.env,
        ASPNETCORE_ENVIRONMENT: 'Test',
        DOTNET_ENVIRONMENT: 'Test',
        Hexalith__FrontComposer__Specimens__Enabled: 'true',
        Hexalith__FrontComposer__StubCommandService__AcknowledgeDelayMs: '1500',
        Hexalith__FrontComposer__StubCommandService__ConfirmDelayMs: process.env.FC_E2E_STORY_3_6_CONFIRM_DELAY_MS ?? '6500',
      },
    }, 60_000);
    try {
      await expect.poll(() => [...host.listeningOrigins][0]).toBeTruthy();
      const baseUrl = [...host.listeningOrigins][0];
      await waitForOwnedServer(host, baseUrl, Date.now() + 10_000);
      await use(baseUrl);
    } finally {
      await stopOwnedProcess(host);
    }
  },
  baseURL: async ({ submissionBaseUrl }, use) => {
    await use(submissionBaseUrl);
  },
});
