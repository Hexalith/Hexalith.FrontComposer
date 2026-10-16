# Instructions

- Following Playwright test failed.
- Explain why, be concise, respect Playwright best practices.
- Provide a snippet of code with the fix, if possible.

# Test info

- Name: responsive-accessibility.spec.ts >> Story 13.6 responsive operator surfaces >> type: 320 CSS-pixel reflow with all text-spacing overrides
- Location: specs/responsive-accessibility.spec.ts:306:7

# Error details

```
Error: AE-05 focused visual geometry

AE-05 focused visual geometry

expect(received).toEqual(expected) // deep equality

- Expected  - 1
+ Received  + 3

- Array []
+ Array [
+   "app content obscures target or focus ring: {\"target\":\"DIV#\",\"requireIndicator\":true,\"box\":{\"left\":27,\"top\":621.63330078125,\"right\":293,\"bottom\":848.63330078125},\"ring\":1,\"failures\":[\"app content obscures target or focus ring\"],\"occluder\":\"DIV# by SPAN# at 103.76667022705078,621.88330078125\"}",
+ ]

Call Log:
- Timeout 10000ms exceeded while waiting on the predicate
```

# Page snapshot

```yaml
- generic [ref=e2]:
  - link "Skip to content" [ref=e3] [cursor=pointer]:
    - /url: /__frontcomposer/specimens/type#fc-main-content
  - link "Skip to navigation" [ref=e4] [cursor=pointer]:
    - /url: /__frontcomposer/specimens/type#fc-nav
  - generic [ref=e6]:
    - banner [ref=e7]:
      - generic [ref=e8]:
        - generic [ref=e9]:
          - button "Toggle navigation" [ref=e12] [cursor=pointer]
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
                - /url: /__frontcomposer/specimens/type?theme=light&density=compact
              - link "comfortable" [ref=e55] [cursor=pointer]:
                - /url: /__frontcomposer/specimens/type?theme=light&density=comfortable
              - link "roomy" [ref=e56] [cursor=pointer]:
                - /url: /__frontcomposer/specimens/type?theme=light&density=roomy
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
  413 |         if (alpha > 0 || css.backgroundImage !== 'none' || hit.matches('svg,path,img,video,canvas')) return true;
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