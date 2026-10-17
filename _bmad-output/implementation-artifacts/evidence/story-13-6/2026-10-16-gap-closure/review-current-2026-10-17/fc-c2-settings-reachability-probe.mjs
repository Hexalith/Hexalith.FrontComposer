import { chromium, expect } from '/home/administrator/projects/hexalith/frontcomposer/tests/e2e/node_modules/@playwright/test/index.mjs';
import { applyTextSpacing } from '/home/administrator/projects/hexalith/frontcomposer/tests/e2e/helpers/responsive-accessibility.ts';
const browser = await chromium.launch();
try {
  const page = await browser.newPage({ ignoreHTTPSErrors: true, viewport: { width: 320, height: 900 } });
  await page.goto('https://localhost:7201/counter');
  await page.locator('.fc-shell-root[data-fc-interactive="true"]').waitFor();
  await page.getByTestId('fc-settings-button').click();
  await expect(page.getByTestId('fc-settings-dialog')).toBeVisible();
  await applyTextSpacing(page);
  const preview = page.getByRole('button', { name: 'Preview', exact: true });
  const sample = () => preview.evaluate(element => {
    const box = element.getBoundingClientRect();
    const parent = node => node.assignedSlot ?? node.parentElement ?? (node.getRootNode() instanceof ShadowRoot ? node.getRootNode().host : null);
    const scrollports = [];
    for (let node = element; node; node = parent(node)) if (node.scrollHeight > node.clientHeight && /(auto|scroll)/u.test(getComputedStyle(node).overflowY)) scrollports.push({ element: node.tagName + '.' + node.className, scrollTop: node.scrollTop, scrollHeight: node.scrollHeight, clientHeight: node.clientHeight });
    return { box: box.toJSON(), hit: document.elementFromPoint(box.left + box.width / 2, box.top + box.height / 2)?.outerHTML, active: element.getRootNode().activeElement === element, scrollports };
  });
  console.log('before', JSON.stringify(await sample()));
  await preview.scrollIntoViewIfNeeded();
  await preview.click({ timeout: 3000 });
  await expect(preview).toHaveAttribute('aria-expanded', 'true');
  console.log('real click succeeded', JSON.stringify(await sample()));
  await preview.focus();
  console.log('keyboard focus', JSON.stringify(await sample()));
} finally { await browser.close(); }
