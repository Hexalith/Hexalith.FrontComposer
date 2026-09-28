import { expect, test } from '../fixtures/index.js';
import { CounterPage } from '../page-objects/counter.page.js';
import { ShellPage } from '../page-objects/shell.page.js';

test.describe('Story 11.7: generated command and module route contract', () => {
  test.beforeEach(async ({ page }) => {
    await page.addInitScript(() => {
      window.localStorage.clear();
      window.sessionStorage.clear();
    });
  });

  test('Home shortcut opens the directory while the Counter root remains available', async ({ page, tenant }) => {
    expect(tenant.tenantId).toBeTruthy();
    await new CounterPage(page).goto();

    await page.keyboard.press('g');
    await page.keyboard.press('h');

    await expect(page).toHaveURL(/\/home$/);
    await expect(page.getByRole('main').getByRole('heading', { level: 1 })).toBeVisible();
    await page.goto('/');
    await expect(page.getByRole('heading', { name: 'Hexalith FrontComposer — Counter Sample', level: 1 })).toBeVisible();
  });

  test('specimen module tile opens its default Overview tab', async ({ page, tenant }) => {
    expect(tenant.tenantId).toBeTruthy();
    await page.setViewportSize({ width: 1920, height: 900 });
    await new CounterPage(page).goto();

    const specimenTile = page.getByTestId('fc-nav-context-Specimens');
    await expect(specimenTile).toBeVisible();
    await specimenTile.click();
    await expect(page).toHaveURL(/\/specimens$/);
    await expect(page.getByRole('heading', { name: 'Specimens', level: 1 })).toBeFocused();
    await expect(page.getByRole('tab', { name: 'Overview' })).toHaveAttribute('aria-selected', 'true');
    await expect(page.getByTestId('specimens-overview-type')).toHaveAttribute('href', '/__frontcomposer/specimens/type');

    await page.goto('/specimens/overview');
    await expect(page.getByRole('tab', { name: 'Overview' })).toHaveAttribute('aria-selected', 'true');
  });

  test('module workspace palette activation reaches the generated full-page command', async ({ page, tenant }) => {
    expect(tenant.tenantId).toBeTruthy();

    await page.setViewportSize({ width: 1920, height: 900 });
    const counter = new CounterPage(page);
    await counter.goto();
    await expect(page).toHaveURL(/\/counter$/);
    await expect(counter.heading).toBeVisible();
    await expect(counter.heading).toBeFocused();
    const headingBox = await counter.heading.boundingBox();
    const bannerBox = await page.getByRole('banner').boundingBox();
    expect(headingBox).not.toBeNull();
    expect(bannerBox).not.toBeNull();
    expect(headingBox!.y).toBeGreaterThanOrEqual(bannerBox!.y + bannerBox!.height);

    const shell = new ShellPage(page);
    await shell.shellRoot.waitFor();
    await page.getByTestId('fc-nav-flyout-trigger-Counter').click();
    await expect(shell.counterProjectionItem).toBeVisible();
    await expect(shell.counterProjectionItem).toHaveAttribute('data-href', '/counter/counter-projection');
    await shell.counterProjectionItem.click();
    await expect(page).toHaveURL(/\/counter\/counter-projection$/);
    await expect(counter.heading).toBeVisible();

    await counter.goto();
    await expect(page).toHaveURL(/\/counter$/);

    await page.getByTestId('fc-palette-trigger').click();
    await page.getByRole('searchbox').pressSequentially('Configure');
    const configureCounter = page.getByTestId('fc-palette-option').filter({ hasText: 'ConfigureCounterCommand' });
    await expect(configureCounter).toBeVisible();
    await configureCounter.click();

    await expect(page).toHaveURL(/\/commands\/Counter\/ConfigureCounterCommand$/);
    const commandHeading = page
      .getByRole('main')
      .getByRole('heading', { name: 'Configure Counter', exact: true, level: 1 });
    await expect(commandHeading).toBeVisible();
    await expect(commandHeading).toBeFocused();
    await expect(page.locator('.fc-command-form[aria-label="Configure Counter command form"]')).toBeVisible();
  });

  test('invalid module tab selects Overview with one persistent announcement', async ({ page, tenant }) => {
    expect(tenant.tenantId).toBeTruthy();
    await page.goto('/counter/unknown-tab');
    await new ShellPage(page).shellRoot.waitFor();

    await expect(page).toHaveURL(/\/counter\/overview$/);
    await expect(page.getByRole('tab', { name: 'Overview' })).toHaveAttribute('aria-selected', 'true');
    const fallback = page.getByTestId('fc-module-tab-fallback');
    await expect(fallback).toHaveCount(1);
    await expect(fallback).toContainText('Overview');
  });

  test('keyboard tab selection keeps tab focus while changing the module route', async ({ page, tenant }) => {
    expect(tenant.tenantId).toBeTruthy();
    await new CounterPage(page).goto();

    const overview = page.getByRole('tab', { name: 'Overview' });
    const projection = page.getByRole('tab', { name: 'Projection' });
    await overview.focus();
    await page.keyboard.press('ArrowRight');

    await expect(page).toHaveURL(/\/counter\/counter-projection$/);
    await expect(projection).toHaveAttribute('aria-selected', 'true');
    await expect(projection).toBeFocused();
    await expect(page.getByRole('tabpanel', { name: 'Projection' })).toBeVisible();
  });

  test('palette and settings return focus to their captured header triggers', async ({ page, tenant }) => {
    expect(tenant.tenantId).toBeTruthy();
    await new ShellPage(page).goto();

    const paletteTrigger = page.getByTestId('fc-palette-trigger');
    await paletteTrigger.click();
    await expect(page.getByRole('searchbox')).toBeFocused();
    await page.keyboard.press('Escape');
    await expect(paletteTrigger).toBeFocused();

    const settingsTrigger = page.getByTestId('fc-settings-button');
    await settingsTrigger.click();
    await expect(page.locator('#fc-settings-heading')).toBeFocused();
    await page.getByTestId('fc-settings-done').click();
    await expect(settingsTrigger).toBeFocused();
  });

  test('mixed-case tab URL selects Projection without fallback', async ({ page, tenant }) => {
    expect(tenant.tenantId).toBeTruthy();
    await page.goto('/counter/COUNTER-PROJECTION');
    await new ShellPage(page).shellRoot.waitFor();

    await expect(page.getByRole('tab', { name: 'Projection' })).toHaveAttribute('aria-selected', 'true');
    await expect(page.getByTestId('fc-module-tab-fallback')).toHaveText('');
  });

  test('failed-route focus confirmation leaves an open palette query and focus intact', async ({ page, tenant }) => {
    expect(tenant.tenantId).toBeTruthy();
    await new CounterPage(page).goto();
    await page.getByTestId('fc-palette-trigger').click();
    const search = page.getByRole('searchbox');
    await search.fill('Configure');
    await expect(search).toBeFocused();

    await page.evaluate(async () => {
      const modulePath = '/_content/Hexalith.FrontComposer.Shell/js/fc-focus.js';
      const focus = await import(modulePath);
      const owner = { invokeMethodAsync: () => Promise.resolve() };
      (document.querySelector('main h1, [role="main"] h1') as HTMLElement | null)?.focus();
      focus.focusRouteHeading(location.href, owner, true);
    });

    await expect(search).toHaveValue('Configure');
    await expect(search).toBeFocused();
  });

  test('denied routed content reports failure without confirming the route', async ({ page, tenant }) => {
    expect(tenant.tenantId).toBeTruthy();
    await new CounterPage(page).goto();
    await page.evaluate(async () => {
      const modulePath = '/_content/Hexalith.FrontComposer.Shell/js/fc-focus.js';
      const focus = await import(modulePath);
      const calls: string[] = [];
      (window as any).__fcDeniedRouteCalls = calls;
      const heading = document.querySelector('[data-fc-route-content] h1')!;
      const denied = document.createElement('div');
      denied.setAttribute('data-fc-route-denied', 'true');
      heading.replaceWith(denied);
      denied.append(heading);
      focus.focusRouteHeading(location.href, {
        invokeMethodAsync: (method: string) => {
          calls.push(method);
          return Promise.resolve();
        },
      });
    });

    await expect.poll(() => page.evaluate(() => (window as any).__fcDeniedRouteCalls))
      .toEqual(['ReportRouteHeadingDenied']);
  });

  test('removed overlay origin restores focus to the current route heading', async ({ page, tenant }) => {
    expect(tenant.tenantId).toBeTruthy();
    await new CounterPage(page).goto();
    await page.getByTestId('fc-settings-button').click();
    await expect(page.locator('#fc-settings-heading')).toBeFocused();
    await page.evaluate(() => document.querySelector('[data-testid="fc-settings-button"]')?.remove());
    await page.getByTestId('fc-settings-done').click();
    await expect(page.getByRole('main').getByRole('heading', { level: 1, name: 'Counter' })).toBeFocused();
  });

  test('route focus waits for a delayed incoming heading instead of confirming the outgoing one', async ({ page, tenant }) => {
    expect(tenant.tenantId).toBeTruthy();
    await new CounterPage(page).goto();
    await page.evaluate(async () => {
      const modulePath = '/_content/Hexalith.FrontComposer.Shell/js/fc-focus.js';
      const focus = await import(modulePath);
      const confirmed: string[] = [];
      (window as any).__fcConfirmedHeadings = confirmed;
      const owner = {
        invokeMethodAsync: (method: string, route: string) => {
          if (method === 'ConfirmRouteHeading') confirmed.push(new URL(route).pathname);
          return Promise.resolve();
        },
      };
      focus.focusRouteHeading(location.href, owner);
      history.pushState({}, '', '/counter/overview');
      const authorizing = document.createElement('div');
      authorizing.setAttribute('data-fc-route-authorizing', 'true');
      document.querySelector('main, [role="main"]')?.append(authorizing);
      focus.focusRouteHeading(location.href, owner);
    });

    await expect.poll(() => page.evaluate(() => (window as any).__fcConfirmedHeadings)).toEqual(['/counter']);
    await page.evaluate(() => {
      document.querySelector('[data-fc-route-authorizing]')?.remove();
      const heading = document.createElement('h1');
      heading.textContent = 'Incoming page';
      document.querySelector('main h1, [role="main"] h1')?.replaceWith(heading);
      document.querySelector('[data-fc-route-content]')?.setAttribute('data-fc-route-content', location.href);
    });
    await expect.poll(() => page.evaluate(() => (window as any).__fcConfirmedHeadings)).toEqual(['/counter', '/counter/overview']);
    await expect(page.getByRole('heading', { level: 1, name: 'Incoming page' })).toBeFocused();
  });

  test('route focus reports a permanently missing heading after authorization deadline', async ({ page, tenant }) => {
    expect(tenant.tenantId).toBeTruthy();
    await new CounterPage(page).goto();
    await page.clock.install();
    await page.evaluate(async () => {
      const modulePath = '/_content/Hexalith.FrontComposer.Shell/js/fc-focus.js';
      const focus = await import(modulePath);
      const calls: string[] = [];
      (window as any).__fcMissingHeadingCalls = calls;
      const authorizing = document.createElement('div');
      authorizing.setAttribute('data-fc-route-authorizing', 'true');
      document.body.append(authorizing);
      focus.focusRouteHeading(location.href, {
        invokeMethodAsync: (method: string) => {
          calls.push(method);
          return Promise.resolve();
        },
      });
    });

    await page.clock.fastForward(10_000);
    expect(await page.evaluate(() => (window as any).__fcMissingHeadingCalls)).toEqual([]);

    await page.clock.fastForward(25_000);
    await expect.poll(() => page.evaluate(() => (window as any).__fcMissingHeadingCalls))
      .toEqual(['ReportRouteHeadingMissing']);
  });

  test('route focus confirms a reused heading with identical text after a route parameter change', async ({ page, tenant }) => {
    expect(tenant.tenantId).toBeTruthy();
    await new CounterPage(page).goto();
    await page.evaluate(async () => {
      const modulePath = '/_content/Hexalith.FrontComposer.Shell/js/fc-focus.js';
      const focus = await import(modulePath);
      const confirmed: string[] = [];
      (window as any).__fcConfirmedHeadings = confirmed;
      const owner = {
        invokeMethodAsync: (method: string, route: string) => {
          if (method === 'ConfirmRouteHeading') confirmed.push(new URL(route).pathname);
          return Promise.resolve();
        },
      };
      focus.focusRouteHeading(location.href, owner);
      history.pushState({}, '', '/counter/overview');
      focus.focusRouteHeading(location.href, owner);
    });
    await expect.poll(() => page.evaluate(() => (window as any).__fcConfirmedHeadings)).toEqual(['/counter']);
    await page.evaluate(() => {
      document.querySelector('[data-fc-route-content]')?.setAttribute('data-fc-route-content', location.href);
    });
    await expect.poll(() => page.evaluate(() => (window as any).__fcConfirmedHeadings)).toEqual(['/counter', '/counter/overview']);
    await expect(page.getByRole('main').getByRole('heading', { level: 1, name: 'Counter' })).toBeFocused();
  });
});

test('later route heading replacement preserves an operator control focus', async ({ page }) => {
  await new CounterPage(page).goto();
  const trigger = page.getByTestId('fc-palette-trigger');
  await trigger.focus();
  await page.evaluate(() => {
    const old = document.querySelector('[data-fc-route-content] h1')!;
    const replacement = old.cloneNode(true);
    old.replaceWith(replacement);
  });
  await expect(trigger).toBeFocused();
});

test('repeated palette shortcut preserves the original invoker', async ({ page }) => {
  await new CounterPage(page).goto();
  const trigger = page.getByTestId('fc-palette-trigger');
  await trigger.focus();
  await page.keyboard.press('Control+k');
  await expect(page.getByRole('searchbox')).toBeFocused();
  await page.keyboard.press('Control+k');
  await page.keyboard.press('Escape');
  await expect(trigger).toBeFocused();
});

test('typing before delayed overlay entry does not cancel focus', async ({ page }) => {
  await new CounterPage(page).goto();
  await page.evaluate(async () => {
    const modulePath = '/_content/Hexalith.FrontComposer.Shell/js/fc-focus.js';
    const focus = await import(modulePath);
    const origin = document.querySelector('[data-testid="fc-palette-trigger"]') as HTMLElement;
    origin.focus();
    focus.captureOverlayOrigin();
    origin.dispatchEvent(new KeyboardEvent('keydown', { key: 'a', bubbles: true }));
    const heading = document.querySelector('#fc-main-content h1') as HTMLElement;
    heading.id = 'delayed-overlay-entry';
    focus.focusOverlayEntry(heading.id, true);
  });
  await expect(page.getByRole('main').getByRole('heading', { level: 1 })).toBeFocused();
});

test('owned denied route suppresses the denied heading focus', async ({ page }) => {
  await new CounterPage(page).goto();
  const trigger = page.getByTestId('fc-palette-trigger');
  await trigger.focus();
  await page.evaluate(async () => {
    const modulePath = '/_content/Hexalith.FrontComposer.Shell/js/fc-focus.js';
    const focus = await import(modulePath);
    const heading = document.querySelector('[data-fc-route-content] h1')!;
    const denied = document.createElement('div');
    denied.setAttribute('data-fc-route-denied', 'true');
    heading.replaceWith(denied);
    denied.append(heading);
    focus.focusRouteHeading(location.href, { invokeMethodAsync: () => Promise.resolve() }, false, true);
  });
  await expect(trigger).toBeFocused();
});


test('later keyboard focus movement suppresses delayed overlay entry', async ({ page }) => {
  await new CounterPage(page).goto();
  await page.evaluate(async () => {
    const modulePath = '/_content/Hexalith.FrontComposer.Shell/js/fc-focus.js';
    const focus = await import(modulePath);
    const origin = document.querySelector('[data-testid="fc-palette-trigger"]') as HTMLElement;
    origin.focus();
    focus.captureOverlayOrigin();
    origin.dispatchEvent(new KeyboardEvent('keydown', { key: 'Tab', bubbles: true }));
    (document.querySelector('[data-testid="fc-settings-button"]') as HTMLElement).focus();
    const heading = document.querySelector('#fc-main-content h1') as HTMLElement;
    heading.id = 'delayed-overlay-entry';
    focus.focusOverlayEntry(heading.id, true);
  });
  await expect(page.getByTestId('fc-settings-button')).toBeFocused();
});


for (const [route, heading] of [
  ['/__frontcomposer/specimens/header-logo/default', 'Default header logo'],
  ['/__frontcomposer/specimens/header-logo/custom', 'Custom header logo'],
]) {
  test(`header specimen has a focused route heading: ${heading}`, async ({ page }) => {
    await page.goto(route);
    await page.locator('.fc-shell-root[data-fc-interactive="true"]').waitFor();
    const routeHeading = page.getByRole('main').getByRole('heading', { level: 1, name: heading });
    await expect(routeHeading).toHaveCount(1);
    await expect(routeHeading).toBeFocused();
  });
}
