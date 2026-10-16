# Instructions

- Following Playwright test failed.
- Explain why, be concise, respect Playwright best practices.
- Provide a snippet of code with the fix, if possible.

# Test info

- Name: accessibility-state-matrix.spec.ts >> AE complete state matrix light >> generated zero-field policy: safe feedback, focus, owner, next attempt
- Location: specs/accessibility-state-matrix.spec.ts:105:7

# Error details

```
Error: locator.evaluate: Target page, context or browser has been closed
```

# Test source

```ts
  19  |     const visit = (root: Document | ShadowRoot) => {
  20  |       if (!roots.has(root)) { roots.add(root); observer.observe(root instanceof Document ? root.documentElement : root, { childList: true, subtree: true }); }
  21  |       if (!root.querySelector('[data-ae-text-spacing]')) {
  22  |         const style = document.createElement('style'); style.dataset.aeTextSpacing = 'true'; style.textContent = css;
  23  |         (root instanceof Document ? root.head : root).append(style);
  24  |       }
  25  |       root.querySelectorAll('*').forEach(element => { if (element.shadowRoot) visit(element.shadowRoot); });
  26  |     };
  27  |     const observer = new MutationObserver(() => { for (const root of roots) visit(root); });
  28  |     const original = Element.prototype.attachShadow;
  29  |     const patched = function (this: Element, init: ShadowRootInit) { const root = original.call(this, init); visit(root); return root; };
  30  |     Element.prototype.attachShadow = patched;
  31  |     owner.aeSpacingCleanup = () => {
  32  |       observer.disconnect();
  33  |       if (Element.prototype.attachShadow === patched) Element.prototype.attachShadow = original;
  34  |       for (const root of roots) root.querySelectorAll('[data-ae-text-spacing]').forEach(style => style.remove());
  35  |       delete owner.aeSpacingCleanup;
  36  |     };
  37  |     visit(document);
  38  |   });
  39  | }
  40  | 
  41  | /** Remove the overrides and their observer/prototype seam before a restoration round trip. */
  42  | export async function removeTextSpacing(page: Page): Promise<void> {
  43  |   await page.evaluate(() => (window as unknown as { aeSpacingCleanup?: () => void }).aeSpacingCleanup?.());
  44  | }
  45  | 
  46  | export async function expectNoPageOverflow(page: Page): Promise<void> {
  47  |   const geometry = await page.evaluate(() => {
  48  |     const root = document.scrollingElement ?? document.documentElement;
  49  |     return { width: root.clientWidth, scrollWidth: root.scrollWidth };
  50  |   });
  51  |   expect(geometry.scrollWidth, `Page overflow: ${JSON.stringify(geometry)}`).toBeLessThanOrEqual(geometry.width + 1);
  52  | }
  53  | 
  54  | /** Inventory actual hit targets, collapsing a Fluent control and its shadow control to one target. */
  55  | export async function inventoryTargets(page: Page, exceptions: TargetException[] = []) {
  56  |   return page.evaluate((records) => {
  57  |     const selector = 'a[href],button,input:not([type="hidden"]),select,textarea,summary,[role="button"],[role="tab"],[role="option"],[role="menuitem"],[role="checkbox"],[role="radio"],[role="switch"],fluent-button,fluent-text-input,fluent-number-input,fluent-select,fluent-combobox,fluent-dropdown,fluent-radio,fluent-switch,[tabindex]:not([tabindex="-1"]),[onclick]';
  58  |     const all: Element[] = [];
  59  |     const visit = (root: Document | ShadowRoot) => {
  60  |       root.querySelectorAll('*').forEach(element => { all.push(element); if (element.shadowRoot) visit(element.shadowRoot); });
  61  |     };
  62  |     visit(document);
  63  |     const visible = (element: Element) => {
  64  |       const rect = element.getBoundingClientRect();
  65  |       const style = getComputedStyle(element);
  66  |       return rect.width > 0 && rect.height > 0 && style.visibility !== 'hidden' && style.display !== 'none'
  67  |         && !element.closest('[inert]') && !element.closest('[aria-hidden="true"]');
  68  |     };
  69  |     const modalDialog = all.find(element => element.matches('dialog[open]'));
  70  |     const modalRoot = modalDialog?.getRootNode();
  71  |     const modal = modalRoot instanceof ShadowRoot ? modalRoot.host : modalDialog;
  72  |     const candidates = all.filter(element => element.matches(selector) && visible(element)
  73  |       && (!modal || element === modal || modal.contains(element) || element.getRootNode() instanceof ShadowRoot && modal.contains((element.getRootNode() as ShadowRoot).host)));
  74  |     const targets = candidates.filter(element => {
  75  |       const root = element.getRootNode();
  76  |       return !(root instanceof ShadowRoot && candidates.includes(root.host))
  77  |         && !candidates.some(ancestor => ancestor !== element && ancestor.tagName.startsWith('FLUENT-') && ancestor.contains(element));
  78  |     });
  79  |     const boxes = targets.map(element => ({ element, rect: element.getBoundingClientRect() }));
  80  |     const intersects = (a: DOMRect, b: DOMRect) => a.left < b.right && b.left < a.right && a.top < b.bottom && b.top < a.bottom;
  81  |     const circleIntersectsRect = (x: number, y: number, rect: DOMRect) => Math.hypot(x - Math.max(rect.left, Math.min(x, rect.right)), y - Math.max(rect.top, Math.min(y, rect.bottom))) < 12 - .01;
  82  |     const used = new Set<string>();
  83  |     const inventory = boxes.map(({ element, rect }, index) => {
  84  |       const identifier = element.id ? `#${element.id}` : element.getAttribute('data-testid') ? `[data-testid="${element.getAttribute('data-testid')}"]` : `${element.tagName.toLowerCase()}[inventory-index="${index}"]`;
  85  |       const record = records.find(item => element.matches(item.selector));
  86  |       let failure: string | null = null;
  87  |       const conforming = rect.width >= 24 && rect.height >= 24;
  88  |       if (record) used.add(record.selector);
  89  |       if (!conforming) {
  90  |         if (!record || !record.rationale.trim() || !record.keyboardPath.trim() || !record.evidence.trim()) failure = 'Undersized target lacks an evidenced exception';
  91  |         else if (record.kind === 'Spacing') {
  92  |           const x = rect.left + rect.width / 2, y = rect.top + rect.height / 2;
  93  |           if (boxes.some(other => other.element !== element && (circleIntersectsRect(x, y, other.rect)
  94  |             || (other.rect.width < 24 || other.rect.height < 24) && Math.hypot(x - other.rect.left - other.rect.width / 2, y - other.rect.top - other.rect.height / 2) < 24 - .01))) failure = '24px spacing circle intersects a target or neighboring undersized circle';
  95  |         } else if (record.kind === 'Equivalent') {
  96  |           const equivalent = record.equivalentSelector ? document.querySelector(record.equivalentSelector) : null;
  97  |           const box = equivalent?.getBoundingClientRect();
  98  |           if (!equivalent || equivalent === element || !visible(equivalent) || !box || box.width < 24 || box.height < 24 || intersects(rect, box)) failure = 'Equivalent control is absent, identical, overlapping, or undersized';
  99  |         } else if (record.kind === 'Inline') {
  100 |           if (!element.closest('p,li,dd') || !['inline', 'inline-block'].includes(getComputedStyle(element).display) || element.matches('button,[role="button"],fluent-button')) failure = 'Inline exception does not apply to a control outside running text';
  101 |         } else if (record.kind === 'User Agent Control' && !element.matches('input,select,textarea')) failure = 'User agent exception requires a native control';
  102 |       }
  103 |       return { target: identifier, width: rect.width, height: rect.height, conforming, exception: record ?? null, failure };
  104 |     });
  105 |     return { inventory, unusedExceptions: records.filter(record => !used.has(record.selector)).map(record => record.selector) };
  106 |   }, exceptions);
  107 | }
  108 | 
  109 | export async function expectTargetSizes(page: Page, exceptions: TargetException[] = []) {
  110 |   const result = await inventoryTargets(page, exceptions);
  111 |   expect(result.inventory.length, 'Pointer-target inventory must be nonempty').toBeGreaterThan(0);
  112 |   expect(result.unusedExceptions, 'Stale or unmatched exception selectors').toEqual([]);
  113 |   expect(result.inventory.filter(target => target.failure), 'AE-04 target-size failures').toEqual([]);
  114 |   return result.inventory;
  115 | }
  116 | 
  117 | /** Check the whole rectangle and outline against viewport, clipping ancestors, and occluding boxes. */
  118 | export async function focusGeometry(target: Locator) {
> 119 |   return target.evaluate(element => {
      |                 ^ Error: locator.evaluate: Target page, context or browser has been closed
  120 |     const rect = element.getBoundingClientRect();
  121 |     const composedParent = (node: Element): Element | null => node.assignedSlot ?? node.parentElement ?? (node.getRootNode() instanceof ShadowRoot ? (node.getRootNode() as ShadowRoot).host : null);
  122 |     const visuals: Element[] = [element];
  123 |     // Fluent paints focus on internal wrappers/pseudo-elements as well as its active input.
  124 |     const visitFocus = (root: ShadowRoot) => {
  125 |       for (const child of root.querySelectorAll('*')) {
  126 |         if (child.matches(':focus-within')) visuals.push(child);
  127 |         if (child.shadowRoot?.activeElement) visitFocus(child.shadowRoot);
  128 |       }
  129 |     };
  130 |     for (let owner: Element | null = element; owner; owner = composedParent(owner)) {
  131 |       if ((owner === element || owner.matches('fluent-text-input,fluent-number-input,fluent-dropdown,fluent-combobox,fluent-select,fluent-button,fluent-anchor-button,fluent-switch,fluent-radio')) && owner.shadowRoot && owner.matches(':focus-within')) {
  132 |         if (!visuals.includes(owner)) visuals.push(owner);
  133 |         visitFocus(owner.shadowRoot);
  134 |       }
  135 |     }
  136 |     const painted = (value: string) => value !== 'transparent' && !/rgba\([^)]*,\s*0\s*\)$/u.test(value);
  137 |     const outerShadows = (value: string) => value.split(/,(?![^()]*\))/u).flatMap(shadow => {
  138 |       if (shadow === 'none' || /\binset\b/u.test(shadow)) return [];
  139 |       const color = shadow.match(/(?:rgba?|color)\([^)]*\)/u)?.[0];
  140 |       if (color && !painted(color)) return [];
  141 |       const lengths = shadow.replace(/(?:rgba?|color)\([^)]*\)/gu, '').match(/-?[\d.]+px/gu)?.map(Number.parseFloat) ?? [];
  142 |       if (lengths.length < 2) return [];
  143 |       return [{ x: lengths[0], y: lengths[1], blur: lengths[2] ?? 0, extent: Math.max(0, (lengths[3] ?? 0) + (lengths[2] ?? 0) * 1.5) }];
  144 |     });
  145 |     const geometryLimits: string[] = [];
  146 |     const pseudoBox = (visual: Element, css: CSSStyleDeclaration) => {
  147 |       const px = (value: string) => /^-?[\d.]+px$/u.test(value) ? parseFloat(value) : null;
  148 |       if (!['absolute', 'fixed'].includes(css.position)) {
  149 |         geometryLimits.push('flow-positioned pseudo focus paint needs pixel evidence');
  150 |         return visual.getBoundingClientRect();
  151 |       }
  152 |       let block: Element | null = css.position === 'fixed' ? null : visual;
  153 |       if (css.position === 'fixed') {
  154 |         for (let ancestor = composedParent(visual); ancestor; ancestor = composedParent(ancestor)) {
  155 |           if (getComputedStyle(ancestor).transform !== 'none') { geometryLimits.push('fixed pseudo in transformed ancestry needs pixel evidence'); break; }
  156 |         }
  157 |       }
  158 |       while (block && getComputedStyle(block).position === 'static' && getComputedStyle(block).transform === 'none') block = composedParent(block);
  159 |       if (block && getComputedStyle(block).transform !== 'none') {
  160 |         const matrix = new DOMMatrixReadOnly(getComputedStyle(block).transform);
  161 |         if (!matrix.is2D || matrix.a !== 1 || matrix.d !== 1 || matrix.b !== 0 || matrix.c !== 0) geometryLimits.push('transformed pseudo containing block needs pixel evidence');
  162 |       }
  163 |       const rect = block?.getBoundingClientRect();
  164 |       const base = block && rect ? { left: rect.left + block.clientLeft, top: rect.top + block.clientTop, width: block.clientWidth, height: block.clientHeight } : { left: 0, top: 0, width: innerWidth, height: innerHeight };
  165 |       const left = px(css.left), right = px(css.right), top = px(css.top), bottom = px(css.bottom);
  166 |       let width = px(css.width), height = px(css.height);
  167 |       if (width !== null && css.boxSizing !== 'border-box') width += parseFloat(css.paddingLeft) + parseFloat(css.paddingRight) + parseFloat(css.borderLeftWidth) + parseFloat(css.borderRightWidth);
  168 |       if (height !== null && css.boxSizing !== 'border-box') height += parseFloat(css.paddingTop) + parseFloat(css.paddingBottom) + parseFloat(css.borderTopWidth) + parseFloat(css.borderBottomWidth);
  169 |       width ??= left !== null && right !== null ? base.width - left - right - parseFloat(css.marginLeft) - parseFloat(css.marginRight) : null;
  170 |       height ??= top !== null && bottom !== null ? base.height - top - bottom - parseFloat(css.marginTop) - parseFloat(css.marginBottom) : null;
  171 |       if (width === null || height === null || left === null && right === null || top === null && bottom === null) {
  172 |         geometryLimits.push('unresolved pseudo focus bounds need pixel evidence');
  173 |         return visual.getBoundingClientRect();
  174 |       }
  175 |       const x = base.left + (left !== null ? left + parseFloat(css.marginLeft) : base.width - right! - width - parseFloat(css.marginRight));
  176 |       const y = base.top + (top !== null ? top + parseFloat(css.marginTop) : base.height - bottom! - height - parseFloat(css.marginBottom));
  177 |       const matrix = new DOMMatrixReadOnly(css.transform === 'none' ? undefined : css.transform);
  178 |       const origin = css.transformOrigin.split(' ').map((value, axis) => value.endsWith('%') ? parseFloat(value) * (axis === 0 ? width : height) / 100 : parseFloat(value));
  179 |       if (!matrix.is2D || origin.length < 2 || origin.some(value => !Number.isFinite(value))) geometryLimits.push('unsupported pseudo transform needs pixel evidence');
  180 |       const corners = [[0, 0], [width, 0], [0, height], [width, height]].map(([a, b]) => ({ x: x + origin[0] + matrix.a * (a - origin[0]) + matrix.c * (b - origin[1]) + matrix.e, y: y + origin[1] + matrix.b * (a - origin[0]) + matrix.d * (b - origin[1]) + matrix.f }));
  181 |       return { left: Math.min(...corners.map(point => point.x)), top: Math.min(...corners.map(point => point.y)), right: Math.max(...corners.map(point => point.x)), bottom: Math.max(...corners.map(point => point.y)) };
  182 |     };
  183 |     let indicator = false, ring = 0;
  184 |     const indicatorOwners = new Set<Element>();
  185 |     const box = { left: rect.left, top: rect.top, right: rect.right, bottom: rect.bottom };
  186 |     for (const visual of visuals) for (const pseudo of [null, '::before', '::after']) {
  187 |       const css = getComputedStyle(visual, pseudo);
  188 |       if (css.visibility === 'hidden' || css.display === 'none' || Number(css.opacity) === 0 || pseudo && css.content === 'none') continue;
  189 |       if (css.transform !== 'none') { const matrix = new DOMMatrixReadOnly(css.transform); if (matrix.a === 0 || matrix.d === 0) continue; }
  190 |       const outline = parseFloat(css.outlineWidth) > 0 && css.outlineStyle !== 'none' && painted(css.outlineColor);
  191 |       const shadows = outerShadows(css.boxShadow);
  192 |       const shadow = shadows.length > 0;
  193 |       const pseudoBorder = pseudo && ['Top', 'Right', 'Bottom', 'Left'].some(side => parseFloat(css.getPropertyValue(`border-${side.toLowerCase()}-width`)) > 0 && !['none', 'hidden'].includes(css.getPropertyValue(`border-${side.toLowerCase()}-style`)) && painted(css.getPropertyValue(`border-${side.toLowerCase()}-color`)));
  194 |       indicator ||= outline || shadow || Boolean(pseudoBorder);
  195 |       if (outline) ring = Math.max(ring, Math.max(0, parseFloat(css.outlineWidth) + parseFloat(css.outlineOffset)) || 0);
  196 |       if (outline || shadow || pseudoBorder) {
  197 |         indicatorOwners.add(visual);
  198 |         const visualBox = pseudo ? pseudoBox(visual, css) : visual.getBoundingClientRect(), extent = outline ? Math.max(0, parseFloat(css.outlineWidth) + parseFloat(css.outlineOffset)) : 0;
  199 |         box.left = Math.min(box.left, visualBox.left - extent); box.top = Math.min(box.top, visualBox.top - extent);
  200 |         box.right = Math.max(box.right, visualBox.right + extent); box.bottom = Math.max(box.bottom, visualBox.bottom + extent);
  201 |         for (const paint of shadows) {
  202 |           if (paint.blur > 0) geometryLimits.push('blurred focus shadow needs pixel evidence');
  203 |           ring = Math.max(ring, Math.abs(paint.x) + paint.extent, Math.abs(paint.y) + paint.extent);
  204 |           box.left = Math.min(box.left, visualBox.left + paint.x - paint.extent);
  205 |           box.top = Math.min(box.top, visualBox.top + paint.y - paint.extent);
  206 |           box.right = Math.max(box.right, visualBox.right + paint.x + paint.extent);
  207 |           box.bottom = Math.max(box.bottom, visualBox.bottom + paint.y + paint.extent);
  208 |         }
  209 |       }
  210 |     }
  211 |     const failures: string[] = [...new Set(geometryLimits)];
  212 |     if (!indicator) failures.push('focused target has no visible focus indicator');
  213 |     let occluder: string | null = null;
  214 |     if (box.left < -.5 || box.top < -.5 || box.right > innerWidth + .5 || box.bottom > innerHeight + .5) failures.push('viewport clips target or focus ring');
  215 |     const parent = (node: Element): Element | null => node.assignedSlot ?? node.parentElement ?? (node.getRootNode() instanceof ShadowRoot ? (node.getRootNode() as ShadowRoot).host : null);
  216 |     for (let ancestor = element.matches(':popover-open,:modal') ? null : parent(element); ancestor; ancestor = parent(ancestor)) {
  217 |       const css = getComputedStyle(ancestor), clip = ancestor.getBoundingClientRect();
  218 |       const left = clip.left + ancestor.clientLeft, top = clip.top + ancestor.clientTop;
  219 |       // A wrapper clips its descendants, but its own outline is painted outside its overflow box.
```