import {firefox,expect} from '/home/administrator/projects/hexalith/frontcomposer/tests/e2e/node_modules/@playwright/test/index.mjs';
import {applyTextSpacing,settlePaint,inventoryTargets,measureSurfaceContrast,expectNoPageOverflow,expectNoTextClipping} from '/tmp/fc-review3-diagnostic-helper.ts';
const browser=await firefox.launch();
const ready=async(page,path,theme)=>{await page.goto('https://localhost:7201'+path);await page.locator('.fc-shell-root[data-fc-interactive="true"]').waitFor();await page.getByTitle('Change theme').first().click();await page.getByRole('menuitem',{name:theme==='dark'?'Dark':'Light',exact:true}).click();await page.setViewportSize({width:320,height:900});await page.emulateMedia({reducedMotion:'reduce'});await applyTextSpacing(page);await settlePaint(page);};
try{
 const page=await browser.newPage({ignoreHTTPSErrors:true});
 await ready(page,'/counter','dark');await expectNoPageOverflow(page);await expectNoTextClipping(page);await settlePaint(page);await inventoryTargets(page);
 try{await measureSurfaceContrast(page.locator('.fc-shell-root'));}catch(error){console.log('DARK_TEXT_AFTER_INVENTORY',error.message)}
 await ready(page,'/__frontcomposer/specimens/type?generatedGrid=true&accessibilityScenarios=true&density=compact&theme=light','light');await page.getByTestId('fc-ae-lifecycle-rejected').click();await expect(page.getByTestId('fc-lifecycle-ae-state')).toHaveAttribute('data-lifecycle-state','rejected');await page.locator(':focus').first().scrollIntoViewIfNeeded();await settlePaint(page);await page.getByTestId('fc-lifecycle-ae-state').scrollIntoViewIfNeeded();
 const target=page.getByRole('button',{name:'Hide this message',exact:true});
 console.log('POINTER',JSON.stringify(await target.evaluate(element=>{
  const parent=node=>node.assignedSlot??node.parentElement??(node.getRootNode() instanceof ShadowRoot?node.getRootNode().host:null);
  const id=node=>node.tagName+'#'+node.id+'.'+node.className;const box=element.getBoundingClientRect(),chain=[];
  for(let n=element;n;n=parent(n)){const c=getComputedStyle(n);chain.push({element:id(n),rect:n.getBoundingClientRect().toJSON(),overflow:[c.overflowX,c.overflowY],clip:c.clipPath,display:c.display,visibility:c.visibility,pointer:c.pointerEvents,position:c.position,zIndex:c.zIndex,scroll:[n.scrollLeft,n.scrollTop,n.scrollWidth,n.scrollHeight,n.clientWidth,n.clientHeight]});}
  const hits=[];for(const x of [box.left+1,box.left+box.width/2,box.right-1])for(const y of [box.top+1,box.top+box.height/2,box.bottom-1]){let hit=document.elementFromPoint(x,y),path=[];while(hit){path.push({element:id(hit),html:hit.outerHTML.slice(0,180)});if(!hit.shadowRoot)break;const child=hit.shadowRoot.elementFromPoint(x,y);if(!child||child===hit)break;hit=child;}hits.push({x,y,path});}return{html:element.outerHTML,box:box.toJSON(),chain,hits};
 })));
 try{await target.click({timeout:3000});console.log('REAL_DISMISS_CLICK_SUCCEEDED',await target.count());}catch(error){console.log('REAL_DISMISS_CLICK_FAILED',error.message)}
}catch(error){console.log('PROBE_ERROR',error.message)}finally{await browser.close();}
