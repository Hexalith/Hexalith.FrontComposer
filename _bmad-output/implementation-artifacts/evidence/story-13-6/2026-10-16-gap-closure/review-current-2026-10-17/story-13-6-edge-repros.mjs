import { chromium } from '/home/administrator/projects/hexalith/frontcomposer/tests/e2e/node_modules/@playwright/test/index.mjs';
import { readFileSync } from 'node:fs';
import { expectNoTextClipping, expectSurfaceContrast, focusGeometry } from '/home/administrator/projects/hexalith/frontcomposer/tests/e2e/helpers/responsive-accessibility.ts';
const browser = await chromium.launch({ headless: true });
try {
  const page = await browser.newPage();
  await page.setContent('<div id="host" style="display:block;width:60px;height:20px;overflow:hidden"></div>');
  await page.locator('#host').evaluate(element => { element.attachShadow({ mode: 'open' }).innerHTML = '<span style="white-space:nowrap">This label is clipped by its host</span>'; });
  const bounds = await page.locator('#host').evaluate(element => { const range = document.createRange(); range.selectNodeContents(element.shadowRoot.querySelector('span')); return { host: element.getBoundingClientRect().toJSON(), text: range.getBoundingClientRect().toJSON() }; });
  await expectNoTextClipping(page);
  console.log('SHADOW_CLIPPING_PASSED', JSON.stringify(bounds));
  await page.setContent('<style>body{background:white}#under{position:absolute;left:0;top:0;width:300px;height:200px;background:black}button{position:relative;margin:40px;width:100px;height:40px;outline:none;background:white;border:0}button:focus{outline:2px solid black}</style><div id="under"></div><button>Action</button>');
  await page.locator('button').focus();
  console.log('FOCUS_GEOMETRY', JSON.stringify(await focusGeometry(page.locator('button'))));
  console.log('FOCUS_CONTRAST', JSON.stringify(await expectSurfaceContrast(page.locator('button'), 'focus')));
  await page.setContent('<div data-fc-datagrid="same"><p id="first" tabindex="-1">First missing explanation</p></div><div data-fc-datagrid="same"><div class="fc-expand-in-row-detail"><button id="second-detail">Second detail</button></div><p id="second" tabindex="-1">Second missing explanation</p></div>');
  const focusSource = readFileSync('/home/administrator/projects/hexalith/frontcomposer/src/Hexalith.FrontComposer.Shell/wwwroot/js/fc-focus.js', 'utf8');
  const duplicate = await page.evaluate(async source => {
    const module = await import(URL.createObjectURL(new Blob([source], { type: 'text/javascript' })));
    module.trackDetailFocus('first-tracker', 'same'); module.trackDetailFocus('second-tracker', 'same');
    document.querySelector('#second-detail').focus();
    document.querySelector('.fc-expand-in-row-detail').remove();
    const recovered = module.focusReplacementHeading('first-tracker', 'first');
    return { recovered, activeElement: document.activeElement.id };
  }, focusSource);
  console.log('DUPLICATE_PROJECTION_RECOVERY', JSON.stringify(duplicate));
} finally { await browser.close(); }
