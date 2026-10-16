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
    const selector = 'a[href],button,input:not([type="hidden"]),select,textarea,summary,[role="button"],[role="tab"],[role="option"],[role="menuitem"],[role="checkbox"],[role="radio"],[role="switch"],fluent-button,fluent-text-input,fluent-number-input,fluent-select,fluent-combobox,fluent-dropdown,fluent-radio,fluent-switch,[tabindex]:not([tabindex="-1"]),[onclick]';
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

type PaintBox = { left: number; top: number; right: number; bottom: number };
type PaintClip = { kind: 'none' | 'rect' | 'hidden' | 'unsupported'; box?: PaintBox };
type RestingPaint = Map<Element, Record<string, Record<string, string>>>;
type PaintAudit = { clip: (css: CSSStyleDeclaration, box: PaintBox) => PaintClip; parse: (value: string) => number[]; resting: (scope: Element) => RestingPaint; changed: (element: Element, pseudo: string | null, style: CSSStyleDeclaration, resting: RestingPaint, properties: string[]) => boolean };

/** Observe resting pseudo-class paint synchronously, suppressing application focus callbacks. */
async function preparePaintAudit(root: Locator) {
  await root.evaluate(() => {
    const parse = (value: string): number[] => {
      if (!CSS.supports('color', value)) throw new Error(`Unsupported computed paint: ${value}`);
      const canvas = document.createElement('canvas'); canvas.width = canvas.height = 1;
      const context = canvas.getContext('2d')!; context.fillStyle = value; context.fillRect(0, 0, 1, 1);
      const pixel = context.getImageData(0, 0, 1, 1).data;
      return [pixel[0], pixel[1], pixel[2], pixel[3] / 255];
    };
    const clip = (css: CSSStyleDeclaration, box: PaintBox): PaintClip => {
      let result = { left: box.left, top: box.top, right: box.right, bottom: box.bottom }, clipped = false;
      const width = box.right - box.left, height = box.bottom - box.top;
      const length = (value: string, extent: number): number | null => {
        if (/^-?[\d.]+px$/u.test(value) || value === '0') return parseFloat(value);
        if (/^-?[\d.]+%$/u.test(value)) return parseFloat(value) * extent / 100;
        const calc = value.match(/^calc\(\s*(-?[\d.]+)%\s*([+-])\s*([\d.]+)px\s*\)$/u);
        return calc ? Number(calc[1]) * extent / 100 + (calc[2] === '+' ? 1 : -1) * Number(calc[3]) : null;
      };
      if (css.clip && css.clip !== 'auto' && ['absolute','fixed'].includes(css.position)) {
        const rect = css.clip.match(/^rect\((.*)\)$/u)?.[1].split(/[,\s]+/u).filter(Boolean);
        if (!rect || rect.length !== 4) return { kind: 'unsupported' };
        const values = rect.map((value, side) => value === 'auto' ? [0,width,height,0][side] : length(value, side % 2 ? width : height));
        if (values.some(value => value === null)) return { kind: 'unsupported' };
        result = { left: box.left + values[3]!, top: box.top + values[0]!, right: box.left + values[1]!, bottom: box.top + values[2]! }; clipped = true;
      }
      if (css.clipPath && css.clipPath !== 'none') {
        const tokens = css.clipPath.match(/^inset\((.*)\)$/u)?.[1].match(/calc\([^)]*\)|[^\s]+/gu);
        if (!tokens || tokens.length > 4) return { kind: 'unsupported' };
        const sides = [tokens[0], tokens[1] ?? tokens[0], tokens[2] ?? tokens[0], tokens[3] ?? tokens[1] ?? tokens[0]];
        const values = sides.map((value, side) => length(value, side % 2 ? width : height));
        if (values.some(value => value === null)) return { kind: 'unsupported' };
        result = { left: Math.max(result.left, box.left + values[3]!), top: Math.max(result.top, box.top + values[0]!), right: Math.min(result.right, box.right - values[1]!), bottom: Math.min(result.bottom, box.bottom - values[2]!) }; clipped = true;
      }
      if (clipped && (result.right <= result.left || result.bottom <= result.top)) return { kind: 'hidden' };
      if (['mask-image','-webkit-mask-image','mask-border-source','-webkit-mask-box-image-source'].some(property => { const value = css.getPropertyValue(property); return value && value !== 'none'; })) return { kind: 'unsupported' };
      return clipped ? { kind: 'rect', box: result } : { kind: 'none' };
    };
    const properties = ['outline-width','outline-style','outline-color','outline-offset','box-shadow','content','display','visibility','opacity','transform', ...['top','right','bottom','left'].flatMap(side => [`border-${side}-width`,`border-${side}-style`,`border-${side}-color`])];
    const resting = (scope: Element) => {
      const elements = new Set<Element>();
      const visit = (element: Element) => { elements.add(element); if (element.shadowRoot) for (const child of element.shadowRoot.children) visit(child); for (const child of element.children) visit(child); };
      visit(scope);
      for (let parent: Element | null = scope; parent; parent = parent.parentElement ?? (parent.getRootNode() instanceof ShadowRoot ? (parent.getRootNode() as ShadowRoot).host : null)) {
        elements.add(parent); if (parent.shadowRoot) for (const child of parent.shadowRoot.children) visit(child);
      }
      let active = document.activeElement as HTMLElement | null;
      while (active?.shadowRoot?.activeElement instanceof HTMLElement) active = active.shadowRoot.activeElement;
      const stop = (event: Event) => event.stopImmediatePropagation();
      const events = ['focus','blur','focusin','focusout'];
      for (const event of events) window.addEventListener(event, stop, true);
      const snapshots: RestingPaint = new Map();
      // Compare settled pseudo-class endpoints; a running blur transition retains the old
      // focused transform at time zero. Remove these synchronous probe rules after restoration.
      const probeStyles = [...new Set([...elements].map(element => element.getRootNode()))].flatMap(root => {
        if (!(root instanceof Document || root instanceof ShadowRoot)) return [];
        const style = document.createElement('style'); style.textContent = '*,*::before,*::after{transition:none!important}';
        (root instanceof Document ? root.head : root).append(style); return [style];
      });
      for (const element of elements) getComputedStyle(element).transform;
      try {
        active?.blur();
        for (const element of elements) {
          const paints: Record<string, Record<string, string>> = {};
          for (const pseudo of ['', '::before', '::after']) {
            const css = getComputedStyle(element, pseudo || null);
            paints[pseudo] = Object.fromEntries(properties.map(property => [property, css.getPropertyValue(property)]));
          }
          snapshots.set(element, paints);
        }
      } finally {
        active?.focus({ preventScroll: true });
        for (const element of elements) { getComputedStyle(element).transform; getComputedStyle(element, '::after').transform; }
        for (const style of probeStyles) style.remove();
        for (const event of events) window.removeEventListener(event, stop, true);
      }
      return snapshots;
    };
    const changed: PaintAudit['changed'] = (element, pseudo, css, snapshots, keys) => {
      const baseline = snapshots.get(element)?.[pseudo ?? ''];
      return Boolean(baseline && keys.concat(pseudo ? ['content','display','visibility','opacity','transform'] : []).some(key => baseline[key] !== css.getPropertyValue(key)));
    };
    (window as typeof window & { __aePaintAudit: PaintAudit }).__aePaintAudit = { parse, resting, changed, clip };
  });
}

/** Check the whole rectangle and outline against viewport, clipping ancestors, and occluding boxes. */
export async function focusGeometry(target: Locator, { requireIndicator = true } = {}) {
  await preparePaintAudit(target);
  return target.evaluate((element, requireIndicator) => {
    const audit = (window as typeof window & { __aePaintAudit: PaintAudit }).__aePaintAudit;
    const resting = audit.resting(element);
    const rect = element.getBoundingClientRect();
    const composedParent = (node: Element): Element | null => node.assignedSlot ?? node.parentElement ?? (node.getRootNode() instanceof ShadowRoot ? (node.getRootNode() as ShadowRoot).host : null);
    const visuals: Element[] = [element];
    // Fluent paints focus on internal wrappers/pseudo-elements as well as its active input.
    const visitFocus = (root: ShadowRoot) => {
      for (const child of root.querySelectorAll('*')) {
        if (child.matches(':focus-within')) visuals.push(child);
        if (child.shadowRoot?.activeElement) visitFocus(child.shadowRoot);
      }
    };
    for (let owner: Element | null = element; owner; owner = composedParent(owner)) {
      if ((owner === element || owner.matches('fluent-text-input,fluent-number-input,fluent-dropdown,fluent-combobox,fluent-select,fluent-button,fluent-anchor-button,fluent-switch,fluent-radio')) && owner.shadowRoot && owner.matches(':focus-within')) {
        if (!visuals.includes(owner)) visuals.push(owner);
        visitFocus(owner.shadowRoot);
      }
    }
    const painted = (value: string) => audit.parse(value)[3] > 0;
    const outerShadows = (value: string) => value.split(/,(?![^()]*\))/u).flatMap(shadow => {
      if (shadow === 'none' || /\binset\b/u.test(shadow)) return [];
      const color = shadow.match(/[a-z][a-z0-9-]*\([^()]*\)/iu)?.[0];
      if (!color) throw new Error('Unsupported shadow paint needs pixel evidence');
      if (!painted(color)) return [];
      const lengths = shadow.replace(/[a-z][a-z0-9-]*\([^()]*\)/giu, '').match(/-?[\d.]+px/gu)?.map(Number.parseFloat) ?? [];
      if (lengths.length < 2) return [];
      const [x, y, blur = 0, spread = 0] = lengths;
      if (x === 0 && y === 0 && blur === 0 && spread <= 0) return [];
      return [{ x, y, blur, extent: Math.max(0, spread + blur * 1.5) }];
    });
    const geometryLimits: string[] = [];
    const pseudoBox = (visual: Element, css: CSSStyleDeclaration) => {
      const px = (value: string) => /^-?[\d.]+px$/u.test(value) ? parseFloat(value) : null;
      if (!['absolute', 'fixed'].includes(css.position)) {
        geometryLimits.push('flow-positioned pseudo focus paint needs pixel evidence');
        return visual.getBoundingClientRect();
      }
      let block: Element | null = css.position === 'fixed' ? null : visual;
      if (css.position === 'fixed') {
        for (let ancestor = composedParent(visual); ancestor; ancestor = composedParent(ancestor)) {
          if (getComputedStyle(ancestor).transform !== 'none') { geometryLimits.push('fixed pseudo in transformed ancestry needs pixel evidence'); break; }
        }
      }
      while (block && getComputedStyle(block).position === 'static' && getComputedStyle(block).transform === 'none') block = composedParent(block);
      if (block && getComputedStyle(block).transform !== 'none') {
        const matrix = new DOMMatrixReadOnly(getComputedStyle(block).transform);
        if (!matrix.is2D || matrix.a !== 1 || matrix.d !== 1 || matrix.b !== 0 || matrix.c !== 0) geometryLimits.push('transformed pseudo containing block needs pixel evidence');
      }
      const rect = block?.getBoundingClientRect();
      const base = block && rect ? { left: rect.left + block.clientLeft, top: rect.top + block.clientTop, width: block.clientWidth, height: block.clientHeight } : { left: 0, top: 0, width: innerWidth, height: innerHeight };
      const left = px(css.left), right = px(css.right), top = px(css.top), bottom = px(css.bottom);
      let width = px(css.width), height = px(css.height);
      if (width !== null && css.boxSizing !== 'border-box') width += parseFloat(css.paddingLeft) + parseFloat(css.paddingRight) + parseFloat(css.borderLeftWidth) + parseFloat(css.borderRightWidth);
      if (height !== null && css.boxSizing !== 'border-box') height += parseFloat(css.paddingTop) + parseFloat(css.paddingBottom) + parseFloat(css.borderTopWidth) + parseFloat(css.borderBottomWidth);
      width ??= left !== null && right !== null ? base.width - left - right - parseFloat(css.marginLeft) - parseFloat(css.marginRight) : null;
      height ??= top !== null && bottom !== null ? base.height - top - bottom - parseFloat(css.marginTop) - parseFloat(css.marginBottom) : null;
      if (width === null || height === null || left === null && right === null || top === null && bottom === null) {
        geometryLimits.push('unresolved pseudo focus bounds need pixel evidence');
        return visual.getBoundingClientRect();
      }
      const x = base.left + (left !== null ? left + parseFloat(css.marginLeft) : base.width - right! - width - parseFloat(css.marginRight));
      const y = base.top + (top !== null ? top + parseFloat(css.marginTop) : base.height - bottom! - height - parseFloat(css.marginBottom));
      const matrix = new DOMMatrixReadOnly(css.transform === 'none' ? undefined : css.transform);
      const origin = css.transformOrigin.split(' ').map((value, axis) => value.endsWith('%') ? parseFloat(value) * (axis === 0 ? width : height) / 100 : parseFloat(value));
      if (!matrix.is2D || origin.length < 2 || origin.some(value => !Number.isFinite(value))) geometryLimits.push('unsupported pseudo transform needs pixel evidence');
      const corners = [[0, 0], [width, 0], [0, height], [width, height]].map(([a, b]) => ({ x: x + origin[0] + matrix.a * (a - origin[0]) + matrix.c * (b - origin[1]) + matrix.e, y: y + origin[1] + matrix.b * (a - origin[0]) + matrix.d * (b - origin[1]) + matrix.f }));
      return { left: Math.min(...corners.map(point => point.x)), top: Math.min(...corners.map(point => point.y)), right: Math.max(...corners.map(point => point.x)), bottom: Math.max(...corners.map(point => point.y)) };
    };
    let indicator = false, hardIndicator = false, blurredShadow = false, ring = 0;
    const indicatorOwners = new Set<Element>();
    const box = { left: rect.left, top: rect.top, right: rect.right, bottom: rect.bottom };
    for (const visual of visuals) for (const pseudo of [null, '::before', '::after']) {
      const css = getComputedStyle(visual, pseudo);
      if (css.visibility === 'hidden' || css.display === 'none' || Number(css.opacity) === 0 || pseudo && css.content === 'none') continue;
      if (css.transform !== 'none') { const matrix = new DOMMatrixReadOnly(css.transform); if (matrix.a === 0 || matrix.d === 0) continue; }
      const outline = audit.changed(visual, pseudo, css, resting, ['outline-width','outline-style','outline-color','outline-offset']) && parseFloat(css.outlineWidth) > 0 && css.outlineStyle !== 'none' && painted(css.outlineColor);
      const shadows = audit.changed(visual, pseudo, css, resting, ['box-shadow']) ? outerShadows(css.boxShadow) : [];
      const shadow = shadows.length > 0;
      let pseudoBorder = pseudo && audit.changed(visual, pseudo, css, resting, ['border-top-width','border-right-width','border-bottom-width','border-left-width','border-top-color','border-right-color','border-bottom-color','border-left-color','border-top-style','border-right-style','border-bottom-style','border-left-style']) && ['Top', 'Right', 'Bottom', 'Left'].some(side => parseFloat(css.getPropertyValue(`border-${side.toLowerCase()}-width`)) > 0 && !['none', 'hidden'].includes(css.getPropertyValue(`border-${side.toLowerCase()}-style`)) && painted(css.getPropertyValue(`border-${side.toLowerCase()}-color`)));
      const visualBox = pseudo ? pseudoBox(visual, css) : visual.getBoundingClientRect();
      const ownClip = audit.clip(css, visualBox);
      let paintBox = visualBox;
      // Fluent's focus underline intentionally clips its empty pseudo box to a painted border.
      // Measure that visible border, while external outlines/shadows and unknown masks fail closed.
      if (pseudoBorder && !outline && !shadow && ['rect','hidden'].includes(ownClip.kind)) {
        const matrix = new DOMMatrixReadOnly(css.transform === 'none' ? undefined : css.transform);
        if (!matrix.is2D || matrix.a !== 1 || matrix.d !== 1 || matrix.b !== 0 || matrix.c !== 0) geometryLimits.push('focus clip-path/clip/mask needs pixel evidence');
        const borders = ['top','right','bottom','left'].flatMap(side => {
          const width = parseFloat(css.getPropertyValue(`border-${side}-width`));
          if (!width || ['none','hidden'].includes(css.getPropertyValue(`border-${side}-style`)) || !painted(css.getPropertyValue(`border-${side}-color`)) || !ownClip.box) return [];
          const edge = { ...visualBox };
          if (side === 'top') edge.bottom = edge.top + width;
          if (side === 'bottom') edge.top = edge.bottom - width;
          if (side === 'left') edge.right = edge.left + width;
          if (side === 'right') edge.left = edge.right - width;
          const visible = { left: Math.max(edge.left, ownClip.box.left), right: Math.min(edge.right, ownClip.box.right), top: Math.max(edge.top, ownClip.box.top), bottom: Math.min(edge.bottom, ownClip.box.bottom) };
          return visible.right > visible.left && visible.bottom > visible.top ? [visible] : [];
        });
        pseudoBorder = borders.length > 0;
        if (borders.length) paintBox = { left: Math.min(...borders.map(box => box.left)), top: Math.min(...borders.map(box => box.top)), right: Math.max(...borders.map(box => box.right)), bottom: Math.max(...borders.map(box => box.bottom)) };
      } else if ((outline || shadow || pseudoBorder) && ownClip.kind !== 'none') geometryLimits.push('focus clip-path/clip/mask needs pixel evidence');
      indicator ||= outline || shadow || Boolean(pseudoBorder);
      hardIndicator ||= outline || Boolean(pseudoBorder) || shadows.some(paint => paint.blur === 0);
      if (outline) ring = Math.max(ring, Math.max(0, parseFloat(css.outlineWidth) + parseFloat(css.outlineOffset)) || 0);
      if (outline || shadow || pseudoBorder) {
        indicatorOwners.add(visual);
        const extent = outline ? Math.max(0, parseFloat(css.outlineWidth) + parseFloat(css.outlineOffset)) : 0;
        box.left = Math.min(box.left, paintBox.left - extent); box.top = Math.min(box.top, paintBox.top - extent);
        box.right = Math.max(box.right, paintBox.right + extent); box.bottom = Math.max(box.bottom, paintBox.bottom + extent);
        for (const paint of shadows) {
          if (paint.blur > 0) { blurredShadow = true; continue; }
          ring = Math.max(ring, Math.abs(paint.x) + paint.extent, Math.abs(paint.y) + paint.extent);
          box.left = Math.min(box.left, visualBox.left + paint.x - paint.extent);
          box.top = Math.min(box.top, visualBox.top + paint.y - paint.extent);
          box.right = Math.max(box.right, visualBox.right + paint.x + paint.extent);
          box.bottom = Math.max(box.bottom, visualBox.bottom + paint.y + paint.extent);
        }
      }
    }
    // Prove the complete sharp indicator independently of a coexisting blurred elevation.
    // A blur has no exact CSS paint boundary and cannot alone establish focus visibility.
    if (blurredShadow && !hardIndicator) geometryLimits.push('blurred focus shadow needs pixel evidence');
    const failures: string[] = [...new Set(geometryLimits)];
    if (requireIndicator && !indicator) failures.push('focused target has no visible focus indicator');
    let occluder: string | null = null;
    if (box.left < -.5 || box.top < -.5 || box.right > innerWidth + .5 || box.bottom > innerHeight + .5) failures.push('viewport clips target or focus ring');
    const parent = (node: Element): Element | null => node.assignedSlot ?? node.parentElement ?? (node.getRootNode() instanceof ShadowRoot ? (node.getRootNode() as ShadowRoot).host : null);
    for (const owner of new Set([element, ...indicatorOwners])) for (let clipped: Element | null = owner; clipped; clipped = parent(clipped)) {
      const own = audit.clip(getComputedStyle(clipped), clipped.getBoundingClientRect());
      if (own.kind !== 'none') failures.push('focus clip-path/clip/mask needs pixel evidence');
      if (clipped.matches(':popover-open,:modal')) break;
    }
    for (let ancestor = element.matches(':popover-open,:modal') ? null : parent(element); ancestor; ancestor = parent(ancestor)) {
      const css = getComputedStyle(ancestor), clip = ancestor.getBoundingClientRect();
      const left = clip.left + ancestor.clientLeft, top = clip.top + ancestor.clientTop;
      // A wrapper clips its descendants, but its own outline is painted outside its overflow box.
      const clipped = indicatorOwners.has(ancestor) ? rect : box;
      if (/(auto|scroll|hidden|clip)/u.test(css.overflowX) && (clipped.left < left - .5 || clipped.right > left + ancestor.clientWidth + .5)) failures.push('horizontal scrollport clips target or ring');
      if (/(auto|scroll|hidden|clip)/u.test(css.overflowY) && (clipped.top < top - .5 || clipped.bottom > top + ancestor.clientHeight + .5)) failures.push('vertical scrollport clips target or ring');
      if (ancestor.matches(':popover-open,:modal')) break;
    }
    const contains = (ancestor: Element, child: Element) => {
      for (let current: Element | null = child; current; current = parent(current)) if (current === ancestor) return true;
      return false;
    };
    const ownsDropdownIndicator = (candidate: Element) => {
      // Fluent's light-DOM combobox and fallback SVG occupy sibling shadow slots.
      // Their common host owns the passive indicator, but not arbitrary overlapping siblings.
      const host = element.matches('button[slot="control"][role="combobox"]') ? element.parentElement : null;
      if (!host?.matches('fluent-dropdown') || element.assignedSlot?.getRootNode() !== host.shadowRoot) return false;
      const icon = candidate.closest('svg[aria-hidden="true"]');
      if (!icon || icon.matches('[tabindex],a[href]') || icon.querySelector('[tabindex],a[href]')) return false;
      const bounds = icon.getBoundingClientRect(), control = host.getBoundingClientRect();
      if (bounds.left < control.left || bounds.top < control.top || bounds.right > control.right || bounds.bottom > control.bottom) return false;
      for (let owner: Element | null = icon; owner && owner !== host; owner = parent(owner)) {
        if (owner instanceof HTMLSlotElement && owner.name === 'indicator' && owner.getRootNode() === host.shadowRoot) return true;
      }
      return false;
    };
    const related = (candidate: Element) => contains(candidate, element) || contains(element, candidate) || ownsDropdownIndicator(candidate);
    // Test every overlapping app box at its intersection center. This detects even a small
    // off-center obstruction missed by center-only or in-viewport assertions.
    const candidates: Element[] = [];
    const visitOccluders = (root: Document | ShadowRoot) => {
      for (const candidate of root.querySelectorAll('*')) {
        candidates.push(candidate);
        if (candidate.shadowRoot) visitOccluders(candidate.shadowRoot);
      }
    };
    visitOccluders(document);
    const deepestHit = (x: number, y: number) => {
      let hit = document.elementFromPoint(x, y);
      while (hit?.shadowRoot) {
        const inner = hit.shadowRoot.elementFromPoint(x, y);
        if (!inner || inner === hit) break;
        hit = inner;
      }
      return hit;
    };
    for (const candidate of candidates) {
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
        hit = deepestHit(x, y);
      } finally {
        if (pointerOptOut) { if (originalStyle === null) probe.removeAttribute('style'); else probe.setAttribute('style', originalStyle); }
      }
      const insideTarget = x > rect.left + .5 && x < rect.right - .5 && y > rect.top + .5 && y < rect.bottom - .5;
      const paintsAtPoint = (hit: Element) => {
        const css = getComputedStyle(hit);
        const alpha = audit.parse(css.backgroundColor)[3];
        if (alpha > 0 || css.backgroundImage !== 'none' || hit instanceof SVGGraphicsElement || hit.matches('img,video,canvas')) return true;
        return [...hit.childNodes].filter(node => node.nodeType === Node.TEXT_NODE && node.textContent?.trim()).some(node => {
          const range = document.createRange(); range.selectNodeContents(node);
          return [...range.getClientRects()].some(text => x >= text.left && x <= text.right && y >= text.top && y <= text.bottom);
        });
      };
      const paintsAbove = (a: Element, b: Element) => {
        // Compare the first divergent stacking contexts, never maxima from unrelated levels.
        const context = (node: Element) => {
          const style = getComputedStyle(node);
          return node === document.documentElement || ['fixed','sticky'].includes(style.position) || style.zIndex !== 'auto' && (style.position !== 'static' || /flex|grid/u.test(getComputedStyle(parent(node) ?? document.documentElement).display)) || style.transform !== 'none' || Number(style.opacity) < 1 || style.isolation === 'isolate' || style.filter !== 'none' || style.perspective !== 'none' || /paint|layout|strict|content/u.test(style.contain) || style.mixBlendMode !== 'normal' || /transform|opacity|filter/u.test(style.willChange);
        };
        const chain = (node: Element) => { const nodes: Element[] = []; for (let current: Element | null = node; current; current = parent(current)) if (context(current)) nodes.unshift(current); return nodes; };
        const left = chain(a), right = chain(b); let index = 0;
        while (left[index] && left[index] === right[index]) index++;
        const first = left[index] ?? a, second = right[index] ?? b;
        const az = Number.parseInt(getComputedStyle(first).zIndex) || 0, bz = Number.parseInt(getComputedStyle(second).zIndex) || 0;
        if (az !== bz) return az > bz;
        if (first.getRootNode() !== second.getRootNode()) return null;
        const phase = (node: Element) => { const style = getComputedStyle(node); return context(node) || style.position !== 'static' ? 3 : /inline/u.test(style.display) ? 2 : style.cssFloat !== 'none' ? 1 : 0; };
        if (phase(first) !== phase(second)) return phase(first) > phase(second);
        return Boolean(second.compareDocumentPosition(first) & Node.DOCUMENT_POSITION_FOLLOWING);
      };
      // Outlines are painted outside hit boxes. Content behind the outline is not an
      // obstruction simply because elementFromPoint cannot hit that outline itself.
      if (hit && !related(hit) && (!pointerOptOut || paintsAtPoint(hit)) && (insideTarget || paintsAtPoint(hit) && paintsAbove(hit, element) !== false)) { occluder = `${element.tagName}#${element.id} by ${hit.tagName}#${hit.id} at ${x},${y}`; failures.push('app content obscures target or focus ring'); break; }
    }
    return { target: `${element.tagName}#${element.id}`, requireIndicator, box, ring, failures, occluder };
  }, requireIndicator);
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
          // Native popovers/dialogs paint in the top layer and escape outer overflow
          // clips. Their own clipping boxes are still evaluated independently.
          let escapesOwner = false;
          for (let parent = node.parentElement; parent && parent !== element; parent = parent.parentElement) {
            if (parent.matches(':popover-open,:modal')) { escapesOwner = true; break; }
          }
          if (escapesOwner) continue;
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
            failures.push(element.id ? `#${element.id}` : `${element.tagName.toLowerCase()}.${String(element.className).trim().replace(/\s+/gu, '.')}`); break;
          }
        }
      }
    };
    visit(document);
    return [...new Set(failures)];
  });
  expect(failures, 'AE-03 clipped text ranges').toEqual([]);
}

export type ContrastKind = 'text' | 'border' | 'icon' | 'focus' | 'cue';

/** Sample actual painted nodes in the composed tree; empty/unsupported samples fail closed. */
export async function measureSurfaceContrast(root: Locator, kind: ContrastKind = 'text') {
  await preparePaintAudit(root);
  return root.evaluate((scope, sampleKind) => {
    const parent = (node: Element): Element | null => node.assignedSlot ?? node.parentElement ?? (node.getRootNode() instanceof ShadowRoot ? (node.getRootNode() as ShadowRoot).host : null);
    const audit = (window as typeof window & { __aePaintAudit: PaintAudit }).__aePaintAudit;
    const parse = audit.parse;
    const resting = sampleKind === 'focus' ? audit.resting(scope) : new Map();
    const composite = (fg: number[], bg: number[]) => [0, 1, 2].map(i => fg[i] * fg[3] + bg[i] * (1 - fg[3])).concat(1);
    const background = (element: Element | null) => {
      const layers: number[][] = [];
      for (let current = element; current; current = parent(current)) {
        const css = getComputedStyle(current);
        if (css.backgroundImage !== 'none') throw new Error('Background image needs pixel evidence');
        if (Number(css.opacity) > 0 && Number(css.opacity) < 1) throw new Error(`Translucent group needs pixel evidence: ${current.tagName}.${current.className} opacity=${css.opacity}`);
        layers.push(parse(css.backgroundColor));
      }
      return layers.reverse().reduce((bg, fg) => composite(fg, bg), [255, 255, 255, 1]);
    };
    const luminance = (color: number[]) => color.slice(0, 3).map(channel => { const c = channel / 255; return c <= .04045 ? c / 12.92 : ((c + .055) / 1.055) ** 2.4; }).reduce((sum, c, i) => sum + c * [.2126, .7152, .0722][i], 0);
    const visible = (element: Element) => {
      if (element.getClientRects().length === 0 && getComputedStyle(element).display !== 'contents') return false;
      for (let current: Element | null = element; current; current = parent(current)) {
        const css = getComputedStyle(current);
        if (sampleKind === 'icon' && current.matches('fluent-skeleton,.fluent-skeleton,.fc-empty-state-body__icon')) return false;
        if (audit.clip(css, current.getBoundingClientRect()).kind === 'hidden') return false;
        if (css.visibility === 'hidden' || css.display === 'none' || Number(css.opacity) === 0 || current.matches('[hidden],.fc-sr-only,[disabled],[aria-disabled="true"]')) return false;
      }
      return true;
    };
    const samples: { owner: number; node: string; paint: string; foreground: number[]; background: number[]; ratio: number; minimum: number }[] = [];
    const owners = new Map<Element, number>();
    const controlOwner = (element: Element) => {
      let nearest: Element | null = null;
      for (let current: Element | null = element; current; current = parent(current)) {
        if (current.matches('fluent-button,fluent-text-input,fluent-number-input,fluent-dropdown,fluent-combobox,fluent-select,fluent-radio,fluent-switch')) return current;
        if (!nearest && current.matches('button,input,select,textarea,a[href],[role="button"],[role="tab"],[role="radio"],[role="option"]')) nearest = current;
        if (current === scope) break;
      }
      return nearest ?? element;
    };
    const sample = (element: Element, value: string, label: string, minimum: number, outside = false, alpha = 1, paintBoxes?: DOMRect[]) => {
      let boxes: PaintBox[] = paintBoxes ?? [element.getBoundingClientRect()];
      for (let current: Element | null = element; current; current = parent(current)) {
        const css = getComputedStyle(current);
        const clipping = audit.clip(css, current.getBoundingClientRect());
        if (clipping.kind === 'hidden') return;
        if (clipping.kind === 'unsupported') throw new Error('Clipped contrast paint needs pixel evidence');
        if (clipping.box) boxes = boxes.map(box => ({ left: Math.max(box.left, clipping.box!.left), right: Math.min(box.right, clipping.box!.right), top: Math.max(box.top, clipping.box!.top), bottom: Math.min(box.bottom, clipping.box!.bottom) }));
      }
      if (!boxes.some(box => box.right > box.left && box.bottom > box.top)) return;
      const color = parse(value); color[3] *= alpha;
      if (color[3] === 0 && sampleKind !== 'icon') return;
      const bg = background(outside ? parent(element) : element);
      const fg = composite(color, bg), a = luminance(fg), b = luminance(bg);
      const owner = controlOwner(element);
      if (!owners.has(owner)) owners.set(owner, owners.size);
      samples.push({ owner: owners.get(owner)!, node: `${element.tagName.toLowerCase()}${element.id ? '#' + element.id : ''}`, paint: label, foreground: fg, background: bg, ratio: (Math.max(a, b) + .05) / (Math.min(a, b) + .05), minimum });
    };
    const sampledText = new Set<Node>();
    const visited = new Set<Element>();
    const visit = (element: Element) => {
      if (visited.has(element) || !visible(element)) return;
      visited.add(element);
      const css = getComputedStyle(element);
      if (sampleKind === 'text') {
        if (element instanceof HTMLInputElement || element instanceof HTMLTextAreaElement) {
          const paint = element.value.trim() ? css.color : element.getAttribute('placeholder') ? getComputedStyle(element, '::placeholder').color : null;
          if (paint) sample(element, paint, element.value.trim() ? 'input value' : 'input placeholder', 4.5);
        }
        const textNodes = element instanceof HTMLSlotElement ? element.assignedNodes({ flatten: true }) : element.childNodes;
        for (const node of textNodes) if (node.nodeType === Node.TEXT_NODE && node.textContent?.trim() && !sampledText.has(node)) {
          sampledText.add(node);
          const range = document.createRange(); range.selectNodeContents(node);
          if (![...range.getClientRects()].some(box => box.width > 0 && box.height > 0)) continue;
          const textOwner = (node as Text).assignedSlot ?? element;
          const textCss = getComputedStyle(textOwner);
          const large = parseFloat(textCss.fontSize) >= 24 || parseFloat(textCss.fontSize) >= 18.667 && parseInt(textCss.fontWeight) >= 700;
          sample(textOwner, textCss.color, node.textContent.trim().slice(0, 80), large ? 3 : 4.5, false, 1, [...range.getClientRects()]);
        }
      } else if (sampleKind === 'icon' && element.matches('svg path,svg polygon,svg circle,svg rect')) {
        for (const property of ['fill', 'stroke']) {
          const paint = css.getPropertyValue(property);
          if (paint !== 'none') { const opacity = css.getPropertyValue(`${property}-opacity`); sample(element, paint, `icon ${property}`, 3, false, parseFloat(opacity) / (opacity.endsWith('%') ? 100 : 1)); }
        }
      } else if (sampleKind === 'border') {
        for (const pseudo of [null, '::before', '::after']) {
          const edge = getComputedStyle(element, pseudo);
          if (pseudo && (edge.content === 'none' || edge.visibility === 'hidden' || Number(edge.opacity) === 0)) continue;
          if (edge.transform !== 'none') { const matrix = new DOMMatrixReadOnly(edge.transform); if (matrix.a === 0 || matrix.d === 0) continue; }
          for (const side of ['top', 'right', 'bottom', 'left']) if (parseFloat(edge.getPropertyValue(`border-${side}-width`)) > 0 && !['none', 'hidden'].includes(edge.getPropertyValue(`border-${side}-style`))) sample(element, edge.getPropertyValue(`border-${side}-color`), `${pseudo ?? 'element'} border-${side}`, 3, true);
        }
      } else if (sampleKind === 'focus' || sampleKind === 'cue') {
        for (const pseudo of [null, '::before', '::after']) {
          const paint = getComputedStyle(element, pseudo);
          if (paint.visibility === 'hidden' || paint.display === 'none' || Number(paint.opacity) === 0 || pseudo && paint.content === 'none') continue;
          if (paint.transform !== 'none') { const matrix = new DOMMatrixReadOnly(paint.transform); if (matrix.a === 0 || matrix.d === 0) continue; }
          const qualifies = (keys: string[]) => sampleKind === 'cue' || audit.changed(element, pseudo, paint, resting, keys);
          if (qualifies(['outline-width','outline-style','outline-color','outline-offset']) && parseFloat(paint.outlineWidth) > 0 && paint.outlineStyle !== 'none') {
            const offset = parseFloat(paint.outlineOffset);
            if (offset < 0 && offset + parseFloat(paint.outlineWidth) > 0) throw new Error('Straddling outline adjacency needs pixel evidence');
            sample(element, paint.outlineColor, `${pseudo ?? 'element'} outline`, 3, offset >= 0);
          }
          if (qualifies(['box-shadow'])) for (const shadow of paint.boxShadow.split(/,(?![^()]*\))/u)) {
            if (/\binset\b/u.test(shadow)) continue;
            const lengths = shadow.replace(/[a-z][a-z0-9-]*\([^()]*\)/giu, '').match(/-?[\d.]+px/gu)?.map(Number.parseFloat) ?? [];
            if (lengths.length < 2 || (lengths[2] ?? 0) > 0 || lengths[0] === 0 && lengths[1] === 0 && (lengths[3] ?? 0) <= 0) continue;
            for (const color of shadow.match(/[a-z][a-z0-9-]*\([^()]*\)/giu) ?? []) sample(element, color, `${pseudo ?? 'element'} box-shadow`, 3, true);
          }
          if (pseudo && qualifies(['border-top-width','border-right-width','border-bottom-width','border-left-width','border-top-color','border-right-color','border-bottom-color','border-left-color'])) for (const side of ['top', 'right', 'bottom', 'left']) if (parseFloat(paint.getPropertyValue(`border-${side}-width`)) > 0 && !['none', 'hidden'].includes(paint.getPropertyValue(`border-${side}-style`))) sample(element, paint.getPropertyValue(`border-${side}-color`), `${pseudo} border-${side}`, 3, true);
        }
      }
      if (element instanceof HTMLSlotElement) for (const child of element.assignedElements({ flatten: true })) visit(child);
      if (element.shadowRoot) for (const child of element.shadowRoot.children) visit(child);
      for (const child of element.children) if (!child.matches('style,script,template')) visit(child);
    };
    visit(scope);
    return samples;
  }, kind);
}

export async function expectSurfaceContrast(root: Locator, kind: ContrastKind = 'text') {
  const samples = await measureSurfaceContrast(root, kind);
  expect(samples.length, `Nonempty ${kind} contrast inventory`).toBeGreaterThan(0);
  // A Fluent control may paint a contrasting edge plus a two-color separator.
  // Alternatives belong to that unique control; another control cannot mask a failure.
  const owners = new Set(samples.map(sample => sample.owner));
  const passingOwners = new Set([...owners].filter(owner => samples.some(sample => sample.owner === owner && sample.ratio >= sample.minimum)));
  const failures = samples.filter(sample => sample.ratio < sample.minimum && (!(kind === 'focus' || kind === 'cue' || kind === 'border') || !passingOwners.has(sample.owner)));
  expect(failures, `${kind} contrast failures`).toEqual([]);
  return samples;
}

/** Diagnostic element captures must leave the operator's scroll/focus geometry unchanged. */
export async function screenshotPreservingScroll(target: Locator, options?: Parameters<Locator['screenshot']>[0]) {
  const positions = await target.evaluateHandle(() => {
    const elements = new Set<Element>([document.scrollingElement ?? document.documentElement]);
    const visit = (root: Document | ShadowRoot) => { for (const element of root.querySelectorAll('*')) {
      if (element.scrollHeight > element.clientHeight || element.scrollWidth > element.clientWidth || element.scrollTop || element.scrollLeft) elements.add(element);
      if (element.shadowRoot) visit(element.shadowRoot);
    } };
    visit(document);
    return [...elements].map(element => ({ element, left: element.scrollLeft, top: element.scrollTop }));
  });
  try { return await target.screenshot(options); }
  finally {
    await positions.evaluate(records => { for (const { element, left, top } of records) if (element.isConnected) element.scrollTo({ left, top, behavior: 'instant' }); });
    await positions.dispose();
  }
}

/** Wait for finite theme/control paint transitions without changing the active styling. */
export async function settlePaint(page: Page): Promise<void> {
  await page.evaluate(async () => {
    for (let pass = 0; pass < 5; pass++) {
      await new Promise<void>(resolve => requestAnimationFrame(() => requestAnimationFrame(() => resolve())));
      const roots: (Document | ShadowRoot)[] = [document];
      const collect = (root: Document | ShadowRoot) => { for (const element of root.querySelectorAll('*')) if (element.shadowRoot) { roots.push(element.shadowRoot); collect(element.shadowRoot); } };
      collect(document);
      const animations = roots.flatMap(root => root.getAnimations()).filter(animation => animation.playState === 'running' && Number.isFinite(Number(animation.effect?.getComputedTiming().endTime)));
      if (!animations.length) return;
      await Promise.allSettled(animations.map(animation => animation.finished));
    }
  });
}
