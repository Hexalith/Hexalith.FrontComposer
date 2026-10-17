import { stripTypeScriptTypes } from 'node:module';
import { readFileSync, writeFileSync } from 'node:fs';
import { pathToFileURL } from 'node:url';
const repo = '/home/administrator/projects/hexalith/frontcomposer';
const pw = pathToFileURL(`${repo}/tests/e2e/node_modules/@playwright/test/index.mjs`).href;
const { chromium } = await import(pw);
const source = readFileSync(`${repo}/tests/e2e/helpers/responsive-accessibility.ts`, 'utf8');
const helper = await import('data:text/javascript;base64,' + Buffer.from(stripTypeScriptTypes(source).replace("from '@playwright/test'", `from '${pw}'`)).toString('base64'));
const browser = await chromium.launch({ headless: true });
const page = await browser.newPage({ viewport: { width: 800, height: 600 } });
const results = [];
async function probe(id, html, action) {
  await page.setContent(html);
  try { const output = await action(); results.push({ id, html, output }); }
  catch (error) { results.push({ id, html, error: error.message }); }
}
await probe('input-value-clipping', '<input value="Unreadable value" style="width:200px;height:32px;padding:0;font-size:64px;line-height:80px;color:black;background:white">', async () => {
  await helper.expectNoTextClipping(page); return { result: 'PASSED', geometry: await page.locator('input').evaluate(el => ({ width: el.getBoundingClientRect().width, height: el.getBoundingClientRect().height, font: getComputedStyle(el).fontSize })) };
});
await probe('shadow-ancestor-clipping', '<div style="width:20px;height:20px;overflow:hidden"><div id="host"></div></div>', async () => {
  await page.locator('#host').evaluate(el => { el.attachShadow({ mode: 'open' }).innerHTML = '<span>Long shadow content clipped by host ancestor</span>'; });
  await helper.expectNoTextClipping(page); return { result: 'PASSED', shadowHtml: '<span>Long shadow content clipped by host ancestor</span>' };
});
await probe('clip-path-text-clipping', '<p style="color:black;clip-path:inset(0 95% 0 0)">A long label that is almost entirely clipped</p>', async () => {
  await helper.expectNoTextClipping(page); return 'PASSED';
});
await probe('image-overlay-contrast', '<p style="background:white;color:black;position:relative">Label<img style="position:absolute;inset:0;width:100%;height:100%" src="data:image/svg+xml,%3Csvg xmlns=%27http://www.w3.org/2000/svg%27 width=%27800%27 height=%2720%27%3E%3Crect width=%27800%27 height=%2720%27 fill=%27black%27/%3E%3C/svg%3E"></p>', async () => {
  await page.locator('img').evaluate(img => img.decode());
  return { result: 'PASSED', samples: await helper.expectSurfaceContrast(page.locator('p')) };
});
await probe('pseudo-overlay-contrast', '<style>p{position:relative;color:black;background:white}p::after{content:"";position:absolute;inset:0;background:black}</style><p>Label</p>', async () => ({ result: 'PASSED', samples: await helper.expectSurfaceContrast(page.locator('p')) }));
await probe('clipped-target-size', '<div style="width:10px;height:10px;overflow:hidden"><button style="width:100px;height:40px">Action</button></div>', () => helper.inventoryTargets(page));
await probe('omitted-interactive-role', '<span role="menuitemcheckbox" aria-checked="false" style="display:inline-block;width:5px;height:5px">X</span><button style="width:100px;height:40px">Action</button>', async () => {
  await page.locator('span').evaluate(el => el.addEventListener('click', () => {}));
  return helper.inventoryTargets(page);
});
await probe('aria-hidden-visible-target', '<div aria-hidden="true"><button style="width:5px;height:5px;padding:0">X</button></div><button style="width:100px;height:40px">Good</button>', () => helper.inventoryTargets(page));
await probe('scaled-outline-clipping', '<style>button{position:absolute;left:3px;top:50px;width:40px;height:20px;transform-origin:left;transform:scale(3);outline:none}button:focus{outline:2px solid black}</style><button>Action</button>', async () => {
  await page.locator('button').focus(); return helper.focusGeometry(page.locator('button'));
});
await page.route('http://review.local/**', route => route.fulfill({ contentType: 'text/html', body: '<div data-fc-datagrid="v"><div class="fc-expand-in-row-detail"><button id="old">Old</button></div></div><h2 tabindex="-1" id="replacement">Replacement</h2>' }));
await page.goto('http://review.local/projection?item=first');
const moduleSource = 'data:text/javascript;base64,' + readFileSync(`${repo}/src/Hexalith.FrontComposer.Shell/wwwroot/js/fc-focus.js`).toString('base64');
const queryOutput = await page.evaluate(async (source) => {
  const m = await import(source);
  m.trackDetailFocus('query', 'v'); document.getElementById('old').focus();
  history.pushState({}, '', '?item=second'); document.getElementById('old').remove();
  return { changedUrl: location.href, restored: m.focusReplacementHeading('query', 'replacement'), activeElement: document.activeElement.id };
}, moduleSource);
results.push({ id: 'same-path-query-navigation-focus', output: queryOutput });
await page.goto('http://review.local/many');
const evictionOutput = await page.evaluate(async (source) => {
  const m = await import(source);
  for (let i = 0; i < 33; i++) m.trackDetailFocus('tracker-' + i, i === 0 ? 'v' : 'other-' + i);
  document.getElementById('old').focus(); document.getElementById('old').remove();
  return { restored: m.focusReplacementHeading('tracker-0', 'replacement'), activeElement: document.activeElement.tagName };
}, moduleSource);
results.push({ id: 'active-focus-tracker-eviction', output: evictionOutput });
await browser.close();
writeFileSync('/tmp/story-13-6-blind-review-repros.json', JSON.stringify(results, null, 2));
for (const result of results) console.log(JSON.stringify(result));
