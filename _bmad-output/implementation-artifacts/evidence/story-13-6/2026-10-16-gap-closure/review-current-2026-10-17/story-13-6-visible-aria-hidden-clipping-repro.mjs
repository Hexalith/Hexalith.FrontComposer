import { chromium } from '/home/administrator/projects/hexalith/frontcomposer/tests/e2e/node_modules/playwright-core/index.mjs';
import { expectNoTextClipping } from '/home/administrator/projects/hexalith/frontcomposer/tests/e2e/helpers/responsive-accessibility.ts';
const browser=await chromium.launch({headless:true});
const page=await browser.newPage();
await page.setContent('<div style="width:10px;height:10px;overflow:hidden" aria-hidden="true"><span>This is a visible, clipped theme label.</span></div>');
let result;
try{await expectNoTextClipping(page);result='passed';}catch(e){result=e.message;}
console.log('aria-hidden visible clipping:',result);
await page.locator('div').evaluate(el=>el.removeAttribute('aria-hidden'));
try{await expectNoTextClipping(page);result='passed';}catch(e){result=e.message.split('\n')[0];}
console.log('same visible clipping without aria-hidden:',result);
await browser.close();
