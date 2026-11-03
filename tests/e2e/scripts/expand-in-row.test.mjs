import assert from 'node:assert/strict';
import { readFile } from 'node:fs/promises';
import test from 'node:test';
import vm from 'node:vm';

const source = await readFile(new URL('../../../src/Hexalith.FrontComposer.Shell/wwwroot/js/fc-expandinrow.js', import.meta.url), 'utf8');

function createEnvironment({ focus = 'trigger', reduceMotion = false } = {}) {
  const frames = [];
  const scrolls = [];
  const initialScrolls = [];
  class Element {
    isConnected = true;
    scrollIntoView(options) { initialScrolls.push(options); }
    getBoundingClientRect() { return { top: 0, bottom: 900 }; }
  }
  class HTMLElement extends Element {
    constructor(heading = false) { super(); this.heading = heading; }
    matches(selector) {
      assert.equal(selector, '#fc-main-content h1, main h1, [role="main"] h1');
      return this.heading;
    }
  }
  const body = new HTMLElement();
  const activeElement = focus === 'body' ? body
    : focus === 'null' ? null
      : focus === 'missing' ? undefined
        : new HTMLElement(focus === 'heading');
  const document = { activeElement, body, documentElement: { clientHeight: 800 } };
  const context = vm.createContext({
    Element, HTMLElement, document,
    window: { innerHeight: 800, matchMedia: () => ({ matches: reduceMotion }), scrollBy: (value) => scrolls.push(value) },
    requestAnimationFrame: (callback) => frames.push(callback),
  });
  vm.runInContext(source.replaceAll('export function', 'function'), context);
  return { context, document, HTMLElement, frames, scrolls, initialScrolls };
}

for (const headingFocusedAfterInitialization of [false, true]) {
  test(`queued expand-in-row correction respects later route focus: ${headingFocusedAfterInitialization}`, () => {
    const { context, document, HTMLElement, frames, scrolls, initialScrolls } = createEnvironment();
    context.initializeExpandInRow(new HTMLElement());
    assert.equal(initialScrolls.length, 1);
    assert.equal(initialScrolls[0].block, 'nearest');
    assert.equal(initialScrolls[0].behavior, 'smooth');
    assert.equal(frames.length, 1);
    if (headingFocusedAfterInitialization) document.activeElement = new HTMLElement(true);
    frames[0]();
    assert.equal(scrolls.length, headingFocusedAfterInitialization ? 0 : 1);
    if (!headingFocusedAfterInitialization) assert.equal(scrolls[0].top, 100);
  });
}

for (const deferToRouteFocus of [false, true]) {
  for (const focus of ['body', 'null', 'missing', 'heading']) {
    test(`initial expand-in-row focus ${focus}, defer to route: ${deferToRouteFocus}`, () => {
      const { context, HTMLElement, frames, scrolls, initialScrolls } = createEnvironment({ focus });
      if (deferToRouteFocus) context.initializeExpandInRow(new HTMLElement(), true);
      else context.initializeExpandInRow(new HTMLElement());
      const shouldScroll = !deferToRouteFocus && focus !== 'heading';
      assert.equal(initialScrolls.length, shouldScroll ? 1 : 0);
      assert.equal(frames.length, shouldScroll ? 1 : 0);
      if (shouldScroll) {
        assert.equal(initialScrolls[0].block, 'nearest');
        assert.equal(initialScrolls[0].behavior, 'smooth');
        frames[0]();
        assert.equal(scrolls[0].top, 100);
      }
      assert.equal(scrolls.length, shouldScroll ? 1 : 0);
    });

    test(`queued correction after focus becomes ${focus}, defer to route: ${deferToRouteFocus}`, () => {
      const { context, document, HTMLElement, frames, scrolls, initialScrolls } = createEnvironment();
      if (deferToRouteFocus) context.initializeExpandInRow(new HTMLElement(), true);
      else context.initializeExpandInRow(new HTMLElement());
      assert.equal(initialScrolls.length, 1);
      assert.equal(frames.length, 1);
      document.activeElement = focus === 'body' ? document.body
        : focus === 'null' ? null
          : focus === 'missing' ? undefined
            : new HTMLElement(true);
      frames[0]();
      const shouldCorrect = !deferToRouteFocus && focus !== 'heading';
      assert.equal(scrolls.length, shouldCorrect ? 1 : 0);
      if (shouldCorrect) assert.equal(scrolls[0].top, 100);
    });
  }

  test(`expand-in-row preserves reduced motion, defer to route: ${deferToRouteFocus}`, () => {
    const { context, HTMLElement, frames, scrolls, initialScrolls } = createEnvironment({
      focus: deferToRouteFocus ? 'trigger' : 'body', reduceMotion: true,
    });
    if (deferToRouteFocus) context.initializeExpandInRow(new HTMLElement(), true);
    else context.initializeExpandInRow(new HTMLElement());
    assert.equal(initialScrolls.length, 1);
    assert.equal(initialScrolls[0].block, 'nearest');
    assert.equal(initialScrolls[0].behavior, 'auto');
    assert.equal(frames.length, 0);
    assert.equal(scrolls.length, 0);
  });
}
