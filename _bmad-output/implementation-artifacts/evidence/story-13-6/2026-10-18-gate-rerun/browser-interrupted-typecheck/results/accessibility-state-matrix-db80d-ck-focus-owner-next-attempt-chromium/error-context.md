# Instructions

- Following Playwright test failed.
- Explain why, be concise, respect Playwright best practices.
- Provide a snippet of code with the fix, if possible.

# Test info

- Name: accessibility-state-matrix.spec.ts >> AE complete state matrix dark >> generated zero-field admission: safe feedback, focus, owner, next attempt
- Location: specs/accessibility-state-matrix.spec.ts:160:7

# Error details

```
Error: page.evaluate: Test ended.
```

# Test source

```ts
  1   | import { expect, type Locator, type Page } from '@playwright/test';
  2   | 
  3   | export type TargetException = {
  4   |   selector: string;
  5   |   kind: 'Inline' | 'Spacing' | 'Equivalent' | 'User Agent Control' | 'Essential';
  6   |   rationale: string;
  7   |   keyboardPath: string;
  8   |   evidence: string;
  9   |   equivalentSelector?: string;
  10  |   essentialReason?: string;
  11  | };
  12  | 
  13  | /** Apply all four overrides through rerenders, including shadow roots attached later. */
  14  | export async function applyTextSpacing(page: Page): Promise<void> {
  15  |   await page.evaluate(() => {
  16  |     const owner = window as unknown as { aeSpacingCleanup?: () => void };
  17  |     owner.aeSpacingCleanup?.();
  18  |     const css = '* { line-height: 1.5 !important; letter-spacing: .12em !important; word-spacing: .16em !important; } p { margin-block-end: 2em !important; }';
  19  |     const roots = new Set<Document | ShadowRoot>();
  20  |     const visit = (root: Document | ShadowRoot) => {
  21  |       if (!roots.has(root)) { roots.add(root); observer.observe(root instanceof Document ? root.documentElement : root, { childList: true, subtree: true }); }
  22  |       if (!root.querySelector('[data-ae-text-spacing]')) {
  23  |         const style = document.createElement('style'); style.dataset.aeTextSpacing = 'true'; style.textContent = css;
  24  |         (root instanceof Document ? root.head : root).append(style);
  25  |       }
  26  |       root.querySelectorAll('*').forEach(element => { if (element.shadowRoot) visit(element.shadowRoot); });
  27  |     };
  28  |     const observer = new MutationObserver(() => { for (const root of roots) visit(root); });
  29  |     const original = Element.prototype.attachShadow;
  30  |     const patched = function (this: Element, init: ShadowRootInit) { const root = original.call(this, init); visit(root); return root; };
  31  |     Element.prototype.attachShadow = patched;
  32  |     owner.aeSpacingCleanup = () => {
  33  |       observer.disconnect();
  34  |       if (Element.prototype.attachShadow === patched) Element.prototype.attachShadow = original;
  35  |       for (const root of roots) root.querySelectorAll('[data-ae-text-spacing]').forEach(style => style.remove());
  36  |       delete owner.aeSpacingCleanup;
  37  |     };
  38  |     visit(document);
  39  |   });
  40  | }
  41  | 
  42  | /** Remove the overrides and their observer/prototype seam before a restoration round trip. */
  43  | export async function removeTextSpacing(page: Page): Promise<void> {
  44  |   await page.evaluate(() => (window as unknown as { aeSpacingCleanup?: () => void }).aeSpacingCleanup?.());
  45  | }
  46  | 
  47  | export async function expectNoPageOverflow(page: Page): Promise<void> {
  48  |   const geometry = await page.evaluate(() => {
  49  |     const root = document.scrollingElement ?? document.documentElement;
  50  |     const unlabelled: string[] = [];
  51  |     const visit = (scope: Document | ShadowRoot) => {
  52  |       for (const element of scope.querySelectorAll('*')) {
  53  |         if (element.shadowRoot) visit(element.shadowRoot);
  54  |         const style = getComputedStyle(element), box = element.getBoundingClientRect();
  55  |         if (box.width === 0 || box.height === 0 || style.visibility === 'hidden' || element.closest('[hidden],[aria-hidden="true"]')) continue;
  56  |         if (!/(auto|scroll)/u.test(style.overflowX) || element.scrollWidth <= element.clientWidth + 1) continue;
  57  |         const labelOwner = element.matches('fluent-tablist') && element.parentElement?.matches('.fluent-tabs') ? element.parentElement : element;
  58  |         const labelRoot = labelOwner.getRootNode() as Document | ShadowRoot;
  59  |         const label = labelOwner.getAttribute('aria-label')?.trim() || labelOwner.getAttribute('aria-labelledby')?.split(/\s+/u).map(id => labelRoot.getElementById(id)?.textContent?.trim()).filter(Boolean).join(' ');
  60  |         const grid = element.matches('[role="region"]') && Boolean(element.querySelector('[role="grid"],table,[data-fc-datagrid]'));
  61  |         const tabs = element.matches('[role="tablist"],fluent-tablist');
  62  |         if (!(label && (grid || tabs))) unlabelled.push(`${element.tagName}#${element.id}`);
  63  |       }
  64  |     };
  65  |     visit(document);
  66  |     return { width: root.clientWidth, scrollWidth: root.scrollWidth, unlabelled };
  67  |   });
  68  |   expect(geometry.scrollWidth, `Page overflow: ${JSON.stringify(geometry)}`).toBeLessThanOrEqual(geometry.width + 1);
  69  |   expect(geometry.unlabelled, 'Horizontal scroll regions must be labelled grids or tablists').toEqual([]);
  70  | }
  71  | 
  72  | /** Inventory actual hit targets, collapsing a Fluent control and its shadow control to one target. */
  73  | export async function inventoryTargets(page: Page, exceptions: TargetException[] = []) {
  74  |   await preparePaintAudit(page.locator('body'));
> 75  |   return page.evaluate((records) => {
      |               ^ Error: page.evaluate: Test ended.
  76  |     const audit = (window as typeof window & { __aePaintAudit: PaintAudit }).__aePaintAudit;
  77  |     const parent = (node: Element): Element | null => node.assignedSlot ?? node.parentElement ?? (node.getRootNode() instanceof ShadowRoot ? (node.getRootNode() as ShadowRoot).host : null);
  78  |     const selector = 'a[href],button,input:not([type="hidden"]),select,textarea,summary,[role="button"],[role="tab"],[role="option"],[role="menuitem"],[role="menuitemcheckbox"],[role="menuitemradio"],[role="checkbox"],[role="radio"],[role="switch"],fluent-button,fluent-text-input,fluent-number-input,fluent-select,fluent-combobox,fluent-dropdown,fluent-radio,fluent-switch,[tabindex]:not([tabindex="-1"]),[onclick]';
  79  |     const all: Element[] = [];
  80  |     const visit = (root: Document | ShadowRoot) => {
  81  |       root.querySelectorAll('*').forEach(element => { all.push(element); if (element.shadowRoot) visit(element.shadowRoot); });
  82  |     };
  83  |     visit(document);
  84  |     const visible = (element: Element) => {
  85  |       const rect = element.getBoundingClientRect();
  86  |       const style = getComputedStyle(element);
  87  |       if (rect.width <= 0 || rect.height <= 0 || style.pointerEvents === 'none' || element.matches('.fc-skip-link:not(:focus)')) return false;
  88  |       for (let current: Element | null = element; current; current = parent(current)) {
  89  |         const css = getComputedStyle(current);
  90  |         if (current.hasAttribute('inert') || css.visibility === 'hidden' || css.display === 'none' || Number(css.opacity) === 0) return false;
  91  |       }
  92  |       // aria-hidden does not deactivate a painted pointer target.
  93  |       return true;
  94  |     };
  95  |     const modalDialog = all.find(element => element.matches('dialog:modal'));
  96  |     const modalRoot = modalDialog?.getRootNode();
  97  |     const modal = modalRoot instanceof ShadowRoot ? modalRoot.host : modalDialog;
  98  |     const candidates = all.filter(element => element.matches(selector) && visible(element)
  99  |       && (!modal || element === modal || modal.contains(element) || element.getRootNode() instanceof ShadowRoot && modal.contains((element.getRootNode() as ShadowRoot).host)));
  100 |     const targets = candidates.filter(element => {
  101 |       const root = element.getRootNode();
  102 |       return !(root instanceof ShadowRoot && candidates.includes(root.host))
  103 |         && !candidates.some(ancestor => ancestor !== element && ancestor.tagName.startsWith('FLUENT-') && ancestor.contains(element));
  104 |     });
  105 |     const containsComposed = (owner: Element, child: Element) => { for (let current: Element | null = child; current; current = parent(current)) if (current === owner) return true; return false; };
  106 |     const hit = (x: number, y: number) => {
  107 |       let element = document.elementFromPoint(x, y);
  108 |       while (element?.shadowRoot) { const inner = element.shadowRoot.elementFromPoint(x, y); if (!inner || inner === element) break; element = inner; }
  109 |       return element;
  110 |     };
  111 |     const paintBounds = all.map(element => ({ element, box: element.getBoundingClientRect() }));
  112 |     const pointerArea = (element: Element, box: PaintBox, fresh = false): PaintBox => {
  113 |       const xs = new Set([box.left, box.right]), ys = new Set([box.top, box.bottom]);
  114 |       for (const candidate of paintBounds) {
  115 |         if (containsComposed(element, candidate.element) || containsComposed(candidate.element, element)) continue;
  116 |         const css = getComputedStyle(candidate.element), rect = fresh ? candidate.element.getBoundingClientRect() : candidate.box;
  117 |         if (css.pointerEvents === 'none' || css.visibility === 'hidden' || css.display === 'none' || rect.left >= box.right || rect.right <= box.left || rect.top >= box.bottom || rect.bottom <= box.top) continue;
  118 |         const clip = audit.clip(css, rect);
  119 |         if (clip.kind === 'unsupported') throw new Error('Clipped pointer occlusion needs pixel evidence');
  120 |         if (clip.kind === 'hidden') continue;
  121 |         const painted = clip.box ?? rect;
  122 |         if (painted.left >= box.right || painted.right <= box.left || painted.top >= box.bottom || painted.bottom <= box.top) continue;
  123 |         xs.add(Math.max(box.left, painted.left)); xs.add(Math.min(box.right, painted.right));
  124 |         ys.add(Math.max(box.top, painted.top)); ys.add(Math.min(box.bottom, painted.bottom));
  125 |       }
  126 |       const x = [...xs].sort((a,b) => a-b), y = [...ys].sort((a,b) => a-b);
  127 |       const cells = y.slice(1).map((bottom, row) => x.slice(1).map((right, col) => { const target = hit((x[col] + right) / 2, (y[row] + bottom) / 2); return target !== null && containsComposed(element, target); }));
  128 |       let best = { left: box.left, right: box.left, top: box.top, bottom: box.top }, score = -1;
  129 |       for (let first = 0; first < x.length - 1; first++) for (let last = first + 1; last < x.length; last++) {
  130 |         let start = 0;
  131 |         for (let row = 0; row < cells.length; row++) {
  132 |           if (!cells[row].slice(first, last).every(Boolean)) { start = row + 1; continue; }
  133 |           const width = x[last] - x[first], height = y[row + 1] - y[start];
  134 |           const rank = (width >= 24 && height >= 24 ? 1e12 : 0) + width * height;
  135 |           if (rank > score) { score = rank; best = { left: x[first], right: x[last], top: y[start], bottom: y[row + 1] }; }
  136 |         }
  137 |       }
  138 |       return best;
  139 |     };
  140 |     const reachable = (element: Element, probeScroll = true): { rect: PaintBox & { width: number; height: number }; limitation: string | null; clipped: boolean } => {
  141 |       const original = element.getBoundingClientRect();
  142 |       let box: PaintBox = { left: original.left, top: original.top, right: original.right, bottom: original.bottom };
  143 |       // Ordinary page/scrollport content can be brought into view. Viewport clipping
  144 |       // constrains fixed chrome, while overflow:hidden/clip constrains every target.
  145 |       let fixed = false;
  146 |       for (let current: Element | null = element; current; current = parent(current)) if (getComputedStyle(current).position === 'fixed') fixed = true;
  147 |       let limitation: string | null = null;
  148 |       for (let current: Element | null = element; current; current = parent(current)) {
  149 |         const css = getComputedStyle(current), bounds = current.getBoundingClientRect();
  150 |         const clip = audit.clip(css, bounds);
  151 |         if (clip.kind === 'unsupported') limitation = 'Target clip/mask needs pixel evidence';
  152 |         if (clip.kind === 'hidden') box.right = box.left;
  153 |         if (clip.box) box = { left: Math.max(box.left, clip.box.left), top: Math.max(box.top, clip.box.top), right: Math.min(box.right, clip.box.right), bottom: Math.min(box.bottom, clip.box.bottom) };
  154 |         if (current !== element && !['inline','contents'].includes(css.display)) {
  155 |           const left = bounds.left + parseFloat(css.borderLeftWidth), right = bounds.right - parseFloat(css.borderRightWidth);
  156 |           const top = bounds.top + parseFloat(css.borderTopWidth), bottom = bounds.bottom - parseFloat(css.borderBottomWidth);
  157 |           if (/(hidden|clip)/u.test(css.overflowX)) { box.left = Math.max(box.left, left); box.right = Math.min(box.right, right); }
  158 |           else if (/(auto|scroll)/u.test(css.overflowX)) {
  159 |             const width = Math.min(Math.max(0, box.right - box.left), Math.max(0, right - left));
  160 |             box.left = Math.max(left, Math.min(box.left, right - width)); box.right = box.left + width;
  161 |           }
  162 |           if (/(hidden|clip)/u.test(css.overflowY)) { box.top = Math.max(box.top, top); box.bottom = Math.min(box.bottom, bottom); }
  163 |           else if (/(auto|scroll)/u.test(css.overflowY)) {
  164 |             const height = Math.min(Math.max(0, box.bottom - box.top), Math.max(0, bottom - top));
  165 |             box.top = Math.max(top, Math.min(box.top, bottom - height)); box.bottom = box.top + height;
  166 |           }
  167 |         }
  168 |         if (current.matches(':popover-open,:modal')) break;
  169 |       }
  170 |       if (fixed) box = { left: Math.max(0, box.left), top: Math.max(0, box.top), right: Math.min(innerWidth, box.right), bottom: Math.min(innerHeight, box.bottom) };
  171 |       // Offscreen scrollable content remains reachable by scrolling. Inspect actual
  172 |       // pointer paint only while its measured rectangle is currently on screen.
  173 |       if (box.left >= 0 && box.top >= 0 && box.right <= innerWidth && box.bottom <= innerHeight
  174 |         && Math.abs(box.left - original.left) < .5 && Math.abs(box.top - original.top) < .5 && box.right > box.left && box.bottom > box.top) box = pointerArea(element, box, !probeScroll);
  175 |       const outsideViewport = !fixed && (box.left < 0 || box.top < 0 || box.right > innerWidth || box.bottom > innerHeight);
```