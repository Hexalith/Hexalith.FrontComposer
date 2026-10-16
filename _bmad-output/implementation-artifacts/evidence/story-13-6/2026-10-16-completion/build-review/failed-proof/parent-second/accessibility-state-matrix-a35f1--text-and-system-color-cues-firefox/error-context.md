# Instructions

- Following Playwright test failed.
- Explain why, be concise, respect Playwright best practices.
- Provide a snippet of code with the fix, if possible.

# Test info

- Name: accessibility-state-matrix.spec.ts >> AE complete state matrix dark >> all eleven actual lifecycle wrapper states retain immediate text and system-color cues
- Location: specs/accessibility-state-matrix.spec.ts:192:5

# Error details

```
Error: page.evaluate: Test ended.
```

# Test source

```ts
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
  631 |   expect(samples.length, `Nonempty ${kind} contrast inventory`).toBeGreaterThan(0);
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
> 661 |   await page.evaluate(async () => {
      |              ^ Error: page.evaluate: Test ended.
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