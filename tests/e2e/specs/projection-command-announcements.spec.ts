import { expect, test } from '../fixtures/index.js';
import { fillFieldByLabel } from '../helpers/fluent-fields.js';

test.describe('Story 13.4: projection and command announcements', () => {
  test('server revision adopts text already rendered by Blazor without duplicate mutation', async ({ page }) => {
    await page.goto('/counter');
    await page.locator('.fc-shell-root[data-fc-interactive="true"]').waitFor();
    await page.evaluate(async () => {
      const modulePath: string = '/_content/Hexalith.FrontComposer.Shell/js/fc-connectivity.js';
      const connectivity = await import(modulePath);
      const host = document.createElement('div');
      host.id = 'story13-server-revision';
      host.setAttribute('data-fc-connectivity-binding', 'server-revision');
      host.innerHTML = '<div role="status" data-fc-status-revision="1" data-fc-status-message="Loading.">'
        + '<span data-fc-browser-status>Loading.</span></div>';
      document.querySelector('.fc-shell-root')!.append(host);
      host.dataset.mutations = '0';
      new MutationObserver(() => {
        host.dataset.mutations = String(Number(host.dataset.mutations) + 1);
      }).observe(host.querySelector('[data-fc-browser-status]')!, { childList: true, subtree: true, characterData: true });
      connectivity.watchConnectivity({ invokeMethodAsync: () => Promise.resolve() }, host, 'You are offline.');
      const status = host.querySelector('[data-fc-status-revision]')!;
      const text = host.querySelector('[data-fc-browser-status]')!;
      text.textContent = 'Loaded.';
      status.setAttribute('data-fc-status-message', 'Loaded.');
      status.setAttribute('data-fc-status-revision', '2');
    });
    const host = page.locator('#story13-server-revision');
    await expect(host.locator('[data-fc-browser-status]')).toHaveText('Loaded.');
    await expect(host).toHaveAttribute('data-mutations', '1');
    await page.evaluate(async () => {
      const host = document.querySelector<HTMLElement>('#story13-server-revision')!;
      const modulePath: string = '/_content/Hexalith.FrontComposer.Shell/js/fc-connectivity.js';
      const connectivity = await import(modulePath);
      connectivity.unwatchConnectivityBinding(host, 'server-revision');
      host.remove();
    });
  });

  test('initial offline watcher preserves identical status and later episode speaks', async ({ page }) => {
    await page.goto('/counter');
    await page.locator('.fc-shell-root[data-fc-interactive="true"]').waitFor();
    await page.evaluate(async () => {
      const modulePath: string = '/_content/Hexalith.FrontComposer.Shell/js/fc-connectivity.js';
      await import(modulePath);
    });
    const offlineCopy = 'You are offline. Data cannot be refreshed.';
    try {
      await page.context().setOffline(true);
      await page.evaluate(async (copy) => {
        const host = document.createElement('div');
        host.id = 'story13-initial-offline';
        host.setAttribute('data-fc-connectivity-binding', 'initial-offline');
        host.innerHTML = '<div data-fc-browser-offline></div><div role="status" data-fc-status-revision="1" '
          + `data-fc-status-message="${copy}"><span data-fc-browser-status>${copy}</span></div>`;
        document.querySelector('.fc-shell-root')!.append(host);
        const target = host.querySelector('[data-fc-browser-status]')!;
        host.dataset.mutations = '0';
        new MutationObserver(() => { host.dataset.mutations = String(Number(host.dataset.mutations) + 1); })
          .observe(target, { childList: true, subtree: true, characterData: true });
        const modulePath: string = '/_content/Hexalith.FrontComposer.Shell/js/fc-connectivity.js';
        const connectivity = await import(modulePath);
        connectivity.watchConnectivity({ invokeMethodAsync: () => Promise.resolve() }, host, copy);
      }, offlineCopy);
      await expect(page.locator('#story13-initial-offline [data-fc-browser-status]')).toHaveText(offlineCopy);
      await expect(page.locator('#story13-initial-offline')).toHaveAttribute('data-mutations', '0');
      await page.locator('#story13-initial-offline [data-fc-status-revision]').evaluate((status) => {
        status.setAttribute('data-fc-status-message', 'Checking for updates.');
        status.setAttribute('data-fc-status-revision', '2');
      });
      await expect(page.locator('#story13-initial-offline [data-fc-browser-status]')).toHaveText(offlineCopy);
      await expect(page.locator('#story13-initial-offline')).toHaveAttribute('data-mutations', '0');
      await page.context().setOffline(false);
      await page.context().setOffline(true);
      await expect.poll(async () => Number(await page.locator('#story13-initial-offline').getAttribute('data-mutations')))
        .toBeGreaterThan(0);
      await expect(page.locator('#story13-initial-offline [data-fc-browser-status]')).toHaveText(offlineCopy);
    } finally {
      await page.context().setOffline(false);
      await page.evaluate(async () => {
        const host = document.querySelector<HTMLElement>('#story13-initial-offline');
        if (!host) return;
        const modulePath: string = '/_content/Hexalith.FrontComposer.Shell/js/fc-connectivity.js';
        const connectivity = await import(modulePath);
        connectivity.unwatchConnectivityBinding(host, 'initial-offline');
        host.remove();
      });
    }
  });

  test('browser status restores unchanged server fallback after offline and online', async ({ page }) => {
    await page.goto('/counter');
    await page.locator('.fc-shell-root[data-fc-interactive="true"]').waitFor();
    await page.evaluate(async () => {
      const modulePath: string = '/_content/Hexalith.FrontComposer.Shell/js/fc-connectivity.js';
      const connectivity = await import(modulePath);
      const host = document.createElement('div');
      host.id = 'story13-connectivity-replay';
      host.setAttribute('data-fc-connectivity-binding', 'replay');
      host.innerHTML = '<div data-fc-server-connectivity></div><div data-fc-browser-offline hidden></div>'
        + '<div role="status" data-fc-status-revision="7" data-fc-status-fallback="true" '
        + 'data-fc-status-message="Live updates are unavailable. Checking for updates.">'
        + '<span data-fc-browser-status>Live updates are unavailable. Checking for updates.</span></div>'
        + '<p data-fc-browser-stale data-fc-stale-key="projection:replay" data-fc-cached-result="read-1" hidden>Cached data</p>';
      document.querySelector('.fc-shell-root')!.append(host);
      host.dataset.initialMutations = '0';
      const observer = new MutationObserver(() => {
        host.dataset.initialMutations = String(Number(host.dataset.initialMutations) + 1);
      });
      observer.observe(host.querySelector('[data-fc-browser-status]')!, { childList: true, subtree: true, characterData: true });
      host.dataset.callbackCount = '0';
      const reference = { invokeMethodAsync: () => {
        host.dataset.callbackCount = String(Number(host.dataset.callbackCount) + 1);
        return Promise.resolve();
      } };
      const firstId = connectivity.watchConnectivity(reference, host, 'You are offline. Data cannot be refreshed.');
      const retryId = connectivity.watchConnectivity(reference, host, 'You are offline. Data cannot be refreshed.');
      host.dataset.watchId = String(firstId);
      host.dataset.retryId = String(retryId);
    });
    const status = page.locator('#story13-connectivity-replay [role="status"]');
    await expect(status).toHaveText('Live updates are unavailable. Checking for updates.');
    await expect(page.locator('#story13-connectivity-replay')).toHaveAttribute('data-initial-mutations', '0');
    await expect(page.locator('#story13-connectivity-replay')).toHaveAttribute('data-retry-id', await page.locator('#story13-connectivity-replay').getAttribute('data-watch-id') ?? '');
    try {
      await page.context().setOffline(true);
      await expect(status).toHaveText('You are offline. Data cannot be refreshed.');
      await expect(page.locator('#story13-connectivity-replay')).toHaveAttribute('data-callback-count', '1');
      await expect(page.locator('#story13-connectivity-replay [data-fc-browser-stale]')).toBeVisible();
      await status.locator('[data-fc-browser-status]').evaluate((span) => {
        span.textContent = 'Live updates are unavailable. Checking for updates.';
      });
      await expect(status).toHaveText('You are offline. Data cannot be refreshed.');
      await page.context().setOffline(false);
      await expect(status).toHaveText('Live updates are unavailable. Checking for updates.');
      const stale = page.locator('#story13-connectivity-replay [data-fc-browser-stale]');
      await expect(stale).toBeVisible();
      await stale.evaluate((label) => {
        const replacement = label.cloneNode(true) as HTMLElement;
        replacement.hidden = true;
        label.replaceWith(replacement);
      });
      await expect(stale).toBeVisible();
      await stale.evaluate((label) => label.setAttribute('data-fc-cached-result', 'read-2'));
      await expect(stale).toBeHidden();
      await status.evaluate((node) => {
        node.setAttribute('data-fc-status-message', 'Data refreshed.');
        node.setAttribute('data-fc-status-revision', '8');
        node.setAttribute('data-fc-status-fallback', 'false');
      });
      await expect(status).toHaveText('Data refreshed.');
      await page.context().setOffline(true);
      await expect(status).toHaveText('You are offline. Data cannot be refreshed.');
      await page.context().setOffline(false);
      await expect(status).toHaveText('Data refreshed.');
    } finally {
      await page.context().setOffline(false);
      await page.evaluate(async () => {
        const host = document.querySelector<HTMLElement>('#story13-connectivity-replay');
        if (!host) return;
        const modulePath: string = '/_content/Hexalith.FrontComposer.Shell/js/fc-connectivity.js';
        const connectivity = await import(modulePath);
        connectivity.unwatchConnectivityBinding(host, 'replay');
        host.remove();
      });
    }
  });

  test('browser stale label waits for the cached page read, including an unchanged read', async ({ page }) => {
    await page.goto('/counter');
    await page.locator('.fc-shell-root[data-fc-interactive="true"]').waitFor();
    await page.evaluate(async () => {
      const modulePath: string = '/_content/Hexalith.FrontComposer.Shell/js/fc-connectivity.js';
      const connectivity = await import(modulePath);
      const host = document.createElement('div');
      host.id = 'story13-page-freshness';
      host.setAttribute('data-fc-connectivity-binding', 'page-freshness');
      host.innerHTML = '<div role="status"><span data-fc-browser-status></span></div>'
        + '<p data-fc-browser-stale data-fc-stale-key="projection:page" '
        + 'data-fc-cached-result="pages:0|0:1|500:1" hidden>Cached rows may be old.</p>';
      document.querySelector('.fc-shell-root')!.append(host);
      connectivity.watchConnectivity({ invokeMethodAsync: () => Promise.resolve() }, host, 'You are offline.');
    });
    const stale = page.locator('#story13-page-freshness [data-fc-browser-stale]');
    try {
      await page.context().setOffline(true);
      await expect(stale).toBeVisible();
      await page.context().setOffline(false);
      await expect(stale).toBeVisible();
      await stale.evaluate(label => label.setAttribute('data-fc-cached-result', 'pages:0|0:1|500:2'));
      await expect(stale).toBeVisible();
      await stale.evaluate(label => label.setAttribute('data-fc-cached-result', 'pages:500|0:1|500:2'));
      await expect(stale).toBeHidden();
      await stale.evaluate(label => label.setAttribute('data-fc-cached-result', 'pages:0|0:1|500:2'));
      await expect(stale).toBeVisible();
      await stale.evaluate(label => label.setAttribute('data-fc-cached-result', 'pages:0|0:2|500:2'));
      await expect(stale).toBeHidden();
      await page.context().setOffline(true);
      await expect(stale).toBeVisible();
      await page.context().setOffline(false);
      await stale.evaluate(label => label.setAttribute('data-fc-cached-result', 'pages:500|0:2|500:2'));
      await expect(stale).toBeVisible();
      await stale.evaluate(label => label.setAttribute('data-fc-cached-result', 'pages:500|0:3|500:2'));
      await expect(stale).toBeVisible();
      await stale.evaluate(label => label.setAttribute('data-fc-cached-result', 'pages:500|0:3|500:3'));
      await expect(stale).toBeHidden();
      await stale.evaluate(label => label.setAttribute('data-fc-cached-result', 'pages:0|0:3'));
      await page.context().setOffline(true);
      await expect(stale).toBeVisible();
      await page.context().setOffline(false);
      await expect(stale).toBeVisible();
      await stale.evaluate(label => label.setAttribute('data-fc-cached-result', 'pages:0|0:3|500:1'));
      await expect(stale).toBeVisible();
      await stale.evaluate(label => label.setAttribute('data-fc-cached-result', 'pages:500|0:3|500:1'));
      await expect(stale).toBeHidden();
    } finally {
      await page.context().setOffline(false);
      await page.evaluate(async () => {
        const host = document.querySelector<HTMLElement>('#story13-page-freshness');
        if (!host) return;
        const modulePath: string = '/_content/Hexalith.FrontComposer.Shell/js/fc-connectivity.js';
        const connectivity = await import(modulePath);
        connectivity.unwatchConnectivityBinding(host, 'page-freshness');
        host.remove();
      });
    }
  });

  test('browser stale notice rebinds when only its surface key changes', async ({ page }) => {
    await page.goto('/counter');
    await page.locator('.fc-shell-root[data-fc-interactive="true"]').waitFor();
    await page.evaluate(async () => {
      const modulePath: string = '/_content/Hexalith.FrontComposer.Shell/js/fc-connectivity.js';
      const connectivity = await import(modulePath);
      const host = document.createElement('div');
      host.id = 'story13-surface-rebind';
      host.setAttribute('data-fc-connectivity-binding', 'surface-rebind');
      host.innerHTML = '<div role="status"><span data-fc-browser-status></span></div>'
        + '<p data-fc-browser-stale data-fc-stale-key="projection:old" data-fc-stale-scope="1" '
        + 'data-fc-cached-result="pages:0|0:1|500:1" hidden>Cached rows may be old.</p>';
      document.querySelector('.fc-shell-root')!.append(host);
      connectivity.watchConnectivity({ invokeMethodAsync: () => Promise.resolve() }, host, 'You are offline.');
    });
    const stale = page.locator('#story13-surface-rebind [data-fc-browser-stale]');
    try {
      await page.context().setOffline(true);
      await expect(stale).toBeVisible();
      await page.context().setOffline(false);
      await expect(stale).toBeVisible();
      await stale.evaluate(label => label.setAttribute('data-fc-stale-key', 'projection:new'));
      await expect(stale).toBeHidden();

      await page.context().setOffline(true);
      await expect(stale).toBeVisible();
      await page.context().setOffline(false);
      await stale.evaluate(label => label.setAttribute('data-fc-cached-result', 'pages:0|0:1|500:2'));
      await expect(stale).toBeVisible();
      await stale.evaluate(label => label.setAttribute('data-fc-cached-result', 'pages:0|0:2|500:2'));
      await expect(stale).toBeHidden();
      await stale.evaluate(label => label.setAttribute('data-fc-stale-key', 'projection:old'));
      await expect(stale).toBeHidden();
    } finally {
      await page.context().setOffline(false);
      await page.evaluate(async () => {
        const host = document.querySelector<HTMLElement>('#story13-surface-rebind');
        if (!host) return;
        const modulePath: string = '/_content/Hexalith.FrontComposer.Shell/js/fc-connectivity.js';
        const connectivity = await import(modulePath);
        connectivity.unwatchConnectivityBinding(host, 'surface-rebind');
        host.remove();
      });
    }
  });

  test('co-mounted cached pages keep separate browser freshness baselines', async ({ page }) => {
    await page.goto('/counter');
    await page.locator('.fc-shell-root[data-fc-interactive="true"]').waitFor();
    await page.evaluate(async () => {
      const modulePath: string = '/_content/Hexalith.FrontComposer.Shell/js/fc-connectivity.js';
      const connectivity = await import(modulePath);
      const host = document.createElement('div');
      host.id = 'story13-two-cached-pages';
      host.setAttribute('data-fc-connectivity-binding', 'two-pages');
      host.innerHTML = '<div role="status"><span data-fc-browser-status></span></div>'
        + '<p data-fc-browser-stale data-fc-stale-owner="copy-a" data-fc-stale-key="projection:shared" data-fc-stale-scope="1" data-fc-cached-result="pages:0|0:1|500:1" hidden>A</p>'
        + '<p data-fc-browser-stale data-fc-stale-owner="copy-b" data-fc-stale-key="projection:shared" data-fc-stale-scope="1" data-fc-cached-result="pages:500|0:1|500:1" hidden>B</p>';
      document.querySelector('.fc-shell-root')!.append(host);
      connectivity.watchConnectivity({ invokeMethodAsync: () => Promise.resolve() }, host, 'You are offline.');
    });
    const a = page.locator('#story13-two-cached-pages [data-fc-stale-owner="copy-a"]');
    const b = page.locator('#story13-two-cached-pages [data-fc-stale-owner="copy-b"]');
    try {
      await page.context().setOffline(true);
      await expect(a).toBeVisible();
      await expect(b).toBeVisible();
      await page.context().setOffline(false);
      await a.evaluate(label => label.setAttribute('data-fc-cached-result', 'pages:0|0:2|500:1'));
      await expect(a).toBeHidden();
      await expect(b).toBeVisible();
      await b.evaluate(label => {
        const replacement = label.cloneNode(true) as HTMLElement;
        label.replaceWith(replacement);
      });
      await expect(b).toBeVisible();
      await b.evaluate(label => label.setAttribute('data-fc-cached-result', 'pages:500|0:3|500:1'));
      await expect(b).toBeVisible();
      await b.evaluate(label => label.setAttribute('data-fc-cached-result', 'pages:500|0:3|500:2'));
      await expect(b).toBeHidden();
      await page.context().setOffline(true);
      await expect(a).toBeVisible();
      await expect(b).toBeVisible();
      await page.context().setOffline(false);
      await a.evaluate(label => label.setAttribute('data-fc-stale-key', 'projection:rebound'));
      await expect(a).toBeHidden();
      await expect(b).toBeVisible();
      await b.evaluate(label => label.setAttribute('data-fc-stale-scope', '2'));
      await expect(b).toBeHidden();
    } finally {
      await page.context().setOffline(false);
      await page.evaluate(async () => {
        const host = document.querySelector<HTMLElement>('#story13-two-cached-pages');
        if (!host) return;
        const modulePath: string = '/_content/Hexalith.FrontComposer.Shell/js/fc-connectivity.js';
        const connectivity = await import(modulePath);
        connectivity.unwatchConnectivityBinding(host, 'two-pages');
        host.remove();
      });
    }
  });

  test('old scroll cleanup leaves the replacement view generation active', async ({ page }) => {
    await page.goto('/counter');
    const result = await page.evaluate(async () => {
      const modulePath: string = '/_content/Hexalith.FrontComposer.Shell/js/fc-datagrid.js';
      const grid = await import(modulePath);
      const calls: number[] = [];
      const first = { invokeMethodAsync: (_: string, _key: string, top: number) => {
        calls.push(top);
        return Promise.resolve();
      } };
      const replacement = { invokeMethodAsync: (_: string, _key: string, top: number) => {
        calls.push(top);
        return Promise.resolve();
      } };
      grid.captureScrollThrottled('story13-remount', 1, first, 1);
      grid.captureScrollThrottled('story13-remount', 2, replacement, 2);
      grid.captureScrollThrottled('story13-remount', 3, replacement, 2);
      grid.disposeViewKey('story13-remount', 1);
      await new Promise(resolve => setTimeout(resolve, 75));
      grid.disposeViewKey('story13-remount', 2);
      return calls;
    });
    expect(result).toContain(3);
  });

  test('co-mounted same-key grids capture restore and dispose their own DOM instances', async ({ page }) => {
    await page.goto('/counter');
    const result = await page.evaluate(async () => {
      const modulePath: string = '/_content/Hexalith.FrontComposer.Shell/js/fc-datagrid.js';
      const grid = await import(modulePath);
      const viewKey = 'story13-co-mounted';
      const calls: Record<string, number[]> = { first: [], second: [], replacement: [] };
      const makeGrid = (instance: string) => {
        const container = document.createElement('div');
        container.setAttribute('data-fc-datagrid', viewKey);
        container.setAttribute('data-fc-datagrid-instance', instance);
        container.innerHTML = '<div class="fluent-data-grid-scroll-container" style="height:10px;overflow:auto">'
          + '<div style="height:1000px"></div></div>';
        document.body.append(container);
        return container.querySelector<HTMLElement>('.fluent-data-grid-scroll-container')!;
      };
      const first = makeGrid('first');
      const second = makeGrid('second');
      const reference = (name: string) => ({ invokeMethodAsync: (_: string, _key: string, top: number) => {
        calls[name].push(top);
        return Promise.resolve();
      } });
      try {
        first.scrollTop = 11;
        second.scrollTop = 21;
        grid.captureScrollThrottled(viewKey, 0, reference('first'), 1, 'first');
        grid.captureScrollThrottled(viewKey, 0, reference('second'), 1, 'second');
        first.scrollTop = 12;
        second.scrollTop = 22;
        grid.captureScrollThrottled(viewKey, 0, reference('first'), 1, 'first');
        grid.captureScrollThrottled(viewKey, 0, reference('second'), 1, 'second');
        grid.disposeViewKey(viewKey, 1, 'first');
        await new Promise(resolve => setTimeout(resolve, 75));
        grid.scrollToOffset(viewKey, 57, 'second');
        const afterRestore = { first: first.scrollTop, second: second.scrollTop };
        const replacement = makeGrid('replacement');
        replacement.scrollTop = 31;
        grid.captureScrollThrottled(viewKey, 0, reference('replacement'), 2, 'replacement');
        grid.disposeViewKey(viewKey, 1, 'first');
        replacement.scrollTop = 32;
        grid.captureScrollThrottled(viewKey, 0, reference('replacement'), 2, 'replacement');
        await new Promise(resolve => setTimeout(resolve, 75));
        grid.disposeViewKey(viewKey, 1, 'second');
        grid.disposeViewKey(viewKey, 2, 'replacement');
        return { calls, afterRestore };
      } finally {
        document.querySelectorAll(`[data-fc-datagrid="${viewKey}"]`).forEach(node => node.remove());
      }
    });
    expect(result.calls.first).toEqual([11]);
    expect(result.calls.second).toEqual([21, 22]);
    expect(result.afterRestore).toEqual({ first: 12, second: 57 });
    expect(result.calls.replacement).toEqual([31, 32]);
  });

  test('rendered second grid forwards its instance through the generated scroll handler', async ({ page, tenant }) => {
    expect(tenant.tenantId).toBeTruthy();
    await page.goto('/counter/counter-projection?story13CoMounted=true');
    await page.locator('.fc-shell-root[data-fc-interactive="true"]').waitFor();
    const grids = page.locator('[data-fc-datagrid="Counter:Counter.Domain.CounterProjection"]');
    await expect(grids).toHaveCount(2);
    const ids = await grids.evaluateAll(nodes => nodes.map(node => node.getAttribute('data-fc-datagrid-instance')));
    expect(ids[0]).toBeTruthy();
    expect(ids[1]).toBeTruthy();
    expect(ids[0]).not.toBe(ids[1]);

    await grids.evaluateAll(nodes => {
      const scrollers = nodes.map(node => {
        const scroller = (node.querySelector('.fluent-data-grid-scroll-container') || node) as HTMLElement;
        scroller.style.height = '24px';
        scroller.style.overflowY = 'auto';
        const spacer = document.createElement('div');
        spacer.style.height = '1000px';
        scroller.append(spacer);
        return scroller;
      });
      scrollers[0].scrollTop = 11;
      scrollers[1].scrollTop = 123;
      scrollers[1].dispatchEvent(new Event('scroll', { bubbles: true }));
    });

    const probe = page.getByTestId('fc-story-13-scroll-probe');
    await expect.poll(async () => {
      await probe.click();
      return Number(await probe.getAttribute('data-fc-scroll-top'));
    }).toBe(123);
  });

  test('browser stale ownership resets on scope change and clears only after the new scope target read', async ({ page }) => {
    await page.goto('/counter');
    await page.locator('.fc-shell-root[data-fc-interactive="true"]').waitFor();
    await page.evaluate(async () => {
      const modulePath: string = '/_content/Hexalith.FrontComposer.Shell/js/fc-connectivity.js';
      const connectivity = await import(modulePath);
      const host = document.createElement('div');
      host.id = 'story13-scope-freshness';
      host.setAttribute('data-fc-connectivity-binding', 'scope-freshness');
      host.innerHTML = '<div role="status"><span data-fc-browser-status></span></div>'
        + '<p data-fc-browser-stale data-fc-stale-key="projection:scope" data-fc-stale-scope="1" '
        + 'data-fc-cached-result="pages:0|0:1" hidden>Cached rows may be old.</p>';
      document.querySelector('.fc-shell-root')!.append(host);
      connectivity.watchConnectivity({ invokeMethodAsync: () => Promise.resolve() }, host, 'You are offline.');
    });
    const stale = page.locator('#story13-scope-freshness [data-fc-browser-stale]');
    try {
      await page.context().setOffline(true);
      await expect(stale).toBeVisible();
      await page.context().setOffline(false);
      await expect(stale).toBeVisible();
      await stale.evaluate(label => label.setAttribute('data-fc-stale-scope', '2'));
      await expect(stale).toBeHidden();
      await page.context().setOffline(true);
      await expect(stale).toBeVisible();
      await page.context().setOffline(false);
      await stale.evaluate(label => label.setAttribute('data-fc-cached-result', 'pages:0|0:1|500:2'));
      await expect(stale).toBeVisible();
      await stale.evaluate(label => label.setAttribute('data-fc-cached-result', 'pages:0|0:2|500:2'));
      await expect(stale).toBeHidden();
    } finally {
      await page.context().setOffline(false);
      await page.evaluate(async () => {
        const host = document.querySelector<HTMLElement>('#story13-scope-freshness');
        if (!host) return;
        const modulePath: string = '/_content/Hexalith.FrontComposer.Shell/js/fc-connectivity.js';
        const connectivity = await import(modulePath);
        connectivity.unwatchConnectivityBinding(host, 'scope-freshness');
        host.remove();
      });
    }
  });

  test('browser offline preserves cached rows and speaks through one connection status without the circuit', async ({ page, tenant }) => {
    expect(tenant.tenantId).toBeTruthy();
    await page.goto('/counter/counter-projection?story13ReadProof=true');
    await page.locator('.fc-shell-root[data-fc-interactive="true"]').waitFor();
    const connection = page.locator('[data-fc-connectivity-binding]');
    await expect(connection).toHaveAttribute('data-fc-connectivity-ready', 'true');
    const rows = page.locator('.fc-projection-grid').getByRole('row');
    await expect(rows.first()).toBeVisible();
    const retainedRows = await rows.count();

    const status = page.getByTestId('fc-projection-connection-status');
    try {
      await page.context().setOffline(true);
      await expect.poll(() => page.evaluate(() => navigator.onLine)).toBe(false);
      await expect(status).toBeVisible();
      await expect(status).toContainText('You are offline');
      await expect(connection.getByTestId('fc-surface-status')).toHaveCount(1);
      await expect(connection.getByTestId('fc-surface-status')).toHaveText('You are offline. Data cannot be refreshed.');
      await expect(page.getByTestId('fc-surface-status').filter({ hasText: 'You are offline' })).toHaveCount(1);
      await expect(page.getByTestId('fc-projection-stale-notice')).toBeVisible();
      await expect(rows).toHaveCount(retainedRows);
      await expect(connection.locator('[role="alert"], [aria-live="assertive"]')).toHaveCount(0);

      await page.context().setOffline(false);
      await expect.poll(() => page.evaluate(() => navigator.onLine)).toBe(true);
      await expect(connection.getByTestId('fc-surface-status')).not.toContainText('You are offline');
      // This cached client-lane specimen has no new read: online alone cannot erase its age warning.
      const staleNotice = page.getByTestId('fc-projection-stale-notice');
      await expect(staleNotice).toBeVisible();
      await expect(rows).toHaveCount(retainedRows);

      const cachedResult = await staleNotice.getAttribute('data-fc-cached-result');
      const retainedLabel = await page.locator('[data-fc-browser-stale]').elementHandle();
      expect(retainedLabel).not.toBeNull();
      const refresh = page.getByTestId('fc-story-13-read-proof');
      await refresh.click();
      await expect(refresh).toHaveAttribute('data-fc-read-attempts', '1');
      await expect(refresh).toHaveAttribute('data-fc-read-count', '1');
      await expect(refresh).toHaveAttribute('data-fc-read-row-id', 'counter-main');
      await expect.poll(() => retainedLabel!.evaluate((label) => label.isConnected)).toBe(true);
      await expect.poll(() => page.locator('[data-fc-browser-stale]').getAttribute('data-fc-cached-result'))
        .not.toBe(cachedResult);
      const refreshedResult = await page.locator('[data-fc-browser-stale]').getAttribute('data-fc-cached-result');
      const pageKey = cachedResult?.match(/^pages:(\d+)\|/)?.[1];
      expect(pageKey).toBeTruthy();
      const pageVersion = (value: string | null) => Number(value?.match(new RegExp(`(?:^|\\|)${pageKey}:(\\d+)`))?.[1] ?? 0);
      expect(pageVersion(refreshedResult)).toBeGreaterThan(pageVersion(cachedResult));
      await expect(page.getByTestId('fc-projection-stale-notice')).toHaveCount(0);
    } finally {
      await page.context().setOffline(false);
    }
  });

  test('a command keeps one polite status and closes with canonical confirmation', async ({ page, lifecycle, tenant }) => {
    expect(tenant.tenantId).toBeTruthy();
    await page.goto('/counter');
    await page.locator('.fc-shell-root[data-fc-interactive="true"]').waitFor();

    const form = page.locator('.fc-command-form[aria-label="Batch Increment command form"]');
    const wrapper = lifecycle.locator('batch-increment');
    const status = wrapper.getByTestId('fc-surface-status');
    await expect(status).toHaveCount(1);
    await expect(form.locator('[role="status"][aria-live="polite"]')).toHaveCount(1);
    await expect(status).toHaveAttribute('aria-live', 'polite');
    await expect(status).toHaveAttribute('aria-atomic', 'true');

    await fillFieldByLabel(form, 'Amount', '2');
    await fillFieldByLabel(form, 'Note', 'QA story 13.4 status');
    await form.getByRole('button', { name: 'Batch Increment' }).click();

    const terminal = await lifecycle.waitForTerminal('batch-increment');
    expect(terminal).toBe('confirmed');
    await expect(status).toHaveText('Command confirmed.');
    await expect(wrapper.locator('[role="alert"], [aria-live="assertive"]')).toHaveCount(0);
    await expect(status).toHaveCount(1);
  });

  test('a hidden expanded row focuses the described Clear filter button once', async ({ page }) => {
    await page.goto('/__frontcomposer/specimens/type?announcementFocus=true');
    await page.locator('.fc-shell-root[data-fc-interactive="true"]').waitFor();

    const fixture = page.getByTestId('fc-announcement-focus-fixture');
    await fixture.getByTestId('fc-focus-hide-detail').click();
    const clearFilter = fixture.getByTestId('fc-expanded-row-hidden-banner-clear');
    await expect(clearFilter).toBeFocused();
    const descriptionId = await clearFilter.getAttribute('aria-describedby');
    expect(descriptionId).toBeTruthy();
    await expect(fixture.locator(`#${descriptionId}`)).toContainText('hidden');

    const laterResult = fixture.getByTestId('fc-focus-repeat-result');
    await laterResult.click();
    await expect(laterResult).toBeFocused();
    await expect(clearFilter).toBeVisible();
  });

  test('continuously hidden pending result speaks without refocusing and a visible transition refocuses', async ({ page }) => {
    await page.goto('/__frontcomposer/specimens/type?announcementFocus=true');
    await page.locator('.fc-shell-root[data-fc-interactive="true"]').waitFor();
    const fixture = page.getByTestId('fc-announcement-focus-fixture');
    const clearFilter = fixture.getByTestId('fc-expanded-row-hidden-banner-clear');
    await fixture.getByTestId('fc-focus-hide-detail').click();
    await expect(clearFilter).toBeFocused();

    await fixture.getByTestId('fc-focus-pending-result').click();
    await expect(clearFilter).toBeVisible();
    const settle = fixture.getByTestId('fc-focus-settle-result');
    await settle.click();
    await expect(settle).toBeFocused();
    const status = fixture.getByTestId('fc-surface-status');
    await expect(status).toHaveText('The expanded row is hidden by the current filters.');
    const revision = await status.locator('[data-message-revision]').getAttribute('data-message-revision');
    await settle.click();
    await expect(status.locator('[data-message-revision]')).toHaveAttribute('data-message-revision', revision!);
    await expect(settle).toBeFocused();

    await fixture.getByTestId('fc-focus-show-detail').click();
    await fixture.getByTestId('fc-focus-hide-detail').click();
    await expect(clearFilter).toBeFocused();
  });

  test('a blocked scope focuses its replacement heading', async ({ page }) => {
    await page.goto('/__frontcomposer/specimens/type?announcementFocus=true');
    await page.locator('.fc-shell-root[data-fc-interactive="true"]').waitFor();

    const fixture = page.getByTestId('fc-announcement-focus-fixture');
    await fixture.getByTestId('fc-focus-block-scope').click();
    const heading = fixture.getByTestId('fc-scope-blocked').getByRole('heading', { level: 1 });
    await expect(heading).toBeFocused();
  });
});
