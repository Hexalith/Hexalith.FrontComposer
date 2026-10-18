import { expect, type Locator, type Page } from '@playwright/test';

export type TargetException = {
  selector: string;
  kind: 'Inline' | 'Spacing' | 'Equivalent' | 'User Agent Control' | 'Essential';
  rationale: string;
  keyboardPath: string;
  evidence: string;
  equivalentSelector?: string;
  essentialReason?: string;
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
    const unlabelled: string[] = [];
    const visit = (scope: Document | ShadowRoot) => {
      for (const element of scope.querySelectorAll('*')) {
        if (element.shadowRoot) visit(element.shadowRoot);
        const style = getComputedStyle(element), box = element.getBoundingClientRect();
        if (box.width === 0 || box.height === 0 || style.visibility === 'hidden' || element.closest('[hidden],[aria-hidden="true"]')) continue;
        if (!/(auto|scroll)/u.test(style.overflowX) || element.scrollWidth <= element.clientWidth + 1) continue;
        const labelOwner = element.matches('fluent-tablist') && element.parentElement?.matches('.fluent-tabs') ? element.parentElement : element;
        const labelRoot = labelOwner.getRootNode() as Document | ShadowRoot;
        const label = labelOwner.getAttribute('aria-label')?.trim() || labelOwner.getAttribute('aria-labelledby')?.split(/\s+/u).map(id => labelRoot.getElementById(id)?.textContent?.trim()).filter(Boolean).join(' ');
        const grid = element.matches('[role="region"]') && Boolean(element.querySelector('[role="grid"],table,[data-fc-datagrid]'));
        const tabs = element.matches('[role="tablist"],fluent-tablist');
        if (!(label && (grid || tabs))) unlabelled.push(`${element.tagName}#${element.id}`);
      }
    };
    visit(document);
    return { width: root.clientWidth, scrollWidth: root.scrollWidth, unlabelled };
  });
  expect(geometry.scrollWidth, `Page overflow: ${JSON.stringify(geometry)}`).toBeLessThanOrEqual(geometry.width + 1);
  expect(geometry.unlabelled, 'Horizontal scroll regions must be labelled grids or tablists').toEqual([]);
}

/** Inventory actual hit targets, collapsing a Fluent control and its shadow control to one target. */
export async function inventoryTargets(page: Page, exceptions: TargetException[] = []) {
  await preparePaintAudit(page.locator('body'));
  return page.evaluate((records) => {
    const audit = (window as typeof window & { __aePaintAudit: PaintAudit }).__aePaintAudit;
    const parent = (node: Element): Element | null => node.assignedSlot ?? node.parentElement ?? (node.getRootNode() instanceof ShadowRoot ? (node.getRootNode() as ShadowRoot).host : null);
    const selector = 'a[href],button,input:not([type="hidden"]),select,textarea,summary,[role="button"],[role="tab"],[role="option"],[role="menuitem"],[role="menuitemcheckbox"],[role="menuitemradio"],[role="checkbox"],[role="radio"],[role="switch"],fluent-button,fluent-text-input,fluent-number-input,fluent-select,fluent-combobox,fluent-dropdown,fluent-radio,fluent-switch,[tabindex]:not([tabindex="-1"]),[onclick]';
    const all: Element[] = [];
    const visit = (root: Document | ShadowRoot) => {
      root.querySelectorAll('*').forEach(element => { all.push(element); if (element.shadowRoot) visit(element.shadowRoot); });
    };
    visit(document);
    const visible = (element: Element) => {
      const rect = element.getBoundingClientRect();
      const style = getComputedStyle(element);
      if (rect.width <= 0 || rect.height <= 0 || style.pointerEvents === 'none' || element.matches('.fc-skip-link:not(:focus)')) return false;
      for (let current: Element | null = element; current; current = parent(current)) {
        const css = getComputedStyle(current);
        if (current.hasAttribute('inert') || css.visibility === 'hidden' || css.display === 'none' || Number(css.opacity) === 0) return false;
      }
      // aria-hidden does not deactivate a painted pointer target.
      return true;
    };
    const modalDialog = all.find(element => element.matches('dialog:modal'));
    const modalRoot = modalDialog?.getRootNode();
    const modal = modalRoot instanceof ShadowRoot ? modalRoot.host : modalDialog;
    const candidates = all.filter(element => element.matches(selector) && visible(element)
      && (!modal || element === modal || modal.contains(element) || element.getRootNode() instanceof ShadowRoot && modal.contains((element.getRootNode() as ShadowRoot).host)));
    const targets = candidates.filter(element => {
      const root = element.getRootNode();
      return !(root instanceof ShadowRoot && candidates.includes(root.host))
        && !candidates.some(ancestor => ancestor !== element && ancestor.tagName.startsWith('FLUENT-') && ancestor.contains(element));
    });
    const containsComposed = (owner: Element, child: Element) => { for (let current: Element | null = child; current; current = parent(current)) if (current === owner) return true; return false; };
    const hit = (x: number, y: number) => {
      let element = document.elementFromPoint(x, y);
      while (element?.shadowRoot) { const inner = element.shadowRoot.elementFromPoint(x, y); if (!inner || inner === element) break; element = inner; }
      return element;
    };
    const paintBounds = all.map(element => ({ element, box: element.getBoundingClientRect() }));
    const pointerArea = (element: Element, box: PaintBox, fresh = false): PaintBox => {
      const xs = new Set([box.left, box.right]), ys = new Set([box.top, box.bottom]);
      for (const candidate of paintBounds) {
        if (containsComposed(element, candidate.element) || containsComposed(candidate.element, element)) continue;
        const css = getComputedStyle(candidate.element), rect = fresh ? candidate.element.getBoundingClientRect() : candidate.box;
        if (css.pointerEvents === 'none' || css.visibility === 'hidden' || css.display === 'none' || rect.left >= box.right || rect.right <= box.left || rect.top >= box.bottom || rect.bottom <= box.top) continue;
        const clip = audit.clip(css, rect);
        if (clip.kind === 'unsupported') throw new Error('Clipped pointer occlusion needs pixel evidence');
        if (clip.kind === 'hidden') continue;
        const painted = clip.box ?? rect;
        if (painted.left >= box.right || painted.right <= box.left || painted.top >= box.bottom || painted.bottom <= box.top) continue;
        xs.add(Math.max(box.left, painted.left)); xs.add(Math.min(box.right, painted.right));
        ys.add(Math.max(box.top, painted.top)); ys.add(Math.min(box.bottom, painted.bottom));
      }
      const x = [...xs].sort((a,b) => a-b), y = [...ys].sort((a,b) => a-b);
      const cells = y.slice(1).map((bottom, row) => x.slice(1).map((right, col) => { const target = hit((x[col] + right) / 2, (y[row] + bottom) / 2); return target !== null && containsComposed(element, target); }));
      let best = { left: box.left, right: box.left, top: box.top, bottom: box.top }, score = -1;
      for (let first = 0; first < x.length - 1; first++) for (let last = first + 1; last < x.length; last++) {
        let start = 0;
        for (let row = 0; row < cells.length; row++) {
          if (!cells[row].slice(first, last).every(Boolean)) { start = row + 1; continue; }
          const width = x[last] - x[first], height = y[row + 1] - y[start];
          const rank = (width >= 24 && height >= 24 ? 1e12 : 0) + width * height;
          if (rank > score) { score = rank; best = { left: x[first], right: x[last], top: y[start], bottom: y[row + 1] }; }
        }
      }
      return best;
    };
    const reachable = (element: Element, probeScroll = true): { rect: PaintBox & { width: number; height: number }; limitation: string | null; clipped: boolean } => {
      const original = element.getBoundingClientRect();
      let box: PaintBox = { left: original.left, top: original.top, right: original.right, bottom: original.bottom };
      // Ordinary page/scrollport content can be brought into view. Viewport clipping
      // constrains fixed chrome, while overflow:hidden/clip constrains every target.
      let fixed = false;
      for (let current: Element | null = element; current; current = parent(current)) if (getComputedStyle(current).position === 'fixed') fixed = true;
      let limitation: string | null = null;
      for (let current: Element | null = element; current; current = parent(current)) {
        const css = getComputedStyle(current), bounds = current.getBoundingClientRect();
        const clip = audit.clip(css, bounds);
        if (clip.kind === 'unsupported') limitation = 'Target clip/mask needs pixel evidence';
        if (clip.kind === 'hidden') box.right = box.left;
        if (clip.box) box = { left: Math.max(box.left, clip.box.left), top: Math.max(box.top, clip.box.top), right: Math.min(box.right, clip.box.right), bottom: Math.min(box.bottom, clip.box.bottom) };
        if (current !== element && !['inline','contents'].includes(css.display)) {
          const left = bounds.left + parseFloat(css.borderLeftWidth), right = bounds.right - parseFloat(css.borderRightWidth);
          const top = bounds.top + parseFloat(css.borderTopWidth), bottom = bounds.bottom - parseFloat(css.borderBottomWidth);
          if (/(hidden|clip)/u.test(css.overflowX)) { box.left = Math.max(box.left, left); box.right = Math.min(box.right, right); }
          else if (/(auto|scroll)/u.test(css.overflowX)) {
            const width = Math.min(Math.max(0, box.right - box.left), Math.max(0, right - left));
            box.left = Math.max(left, Math.min(box.left, right - width)); box.right = box.left + width;
          }
          if (/(hidden|clip)/u.test(css.overflowY)) { box.top = Math.max(box.top, top); box.bottom = Math.min(box.bottom, bottom); }
          else if (/(auto|scroll)/u.test(css.overflowY)) {
            const height = Math.min(Math.max(0, box.bottom - box.top), Math.max(0, bottom - top));
            box.top = Math.max(top, Math.min(box.top, bottom - height)); box.bottom = box.top + height;
          }
        }
        if (current.matches(':popover-open,:modal')) break;
      }
      if (fixed) box = { left: Math.max(0, box.left), top: Math.max(0, box.top), right: Math.min(innerWidth, box.right), bottom: Math.min(innerHeight, box.bottom) };
      // Offscreen scrollable content remains reachable by scrolling. Inspect actual
      // pointer paint only while its measured rectangle is currently on screen.
      if (box.left >= 0 && box.top >= 0 && box.right <= innerWidth && box.bottom <= innerHeight
        && Math.abs(box.left - original.left) < .5 && Math.abs(box.top - original.top) < .5 && box.right > box.left && box.bottom > box.top) box = pointerArea(element, box, !probeScroll);
      const outsideViewport = !fixed && (box.left < 0 || box.top < 0 || box.right > innerWidth || box.bottom > innerHeight);
      if (probeScroll && (box.right - box.left < 24 || box.bottom - box.top < 24 || outsideViewport)) {
        const offsets: { element: Element; left: number; top: number }[] = [];
        let scrollable = false;
        for (let current: Element | null = element; current; current = parent(current)) {
          offsets.push({ element: current, left: current.scrollLeft, top: current.scrollTop });
          const css = getComputedStyle(current);
          if (/(auto|scroll)/u.test(css.overflowX) && current.scrollWidth > current.clientWidth
            || /(auto|scroll)/u.test(css.overflowY) && current.scrollHeight > current.clientHeight) scrollable = true;
        }
        const scrolling = document.scrollingElement;
        if (scrolling && !offsets.some(offset => offset.element === scrolling)) offsets.push({ element: scrolling, left: scrolling.scrollLeft, top: scrolling.scrollTop });
        if (scrolling) {
          const css = getComputedStyle(scrolling);
          if (!/(hidden|clip)/u.test(css.overflowX) && scrolling.scrollWidth > scrolling.clientWidth
            || !/(hidden|clip)/u.test(css.overflowY) && scrolling.scrollHeight > scrolling.clientHeight) scrollable = true;
        }
        if (scrollable) {
          try {
            element.scrollIntoView({ block: 'center', inline: 'nearest', behavior: 'instant' });
            const moved = element.getBoundingClientRect(), measured = reachable(element, false);
            const dx = original.left - moved.left, dy = original.top - moved.top;
            // Return target-local reachable geometry in the original page coordinates,
            // so spacing comparisons retain their common unscrolled reference frame.
            return { ...measured, rect: { ...measured.rect, left: measured.rect.left + dx, right: measured.rect.right + dx, top: measured.rect.top + dy, bottom: measured.rect.bottom + dy } };
          } finally {
            for (const offset of offsets.reverse()) offset.element.scrollTo({ left: offset.left, top: offset.top, behavior: 'instant' });
          }
        }
      }
      if (!fixed) box = { left: Math.max(0, box.left), top: Math.max(0, box.top), right: Math.min(innerWidth, box.right), bottom: Math.min(innerHeight, box.bottom) };
      const rect = { ...box, width: Math.max(0, box.right - box.left), height: Math.max(0, box.bottom - box.top) };
      return { rect, limitation, clipped: rect.width < original.width - .5 || rect.height < original.height - .5 };
    };
    const popovers = all.filter(element => element.matches(':popover-open'));
    const boxes = targets.map(element => ({ element, ...reachable(element) })).filter(({element,rect}) => {
      if (rect.width > 0 && rect.height > 0 || element.hasAttribute('data-ae-essential-target') || records.some(record => record.kind === 'Essential' && element.matches(record.selector))) return true;
      const bounds = element.getBoundingClientRect(), covering = hit((bounds.left+bounds.right)/2,(bounds.top+bounds.bottom)/2);
      // A native popover temporarily removes fully covered background controls
      // from the active pointer surface; essential controls retain explicit proof.
      return !covering || !popovers.some(popover => containsComposed(popover,covering));
    });
    const intersects = (a: PaintBox, b: PaintBox) => a.left < b.right && b.left < a.right && a.top < b.bottom && b.top < a.bottom;
    const circleIntersectsRect = (x: number, y: number, rect: PaintBox) => Math.hypot(x - Math.max(rect.left, Math.min(x, rect.right)), y - Math.max(rect.top, Math.min(y, rect.bottom))) < 12 - .01;
    const used = new Set<string>();
    const inventory = boxes.map(({ element, rect, limitation, clipped }, index) => {
      const identifier = element.id ? `#${element.id}` : element.getAttribute('data-testid') ? `[data-testid="${element.getAttribute('data-testid')}"]` : `${element.tagName.toLowerCase()}[inventory-index="${index}"]`;
      const record = records.find(item => element.matches(item.selector));
      let failure: string | null = null;
      const conforming = rect.width >= 24 && rect.height >= 24;
      if (record) used.add(record.selector);
      if (record && conforming) failure = 'Exception is stale: target already meets 24px minimum';
      if (!conforming) {
        if (!record || !record.rationale.trim() || !record.keyboardPath.trim() || !record.evidence.trim()) failure = 'Undersized target lacks an evidenced exception';
        else if (record.kind === 'Spacing') {
          const x = rect.left + rect.width / 2, y = rect.top + rect.height / 2;
          if (boxes.some(other => other.element !== element && (circleIntersectsRect(x, y, other.rect)
            || (other.rect.width < 24 || other.rect.height < 24) && Math.hypot(x - other.rect.left - other.rect.width / 2, y - other.rect.top - other.rect.height / 2) < 24 - .01))) failure = '24px spacing circle intersects a target or neighboring undersized circle';
        } else if (record.kind === 'Equivalent') {
          const equivalent = record.equivalentSelector ? document.querySelector(record.equivalentSelector) : null;
          const box = equivalent ? reachable(equivalent).rect : null;
          if (!equivalent || equivalent === element || !visible(equivalent) || !box || box.width < 24 || box.height < 24 || intersects(rect, box)) failure = 'Equivalent control is absent, identical, overlapping, or undersized';
        } else if (record.kind === 'Inline') {
          if (!element.closest('p,li,dd') || !['inline', 'inline-block'].includes(getComputedStyle(element).display) || element.matches('button,[role="button"],fluent-button')) failure = 'Inline exception does not apply to a control outside running text';
        } else if (record.kind === 'Essential' && (!record.essentialReason?.trim() || !element.matches('[data-ae-essential-target]'))) failure = 'Essential exception requires an identified essential presentation and specific justification';
        else if (record.kind === 'User Agent Control' && (!element.matches('input,select,textarea') || getComputedStyle(element).appearance === 'none' || element.hasAttribute('style'))) failure = 'User agent exception requires an unmodified native control';
      }
      if (limitation) failure = limitation;
      if (rect.width === 0 || rect.height === 0 || clipped && !conforming) failure = 'Clipping leaves the target without the required reachable area';
      const css = getComputedStyle(element);
      const equivalent = record?.kind === 'Equivalent' && record.equivalentSelector ? document.querySelector(record.equivalentSelector) : null;
      const equivalentBox = equivalent ? reachable(equivalent).rect : null;
      return { target: identifier, width: rect.width, height: rect.height,
        box: { left: rect.left, top: rect.top, right: rect.right, bottom: rect.bottom },
        traits: { runningText: Boolean(element.closest('p,li,dd')) && ['inline','inline-block'].includes(css.display) && !element.matches('button,[role="button"],fluent-button'), nativeUnmodified: element.matches('input,select,textarea') && css.appearance !== 'none' && !element.hasAttribute('style'), essentialIdentified: element.hasAttribute('data-ae-essential-target') },
        equivalent: equivalent && equivalentBox ? { distinct: equivalent !== element, visible: visible(equivalent), width: equivalentBox.width, height: equivalentBox.height, box: { left: equivalentBox.left, top: equivalentBox.top, right: equivalentBox.right, bottom: equivalentBox.bottom } } : null,
        conforming, exception: record ?? null, failure };
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
type ShadowContrastBound = {
  kind: 'conservative-black-shadow-composite'; lowerLayer: string;
  layers: { layer: string; declaredAlpha: number; maximumAlpha: number; exteriorBlur?: { factor: number; footprint: PaintBox; source: PaintBox } }[];
  backgroundLayers: ShadowContrastBound['layers'];
  sampleBox?: PaintBox;
  foreground: { minimum: number[]; maximum: number[] };
  background: { minimum: number[]; maximum: number[] };
  closestForeground: number[]; closestBackground: number[]; ratioMinimum: number;
};

type PaintAudit = { extraPaint: (element: Element, pseudo: string | null, omitOwnedElevation?: boolean) => PaintBox[]; shadowBound: (element: Element, pseudo: string | null, layer: string) => ShadowContrastBound | undefined; composition: (css: CSSStyleDeclaration) => void; clip: (css: CSSStyleDeclaration, box: PaintBox) => PaintClip; parse: (value: string) => number[]; resting: (scope: Element) => RestingPaint; changed: (element: Element, pseudo: string | null, style: CSSStyleDeclaration, resting: RestingPaint, properties: string[]) => boolean; shadows: (element: Element, pseudo: string | null, style: CSSStyleDeclaration, resting: RestingPaint) => string[] };

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
    const composition = (css: CSSStyleDeclaration) => {
      if (css.filter !== 'none' || css.mixBlendMode !== 'normal' || css.getPropertyValue('backdrop-filter') && css.getPropertyValue('backdrop-filter') !== 'none') throw new Error('Filtered or blended paint needs pixel evidence');
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
        if (values.some(value => value === null || value < 0)) return { kind: 'unsupported' };
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
    const shadowBounds = new WeakMap<Element, Map<string, ShadowContrastBound>>();
    const shadowBound: PaintAudit['shadowBound'] = (element, pseudo, layer) => shadowBounds.get(element)?.get(`${pseudo ?? ''}|${layer}`);
    // Use unquantized computed rgb/rgba alpha for bounds. Other color spaces remain
    // unsupported here instead of treating canvas's rounded channels as exact paint.
    const rawColor = (value: string) => {
      const components = value.match(/^rgba?\((.*)\)$/u)?.[1].split(/[\s,/]+/u).filter(Boolean);
      if (!components || ![3,4].includes(components.length) || components.some(value => !/^\d+(?:\.\d+)?$/u.test(value))) return null;
      const rgb = components.slice(0, 3).map(Number), alpha = Number(components[3] ?? 1);
      return rgb.every(value => Number.isInteger(value) && value >= 0 && value <= 255) && alpha >= 0 && alpha <= 1 ? { rgb, alpha } : null;
    };
    const composedParent = (node: Element): Element | null => node.assignedSlot ?? node.parentElement ?? (node.getRootNode() instanceof ShadowRoot ? (node.getRootNode() as ShadowRoot).host : null);
    const luminance = (rgb: number[]) => rgb.map(channel => { const c = channel / 255; return c <= .04045 ? c / 12.92 : ((c + .055) / 1.055) ** 2.4; }).reduce((sum, c, i) => sum + c * [.2126,.7152,.0722][i], 0);
    const adjacentBackground = (element: Element) => {
      const layers: { rgb: number[]; alpha: number }[] = [];
      for (let owner = composedParent(element); owner; owner = composedParent(owner)) {
        const css = getComputedStyle(owner); composition(css);
        if (css.backgroundImage !== 'none' || Number(css.opacity) !== 1) throw new Error('Shadow background composition needs pixel evidence');
        const color = rawColor(css.backgroundColor);
        if (!color) throw new Error('Shadow background color needs pixel evidence');
        layers.push(color);
      }
      const canvas = document.createElement('span'); canvas.style.backgroundColor = 'Canvas'; document.body.append(canvas);
      const initial = rawColor(getComputedStyle(canvas).backgroundColor); canvas.remove();
      if (!initial || initial.alpha !== 1) throw new Error('Shadow canvas color needs pixel evidence');
      return layers.reverse().reduce((bg, layer) => layer.rgb.map((value, i) => value * layer.alpha + bg[i] * (1 - layer.alpha)), initial.rgb);
    };
    const shadows: PaintAudit['shadows'] = (element, pseudo, css, snapshots) => {
      const split = (value: string) => value.split(/,(?![^()]*\))/u).map(layer => layer.trim()).filter(layer => layer !== 'none');
      const baseline = snapshots.get(element)?.[pseudo ?? ''];
      // A permanent layer cannot certify an invisible focus-only addition.
      const old = split(baseline?.['box-shadow'] ?? 'none');
      const layers = split(css.boxShadow);
      const extent = (layer: string) => {
        if (/\binset\b/u.test(layer)) return null;
        const color = layer.match(/[a-z][a-z0-9-]*\([^()]*\)/iu)?.[0];
        const values = layer.replace(/[a-z][a-z0-9-]*\([^()]*\)/giu, '').match(/-?[\d.]+px/gu)?.map(Number.parseFloat) ?? [];
        return color && values.length >= 2 && parse(color)[3] > 0 ? { x: values[0], y: values[1], spread: values[3] ?? 0, blur: values[2] ?? 0, alpha: parse(color)[3], rgb: parse(color).slice(0, 3), raw: rawColor(color), layer } : null;
      };
      return layers.filter((layer, layerIndex) => {
        if (!pseudo) {
          const paint = extent(layer);
          // An outer shadow paints behind the opaque border box. A shifted negative
          // spread wholly inside that box changes no visible pixels.
          if (paint && paint.blur === 0 && parse(css.backgroundColor)[3] === 1 && css.backgroundClip === 'border-box'
            && paint.x - paint.spread >= 0 && paint.y - paint.spread >= 0
            && paint.x + paint.spread <= 0 && paint.y + paint.spread <= 0) return false;
        }
        const index = old.indexOf(layer);
        if (index < 0) {
          const paint = extent(layer);
          const preceding = layers.slice(0, layerIndex).map(extent).filter(paint => paint !== null);
          if (paint?.blur === 0) {
            const bounds = element.getBoundingClientRect();
            const envelope = (layer: typeof paint) => {
              const spread = layer.spread + layer.blur * 1.5;
              return { left: bounds.left + layer.x - spread, right: bounds.right + layer.x + spread,
                top: bounds.top + layer.y - spread, bottom: bounds.bottom + layer.y + spread };
            };
            const a = envelope(paint);
            const overlays = preceding.filter(cover => {
              if (cover.alpha >= 1 && cover.blur === 0) return false;
              const b = envelope(cover);
              return a.right > a.left && a.bottom > a.top && b.right > b.left && b.bottom > b.top
                && a.left - 1 < b.right && a.right + 1 > b.left && a.top - 1 < b.bottom && a.bottom + 1 > b.top;
            });
            if (overlays.length) {
              const unsupported = () => new Error('Overlapping translucent shadow layers need pixel evidence');
              if (pseudo || css.transform !== 'none' || Number(css.opacity) !== 1 || paint.raw?.alpha !== 1
                || overlays.some(cover => !cover.raw || cover.raw.rgb.some(value => value !== 0))) throw unsupported();
              // Later shadows can also darken the adjacent background. Bound every
              // other outer layer there, retaining only supported black composition.
              const adjacent = layers.filter((other, index) => index !== layerIndex && !/\binset\b/u.test(other)).map(extent);
              if (adjacent.some(cover => !cover?.raw || cover.raw.rgb.some(value => value !== 0))) throw unsupported();
              // Every blur's local alpha is in [0, declared alpha]. Add one 8-bit alpha
              // step and round RGB endpoints outward; never tighten a bound to pass.
              const record = (cover: NonNullable<ReturnType<typeof extent>>) => ({ layer: cover.layer, declaredAlpha: cover.raw!.alpha, maximumAlpha: Math.min(1, cover.raw!.alpha + 1 / 255) });
              const foregroundLayers = overlays.map(record), backgroundLayers = adjacent.map(cover => record(cover!));
              const transmission = (layers: typeof foregroundLayers) => layers.reduce((value, layer) => value * (1 - layer.maximumAlpha), 1);
              const background = adjacentBackground(element);
              const interval = (rgb: number[], layers: typeof foregroundLayers) => ({ minimum: rgb.map(value => Math.max(0, Math.floor(value * transmission(layers)))), maximum: rgb.map(value => Math.min(255, Math.ceil(value))) });
              const foreground = interval(paint.raw.rgb, foregroundLayers), backdrop = interval(background, backgroundLayers);
              const f = [luminance(foreground.minimum), luminance(foreground.maximum)], b = [luminance(backdrop.minimum), luminance(backdrop.maximum)];
              const foregroundAbove = f[0] > b[1], backgroundAbove = b[0] > f[1];
              const ratioMinimum = Math.max(1, (foregroundAbove ? (f[0] + .05) / (b[1] + .05) : backgroundAbove ? (b[0] + .05) / (f[1] + .05) : 1) - 1e-12);
              if (ratioMinimum < 3) throw unsupported();
              const bound: ShadowContrastBound = { kind: 'conservative-black-shadow-composite', lowerLayer: layer, layers: foregroundLayers, backgroundLayers,
                foreground, background: backdrop, closestForeground: foregroundAbove ? foreground.minimum : foreground.maximum,
                closestBackground: foregroundAbove ? backdrop.maximum : backdrop.minimum, ratioMinimum };
              if (!shadowBounds.has(element)) shadowBounds.set(element, new Map());
              shadowBounds.get(element)!.set(`${pseudo ?? ''}|${layer}`, bound);
            }
          }
          return !paint || !preceding.some(cover => {
            if (paint.blur !== 0 || cover.blur !== 0 || cover.alpha !== 1) return false;
            const covered = cover.x - cover.spread <= paint.x - paint.spread && cover.y - cover.spread <= paint.y - paint.spread
              && cover.x + cover.spread >= paint.x + paint.spread && cover.y + cover.spread >= paint.y + paint.spread;
            if (covered && (cover.x !== paint.x || cover.y !== paint.y)
              && (css.transform !== 'none' || [css.borderTopLeftRadius,css.borderTopRightRadius,css.borderBottomLeftRadius,css.borderBottomRightRadius].some(radius => parseFloat(radius) !== 0))) throw new Error('Overlapping opaque shadow geometry needs pixel evidence');
            return covered;
          });
        }
        old.splice(index, 1); return false;
      });
    };
    const extraPaint = (element: Element, pseudo: string | null, omitOwnedElevation = false): PaintBox[] => {
      const css = getComputedStyle(element, pseudo);
      if (css.visibility === 'hidden' || css.display === 'none' || Number(css.opacity) === 0 || pseudo && ['none','normal'].includes(css.content)) return [];
      const borders = ['top','right','bottom','left'].filter(side => parseFloat(css.getPropertyValue(`border-${side}-width`)) > 0 && !['none','hidden'].includes(css.getPropertyValue(`border-${side}-style`)) && parse(css.getPropertyValue(`border-${side}-color`))[3] > 0);
      const filled = pseudo && (parse(css.backgroundColor)[3] > 0 || css.backgroundImage !== 'none');
      if (!filled && !borders.length && css.boxShadow === 'none') return [];
      const rect = element.getBoundingClientRect();
      let box: PaintBox = { left:rect.left, right:rect.right, top:rect.top, bottom:rect.bottom };
      if (pseudo) {
        const transform = new DOMMatrixReadOnly(css.transform === 'none' ? undefined : css.transform);
        if (transform.is2D && Math.abs(transform.a * transform.d - transform.b * transform.c) < .000001) return [];
        if (!['absolute','fixed'].includes(css.position)) throw new Error('Flow pseudo obstruction needs pixel evidence');
        const parent = (node: Element): Element | null => node.assignedSlot ?? node.parentElement ?? (node.getRootNode() instanceof ShadowRoot ? (node.getRootNode() as ShadowRoot).host : null);
        if (css.position === 'fixed') for (let ancestor: Element | null = element; ancestor; ancestor = parent(ancestor)) {
          if (getComputedStyle(ancestor).transform !== 'none') throw new Error('Fixed pseudo transformed containing block needs pixel evidence');
        }
        let block: Element | null = css.position === 'fixed' ? null : element;
        while (block && getComputedStyle(block).position === 'static' && getComputedStyle(block).transform === 'none') block = parent(block);
        const bounds = block?.getBoundingClientRect(), base = block && bounds ? { left: bounds.left + block.clientLeft, top: bounds.top + block.clientTop, width: block.clientWidth, height: block.clientHeight } : { left: 0, top: 0, width: innerWidth, height: innerHeight };
        const px = (value: string) => /^-?[\d.]+px$/u.test(value) ? parseFloat(value) : null;
        const left = px(css.left), right = px(css.right), top = px(css.top), bottom = px(css.bottom);
        let width = px(css.width), height = px(css.height);
        if (width !== null && css.boxSizing !== 'border-box') width += parseFloat(css.paddingLeft) + parseFloat(css.paddingRight) + parseFloat(css.borderLeftWidth) + parseFloat(css.borderRightWidth);
        if (height !== null && css.boxSizing !== 'border-box') height += parseFloat(css.paddingTop) + parseFloat(css.paddingBottom) + parseFloat(css.borderTopWidth) + parseFloat(css.borderBottomWidth);
        width ??= left !== null && right !== null ? base.width-left-right : null;
        height ??= top !== null && bottom !== null ? base.height-top-bottom : null;
        if (width === null || height === null || left === null && right === null || top === null && bottom === null) throw new Error('Unresolved pseudo obstruction needs pixel evidence');
        const matrix = new DOMMatrixReadOnly(css.transform === 'none' ? undefined : css.transform);
        if (!matrix.is2D) throw new Error('Transformed pseudo obstruction needs pixel evidence');
        if (Math.abs(matrix.a*matrix.d-matrix.b*matrix.c) < .000001) return [];
        const x = base.left + (left ?? base.width-right!-width) + parseFloat(css.marginLeft);
        const y = base.top + (top ?? base.height-bottom!-height) + parseFloat(css.marginTop);
        const origin=css.transformOrigin.split(' ').map(Number.parseFloat);
        if (origin.length<2 || origin.some(value=>!Number.isFinite(value))) throw new Error('Pseudo transform origin needs pixel evidence');
        const points=[[0,0],[width,0],[0,height],[width,height]].map(([a,b])=>({x:x+origin[0]+matrix.a*(a-origin[0])+matrix.c*(b-origin[1])+matrix.e,y:y+origin[1]+matrix.b*(a-origin[0])+matrix.d*(b-origin[1])+matrix.f}));
        box={left:Math.min(...points.map(point=>point.x)),right:Math.max(...points.map(point=>point.x)),top:Math.min(...points.map(point=>point.y)),bottom:Math.max(...points.map(point=>point.y))};
      }
      const boxes: PaintBox[] = filled ? [{ ...box }] : [];
      for (const side of borders) {
        const edge = { ...box }, width = parseFloat(css.getPropertyValue(`border-${side}-width`));
        if (side === 'top') edge.bottom = edge.top+width;
        if (side === 'bottom') edge.top = edge.bottom-width;
        if (side === 'left') edge.right = edge.left+width;
        if (side === 'right') edge.left = edge.right-width;
        boxes.push(edge);
      }
      for (const shadow of omitOwnedElevation ? [] : css.boxShadow.split(/,(?![^()]*\))/u)) {
        if (shadow === 'none') continue;
        const color = shadow.match(/[a-z][a-z0-9-]*\([^()]*\)/iu)?.[0];
        if (!color || parse(color)[3] === 0) continue;
        const lengths = shadow.replace(/[a-z][a-z0-9-]*\([^()]*\)/giu,'').match(/-?[\d.]+px/gu)?.map(Number.parseFloat) ?? [];
        if (lengths.length < 2) throw new Error('Unresolved competing shadow needs pixel evidence');
        const [x,y,blur=0,spread=0] = lengths, extent = spread+blur*1.5;
        if (/\binset\b/u.test(shadow)) { if (x || y || blur || spread) boxes.push({ ...box }); continue; }
        if (box.right-box.left+2*spread <= 0 || box.bottom-box.top+2*spread <= 0 || x === 0 && y === 0 && extent <= 0) continue;
        boxes.push({ left:box.left+x-extent,right:box.right+x+extent,top:box.top+y-extent,bottom:box.bottom+y+extent });
      }
      const clipping = clip(css, box);
      if (clipping.kind === 'hidden') return [];
      if (clipping.kind === 'unsupported') throw new Error('Competing clip/mask needs pixel evidence');
      return boxes.map(box => clipping.box ? { left:Math.max(box.left,clipping.box.left),right:Math.min(box.right,clipping.box.right),top:Math.max(box.top,clipping.box.top),bottom:Math.min(box.bottom,clipping.box.bottom) } : box).filter(box=>box.right>box.left&&box.bottom>box.top);
    };
    (window as typeof window & { __aePaintAudit: PaintAudit }).__aePaintAudit = { parse, resting, changed, clip, shadows, composition, shadowBound, extraPaint };
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
      if (rect.width + 2 * spread <= 0 || rect.height + 2 * spread <= 0 || x === 0 && y === 0 && blur === 0 && spread <= 0) return [];
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
        for (let ancestor: Element | null = visual; ancestor; ancestor = composedParent(ancestor)) {
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
    const shadowBounds: ShadowContrastBound[] = [];
    const box = { left: rect.left, top: rect.top, right: rect.right, bottom: rect.bottom };
    for (const visual of visuals) for (const pseudo of [null, '::before', '::after']) {
      const css = getComputedStyle(visual, pseudo);
      if (css.visibility === 'hidden' || css.display === 'none' || Number(css.opacity) === 0 || pseudo && css.content === 'none') continue;
      let hiddenAncestor = false;
      for (let owner: Element | null = visual; owner; owner = composedParent(owner)) { const style = getComputedStyle(owner); if (Number(style.opacity) === 0 || style.visibility === 'hidden' || style.display === 'none') hiddenAncestor = true; }
      if (hiddenAncestor) continue;
      if (css.transform !== 'none') { const matrix = new DOMMatrixReadOnly(css.transform); if (matrix.a === 0 || matrix.d === 0) continue; }
      const outline = audit.changed(visual, pseudo, css, resting, ['outline-width','outline-style','outline-color','outline-offset']) && parseFloat(css.outlineWidth) > 0 && css.outlineStyle !== 'none' && painted(css.outlineColor);
      const shadowLayers = audit.changed(visual, pseudo, css, resting, ['box-shadow']) ? audit.shadows(visual, pseudo, css, resting) : [];
      for (const layer of shadowLayers) { const bound = audit.shadowBound(visual, pseudo, layer); if (bound) shadowBounds.push(bound); }
      const shadows = shadowLayers.flatMap(outerShadows);
      const shadow = shadows.length > 0;
      let pseudoBorder = pseudo && audit.changed(visual, pseudo, css, resting, ['border-top-width','border-right-width','border-bottom-width','border-left-width','border-top-color','border-right-color','border-bottom-color','border-left-color','border-top-style','border-right-style','border-bottom-style','border-left-style']) && ['Top', 'Right', 'Bottom', 'Left'].some(side => parseFloat(css.getPropertyValue(`border-${side.toLowerCase()}-width`)) > 0 && !['none', 'hidden'].includes(css.getPropertyValue(`border-${side.toLowerCase()}-style`)) && painted(css.getPropertyValue(`border-${side.toLowerCase()}-color`)));
      if (outline || shadow || pseudoBorder) {
        audit.composition(css);
        if (pseudo && (audit.parse(css.backgroundColor)[3] > 0 || css.backgroundImage !== 'none')) throw new Error('Pseudo focus background composition needs pixel evidence');
      }
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
    for (const owner of new Set([element, ...indicatorOwners])) for (let current: Element | null = owner; current; current = composedParent(current)) {
      const css = getComputedStyle(current), matrix = new DOMMatrixReadOnly(css.transform === 'none' ? undefined : css.transform);
      if (!matrix.is2D || matrix.a !== 1 || matrix.d !== 1 || matrix.b !== 0 || matrix.c !== 0
        || css.scale && !['none','1'].includes(css.scale) || css.rotate && css.rotate !== 'none'
        || css.zoom && !['normal','1'].includes(css.zoom)) geometryLimits.push('transformed focus indicator envelope needs pixel evidence');
    }
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
    const related = (candidate: Element) => {
      if (contains(candidate, element) || ownsDropdownIndicator(candidate)) return true;
      const menuIcon = candidate.closest('svg');
      const menuRoot = menuIcon?.getRootNode();
      // The menu button's built-in passive caret is part of its own control paint.
      if (element.matches('fluent-menu-button') && menuRoot instanceof ShadowRoot && menuRoot.host === element
        && menuIcon && !menuIcon.matches('[tabindex],a[href]') && !menuIcon.querySelector('[tabindex],a[href]')) return true;
      if (!contains(element, candidate)) return false;
      const bounds = { ...candidate.getBoundingClientRect().toJSON() } as PaintBox;
      const css = getComputedStyle(candidate);
      // Scrollport contents may have larger layout boxes; only their clipped paint
      // can cover the target. Keep visible overflow in the obstruction inventory.
      for (let owner = parent(candidate); owner; owner = parent(owner)) {
        const style = getComputedStyle(owner), clip = owner.getBoundingClientRect();
        if (/(auto|scroll|hidden|clip)/u.test(style.overflowX)) {
          bounds.left = Math.max(bounds.left, clip.left + owner.clientLeft);
          bounds.right = Math.min(bounds.right, clip.left + owner.clientLeft + owner.clientWidth);
        }
        if (/(auto|scroll|hidden|clip)/u.test(style.overflowY)) {
          bounds.top = Math.max(bounds.top, clip.top + owner.clientTop);
          bounds.bottom = Math.min(bounds.bottom, clip.top + owner.clientTop + owner.clientHeight);
        }
        if (owner === element) break;
      }
      // Flow positioning does not keep negatively margined opaque descendants inside the target.
      const contained = bounds.left >= rect.left && bounds.top >= rect.top && bounds.right <= rect.right && bounds.bottom <= rect.bottom;
      const ordinaryFlow = css.position === 'static' && css.transform === 'none' || css.position === 'relative' && css.zIndex === 'auto' && css.transform === 'none'
        && [css.top, css.right, css.bottom, css.left].every(offset => offset === 'auto' || offset === '0px');
      const root = candidate.getRootNode();
      const nativePart = root instanceof ShadowRoot && root.host.tagName.startsWith('FLUENT-')
        && contains(element, root.host) && candidate.matches('.control,.content,.indicator,.checked-indicator');
      // Spaced Fluent labels may overhang their host. Their transparent flow wrappers
      // and directly slotted text remain owned content; opaque descendants still occlude.
      const nativeLabel = element.tagName.startsWith('FLUENT-') && (nativePart || root instanceof ShadowRoot && root.host === element || candidate.assignedSlot?.getRootNode() === element.shadowRoot)
        && candidate.matches('span,slot') && audit.parse(css.backgroundColor)[3] === 0 && css.backgroundImage === 'none' && css.boxShadow === 'none'
        && ['top','right','bottom','left'].every(side => ['none','hidden'].includes(css.getPropertyValue(`border-${side}-style`)) || parseFloat(css.getPropertyValue(`border-${side}-width`)) === 0);
      if (ordinaryFlow && (contained || nativeLabel)) return true;
      return contained && (audit.parse(css.backgroundColor)[3] === 0 || nativePart);
    };
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
      while (hit) {
        if (hit.shadowRoot) {
          const inner = hit.shadowRoot.elementFromPoint(x, y);
          if (!inner || inner === hit) break;
          hit = inner;
        } else if (hit instanceof HTMLSlotElement) {
          // WebKit returns the slot for projected paint. Inspect its actual assigned
          // element so a transparent slot cannot hide an opaque overflowing child.
          const assigned = hit.assignedElements({ flatten: true }).filter(element => {
            const box = element.getBoundingClientRect();
            return x >= box.left && x <= box.right && y >= box.top && y <= box.bottom;
          }).at(-1);
          if (!assigned) break;
          hit = assigned;
        } else break;
      }
      return hit;
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
      // Outlines are painted outside hit boxes. Content behind the outline is not an
      // obstruction simply because elementFromPoint cannot hit that outline itself.
      if (hit && !related(hit) && (!pointerOptOut || paintsAtPoint(hit)) && (insideTarget || paintsAtPoint(hit) && paintsAbove(hit, element) !== false)) { occluder = `${element.tagName}#${element.id} by ${hit.tagName}#${hit.id} at ${x},${y}`; failures.push('app content obscures target or focus ring'); break; }
    }
    const plane = (node: Element) => { for (let current: Element | null = node; current; current = parent(current)) if (current.matches(':popover-open,:modal')) return current; return null; };
    const focusPlane=plane(element);
    for (const candidate of candidates) {
      if (related(candidate) || focusPlane && !plane(candidate)) continue;
      let hidden = false;
      for (let owner: Element | null = candidate; owner; owner = parent(owner)) { const css = getComputedStyle(owner); if (css.visibility === 'hidden' || css.display === 'none' || Number(css.opacity) === 0) hidden = true; }
      if (hidden) continue;
      for (const pseudo of [null, '::before', '::after']) {
        if (!pseudo && paintsAbove(candidate,element) === false) continue;
        const root=candidate.getRootNode();
        const popup=root instanceof ShadowRoot ? root.host : null;
        const css=getComputedStyle(candidate);
        const shadowSignature=(value: string) => value.split(/,(?![^()]*\))/u).map(shadow=> {
          const color=shadow.match(/[a-z][a-z0-9-]*\([^()]*\)/iu)?.[0];
          const lengths=shadow.replace(/[a-z][a-z0-9-]*\([^()]*\)/giu,'').match(/-?[\d.]+(?:px)?/gu)?.map(Number.parseFloat) ?? [];
          return {color:color ? audit.parse(color) : [],lengths:[...lengths,...Array(Math.max(0,4-lengths.length)).fill(0)],inset:/\binset\b/u.test(shadow)};
        });
        const signature=shadowSignature(css.boxShadow);
        const nativeRule=root instanceof ShadowRoot && root.adoptedStyleSheets.some(sheet=>[...sheet.cssRules].some(rule=>rule instanceof CSSStyleRule && rule.selectorText === ':host div[fuib][popover]' && rule.style.boxShadow.trim() === 'var(--shadow16)'));
        const token=popup ? getComputedStyle(popup).getPropertyValue('--shadow16').trim() : '';
        const nativeEnvelope=signature.length === 2 && signature.every(layer=>!layer.inset&&layer.color.slice(0,3).every(channel=>channel===0))
          && JSON.stringify(signature.map(layer=>layer.lengths)) === '[[0,0,2,0],[0,8,16,0]]'
          && [[.12,.14],[.24,.28]].some(pair=>pair.every((alpha,index)=>signature[index].color[3] === audit.parse(`rgba(0,0,0,${alpha})`)[3]));
        const ownedElevation=!pseudo && element.id && popup?.matches('fluent-popover-b[opened="true"]') && popup.getAttribute('anchor-id') === element.id
          && candidate.matches('div[fuib][part="dialog"][popover]') && nativeRule && nativeEnvelope && token !== ''
          && JSON.stringify(signature) === JSON.stringify(shadowSignature(token));
        // Only the native Fluent shadow16 catalog envelope belongs to its explicit
        // anchor. Authored, near-opaque, enlarged, and unrelated covers remain proof gaps.
        let paint = audit.extraPaint(candidate, pseudo, Boolean(ownedElevation));
        for (let owner: Element | null = candidate; owner && paint.length; owner = parent(owner)) {
          const css = getComputedStyle(owner), bounds = owner.getBoundingClientRect();
          if (owner !== candidate || pseudo) paint = paint.map(box => ({ left: /(auto|scroll|hidden|clip)/u.test(css.overflowX) ? Math.max(box.left,bounds.left) : box.left, right: /(auto|scroll|hidden|clip)/u.test(css.overflowX) ? Math.min(box.right,bounds.right) : box.right, top: /(auto|scroll|hidden|clip)/u.test(css.overflowY) ? Math.max(box.top,bounds.top) : box.top, bottom: /(auto|scroll|hidden|clip)/u.test(css.overflowY) ? Math.min(box.bottom,bounds.bottom) : box.bottom }));
          if (owner.matches(':popover-open,:modal') || pseudo && getComputedStyle(candidate,pseudo).position === 'fixed') break;
        }
        if (paint.some(other => other.right > other.left && other.bottom > other.top && other.left < box.right && other.right > box.left && other.top < box.bottom && other.bottom > box.top)) {
          occluder = JSON.stringify({candidate:candidate.tagName+'#'+candidate.id,pseudo,paint});
          failures.push('competing pseudo/border/shadow paint needs pixel evidence');
          break;
        }
      }
    }
    return { target: `${element.tagName}#${element.id}`, requireIndicator, box, ring, failures, occluder, shadowBounds };
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
    for (let current: Element | null = element; current; current = current.assignedSlot ?? current.parentElement ?? (current.getRootNode() instanceof ShadowRoot ? (current.getRootNode() as ShadowRoot).host : null)) {
      const css = getComputedStyle(current);
      if (Number(css.opacity) !== 1) throw new Error('Group opacity needs pixel evidence');
      backgrounds.push(parse(css.backgroundColor));
    }
    const probe = document.createElement('span'); probe.style.color = 'Canvas'; document.documentElement.append(probe);
    let background = parse(getComputedStyle(probe).color); probe.remove();
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
  await preparePaintAudit(page.locator('body'));
  const failures = await page.evaluate(() => {
    const audit = (window as typeof window & { __aePaintAudit: PaintAudit }).__aePaintAudit;
    const failures = new Set<string>(), visited = new Set<Node>();
    const parent = (node: Node): Element | null => node instanceof Element && node.assignedSlot
      ? node.assignedSlot : node instanceof Text && node.assignedSlot ? node.assignedSlot
        : node.parentElement ?? (node.getRootNode() instanceof ShadowRoot ? (node.getRootNode() as ShadowRoot).host : null);
    const identify = (element: Element) => element.id ? `#${element.id}` : `${element.tagName.toLowerCase()}.${String(element.className).trim().replace(/\s+/gu, '.')}`;
    const excluded = (element: Element) => {
      for (let current: Element | null = element; current; current = parent(current)) {
        const css = getComputedStyle(current);
        if (current.matches('style,script,template,[hidden],.fc-sr-only,.fc-skip-link:not(:focus),.fc-lifecycle-heading:not(:focus-within)')
          || css.visibility === 'hidden' || css.display === 'none' || Number(css.opacity) === 0) return true;
      }
      return false;
    };
    const check = (owner: Element, initial: PaintBox[]) => {
      let boxes = initial.filter(box => box.right > box.left && box.bottom > box.top);
      for (let current: Element | null = owner; current && boxes.length; current = parent(current)) {
        const css = getComputedStyle(current), bounds = current.getBoundingClientRect();
        const explicit = audit.clip(css, bounds);
        if (explicit.kind === 'unsupported') failures.add(`${identify(current)}: text clip/mask needs pixel evidence`);
        if (explicit.kind === 'hidden') { failures.add(identify(current)); break; }
        const clip = explicit.box;
        if (clip) {
          if (boxes.some(box => box.left < clip.left - 1 || box.right > clip.right + 1 || box.top < clip.top - 1 || box.bottom > clip.bottom + 1)) failures.add(identify(current));
          boxes = boxes.map(box => ({ left: Math.max(box.left, clip.left), right: Math.min(box.right, clip.right), top: Math.max(box.top, clip.top), bottom: Math.min(box.bottom, clip.bottom) }));
        }
        if (!['inline', 'contents'].includes(css.display)) {
          const left = bounds.left + parseFloat(css.borderLeftWidth), right = bounds.right - parseFloat(css.borderRightWidth);
          const top = bounds.top + parseFloat(css.borderTopWidth), bottom = bounds.bottom - parseFloat(css.borderBottomWidth);
          if (boxes.some(box => /(hidden|clip)/u.test(css.overflowX) && (box.left < left - 1 || box.right > right + 1)
            || /(hidden|clip)/u.test(css.overflowY) && (box.top < top - 1 || box.bottom > bottom + 1))) failures.add(identify(current));
          // A nested scrollport owns its offscreen content. Only its visible portion
          // can be clipped again by an outer owner, including across shadow slots.
          boxes = boxes.map(box => ({
            left: /(auto|scroll|hidden|clip)/u.test(css.overflowX) ? Math.max(box.left, left) : box.left,
            right: /(auto|scroll|hidden|clip)/u.test(css.overflowX) ? Math.min(box.right, right) : box.right,
            top: /(auto|scroll|hidden|clip)/u.test(css.overflowY) ? Math.max(box.top, top) : box.top,
            bottom: /(auto|scroll|hidden|clip)/u.test(css.overflowY) ? Math.min(box.bottom, bottom) : box.bottom,
          }));
        }
        boxes = boxes.filter(box => box.right > box.left && box.bottom > box.top);
        // Native top-layer surfaces escape outer clips, but retain their own checks.
        if (current.matches(':popover-open,:modal')) break;
      }
    };
    const visit = (node: Node) => {
      if (visited.has(node)) return;
      visited.add(node);
      if (node instanceof Text) {
        const owner = parent(node);
        if (!node.textContent?.trim() || !owner || excluded(owner)) return;
        const range = document.createRange(); range.selectNodeContents(node);
        check(owner, [...range.getClientRects()]);
      } else if (node instanceof Element) {
        if (excluded(node)) return;
        if ((node instanceof HTMLInputElement && ['text','search','email','url','tel','password','number'].includes(node.type) || node instanceof HTMLTextAreaElement || node instanceof HTMLSelectElement)
          && (node instanceof HTMLSelectElement ? node.selectedOptions.length > 0 : node.value || node.placeholder)) {
          const css = getComputedStyle(node), bounds = node.getBoundingClientRect();
          if (bounds.width && bounds.height) {
            const context = document.createElement('canvas').getContext('2d')!;
            context.font = css.font;
            const glyph = context.measureText(node instanceof HTMLSelectElement ? [...node.selectedOptions].map(option => option.label).join(' ') : node.value || node.placeholder);
            const height = glyph.actualBoundingBoxAscent + glyph.actualBoundingBoxDescent;
            const available = bounds.height - parseFloat(css.borderTopWidth) - parseFloat(css.borderBottomWidth) - parseFloat(css.paddingTop) - parseFloat(css.paddingBottom);
            if (height > available + 1) failures.add(`${identify(node)}: native value is vertically clipped`);
            // Native values scroll horizontally by design; their painted viewport
            // still participates in outer composed clipping.
            check(node, [{ left: bounds.left + parseFloat(css.borderLeftWidth), top: bounds.top + parseFloat(css.borderTopWidth), right: bounds.right - parseFloat(css.borderRightWidth), bottom: bounds.bottom - parseFloat(css.borderBottomWidth) }]);
          }
        }
        for (const pseudo of ['::before', '::after']) {
          const generated = getComputedStyle(node, pseudo);
          if (!generated.content || ['none','normal','""',"''"].includes(generated.content) || generated.display === 'none' || generated.visibility === 'hidden' || Number(generated.opacity) === 0) continue;
          const visualContent = generated.content.match(/^("(?:\\.|[^"\\])*"|'(?:\\.|[^'\\])*')(?:\s*\/.*)?$/u)?.[1];
          if (visualContent && ['""', "''"].includes(visualContent)) continue;
          if (/(hidden|clip)/u.test(generated.overflowX + generated.overflowY)) {
            failures.add(`${identify(node)}: generated text clipping needs pixel evidence`);
            continue;
          }
          // Supported normal-flow glyphs fit their host's scroll envelope. Use that
          // conservative envelope through composed clips, including required markers.
          if (visualContent && generated.position === 'static' && generated.transform === 'none' && generated.cssFloat === 'none'
            && node.clientWidth > 0 && node.clientHeight > 0 && node.scrollWidth <= node.clientWidth + .5 && node.scrollHeight <= node.clientHeight + .5
            && [generated.marginTop, generated.marginRight, generated.marginBottom, generated.marginLeft].every(value => Number.isFinite(parseFloat(value)) && parseFloat(value) >= 0)) {
            check(node, [node.getBoundingClientRect()]);
            continue;
          }
          for (let owner: Element | null = node; owner; owner = parent(owner)) {
            const css = getComputedStyle(owner);
            if (/(hidden|clip)/u.test(css.overflowX + css.overflowY) || audit.clip(css, owner.getBoundingClientRect()).kind !== 'none') {
              failures.add(`${identify(owner)}: generated text clipping needs pixel evidence`); break;
            }
            if (owner.matches(':popover-open,:modal')) break;
          }
        }
        if (node instanceof HTMLSlotElement) for (const child of node.assignedNodes({ flatten: true })) visit(child);
        if (node.shadowRoot) for (const child of node.shadowRoot.childNodes) visit(child);
        for (const child of node.childNodes) visit(child);
      }
    };
    visit(document.documentElement);
    return [...failures];
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
        if (css.filter !== 'none' || css.mixBlendMode !== 'normal' || css.getPropertyValue('backdrop-filter') && css.getPropertyValue('backdrop-filter') !== 'none') throw new Error('Filtered or blended paint needs pixel evidence');
        if (Number(css.opacity) > 0 && Number(css.opacity) < 1) throw new Error(`Translucent group needs pixel evidence: ${current.tagName}.${current.className} opacity=${css.opacity}`);
        layers.push(parse(css.backgroundColor));
      }
      const probe = document.createElement('span'); probe.style.color = 'Canvas'; document.documentElement.append(probe);
      const canvas = parse(getComputedStyle(probe).color); probe.remove();
      return layers.reverse().reduce((bg, fg) => composite(fg, bg), canvas);
    };
    const luminance = (color: number[]) => color.slice(0, 3).map(channel => { const c = channel / 255; return c <= .04045 ? c / 12.92 : ((c + .055) / 1.055) ** 2.4; }).reduce((sum, c, i) => sum + c * [.2126, .7152, .0722][i], 0);
    const visible = (element: Element) => {
      if (element.getClientRects().length === 0 && getComputedStyle(element).display !== 'contents') return false;
      for (let current: Element | null = element; current; current = parent(current)) {
        const css = getComputedStyle(current);
        if (sampleKind === 'icon' && current.matches('fluent-skeleton,.fluent-skeleton,.fc-empty-state-body__icon')) return false;
        if (audit.clip(css, current.getBoundingClientRect()).kind === 'hidden') return false;
        if (css.visibility === 'hidden' || css.display === 'none' || Number(css.opacity) === 0 || current.matches('[hidden],.fc-sr-only,.fc-skip-link:not(:focus),[disabled],[aria-disabled="true"]')) return false;
      }
      return true;
    };
    const samples: { owner: number; node: string; paint: string; foreground: number[]; background: number[]; ratio: number; minimum: number; measurement?: 'conservative-bound'; shadowBound?: ShadowContrastBound }[] = [];
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
    const pseudoEdges = new Map<Element, PaintBox>();
    const pseudoOutside = (element: Element, css: CSSStyleDeclaration, side: string) => {
      pseudoEdges.delete(element);
      if (!['absolute','fixed'].includes(css.position)) {
        if ([css.marginTop,css.marginRight,css.marginBottom,css.marginLeft].some(value => parseFloat(value) < 0)) throw new Error('Flow pseudo border adjacency needs pixel evidence');
        const clipping = audit.clip(css, element.getBoundingClientRect());
        if (clipping.kind === 'hidden') return null;
        if (clipping.kind !== 'none') throw new Error('Clipped pseudo border needs pixel evidence');
        return false;
      }
      let containing: Element | null = css.position === 'fixed' ? null : element;
      while (containing && getComputedStyle(containing).position === 'static' && getComputedStyle(containing).transform === 'none') containing = parent(containing);
      if (containing && getComputedStyle(containing).transform !== 'none') {
        const transform = new DOMMatrixReadOnly(getComputedStyle(containing).transform);
        if (!transform.is2D || transform.a !== 1 || transform.d !== 1 || transform.b !== 0 || transform.c !== 0) throw new Error('Transformed pseudo border adjacency needs pixel evidence');
      }
      const bounds = containing?.getBoundingClientRect();
      const base = containing && bounds ? { left: bounds.left + containing.clientLeft, top: bounds.top + containing.clientTop, width: containing.clientWidth, height: containing.clientHeight } : { left: 0, top: 0, width: innerWidth, height: innerHeight };
      const px = (value: string) => /^-?[\d.]+px$/u.test(value) ? parseFloat(value) : null;
      const width = px(css.width), height = px(css.height), left = px(css.left), right = px(css.right), top = px(css.top), bottom = px(css.bottom);
      if (width === null || height === null || left === null && right === null || top === null && bottom === null) throw new Error('Unresolved pseudo border adjacency needs pixel evidence');
      const extraX = css.boxSizing === 'border-box' ? 0 : parseFloat(css.paddingLeft) + parseFloat(css.paddingRight) + parseFloat(css.borderLeftWidth) + parseFloat(css.borderRightWidth);
      const extraY = css.boxSizing === 'border-box' ? 0 : parseFloat(css.paddingTop) + parseFloat(css.paddingBottom) + parseFloat(css.borderTopWidth) + parseFloat(css.borderBottomWidth);
      const matrix = new DOMMatrixReadOnly(css.transform === 'none' ? undefined : css.transform);
      if (!matrix.is2D || matrix.a !== 1 || matrix.d !== 1 || matrix.b !== 0 || matrix.c !== 0) throw new Error('Transformed pseudo border adjacency needs pixel evidence');
      const x = base.left + (left !== null ? left + parseFloat(css.marginLeft) : base.width - right! - width - extraX - parseFloat(css.marginRight)) + matrix.e;
      const y = base.top + (top !== null ? top + parseFloat(css.marginTop) : base.height - bottom! - height - extraY - parseFloat(css.marginBottom)) + matrix.f;
      const box = { left: x, top: y, right: x + width + extraX, bottom: y + height + extraY };
      const clipping = audit.clip(css, box);
      if (clipping.kind === 'hidden') return null;
      if (clipping.kind === 'unsupported') throw new Error('Clipped pseudo border needs pixel evidence');
      const thickness = parseFloat(css.getPropertyValue(`border-${side}-width`));
      if (side === 'top') box.bottom = box.top + thickness;
      if (side === 'bottom') box.top = box.bottom - thickness;
      if (side === 'left') box.right = box.left + thickness;
      if (side === 'right') box.left = box.right - thickness;
      if (clipping.box) {
        box.left = Math.max(box.left, clipping.box.left); box.right = Math.min(box.right, clipping.box.right);
        box.top = Math.max(box.top, clipping.box.top); box.bottom = Math.min(box.bottom, clipping.box.bottom);
        if (box.right <= box.left || box.bottom <= box.top) return null;
      }
      pseudoEdges.set(element, box);
      const control = element.getBoundingClientRect();
      if (box.left >= control.left - .5 && box.right <= control.right + .5 && box.top >= control.top - .5 && box.bottom <= control.bottom + .5) return false;
      if (box.right <= control.left + .5 || box.left >= control.right - .5 || box.bottom <= control.top + .5 || box.top >= control.bottom - .5) return true;
      throw new Error('Straddling pseudo border adjacency needs pixel evidence');
    };
    const pagePaint: Element[] = [];
    const collectPaint = (root: Document | ShadowRoot) => {
      for (const candidate of root.querySelectorAll('*')) { pagePaint.push(candidate); if (candidate.shadowRoot) collectPaint(candidate.shadowRoot); }
    };
    collectPaint(document);
    const clippedPaint = (element: Element, boxes: PaintBox[], content = true): PaintBox[] => {
      if (!boxes.length) return [];
      for (let current: Element | null = element; current; current = parent(current)) {
        const css = getComputedStyle(current);
        if (css.filter !== 'none' || css.mixBlendMode !== 'normal' || css.getPropertyValue('backdrop-filter') && css.getPropertyValue('backdrop-filter') !== 'none') throw new Error('Filtered or blended paint needs pixel evidence');
        const clipping = audit.clip(css, current.getBoundingClientRect());
        if (clipping.kind === 'hidden') return [];
        if (clipping.kind === 'unsupported') throw new Error('Clipped contrast paint needs pixel evidence');
        if (clipping.box) boxes = boxes.map(box => ({ left: Math.max(box.left, clipping.box!.left), right: Math.min(box.right, clipping.box!.right), top: Math.max(box.top, clipping.box!.top), bottom: Math.min(box.bottom, clipping.box!.bottom) }));
        // Scrollports paint only the visible part of a descendant's text range. The
        // offscreen remainder cannot overlap a footer or another neighboring surface.
        const bounds = current.getBoundingClientRect();
        const clipX = /(auto|scroll|hidden|clip)/u.test(css.overflowX), clipY = /(auto|scroll|hidden|clip)/u.test(css.overflowY);
        // Non-replaced inline boxes and display:contents have no overflow clip box.
        if ((content || current !== element) && !['inline', 'contents'].includes(css.display) && (clipX || clipY)) {
          for (let ancestor: Element | null = current; ancestor; ancestor = parent(ancestor)) {
            const style = getComputedStyle(ancestor), transform = new DOMMatrixReadOnly(style.transform === 'none' ? undefined : style.transform);
            if (!transform.is2D || transform.a !== 1 || transform.d !== 1 || transform.b !== 0 || transform.c !== 0
              || style.scale && !['none', '1'].includes(style.scale) || style.rotate && style.rotate !== 'none'
              || style.zoom && !['normal', '1'].includes(style.zoom)) throw new Error('Transformed contrast clipping needs pixel evidence');
          }
          const margin = css.getPropertyValue('overflow-clip-margin').trim();
          if ((css.overflowX === 'clip' || css.overflowY === 'clip') && margin && margin !== '0px') throw new Error('Expanded overflow clipping needs pixel evidence');
          // Viewport rectangles and computed borders preserve fractional padding-box
          // edges. Including a scrollbar gutter is conservative; integer client sizes
          // can round away real paint and must not certify an overlap as absent.
          const left = bounds.left + parseFloat(css.borderLeftWidth), right = bounds.right - parseFloat(css.borderRightWidth);
          const top = bounds.top + parseFloat(css.borderTopWidth), bottom = bounds.bottom - parseFloat(css.borderBottomWidth);
          boxes = boxes.map(box => ({
            left: clipX ? Math.max(box.left, left) : box.left,
            right: clipX ? Math.min(box.right, right) : box.right,
            top: clipY ? Math.max(box.top, top) : box.top,
            bottom: clipY ? Math.min(box.bottom, bottom) : box.bottom,
          }));
        }
        // Out-of-flow backgrounds can escape an outer overflow clip. Without
        // resolving their containing block, retain the overlap for pixel evidence.
        if (current.matches(':popover-open,:modal') || !content && ['absolute', 'fixed'].includes(css.position)) break;
      }
      return boxes.filter(box => box.right > box.left && box.bottom > box.top);
    };
    const pseudoPaint = (element: Element, css: CSSStyleDeclaration): PaintBox[] => {
      if (!['absolute','fixed'].includes(css.position)) throw new Error('Overlapping pseudo paint geometry needs pixel evidence');
      if (css.position === 'fixed') for (let ancestor: Element | null = element; ancestor; ancestor = parent(ancestor)) {
        if (getComputedStyle(ancestor).transform !== 'none') throw new Error('Fixed pseudo transformed containing block needs pixel evidence');
      }
      let block: Element | null = css.position === 'fixed' ? null : element;
      while (block && getComputedStyle(block).position === 'static' && getComputedStyle(block).transform === 'none') block = parent(block);
      const rect = block?.getBoundingClientRect();
      const base = block && rect ? { left: rect.left + block.clientLeft, top: rect.top + block.clientTop, width: block.clientWidth, height: block.clientHeight } : { left: 0, top: 0, width: innerWidth, height: innerHeight };
      const px = (value: string) => /^-?[\d.]+px$/u.test(value) ? parseFloat(value) : null;
      const left = px(css.left), right = px(css.right), top = px(css.top), bottom = px(css.bottom);
      let width = px(css.width), height = px(css.height);
      if (width !== null && css.boxSizing !== 'border-box') width += parseFloat(css.paddingLeft) + parseFloat(css.paddingRight) + parseFloat(css.borderLeftWidth) + parseFloat(css.borderRightWidth);
      if (height !== null && css.boxSizing !== 'border-box') height += parseFloat(css.paddingTop) + parseFloat(css.paddingBottom) + parseFloat(css.borderTopWidth) + parseFloat(css.borderBottomWidth);
      width ??= left !== null && right !== null ? base.width - left - right : null;
      height ??= top !== null && bottom !== null ? base.height - top - bottom : null;
      if (width === null || height === null || left === null && right === null || top === null && bottom === null) throw new Error('Overlapping pseudo paint geometry needs pixel evidence');
      const transform = new DOMMatrixReadOnly(css.transform === 'none' ? undefined : css.transform);
      if (!transform.is2D || transform.a !== 1 || transform.d !== 1 || transform.b !== 0 || transform.c !== 0) throw new Error('Overlapping transformed pseudo paint needs pixel evidence');
      const x = base.left + (left ?? base.width - right! - width) + parseFloat(css.marginLeft) + transform.e;
      const y = base.top + (top ?? base.height - bottom! - height) + parseFloat(css.marginTop) + transform.f;
      let box: PaintBox = { left: x, top: y, right: x + width, bottom: y + height };
      const clip = audit.clip(css, box);
      if (clip.kind === 'hidden') return [];
      if (clip.kind === 'unsupported') throw new Error('Overlapping pseudo clip needs pixel evidence');
      if (clip.box) box = { left: Math.max(box.left, clip.box.left), right: Math.min(box.right, clip.box.right), top: Math.max(box.top, clip.box.top), bottom: Math.min(box.bottom, clip.box.bottom) };
      return clippedPaint(element, [box], false);
    };
    const sample = (element: Element, value: string, label: string, minimum: number, outside = false, alpha = 1, paintBoxes?: PaintBox[], shadowBound?: ShadowContrastBound) => {
      const boxes = clippedPaint(element, paintBoxes ?? [element.getBoundingClientRect()], !outside);
      if (!boxes.length) return;
      const competingShadows = boxes.map<ShadowContrastBound['layers']>(() => []);
      const containsComposed = (ancestor: Element, child: Element) => {
        for (let current: Element | null = child; current; current = parent(current)) if (current === ancestor) return true;
        return false;
      };
      let plane: Element | null = null, opaquePlaneBackdrop = false;
      for (let current: Element | null = element; current; current = parent(current)) {
        const css = getComputedStyle(current), bounds = current.getBoundingClientRect();
        const radii = [css.borderTopLeftRadius, css.borderTopRightRadius, css.borderBottomLeftRadius, css.borderBottomRightRadius];
        const inset = radii.every(value => /^\d+(?:\.\d+)?px$/u.test(value)) ? Math.max(...radii.map(Number.parseFloat)) : Infinity;
        if (parse(css.backgroundColor)[3] === 1 && Number(css.opacity) === 1 && boxes.every(box => box.left >= bounds.left + inset && box.right <= bounds.right - inset && box.top >= bounds.top + inset && box.bottom <= bounds.bottom - inset)) opaquePlaneBackdrop = true;
        if (current.matches(':popover-open,:modal')) { plane = current; break; }
      }
      // Unrelated overlapping paint under glyphs is not represented by ancestor colors.
      // Reject it until a pixel measurement can establish its compositing order.
      if (sampleKind === 'text' || sampleKind === 'icon' || sampleKind === 'focus' && outside) {
        const root = element.getRootNode();
        for (const candidate of pagePaint) {
          const ancestor = candidate === element || containsComposed(candidate, element);
          // An opaque native plane shields text from ordinary page backgrounds.
          // Another native plane may be above it and remains unresolved paint.
          let candidatePlane: Element | null = null;
          for (let current: Element | null = candidate; current; current = parent(current)) if (current.matches(':popover-open,:modal')) { candidatePlane = current; break; }
          if (plane && opaquePlaneBackdrop && !candidatePlane) continue;
          const css = getComputedStyle(candidate), rect = candidate.getBoundingClientRect();
          let hiddenPaint = false;
          for (let current: Element | null = candidate; current; current = parent(current)) {
            const style = getComputedStyle(current);
            if (style.visibility === 'hidden' || style.display === 'none' || Number(style.opacity) === 0 || audit.clip(style, current.getBoundingClientRect()).kind === 'hidden') { hiddenPaint = true; break; }
          }
          if (hiddenPaint) continue;
          const intersects = (rect: PaintBox) => boxes.some(box => rect.left < box.right && rect.right > box.left && rect.top < box.bottom && rect.bottom > box.top);
          for (const pseudo of ['::before','::after']) {
            const generated = getComputedStyle(candidate, pseudo);
            if (generated.content === 'none' || generated.display === 'none' || generated.visibility === 'hidden' || Number(generated.opacity) === 0
              || parse(generated.backgroundColor)[3] === 0 && generated.backgroundImage === 'none') continue;
            // A transparent slot/caret and an empty focus border own no covering fill.
            // Positional generated fills can cover their own text, including when
            // their CSS backgrounds are absent from the Element inventory.
            if (!(candidate === element && sampleKind === 'focus' && label.startsWith(pseudo)) && ['absolute','fixed'].includes(generated.position)) {
              let generatedBoxes=pseudoPaint(candidate,generated);
              const bounds=element.getBoundingClientRect();
              // Fluent tablist owns the selected tab's contained underline. Computed
              // CSS serializes subpixel offsets to fewer digits than DOMRect; constrain
              // only that proven contained paint to its selected control's exact edge.
              if (sampleKind === 'focus' && outside && pseudo === '::after' && candidate.matches('fluent-tablist')
                && element.matches('fluent-tab[aria-selected="true"]') && containsComposed(candidate,element)
                && generatedBoxes.every(box=>box.left>=bounds.left-.001&&box.right<=bounds.right+.001&&box.top>=bounds.top-.001&&box.bottom<=bounds.bottom+.001)) {
                generatedBoxes=generatedBoxes.map(box=>({left:Math.max(box.left,bounds.left),right:Math.min(box.right,bounds.right),top:Math.max(box.top,bounds.top),bottom:Math.min(box.bottom,bounds.bottom)}));
              }
              if (generatedBoxes.some(intersects)) throw new Error('Overlapping pseudo background needs pixel evidence');
            }
          }
          if (!ancestor) for (const pseudo of [null, '::before', '::after']) {
            const paintBoxes = clippedPaint(candidate, audit.extraPaint(candidate, pseudo), false);
            if (!paintBoxes.some(intersects)) continue;
            const unsupported = () => new Error(`Competing border/shadow paint needs pixel evidence: ${JSON.stringify({ element: element.tagName + '#' + element.id + '.' + element.className, candidate: candidate.tagName + '#' + candidate.id + '.' + candidate.className, pseudo, boxes, paintBoxes, candidateBox: rect.toJSON(), shadow: getComputedStyle(candidate, pseudo).boxShadow })}`);
            if (sampleKind !== 'text' || pseudo) throw unsupported();
            const borders = ['top','right','bottom','left'].flatMap(side => {
              const width = parseFloat(css.getPropertyValue(`border-${side}-width`));
              if (!width || ['none','hidden'].includes(css.getPropertyValue(`border-${side}-style`)) || parse(css.getPropertyValue(`border-${side}-color`))[3] === 0) return [];
              return [{ left: side === 'right' ? rect.right - width : rect.left, right: side === 'left' ? rect.left + width : rect.right,
                top: side === 'bottom' ? rect.bottom - width : rect.top, bottom: side === 'top' ? rect.top + width : rect.bottom }];
            });
            if (clippedPaint(candidate, borders, false).some(intersects)) throw unsupported();
            for (let current: Element | null = candidate; current; current = parent(current)) {
              const style = getComputedStyle(current); audit.composition(style);
              if (Number(style.opacity) !== 1 || style.transform !== 'none' || style.scale && !['none','1'].includes(style.scale)
                || style.rotate && style.rotate !== 'none' || style.zoom && !['normal','1'].includes(style.zoom)) throw unsupported();
            }
            for (const layer of css.boxShadow.split(/,(?![^()]*\))/u)) {
              const color = layer.match(/rgba?\(([^()]*)\)/u), rgba = color?.[1].split(/[\s,/]+/u).filter(Boolean).map(Number);
              const lengths = layer.replace(/rgba?\([^()]*\)/u,'').match(/-?[\d.]+px/gu)?.map(Number.parseFloat) ?? [];
              if (!rgba || ![3,4].includes(rgba.length) || rgba.some(value => !Number.isFinite(value)) || rgba.slice(0,3).some(value => value !== 0)
                || /\binset\b/u.test(layer) || lengths.length < 3 || lengths[2] <= 0) throw unsupported();
              const alpha = rgba[3] ?? 1;
              if (alpha < 0 || alpha >= 1) throw unsupported();
              const [x,y,blur,spread=0] = lengths;
              const source = { left: rect.left + x - spread, right: rect.right + x + spread, top: rect.top + y - spread, bottom: rect.bottom + y + spread };
              if (source.right <= source.left || source.bottom <= source.top) continue;
              const envelopes = clippedPaint(candidate, [{ left: source.left - blur * 1.5, right: source.right + blur * 1.5, top: source.top - blur * 1.5, bottom: source.bottom + blur * 1.5 }], false);
              boxes.forEach((box,index) => {
                if (!envelopes.some(paint => paint.left < box.right && paint.right > box.left && paint.top < box.bottom && paint.bottom > box.top)) return;
                // Enclose a full pixel footprint before proving exteriority. A
                // normalized symmetric Gaussian receives at most half its source
                // alpha outside the source rectangle; CSS Backgrounds3 §6.1.2
                // permits 5% approximation error, so retain .55 plus quantization.
                // https://www.w3.org/TR/css-backgrounds-3/#shadow-blur
                const footprint = { left: box.left - 1, right: box.right + 1, top: box.top - 1, bottom: box.bottom + 1 };
                const exterior = footprint.right <= source.left || footprint.left >= source.right || footprint.bottom <= source.top || footprint.top >= source.bottom;
                competingShadows[index].push({ layer, declaredAlpha: alpha, maximumAlpha: Math.min(1, alpha * (exterior ? .55 : 1) + 1 / 255), ...(exterior ? { exteriorBlur: { factor: .55, footprint, source } } : {}) });
              });
            }
          }
          if (ancestor) continue;
          if (sampleKind === 'text' && !containsComposed(element, candidate) && parse(glyphPaint(css))[3] > 0) {
            const glyphBoxes = [...candidate.childNodes].flatMap(node => {
              if (!(node instanceof Text) || !node.textContent?.trim()) return [];
              const range = document.createRange(); range.selectNodeContents(node);
              return [...range.getClientRects()];
            });
            if (clippedPaint(candidate, glyphBoxes, false).some(intersects)) throw new Error('Overlapping text needs pixel evidence');
          }
          const vector = candidate.matches('svg path,svg polygon,svg circle,svg rect,svg ellipse,svg line,svg polyline,svg use,svg text');
          const vectorPaint = vector && (css.fill !== 'none' && Number(css.fillOpacity) > 0 && parse(css.fill)[3] > 0
            || css.stroke !== 'none' && parseFloat(css.strokeWidth) > 0 && Number(css.strokeOpacity) > 0 && parse(css.stroke)[3] > 0);
          // An SVG viewport includes transparent gutters. Its actual shape boxes,
          // rather than the empty viewport, compete with neighboring ring paint.
          const replaced = candidate.matches('img,canvas,video,iframe,object,embed') || vectorPaint;
          if (parse(css.backgroundColor)[3] === 0 && css.backgroundImage === 'none' && !replaced) continue;
          let competingBox: PaintBox = rect;
          if (vectorPaint && css.stroke !== 'none' && parseFloat(css.strokeWidth) > 0 && parse(css.stroke)[3] > 0) {
            const matrix = (candidate as SVGGraphicsElement).getScreenCTM();
            if (!matrix) throw new Error('Unresolved competing SVG stroke needs pixel evidence');
            const half = parseFloat(css.strokeWidth) / 2, fixed = css.vectorEffect === 'non-scaling-stroke';
            const x = fixed ? half : half * Math.hypot(matrix.a, matrix.c), y = fixed ? half : half * Math.hypot(matrix.b, matrix.d);
            competingBox = { left: rect.left - x, right: rect.right + x, top: rect.top - y, bottom: rect.bottom + y };
          }
          if (!intersects(competingBox)) continue;
          // A background behind a composed-tree scrollport cannot paint over adjacent
          // text. Keep real visible intersections, including tiny shadow-root overlaps.
          const candidateBoxes = clippedPaint(candidate, [competingBox], false);
          if (candidateBoxes.some(intersects)) throw new Error(`Overlapping ${replaced ? 'replaced paint' : 'sibling background'} needs pixel evidence: ${JSON.stringify({ element: element.tagName + '#' + element.id + '.' + element.className, candidate: candidate.tagName + '#' + candidate.id + '.' + candidate.className, boxes, candidateBox: rect.toJSON(), candidateBoxes, candidateClip: audit.clip(css, rect), candidatePosition: css.position, root: root instanceof ShadowRoot ? root.host.tagName : 'document' })}`);
        }
      }
      const color = parse(value); color[3] *= alpha;
      if (color[3] === 0 && sampleKind !== 'icon') return;
      const bg = background(outside ? parent(element) : element);
      const fg = composite(color, bg), a = luminance(fg), b = luminance(bg);
      let measuredBound = shadowBound;
      for (const [index,layers] of competingShadows.entries()) {
        if (!layers.length) continue;
        // Aggregate every intersecting layer per text fragment. Shadows on
        // disjoint lines cannot all darken one glyph or its adjacent backdrop.
        const transmission = layers.reduce((value, layer) => value * (1 - layer.maximumAlpha), 1);
        const interval = (rgb: number[]) => ({ minimum: rgb.slice(0,3).map(value => Math.max(0, Math.floor(value * transmission))), maximum: rgb.slice(0,3).map(value => Math.min(255, Math.ceil(value))) });
        const foreground = interval(fg), backdrop = interval(bg);
        const f = [luminance(foreground.minimum), luminance(foreground.maximum)], b = [luminance(backdrop.minimum), luminance(backdrop.maximum)];
        const foregroundAbove = f[0] > b[1], backgroundAbove = b[0] > f[1];
        const ratioMinimum = Math.max(1, (foregroundAbove ? (f[0] + .05) / (b[1] + .05) : backgroundAbove ? (b[0] + .05) / (f[1] + .05) : 1) - 1e-12);
        if (ratioMinimum < minimum) throw new Error(`Competing border/shadow paint needs pixel evidence: conservative ratio ${ratioMinimum} below ${minimum}`);
        if (!measuredBound || ratioMinimum < measuredBound.ratioMinimum) measuredBound = { kind: 'conservative-black-shadow-composite', lowerLayer: value, layers, backgroundLayers: layers, sampleBox: boxes[index],
          foreground, background: backdrop, closestForeground: foregroundAbove ? foreground.minimum : foreground.maximum,
          closestBackground: foregroundAbove ? backdrop.maximum : backdrop.minimum, ratioMinimum };
      }
      const owner = controlOwner(element);
      if (!owners.has(owner)) owners.set(owner, owners.size);
      samples.push({ ...(measuredBound ? { measurement: 'conservative-bound' as const, shadowBound: measuredBound } : {}), owner: owners.get(owner)!, node: `${element.tagName.toLowerCase()}${element.id ? '#' + element.id : ''}`, paint: label, foreground: measuredBound?.closestForeground ?? fg, background: measuredBound?.closestBackground ?? bg, ratio: measuredBound?.ratioMinimum ?? (Math.max(a, b) + .05) / (Math.min(a, b) + .05), minimum });
    };
    const glyphPaint = (css: CSSStyleDeclaration) => {
      if (css.textShadow !== 'none') throw new Error('Text shadow composition needs pixel evidence');
      if (parseFloat(css.getPropertyValue('-webkit-text-stroke-width')) > 0) throw new Error('Text stroke composition needs pixel evidence');
      return matchMedia('(forced-colors: active)').matches && css.forcedColorAdjust !== 'none' ? css.color : css.getPropertyValue('-webkit-text-fill-color') || css.color;
    };
    const filledArea = (element: Element) => {
      if (!element.matches('path,polygon,polyline')) return true;
      const box = (element as SVGGraphicsElement).getBBox();
      if (box.width <= 0 || box.height <= 0) return false;
      const path = element.matches('path') ? new Path2D(element.getAttribute('d') ?? '') : new Path2D();
      if (!element.matches('path')) {
        const points = (element as SVGPolygonElement).points;
        for (let i = 0; i < points.numberOfItems; i++) { const point = points.getItem(i); if (i === 0) path.moveTo(point.x, point.y); else path.lineTo(point.x, point.y); }
      }
      // A nonempty bounding box can still describe a zero-area diagonal path.
      const canvas = document.createElement('canvas'); canvas.width = canvas.height = 128;
      const context = canvas.getContext('2d')!;
      context.setTransform(126 / box.width, 0, 0, 126 / box.height, 1 - box.x * 126 / box.width, 1 - box.y * 126 / box.height);
      context.fill(path, getComputedStyle(element).fillRule as CanvasFillRule);
      return context.getImageData(0, 0, 128, 128).data.some((channel, i) => i % 4 === 3 && channel > 0);
    };
    const sampledText = new Set<Node>();
    const visited = new Set<Element>();
    const visit = (element: Element) => {
      if (visited.has(element) || !visible(element)) return;
      visited.add(element);
      const css = getComputedStyle(element);
      if (sampleKind === 'text') {
        if (element instanceof HTMLInputElement || element instanceof HTMLTextAreaElement) {
          const placeholder = getComputedStyle(element, '::placeholder');
          const glyph = glyphPaint;
          const paint = element.value.trim() ? glyph(css) : element.getAttribute('placeholder') ? glyph(placeholder) : null;
          if (paint && !element.value.trim()) { audit.composition(placeholder); if (placeholder.transform !== 'none') throw new Error('Placeholder geometry needs pixel evidence'); }
          if (paint) sample(element, paint, element.value.trim() ? 'input value' : 'input placeholder', 4.5, false, element.value.trim() ? 1 : Number(placeholder.opacity));
        }
        const textNodes = element instanceof HTMLSlotElement ? element.assignedNodes({ flatten: true }) : element.childNodes;
        for (const node of textNodes) if (node.nodeType === Node.TEXT_NODE && node.textContent?.trim() && !sampledText.has(node)) {
          sampledText.add(node);
          const range = document.createRange(); range.selectNodeContents(node);
          if (![...range.getClientRects()].some(box => box.width > 0 && box.height > 0)) continue;
          const textOwner = (node as Text).assignedSlot ?? element;
          const textCss = getComputedStyle(textOwner);
          const large = parseFloat(textCss.fontSize) >= 24 || parseFloat(textCss.fontSize) >= 18.667 && parseInt(textCss.fontWeight) >= 700;
          sample(textOwner, glyphPaint(textCss), node.textContent.trim().slice(0, 80), large ? 3 : 4.5, false, 1, [...range.getClientRects()]);
        }
        for (const pseudo of ['::before', '::after']) {
          const generated = getComputedStyle(element, pseudo);
          if (!generated.content || ['none','normal','""',"''"].includes(generated.content) || generated.visibility === 'hidden' || generated.display === 'none' || Number(generated.opacity) === 0) continue;
          const visualContent = generated.content.match(/^("(?:\\.|[^"\\])*"|'(?:\\.|[^'\\])*')(?:\s*\/.*)?$/u)?.[1];
          if (!visualContent) throw new Error(`Generated content needs pixel evidence: ${element.tagName}${pseudo} ${generated.content}`);
          if (['""', "''"].includes(visualContent)) continue;
          audit.composition(generated);
          if (generated.position !== 'static' || generated.transform !== 'none' || generated.cssFloat !== 'none'
            || element.clientWidth > 0 && element.scrollWidth > element.clientWidth + .5 || element.clientHeight > 0 && element.scrollHeight > element.clientHeight + .5
            || [generated.marginTop, generated.marginRight, generated.marginBottom, generated.marginLeft].some(value => !Number.isFinite(parseFloat(value)) || parseFloat(value) < 0)) throw new Error(`Generated text geometry needs pixel evidence: ${JSON.stringify({ element: element.tagName + '#' + element.id + '.' + element.className, pseudo, content: generated.content, position: generated.position, margins: [generated.marginTop, generated.marginRight, generated.marginBottom, generated.marginLeft], transform: generated.transform, float: generated.cssFloat, root: element.getRootNode() instanceof ShadowRoot ? (element.getRootNode() as ShadowRoot).host.tagName : 'document' })}`);
          if (parse(generated.backgroundColor)[3] > 0 || generated.backgroundImage !== 'none') throw new Error('Generated text background needs pixel evidence');
          const large = parseFloat(generated.fontSize) >= 24 || parseFloat(generated.fontSize) >= 18.667 && parseInt(generated.fontWeight) >= 700;
          sample(element, glyphPaint(generated), `${pseudo} generated text`, large ? 3 : 4.5, false, Number(generated.opacity));
        }
      } else if (sampleKind === 'icon' && element.matches('svg path,svg polygon,svg circle,svg rect,svg ellipse,svg line,svg polyline,svg use,svg text')) {
        if (element.matches('use')) throw new Error('SVG use instance needs pixel evidence');
        for (const property of ['fill', 'stroke']) {
          const paint = css.getPropertyValue(property);
          if (property === 'stroke' && parseFloat(css.strokeWidth) <= 0 || property === 'fill' && element.matches('line')) continue;
          if (paint !== 'none') {
            const opacity = css.getPropertyValue(`${property}-opacity`), bounds = element.getBoundingClientRect();
            if (property === 'fill' && (!bounds.width || !bounds.height || !filledArea(element))) continue;
            let boxes: PaintBox[] | undefined;
            if (property === 'stroke') {
              if (css.strokeDasharray !== 'none') throw new Error('SVG dashed stroke needs pixel evidence');
              const matrix = (element as SVGGraphicsElement).getScreenCTM();
              if (!matrix) throw new Error('Unresolved SVG stroke envelope needs pixel evidence');
              const half = parseFloat(css.strokeWidth) / 2, fixed = css.vectorEffect === 'non-scaling-stroke';
              const x = fixed ? half : half * Math.hypot(matrix.a, matrix.c), y = fixed ? half : half * Math.hypot(matrix.b, matrix.d);
              boxes = [{ left: bounds.left - x, right: bounds.right + x, top: bounds.top - y, bottom: bounds.bottom + y }];
            }
            sample(element, paint, `icon ${property}`, 3, false, parseFloat(opacity) / (opacity.endsWith('%') ? 100 : 1), boxes);
          }
        }
      } else if (sampleKind === 'border') {
        for (const pseudo of [null, '::before', '::after']) {
          const edge = getComputedStyle(element, pseudo);
          if (pseudo && (edge.content === 'none' || edge.visibility === 'hidden' || Number(edge.opacity) === 0)) continue;
          if (edge.transform !== 'none') { const matrix = new DOMMatrixReadOnly(edge.transform); if (matrix.a === 0 || matrix.d === 0) continue; }
          audit.composition(edge);
          if (pseudo && (parse(edge.backgroundColor)[3] > 0 || edge.backgroundImage !== 'none')) throw new Error('Pseudo border background composition needs pixel evidence');
          for (const side of ['top', 'right', 'bottom', 'left']) if (parseFloat(edge.getPropertyValue(`border-${side}-width`)) > 0 && !['none', 'hidden'].includes(edge.getPropertyValue(`border-${side}-style`))) {
            const outside = pseudo ? pseudoOutside(element, edge, side) : true;
            if (outside !== null) sample(element, edge.getPropertyValue(`border-${side}-color`), `${pseudo ?? 'element'} border-${side}`, 3, outside, Number(edge.opacity));
          }
        }
      } else if (sampleKind === 'focus' || sampleKind === 'cue') {
        for (const pseudo of [null, '::before', '::after']) {
          const paint = getComputedStyle(element, pseudo);
          if (paint.visibility === 'hidden' || paint.display === 'none' || Number(paint.opacity) === 0 || pseudo && paint.content === 'none') continue;
          if (paint.transform !== 'none') { const matrix = new DOMMatrixReadOnly(paint.transform); if (matrix.a === 0 || matrix.d === 0) continue; }
          const qualifies = (keys: string[]) => {
            const changed = sampleKind === 'cue' || audit.changed(element, pseudo, paint, resting, keys);
            if (changed) {
              audit.composition(paint);
              if (pseudo && (parse(paint.backgroundColor)[3] > 0 || paint.backgroundImage !== 'none')) throw new Error('Pseudo focus background composition needs pixel evidence');
            }
            return changed;
          };
          if (qualifies(['outline-width','outline-style','outline-color','outline-offset']) && parseFloat(paint.outlineWidth) > 0 && paint.outlineStyle !== 'none') {
            if (pseudo && audit.clip(paint, element.getBoundingClientRect()).kind !== 'none') throw new Error('Clipped pseudo focus paint needs pixel evidence');
            const offset = parseFloat(paint.outlineOffset);
            if (offset < 0 && offset + parseFloat(paint.outlineWidth) > 0) throw new Error('Straddling outline adjacency needs pixel evidence');
            let outlineBoxes: PaintBox[] | undefined;
            if (offset >= 0) {
              const extent = offset + parseFloat(paint.outlineWidth);
              outlineBoxes = (pseudo ? pseudoPaint(element, paint) : [element.getBoundingClientRect()]).flatMap(rect => [
                { left: rect.left - extent, right: rect.right + extent, top: rect.top - extent, bottom: rect.top - offset },
                { left: rect.left - extent, right: rect.right + extent, top: rect.bottom + offset, bottom: rect.bottom + extent },
                { left: rect.left - extent, right: rect.left - offset, top: rect.top - offset, bottom: rect.bottom + offset },
                { left: rect.right + offset, right: rect.right + extent, top: rect.top - offset, bottom: rect.bottom + offset },
              ]);
            }
            sample(element, paint.outlineColor, `${pseudo ?? 'element'} outline`, 3, offset >= 0, Number(paint.opacity), outlineBoxes);
          }
          if (qualifies(['box-shadow'])) for (const shadow of audit.shadows(element, pseudo, paint, resting)) {
            if (/\binset\b/u.test(shadow)) continue;
            if (pseudo && audit.clip(paint, element.getBoundingClientRect()).kind !== 'none') throw new Error('Clipped pseudo focus paint needs pixel evidence');
            const lengths = shadow.replace(/[a-z][a-z0-9-]*\([^()]*\)/giu, '').match(/-?[\d.]+px/gu)?.map(Number.parseFloat) ?? [];
            const bounds = element.getBoundingClientRect(), spread = lengths[3] ?? 0;
            if (bounds.width + 2 * spread <= 0 || bounds.height + 2 * spread <= 0 || lengths.length < 2 || (lengths[2] ?? 0) > 0 || lengths[0] === 0 && lengths[1] === 0 && (lengths[3] ?? 0) <= 0) continue;
            const source = pseudo ? pseudoPaint(element, paint) : [bounds];
            const shadowBoxes = source.flatMap(rect => {
              const outer = { left: rect.left + lengths[0] - spread, right: rect.right + lengths[0] + spread, top: rect.top + lengths[1] - spread, bottom: rect.bottom + lengths[1] + spread };
              // Only paint outside the opaque control contributes an outer ring.
              return [
                { ...outer, bottom: Math.min(outer.bottom, bounds.top) },
                { ...outer, top: Math.max(outer.top, bounds.bottom) },
                { left: outer.left, right: Math.min(outer.right, bounds.left), top: Math.max(outer.top, bounds.top), bottom: Math.min(outer.bottom, bounds.bottom) },
                { left: Math.max(outer.left, bounds.right), right: outer.right, top: Math.max(outer.top, bounds.top), bottom: Math.min(outer.bottom, bounds.bottom) },
              ].filter(box => box.right > box.left && box.bottom > box.top);
            });
            for (const color of shadow.match(/[a-z][a-z0-9-]*\([^()]*\)/giu) ?? []) sample(element, color, `${pseudo ?? 'element'} box-shadow${audit.shadowBound(element, pseudo, shadow) ? ' (conservative composite bound)' : ''}`, 3, true, Number(paint.opacity), shadowBoxes, audit.shadowBound(element, pseudo, shadow));
          }
          if (pseudo && qualifies(['border-top-width','border-right-width','border-bottom-width','border-left-width','border-top-color','border-right-color','border-bottom-color','border-left-color'])) for (const side of ['top', 'right', 'bottom', 'left']) if (parseFloat(paint.getPropertyValue(`border-${side}-width`)) > 0 && !['none', 'hidden'].includes(paint.getPropertyValue(`border-${side}-style`))) {
            const outside = pseudoOutside(element, paint, side);
            if (outside !== null) sample(element, paint.getPropertyValue(`border-${side}-color`), `${pseudo} border-${side}`, 3, outside, Number(paint.opacity), pseudoEdges.has(element) ? [pseudoEdges.get(element)!] : undefined);
          }
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
