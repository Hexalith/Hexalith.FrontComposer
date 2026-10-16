import { chromium } from '/home/administrator/projects/hexalith/frontcomposer/tests/e2e/node_modules/@playwright/test/index.mjs';
import { focusGeometry, expectSurfaceContrast, measureSurfaceContrast } from '/home/administrator/projects/hexalith/frontcomposer/tests/e2e/helpers/responsive-accessibility.ts';
const browser = await chromium.launch({headless:true});
const page = await browser.newPage();
async function run(name, html, action) {
  try { await page.setContent(html); console.log(name, JSON.stringify(await action())); }
  catch (e) { console.log(name, 'REJECTED:', e.message.split('\n')[0]); }
}
await run('pseudo-opacity', '<style>main{background:white;padding:40px}button{appearance:none;background:white;outline:none;position:relative;border:none}button::after{content:"";position:absolute;inset:-3px;border:3px solid black;opacity:0}button:focus::after{opacity:.01}</style><main><button>Save</button></main>', async()=>{await page.locator('button').focus(); return {geometry:await focusGeometry(page.locator('button')),contrast:await expectSurfaceContrast(page.locator('button'),'focus')};});
await run('svg-omitted-line', '<main style="background:white"><svg width="40" height="40"><path fill="black" d="M0 0h10v10H0z"/><line x1="10" y1="20" x2="40" y2="20" stroke="#eee" stroke-width="5"/></svg></main>', ()=>expectSurfaceContrast(page.locator('main'),'icon'));
await run('svg-no-paint', '<main style="background:white"><svg width="40" height="40"><rect x="4" y="4" width="30" height="30" fill="none" stroke="black" stroke-width="0"/></svg></main>', ()=>expectSurfaceContrast(page.locator('main'),'icon'));
await run('filter-zero-contrast', '<main style="background:black"><p style="color:white;background:black;filter:brightness(0)">Invisible text</p></main>', ()=>expectSurfaceContrast(page.locator('p')));
await run('text-fill-zero-contrast', '<main style="background:white"><p style="color:black;-webkit-text-fill-color:white">Invisible text</p></main>', ()=>expectSurfaceContrast(page.locator('p')));
await run('sibling-background-zero-contrast', '<main style="position:relative;background:white"><div style="position:absolute;inset:0;background:black"></div><p style="position:relative;color:black">Invisible text</p></main>', ()=>expectSurfaceContrast(page.locator('p')));
await run('opacity-hidden-focus', '<style>button{margin:40px;outline:none}button:focus{outline:3px solid black}</style><main style="opacity:0"><button>Invisible save</button></main>',async()=>{await page.locator('button').focus();return focusGeometry(page.locator('button'));});
await run('descendant-occlusion', '<style>button{position:absolute;left:40px;top:40px;width:100px;height:40px;outline:none}button:focus{outline:3px solid red}</style><button>Save<span style="position:absolute;inset:-10px;background:black;z-index:100"></span></button>',async()=>{await page.locator('button').focus();return focusGeometry(page.locator('button'));});
await run('pseudo-generated-text', '<style>p{background:white;color:black}p::before{content:"Low contrast generated label";color:#eee}</style><p>Readable</p>', ()=>expectSurfaceContrast(page.locator('p')));
await browser.close();
