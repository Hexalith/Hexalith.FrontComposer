import assert from 'node:assert/strict';
import { readFile } from 'node:fs/promises';
import test from 'node:test';
import vm from 'node:vm';

const source = await readFile(new URL('../../../src/Hexalith.FrontComposer.Shell/wwwroot/js/fc-expandinrow.js', import.meta.url), 'utf8');

for (const headingFocusedAfterInitialization of [false, true]) {
  test(`queued expand-in-row correction respects later route focus: ${headingFocusedAfterInitialization}`, () => {
    const frames = [];
    const scrolls = [];
    class Element {
      isConnected = true;
      scrollIntoView() {}
      getBoundingClientRect() { return { top: 0, bottom: 900 }; }
    }
    class HTMLElement extends Element {
      constructor(heading = false) { super(); this.heading = heading; }
      matches(selector) {
        assert.equal(selector, '#fc-main-content h1, main h1, [role="main"] h1');
        return this.heading;
      }
    }
    const document = { activeElement: new HTMLElement(), documentElement: { clientHeight: 800 } };
    const context = vm.createContext({
      Element, HTMLElement, document,
      window: { innerHeight: 800, matchMedia: () => ({ matches: false }), scrollBy: (value) => scrolls.push(value) },
      requestAnimationFrame: (callback) => frames.push(callback),
    });
    vm.runInContext(source.replaceAll('export function', 'function'), context);
    context.initializeExpandInRow(new HTMLElement());
    assert.equal(frames.length, 1);
    if (headingFocusedAfterInitialization) document.activeElement = new HTMLElement(true);
    frames[0]();
    assert.equal(scrolls.length, headingFocusedAfterInitialization ? 0 : 1);
    if (!headingFocusedAfterInitialization) assert.equal(scrolls[0].top, 100);
  });
}
