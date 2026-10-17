import {chromium} from '/home/administrator/projects/hexalith/frontcomposer/tests/e2e/node_modules/@playwright/test/index.mjs';
import {inventoryTargets} from '/home/administrator/projects/hexalith/frontcomposer/tests/e2e/helpers/responsive-accessibility.ts';
const browser=await chromium.launch({headless:true});const page=await browser.newPage();
await page.setContent('<button style="height:30px">Unrelated safe target</button><div id="small" style="position:absolute;left:200px;top:100px;width:10px;height:10px;background:black"></div>');
await page.locator('#small').evaluate(e=>e.addEventListener('click',()=>{window.activated=true}));
console.log('inventory',JSON.stringify(await inventoryTargets(page)));await page.locator('#small').click();console.log('activated',await page.evaluate(()=>window.activated));
await browser.close();
