# Instructions

- Following Playwright test failed.
- Explain why, be concise, respect Playwright best practices.
- Provide a snippet of code with the fix, if possible.

# Test info

- Name: accessibility-state-matrix.spec.ts >> shell chrome, theme menu and exact desktop navigation rails in dark
- Location: specs/accessibility-state-matrix.spec.ts:462:3

# Error details

```
Test timeout of 60000ms exceeded.
```

```
Error: locator.evaluate: Test timeout of 60000ms exceeded.
Call log:
  - waiting for getByTestId('fc-shell-navigation').getByTestId('fc-navigation-rail')

```

# Page snapshot

```yaml
- generic [ref=e2]:
  - link "Skip to content" [ref=e3] [cursor=pointer]:
    - /url: /counter#fc-main-content
  - link "Skip to navigation" [ref=e4] [cursor=pointer]:
    - /url: /counter#fc-nav
  - generic [ref=e6]:
    - banner [ref=e7]:
      - generic [ref=e8]:
        - generic [ref=e9]:
          - button "Toggle navigation" [ref=e11] [cursor=pointer]
          - generic [ref=e15]: Hexalith FrontComposer
        - generic [ref=e16]:
          - button "Change theme" [active] [ref=e18] [cursor=pointer]:
            - generic [ref=e19]:
              - generic:
                - generic [ref=e20]: "Change theme: Dark"
                - generic [aria-hidden] [ref=e21]: Dark
          - button "Open command palette" [ref=e25] [cursor=pointer]
          - button "Open settings" [ref=e29] [cursor=pointer]
          - button "FU" [ref=e34] [cursor=pointer]
    - navigation "Primary navigation" [ref=e38]:
      - generic [ref=e39]:
        - button "Counter" [ref=e40] [cursor=pointer]
        - button "More Counter pages" [ref=e47] [cursor=pointer]
        - button "Default" [ref=e54] [cursor=pointer]
        - button "Specimens" [ref=e60] [cursor=pointer]
        - button "More Specimens pages" [ref=e67] [cursor=pointer]
    - generic [ref=e74]:
      - status [ref=e75]: Density set to Compact.
      - main [ref=e76]:
        - status [ref=e77]
        - status [ref=e78]
        - generic [ref=e79]:
          - heading "Counter" [level=1] [ref=e80]
          - generic "Counter pages" [ref=e81]:
            - tablist [ref=e82]:
              - generic:
                - tab "Overview" [selected] [ref=e83] [cursor=pointer]
                - tab "Projection" [ref=e85] [cursor=pointer]
            - tabpanel "Overview" [ref=e87]:
              - generic [ref=e89]:
                - generic:
                  - generic [ref=e90]:
                    - heading [level=2] [ref=e91]:
                      - button "Create with an exact target" [expanded] [ref=e92] [cursor=pointer]
                    - region "Create with an exact target" [ref=e95]:
                      - group [ref=e97]:
                        - generic "Create Counter command form" [ref=e98]:
                          - generic [ref=e99]:
                            - generic [ref=e100]:
                              - generic [ref=e102]:
                                - generic: Counter key
                                - generic "Counter key" [ref=e104]:
                                  - textbox "Counter key, Required" [ref=e106]
                              - generic [ref=e108]:
                                - generic: Initial Value
                                - generic "Initial Value" [ref=e110]:
                                  - textbox "Initial Value, Required" [ref=e112]: "0"
                              - button "Create Counter" [ref=e113] [cursor=pointer]
                            - status [ref=e115]
                  - generic [ref=e116]:
                    - heading [level=2] [ref=e117]:
                      - button "Update with an exact target" [expanded] [ref=e118] [cursor=pointer]
                    - region "Update with an exact target" [ref=e121]:
                      - group [ref=e123]:
                        - generic "Update Counter command form" [ref=e124]:
                          - generic [ref=e125]:
                            - generic [ref=e126]:
                              - generic [ref=e128]:
                                - generic: Counter key
                                - generic "Counter key" [ref=e130]:
                                  - textbox "Counter key, Required" [ref=e132]
                              - generic [ref=e134]:
                                - generic: Amount
                                - generic "Amount" [ref=e136]:
                                  - textbox "Amount, Required" [ref=e138]: "1"
                              - button "Update Counter" [ref=e139] [cursor=pointer]
                            - status [ref=e141]
                  - generic [ref=e142]:
                    - heading [level=2] [ref=e143]:
                      - button "Compact inline (3 fields)" [expanded] [ref=e144] [cursor=pointer]
                    - region "Compact inline (3 fields)" [ref=e147]:
                      - group [ref=e149]:
                        - generic "Batch Increment command form" [ref=e150]:
                          - generic [ref=e151]:
                            - generic [ref=e152]:
                              - generic [ref=e154]:
                                - generic: Amount
                                - generic "Amount" [ref=e156]:
                                  - textbox "Amount, Required" [ref=e158]: "1"
                              - generic [ref=e160]:
                                - generic: Note
                                - generic "Note" [ref=e162]:
                                  - textbox "Note, Required" [ref=e164]
                              - generic [ref=e166]:
                                - generic: Effective Date
                                - generic "Effective Date" [ref=e168]:
                                  - generic [ref=e169]:
                                    - textbox "Effective Date" [ref=e170]:
                                      - /placeholder: M/d/yyyy
                                      - text: 10/18/2026
                                    - button "Show date picker" [ref=e172]
                              - button "Batch Increment" [ref=e174] [cursor=pointer]
                            - status [ref=e176]
                  - generic [ref=e177]:
                    - heading [level=2] [ref=e178]:
                      - button "Inline + popover (1 field)" [expanded] [ref=e179] [cursor=pointer]
                    - region "Inline + popover (1 field)" [ref=e182]:
                      - button "Increment" [ref=e183] [cursor=pointer]
                  - generic [ref=e187]:
                    - heading [level=2] [ref=e188]:
                      - button "Full-page command (5 fields)" [expanded] [ref=e189] [cursor=pointer]
                    - region "Full-page command (5 fields)" [ref=e192]:
                      - generic:
                        - note [ref=e193]: Navigation state persistence (scroll, filter, sort across FullPage round-trip) lands in Story 4.3. The current demo proves routing + breadcrumb return only.
                        - link [ref=e194] [cursor=pointer]:
                          - /url: /commands/Counter/ConfigureCounterCommand?returnPath=%2Fcounter&projectionTypeFqn=Counter.Domain.CounterProjection
                          - generic [ref=e195]:
                            - generic:
                              - link [ref=e196]:
                                - /url: /commands/Counter/ConfigureCounterCommand?returnPath=%2Fcounter&projectionTypeFqn=Counter.Domain.CounterProjection
                              - text: Configure Counter
          - status [ref=e197]
    - generic [ref=e198]: Hexalith FrontComposer © 2026
```

# Test source

```ts
  920  |       }
  921  |       return false;
  922  |     };
  923  |     const check = (owner: Element, initial: PaintBox[]) => {
  924  |       let boxes = initial.filter(box => box.right > box.left && box.bottom > box.top);
  925  |       for (let current: Element | null = owner; current && boxes.length; current = parent(current)) {
  926  |         const css = getComputedStyle(current), bounds = current.getBoundingClientRect();
  927  |         const explicit = audit.clip(css, bounds);
  928  |         if (explicit.kind === 'unsupported') failures.add(`${identify(current)}: text clip/mask needs pixel evidence`);
  929  |         if (explicit.kind === 'hidden') { failures.add(identify(current)); break; }
  930  |         const clip = explicit.box;
  931  |         if (clip) {
  932  |           if (boxes.some(box => box.left < clip.left - 1 || box.right > clip.right + 1 || box.top < clip.top - 1 || box.bottom > clip.bottom + 1)) failures.add(identify(current));
  933  |           boxes = boxes.map(box => ({ left: Math.max(box.left, clip.left), right: Math.min(box.right, clip.right), top: Math.max(box.top, clip.top), bottom: Math.min(box.bottom, clip.bottom) }));
  934  |         }
  935  |         if (!['inline', 'contents'].includes(css.display)) {
  936  |           const left = bounds.left + parseFloat(css.borderLeftWidth), right = bounds.right - parseFloat(css.borderRightWidth);
  937  |           const top = bounds.top + parseFloat(css.borderTopWidth), bottom = bounds.bottom - parseFloat(css.borderBottomWidth);
  938  |           if (boxes.some(box => /(hidden|clip)/u.test(css.overflowX) && (box.left < left - 1 || box.right > right + 1)
  939  |             || /(hidden|clip)/u.test(css.overflowY) && (box.top < top - 1 || box.bottom > bottom + 1))) failures.add(identify(current));
  940  |           // A nested scrollport owns its offscreen content. Only its visible portion
  941  |           // can be clipped again by an outer owner, including across shadow slots.
  942  |           boxes = boxes.map(box => ({
  943  |             left: /(auto|scroll|hidden|clip)/u.test(css.overflowX) ? Math.max(box.left, left) : box.left,
  944  |             right: /(auto|scroll|hidden|clip)/u.test(css.overflowX) ? Math.min(box.right, right) : box.right,
  945  |             top: /(auto|scroll|hidden|clip)/u.test(css.overflowY) ? Math.max(box.top, top) : box.top,
  946  |             bottom: /(auto|scroll|hidden|clip)/u.test(css.overflowY) ? Math.min(box.bottom, bottom) : box.bottom,
  947  |           }));
  948  |         }
  949  |         boxes = boxes.filter(box => box.right > box.left && box.bottom > box.top);
  950  |         // Native top-layer surfaces escape outer clips, but retain their own checks.
  951  |         if (current.matches(':popover-open,:modal')) break;
  952  |       }
  953  |     };
  954  |     const visit = (node: Node) => {
  955  |       if (visited.has(node)) return;
  956  |       visited.add(node);
  957  |       if (node instanceof Text) {
  958  |         const owner = parent(node);
  959  |         if (!node.textContent?.trim() || !owner || excluded(owner)) return;
  960  |         const range = document.createRange(); range.selectNodeContents(node);
  961  |         check(owner, [...range.getClientRects()]);
  962  |       } else if (node instanceof Element) {
  963  |         if (excluded(node)) return;
  964  |         if ((node instanceof HTMLInputElement && ['text','search','email','url','tel','password','number'].includes(node.type) || node instanceof HTMLTextAreaElement || node instanceof HTMLSelectElement)
  965  |           && (node instanceof HTMLSelectElement ? node.selectedOptions.length > 0 : node.value || node.placeholder)) {
  966  |           const css = getComputedStyle(node), bounds = node.getBoundingClientRect();
  967  |           if (bounds.width && bounds.height) {
  968  |             const context = document.createElement('canvas').getContext('2d')!;
  969  |             context.font = css.font;
  970  |             const glyph = context.measureText(node instanceof HTMLSelectElement ? [...node.selectedOptions].map(option => option.label).join(' ') : node.value || node.placeholder);
  971  |             const height = glyph.actualBoundingBoxAscent + glyph.actualBoundingBoxDescent;
  972  |             const available = bounds.height - parseFloat(css.borderTopWidth) - parseFloat(css.borderBottomWidth) - parseFloat(css.paddingTop) - parseFloat(css.paddingBottom);
  973  |             if (height > available + 1) failures.add(`${identify(node)}: native value is vertically clipped`);
  974  |             // Native values scroll horizontally by design; their painted viewport
  975  |             // still participates in outer composed clipping.
  976  |             check(node, [{ left: bounds.left + parseFloat(css.borderLeftWidth), top: bounds.top + parseFloat(css.borderTopWidth), right: bounds.right - parseFloat(css.borderRightWidth), bottom: bounds.bottom - parseFloat(css.borderBottomWidth) }]);
  977  |           }
  978  |         }
  979  |         for (const pseudo of ['::before', '::after']) {
  980  |           const generated = getComputedStyle(node, pseudo);
  981  |           if (!generated.content || ['none','normal','""',"''"].includes(generated.content) || generated.display === 'none' || generated.visibility === 'hidden' || Number(generated.opacity) === 0) continue;
  982  |           const visualContent = generated.content.match(/^("(?:\\.|[^"\\])*"|'(?:\\.|[^'\\])*')(?:\s*\/.*)?$/u)?.[1];
  983  |           if (visualContent && ['""', "''"].includes(visualContent)) continue;
  984  |           if (/(hidden|clip)/u.test(generated.overflowX + generated.overflowY)) {
  985  |             failures.add(`${identify(node)}: generated text clipping needs pixel evidence`);
  986  |             continue;
  987  |           }
  988  |           // Supported normal-flow glyphs fit their host's scroll envelope. Use that
  989  |           // conservative envelope through composed clips, including required markers.
  990  |           if (visualContent && generated.position === 'static' && generated.transform === 'none' && generated.cssFloat === 'none'
  991  |             && node.clientWidth > 0 && node.clientHeight > 0 && node.scrollWidth <= node.clientWidth + .5 && node.scrollHeight <= node.clientHeight + .5
  992  |             && [generated.marginTop, generated.marginRight, generated.marginBottom, generated.marginLeft].every(value => Number.isFinite(parseFloat(value)) && parseFloat(value) >= 0)) {
  993  |             check(node, [node.getBoundingClientRect()]);
  994  |             continue;
  995  |           }
  996  |           for (let owner: Element | null = node; owner; owner = parent(owner)) {
  997  |             const css = getComputedStyle(owner);
  998  |             if (/(hidden|clip)/u.test(css.overflowX + css.overflowY) || audit.clip(css, owner.getBoundingClientRect()).kind !== 'none') {
  999  |               failures.add(`${identify(owner)}: generated text clipping needs pixel evidence`); break;
  1000 |             }
  1001 |             if (owner.matches(':popover-open,:modal')) break;
  1002 |           }
  1003 |         }
  1004 |         if (node instanceof HTMLSlotElement) for (const child of node.assignedNodes({ flatten: true })) visit(child);
  1005 |         if (node.shadowRoot) for (const child of node.shadowRoot.childNodes) visit(child);
  1006 |         for (const child of node.childNodes) visit(child);
  1007 |       }
  1008 |     };
  1009 |     visit(document.documentElement);
  1010 |     return [...failures];
  1011 |   });
  1012 |   expect(failures, 'AE-03 clipped text ranges').toEqual([]);
  1013 | }
  1014 | 
  1015 | export type ContrastKind = 'text' | 'border' | 'icon' | 'focus' | 'cue';
  1016 | 
  1017 | /** Sample actual painted nodes in the composed tree; empty/unsupported samples fail closed. */
  1018 | export async function measureSurfaceContrast(root: Locator, kind: ContrastKind = 'text') {
  1019 |   await preparePaintAudit(root);
> 1020 |   return root.evaluate((scope, sampleKind) => {
       |               ^ Error: locator.evaluate: Test timeout of 60000ms exceeded.
  1021 |     const parent = (node: Element): Element | null => node.assignedSlot ?? node.parentElement ?? (node.getRootNode() instanceof ShadowRoot ? (node.getRootNode() as ShadowRoot).host : null);
  1022 |     const audit = (window as typeof window & { __aePaintAudit: PaintAudit }).__aePaintAudit;
  1023 |     const parse = audit.parse;
  1024 |     const resting = sampleKind === 'focus' ? audit.resting(scope) : new Map();
  1025 |     const composite = (fg: number[], bg: number[]) => [0, 1, 2].map(i => fg[i] * fg[3] + bg[i] * (1 - fg[3])).concat(1);
  1026 |     const background = (element: Element | null) => {
  1027 |       const layers: number[][] = [];
  1028 |       for (let current = element; current; current = parent(current)) {
  1029 |         const css = getComputedStyle(current);
  1030 |         if (css.backgroundImage !== 'none') throw new Error('Background image needs pixel evidence');
  1031 |         if (css.filter !== 'none' || css.mixBlendMode !== 'normal' || css.getPropertyValue('backdrop-filter') && css.getPropertyValue('backdrop-filter') !== 'none') throw new Error('Filtered or blended paint needs pixel evidence');
  1032 |         if (Number(css.opacity) > 0 && Number(css.opacity) < 1) throw new Error(`Translucent group needs pixel evidence: ${current.tagName}.${current.className} opacity=${css.opacity}`);
  1033 |         layers.push(parse(css.backgroundColor));
  1034 |       }
  1035 |       const probe = document.createElement('span'); probe.style.color = 'Canvas'; document.documentElement.append(probe);
  1036 |       const canvas = parse(getComputedStyle(probe).color); probe.remove();
  1037 |       return layers.reverse().reduce((bg, fg) => composite(fg, bg), canvas);
  1038 |     };
  1039 |     const luminance = (color: number[]) => color.slice(0, 3).map(channel => { const c = channel / 255; return c <= .04045 ? c / 12.92 : ((c + .055) / 1.055) ** 2.4; }).reduce((sum, c, i) => sum + c * [.2126, .7152, .0722][i], 0);
  1040 |     const visible = (element: Element) => {
  1041 |       if (element.getClientRects().length === 0 && getComputedStyle(element).display !== 'contents') return false;
  1042 |       for (let current: Element | null = element; current; current = parent(current)) {
  1043 |         const css = getComputedStyle(current);
  1044 |         if (sampleKind === 'icon' && current.matches('fluent-skeleton,.fluent-skeleton,.fc-empty-state-body__icon')) return false;
  1045 |         if (audit.clip(css, current.getBoundingClientRect()).kind === 'hidden') return false;
  1046 |         if (css.visibility === 'hidden' || css.display === 'none' || Number(css.opacity) === 0 || current.matches('[hidden],.fc-sr-only,.fc-skip-link:not(:focus),[disabled],[aria-disabled="true"]')) return false;
  1047 |       }
  1048 |       return true;
  1049 |     };
  1050 |     const samples: { owner: number; node: string; paint: string; foreground: number[]; background: number[]; ratio: number; minimum: number; measurement?: 'conservative-bound'; shadowBound?: ShadowContrastBound }[] = [];
  1051 |     const owners = new Map<Element, number>();
  1052 |     const controlOwner = (element: Element) => {
  1053 |       let nearest: Element | null = null;
  1054 |       for (let current: Element | null = element; current; current = parent(current)) {
  1055 |         if (current.matches('fluent-button,fluent-text-input,fluent-number-input,fluent-dropdown,fluent-combobox,fluent-select,fluent-radio,fluent-switch')) return current;
  1056 |         if (!nearest && current.matches('button,input,select,textarea,a[href],[role="button"],[role="tab"],[role="radio"],[role="option"]')) nearest = current;
  1057 |         if (current === scope) break;
  1058 |       }
  1059 |       return nearest ?? element;
  1060 |     };
  1061 |     const pseudoEdges = new Map<Element, PaintBox>();
  1062 |     const pseudoOutside = (element: Element, css: CSSStyleDeclaration, side: string) => {
  1063 |       pseudoEdges.delete(element);
  1064 |       if (!['absolute','fixed'].includes(css.position)) {
  1065 |         if ([css.marginTop,css.marginRight,css.marginBottom,css.marginLeft].some(value => parseFloat(value) < 0)) throw new Error('Flow pseudo border adjacency needs pixel evidence');
  1066 |         const clipping = audit.clip(css, element.getBoundingClientRect());
  1067 |         if (clipping.kind === 'hidden') return null;
  1068 |         if (clipping.kind !== 'none') throw new Error('Clipped pseudo border needs pixel evidence');
  1069 |         return false;
  1070 |       }
  1071 |       let containing: Element | null = css.position === 'fixed' ? null : element;
  1072 |       while (containing && getComputedStyle(containing).position === 'static' && getComputedStyle(containing).transform === 'none') containing = parent(containing);
  1073 |       if (containing && getComputedStyle(containing).transform !== 'none') {
  1074 |         const transform = new DOMMatrixReadOnly(getComputedStyle(containing).transform);
  1075 |         if (!transform.is2D || transform.a !== 1 || transform.d !== 1 || transform.b !== 0 || transform.c !== 0) throw new Error('Transformed pseudo border adjacency needs pixel evidence');
  1076 |       }
  1077 |       const bounds = containing?.getBoundingClientRect();
  1078 |       const base = containing && bounds ? { left: bounds.left + containing.clientLeft, top: bounds.top + containing.clientTop, width: containing.clientWidth, height: containing.clientHeight } : { left: 0, top: 0, width: innerWidth, height: innerHeight };
  1079 |       const px = (value: string) => /^-?[\d.]+px$/u.test(value) ? parseFloat(value) : null;
  1080 |       const width = px(css.width), height = px(css.height), left = px(css.left), right = px(css.right), top = px(css.top), bottom = px(css.bottom);
  1081 |       if (width === null || height === null || left === null && right === null || top === null && bottom === null) throw new Error('Unresolved pseudo border adjacency needs pixel evidence');
  1082 |       const extraX = css.boxSizing === 'border-box' ? 0 : parseFloat(css.paddingLeft) + parseFloat(css.paddingRight) + parseFloat(css.borderLeftWidth) + parseFloat(css.borderRightWidth);
  1083 |       const extraY = css.boxSizing === 'border-box' ? 0 : parseFloat(css.paddingTop) + parseFloat(css.paddingBottom) + parseFloat(css.borderTopWidth) + parseFloat(css.borderBottomWidth);
  1084 |       const matrix = new DOMMatrixReadOnly(css.transform === 'none' ? undefined : css.transform);
  1085 |       if (!matrix.is2D || matrix.a !== 1 || matrix.d !== 1 || matrix.b !== 0 || matrix.c !== 0) throw new Error('Transformed pseudo border adjacency needs pixel evidence');
  1086 |       const x = base.left + (left !== null ? left + parseFloat(css.marginLeft) : base.width - right! - width - extraX - parseFloat(css.marginRight)) + matrix.e;
  1087 |       const y = base.top + (top !== null ? top + parseFloat(css.marginTop) : base.height - bottom! - height - extraY - parseFloat(css.marginBottom)) + matrix.f;
  1088 |       const box = { left: x, top: y, right: x + width + extraX, bottom: y + height + extraY };
  1089 |       const clipping = audit.clip(css, box);
  1090 |       if (clipping.kind === 'hidden') return null;
  1091 |       if (clipping.kind === 'unsupported') throw new Error('Clipped pseudo border needs pixel evidence');
  1092 |       const thickness = parseFloat(css.getPropertyValue(`border-${side}-width`));
  1093 |       if (side === 'top') box.bottom = box.top + thickness;
  1094 |       if (side === 'bottom') box.top = box.bottom - thickness;
  1095 |       if (side === 'left') box.right = box.left + thickness;
  1096 |       if (side === 'right') box.left = box.right - thickness;
  1097 |       if (clipping.box) {
  1098 |         box.left = Math.max(box.left, clipping.box.left); box.right = Math.min(box.right, clipping.box.right);
  1099 |         box.top = Math.max(box.top, clipping.box.top); box.bottom = Math.min(box.bottom, clipping.box.bottom);
  1100 |         if (box.right <= box.left || box.bottom <= box.top) return null;
  1101 |       }
  1102 |       pseudoEdges.set(element, box);
  1103 |       const control = element.getBoundingClientRect();
  1104 |       if (box.left >= control.left - .5 && box.right <= control.right + .5 && box.top >= control.top - .5 && box.bottom <= control.bottom + .5) return false;
  1105 |       if (box.right <= control.left + .5 || box.left >= control.right - .5 || box.bottom <= control.top + .5 || box.top >= control.bottom - .5) return true;
  1106 |       throw new Error('Straddling pseudo border adjacency needs pixel evidence');
  1107 |     };
  1108 |     const pagePaint: Element[] = [];
  1109 |     const collectPaint = (root: Document | ShadowRoot) => {
  1110 |       for (const candidate of root.querySelectorAll('*')) { pagePaint.push(candidate); if (candidate.shadowRoot) collectPaint(candidate.shadowRoot); }
  1111 |     };
  1112 |     collectPaint(document);
  1113 |     const clippedPaint = (element: Element, boxes: PaintBox[], content = true): PaintBox[] => {
  1114 |       if (!boxes.length) return [];
  1115 |       for (let current: Element | null = element; current; current = parent(current)) {
  1116 |         const css = getComputedStyle(current);
  1117 |         if (css.filter !== 'none' || css.mixBlendMode !== 'normal' || css.getPropertyValue('backdrop-filter') && css.getPropertyValue('backdrop-filter') !== 'none') throw new Error('Filtered or blended paint needs pixel evidence');
  1118 |         const clipping = audit.clip(css, current.getBoundingClientRect());
  1119 |         if (clipping.kind === 'hidden') return [];
  1120 |         if (clipping.kind === 'unsupported') throw new Error('Clipped contrast paint needs pixel evidence');
```