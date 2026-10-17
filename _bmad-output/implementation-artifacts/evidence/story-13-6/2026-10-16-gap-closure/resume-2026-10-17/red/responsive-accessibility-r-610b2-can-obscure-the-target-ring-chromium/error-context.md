# Instructions

- Following Playwright test failed.
- Explain why, be concise, respect Playwright best practices.
- Provide a snippet of code with the fix, if possible.

# Test info

- Name: responsive-accessibility.spec.ts >> resume review ordinary flow descendant can obscure the target ring
- Location: specs/responsive-accessibility.spec.ts:1284:1

# Error details

```
Error: expect(received).toContain(expected) // indexOf

Expected value: "app content obscures target or focus ring"
Received array: []
```

# Page snapshot

```yaml
- button "Action" [active] [ref=e2]
```

# Test source

```ts
  1187 |     if (document.activeElement !== svg) throw new Error('SVG fixture did not receive focus');
  1188 |     focus.restoreOverlayOrigin();
  1189 |     await new Promise<void>(resolve => requestAnimationFrame(() => requestAnimationFrame(() => resolve())));
  1190 |   });
  1191 |   await expect(page.locator('#operator-svg')).toBeFocused();
  1192 | });
  1193 | 
  1194 | 
  1195 | for (const effect of ['mix-blend-mode:multiply', 'backdrop-filter:brightness(0)']) {
  1196 |   test(`review actual generated pseudo composition rejects ${effect}`, async ({ page }) => {
  1197 |     await page.setContent(`<style>div{background:white}div::before{content:"Label";color:black;${effect}}</style><div></div>`);
  1198 |     await expect(measureSurfaceContrast(page.locator('div'))).rejects.toThrow(/Filtered or blended/);
  1199 |     await page.addStyleTag({ content: 'div::before{mix-blend-mode:normal;backdrop-filter:none}' });
  1200 |     await expectSurfaceContrast(page.locator('div'));
  1201 |   });
  1202 | }
  1203 | 
  1204 | 
  1205 | test('review translucent permanent shadow cannot certify covered changed focus paint', async ({ page }) => {
  1206 |   await page.setContent('<style>button{margin:40px;width:100px;height:40px;background:white;outline:none;border:0;border-radius:0;box-shadow:0 0 0 3px rgb(255 255 255 / .95)}button:focus{box-shadow:0 0 0 3px rgb(255 255 255 / .95),0 0 0 2px black}</style><button>Action</button>');
  1207 |   await page.locator('button').focus();
  1208 |   await expect(focusGeometry(page.locator('button'))).rejects.toThrow(/Overlapping translucent shadow layers/);
  1209 |   await expect(measureSurfaceContrast(page.locator('button'), 'focus')).rejects.toThrow(/Overlapping translucent shadow layers/);
  1210 |   await page.addStyleTag({ content: 'button{box-shadow:0 0 0 3px white}button:focus{box-shadow:0 0 0 3px white,0 0 0 5px black}' });
  1211 |   expect((await focusGeometry(page.locator('button'))).failures).toEqual([]);
  1212 |   await expectSurfaceContrast(page.locator('button'), 'focus');
  1213 |   await page.addStyleTag({ content: 'button{box-shadow:none}button:focus{box-shadow:0 0 2px rgb(0 0 0 / .12),0 2px 4px rgb(0 0 0 / .14),0 0 0 2px black}' });
  1214 |   expect((await focusGeometry(page.locator('button'))).failures).toEqual([]);
  1215 |   const light = await expectSurfaceContrast(page.locator('button'), 'focus');
  1216 |   expect(light.some(sample => sample.measurement === 'conservative-bound' && sample.ratio >= 3 && sample.ratio < 21)).toBe(true);
  1217 |   await page.addStyleTag({ content: 'body,button{background:rgb(41,41,41)}button:focus{box-shadow:0 0 2px rgb(0 0 0 / .24),0 2px 4px rgb(0 0 0 / .28),0 0 0 2px white}' });
  1218 |   const geometry = await focusGeometry(page.locator('button'));
  1219 |   expect(geometry.failures).toEqual([]);
  1220 |   expect(geometry.shadowBounds).toHaveLength(1);
  1221 |   const dark = await expectSurfaceContrast(page.locator('button'), 'focus');
  1222 |   const bound = dark.find(sample => sample.measurement === 'conservative-bound')!.shadowBound!;
  1223 |   expect(bound.ratioMinimum).toBeGreaterThanOrEqual(3);
  1224 |   expect(bound.layers).toHaveLength(2);
  1225 |   expect(bound.backgroundLayers).toHaveLength(2);
  1226 |   expect(bound.layers.every(layer => layer.maximumAlpha >= layer.declaredAlpha)).toBe(true);
  1227 |   expect([...bound.foreground.minimum, ...bound.foreground.maximum, ...bound.background.minimum, ...bound.background.maximum].every(Number.isInteger)).toBe(true);
  1228 |   expect(bound.foreground.minimum.every((value, i) => value < bound.foreground.maximum[i])).toBe(true);
  1229 |   expect(bound.background.minimum.every((value, i) => value < bound.background.maximum[i])).toBe(true);
  1230 |   // A later black blur can erase contrast against a light adjacent background;
  1231 |   // the foreground-only overlays cannot bound that composition on their own.
  1232 |   await page.addStyleTag({ content: 'body,button{background:white}button:focus{box-shadow:0 0 2px rgb(0 0 0 / .12),0 0 0 2px black,0 0 12px rgb(0 0 0 / .9)}' });
  1233 |   await expect(focusGeometry(page.locator('button'))).rejects.toThrow(/Overlapping translucent shadow layers/);
  1234 |   await expect(measureSurfaceContrast(page.locator('button'), 'focus')).rejects.toThrow(/Overlapping translucent shadow layers/);
  1235 |   // Canvas can quantize .999 to opaque; a blur still has spatial alpha below one.
  1236 |   await page.addStyleTag({ content: 'body,button{background:black}button:focus{box-shadow:0 0 2px 20px rgb(0 0 0 / .999),0 0 0 2px white}' });
  1237 |   await expect(focusGeometry(page.locator('button'))).rejects.toThrow(/Overlapping translucent shadow layers/);
  1238 |   await expect(measureSurfaceContrast(page.locator('button'), 'focus')).rejects.toThrow(/Overlapping translucent shadow layers/);
  1239 |   await page.addStyleTag({ content: 'body,button{background:color(srgb .16 .16 .16)}button:focus{box-shadow:0 0 2px rgb(0 0 0 / .24),0 0 0 2px white}' });
  1240 |   await expect(measureSurfaceContrast(page.locator('button'), 'focus')).rejects.toThrow(/Shadow background color/);
  1241 | });
  1242 | 
  1243 | 
  1244 | test('review normal-flow generated label spacing remains measured', async ({ page }) => {
  1245 |   await page.setContent('<style>label{background:white;color:black}label::after{content:"*" / "";margin-left:4px;color:black}</style><label>Required field</label>');
  1246 |   expect((await expectSurfaceContrast(page.locator('label'))).some(sample => sample.paint === '::after generated text')).toBe(true);
  1247 |   await page.addStyleTag({ content: 'label::after{margin-left:-20px}' });
  1248 |   await expect(measureSurfaceContrast(page.locator('label'))).rejects.toThrow(/Generated text geometry/);
  1249 | });
  1250 | 
  1251 | test('review clipped text cannot overlap an adjacent static footer but visible overlap is rejected', async ({ page }) => {
  1252 |   await page.setContent('<style>body{background:white}#root{position:fixed;left:40px;top:40px;width:200px}#port{height:30px;overflow:hidden;background:white}p{margin:0;font-size:12px;line-height:20px;color:black}footer{height:30px;background:black}</style><div id="root"><div id="port"><p>Visible line<br>Partly clipped line</p></div><footer></footer></div>');
  1253 |   await expectSurfaceContrast(page.locator('p'));
  1254 |   await page.addStyleTag({ content: '#port{height:60px}footer{margin-top:-30px}' });
  1255 |   await expect(measureSurfaceContrast(page.locator('p'))).rejects.toThrow(/Overlapping sibling background/);
  1256 | });
  1257 | 
  1258 | 
  1259 | test('review opaque native top-layer surfaces shield text from page backgrounds only', async ({ page }) => {
  1260 |   await page.setContent('<style>body{background:white}#under{position:fixed;inset:0;background:black}dialog{background:white;color:black}p{position:relative}</style><div id="under"></div><dialog><p>Modal label</p></dialog>');
  1261 |   await page.locator('dialog').evaluate(dialog => (dialog as HTMLDialogElement).showModal());
  1262 |   await expectSurfaceContrast(page.locator('p'));
  1263 |   await page.evaluate(() => {
  1264 |     const overlay = document.createElement('div'); overlay.id = 'over'; overlay.setAttribute('popover', 'manual');
  1265 |     overlay.style.cssText = 'position:fixed;inset:0;width:100vw;height:100vh;margin:0;background:black';
  1266 |     document.body.append(overlay); overlay.showPopover();
  1267 |   });
  1268 |   await expect(measureSurfaceContrast(page.locator('p'))).rejects.toThrow(/Overlapping sibling background/);
  1269 |   await page.locator('#over').evaluate(overlay => { (overlay as HTMLElement).hidePopover(); overlay.remove(); });
  1270 |   await page.locator('dialog').evaluate(dialog => { const layer = document.createElement('div'); layer.style.cssText = 'position:absolute;inset:0;background:black'; dialog.prepend(layer); });
  1271 |   await expect(measureSurfaceContrast(page.locator('p'))).rejects.toThrow(/Overlapping sibling background/);
  1272 | });
  1273 | 
  1274 | test('resume review offset opaque shadow cannot hide changed focus paint', async ({ page }) => {
  1275 |   await page.setContent('<style>body{background:white}button{margin:40px;width:100px;height:40px;background:white;outline:none;border:0;border-radius:0;box-shadow:1px 0 0 10px white}button:focus{box-shadow:1px 0 0 10px white,0 0 0 2px black}</style><button>Action</button>');
  1276 |   await page.locator('button').focus();
  1277 |   expect((await focusGeometry(page.locator('button'))).failures).toContain('focused target has no visible focus indicator');
  1278 |   await expect(expectSurfaceContrast(page.locator('button'), 'focus')).rejects.toThrow(/Nonempty/);
  1279 |   await page.addStyleTag({ content: 'button:focus{box-shadow:1px 0 0 10px white,0 0 0 14px black}' });
  1280 |   expect((await focusGeometry(page.locator('button'))).failures).toEqual([]);
  1281 |   await expectSurfaceContrast(page.locator('button'), 'focus');
  1282 | });
  1283 | 
  1284 | test('resume review ordinary flow descendant can obscure the target ring', async ({ page }) => {
  1285 |   await page.setContent('<style>button{margin:40px;width:100px;height:40px;outline:none}button:focus{outline:2px solid black}span{display:block;width:140px;height:80px;margin:-50px 0 0 -20px;background:white}</style><button>Action<span></span></button>');
  1286 |   await page.locator('button').focus();
> 1287 |   expect((await focusGeometry(page.locator('button'))).failures).toContain('app content obscures target or focus ring');
       |                                                                  ^ Error: expect(received).toContain(expected) // indexOf
  1288 |   await page.locator('span').evaluate(node => { node.style.cssText = 'width:10px;height:10px;margin:0'; });
  1289 |   expect((await focusGeometry(page.locator('button'))).failures).toEqual([]);
  1290 | });
  1291 | 
  1292 | test('resume review fully clipped pseudo borders supply no contrast evidence', async ({ page }) => {
  1293 |   await page.setContent('<style>button{position:relative;margin:40px;width:100px;height:40px;background:white;outline:none;border:0}button::after{content:"";position:absolute;inset:0;border:2px solid black;clip-path:inset(50%)}</style><button>Action</button>');
  1294 |   for (const kind of ['border', 'cue'] as const) await expect(expectSurfaceContrast(page.locator('button'), kind)).rejects.toThrow(/Nonempty|Clipped pseudo/);
  1295 |   await page.addStyleTag({ content: 'button::after{clip-path:none}' });
  1296 |   for (const kind of ['border', 'cue'] as const) await expectSurfaceContrast(page.locator('button'), kind);
  1297 | });
  1298 | 
  1299 | test('resume review overflowing generated text cannot borrow its host background', async ({ page }) => {
  1300 |   await page.setContent('<style>body{background:black}div{background:white;color:black;width:50px;white-space:nowrap}div::after{content:"Long generated label that overflows"}</style><div></div>');
  1301 |   await expect(measureSurfaceContrast(page.locator('div'))).rejects.toThrow(/Generated text geometry/);
  1302 |   await page.addStyleTag({ content: 'div{width:500px}' });
  1303 |   await expectSurfaceContrast(page.locator('div'));
  1304 | });
  1305 | 
  1306 | test('resume review descendant background paint cannot cover sampled text', async ({ page }) => {
  1307 |   await page.setContent('<style>p{position:relative;background:white;color:black}span{position:absolute;inset:0;background:black;z-index:1}</style><p>Visible label<span></span></p>');
  1308 |   await expect(measureSurfaceContrast(page.locator('p'))).rejects.toThrow(/Overlapping sibling background/);
  1309 |   await page.locator('span').evaluate(node => node.remove());
  1310 |   await expectSurfaceContrast(page.locator('p'));
  1311 | });
  1312 | 
  1313 | for (const slotted of [false, true]) test(`resume review cross-root background paint is checked for ${slotted ? 'slotted' : 'shadow'} text`, async ({ page }) => {
  1314 |   await page.setContent('<div id="host"></div>');
  1315 |   await page.locator('#host').evaluate((host, slotted) => {
  1316 |     const root = host.attachShadow({ mode: 'open' });
  1317 |     if (slotted) {
  1318 |       host.innerHTML = '<p slot="label" style="background:white;color:black;position:relative">Visible label</p>';
  1319 |       root.innerHTML = '<slot name="label"></slot><div id="cover" style="position:fixed;inset:0;background:black;z-index:10"></div>';
  1320 |     } else {
  1321 |       root.innerHTML = '<p style="background:white;color:black">Visible label</p>';
  1322 |       const cover = document.createElement('div'); cover.id = 'cover'; cover.style.cssText = 'position:fixed;inset:0;background:black;z-index:10'; document.body.append(cover);
  1323 |     }
  1324 |   }, slotted);
  1325 |   await expect(measureSurfaceContrast(page.locator('p'))).rejects.toThrow(/Overlapping sibling background/);
  1326 |   await page.locator('#cover').evaluate(node => node.remove());
  1327 |   await expectSurfaceContrast(page.locator('p'));
  1328 | });
  1329 | 
  1330 | test('resume review text stroke requires actual glyph paint evidence', async ({ page }) => {
  1331 |   await page.setContent('<p style="background:white;color:black;-webkit-text-stroke:10px white">Label</p>');
  1332 |   await expect(measureSurfaceContrast(page.locator('p'))).rejects.toThrow(/Text stroke/);
  1333 |   await page.locator('p').evaluate(node => node.style.setProperty('-webkit-text-stroke-width', '0px'));
  1334 |   await expectSurfaceContrast(page.locator('p'));
  1335 | });
  1336 | 
  1337 | test('resume review degenerate SVG fill supplies no painted icon', async ({ page }) => {
  1338 |   await page.setContent('<svg width="100" height="100" style="background:white"><path d="M10 10L80 80" fill="black"/></svg>');
  1339 |   await expect(expectSurfaceContrast(page.locator('svg'), 'icon')).rejects.toThrow(/Nonempty/);
  1340 |   await page.locator('path').evaluate(node => node.setAttribute('d', 'M10 10L80 80L10 80Z'));
  1341 |   await expectSurfaceContrast(page.locator('svg'), 'icon');
  1342 | });
  1343 | 
  1344 | test('resume review unpainted SVG dash patterns cannot certify an icon', async ({ page }) => {
  1345 |   await page.setContent('<svg width="100" height="40" style="background:white"><line x1="10" y1="20" x2="80" y2="20" stroke="black" stroke-width="3" stroke-dasharray="0 1000"/></svg>');
  1346 |   await expect(measureSurfaceContrast(page.locator('svg'), 'icon')).rejects.toThrow(/SVG dashed stroke/);
  1347 |   await page.locator('line').evaluate(node => node.setAttribute('stroke-dasharray', 'none'));
  1348 |   await expectSurfaceContrast(page.locator('svg'), 'icon');
  1349 | });
  1350 | 
  1351 | test('resume review transformed static containing block owns pseudo border adjacency', async ({ page }) => {
  1352 |   await page.setContent('<style>body{background:white}button{margin:40px;width:100px;height:40px;position:static;transform:translate(0px);background:black;outline:none;border:0}button:focus::after{content:"";position:absolute;inset:0;border:2px solid black}</style><button>Action</button>');
  1353 |   await page.locator('button').focus();
  1354 |   await expect(expectSurfaceContrast(page.locator('button'), 'focus')).rejects.toThrow(/contrast failures/);
  1355 |   await page.addStyleTag({ content: 'button:focus::after{border-color:white}' });
  1356 |   await expectSurfaceContrast(page.locator('button'), 'focus');
  1357 | });
  1358 | 
```