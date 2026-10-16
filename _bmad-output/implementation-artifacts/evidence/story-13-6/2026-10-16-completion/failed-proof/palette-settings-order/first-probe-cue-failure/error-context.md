# Instructions

- Following Playwright test failed.
- Explain why, be concise, respect Playwright best practices.
- Provide a snippet of code with the fix, if possible.

# Test info

- Name: review-focus-order.temp.spec.ts >> active toolbar, tabs, palette and settings states in light
- Location: specs/review-focus-order.temp.spec.ts:327:3

# Error details

```
Error: Nonempty cue contrast inventory

expect(received).toBeGreaterThan(expected)

Expected: > 0
Received:   0
```

# Page snapshot

```yaml
- generic [ref=e2]:
  - link "Skip to content" [ref=e3] [cursor=pointer]:
    - /url: /__frontcomposer/specimens/page-toolbar#fc-main-content
  - link "Skip to navigation" [ref=e4] [cursor=pointer]:
    - /url: /__frontcomposer/specimens/page-toolbar#fc-nav
  - generic [ref=e6]:
    - banner [ref=e7]:
      - generic [ref=e8]:
        - generic [ref=e9]:
          - generic [ref=e10]:
            - button "Toggle navigation" [ref=e12] [cursor=pointer]
            - generic:
              - generic:
                - generic: Counter
                - generic: Default
                - generic: Specimens
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
      - status [ref=e38]: Density set to Comfortable.
      - main [ref=e39]:
        - status [ref=e40]
        - status [ref=e41]
        - main [ref=e43]:
          - generic [ref=e44]:
            - paragraph [ref=e45]: FrontComposer accessibility specimen
            - heading "Page toolbar specimen" [level=1] [ref=e46]
            - paragraph [ref=e47]: Reusable search, filter, view, action, and tab toolbar rendered with Fluent UI.
          - region [ref=e48]:
            - heading "Toolbar" [level=2] [ref=e49]
            - toolbar "Orders page tools" [ref=e51]:
              - generic "Search orders" [ref=e53]:
                - searchbox "Search orders" [ref=e55]: open
              - button "Order filters" [ref=e56] [cursor=pointer]
              - button "View options" [ref=e59] [cursor=pointer]
              - generic [ref=e64]: Refresh
          - region [ref=e67]:
            - heading "Page tabs" [level=2] [ref=e68]
            - generic "Order sections" [ref=e69]:
              - tablist [ref=e70]:
                - generic:
                  - tab "Summary" [ref=e71] [cursor=pointer]
                  - tab "Activity" [selected] [ref=e73] [cursor=pointer]
                  - tab "Archived" [disabled]
                  - tab "History" [ref=e75] [cursor=pointer]
              - tabpanel "Activity" [ref=e77]:
                - paragraph [ref=e79]: Activity panel content
          - region [ref=e80]:
            - heading "Toolbar state" [level=2] [ref=e81]
            - generic [ref=e82]:
              - generic [ref=e83]: "Current search: open"
              - generic [ref=e84]: "Active tab: activity"
              - generic [ref=e85]: "Refresh count: 0"
    - generic [ref=e86]: Hexalith FrontComposer © 2026
  - alertdialog [ref=e89]:
    - generic [ref=e90]:
      - generic [ref=e91]: Command palette
      - dialog "Command palette" [ref=e95]:
        - generic [ref=e96]:
          - combobox [expanded] [active] [ref=e98]:
            - searchbox [ref=e100]: counter
          - listbox [ref=e102]:
            - heading "Projections" [level=4] [ref=e103]
            - group "Projections" [ref=e104]:
              - option "CounterProjection" [selected] [ref=e105] [cursor=pointer]
            - heading "Commands" [level=4] [ref=e107]
            - group "Commands" [ref=e108]:
              - option "ConfigureCounterCommand" [ref=e109] [cursor=pointer]
          - status [ref=e111]
```

# Test source

```ts
  531 |         const css = getComputedStyle(current);
  532 |         if (sampleKind === 'icon' && current.matches('fluent-skeleton,.fluent-skeleton,.fc-empty-state-body__icon')) return false;
  533 |         if (audit.clip(css, current.getBoundingClientRect()).kind === 'hidden') return false;
  534 |         if (css.visibility === 'hidden' || css.display === 'none' || Number(css.opacity) === 0 || current.matches('[hidden],.fc-sr-only,[disabled],[aria-disabled="true"]')) return false;
  535 |       }
  536 |       return true;
  537 |     };
  538 |     const samples: { owner: number; node: string; paint: string; foreground: number[]; background: number[]; ratio: number; minimum: number }[] = [];
  539 |     const owners = new Map<Element, number>();
  540 |     const controlOwner = (element: Element) => {
  541 |       let nearest: Element | null = null;
  542 |       for (let current: Element | null = element; current; current = parent(current)) {
  543 |         if (current.matches('fluent-button,fluent-text-input,fluent-number-input,fluent-dropdown,fluent-combobox,fluent-select,fluent-radio,fluent-switch')) return current;
  544 |         if (!nearest && current.matches('button,input,select,textarea,a[href],[role="button"],[role="tab"],[role="radio"],[role="option"]')) nearest = current;
  545 |         if (current === scope) break;
  546 |       }
  547 |       return nearest ?? element;
  548 |     };
  549 |     const sample = (element: Element, value: string, label: string, minimum: number, outside = false, alpha = 1, paintBoxes?: DOMRect[]) => {
  550 |       let boxes: PaintBox[] = paintBoxes ?? [element.getBoundingClientRect()];
  551 |       for (let current: Element | null = element; current; current = parent(current)) {
  552 |         const css = getComputedStyle(current);
  553 |         const clipping = audit.clip(css, current.getBoundingClientRect());
  554 |         if (clipping.kind === 'hidden') return;
  555 |         if (clipping.kind === 'unsupported') throw new Error('Clipped contrast paint needs pixel evidence');
  556 |         if (clipping.box) boxes = boxes.map(box => ({ left: Math.max(box.left, clipping.box!.left), right: Math.min(box.right, clipping.box!.right), top: Math.max(box.top, clipping.box!.top), bottom: Math.min(box.bottom, clipping.box!.bottom) }));
  557 |       }
  558 |       if (!boxes.some(box => box.right > box.left && box.bottom > box.top)) return;
  559 |       const color = parse(value); color[3] *= alpha;
  560 |       if (color[3] === 0 && sampleKind !== 'icon') return;
  561 |       const bg = background(outside ? parent(element) : element);
  562 |       const fg = composite(color, bg), a = luminance(fg), b = luminance(bg);
  563 |       const owner = controlOwner(element);
  564 |       if (!owners.has(owner)) owners.set(owner, owners.size);
  565 |       samples.push({ owner: owners.get(owner)!, node: `${element.tagName.toLowerCase()}${element.id ? '#' + element.id : ''}`, paint: label, foreground: fg, background: bg, ratio: (Math.max(a, b) + .05) / (Math.min(a, b) + .05), minimum });
  566 |     };
  567 |     const sampledText = new Set<Node>();
  568 |     const visited = new Set<Element>();
  569 |     const visit = (element: Element) => {
  570 |       if (visited.has(element) || !visible(element)) return;
  571 |       visited.add(element);
  572 |       const css = getComputedStyle(element);
  573 |       if (sampleKind === 'text') {
  574 |         if (element instanceof HTMLInputElement || element instanceof HTMLTextAreaElement) {
  575 |           const paint = element.value.trim() ? css.color : element.getAttribute('placeholder') ? getComputedStyle(element, '::placeholder').color : null;
  576 |           if (paint) sample(element, paint, element.value.trim() ? 'input value' : 'input placeholder', 4.5);
  577 |         }
  578 |         const textNodes = element instanceof HTMLSlotElement ? element.assignedNodes({ flatten: true }) : element.childNodes;
  579 |         for (const node of textNodes) if (node.nodeType === Node.TEXT_NODE && node.textContent?.trim() && !sampledText.has(node)) {
  580 |           sampledText.add(node);
  581 |           const range = document.createRange(); range.selectNodeContents(node);
  582 |           if (![...range.getClientRects()].some(box => box.width > 0 && box.height > 0)) continue;
  583 |           const textOwner = (node as Text).assignedSlot ?? element;
  584 |           const textCss = getComputedStyle(textOwner);
  585 |           const large = parseFloat(textCss.fontSize) >= 24 || parseFloat(textCss.fontSize) >= 18.667 && parseInt(textCss.fontWeight) >= 700;
  586 |           sample(textOwner, textCss.color, node.textContent.trim().slice(0, 80), large ? 3 : 4.5, false, 1, [...range.getClientRects()]);
  587 |         }
  588 |       } else if (sampleKind === 'icon' && element.matches('svg path,svg polygon,svg circle,svg rect')) {
  589 |         for (const property of ['fill', 'stroke']) {
  590 |           const paint = css.getPropertyValue(property);
  591 |           if (paint !== 'none') { const opacity = css.getPropertyValue(`${property}-opacity`); sample(element, paint, `icon ${property}`, 3, false, parseFloat(opacity) / (opacity.endsWith('%') ? 100 : 1)); }
  592 |         }
  593 |       } else if (sampleKind === 'border') {
  594 |         for (const pseudo of [null, '::before', '::after']) {
  595 |           const edge = getComputedStyle(element, pseudo);
  596 |           if (pseudo && (edge.content === 'none' || edge.visibility === 'hidden' || Number(edge.opacity) === 0)) continue;
  597 |           if (edge.transform !== 'none') { const matrix = new DOMMatrixReadOnly(edge.transform); if (matrix.a === 0 || matrix.d === 0) continue; }
  598 |           for (const side of ['top', 'right', 'bottom', 'left']) if (parseFloat(edge.getPropertyValue(`border-${side}-width`)) > 0 && !['none', 'hidden'].includes(edge.getPropertyValue(`border-${side}-style`))) sample(element, edge.getPropertyValue(`border-${side}-color`), `${pseudo ?? 'element'} border-${side}`, 3, true);
  599 |         }
  600 |       } else if (sampleKind === 'focus' || sampleKind === 'cue') {
  601 |         for (const pseudo of [null, '::before', '::after']) {
  602 |           const paint = getComputedStyle(element, pseudo);
  603 |           if (paint.visibility === 'hidden' || paint.display === 'none' || Number(paint.opacity) === 0 || pseudo && paint.content === 'none') continue;
  604 |           if (paint.transform !== 'none') { const matrix = new DOMMatrixReadOnly(paint.transform); if (matrix.a === 0 || matrix.d === 0) continue; }
  605 |           const qualifies = (keys: string[]) => sampleKind === 'cue' || audit.changed(element, pseudo, paint, resting, keys);
  606 |           if (qualifies(['outline-width','outline-style','outline-color','outline-offset']) && parseFloat(paint.outlineWidth) > 0 && paint.outlineStyle !== 'none') {
  607 |             const offset = parseFloat(paint.outlineOffset);
  608 |             if (offset < 0 && offset + parseFloat(paint.outlineWidth) > 0) throw new Error('Straddling outline adjacency needs pixel evidence');
  609 |             sample(element, paint.outlineColor, `${pseudo ?? 'element'} outline`, 3, offset >= 0);
  610 |           }
  611 |           if (qualifies(['box-shadow'])) for (const shadow of paint.boxShadow.split(/,(?![^()]*\))/u)) {
  612 |             if (/\binset\b/u.test(shadow)) continue;
  613 |             const lengths = shadow.replace(/[a-z][a-z0-9-]*\([^()]*\)/giu, '').match(/-?[\d.]+px/gu)?.map(Number.parseFloat) ?? [];
  614 |             if (lengths.length < 2 || (lengths[2] ?? 0) > 0 || lengths[0] === 0 && lengths[1] === 0 && (lengths[3] ?? 0) <= 0) continue;
  615 |             for (const color of shadow.match(/[a-z][a-z0-9-]*\([^()]*\)/giu) ?? []) sample(element, color, `${pseudo ?? 'element'} box-shadow`, 3, true);
  616 |           }
  617 |           if (pseudo && qualifies(['border-top-width','border-right-width','border-bottom-width','border-left-width','border-top-color','border-right-color','border-bottom-color','border-left-color'])) for (const side of ['top', 'right', 'bottom', 'left']) if (parseFloat(paint.getPropertyValue(`border-${side}-width`)) > 0 && !['none', 'hidden'].includes(paint.getPropertyValue(`border-${side}-style`))) sample(element, paint.getPropertyValue(`border-${side}-color`), `${pseudo} border-${side}`, 3, true);
  618 |         }
  619 |       }
  620 |       if (element instanceof HTMLSlotElement) for (const child of element.assignedElements({ flatten: true })) visit(child);
  621 |       if (element.shadowRoot) for (const child of element.shadowRoot.children) visit(child);
  622 |       for (const child of element.children) if (!child.matches('style,script,template')) visit(child);
  623 |     };
  624 |     visit(scope);
  625 |     return samples;
  626 |   }, kind);
  627 | }
  628 | 
  629 | export async function expectSurfaceContrast(root: Locator, kind: ContrastKind = 'text') {
  630 |   const samples = await measureSurfaceContrast(root, kind);
> 631 |   expect(samples.length, `Nonempty ${kind} contrast inventory`).toBeGreaterThan(0);
      |                                                                 ^ Error: Nonempty cue contrast inventory
  632 |   // A Fluent control may paint a contrasting edge plus a two-color separator.
  633 |   // Alternatives belong to that unique control; another control cannot mask a failure.
  634 |   const owners = new Set(samples.map(sample => sample.owner));
  635 |   const passingOwners = new Set([...owners].filter(owner => samples.some(sample => sample.owner === owner && sample.ratio >= sample.minimum)));
  636 |   const failures = samples.filter(sample => sample.ratio < sample.minimum && (!(kind === 'focus' || kind === 'cue' || kind === 'border') || !passingOwners.has(sample.owner)));
  637 |   expect(failures, `${kind} contrast failures`).toEqual([]);
  638 |   return samples;
  639 | }
  640 | 
  641 | /** Diagnostic element captures must leave the operator's scroll/focus geometry unchanged. */
  642 | export async function screenshotPreservingScroll(target: Locator, options?: Parameters<Locator['screenshot']>[0]) {
  643 |   const positions = await target.evaluateHandle(() => {
  644 |     const elements = new Set<Element>([document.scrollingElement ?? document.documentElement]);
  645 |     const visit = (root: Document | ShadowRoot) => { for (const element of root.querySelectorAll('*')) {
  646 |       if (element.scrollHeight > element.clientHeight || element.scrollWidth > element.clientWidth || element.scrollTop || element.scrollLeft) elements.add(element);
  647 |       if (element.shadowRoot) visit(element.shadowRoot);
  648 |     } };
  649 |     visit(document);
  650 |     return [...elements].map(element => ({ element, left: element.scrollLeft, top: element.scrollTop }));
  651 |   });
  652 |   try { return await target.screenshot(options); }
  653 |   finally {
  654 |     await positions.evaluate(records => { for (const { element, left, top } of records) if (element.isConnected) element.scrollTo({ left, top, behavior: 'instant' }); });
  655 |     await positions.dispose();
  656 |   }
  657 | }
  658 | 
  659 | /** Wait for finite theme/control paint transitions without changing the active styling. */
  660 | export async function settlePaint(page: Page): Promise<void> {
  661 |   await page.evaluate(async () => {
  662 |     for (let pass = 0; pass < 5; pass++) {
  663 |       await new Promise<void>(resolve => requestAnimationFrame(() => requestAnimationFrame(() => resolve())));
  664 |       const roots: (Document | ShadowRoot)[] = [document];
  665 |       const collect = (root: Document | ShadowRoot) => { for (const element of root.querySelectorAll('*')) if (element.shadowRoot) { roots.push(element.shadowRoot); collect(element.shadowRoot); } };
  666 |       collect(document);
  667 |       const animations = roots.flatMap(root => root.getAnimations()).filter(animation => animation.playState === 'running' && Number.isFinite(Number(animation.effect?.getComputedTiming().endTime)));
  668 |       if (!animations.length) return;
  669 |       await Promise.allSettled(animations.map(animation => animation.finished));
  670 |     }
  671 |   });
  672 | }
  673 | 
```