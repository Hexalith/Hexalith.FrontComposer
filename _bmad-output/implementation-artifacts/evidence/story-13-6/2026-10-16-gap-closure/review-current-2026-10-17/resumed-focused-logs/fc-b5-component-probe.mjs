import { chromium, firefox, webkit } from '/home/administrator/projects/hexalith/frontcomposer/tests/e2e/node_modules/@playwright/test/index.mjs';
import { readFileSync, writeFileSync } from 'node:fs';
import { createHash } from 'node:crypto';
const before = readFileSync('/tmp/fc-focus-before-b5.js', 'utf8');
const after = readFileSync('/home/administrator/projects/hexalith/frontcomposer/src/Hexalith.FrontComposer.Shell/wwwroot/js/fc-focus.js', 'utf8');
const results = [];
for (const [engine, browserType] of Object.entries({ chromium, firefox, webkit })) {
  const browser = await browserType.launch();
  for (const [version, source] of Object.entries({ before, after })) {
    const context = await browser.newContext({ ignoreHTTPSErrors: true });
    const page = await context.newPage();
    await page.route('**/fc-focus.js*', route => route.fulfill({ contentType: 'text/javascript', body: source }));
    await page.goto('https://localhost:7201/__frontcomposer/specimens/type?announcementFocus=true');
    await page.locator('.fc-shell-root[data-fc-interactive="true"]').waitFor();
    const fixture = page.getByTestId('fc-duplicate-missing-detail-fixture');
    await fixture.waitFor();
    await fixture.evaluate(root => {
      const result = { focus: [], domSpeech: [] };
      window.aeProbe = result;
      root.addEventListener('focusin', event => {
        if (event.target instanceof Element && event.target.matches('.fc-expanded-row-hidden-banner-text')) result.focus.push(event.target.closest('[data-fc-datagrid]').getAttribute('data-testid'));
      });
      new MutationObserver(() => { for (const node of root.querySelectorAll('[data-testid="fc-surface-status"]')) if (node.textContent?.trim()) result.domSpeech.push(node.textContent.trim()); }).observe(root, { childList: true, subtree: true, characterData: true });
    });
    await page.getByTestId('fc-duplicate-missing-detail-1').getByTestId('fc-remove-duplicate-focused-detail').focus();
    await page.keyboard.press('Enter');
    await fixture.locator('.fc-expanded-row-hidden-banner-text').nth(1).waitFor();
    await page.waitForFunction(() => document.activeElement?.matches('.fc-expanded-row-hidden-banner-text'));
    const duplicate = await page.evaluate(() => ({ ...window.aeProbe, activeProjection: document.activeElement?.closest('[data-fc-datagrid]')?.getAttribute('data-testid') }));
    const query = await page.evaluate(async () => {
      const focus = await import('/_content/Hexalith.FrontComposer.Shell/js/fc-focus.js');
      const root = document.createElement('div'); root.setAttribute('data-fc-datagrid', 'query-probe');
      root.innerHTML = '<div class="fc-expand-in-row-detail"><button>Old query</button></div><span id="probe-new-query-explanation" tabindex="-1">New query</span>';
      document.body.append(root); focus.trackDetailFocus('query-probe', 'query-probe');
      const button = root.querySelector('button'); button.focus(); history.pushState({}, '', location.pathname + '?query=new'); button.remove();
      const recovered = focus.focusReplacementHeading('query-probe', 'probe-new-query-explanation');
      focus.cancelReplacementFocus('query-probe'); root.remove();
      return { staleRecovery: recovered };
    });
    results.push({ engine, version, moduleSha256: createHash('sha256').update(source).digest('hex'), duplicate, query, expectedPhysicalProjection: 'fc-duplicate-missing-detail-1', heardSpeech: 'not observed' });
    await context.close();
  }
  await browser.close();
}
const output = JSON.stringify({ schema: 'story-13-6-b5-component-comparison-v1', results }, null, 2) + '\n';
writeFileSync('/tmp/fc-b5-component-comparison.json', output);
process.stdout.write(output);
