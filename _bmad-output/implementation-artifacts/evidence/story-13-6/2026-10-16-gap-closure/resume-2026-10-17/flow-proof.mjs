import { execFileSync } from 'node:child_process';
import { pathToFileURL } from 'node:url';
import { stripTypeScriptTypes } from 'node:module';
import { writeFileSync } from 'node:fs';
const root=process.cwd();
const { chromium, firefox, webkit }=await import(pathToFileURL(root+'/tests/e2e/node_modules/playwright/index.mjs'));
const source=execFileSync('git',['show','HEAD:tests/e2e/helpers/responsive-accessibility.ts'],{encoding:'utf8'}).replace('@playwright/test',pathToFileURL(root+'/tests/e2e/node_modules/@playwright/test/index.mjs').href);
const old=await import('data:text/javascript;base64,'+Buffer.from(stripTypeScriptTypes(source)).toString('base64'));
const current=await import(pathToFileURL(root+'/tests/e2e/helpers/responsive-accessibility.ts'));
const results=[];
for (const [engine,launcher] of Object.entries({chromium,firefox,webkit})) {
 const browser=await launcher.launch();
 try {
  const page=await browser.newPage();
  await page.setContent('<style>a{display:block;margin:40px;width:100px;height:40px;outline:none}a:focus{outline:2px solid black}span{display:block;width:140px;height:80px;margin:-50px 0 0 -20px;background:white}</style><a href="#">Action<span></span></a>');
  await page.locator('a').focus();
  const before=await old.focusGeometry(page.locator('a')),after=await current.focusGeometry(page.locator('a'));
  if(before.failures.length||!after.failures.includes('app content obscures target or focus ring')) throw new Error(engine+' did not distinguish the original defect');
  results.push({engine,before,after});
 } finally {await browser.close();}
}
writeFileSync('/tmp/story-13-6-flow-proof.json',JSON.stringify(results,null,2)+'\n');
console.log('Original helper falsely passes and patched helper rejects ordinary-flow coverage in all three engines.');
