import { chromium, expect } from '/home/administrator/projects/hexalith/frontcomposer/tests/e2e/node_modules/@playwright/test/index.mjs';
import { expectFocusVisible } from '/home/administrator/projects/hexalith/frontcomposer/tests/e2e/helpers/responsive-accessibility.ts';
import { readFileSync } from 'node:fs';
import { stripTypeScriptTypes } from 'node:module';
const source=readFileSync('/home/administrator/projects/hexalith/frontcomposer/tests/e2e/specs/responsive-accessibility.spec.ts','utf8');
const fragment=source.slice(source.indexOf('async function verifyKeyboardCycle('),source.indexOf("test('row metrics preserve"));
const verifyKeyboardCycle=eval('('+stripTypeScriptTypes(fragment,{mode:'strip'})+')');
const browser=await chromium.launch({headless:true});
for(let n=0;n<3;n++) {const page=await browser.newPage();
await page.setContent('<style>button{margin:8px;outline:none}button:focus{outline:2px solid}</style><button id="a">First</button><button id="b">Last</button>');
await page.locator('#b').evaluate(button=>button.addEventListener('keydown',event=>{if(event.key==='Tab'&&!event.shiftKey)setTimeout(()=>button.focus(),0)}));
try { await verifyKeyboardCycle(page); console.log('cycle',n,'PASSED'); } catch(e) {console.log('cycle',n,'REJECTED',e.message.split('\n')[0])}
await page.locator('#b').focus();await page.keyboard.press('Tab');await page.waitForTimeout(50);console.log('after real Tab',await page.evaluate(()=>({focused:document.activeElement.id,documentFocused:document.hasFocus()})));await page.close();}
await browser.close();
