# Instructions

- Following Playwright test failed.
- Explain why, be concise, respect Playwright best practices.
- Provide a snippet of code with the fix, if possible.

# Test info

- Name: responsive-accessibility.spec.ts >> narrow specimen navigation and grid retain unobscured focus in dark
- Location: specs/responsive-accessibility.spec.ts:280:3

# Error details

```
Error: focus contrast failures

expect(received).toEqual(expected) // deep equality

- Expected  -  1
+ Received  + 21

- Array []
+ Array [
+   Object {
+     "background": Array [
+       255,
+       255,
+       255,
+       1,
+     ],
+     "foreground": Array [
+       51,
+       153,
+       255,
+       1,
+     ],
+     "minimum": 3,
+     "node": "div",
+     "owner": 0,
+     "paint": "element outline",
+     "ratio": 2.9406597621705757,
+   },
+ ]
```

# Page snapshot

```yaml
- generic [ref=e2]:
  - link "Skip to content" [ref=e3] [cursor=pointer]:
    - /url: /__frontcomposer/specimens/type?theme=dark#fc-main-content
  - link "Skip to navigation" [ref=e4] [cursor=pointer]:
    - /url: /__frontcomposer/specimens/type?theme=dark#fc-nav
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
            - heading "Type, theme, density, lifecycle, and navigation specimen" [level=1] [ref=e46]
            - paragraph [ref=e47]: Generated shell surfaces rendered with deterministic local data.
            - link "Skip to command form" [ref=e48] [cursor=pointer]:
              - /url: "#fc-specimen-command-form"
          - region [ref=e49]:
            - heading "Theme and density controls" [level=2] [ref=e50]
            - generic [ref=e51]:
              - link "light" [ref=e52] [cursor=pointer]:
                - /url: /__frontcomposer/specimens/type?theme=light&density=comfortable
              - link "dark" [ref=e53] [cursor=pointer]:
                - /url: /__frontcomposer/specimens/type?theme=dark&density=comfortable
              - link "compact" [ref=e54] [cursor=pointer]:
                - /url: /__frontcomposer/specimens/type?theme=dark&density=compact
              - link "comfortable" [ref=e55] [cursor=pointer]:
                - /url: /__frontcomposer/specimens/type?theme=dark&density=comfortable
              - link "roomy" [ref=e56] [cursor=pointer]:
                - /url: /__frontcomposer/specimens/type?theme=dark&density=roomy
          - region [ref=e57]:
            - heading "Type ramp" [level=2] [ref=e58]
            - generic [ref=e59]:
              - article [ref=e60]:
                - paragraph [ref=e61]: Display
                - generic [ref=e62]: FrontComposer Display
              - article [ref=e63]:
                - paragraph [ref=e64]: Title
                - generic [ref=e65]: Generated command title
              - article [ref=e66]:
                - paragraph [ref=e67]: Subtitle
                - generic [ref=e68]: Projection subtitle and context
              - article [ref=e69]:
                - paragraph [ref=e70]: Body
                - generic [ref=e71]: Readable generated body text
              - article [ref=e72]:
                - paragraph [ref=e73]: Caption
                - generic [ref=e74]: Supplemental metadata caption
              - article [ref=e75]:
                - paragraph [ref=e76]: Code
                - generic [ref=e77]: FC-STATIC-0001
          - region [ref=e78]:
            - heading "Semantic color tokens" [level=2] [ref=e79]
            - generic [ref=e80]:
              - article [ref=e81]:
                - heading "light theme" [level=3] [ref=e82]
                - generic [ref=e83]:
                  - generic [ref=e85]: Neutral
                  - strong [ref=e86]: Available
                - generic [ref=e87]:
                  - generic [ref=e89]: Informational
                  - strong [ref=e90]: Queued
                - generic [ref=e91]:
                  - generic [ref=e93]: Success
                  - strong [ref=e94]: Confirmed
                - generic [ref=e95]:
                  - generic [ref=e97]: Warning
                  - strong [ref=e98]: Needs review
                - generic [ref=e99]:
                  - generic [ref=e101]: Danger
                  - strong [ref=e102]: Rejected
                - generic [ref=e103]:
                  - generic [ref=e105]: Accent
                  - strong [ref=e106]: Selected
              - article [ref=e107]:
                - heading "dark theme" [level=3] [ref=e108]
                - generic [ref=e109]:
                  - generic [ref=e111]: Neutral
                  - strong [ref=e112]: Available
                - generic [ref=e113]:
                  - generic [ref=e115]: Informational
                  - strong [ref=e116]: Queued
                - generic [ref=e117]:
                  - generic [ref=e119]: Success
                  - strong [ref=e120]: Confirmed
                - generic [ref=e121]:
                  - generic [ref=e123]: Warning
                  - strong [ref=e124]: Needs review
                - generic [ref=e125]:
                  - generic [ref=e127]: Danger
                  - strong [ref=e128]: Rejected
                - generic [ref=e129]:
                  - generic [ref=e131]: Accent
                  - strong [ref=e132]: Selected
          - region [ref=e133]:
            - heading "Density states" [level=2] [ref=e134]
            - generic [ref=e135]:
              - article [ref=e136]:
                - heading "compact density" [level=3] [ref=e137]
                - button "Primary action" [ref=e138]
                - generic [ref=e139]:
                  - text: Label
                  - textbox "compact density text input" [ref=e140]: compact sample
              - article [ref=e141]:
                - heading "comfortable density" [level=3] [ref=e142]
                - button "Primary action" [ref=e143]
                - generic [ref=e144]:
                  - text: Label
                  - textbox "comfortable density text input" [ref=e145]: comfortable sample
              - article [ref=e146]:
                - heading "roomy density" [level=3] [ref=e147]
                - button "Primary action" [ref=e148]
                - generic [ref=e149]:
                  - text: Label
                  - textbox "roomy density text input" [ref=e150]: roomy sample
          - region [ref=e151]:
            - heading "Generated projection DataGrid and badge states" [level=2] [ref=e152]
            - status [ref=e153]
            - paragraph [ref=e154]: 6 specimen status
            - group "Status filter chips" [ref=e155]:
              - button "Neutral, currently inactive" [ref=e156] [cursor=pointer]:
                - generic [ref=e157]: Neutral
              - button "Info, currently inactive" [ref=e158] [cursor=pointer]:
                - generic [ref=e159]: Info
              - button "Success, currently inactive" [ref=e160] [cursor=pointer]:
                - generic [ref=e161]: Success
              - button "Warning, currently inactive" [ref=e162] [cursor=pointer]:
                - generic [ref=e163]: Warning
              - button "Danger, currently inactive" [ref=e164] [cursor=pointer]:
                - generic [ref=e165]: Danger
              - button "Accent, currently inactive" [ref=e166] [cursor=pointer]:
                - generic [ref=e167]: Accent
            - text: 6 specimen status
            - region "specimen status" [active] [ref=e168]:
              - table [ref=e169]:
                - rowgroup [ref=e170]:
                  - row [ref=e171]:
                    - cell [ref=e172]:
                      - generic [ref=e173]:
                        - text: FC-1000Neutral projection
                        - 'img "Status: Neutral" [ref=e175]'
                        - text: Shell
                  - row [ref=e178]:
                    - cell [ref=e179]:
                      - generic [ref=e180]:
                        - text: FC-1001Info projection
                        - 'img "Status: Info" [ref=e182]'
                        - text: SourceTools
                  - row [ref=e185]:
                    - cell [ref=e186]:
                      - generic [ref=e187]:
                        - text: FC-1002Success projection
                        - 'img "Status: Success" [ref=e189]'
                        - text: Contracts
                  - row [ref=e192]:
                    - cell [ref=e193]:
                      - generic [ref=e194]:
                        - text: FC-1003Warning projection
                        - 'img "Status: Warning" [ref=e196]'
                        - text: Testing
                  - row [ref=e199]:
                    - cell [ref=e200]:
                      - generic [ref=e201]:
                        - text: FC-1004Danger projection
                        - 'img "Status: Danger" [ref=e203]'
                        - text: CI
                  - row [ref=e206]:
                    - cell [ref=e207]:
                      - generic [ref=e208]:
                        - text: FC-1005Accent projection
                        - 'img "Status: Accent" [ref=e210]'
                        - text: Docs
              - region "Details for"
          - region [ref=e213]:
            - heading "Generated Counter projection grid" [level=2] [ref=e214]
            - status [ref=e215]
            - paragraph [ref=e216]: 1 counters
            - text: 1 counters
            - region "counters" [ref=e217]:
              - table [ref=e218]:
                - rowgroup [ref=e219]:
                  - row [ref=e220]:
                    - cell [ref=e221]:
                      - generic [ref=e222]:
                        - text: fc-correlation-0002421/15/2026
                        - generic "Metadata with unsupported type System.Collections.Generic.Dictionary requires a custom renderer" [ref=e224]:
                          - strong [ref=e225]: Metadata
                          - text: (System.Collections.Generic.Dictionary) This field requires a custom renderer.
                          - link "Learn how to customize this field" [ref=e226] [cursor=pointer]:
                            - /url: https://hexalith.github.io/FrontComposer/diagnostics/HFC1002
              - region "Details for"
          - region [ref=e227]:
            - heading "Command form and lifecycle states" [level=2] [ref=e228]
            - form "Specimen command form" [ref=e229]:
              - generic [ref=e230]:
                - text: Counter name
                - textbox "Counter name" [ref=e231]: Specimen counter
              - generic [ref=e232]:
                - text: Amount
                - spinbutton "Amount" [ref=e233]: "3"
              - button "Submit command" [ref=e234]
              - button "Cancel" [ref=e235]
            - list "Five command lifecycle states" [ref=e236]:
              - listitem [ref=e237]:
                - strong [ref=e238]: Idle
                - text: Ready to submit.
              - listitem [ref=e239]:
                - strong [ref=e240]: Submitting
                - text: Submitting command.
              - listitem [ref=e241]:
                - strong [ref=e242]: Acknowledged
                - text: Command accepted by the host.
              - listitem [ref=e243]:
                - strong [ref=e244]: Syncing
                - text: Waiting for projection synchronization.
              - listitem [ref=e245]:
                - strong [ref=e246]: Confirmed or rejected
                - text: Terminal confirmation and rejection states remain text-visible.
          - region [ref=e247]:
            - heading "Destructive command confirmation" [level=2] [ref=e248]
            - group [ref=e250]:
              - generic "Purge Specimen Record command form" [ref=e251]:
                - generic [ref=e252]:
                  - generic [ref=e253]:
                    - group "Purge details" [ref=e254]:
                      - generic [ref=e257]:
                        - generic: Record Id
                        - generic "Record Id" [ref=e259]:
                          - textbox "Record Id, Required" [ref=e261]: FC-1002
                        - generic: Record to purge.
                      - generic [ref=e264]:
                        - generic: Reason
                        - generic "Reason" [ref=e266]:
                          - textbox "Reason, Required" [ref=e268]: QA destructive confirmation coverage
                        - generic: Why this purge is required.
                    - generic [ref=e271]:
                      - generic: Retention
                      - generic "Retention" [ref=e273] [cursor=pointer]:
                        - combobox "Retention" [ref=e275]
                      - generic: How long the purged record stays recoverable.
                    - button "Purge Specimen Record" [ref=e280] [cursor=pointer]
                  - status [ref=e282]
          - region [ref=e283]:
            - heading "Policy-gated command authorization" [level=2] [ref=e284]
            - group [ref=e286]:
              - generic "Policy Allowed Specimen command form" [ref=e287]:
                - generic [ref=e288]:
                  - generic [ref=e289]:
                    - generic [ref=e291]:
                      - generic: Record Id
                      - generic "Record Id" [ref=e293]:
                        - textbox "Record Id, Required" [ref=e295]: FC-AUTH-ALLOW
                    - generic [ref=e297]:
                      - generic: Reason
                      - generic "Reason" [ref=e299]:
                        - textbox "Reason, Required" [ref=e301]: QA policy allowed coverage
                    - button "Policy Allowed Specimen" [ref=e302] [cursor=pointer]
                  - status [ref=e304]
            - group [ref=e305]:
              - group [ref=e306]:
                - heading "Permission required" [level=2] [ref=e307]
                - paragraph [ref=e308]: You do not have permission to Policy Denied Specimen.
          - region [ref=e309]:
            - heading "Expanded detail view" [level=2] [ref=e310]
            - group [ref=e311]:
              - generic "Open projection details for FC-1002" [ref=e312]
              - generic [ref=e313]:
                - term [ref=e314]: Correlation
                - definition [ref=e315]: fc-correlation-0002
                - term [ref=e316]: Last synchronized
                - definition [ref=e317]: 2026-01-15 13:45 UTC
          - navigation "Specimen multi-level navigation" [ref=e318]:
            - heading "Multi-level navigation" [level=2] [ref=e319]
            - list [ref=e320]:
              - listitem [ref=e321]:
                - link "Foundation" [ref=e322] [cursor=pointer]:
                  - /url: "#fc-type-ramp-title"
              - listitem [ref=e323]:
                - button "Generated surfaces" [expanded] [ref=e324]
                - list [ref=e325]:
                  - listitem [ref=e326]:
                    - link "Projection grid" [ref=e327] [cursor=pointer]:
                      - /url: "#fc-badge-grid-title"
                  - listitem [ref=e328]:
                    - link "Command lifecycle" [ref=e329] [cursor=pointer]:
                      - /url: "#fc-specimen-command-title"
    - generic [ref=e330]: Hexalith FrontComposer © 2026
```

# Test source

```ts
  551 |     };
  552 |     const samples: { owner: number; node: string; paint: string; foreground: number[]; background: number[]; ratio: number; minimum: number }[] = [];
  553 |     const owners = new Map<Element, number>();
  554 |     const controlOwner = (element: Element) => {
  555 |       let nearest: Element | null = null;
  556 |       for (let current: Element | null = element; current; current = parent(current)) {
  557 |         if (current.matches('fluent-button,fluent-text-input,fluent-number-input,fluent-dropdown,fluent-combobox,fluent-select,fluent-radio,fluent-switch')) return current;
  558 |         if (!nearest && current.matches('button,input,select,textarea,a[href],[role="button"],[role="tab"],[role="radio"],[role="option"]')) nearest = current;
  559 |         if (current === scope) break;
  560 |       }
  561 |       return nearest ?? element;
  562 |     };
  563 |     const sample = (element: Element, value: string, label: string, minimum: number, outside = false, alpha = 1, paintBoxes?: DOMRect[]) => {
  564 |       let boxes: PaintBox[] = paintBoxes ?? [element.getBoundingClientRect()];
  565 |       for (let current: Element | null = element; current; current = parent(current)) {
  566 |         const css = getComputedStyle(current);
  567 |         const clipping = audit.clip(css, current.getBoundingClientRect());
  568 |         if (clipping.kind === 'hidden') return;
  569 |         if (clipping.kind === 'unsupported') throw new Error('Clipped contrast paint needs pixel evidence');
  570 |         if (clipping.box) boxes = boxes.map(box => ({ left: Math.max(box.left, clipping.box!.left), right: Math.min(box.right, clipping.box!.right), top: Math.max(box.top, clipping.box!.top), bottom: Math.min(box.bottom, clipping.box!.bottom) }));
  571 |       }
  572 |       if (!boxes.some(box => box.right > box.left && box.bottom > box.top)) return;
  573 |       const color = parse(value); color[3] *= alpha;
  574 |       if (color[3] === 0 && sampleKind !== 'icon') return;
  575 |       const bg = background(outside ? parent(element) : element);
  576 |       const fg = composite(color, bg), a = luminance(fg), b = luminance(bg);
  577 |       const owner = controlOwner(element);
  578 |       if (!owners.has(owner)) owners.set(owner, owners.size);
  579 |       samples.push({ owner: owners.get(owner)!, node: `${element.tagName.toLowerCase()}${element.id ? '#' + element.id : ''}`, paint: label, foreground: fg, background: bg, ratio: (Math.max(a, b) + .05) / (Math.min(a, b) + .05), minimum });
  580 |     };
  581 |     const sampledText = new Set<Node>();
  582 |     const visited = new Set<Element>();
  583 |     const visit = (element: Element) => {
  584 |       if (visited.has(element) || !visible(element)) return;
  585 |       visited.add(element);
  586 |       const css = getComputedStyle(element);
  587 |       if (sampleKind === 'text') {
  588 |         if (element instanceof HTMLInputElement || element instanceof HTMLTextAreaElement) {
  589 |           const paint = element.value.trim() ? css.color : element.getAttribute('placeholder') ? getComputedStyle(element, '::placeholder').color : null;
  590 |           if (paint) sample(element, paint, element.value.trim() ? 'input value' : 'input placeholder', 4.5);
  591 |         }
  592 |         const textNodes = element instanceof HTMLSlotElement ? element.assignedNodes({ flatten: true }) : element.childNodes;
  593 |         for (const node of textNodes) if (node.nodeType === Node.TEXT_NODE && node.textContent?.trim() && !sampledText.has(node)) {
  594 |           sampledText.add(node);
  595 |           const range = document.createRange(); range.selectNodeContents(node);
  596 |           if (![...range.getClientRects()].some(box => box.width > 0 && box.height > 0)) continue;
  597 |           const textOwner = (node as Text).assignedSlot ?? element;
  598 |           const textCss = getComputedStyle(textOwner);
  599 |           const large = parseFloat(textCss.fontSize) >= 24 || parseFloat(textCss.fontSize) >= 18.667 && parseInt(textCss.fontWeight) >= 700;
  600 |           sample(textOwner, textCss.color, node.textContent.trim().slice(0, 80), large ? 3 : 4.5, false, 1, [...range.getClientRects()]);
  601 |         }
  602 |       } else if (sampleKind === 'icon' && element.matches('svg path,svg polygon,svg circle,svg rect')) {
  603 |         for (const property of ['fill', 'stroke']) {
  604 |           const paint = css.getPropertyValue(property);
  605 |           if (paint !== 'none') { const opacity = css.getPropertyValue(`${property}-opacity`); sample(element, paint, `icon ${property}`, 3, false, parseFloat(opacity) / (opacity.endsWith('%') ? 100 : 1)); }
  606 |         }
  607 |       } else if (sampleKind === 'border') {
  608 |         for (const pseudo of [null, '::before', '::after']) {
  609 |           const edge = getComputedStyle(element, pseudo);
  610 |           if (pseudo && (edge.content === 'none' || edge.visibility === 'hidden' || Number(edge.opacity) === 0)) continue;
  611 |           if (edge.transform !== 'none') { const matrix = new DOMMatrixReadOnly(edge.transform); if (matrix.a === 0 || matrix.d === 0) continue; }
  612 |           for (const side of ['top', 'right', 'bottom', 'left']) if (parseFloat(edge.getPropertyValue(`border-${side}-width`)) > 0 && !['none', 'hidden'].includes(edge.getPropertyValue(`border-${side}-style`))) sample(element, edge.getPropertyValue(`border-${side}-color`), `${pseudo ?? 'element'} border-${side}`, 3, true);
  613 |         }
  614 |       } else if (sampleKind === 'focus' || sampleKind === 'cue') {
  615 |         for (const pseudo of [null, '::before', '::after']) {
  616 |           const paint = getComputedStyle(element, pseudo);
  617 |           if (paint.visibility === 'hidden' || paint.display === 'none' || Number(paint.opacity) === 0 || pseudo && paint.content === 'none') continue;
  618 |           if (paint.transform !== 'none') { const matrix = new DOMMatrixReadOnly(paint.transform); if (matrix.a === 0 || matrix.d === 0) continue; }
  619 |           const qualifies = (keys: string[]) => sampleKind === 'cue' || audit.changed(element, pseudo, paint, resting, keys);
  620 |           if (qualifies(['outline-width','outline-style','outline-color','outline-offset']) && parseFloat(paint.outlineWidth) > 0 && paint.outlineStyle !== 'none') {
  621 |             const offset = parseFloat(paint.outlineOffset);
  622 |             if (offset < 0 && offset + parseFloat(paint.outlineWidth) > 0) throw new Error('Straddling outline adjacency needs pixel evidence');
  623 |             sample(element, paint.outlineColor, `${pseudo ?? 'element'} outline`, 3, offset >= 0);
  624 |           }
  625 |           if (qualifies(['box-shadow'])) for (const shadow of paint.boxShadow.split(/,(?![^()]*\))/u)) {
  626 |             if (/\binset\b/u.test(shadow)) continue;
  627 |             const lengths = shadow.replace(/[a-z][a-z0-9-]*\([^()]*\)/giu, '').match(/-?[\d.]+px/gu)?.map(Number.parseFloat) ?? [];
  628 |             if (lengths.length < 2 || (lengths[2] ?? 0) > 0 || lengths[0] === 0 && lengths[1] === 0 && (lengths[3] ?? 0) <= 0) continue;
  629 |             for (const color of shadow.match(/[a-z][a-z0-9-]*\([^()]*\)/giu) ?? []) sample(element, color, `${pseudo ?? 'element'} box-shadow`, 3, true);
  630 |           }
  631 |           if (pseudo && qualifies(['border-top-width','border-right-width','border-bottom-width','border-left-width','border-top-color','border-right-color','border-bottom-color','border-left-color'])) for (const side of ['top', 'right', 'bottom', 'left']) if (parseFloat(paint.getPropertyValue(`border-${side}-width`)) > 0 && !['none', 'hidden'].includes(paint.getPropertyValue(`border-${side}-style`))) sample(element, paint.getPropertyValue(`border-${side}-color`), `${pseudo} border-${side}`, 3, true);
  632 |         }
  633 |       }
  634 |       if (element instanceof HTMLSlotElement) for (const child of element.assignedElements({ flatten: true })) visit(child);
  635 |       if (element.shadowRoot) for (const child of element.shadowRoot.children) visit(child);
  636 |       for (const child of element.children) if (!child.matches('style,script,template')) visit(child);
  637 |     };
  638 |     visit(scope);
  639 |     return samples;
  640 |   }, kind);
  641 | }
  642 | 
  643 | export async function expectSurfaceContrast(root: Locator, kind: ContrastKind = 'text') {
  644 |   const samples = await measureSurfaceContrast(root, kind);
  645 |   expect(samples.length, `Nonempty ${kind} contrast inventory`).toBeGreaterThan(0);
  646 |   // A Fluent control may paint a contrasting edge plus a two-color separator.
  647 |   // Alternatives belong to that unique control; another control cannot mask a failure.
  648 |   const owners = new Set(samples.map(sample => sample.owner));
  649 |   const passingOwners = new Set([...owners].filter(owner => samples.some(sample => sample.owner === owner && sample.ratio >= sample.minimum)));
  650 |   const failures = samples.filter(sample => sample.ratio < sample.minimum && (!(kind === 'focus' || kind === 'cue' || kind === 'border') || !passingOwners.has(sample.owner)));
> 651 |   expect(failures, `${kind} contrast failures`).toEqual([]);
      |                                                 ^ Error: focus contrast failures
  652 |   return samples;
  653 | }
  654 | 
  655 | /** Diagnostic element captures must leave the operator's scroll/focus geometry unchanged. */
  656 | export async function screenshotPreservingScroll(target: Locator, options?: Parameters<Locator['screenshot']>[0]) {
  657 |   const positions = await target.evaluateHandle(() => {
  658 |     const elements = new Set<Element>([document.scrollingElement ?? document.documentElement]);
  659 |     const visit = (root: Document | ShadowRoot) => { for (const element of root.querySelectorAll('*')) {
  660 |       if (element.scrollHeight > element.clientHeight || element.scrollWidth > element.clientWidth || element.scrollTop || element.scrollLeft) elements.add(element);
  661 |       if (element.shadowRoot) visit(element.shadowRoot);
  662 |     } };
  663 |     visit(document);
  664 |     return [...elements].map(element => ({ element, left: element.scrollLeft, top: element.scrollTop }));
  665 |   });
  666 |   try { return await target.screenshot(options); }
  667 |   finally {
  668 |     await positions.evaluate(records => { for (const { element, left, top } of records) if (element.isConnected) element.scrollTo({ left, top, behavior: 'instant' }); });
  669 |     await positions.dispose();
  670 |   }
  671 | }
  672 | 
  673 | /** Wait for finite theme/control paint transitions without changing the active styling. */
  674 | export async function settlePaint(page: Page): Promise<void> {
  675 |   await page.evaluate(async () => {
  676 |     for (let pass = 0; pass < 5; pass++) {
  677 |       await new Promise<void>(resolve => requestAnimationFrame(() => requestAnimationFrame(() => resolve())));
  678 |       const roots: (Document | ShadowRoot)[] = [document];
  679 |       const collect = (root: Document | ShadowRoot) => { for (const element of root.querySelectorAll('*')) if (element.shadowRoot) { roots.push(element.shadowRoot); collect(element.shadowRoot); } };
  680 |       collect(document);
  681 |       const animations = roots.flatMap(root => root.getAnimations()).filter(animation => animation.playState === 'running' && Number.isFinite(Number(animation.effect?.getComputedTiming().endTime)));
  682 |       if (!animations.length) return;
  683 |       await Promise.allSettled(animations.map(animation => animation.finished));
  684 |     }
  685 |   });
  686 | }
  687 | 
```