import { createHash } from 'node:crypto';
import { readFile } from 'node:fs/promises';
import { expect, test } from '../fixtures/index.js';
import { CounterPage } from '../page-objects/counter.page.js';

const modulePath = '/_content/Hexalith.FrontComposer.Shell/js/fc-download.js';

test.beforeEach(async ({ page }) => {
  await new CounterPage(page).goto();
  const served = await page.request.get(modulePath);
  expect(served.ok()).toBe(true);
  const owner = await readFile(new URL('../../../src/Hexalith.FrontComposer.Shell/wwwroot/js/fc-download.js', import.meta.url));
  expect(createHash('sha256').update(await served.body()).digest('hex'))
    .toBe(createHash('sha256').update(owner).digest('hex'));
});

test('stream and JSON downloads preserve exact bytes and safe filename repeatedly', async ({ page }) => {
  const bytes = [0, 239, 187, 191, 123, 10, 32, 125, 255];
  for (const suffix of ['first', 'second']) {
    const pending = page.waitForEvent('download');
    await page.evaluate(async ({ bytes, suffix }) => {
      const path: string = '/_content/Hexalith.FrontComposer.Shell/js/fc-download.js';
      const download = await import(path);
      download.beginDownload(suffix);
      await download.downloadStream(suffix, `safe-${suffix}.json`, 'application/json', {
        arrayBuffer: async () => new Uint8Array(bytes).buffer,
      });
    }, { bytes, suffix });
    const download = await pending;
    expect(download.suggestedFilename()).toBe(`safe-${suffix}.json`);
    expect(await download.failure()).toBeNull();
    const file = await download.path();
    expect(file).not.toBeNull();
    expect(await readFile(file!)).toEqual(Buffer.from(bytes));
  }
  await expect(page.locator('a[download]')).toHaveCount(0);
});

test('cancellation during a stream read prevents the click and object URL creation', async ({ page }) => {
  const result = await page.evaluate(async () => {
    const path: string = '/_content/Hexalith.FrontComposer.Shell/js/fc-download.js';
    const download = await import(path);
    let resolve!: (value: ArrayBuffer) => void;
    let clicks = 0;
    let urls = 0;
    const originalClick = HTMLAnchorElement.prototype.click;
    const originalCreate = URL.createObjectURL;
    HTMLAnchorElement.prototype.click = () => { clicks++; };
    URL.createObjectURL = blob => { urls++; return originalCreate(blob); };
    try {
      download.beginDownload('cancelled');
      const transfer = download.downloadStream('cancelled', 'cancelled.json', 'application/json', {
        arrayBuffer: () => new Promise<ArrayBuffer>(complete => { resolve = complete; }),
      });
      download.cancelDownload('cancelled');
      resolve(new Uint8Array([123, 125]).buffer);
      await transfer;
      return { clicks, urls, anchors: document.querySelectorAll('a[download]').length };
    } finally {
      HTMLAnchorElement.prototype.click = originalClick;
      URL.createObjectURL = originalCreate;
    }
  });
  expect(result).toEqual({ clicks: 0, urls: 0, anchors: 0 });
});

test('click failure still removes the anchor and revokes its object URL', async ({ page }) => {
  const result = await page.evaluate(async () => {
    const path: string = '/_content/Hexalith.FrontComposer.Shell/js/fc-download.js';
    const download = await import(path);
    const created: string[] = [];
    const revoked: string[] = [];
    const originalClick = HTMLAnchorElement.prototype.click;
    const originalCreate = URL.createObjectURL;
    const originalRevoke = URL.revokeObjectURL;
    HTMLAnchorElement.prototype.click = () => { throw new Error('synthetic failure'); };
    URL.createObjectURL = blob => { const url = originalCreate(blob); created.push(url); return url; };
    URL.revokeObjectURL = url => { revoked.push(url); originalRevoke(url); };
    let failed = false;
    try {
      download.beginDownload('failure');
      try {
        await download.downloadStream('failure', 'failure.json', 'application/json', {
          arrayBuffer: async () => new Uint8Array([123, 125]).buffer,
        });
      } catch { failed = true; }
      return { failed, created, revoked, anchors: document.querySelectorAll('a[download]').length };
    } finally {
      HTMLAnchorElement.prototype.click = originalClick;
      URL.createObjectURL = originalCreate;
      URL.revokeObjectURL = originalRevoke;
    }
  });
  expect(result.failed).toBe(true);
  expect(result.created).toHaveLength(1);
  expect(result.revoked).toEqual(result.created);
  expect(result.anchors).toBe(0);
});
