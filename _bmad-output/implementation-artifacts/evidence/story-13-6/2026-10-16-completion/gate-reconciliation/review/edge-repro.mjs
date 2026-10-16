import { chromium } from '/home/administrator/projects/hexalith/frontcomposer/tests/e2e/node_modules/@playwright/test/index.mjs';
import { focusGeometry, expectSurfaceContrast } from '/home/administrator/projects/hexalith/frontcomposer/tests/e2e/helpers/responsive-accessibility.ts';
const browser=await chromium.launch({headless:true});
const page=await browser.newPage({viewport:{width:640,height:480}});
const cases={
negativeSpreadOffset:'button{box-shadow:none}button:focus{box-shadow:10px 0 0 -100px black}',
unchangedShadowLayer:'button{box-shadow:0 0 0 4px black}button:focus{box-shadow:0 0 0 4px black,0 0 0 6px white}',
insetPseudo:'button{background:black!important;color:white;position:relative;width:100px;height:40px}button::after{content:"";position:absolute;inset:5px;border:2px solid black;transform:scaleX(0)}button:focus::after{transform:scaleX(1)}',
pseudoOpacity:'button{position:relative;width:100px;height:40px}button::after{content:"";position:absolute;inset:0;border:2px solid black;opacity:0}button:focus::after{opacity:.1}'
};
for(const [name,css] of Object.entries(cases)){
await page.setContent('<style>body{background:white}button{margin:40px;background:white;appearance:none;outline:none;border:0}'+css+'</style><button>Save</button>');
const resting=await page.screenshot(); await page.locator('button').focus(); const focused=await page.screenshot();
console.log(JSON.stringify({name,identicalPaint:resting.equals(focused),geometry:await focusGeometry(page.locator('button')),contrast:await expectSurfaceContrast(page.locator('button'),'focus')}));
}
await page.setContent('<main style="background:white"><svg width="80" height="40"><rect width="20" height="20" fill="black"/><ellipse cx="50" cy="20" rx="15" ry="15" fill="#eee"/></svg></main>');
console.log(JSON.stringify({name:'ellipseOmitted',contrast:await expectSurfaceContrast(page.locator('main'),'icon')}));
await page.setContent('<main style="background:white;color:black"><p>Readable</p><input placeholder="Low contrast placeholder" style="color:black"><style>input::placeholder{color:black;opacity:.1}</style></main>');
console.log(JSON.stringify({name:'placeholderOpacity',contrast:await expectSurfaceContrast(page.locator('main'))}));
await browser.close();
