# Instructions

- Following Playwright test failed.
- Explain why, be concise, respect Playwright best practices.
- Provide a snippet of code with the fix, if possible.

# Test info

- Name: responsive-accessibility.spec.ts >> Story 13.6 responsive operator surfaces >> data-formatting: 320 CSS-pixel reflow
- Location: specs/responsive-accessibility.spec.ts:361:7

# Error details

```
Error: AE-05 focused visual geometry

AE-05 focused visual geometry

expect(received).toEqual(expected) // deep equality

- Expected  - 1
+ Received  + 3

- Array []
+ Array [
+   "app content obscures target or focus ring: {\"target\":\"A#\",\"requireIndicator\":true,\"box\":{\"left\":56.3125,\"top\":589.890625,\"right\":278.765625,\"bottom\":619.890625},\"ring\":3,\"failures\":[\"app content obscures target or focus ring\"],\"occluder\":\"A# by SPAN# at 79.290283203125,619.640625\"}",
+ ]

Call Log:
- Timeout 10000ms exceeded while waiting on the predicate
```

# Page snapshot

```yaml
- generic [ref=e2]:
  - link "Skip to content" [ref=e3]:
    - /url: /__frontcomposer/specimens/data-formatting?culture=fr&ui-culture=en#fc-main-content
  - link "Skip to navigation" [ref=e4]:
    - /url: /__frontcomposer/specimens/data-formatting?culture=fr&ui-culture=en#fc-nav
  - generic [ref=e6]:
    - banner [ref=e7]:
      - generic [ref=e8]:
        - generic [ref=e9]:
          - generic [ref=e10]:
            - button "Toggle navigation" [ref=e12] [cursor=pointer]
            - generic:
              - generic:
                - generic:
                  - generic:
                    - button "Close"
          - generic [ref=e16]: Hexalith FrontComposer
        - generic [ref=e17]:
          - button "Change theme" [ref=e19] [cursor=pointer]:
            - generic [ref=e20]:
              - generic:
                - generic [ref=e21]: "Change theme: Light"
                - generic [aria-hidden] [ref=e22]: Light
          - button "Open command palette" [ref=e26] [cursor=pointer]
          - button "Open settings" [ref=e30] [cursor=pointer]
          - button "FU" [ref=e35] [cursor=pointer]
    - generic [ref=e37]:
      - status [ref=e38]
      - main [ref=e39]:
        - status [ref=e40]
        - status [ref=e41]
        - main [ref=e43]:
          - generic [ref=e44]:
            - paragraph [ref=e45]: FrontComposer accessibility specimen
            - heading "Data-formatting specimen" [level=1] [ref=e46]
            - paragraph [ref=e47]: Frozen fixture data rendered with a fixed non-default culture and UTC time base.
          - region [ref=e48]:
            - heading "Projection formatting DataGrid" [level=2] [ref=e49]
            - table [ref=e50]:
              - caption [ref=e51]: One deterministic row per generated formatting rule
              - rowgroup [ref=e52]:
                - row [ref=e53]:
                  - columnheader "Formatting class" [ref=e54]
                  - columnheader "Generated label" [ref=e55]
                  - columnheader "Rendered value" [ref=e56]
                  - columnheader "Accessible name" [ref=e57]
              - rowgroup [ref=e58]:
                - row [ref=e59]:
                  - rowheader "Locale number" [ref=e60]
                  - cell "Total Orders" [ref=e61]
                  - 'cell "Total Orders: 12 345,67" [ref=e62]': 12 345,67
                  - 'cell "Total Orders: 12 345,67" [ref=e63]'
                - row [ref=e64]:
                  - rowheader "Absolute timestamp" [ref=e65]
                  - cell "Submitted At" [ref=e66]
                  - 'cell "Submitted At: 15/01/2026 13:45 UTC" [ref=e67]': 15/01/2026 13:45 UTC
                  - 'cell "Submitted At: 15/01/2026 13:45 UTC" [ref=e68]'
                - row [ref=e69]:
                  - rowheader "Relative timestamp" [ref=e70]
                  - cell "Last Sync" [ref=e71]
                  - 'cell "Last Sync: 3 hours ago" [ref=e72]': 3 hours ago
                  - 'cell "Last Sync: 3 hours ago" [ref=e73]'
                - row [ref=e74]:
                  - rowheader "Truncated identifier" [ref=e75]
                  - cell "Correlation Id" [ref=e76]
                  - 'cell "Correlation Id: fc-correlation-0002..." [ref=e77]': fc-correlation-0002...
                  - 'cell "Correlation Id: fc-correlation-0002..." [ref=e78]'
                - row [ref=e79]:
                  - rowheader "Null value" [ref=e80]
                  - cell "Optional Comment" [ref=e81]
                  - 'cell "Optional Comment: no value" [ref=e82]': —
                  - 'cell "Optional Comment: no value" [ref=e83]'
                - row [ref=e84]:
                  - rowheader "Collection count" [ref=e85]
                  - cell "Approvers" [ref=e86]
                  - 'cell "Approvers: 3 items" [ref=e87]': 3 items
                  - 'cell "Approvers: 3 items" [ref=e88]'
                - row [ref=e89]:
                  - rowheader "Currency" [ref=e90]
                  - cell "Budget" [ref=e91]
                  - 'cell "Budget: 1 234,50 EUR" [ref=e92]': 1 234,50 EUR
                  - 'cell "Budget: 1 234,50 EUR" [ref=e93]'
                - row [ref=e94]:
                  - rowheader "Boolean" [ref=e95]
                  - cell "Is Active" [ref=e96]
                  - 'cell "Is Active: Yes" [ref=e97]': "Yes"
                  - 'cell "Is Active: Yes" [ref=e98]'
                - row [ref=e99]:
                  - rowheader "Truncated enum" [ref=e100]
                  - cell "Lifecycle State" [ref=e101]
                  - 'cell "Lifecycle State: Awaiting Manual Review" [ref=e102]': Awaiting Manual Re...
                  - 'cell "Lifecycle State: Awaiting Manual Review" [ref=e103]'
                - row [ref=e104]:
                  - rowheader "Unsupported field" [ref=e105]
                  - cell "Opaque Payload" [ref=e106]
                  - 'cell "Opaque Payload: Unsupported field" [ref=e107]': Unsupported field
                  - 'cell "Opaque Payload: Unsupported field" [ref=e108]'
          - region [ref=e109]:
            - heading "Generated formatting DataGrid" [level=2] [ref=e110]
            - status [ref=e111]
            - paragraph [ref=e112]: 1 specimen formattings
            - text: 1 specimen formattings
            - region "specimen formattings" [ref=e113]:
              - table [ref=e114]:
                - rowgroup [ref=e115]:
                  - row [ref=e116]:
                    - cell [ref=e117]:
                      - generic [ref=e118]:
                        - text: 1111111112 345,6715/01/20263h ago—
                        - generic "Approvers with unsupported type System.String[] requires a custom renderer" [ref=e120]:
                          - strong [ref=e121]: Approvers
                          - text: (System.String[]) This field requires a custom renderer.
                          - link "Learn how to customize this field" [active] [ref=e122]:
                            - /url: https://hexalith.github.io/FrontComposer/diagnostics/HFC1002
                        - text: 1 234,50 €YesAwaiting Manual Review And Extended Governance Approval
                        - generic "OpaquePayload with unsupported type System.Collections.Generic.Dictionary requires a custom renderer" [ref=e124]:
                          - strong [ref=e125]: OpaquePayload
                          - text: (System.Collections.Generic.Dictionary) This field requires a custom renderer.
                          - link "Learn how to customize this field" [ref=e126]:
                            - /url: https://hexalith.github.io/FrontComposer/diagnostics/HFC1002
              - region "Détails pour"
    - generic [ref=e127]: Hexalith FrontComposer © 2026
```

# Test source

```ts
  346 |       const left = clip.left + ancestor.clientLeft, top = clip.top + ancestor.clientTop;
  347 |       // A wrapper clips its descendants, but its own outline is painted outside its overflow box.
  348 |       const clipped = indicatorOwners.has(ancestor) ? rect : box;
  349 |       if (/(auto|scroll|hidden|clip)/u.test(css.overflowX) && (clipped.left < left - .5 || clipped.right > left + ancestor.clientWidth + .5)) failures.push('horizontal scrollport clips target or ring');
  350 |       if (/(auto|scroll|hidden|clip)/u.test(css.overflowY) && (clipped.top < top - .5 || clipped.bottom > top + ancestor.clientHeight + .5)) failures.push('vertical scrollport clips target or ring');
  351 |       if (ancestor.matches(':popover-open,:modal')) break;
  352 |     }
  353 |     const contains = (ancestor: Element, child: Element) => {
  354 |       for (let current: Element | null = child; current; current = parent(current)) if (current === ancestor) return true;
  355 |       return false;
  356 |     };
  357 |     const ownsDropdownIndicator = (candidate: Element) => {
  358 |       // Fluent's light-DOM combobox and fallback SVG occupy sibling shadow slots.
  359 |       // Their common host owns the passive indicator, but not arbitrary overlapping siblings.
  360 |       const host = element.matches('button[slot="control"][role="combobox"]') ? element.parentElement : null;
  361 |       if (!host?.matches('fluent-dropdown') || element.assignedSlot?.getRootNode() !== host.shadowRoot) return false;
  362 |       const icon = candidate.closest('svg[aria-hidden="true"]');
  363 |       if (!icon || icon.matches('[tabindex],a[href]') || icon.querySelector('[tabindex],a[href]')) return false;
  364 |       const bounds = icon.getBoundingClientRect(), control = host.getBoundingClientRect();
  365 |       if (bounds.left < control.left || bounds.top < control.top || bounds.right > control.right || bounds.bottom > control.bottom) return false;
  366 |       for (let owner: Element | null = icon; owner && owner !== host; owner = parent(owner)) {
  367 |         if (owner instanceof HTMLSlotElement && owner.name === 'indicator' && owner.getRootNode() === host.shadowRoot) return true;
  368 |       }
  369 |       return false;
  370 |     };
  371 |     const related = (candidate: Element) => contains(candidate, element) || contains(element, candidate) || ownsDropdownIndicator(candidate);
  372 |     // Test every overlapping app box at its intersection center. This detects even a small
  373 |     // off-center obstruction missed by center-only or in-viewport assertions.
  374 |     const candidates: Element[] = [];
  375 |     const visitOccluders = (root: Document | ShadowRoot) => {
  376 |       for (const candidate of root.querySelectorAll('*')) {
  377 |         candidates.push(candidate);
  378 |         if (candidate.shadowRoot) visitOccluders(candidate.shadowRoot);
  379 |       }
  380 |     };
  381 |     visitOccluders(document);
  382 |     const deepestHit = (x: number, y: number) => {
  383 |       let hit = document.elementFromPoint(x, y);
  384 |       while (hit?.shadowRoot) {
  385 |         const inner = hit.shadowRoot.elementFromPoint(x, y);
  386 |         if (!inner || inner === hit) break;
  387 |         hit = inner;
  388 |       }
  389 |       return hit;
  390 |     };
  391 |     for (const candidate of candidates) {
  392 |       if (related(candidate)) continue;
  393 |       const css = getComputedStyle(candidate), other = candidate.getBoundingClientRect();
  394 |       if (css.visibility === 'hidden' || css.display === 'none' || Number(css.opacity) === 0 || other.width === 0 || other.height === 0) continue;
  395 |       const left = Math.max(box.left, other.left), right = Math.min(box.right, other.right);
  396 |       const top = Math.max(box.top, other.top), bottom = Math.min(box.bottom, other.bottom);
  397 |       if (left >= right || top >= bottom) continue;
  398 |       const x = (left + right) / 2, y = (top + bottom) / 2;
  399 |       let hit: Element | null;
  400 |       const probe = candidate as HTMLElement;
  401 |       const originalStyle = probe.getAttribute('style');
  402 |       const pointerOptOut = css.pointerEvents === 'none';
  403 |       try {
  404 |         if (pointerOptOut) probe.style.setProperty('pointer-events', 'auto', 'important');
  405 |         hit = deepestHit(x, y);
  406 |       } finally {
  407 |         if (pointerOptOut) { if (originalStyle === null) probe.removeAttribute('style'); else probe.setAttribute('style', originalStyle); }
  408 |       }
  409 |       const insideTarget = x > rect.left + .5 && x < rect.right - .5 && y > rect.top + .5 && y < rect.bottom - .5;
  410 |       const paintsAtPoint = (hit: Element) => {
  411 |         const css = getComputedStyle(hit);
  412 |         const alpha = audit.parse(css.backgroundColor)[3];
  413 |         if (alpha > 0 || css.backgroundImage !== 'none' || hit instanceof SVGGraphicsElement || hit.matches('img,video,canvas')) return true;
  414 |         return [...hit.childNodes].filter(node => node.nodeType === Node.TEXT_NODE && node.textContent?.trim()).some(node => {
  415 |           const range = document.createRange(); range.selectNodeContents(node);
  416 |           return [...range.getClientRects()].some(text => x >= text.left && x <= text.right && y >= text.top && y <= text.bottom);
  417 |         });
  418 |       };
  419 |       const paintsAbove = (a: Element, b: Element) => {
  420 |         // Compare the first divergent stacking contexts, never maxima from unrelated levels.
  421 |         const context = (node: Element) => {
  422 |           const style = getComputedStyle(node);
  423 |           return node === document.documentElement || ['fixed','sticky'].includes(style.position) || style.zIndex !== 'auto' && (style.position !== 'static' || /flex|grid/u.test(getComputedStyle(parent(node) ?? document.documentElement).display)) || style.transform !== 'none' || Number(style.opacity) < 1 || style.isolation === 'isolate' || style.filter !== 'none' || style.perspective !== 'none' || /paint|layout|strict|content/u.test(style.contain) || style.mixBlendMode !== 'normal' || /transform|opacity|filter/u.test(style.willChange);
  424 |         };
  425 |         const chain = (node: Element) => { const nodes: Element[] = []; for (let current: Element | null = node; current; current = parent(current)) if (context(current)) nodes.unshift(current); return nodes; };
  426 |         const left = chain(a), right = chain(b); let index = 0;
  427 |         while (left[index] && left[index] === right[index]) index++;
  428 |         const first = left[index] ?? a, second = right[index] ?? b;
  429 |         const az = Number.parseInt(getComputedStyle(first).zIndex) || 0, bz = Number.parseInt(getComputedStyle(second).zIndex) || 0;
  430 |         if (az !== bz) return az > bz;
  431 |         if (first.getRootNode() !== second.getRootNode()) return null;
  432 |         const phase = (node: Element) => { const style = getComputedStyle(node); return context(node) || style.position !== 'static' ? 3 : /inline/u.test(style.display) ? 2 : style.cssFloat !== 'none' ? 1 : 0; };
  433 |         if (phase(first) !== phase(second)) return phase(first) > phase(second);
  434 |         return Boolean(second.compareDocumentPosition(first) & Node.DOCUMENT_POSITION_FOLLOWING);
  435 |       };
  436 |       // Outlines are painted outside hit boxes. Content behind the outline is not an
  437 |       // obstruction simply because elementFromPoint cannot hit that outline itself.
  438 |       if (hit && !related(hit) && (!pointerOptOut || paintsAtPoint(hit)) && (insideTarget || paintsAtPoint(hit) && paintsAbove(hit, element) !== false)) { occluder = `${element.tagName}#${element.id} by ${hit.tagName}#${hit.id} at ${x},${y}`; failures.push('app content obscures target or focus ring'); break; }
  439 |     }
  440 |     return { target: `${element.tagName}#${element.id}`, requireIndicator, box, ring, failures, occluder };
  441 |   }, requireIndicator);
  442 | }
  443 | 
  444 | export async function expectFocusVisible(target: Locator): Promise<void> {
  445 |   await expect(target).toBeFocused();
> 446 |   await expect.poll(async () => { const result = await focusGeometry(target); return result.failures.map(failure => `${failure}: ${JSON.stringify(result)}`); }, { message: 'AE-05 focused visual geometry' }).toEqual([]);
      |                                                                                                                                                                                                                ^ Error: AE-05 focused visual geometry
  447 | }
  448 | 
  449 | export async function expectTextContrast(target: Locator, minimum?: number, colorProperty = 'color') {
  450 |   const sample = await target.evaluate((element, property) => {
  451 |     const parse = (value: string) => {
  452 |       const channels = value.match(/[\d.]+/gu)?.map(Number) ?? [];
  453 |       if (channels.length < 3) throw new Error('Unsupported computed color');
  454 |       return [channels[0], channels[1], channels[2], channels[3] ?? 1];
  455 |     };
  456 |     const composite = (fg: number[], bg: number[]) => [0, 1, 2].map(i => fg[i] * fg[3] + bg[i] * (1 - fg[3])).concat(1);
  457 |     const backgrounds: number[][] = [];
  458 |     for (let current: Element | null = element; current; current = current.parentElement) backgrounds.push(parse(getComputedStyle(current).backgroundColor));
  459 |     let background = [255, 255, 255, 1];
  460 |     for (const layer of backgrounds.reverse()) background = composite(layer, background);
  461 |     const css = getComputedStyle(element), foreground = composite(parse(css.getPropertyValue(property)), background);
  462 |     const luminance = (color: number[]) => color.slice(0, 3).map(channel => { const c = channel / 255; return c <= .04045 ? c / 12.92 : ((c + .055) / 1.055) ** 2.4; }).reduce((sum, c, i) => sum + c * [.2126, .7152, .0722][i], 0);
  463 |     const a = luminance(foreground), b = luminance(background);
  464 |     const large = Number.parseFloat(css.fontSize) >= 24 || Number.parseFloat(css.fontSize) >= 18.667 && Number.parseInt(css.fontWeight) >= 700;
  465 |     return { foreground, background, ratio: (Math.max(a, b) + .05) / (Math.min(a, b) + .05), minimum: large ? 3 : 4.5 };
  466 |   }, colorProperty);
  467 |   expect(sample.ratio, JSON.stringify(sample)).toBeGreaterThanOrEqual(minimum ?? sample.minimum);
  468 |   return sample;
  469 | }
  470 | 
  471 | /** Find actual clipped text ranges, including controls inside open shadow roots. */
  472 | export async function expectNoTextClipping(page: Page): Promise<void> {
  473 |   const failures = await page.evaluate(() => {
  474 |     const failures: string[] = [];
  475 |     const visit = (root: Document | ShadowRoot) => {
  476 |       for (const element of root.querySelectorAll('*')) {
  477 |         if (element.shadowRoot) visit(element.shadowRoot);
  478 |         if (element.closest('.fc-sr-only,[aria-hidden="true"],[hidden]') || element.getClientRects().length === 0) continue;
  479 |         const css = getComputedStyle(element);
  480 |         if (css.visibility === 'hidden' || css.clip !== 'auto' || css.clipPath !== 'none') continue;
  481 |         if (!/(hidden|clip)/u.test(css.overflowX + css.overflowY)) continue;
  482 |         const box = element.getBoundingClientRect();
  483 |         if (box.width < 2 || box.height < 2) continue;
  484 |         const walker = document.createTreeWalker(element, NodeFilter.SHOW_TEXT);
  485 |         while (walker.nextNode()) {
  486 |           const node = walker.currentNode;
  487 |           if (!node.textContent?.trim() || node.parentElement?.closest('style,script,[hidden],.fc-sr-only,.fc-skip-link:not(:focus),[aria-hidden="true"]') || node.parentElement && getComputedStyle(node.parentElement).visibility === 'hidden') continue;
  488 |           const range = document.createRange(); range.selectNodeContents(node);
  489 |           // Native popovers/dialogs paint in the top layer and escape outer overflow
  490 |           // clips. Their own clipping boxes are still evaluated independently.
  491 |           let escapesOwner = false;
  492 |           for (let parent = node.parentElement; parent && parent !== element; parent = parent.parentElement) {
  493 |             if (parent.matches(':popover-open,:modal')) { escapesOwner = true; break; }
  494 |           }
  495 |           if (escapesOwner) continue;
  496 |           if ([...range.getClientRects()].some(text => {
  497 |             let left = text.left, right = text.right, top = text.top, bottom = text.bottom;
  498 |             // A nested scrollport owns its offscreen text; evaluate only the visible portion at
  499 |             // this ancestor. Each hidden/clip owner is independently checked by the outer loop.
  500 |             for (let parent = node.parentElement; parent && parent !== element; parent = parent.parentElement) {
  501 |               const style = getComputedStyle(parent), clip = parent.getBoundingClientRect();
  502 |               if (/(auto|scroll|hidden|clip)/u.test(style.overflowX)) { left = Math.max(left, clip.left); right = Math.min(right, clip.right); }
  503 |               if (/(auto|scroll|hidden|clip)/u.test(style.overflowY)) { top = Math.max(top, clip.top); bottom = Math.min(bottom, clip.bottom); }
  504 |             }
  505 |             return right > left && bottom > top && (
  506 |               /(hidden|clip)/u.test(css.overflowX) && (left < box.left - 1 || right > box.right + 1)
  507 |               || /(hidden|clip)/u.test(css.overflowY) && (top < box.top - 1 || bottom > box.bottom + 1));
  508 |           })) {
  509 |             failures.push(element.id ? `#${element.id}` : `${element.tagName.toLowerCase()}.${String(element.className).trim().replace(/\s+/gu, '.')}`); break;
  510 |           }
  511 |         }
  512 |       }
  513 |     };
  514 |     visit(document);
  515 |     return [...new Set(failures)];
  516 |   });
  517 |   expect(failures, 'AE-03 clipped text ranges').toEqual([]);
  518 | }
  519 | 
  520 | export type ContrastKind = 'text' | 'border' | 'icon' | 'focus' | 'cue';
  521 | 
  522 | /** Sample actual painted nodes in the composed tree; empty/unsupported samples fail closed. */
  523 | export async function measureSurfaceContrast(root: Locator, kind: ContrastKind = 'text') {
  524 |   await preparePaintAudit(root);
  525 |   return root.evaluate((scope, sampleKind) => {
  526 |     const parent = (node: Element): Element | null => node.assignedSlot ?? node.parentElement ?? (node.getRootNode() instanceof ShadowRoot ? (node.getRootNode() as ShadowRoot).host : null);
  527 |     const audit = (window as typeof window & { __aePaintAudit: PaintAudit }).__aePaintAudit;
  528 |     const parse = audit.parse;
  529 |     const resting = sampleKind === 'focus' ? audit.resting(scope) : new Map();
  530 |     const composite = (fg: number[], bg: number[]) => [0, 1, 2].map(i => fg[i] * fg[3] + bg[i] * (1 - fg[3])).concat(1);
  531 |     const background = (element: Element | null) => {
  532 |       const layers: number[][] = [];
  533 |       for (let current = element; current; current = parent(current)) {
  534 |         const css = getComputedStyle(current);
  535 |         if (css.backgroundImage !== 'none') throw new Error('Background image needs pixel evidence');
  536 |         if (Number(css.opacity) > 0 && Number(css.opacity) < 1) throw new Error(`Translucent group needs pixel evidence: ${current.tagName}.${current.className} opacity=${css.opacity}`);
  537 |         layers.push(parse(css.backgroundColor));
  538 |       }
  539 |       return layers.reverse().reduce((bg, fg) => composite(fg, bg), [255, 255, 255, 1]);
  540 |     };
  541 |     const luminance = (color: number[]) => color.slice(0, 3).map(channel => { const c = channel / 255; return c <= .04045 ? c / 12.92 : ((c + .055) / 1.055) ** 2.4; }).reduce((sum, c, i) => sum + c * [.2126, .7152, .0722][i], 0);
  542 |     const visible = (element: Element) => {
  543 |       if (element.getClientRects().length === 0 && getComputedStyle(element).display !== 'contents') return false;
  544 |       for (let current: Element | null = element; current; current = parent(current)) {
  545 |         const css = getComputedStyle(current);
  546 |         if (sampleKind === 'icon' && current.matches('fluent-skeleton,.fluent-skeleton,.fc-empty-state-body__icon')) return false;
```