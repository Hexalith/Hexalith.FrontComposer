# Instructions

- Following Playwright test failed.
- Explain why, be concise, respect Playwright best practices.
- Provide a snippet of code with the fix, if possible.

# Test info

- Name: accessibility-state-matrix.spec.ts >> active toolbar, tabs, palette and settings states in light
- Location: specs/accessibility-state-matrix.spec.ts:382:3

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
  - link "Skip to content" [ref=e3]:
    - /url: /__frontcomposer/specimens/page-toolbar#fc-main-content
  - link "Skip to navigation" [ref=e4]:
    - /url: /__frontcomposer/specimens/page-toolbar#fc-nav
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
                - generic:
                  - generic:
                    - generic:
                      - generic:
                        - navigation "Primary navigation":
                          - generic:
                            - button "Counter"
                            - generic:
                              - generic:
                                - button "More Counter pages"
                            - button "Default"
                            - button "Specimens"
                            - generic:
                              - generic:
                                - button "More Specimens pages"
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
  815 |       visited.add(element);
  816 |       const css = getComputedStyle(element);
  817 |       if (sampleKind === 'text') {
  818 |         if (element instanceof HTMLInputElement || element instanceof HTMLTextAreaElement) {
  819 |           const placeholder = getComputedStyle(element, '::placeholder');
  820 |           const glyph = glyphPaint;
  821 |           const paint = element.value.trim() ? glyph(css) : element.getAttribute('placeholder') ? glyph(placeholder) : null;
  822 |           if (paint && !element.value.trim()) { audit.composition(placeholder); if (placeholder.transform !== 'none') throw new Error('Placeholder geometry needs pixel evidence'); }
  823 |           if (paint) sample(element, paint, element.value.trim() ? 'input value' : 'input placeholder', 4.5, false, element.value.trim() ? 1 : Number(placeholder.opacity));
  824 |         }
  825 |         const textNodes = element instanceof HTMLSlotElement ? element.assignedNodes({ flatten: true }) : element.childNodes;
  826 |         for (const node of textNodes) if (node.nodeType === Node.TEXT_NODE && node.textContent?.trim() && !sampledText.has(node)) {
  827 |           sampledText.add(node);
  828 |           const range = document.createRange(); range.selectNodeContents(node);
  829 |           if (![...range.getClientRects()].some(box => box.width > 0 && box.height > 0)) continue;
  830 |           const textOwner = (node as Text).assignedSlot ?? element;
  831 |           const textCss = getComputedStyle(textOwner);
  832 |           const large = parseFloat(textCss.fontSize) >= 24 || parseFloat(textCss.fontSize) >= 18.667 && parseInt(textCss.fontWeight) >= 700;
  833 |           sample(textOwner, glyphPaint(textCss), node.textContent.trim().slice(0, 80), large ? 3 : 4.5, false, 1, [...range.getClientRects()]);
  834 |         }
  835 |         for (const pseudo of ['::before', '::after']) {
  836 |           const generated = getComputedStyle(element, pseudo);
  837 |           if (!generated.content || ['none','normal','""',"''"].includes(generated.content) || generated.visibility === 'hidden' || generated.display === 'none' || Number(generated.opacity) === 0) continue;
  838 |           const visualContent = generated.content.match(/^("(?:\\.|[^"\\])*"|'(?:\\.|[^'\\])*')(?:\s*\/.*)?$/u)?.[1];
  839 |           if (!visualContent) throw new Error(`Generated content needs pixel evidence: ${element.tagName}${pseudo} ${generated.content}`);
  840 |           if (['""', "''"].includes(visualContent)) continue;
  841 |           audit.composition(generated);
  842 |           if (generated.position !== 'static' || generated.transform !== 'none' || generated.cssFloat !== 'none'
  843 |             || [generated.marginTop, generated.marginRight, generated.marginBottom, generated.marginLeft].some(value => !Number.isFinite(parseFloat(value)) || parseFloat(value) < 0)) throw new Error(`Generated text geometry needs pixel evidence: ${JSON.stringify({ element: element.tagName + '#' + element.id + '.' + element.className, pseudo, content: generated.content, position: generated.position, margins: [generated.marginTop, generated.marginRight, generated.marginBottom, generated.marginLeft], transform: generated.transform, float: generated.cssFloat, root: element.getRootNode() instanceof ShadowRoot ? (element.getRootNode() as ShadowRoot).host.tagName : 'document' })}`);
  844 |           if (parse(generated.backgroundColor)[3] > 0 || generated.backgroundImage !== 'none') throw new Error('Generated text background needs pixel evidence');
  845 |           const large = parseFloat(generated.fontSize) >= 24 || parseFloat(generated.fontSize) >= 18.667 && parseInt(generated.fontWeight) >= 700;
  846 |           sample(element, glyphPaint(generated), `${pseudo} generated text`, large ? 3 : 4.5, false, Number(generated.opacity));
  847 |         }
  848 |       } else if (sampleKind === 'icon' && element.matches('svg path,svg polygon,svg circle,svg rect,svg ellipse,svg line,svg polyline,svg use,svg text')) {
  849 |         if (element.matches('use')) throw new Error('SVG use instance needs pixel evidence');
  850 |         for (const property of ['fill', 'stroke']) {
  851 |           const paint = css.getPropertyValue(property);
  852 |           if (property === 'stroke' && parseFloat(css.strokeWidth) <= 0 || property === 'fill' && element.matches('line')) continue;
  853 |           if (paint !== 'none') {
  854 |             const opacity = css.getPropertyValue(`${property}-opacity`), bounds = element.getBoundingClientRect();
  855 |             if (property === 'fill' && (!bounds.width || !bounds.height)) continue;
  856 |             let boxes: PaintBox[] | undefined;
  857 |             if (property === 'stroke') {
  858 |               const matrix = (element as SVGGraphicsElement).getScreenCTM();
  859 |               if (!matrix) throw new Error('Unresolved SVG stroke envelope needs pixel evidence');
  860 |               const half = parseFloat(css.strokeWidth) / 2, fixed = css.vectorEffect === 'non-scaling-stroke';
  861 |               const x = fixed ? half : half * Math.hypot(matrix.a, matrix.c), y = fixed ? half : half * Math.hypot(matrix.b, matrix.d);
  862 |               boxes = [{ left: bounds.left - x, right: bounds.right + x, top: bounds.top - y, bottom: bounds.bottom + y }];
  863 |             }
  864 |             sample(element, paint, `icon ${property}`, 3, false, parseFloat(opacity) / (opacity.endsWith('%') ? 100 : 1), boxes);
  865 |           }
  866 |         }
  867 |       } else if (sampleKind === 'border') {
  868 |         for (const pseudo of [null, '::before', '::after']) {
  869 |           const edge = getComputedStyle(element, pseudo);
  870 |           if (pseudo && (edge.content === 'none' || edge.visibility === 'hidden' || Number(edge.opacity) === 0)) continue;
  871 |           if (edge.transform !== 'none') { const matrix = new DOMMatrixReadOnly(edge.transform); if (matrix.a === 0 || matrix.d === 0) continue; }
  872 |           audit.composition(edge);
  873 |           if (pseudo && (parse(edge.backgroundColor)[3] > 0 || edge.backgroundImage !== 'none')) throw new Error('Pseudo border background composition needs pixel evidence');
  874 |           for (const side of ['top', 'right', 'bottom', 'left']) if (parseFloat(edge.getPropertyValue(`border-${side}-width`)) > 0 && !['none', 'hidden'].includes(edge.getPropertyValue(`border-${side}-style`))) sample(element, edge.getPropertyValue(`border-${side}-color`), `${pseudo ?? 'element'} border-${side}`, 3, pseudo ? pseudoOutside(element, edge, side) : true, Number(edge.opacity));
  875 |         }
  876 |       } else if (sampleKind === 'focus' || sampleKind === 'cue') {
  877 |         for (const pseudo of [null, '::before', '::after']) {
  878 |           const paint = getComputedStyle(element, pseudo);
  879 |           if (paint.visibility === 'hidden' || paint.display === 'none' || Number(paint.opacity) === 0 || pseudo && paint.content === 'none') continue;
  880 |           if (paint.transform !== 'none') { const matrix = new DOMMatrixReadOnly(paint.transform); if (matrix.a === 0 || matrix.d === 0) continue; }
  881 |           const qualifies = (keys: string[]) => {
  882 |             const changed = sampleKind === 'cue' || audit.changed(element, pseudo, paint, resting, keys);
  883 |             if (changed) {
  884 |               audit.composition(paint);
  885 |               if (pseudo && (parse(paint.backgroundColor)[3] > 0 || paint.backgroundImage !== 'none')) throw new Error('Pseudo focus background composition needs pixel evidence');
  886 |             }
  887 |             return changed;
  888 |           };
  889 |           if (qualifies(['outline-width','outline-style','outline-color','outline-offset']) && parseFloat(paint.outlineWidth) > 0 && paint.outlineStyle !== 'none') {
  890 |             const offset = parseFloat(paint.outlineOffset);
  891 |             if (offset < 0 && offset + parseFloat(paint.outlineWidth) > 0) throw new Error('Straddling outline adjacency needs pixel evidence');
  892 |             sample(element, paint.outlineColor, `${pseudo ?? 'element'} outline`, 3, offset >= 0, Number(paint.opacity));
  893 |           }
  894 |           if (qualifies(['box-shadow'])) for (const shadow of audit.shadows(element, pseudo, paint, resting)) {
  895 |             if (/\binset\b/u.test(shadow)) continue;
  896 |             const lengths = shadow.replace(/[a-z][a-z0-9-]*\([^()]*\)/giu, '').match(/-?[\d.]+px/gu)?.map(Number.parseFloat) ?? [];
  897 |             const bounds = element.getBoundingClientRect(), spread = lengths[3] ?? 0;
  898 |             if (bounds.width + 2 * spread <= 0 || bounds.height + 2 * spread <= 0 || lengths.length < 2 || (lengths[2] ?? 0) > 0 || lengths[0] === 0 && lengths[1] === 0 && (lengths[3] ?? 0) <= 0) continue;
  899 |             for (const color of shadow.match(/[a-z][a-z0-9-]*\([^()]*\)/giu) ?? []) sample(element, color, `${pseudo ?? 'element'} box-shadow${audit.shadowBound(element, pseudo, shadow) ? ' (conservative composite bound)' : ''}`, 3, true, Number(paint.opacity), undefined, audit.shadowBound(element, pseudo, shadow));
  900 |           }
  901 |           if (pseudo && qualifies(['border-top-width','border-right-width','border-bottom-width','border-left-width','border-top-color','border-right-color','border-bottom-color','border-left-color'])) for (const side of ['top', 'right', 'bottom', 'left']) if (parseFloat(paint.getPropertyValue(`border-${side}-width`)) > 0 && !['none', 'hidden'].includes(paint.getPropertyValue(`border-${side}-style`))) sample(element, paint.getPropertyValue(`border-${side}-color`), `${pseudo} border-${side}`, 3, pseudoOutside(element, paint, side), Number(paint.opacity));
  902 |         }
  903 |       }
  904 |       if (element instanceof HTMLSlotElement) for (const child of element.assignedElements({ flatten: true })) visit(child);
  905 |       if (element.shadowRoot) for (const child of element.shadowRoot.children) visit(child);
  906 |       for (const child of element.children) if (!child.matches('style,script,template')) visit(child);
  907 |     };
  908 |     visit(scope);
  909 |     return samples;
  910 |   }, kind);
  911 | }
  912 | 
  913 | export async function expectSurfaceContrast(root: Locator, kind: ContrastKind = 'text') {
  914 |   const samples = await measureSurfaceContrast(root, kind);
> 915 |   expect(samples.length, `Nonempty ${kind} contrast inventory`).toBeGreaterThan(0);
      |                                                                 ^ Error: Nonempty cue contrast inventory
  916 |   // A Fluent control may paint a contrasting edge plus a two-color separator.
  917 |   // Alternatives belong to that unique control; another control cannot mask a failure.
  918 |   const owners = new Set(samples.map(sample => sample.owner));
  919 |   const passingOwners = new Set([...owners].filter(owner => samples.some(sample => sample.owner === owner && sample.ratio >= sample.minimum)));
  920 |   const failures = samples.filter(sample => sample.ratio < sample.minimum && (!(kind === 'focus' || kind === 'cue' || kind === 'border') || !passingOwners.has(sample.owner)));
  921 |   expect(failures, `${kind} contrast failures`).toEqual([]);
  922 |   return samples;
  923 | }
  924 | 
  925 | /** Diagnostic element captures must leave the operator's scroll/focus geometry unchanged. */
  926 | export async function screenshotPreservingScroll(target: Locator, options?: Parameters<Locator['screenshot']>[0]) {
  927 |   const positions = await target.evaluateHandle(() => {
  928 |     const elements = new Set<Element>([document.scrollingElement ?? document.documentElement]);
  929 |     const visit = (root: Document | ShadowRoot) => { for (const element of root.querySelectorAll('*')) {
  930 |       if (element.scrollHeight > element.clientHeight || element.scrollWidth > element.clientWidth || element.scrollTop || element.scrollLeft) elements.add(element);
  931 |       if (element.shadowRoot) visit(element.shadowRoot);
  932 |     } };
  933 |     visit(document);
  934 |     return [...elements].map(element => ({ element, left: element.scrollLeft, top: element.scrollTop }));
  935 |   });
  936 |   try { return await target.screenshot(options); }
  937 |   finally {
  938 |     await positions.evaluate(records => { for (const { element, left, top } of records) if (element.isConnected) element.scrollTo({ left, top, behavior: 'instant' }); });
  939 |     await positions.dispose();
  940 |   }
  941 | }
  942 | 
  943 | /** Wait for finite theme/control paint transitions without changing the active styling. */
  944 | export async function settlePaint(page: Page): Promise<void> {
  945 |   await page.evaluate(async () => {
  946 |     for (let pass = 0; pass < 5; pass++) {
  947 |       await new Promise<void>(resolve => requestAnimationFrame(() => requestAnimationFrame(() => resolve())));
  948 |       const roots: (Document | ShadowRoot)[] = [document];
  949 |       const collect = (root: Document | ShadowRoot) => { for (const element of root.querySelectorAll('*')) if (element.shadowRoot) { roots.push(element.shadowRoot); collect(element.shadowRoot); } };
  950 |       collect(document);
  951 |       const animations = roots.flatMap(root => root.getAnimations()).filter(animation => animation.playState === 'running' && Number.isFinite(Number(animation.effect?.getComputedTiming().endTime)));
  952 |       if (!animations.length) return;
  953 |       await Promise.allSettled(animations.map(animation => animation.finished));
  954 |     }
  955 |   });
  956 | }
  957 | 
```