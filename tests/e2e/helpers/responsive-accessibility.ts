import { expect, type Locator, type Page } from '@playwright/test';

export type TargetException = {
  selector: string;
  kind: 'Inline' | 'Spacing' | 'Equivalent' | 'User Agent Control' | 'Essential';
  rationale: string;
  keyboardPath: string;
  evidence: string;
  equivalentSelector?: string;
};

/** Apply all four overrides through rerenders, including shadow roots attached later. */
export async function applyTextSpacing(page: Page): Promise<void> {
  await page.evaluate(() => {
    const owner = window as unknown as { aeSpacingCleanup?: () => void };
    owner.aeSpacingCleanup?.();
    const css = '* { line-height: 1.5 !important; letter-spacing: .12em !important; word-spacing: .16em !important; } p { margin-block-end: 2em !important; }';
    const roots = new Set<Document | ShadowRoot>();
    const visit = (root: Document | ShadowRoot) => {
      if (!roots.has(root)) { roots.add(root); observer.observe(root instanceof Document ? root.documentElement : root, { childList: true, subtree: true }); }
      if (!root.querySelector('[data-ae-text-spacing]')) {
        const style = document.createElement('style'); style.dataset.aeTextSpacing = 'true'; style.textContent = css;
        (root instanceof Document ? root.head : root).append(style);
      }
      root.querySelectorAll('*').forEach(element => { if (element.shadowRoot) visit(element.shadowRoot); });
    };
    const observer = new MutationObserver(() => { for (const root of roots) visit(root); });
    const original = Element.prototype.attachShadow;
    const patched = function (this: Element, init: ShadowRootInit) { const root = original.call(this, init); visit(root); return root; };
    Element.prototype.attachShadow = patched;
    owner.aeSpacingCleanup = () => {
      observer.disconnect();
      if (Element.prototype.attachShadow === patched) Element.prototype.attachShadow = original;
      for (const root of roots) root.querySelectorAll('[data-ae-text-spacing]').forEach(style => style.remove());
      delete owner.aeSpacingCleanup;
    };
    visit(document);
  });
}

/** Remove the overrides and their observer/prototype seam before a restoration round trip. */
export async function removeTextSpacing(page: Page): Promise<void> {
  await page.evaluate(() => (window as unknown as { aeSpacingCleanup?: () => void }).aeSpacingCleanup?.());
}

export async function expectNoPageOverflow(page: Page): Promise<void> {
  const geometry = await page.evaluate(() => {
    const root = document.scrollingElement ?? document.documentElement;
    return { width: root.clientWidth, scrollWidth: root.scrollWidth };
  });
  expect(geometry.scrollWidth, `Page overflow: ${JSON.stringify(geometry)}`).toBeLessThanOrEqual(geometry.width + 1);
}

/** Inventory actual hit targets, collapsing a Fluent control and its shadow control to one target. */
export async function inventoryTargets(page: Page, exceptions: TargetException[] = []) {
  return page.evaluate((records) => {
    const selector = 'a[href],button,input:not([type="hidden"]),select,textarea,summary,[role="button"],[role="tab"],[role="option"],[role="menuitem"],[role="checkbox"],[role="switch"],fluent-button,fluent-text-input,fluent-number-input,fluent-select,fluent-combobox,fluent-dropdown,fluent-switch,[tabindex]:not([tabindex="-1"]),[onclick]';
    const all: Element[] = [];
    const visit = (root: Document | ShadowRoot) => {
      root.querySelectorAll('*').forEach(element => { all.push(element); if (element.shadowRoot) visit(element.shadowRoot); });
    };
    visit(document);
    const visible = (element: Element) => {
      const rect = element.getBoundingClientRect();
      const style = getComputedStyle(element);
      return rect.width > 0 && rect.height > 0 && style.visibility !== 'hidden' && style.display !== 'none'
        && !element.closest('[inert]') && !element.closest('[aria-hidden="true"]');
    };
    const modalDialog = all.find(element => element.matches('dialog[open]'));
    const modalRoot = modalDialog?.getRootNode();
    const modal = modalRoot instanceof ShadowRoot ? modalRoot.host : modalDialog;
    const candidates = all.filter(element => element.matches(selector) && visible(element)
      && (!modal || element === modal || modal.contains(element) || element.getRootNode() instanceof ShadowRoot && modal.contains((element.getRootNode() as ShadowRoot).host)));
    const targets = candidates.filter(element => {
      const root = element.getRootNode();
      return !(root instanceof ShadowRoot && candidates.includes(root.host))
        && !candidates.some(ancestor => ancestor !== element && ancestor.tagName.startsWith('FLUENT-') && ancestor.contains(element));
    });
    const boxes = targets.map(element => ({ element, rect: element.getBoundingClientRect() }));
    const intersects = (a: DOMRect, b: DOMRect) => a.left < b.right && b.left < a.right && a.top < b.bottom && b.top < a.bottom;
    const circleIntersectsRect = (x: number, y: number, rect: DOMRect) => Math.hypot(x - Math.max(rect.left, Math.min(x, rect.right)), y - Math.max(rect.top, Math.min(y, rect.bottom))) < 12 - .01;
    const used = new Set<string>();
    const inventory = boxes.map(({ element, rect }, index) => {
      const identifier = element.id ? `#${element.id}` : element.getAttribute('data-testid') ? `[data-testid="${element.getAttribute('data-testid')}"]` : `${element.tagName.toLowerCase()}[inventory-index="${index}"]`;
      const record = records.find(item => element.matches(item.selector));
      let failure: string | null = null;
      const conforming = rect.width >= 24 && rect.height >= 24;
      if (record) used.add(record.selector);
      if (!conforming) {
        if (!record || !record.rationale.trim() || !record.keyboardPath.trim() || !record.evidence.trim()) failure = 'Undersized target lacks an evidenced exception';
        else if (record.kind === 'Spacing') {
          const x = rect.left + rect.width / 2, y = rect.top + rect.height / 2;
          if (boxes.some(other => other.element !== element && (circleIntersectsRect(x, y, other.rect)
            || (other.rect.width < 24 || other.rect.height < 24) && Math.hypot(x - other.rect.left - other.rect.width / 2, y - other.rect.top - other.rect.height / 2) < 24 - .01))) failure = '24px spacing circle intersects a target or neighboring undersized circle';
        } else if (record.kind === 'Equivalent') {
          const equivalent = record.equivalentSelector ? document.querySelector(record.equivalentSelector) : null;
          const box = equivalent?.getBoundingClientRect();
          if (!equivalent || equivalent === element || !visible(equivalent) || !box || box.width < 24 || box.height < 24 || intersects(rect, box)) failure = 'Equivalent control is absent, identical, overlapping, or undersized';
        } else if (record.kind === 'Inline') {
          if (!element.closest('p,li,dd') || !['inline', 'inline-block'].includes(getComputedStyle(element).display) || element.matches('button,[role="button"],fluent-button')) failure = 'Inline exception does not apply to a control outside running text';
        } else if (record.kind === 'User Agent Control' && !element.matches('input,select,textarea')) failure = 'User agent exception requires a native control';
      }
      return { target: identifier, width: rect.width, height: rect.height, conforming, exception: record ?? null, failure };
    });
    return { inventory, unusedExceptions: records.filter(record => !used.has(record.selector)).map(record => record.selector) };
  }, exceptions);
}

export async function expectTargetSizes(page: Page, exceptions: TargetException[] = []) {
  const result = await inventoryTargets(page, exceptions);
  expect(result.inventory.length, 'Pointer-target inventory must be nonempty').toBeGreaterThan(0);
  expect(result.unusedExceptions, 'Stale or unmatched exception selectors').toEqual([]);
  expect(result.inventory.filter(target => target.failure), 'AE-04 target-size failures').toEqual([]);
  return result.inventory;
}

/** Check the whole rectangle and outline against viewport, clipping ancestors, and occluding boxes. */
export async function focusGeometry(target: Locator) {
  return target.evaluate(element => {
    const rect = element.getBoundingClientRect();
    const visuals: Element[] = [element];
    // Fluent paints focus on internal wrappers/pseudo-elements as well as its active input.
    const visitFocus = (root: ShadowRoot) => {
      for (const child of root.querySelectorAll('*')) {
        if (child.matches(':focus-within')) visuals.push(child);
        if (child.shadowRoot?.activeElement) visitFocus(child.shadowRoot);
      }
    };
    for (let owner: Element | null = element; owner; owner = owner.parentElement) {
      if ((owner === element || owner.matches('fluent-field,fluent-dropdown,fluent-combobox')) && owner.shadowRoot && owner.matches(':focus-within')) {
        if (!visuals.includes(owner)) visuals.push(owner);
        visitFocus(owner.shadowRoot);
      }
    }
    const painted = (value: string) => value !== 'transparent' && !/rgba\([^)]*,\s*0\s*\)$/u.test(value);
    let indicator = false, ring = 0;
    const indicatorOwners = new Set<Element>();
    const box = { left: rect.left, top: rect.top, right: rect.right, bottom: rect.bottom };
    for (const visual of visuals) for (const pseudo of [null, '::before', '::after']) {
      const css = getComputedStyle(visual, pseudo);
      if (css.visibility === 'hidden' || css.display === 'none' || Number(css.opacity) === 0 || pseudo && css.content === 'none') continue;
      if (css.transform !== 'none') { const matrix = new DOMMatrixReadOnly(css.transform); if (matrix.a === 0 || matrix.d === 0) continue; }
      const outline = parseFloat(css.outlineWidth) > 0 && css.outlineStyle !== 'none' && painted(css.outlineColor);
      const shadow = css.boxShadow !== 'none';
      const pseudoBorder = pseudo && ['Top', 'Right', 'Bottom', 'Left'].some(side => parseFloat(css.getPropertyValue(`border-${side.toLowerCase()}-width`)) > 0 && !['none', 'hidden'].includes(css.getPropertyValue(`border-${side.toLowerCase()}-style`)) && painted(css.getPropertyValue(`border-${side.toLowerCase()}-color`)));
      indicator ||= outline || shadow || Boolean(pseudoBorder);
      if (outline) ring = Math.max(ring, Math.max(0, parseFloat(css.outlineWidth) + parseFloat(css.outlineOffset)) || 0);
      if (outline || shadow || pseudoBorder) {
        indicatorOwners.add(visual);
        const visualBox = visual.getBoundingClientRect(), extent = outline ? Math.max(0, parseFloat(css.outlineWidth) + parseFloat(css.outlineOffset)) : 0;
        box.left = Math.min(box.left, visualBox.left - extent); box.top = Math.min(box.top, visualBox.top - extent);
        box.right = Math.max(box.right, visualBox.right + extent); box.bottom = Math.max(box.bottom, visualBox.bottom + extent);
      }
    }
    const failures: string[] = [];
    if (!indicator) failures.push('focused target has no visible focus indicator');
    let occluder: string | null = null;
    if (box.left < -.5 || box.top < -.5 || box.right > innerWidth + .5 || box.bottom > innerHeight + .5) failures.push('viewport clips target or focus ring');
    const parent = (node: Element): Element | null => node.assignedSlot ?? node.parentElement ?? (node.getRootNode() instanceof ShadowRoot ? (node.getRootNode() as ShadowRoot).host : null);
    for (let ancestor = parent(element); ancestor; ancestor = parent(ancestor)) {
      const css = getComputedStyle(ancestor), clip = ancestor.getBoundingClientRect();
      const left = clip.left + ancestor.clientLeft, top = clip.top + ancestor.clientTop;
      // A wrapper clips its descendants, but its own outline is painted outside its overflow box.
      const clipped = indicatorOwners.has(ancestor) ? rect : box;
      if (/(auto|scroll|hidden|clip)/u.test(css.overflowX) && (clipped.left < left - .5 || clipped.right > left + ancestor.clientWidth + .5)) failures.push('horizontal scrollport clips target or ring');
      if (/(auto|scroll|hidden|clip)/u.test(css.overflowY) && (clipped.top < top - .5 || clipped.bottom > top + ancestor.clientHeight + .5)) failures.push('vertical scrollport clips target or ring');
    }
    const contains = (ancestor: Element, child: Element) => {
      for (let current: Element | null = child; current; current = parent(current)) if (current === ancestor) return true;
      return false;
    };
    const related = (candidate: Element) => contains(candidate, element) || contains(element, candidate);
    // Test every overlapping app box at its intersection center. This detects even a small
    // off-center obstruction missed by center-only or in-viewport assertions.
    for (const candidate of document.querySelectorAll('*')) {
      if (related(candidate)) continue;
      const css = getComputedStyle(candidate), other = candidate.getBoundingClientRect();
      if (css.visibility === 'hidden' || css.display === 'none' || Number(css.opacity) === 0 || other.width === 0 || other.height === 0) continue;
      const left = Math.max(box.left, other.left), right = Math.min(box.right, other.right);
      const top = Math.max(box.top, other.top), bottom = Math.min(box.bottom, other.bottom);
      if (left >= right || top >= bottom) continue;
      const x = (left + right) / 2, y = (top + bottom) / 2;
      let hit: Element | null;
      const probe = candidate as HTMLElement;
      const originalStyle = probe.getAttribute('style');
      const pointerOptOut = css.pointerEvents === 'none';
      try {
        if (pointerOptOut) probe.style.setProperty('pointer-events', 'auto', 'important');
        hit = document.elementFromPoint(x, y);
      } finally {
        if (pointerOptOut) { if (originalStyle === null) probe.removeAttribute('style'); else probe.setAttribute('style', originalStyle); }
      }
      const insideTarget = x > rect.left + .5 && x < rect.right - .5 && y > rect.top + .5 && y < rect.bottom - .5;
      const paintsAtPoint = (hit: Element) => {
        const css = getComputedStyle(hit);
        const alpha = css.backgroundColor.match(/[\d.]+/gu)?.map(Number) ?? [];
        if (alpha.length >= 3 && (alpha[3] ?? 1) > 0 || css.backgroundImage !== 'none' || hit.matches('svg,path,img,video,canvas')) return true;
        return [...hit.childNodes].filter(node => node.nodeType === Node.TEXT_NODE && node.textContent?.trim()).some(node => {
          const range = document.createRange(); range.selectNodeContents(node);
          return [...range.getClientRects()].some(text => x >= text.left && x <= text.right && y >= text.top && y <= text.bottom);
        });
      };
      const stackingLevel = (node: Element) => {
        let level = 0;
        for (let current: Element | null = node; current; current = parent(current)) {
          const z = Number.parseInt(getComputedStyle(current).zIndex);
          if (Number.isFinite(z)) level = Math.max(level, z);
        }
        return level;
      };
      // Outlines are painted outside hit boxes. Content behind the outline is not an
      // obstruction simply because elementFromPoint cannot hit that outline itself.
      if (hit && !related(hit) && (!pointerOptOut || paintsAtPoint(hit)) && (insideTarget || paintsAtPoint(hit) && stackingLevel(hit) > stackingLevel(element))) { occluder = `${element.tagName}#${element.id} by ${hit.tagName}#${hit.id} at ${x},${y}`; failures.push('app content obscures target or focus ring'); break; }
    }
    return { target: `${element.tagName}#${element.id}`, box, ring, failures, occluder };
  });
}

export async function expectFocusVisible(target: Locator): Promise<void> {
  await expect(target).toBeFocused();
  await expect.poll(async () => { const result = await focusGeometry(target); return result.failures.map(failure => `${failure}: ${JSON.stringify(result)}`); }, { message: 'AE-05 focused visual geometry' }).toEqual([]);
}

export async function expectTextContrast(target: Locator, minimum?: number, colorProperty = 'color') {
  const sample = await target.evaluate((element, property) => {
    const parse = (value: string) => {
      const channels = value.match(/[\d.]+/gu)?.map(Number) ?? [];
      if (channels.length < 3) throw new Error('Unsupported computed color');
      return [channels[0], channels[1], channels[2], channels[3] ?? 1];
    };
    const composite = (fg: number[], bg: number[]) => [0, 1, 2].map(i => fg[i] * fg[3] + bg[i] * (1 - fg[3])).concat(1);
    const backgrounds: number[][] = [];
    for (let current: Element | null = element; current; current = current.parentElement) backgrounds.push(parse(getComputedStyle(current).backgroundColor));
    let background = [255, 255, 255, 1];
    for (const layer of backgrounds.reverse()) background = composite(layer, background);
    const css = getComputedStyle(element), foreground = composite(parse(css.getPropertyValue(property)), background);
    const luminance = (color: number[]) => color.slice(0, 3).map(channel => { const c = channel / 255; return c <= .04045 ? c / 12.92 : ((c + .055) / 1.055) ** 2.4; }).reduce((sum, c, i) => sum + c * [.2126, .7152, .0722][i], 0);
    const a = luminance(foreground), b = luminance(background);
    const large = Number.parseFloat(css.fontSize) >= 24 || Number.parseFloat(css.fontSize) >= 18.667 && Number.parseInt(css.fontWeight) >= 700;
    return { foreground, background, ratio: (Math.max(a, b) + .05) / (Math.min(a, b) + .05), minimum: large ? 3 : 4.5 };
  }, colorProperty);
  expect(sample.ratio, JSON.stringify(sample)).toBeGreaterThanOrEqual(minimum ?? sample.minimum);
  return sample;
}

/** Find actual clipped text ranges, including controls inside open shadow roots. */
export async function expectNoTextClipping(page: Page): Promise<void> {
  const failures = await page.evaluate(() => {
    const failures: string[] = [];
    const visit = (root: Document | ShadowRoot) => {
      for (const element of root.querySelectorAll('*')) {
        if (element.shadowRoot) visit(element.shadowRoot);
        if (element.closest('.fc-sr-only,[aria-hidden="true"],[hidden]') || element.getClientRects().length === 0) continue;
        const css = getComputedStyle(element);
        if (css.visibility === 'hidden' || css.clip !== 'auto' || css.clipPath !== 'none') continue;
        if (!/(hidden|clip)/u.test(css.overflowX + css.overflowY)) continue;
        const box = element.getBoundingClientRect();
        if (box.width < 2 || box.height < 2) continue;
        const walker = document.createTreeWalker(element, NodeFilter.SHOW_TEXT);
        while (walker.nextNode()) {
          const node = walker.currentNode;
          if (!node.textContent?.trim() || node.parentElement?.closest('style,script,[hidden],.fc-sr-only,.fc-skip-link:not(:focus),[aria-hidden="true"]') || node.parentElement && getComputedStyle(node.parentElement).visibility === 'hidden') continue;
          const range = document.createRange(); range.selectNodeContents(node);
          if ([...range.getClientRects()].some(text => {
            let left = text.left, right = text.right, top = text.top, bottom = text.bottom;
            // A nested scrollport owns its offscreen text; evaluate only the visible portion at
            // this ancestor. Each hidden/clip owner is independently checked by the outer loop.
            for (let parent = node.parentElement; parent && parent !== element; parent = parent.parentElement) {
              const style = getComputedStyle(parent), clip = parent.getBoundingClientRect();
              if (/(auto|scroll|hidden|clip)/u.test(style.overflowX)) { left = Math.max(left, clip.left); right = Math.min(right, clip.right); }
              if (/(auto|scroll|hidden|clip)/u.test(style.overflowY)) { top = Math.max(top, clip.top); bottom = Math.min(bottom, clip.bottom); }
            }
            return right > left && bottom > top && (
              /(hidden|clip)/u.test(css.overflowX) && (left < box.left - 1 || right > box.right + 1)
              || /(hidden|clip)/u.test(css.overflowY) && (top < box.top - 1 || bottom > box.bottom + 1));
          })) {
            failures.push(element.id ? `#${element.id}` : element.tagName.toLowerCase()); break;
          }
        }
      }
    };
    visit(document);
    return [...new Set(failures)];
  });
  expect(failures, 'AE-03 clipped text ranges').toEqual([]);
}
