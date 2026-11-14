import { expect, test } from '../fixtures/index.js';
import { CounterPage } from '../page-objects/counter.page.js';
import type { Page } from '@playwright/test';

async function setShellKeyFilter(page: Page, enabled: boolean): Promise<void> {
  await page.locator('.fc-shell-root').evaluate(async (element, register) => {
    const modulePath = '/_content/Hexalith.FrontComposer.Shell/js/fc-keyboard.js';
    const keyboard = await import(modulePath);
    if (register) keyboard.registerShellKeyFilter(element);
    else keyboard.unregisterShellKeyFilter(element);
  }, enabled);
}

async function observeTabDefault(page: Page): Promise<void> {
  await page.evaluate(() => {
    document.addEventListener('keydown', (event) => {
      if (event.key === 'Tab') {
        document.documentElement.setAttribute('data-dw91-tab-prevented', String(event.defaultPrevented));
      }
    });
  });
}

test.describe('Forward skip access after route announcement', () => {
  test.beforeEach(async ({ page }) => {
    await page.setViewportSize({ width: 1920, height: 900 });
    await new CounterPage(page).goto();
    await expect(new CounterPage(page).heading).toBeFocused();
  });

  test('cold-load heading announcement leaves both skip links forward reachable', async ({ page }) => {
    await page.keyboard.press('Tab');
    await expect(page.getByRole('link', { name: 'Skip to content', exact: true })).toBeFocused();
    await page.keyboard.press('Tab');
    await expect(page.getByRole('link', { name: 'Skip to navigation', exact: true })).toBeFocused();
    await page.keyboard.press('Enter');
    await expect(page.locator('#fc-nav:visible')).toBeFocused();
  });

  test('client navigation announces its heading then allows bypass to main content', async ({ page }) => {
    await page.evaluate(() => document.documentElement.setAttribute('data-dw91-document', 'original'));
    await page.getByTestId('fc-nav-context-Specimens').click();
    await expect(page).toHaveURL(/\/specimens$/);
    await expect(page.getByRole('heading', { name: 'Specimens', exact: true, level: 1 })).toBeFocused();
    await expect(page.locator('html')).toHaveAttribute('data-dw91-document', 'original');
    await page.keyboard.press('Tab');
    await expect(page.getByRole('link', { name: 'Skip to content', exact: true })).toBeFocused();
    await page.keyboard.press('Enter');
    await expect(page.locator('#fc-main-content')).toBeFocused();
    await page.keyboard.press('Tab');
    await expect(page.getByRole('tab', { name: 'Overview', exact: true })).toBeFocused();
  });

  test('reverse Tab retains native traversal from the announced heading', async ({ page }) => {
    await setShellKeyFilter(page, false);
    await page.keyboard.press('Shift+Tab');
    const nativeDestination = await page.evaluateHandle(() => document.activeElement);
    await new CounterPage(page).heading.focus();
    await setShellKeyFilter(page, true);
    await observeTabDefault(page);
    await page.keyboard.press('Shift+Tab');
    await expect(page.locator('html')).toHaveAttribute('data-dw91-tab-prevented', 'false');
    await expect.poll(() => page.evaluate((expected) => document.activeElement === expected, nativeDestination)).toBe(true);
    await nativeDestination.dispose();
  });

  for (const key of ['Shift', 'ArrowDown', 'Escape']) {
    test(`${key} without a focus move preserves forward skip access`, async ({ page }) => {
      await page.keyboard.press(key);
      await expect(new CounterPage(page).heading).toBeFocused();
      await page.keyboard.press('Tab');
      await expect(page.getByRole('link', { name: 'Skip to content', exact: true })).toBeFocused();
    });
  }

  for (const tabIndex of [0, -2]) {
    test(`route announcement supports authored tabindex ${tabIndex}`, async ({ page }) => {
      await new CounterPage(page).heading.evaluate((heading, index) => {
        heading.blur();
        heading.tabIndex = index;
        heading.focus();
      }, tabIndex);
      await expect(new CounterPage(page).heading).toBeFocused();
      await page.keyboard.press('Tab');
      await expect(page.getByRole('link', { name: 'Skip to content', exact: true })).toBeFocused();
      await expect(new CounterPage(page).heading).toHaveAttribute('tabindex', String(tabIndex));
      await page.keyboard.press('Enter');
      await expect(page.locator('#fc-main-content')).toBeFocused();
      await page.keyboard.press('Tab');
      if (tabIndex === 0) {
        await expect(new CounterPage(page).heading).toBeFocused();
        await page.keyboard.press('Tab');
      }
      await expect(page.getByRole('tab', { name: 'Overview', exact: true })).toBeFocused();
    });
  }

  test('a deliberate pointer action on the heading retains native forward traversal', async ({ page }) => {
    await setShellKeyFilter(page, false);
    await new CounterPage(page).heading.click();
    await page.keyboard.press('Tab');
    const nativeDestination = await page.evaluateHandle(() => document.activeElement);
    await new CounterPage(page).heading.focus();
    await setShellKeyFilter(page, true);
    await observeTabDefault(page);
    await new CounterPage(page).heading.click();
    await page.keyboard.press('Tab');
    await expect(page.locator('html')).toHaveAttribute('data-dw91-tab-prevented', 'false');
    await expect.poll(() => page.evaluate((expected) => document.activeElement === expected, nativeDestination)).toBe(true);
    await nativeDestination.dispose();
  });

  test('settings restoration preserves a pointer heading choice until a new route announcement', async ({ page }) => {
    await new CounterPage(page).heading.click();
    await page.keyboard.press('Control+,');
    await expect(page.locator('#fc-settings-heading')).toBeFocused();
    await page.getByTestId('fc-settings-done').click();
    await expect(new CounterPage(page).heading).toBeFocused();
    await setShellKeyFilter(page, false);
    await page.keyboard.press('Tab');
    const nativeDestination = await page.evaluateHandle(() => document.activeElement);
    await new CounterPage(page).heading.focus();
    await setShellKeyFilter(page, true);
    await observeTabDefault(page);
    await new CounterPage(page).heading.click();
    await page.keyboard.press('Control+,');
    await expect(page.locator('#fc-settings-heading')).toBeFocused();
    await page.getByTestId('fc-settings-done').click();
    await expect(new CounterPage(page).heading).toBeFocused();
    await page.keyboard.press('Tab');
    await expect(page.locator('html')).toHaveAttribute('data-dw91-tab-prevented', 'false');
    await expect.poll(() => page.evaluate((expected) => document.activeElement === expected, nativeDestination)).toBe(true);
    await nativeDestination.dispose();

    await page.getByTestId('fc-nav-context-Specimens').click();
    await expect(page.getByRole('heading', { name: 'Specimens', exact: true, level: 1 })).toBeFocused();
    await page.keyboard.press('Tab');
    await expect(page.getByRole('link', { name: 'Skip to content', exact: true })).toBeFocused();
  });

  for (const clickHeading of [false, true]) {
    test(`a reused heading on a new route honors fresh pointer input: ${clickHeading}`, async ({ page }) => {
      await observeTabDefault(page);
      await new CounterPage(page).heading.click();
      await new CounterPage(page).heading.evaluate((element) => {
        history.pushState({}, '', '/__dw91/reused-route');
        element.textContent = 'Reused heading on the next route';
        // Focusing the active node emits no focusin, as with a reused routed component.
        element.focus();
      });
      const heading = page.getByRole('heading', { name: 'Reused heading on the next route', exact: true });
      await expect(heading).toBeFocused();
      if (clickHeading) await heading.click();
      await page.keyboard.press('Tab');
      if (clickHeading) {
        await expect(page.locator('html')).toHaveAttribute('data-dw91-tab-prevented', 'false');
      } else {
        await expect(page.getByRole('link', { name: 'Skip to content', exact: true })).toBeFocused();
      }
    });
  }

  test('unregister and repeated register preserve the shell keyboard lifecycle', async ({ page }) => {
    await observeTabDefault(page);
    for (let cycle = 0; cycle < 2; cycle += 1) {
      await setShellKeyFilter(page, false);
      await new CounterPage(page).heading.focus();
      await page.keyboard.press('Tab');
      await expect(page.locator('html')).toHaveAttribute('data-dw91-tab-prevented', 'false');

      await new CounterPage(page).heading.focus();
      await setShellKeyFilter(page, true);
      await setShellKeyFilter(page, true);
      await page.keyboard.press('Tab');
      await expect(page.getByRole('link', { name: 'Skip to content', exact: true })).toBeFocused();
    }
  });

  test('a heading outside main content does not arm the registered shell skip link', async ({ page }) => {
    await observeTabDefault(page);
    await page.getByRole('banner').evaluate((banner) => {
      const heading = document.createElement('h1');
      heading.tabIndex = -1;
      heading.textContent = 'Unrelated banner heading';
      banner.append(heading);
      const button = document.createElement('button');
      button.textContent = 'Banner continuation';
      banner.append(button);
      heading.focus();
    });
    await page.keyboard.press('Tab');
    await expect(page.getByRole('button', { name: 'Banner continuation', exact: true })).toBeFocused();
    await expect(page.locator('html')).toHaveAttribute('data-dw91-tab-prevented', 'false');
  });

  test('opening a shell dialog preserves its own Tab flow', async ({ page }) => {
    await page.getByTestId('fc-settings-button').click();
    await expect(page.locator('#fc-settings-heading')).toBeFocused();
    await page.keyboard.press('Tab');
    await expect(page.getByRole('link', { name: 'Skip to content', exact: true })).not.toBeFocused();
    await expect.poll(() => page.evaluate(() =>
      document.activeElement?.closest('fluent-dialog, dialog, [aria-modal="true"]') !== null)).toBe(true);
  });
});

test.describe('Native dialog readiness for overlay entry', () => {
  for (const scenario of ['entry', 'typing', 'later Tab', 'close', 'replacement'] as const) {
    test(`delayed opening preserves ${scenario} focus ownership`, async ({ page }) => {
      await new CounterPage(page).goto();
      const clockStart = Date.now();
      await page.clock.install({ time: clockStart });
      await page.clock.pauseAt(clockStart + 1000);
      const focusedBeforeOpen = await page.evaluate(async () => {
        const modulePath = '/_content/Hexalith.FrontComposer.Shell/js/fc-focus.js';
        const focus = await import(modulePath);
        const opener = document.createElement('button');
        opener.textContent = 'Delayed dialog opener';
        document.body.append(opener);
        opener.focus();
        const dialog = document.createElement('dialog');
        dialog.id = 'delayed-entry-dialog';
        dialog.setAttribute('aria-labelledby', 'delayed-entry-heading');
        dialog.innerHTML = '<button id="delayed-entry-default" autofocus>Native default</button>'
          + '<h2 id="delayed-entry-heading" tabindex="-1">Delayed entry</h2>'
          + '<button id="delayed-entry-next">Next dialog action</button>';
        document.body.append(dialog);
        const focused = focus.focusOverlayEntry('delayed-entry-heading');
        setTimeout(() => { if (dialog.isConnected) dialog.showModal(); }, 250);
        return focused;
      });
      expect(focusedBeforeOpen).toBe(false);
      await page.clock.runFor(200);
      await expect(page.locator('#delayed-entry-dialog')).not.toHaveAttribute('open', '');
      if (scenario === 'typing') await page.keyboard.press('a');

      if (scenario === 'replacement') {
        await page.evaluate(() => {
          const original = document.querySelector<HTMLDialogElement>('#delayed-entry-dialog')!;
          const replacement = original.cloneNode(true) as HTMLDialogElement;
          original.remove();
          document.body.append(replacement);
          setTimeout(() => replacement.showModal(), 50);
        });
        await page.clock.runFor(60);
        await expect(page.locator('#delayed-entry-default')).toBeFocused();
        return;
      }

      await page.clock.runFor(60);
      await expect(page.locator('#delayed-entry-heading')).toBeFocused();
      if (scenario === 'later Tab') {
        await page.keyboard.press('Tab');
        await expect(page.locator('#delayed-entry-next')).toBeFocused();
      } else if (scenario === 'close') {
        await page.evaluate(() => document.querySelector<HTMLDialogElement>('#delayed-entry-dialog')!.close());
      }
      await page.clock.runFor(200);
      if (scenario === 'entry' || scenario === 'typing') {
        await expect(page.locator('#delayed-entry-heading')).toBeFocused();
      } else if (scenario === 'later Tab') {
        await expect(page.locator('#delayed-entry-next')).toBeFocused();
      } else {
        await page.evaluate(() => document.querySelector<HTMLDialogElement>('#delayed-entry-dialog')!.showModal());
        await expect(page.locator('#delayed-entry-default')).toBeFocused();
      }
    });
  }

  for (const readiness of ['definition', 'shadow attachment'] as const) {
    test(`entry observes delayed custom-element ${readiness}`, async ({ page }) => {
      await new CounterPage(page).goto();
      const clockStart = Date.now();
      await page.clock.install({ time: clockStart });
      await page.clock.pauseAt(clockStart + 1000);
      // A fresh same-origin realm has no pre-registered Fluent element, so both
      // upgrade and deferred attachShadow paths exercise the actual browser APIs.
      await page.evaluate(() => {
        const frame = document.createElement('iframe');
        frame.name = 'delayed-overlay-realm';
        frame.srcdoc = '<!doctype html><html><body><button>Opener</button></body></html>';
        document.body.append(frame);
      });
      await expect.poll(() => page.frame({ name: 'delayed-overlay-realm' }) !== null).toBe(true);
      const frame = page.frame({ name: 'delayed-overlay-realm' })!;
      await frame.waitForURL('about:srcdoc');
      await frame.waitForLoadState('domcontentloaded');
      const focusedBeforeOpen = await frame.evaluate(async ({ origin, readiness }) => {
        const modulePath = `${origin}/_content/Hexalith.FrontComposer.Shell/js/fc-focus.js`;
        const focus = await import(modulePath);
        if (readiness === 'shadow attachment') customElements.define('fluent-dialog', class extends HTMLElement {});
        const host = document.createElement('fluent-dialog');
        host.style.visibility = 'hidden';
        host.innerHTML = '<button autofocus>Native default</button><h2 id="late-shadow-heading" tabindex="-1">Late shadow entry</h2>';
        document.body.append(host);
        const attach = () => {
          const dialog = document.createElement('dialog');
          dialog.innerHTML = '<slot></slot>';
          host.attachShadow({ mode: 'open' }).append(dialog);
          host.style.visibility = 'visible';
          setTimeout(() => dialog.showModal(), 50);
        };
        const focused = focus.focusOverlayEntry('late-shadow-heading');
        setTimeout(() => {
          if (readiness === 'definition') {
            customElements.define('fluent-dialog', class extends HTMLElement {
              constructor() {
                super();
                const dialog = document.createElement('dialog');
                dialog.innerHTML = '<slot></slot>';
                this.attachShadow({ mode: 'open' }).append(dialog);
                setTimeout(() => dialog.showModal(), 50);
              }
            });
            host.style.visibility = 'visible';
          } else attach();
        }, 250);
        return focused;
      }, { origin: new URL(page.url()).origin, readiness });
      expect(focusedBeforeOpen).toBe(false);
      await page.clock.runFor(310);
      await expect(frame.locator('#late-shadow-heading')).toBeFocused();
    });
  }
});


test('landmark skip focus does not override a later programmatic focus move', async ({ page }) => {
  await new CounterPage(page).goto();
  await page.clock.install();
  await page.evaluate(async () => {
    const path: string = '/_content/Hexalith.FrontComposer.Shell/js/fc-keyboard.js';
    const keyboard = await import(path);
    keyboard.focusElement(document.getElementById('fc-main-content'));
    (document.querySelector('.fc-shell-root') as HTMLElement).focus();
  });
  await page.clock.runFor(200);
  await expect(page.locator('.fc-shell-root')).toBeFocused();
  await page.keyboard.press('Tab');
  await expect(page.getByRole('link', { name: 'Skip to content', exact: true })).toBeFocused();
});
