import { expect, test } from '../fixtures/index.js';
import { CounterPage } from '../page-objects/counter.page.js';

test.describe('Deferred Fluent modal initialization', () => {
  test.beforeEach(async ({ page }) => {
    await page.addInitScript(() => {
      window.localStorage.clear();
      window.sessionStorage.clear();
      const probeWindow = window as Window & { __fcDeferredFrameMs?: number };
      const requestFrame = window.requestAnimationFrame.bind(window);
      // Fluent captures requestAnimationFrame during module load. Delay its initial bindings
      // selectively after hydration to reproduce a busy browser exceeding the 150ms correction.
      window.requestAnimationFrame = callback => requestFrame(timestamp => {
        const delay = probeWindow.__fcDeferredFrameMs ?? 0;
        if (delay > 0) setTimeout(() => callback(timestamp), delay);
        else callback(timestamp);
      });
    });
    const counter = new CounterPage(page);
    await counter.goto();
    await expect(counter.heading).toBeFocused();
  });

  test('dialog title survives its deferred native attribute bindings', async ({ page }) => {
    const initiallyLabeled = await page.evaluate(async () => {
      const modulePath = '/_content/Hexalith.FrontComposer.Shell/js/fc-focus.js';
      const focus = await import(modulePath);
      const probeWindow = window as Window & { __fcDeferredFrameMs?: number };
      probeWindow.__fcDeferredFrameMs = 400;
      const host = document.createElement('fluent-dialog');
      host.setAttribute('type', 'alert');
      host.innerHTML = '<h2 id="fc-deferred-dialog-title">Deferred dialog title</h2>'
        + '<div id="fc-deferred-dialog-description" data-testid="fc-deferred-dialog">Deferred dialog description</div>';
      document.body.append(host);
      const labeled = focus.labelDialog('fc-deferred-dialog', 'fc-deferred-dialog-title', 'fc-deferred-dialog-description');
      await new Promise<void>(resolve => requestAnimationFrame(() => resolve()));
      probeWindow.__fcDeferredFrameMs = 0;
      (host as HTMLElement & { show: () => void }).show();
      return labeled;
    });

    expect(initiallyLabeled).toBe(true);
    const modal = page.getByRole('alertdialog');
    await expect(modal).toBeVisible();
    await expect(modal).toHaveAttribute('aria-labelledby', 'fc-deferred-dialog-title');
    await expect(modal).toHaveAccessibleName('Deferred dialog title');
    await expect(modal).toHaveAccessibleDescription('Deferred dialog description');
  });

  test('palette entry waits for modal opening and retains later keyboard focus', async ({ page }) => {
    await page.evaluate(() => {
      const Dialog = customElements.get('fluent-dialog')!;
      const show = Dialog.prototype.show as (this: HTMLElement) => void;
      Dialog.prototype.show = function (this: HTMLElement): void {
        setTimeout(() => show.call(this), 400);
      };
      const pending: Array<() => void> = [];
      const schedule = window.setTimeout.bind(window);
      window.setTimeout = ((handler: TimerHandler, delay?: number, ...args: unknown[]) => {
        if (delay === 150 && typeof handler === 'function') {
          pending.push(() => handler(...args));
          return schedule(() => {}, 0);
        }
        return schedule(handler, delay, ...args);
      }) as typeof window.setTimeout;
      const probeWindow = window as Window & { __fcReleaseEntryCorrections?: () => void };
      probeWindow.__fcReleaseEntryCorrections = () => {
        window.setTimeout = schedule as typeof window.setTimeout;
        pending.forEach(callback => callback());
      };
    });

    await page.getByTestId('fc-palette-trigger').click();
    const search = page.getByRole('searchbox');
    await expect(search).toBeFocused();
    await search.pressSequentially('Configure');
    await expect(search).toHaveValue('Configure');

    // Release the queued 150ms corrections only after a real Tab move, so this assertion covers
    // operator intent even when the browser completes typing outside the normal correction window.
    await page.getByTestId('fc-palette-root').evaluate(root => {
      const button = document.createElement('button');
      button.textContent = 'Deferred modal keyboard target';
      button.setAttribute('data-testid', 'fc-deferred-keyboard-target');
      root.append(button);
    });
    await page.keyboard.press('Tab');
    const keyboardTarget = page.getByTestId('fc-deferred-keyboard-target');
    await expect(keyboardTarget).toBeFocused();
    await page.evaluate(() => {
      const probeWindow = window as Window & { __fcReleaseEntryCorrections?: () => void };
      probeWindow.__fcReleaseEntryCorrections!();
    });
    await expect(keyboardTarget).toBeFocused();

    await page.keyboard.press('Escape');
    await expect(page.getByTestId('fc-palette-root')).toHaveCount(0);
    await expect(page.getByTestId('fc-palette-trigger')).toBeFocused();
  });
});
