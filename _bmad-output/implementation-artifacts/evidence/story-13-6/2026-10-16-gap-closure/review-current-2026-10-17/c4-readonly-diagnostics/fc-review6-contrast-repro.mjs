import {chromium,expect} from '/home/administrator/projects/hexalith/frontcomposer/tests/e2e/node_modules/@playwright/test/index.mjs';
import {expectTextContrast} from './fc-review6-helper.mjs';
const browser=await chromium.launch();
try{for(let attempt=0;attempt<6;attempt++){
 const theme=attempt%2?'dark':'light';const page=await browser.newPage({ignoreHTTPSErrors:true,reducedMotion:'reduce'});await page.addInitScript(()=>{localStorage.clear();sessionStorage.clear();const original=getComputedStyle;window.__unsupportedComputed=[];window.getComputedStyle=function(element,pseudo){const style=original.call(this,element,pseudo);return new Proxy(style,{get(target,key){const value=Reflect.get(target,key,target);if((key==='backgroundColor'||key==='color')&&!(String(value).match(/[\d.]+/gu)?.length>=3))window.__unsupportedComputed.push({key,value,tag:element.tagName,id:element.id,connected:element.isConnected,interactive:document.querySelector('.fc-shell-root')?.dataset.fcInteractive});return typeof value==='function'?value.bind(target):value;}});};});await page.goto('https://localhost:7201/__frontcomposer/specimens/type?theme='+theme+'&density=compact');await expect(page.getByTestId('fc-type-specimen')).toBeVisible();
 for(const selector of ['#fc-main-content h1','[data-testid="fc-lifecycle-idle"]']){
  const target=page.locator(selector).first();await expect(target).toBeVisible();try{console.log(attempt,theme,selector,JSON.stringify(await expectTextContrast(target)));}catch(error){console.log('CONTRAST_FAILURE',error.message);console.log('COMPUTED',JSON.stringify(await page.evaluate(()=>window.__unsupportedComputed)));}
 }
 await page.close();
}}finally{await browser.close();}
