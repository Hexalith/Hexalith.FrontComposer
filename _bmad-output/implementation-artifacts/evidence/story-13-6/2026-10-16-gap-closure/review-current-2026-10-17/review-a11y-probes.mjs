import { chromium } from '/home/administrator/projects/hexalith/frontcomposer/tests/e2e/node_modules/@playwright/test/index.mjs';
import { expectNoTextClipping,measureSurfaceContrast,inventoryTargets,focusGeometry } from '/home/administrator/projects/hexalith/frontcomposer/tests/e2e/helpers/responsive-accessibility.ts';
const browser=await chromium.launch({headless:true}); const page=await browser.newPage({viewport:{width:600,height:400}});
async function probe(name,html,run){await page.setContent(html);try{console.log(name,JSON.stringify(await run()));}catch(e){console.log(name,'THROW',e.message.slice(0,600));}}
await probe('ariahiddenclip','<p aria-hidden="true" style="width:10px;height:8px;overflow:hidden;white-space:nowrap">Important long visible label</p>',async()=>{await expectNoTextClipping(page);return 'PASS';});
await probe('textareaclip','<textarea style="font-size:32px;height:10px;padding:0;border:0;resize:none"></textarea>',async()=>{await page.locator('textarea').fill('Important label');await expectNoTextClipping(page);return page.locator('textarea').evaluate(e=>({scrollHeight:e.scrollHeight,clientHeight:e.clientHeight,value:e.value}));});
await probe('pseudoclip','<style>p::before{content:"Important long visible label"}p{width:10px;height:8px;overflow:hidden;white-space:nowrap}</style><p></p>',async()=>{await expectNoTextClipping(page);return 'PASS';});
await probe('inputbaseline','<input value="Important label" style="font-size:32px;height:40px;line-height:150px;padding:0;border:0">',async()=>{await expectNoTextClipping(page);return page.locator('input').evaluate(e=>({scrollHeight:e.scrollHeight,clientHeight:e.clientHeight}));});
await probe('shadowtext','<p style="color:black;text-shadow:0 0 0 white,0 0 0 white;background:white">Hello</p>',()=>measureSurfaceContrast(page.locator('p')));
await probe('bordernoContrast','<button style="background:black;border:4px solid black;color:white">Save</button>',()=>measureSurfaceContrast(page.locator('button'),'border'));
await probe('occludedtarget','<button style="position:absolute;left:40px;top:40px;width:30px;height:30px;padding:0"></button><div style="position:absolute;left:40px;top:40px;width:29px;height:30px;background:black"></div>',()=>inventoryTargets(page));
await browser.close();
