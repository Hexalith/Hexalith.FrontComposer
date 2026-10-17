import { firefox } from '/home/administrator/projects/hexalith/frontcomposer/tests/e2e/node_modules/playwright/index.mjs';
import { applyTextSpacing, settlePaint, expectSurfaceContrast } from '/home/administrator/projects/hexalith/frontcomposer/tests/e2e/helpers/responsive-accessibility.ts';
import { writeFile } from 'node:fs/promises';
const browser = await firefox.launch();
try {
  const page = await browser.newPage();
  await page.goto('http://localhost:5201/counter');
  await page.locator('.fc-shell-root[data-fc-interactive="true"]').waitFor();
  await page.getByTitle('Change theme').first().click();
  await page.getByRole('menuitem', { name: 'Dark', exact: true }).click();
  await page.setViewportSize({ width: 320, height: 900 });
  await page.emulateMedia({ reducedMotion: 'reduce' });
  await applyTextSpacing(page); await settlePaint(page);
  const footer = page.locator('span').filter({ hasText: /©/ }).last();
  const geometry = await footer.evaluate(label => {
    const parent = node => node.assignedSlot ?? node.parentElement ?? (node.getRootNode() instanceof ShadowRoot ? node.getRootNode().host : null);
    const range = document.createRange(); range.selectNodeContents(label);
    const text = range.getBoundingClientRect();
    const candidates = [];
    const collect = root => {
      for (const node of root.querySelectorAll('*')) {
        if (node.shadowRoot) collect(node.shadowRoot);
        const box = node.getBoundingClientRect(), css = getComputedStyle(node);
        if (!(node.getRootNode() instanceof ShadowRoot) || css.backgroundColor === 'rgba(0, 0, 0, 0)' || box.top > text.bottom + 50 || box.bottom < text.top - 50) continue;
        const ancestors = [];
        for (let current = node; current; current = parent(current)) {
          const style = getComputedStyle(current), bounds = current.getBoundingClientRect();
          ancestors.push({ node: current.tagName + '#' + current.id + '.' + current.className, overflowX: style.overflowX, overflowY: style.overflowY, position: style.position, rect: bounds.toJSON() });
        }
        candidates.push({ host: node.getRootNode().host.tagName, node: node.tagName + '.' + node.className, rect: box.toJSON(), rawOverlap: Math.min(box.bottom, text.bottom) - Math.max(box.top, text.top), ancestors });
      }
    };
    collect(document); return { text: text.toJSON(), candidates };
  });
  const samples = await expectSurfaceContrast(footer);
  await writeFile('/tmp/story-13-6-stabilize-20261017/real-paint-probe.json', JSON.stringify({ viewport: page.viewportSize(), theme: 'dark', reducedMotion: true, spacing: 'all four overrides', geometry, samples }, null, 2));
  console.log(JSON.stringify({ candidateCount: geometry.candidates.length, rawOverlap: geometry.candidates.map(candidate => candidate.rawOverlap), passedTextSamples: samples.length }));
} finally { await browser.close(); }
