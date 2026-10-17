import { expect, test } from '../../../../../../../tests/e2e/fixtures/index.ts';
import { expectSurfaceContrast, measureSurfaceContrast } from './baseline-helper.ts';
test('contrast clips a shadow background at its scrollport while rejecting 1.7px of visible overlap', async ({ page }, info) => {
  await page.setContent('<style>body{background:white}#port{position:absolute;left:40px;top:40px;width:222px;height:60px;overflow:hidden}#host{position:relative;left:0}#label{position:absolute;left:40px;top:100px;margin:0;color:black;font:14px/20px sans-serif}</style><div id="port"><div id="host"></div></div><p id="label">Footer label</p>');
  const geometry = await page.locator('#host').evaluate(host => {
    const background = document.createElement('div');
    background.style.cssText = 'position:relative;width:222px;height:32px;background:black';
    host.attachShadow({ mode: 'open' }).append(background);
    const label = document.querySelector('#label')!;
    const range = document.createRange(); range.selectNodeContents(label);
    const text = range.getBoundingClientRect(), port = host.parentElement!.getBoundingClientRect();
    (host as HTMLElement).style.top = `${text.bottom - 1.7 - port.top}px`;
    const candidate = background.getBoundingClientRect();
    return { text: text.toJSON(), port: port.toJSON(), candidate: candidate.toJSON(), rawOverlap: text.bottom - candidate.top };
  });
  expect(geometry.rawOverlap).toBeCloseTo(1.7, 1);
  expect(geometry.candidate.top).toBeGreaterThan(geometry.port.bottom);
  await info.attach('shadow-background-scrollport-geometry', { body: JSON.stringify(geometry), contentType: 'application/json' });
  for (const overflow of ['hidden', 'auto', 'clip']) {
    await page.locator('#port').evaluate((port, value) => { (port as HTMLElement).style.overflow = value; }, overflow);
    const samples = await expectSurfaceContrast(page.locator('#label'));
    expect(samples).toHaveLength(1);
    expect(samples[0].background.slice(0, 3)).toEqual([255, 255, 255]);
    await page.locator('#port').evaluate(port => { (port as HTMLElement).style.overflow = 'visible'; });
    await expect(measureSurfaceContrast(page.locator('#label'))).rejects.toThrow(/Overlapping sibling background/);
  }
  await page.locator('#port').evaluate(port => { (port as HTMLElement).style.overflow = 'hidden'; });
  await page.locator('#host').evaluate((host, top) => { (host as HTMLElement).style.cssText = `position:fixed;left:40px;top:${top}px`; }, geometry.candidate.top);
  await expect(measureSurfaceContrast(page.locator('#label'))).rejects.toThrow(/Overlapping sibling background/);
});

