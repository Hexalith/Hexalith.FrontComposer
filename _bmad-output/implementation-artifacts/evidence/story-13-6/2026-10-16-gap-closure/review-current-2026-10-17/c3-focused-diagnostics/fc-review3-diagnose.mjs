import {firefox,expect} from '/home/administrator/projects/hexalith/frontcomposer/tests/e2e/node_modules/@playwright/test/index.mjs';
import {applyTextSpacing,settlePaint,inventoryTargets,measureSurfaceContrast} from '/tmp/fc-review3-diagnostic-helper.ts';
const browser=await firefox.launch();
const ready=async(page,path,theme)=>{await page.goto('https://localhost:7201'+path);await page.locator('.fc-shell-root[data-fc-interactive="true"]').waitFor();await page.getByTitle('Change theme').first().click();await page.getByRole('menuitem',{name:theme==='dark'?'Dark':'Light',exact:true}).click();await page.setViewportSize({width:320,height:900});await page.emulateMedia({reducedMotion:'reduce'});await applyTextSpacing(page);await settlePaint(page);};
try {
 const page=await browser.newPage({ignoreHTTPSErrors:true});
 await ready(page,'/counter','dark');
 try{await measureSurfaceContrast(page.locator('.fc-shell-root'));}catch(error){console.log('DARK_TEXT',error.message)}
 await page.screenshot({path:'/tmp/fc-review3-dark-text.png',fullPage:true});
 for(const theme of ['light','dark']){
  await ready(page,'/__frontcomposer/specimens/type?generatedGrid=true&accessibilityScenarios=true&density=compact&theme='+theme,theme);
  for(const state of ['idle','submitting','acknowledged','syncing','confirmed','rejected']){
   await page.getByTestId('fc-ae-lifecycle-'+state.replaceAll('-','')).click();await expect(page.getByTestId('fc-lifecycle-ae-state')).toHaveAttribute('data-lifecycle-state',state);
   if(state==='rejected'){const focused=page.locator(':focus').first();if(await focused.count()){await focused.scrollIntoViewIfNeeded();await settlePaint(page);}await page.getByTestId('fc-lifecycle-ae-state').scrollIntoViewIfNeeded();}
   const results=await inventoryTargets(page);const failures=results.inventory.filter(target=>target.failure);
   console.log('TARGETS',theme,state,JSON.stringify(failures));
   if(failures.length){await page.screenshot({path:'/tmp/fc-review3-target-'+theme+'-'+state+'.png'});break;}
  }
 }
}finally{await browser.close();}
