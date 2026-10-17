import { chromium } from '/home/administrator/projects/hexalith/frontcomposer/tests/e2e/node_modules/@playwright/test/index.mjs';
import { applyTextSpacing,expectNoTextClipping } from '/home/administrator/projects/hexalith/frontcomposer/tests/e2e/helpers/responsive-accessibility.ts';
const b=await chromium.launch();const p=await b.newPage({ignoreHTTPSErrors:true,viewport:{width:320,height:900}});
await p.goto('https://localhost:7201/__frontcomposer/specimens/type?generatedGrid=true&accessibilityScenarios=true&density=compact');await p.locator('.fc-shell-root[data-fc-interactive="true"]').waitFor();await applyTextSpacing(p);
try{await expectNoTextClipping(p);}catch(e){console.log(e.message.slice(0,700));}
console.log(JSON.stringify(await p.evaluate(()=>{const all=[];const walk=r=>{for(const e of r.querySelectorAll('*')){all.push(e);if(e.shadowRoot)walk(e.shadowRoot);}};walk(document);return all.flatMap(e=>['::before','::after'].flatMap(p=>{const s=getComputedStyle(e,p);return !['none','normal','""',"''",''].includes(s.content)?[{tag:e.tagName,id:e.id,part:e.getAttribute('part'),pseudo:p,content:s.content,display:s.display,visibility:s.visibility,width:s.width,height:s.height,position:s.position,clip:s.clipPath,overflow:s.overflow,parent:e.parentElement?.outerHTML.slice(0,300)}]:[];}));}),null,2));
await b.close();
