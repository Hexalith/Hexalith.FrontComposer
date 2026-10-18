# Instructions

- Following Playwright test failed.
- Explain why, be concise, respect Playwright best practices.
- Provide a snippet of code with the fix, if possible.

# Test info

- Name: accessibility-state-matrix.spec.ts >> AE complete state matrix dark >> all eleven actual lifecycle wrapper states retain immediate text and system-color cues
- Location: specs/accessibility-state-matrix.spec.ts:273:5

# Error details

```
Error: Nonempty icon contrast inventory

expect(received).toBeGreaterThan(expected)

Expected: > 0
Received:   0
```

# Page snapshot

```yaml
- generic [ref=e2]:
  - link "Skip to content" [ref=e3] [cursor=pointer]:
    - /url: /__frontcomposer/specimens/type?generatedGrid=true&accessibilityScenarios=true&density=compact&theme=dark#fc-main-content
  - link "Skip to navigation" [ref=e4] [cursor=pointer]:
    - /url: /__frontcomposer/specimens/type?generatedGrid=true&accessibilityScenarios=true&density=compact&theme=dark#fc-nav
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
                - generic [ref=e21]: "Change theme: Dark"
                - generic [aria-hidden] [ref=e22]: Dark
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
                - /url: /__frontcomposer/specimens/type?theme=light&density=compact&generatedGrid=true&accessibilityScenarios=true
              - link "dark" [ref=e53] [cursor=pointer]:
                - /url: /__frontcomposer/specimens/type?theme=dark&density=compact&generatedGrid=true&accessibilityScenarios=true
              - link "compact" [ref=e54] [cursor=pointer]:
                - /url: /__frontcomposer/specimens/type?theme=dark&density=compact&generatedGrid=true&accessibilityScenarios=true
              - link "comfortable" [ref=e55] [cursor=pointer]:
                - /url: /__frontcomposer/specimens/type?theme=dark&density=comfortable&generatedGrid=true&accessibilityScenarios=true
              - link "roomy" [ref=e56] [cursor=pointer]:
                - /url: /__frontcomposer/specimens/type?theme=dark&density=roomy&generatedGrid=true&accessibilityScenarios=true
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
            - region "specimen status" [ref=e168]:
              - grid [ref=e169]:
                - rowgroup [ref=e170]:
                  - row [ref=e171]:
                    - columnheader "Id" [ref=e172]
                    - columnheader "Name" [ref=e176]
                    - columnheader "Status" [ref=e180]
                    - columnheader "Owner" [ref=e184]
                - rowgroup [ref=e188]:
                  - row [ref=e189]:
                    - gridcell "FC-1000" [ref=e190]
                    - gridcell "Neutral projection" [ref=e191]
                    - gridcell [ref=e192]:
                      - 'img "Status: Neutral" [ref=e193]'
                    - gridcell "Shell" [ref=e196]
                  - row [ref=e197]:
                    - gridcell "FC-1001" [ref=e198]
                    - gridcell "Info projection" [ref=e199]
                    - gridcell [ref=e200]:
                      - 'img "Status: Info" [ref=e201]'
                    - gridcell "SourceTools" [ref=e204]
                  - row [ref=e205]:
                    - gridcell "FC-1002" [ref=e206]
                    - gridcell "Success projection" [ref=e207]
                    - gridcell [ref=e208]:
                      - 'img "Status: Success" [ref=e209]'
                    - gridcell "Contracts" [ref=e212]
                  - row [ref=e213]:
                    - gridcell "FC-1003" [ref=e214]
                    - gridcell "Warning projection" [ref=e215]
                    - gridcell [ref=e216]:
                      - 'img "Status: Warning" [ref=e217]'
                    - gridcell "Testing" [ref=e220]
                  - row [ref=e221]:
                    - gridcell "FC-1004" [ref=e222]
                    - gridcell "Danger projection" [ref=e223]
                    - gridcell [ref=e224]:
                      - 'img "Status: Danger" [ref=e225]'
                    - gridcell "CI" [ref=e228]
                  - row [ref=e229]:
                    - gridcell "FC-1005" [ref=e230]
                    - gridcell "Accent projection" [ref=e231]
                    - gridcell [ref=e232]:
                      - 'img "Status: Accent" [ref=e233]'
                    - gridcell "Docs" [ref=e236]
              - region "Details for"
          - region "Automated accessibility scenarios" [ref=e237]:
            - heading "Fixed accessibility scenarios" [level=2] [ref=e238]
            - generic [ref=e239]:
              - generic [ref=e240] [cursor=pointer]: validation
              - generic [ref=e242] [cursor=pointer]: returned-rejection
              - generic [ref=e244] [cursor=pointer]: exception-rejection
              - generic [ref=e246] [cursor=pointer]: forbidden
              - generic [ref=e248] [cursor=pointer]: unavailable
              - generic [ref=e250] [cursor=pointer]: blank-unavailable
              - generic [ref=e252] [cursor=pointer]: rate-limited
              - generic [ref=e254] [cursor=pointer]: blank-rate-limited
              - generic [ref=e256] [cursor=pointer]: transport
              - generic [ref=e258] [cursor=pointer]: admission
              - generic [ref=e260] [cursor=pointer]: policy
              - generic [ref=e262] [cursor=pointer]: accepted
            - paragraph [ref=e264]: validation
            - generic [ref=e265]:
              - button "Run accessibility specimen" [ref=e266] [cursor=pointer]
              - status [ref=e270]
            - status "New item added outside current filters" [ref=e272]: New item. It may not match current filters yet.
            - generic [ref=e273]:
              - generic [ref=e274] [cursor=pointer]: Idle
              - generic [ref=e276] [cursor=pointer]: Submitting
              - generic [ref=e278] [cursor=pointer]: Acknowledged
              - generic [ref=e280] [cursor=pointer]: Syncing
              - generic [active] [ref=e282] [cursor=pointer]: Confirmed
              - generic [ref=e284] [cursor=pointer]: Rejected
              - generic [ref=e286] [cursor=pointer]: IdempotentConfirmed
              - generic [ref=e288] [cursor=pointer]: NeedsReview
              - generic [ref=e290] [cursor=pointer]: Warning
              - generic [ref=e292] [cursor=pointer]: Degraded
              - generic [ref=e294] [cursor=pointer]: DegradedExhausted
            - generic [ref=e296]:
              - generic: Observable lifecycle specimen
              - status [ref=e297]
            - generic [ref=e298]:
              - generic [ref=e299] [cursor=pointer]: Connected
              - generic [ref=e301] [cursor=pointer]: Reconnecting
              - generic [ref=e303] [cursor=pointer]: Disconnected
              - generic [ref=e305] [cursor=pointer]: Start held page
              - generic [ref=e307] [cursor=pointer]: Complete held page
              - generic [ref=e309] [cursor=pointer]: Complete capped page
              - generic [ref=e311] [cursor=pointer]: Apply no-match filter
              - generic [ref=e313] [cursor=pointer]: Reset filter
            - generic [ref=e315]:
              - generic [ref=e316] [cursor=pointer]: Load two rows
              - generic [ref=e318] [cursor=pointer]: Complete missing row result
              - generic [ref=e320] [cursor=pointer]: Complete empty result
              - generic [ref=e322] [cursor=pointer]: Complete failed result
              - generic [ref=e324] [cursor=pointer]: Start pending result
              - generic [ref=e326] [cursor=pointer]: Complete delayed missing result
              - generic [ref=e328] [cursor=pointer]: Change fixed tenant
              - generic [ref=e330] [cursor=pointer]: Change fixed user
              - generic [ref=e332] [cursor=pointer]: Repeat completed result
          - region [ref=e334]:
            - heading "Generated Counter projection grid" [level=2] [ref=e335]
            - status [ref=e336]
            - paragraph [ref=e337]: 1 counters
            - text: 1 counters
            - region "counters" [ref=e338]:
              - grid [ref=e339]:
                - rowgroup [ref=e340]:
                  - row [ref=e341]:
                    - columnheader "Id" [ref=e342]
                    - columnheader "Count" [ref=e346]
                    - columnheader "Last changed" [ref=e350]
                    - columnheader "Metadata (custom renderer needed)" [ref=e354]
                - rowgroup [ref=e358]:
                  - row [ref=e359]:
                    - gridcell "fc-correlation-0002" [ref=e360]
                    - gridcell "42" [ref=e361]
                    - gridcell "1/15/2026" [ref=e362]
                    - gridcell [ref=e363]:
                      - generic "Metadata with unsupported type System.Collections.Generic.Dictionary requires a custom renderer" [ref=e364]:
                        - strong [ref=e365]: Metadata
                        - text: (System.Collections.Generic.Dictionary) This field requires a custom renderer.
                        - link "Learn how to customize this field" [ref=e366] [cursor=pointer]:
                          - /url: https://hexalith.github.io/FrontComposer/diagnostics/HFC1002
              - region "Details for"
          - region [ref=e367]:
            - heading "Command form and lifecycle states" [level=2] [ref=e368]
            - form "Specimen command form" [ref=e369]:
              - generic [ref=e370]:
                - text: Counter name
                - textbox "Counter name" [ref=e371]: Specimen counter
              - generic [ref=e372]:
                - text: Amount
                - spinbutton "Amount" [ref=e373]: "3"
              - button "Submit command" [ref=e374]
              - button "Cancel" [ref=e375]
            - list "Five command lifecycle states" [ref=e376]:
              - listitem [ref=e377]:
                - strong [ref=e378]: Idle
                - text: Ready to submit.
              - listitem [ref=e379]:
                - strong [ref=e380]: Submitting
                - text: Submitting command.
              - listitem [ref=e381]:
                - strong [ref=e382]: Acknowledged
                - text: Command accepted by the host.
              - listitem [ref=e383]:
                - strong [ref=e384]: Syncing
                - text: Waiting for projection synchronization.
              - listitem [ref=e385]:
                - strong [ref=e386]: Confirmed or rejected
                - text: Terminal confirmation and rejection states remain text-visible.
          - region [ref=e387]:
            - heading "Destructive command confirmation" [level=2] [ref=e388]
            - group [ref=e390]:
              - generic "Purge Specimen Record command form" [ref=e391]:
                - generic [ref=e392]:
                  - generic [ref=e393]:
                    - group "Purge details" [ref=e394]:
                      - generic [ref=e396]:
                        - generic [ref=e397]:
                          - generic: Record Id
                          - generic "Record Id" [ref=e399]:
                            - textbox "Record Id, Required" [ref=e401]: FC-1002
                        - generic: Record to purge.
                      - generic [ref=e402]:
                        - generic [ref=e403]:
                          - generic: Reason
                          - generic "Reason" [ref=e405]:
                            - textbox "Reason, Required" [ref=e407]: QA destructive confirmation coverage
                        - generic: Why this purge is required.
                    - generic [ref=e408]:
                      - generic [ref=e409]:
                        - generic: Retention
                        - generic "Retention" [ref=e411] [cursor=pointer]:
                          - combobox "Retention" [ref=e413]
                      - generic: How long the purged record stays recoverable.
                    - button "Purge Specimen Record" [ref=e417] [cursor=pointer]
                  - status [ref=e419]
          - region [ref=e420]:
            - heading "Policy-gated command authorization" [level=2] [ref=e421]
            - group [ref=e423]:
              - generic "Policy Allowed Specimen command form" [ref=e424]:
                - generic [ref=e425]:
                  - generic [ref=e426]:
                    - generic [ref=e428]:
                      - generic: Record Id
                      - generic "Record Id" [ref=e430]:
                        - textbox "Record Id, Required" [ref=e432]: FC-AUTH-ALLOW
                    - generic [ref=e434]:
                      - generic: Reason
                      - generic "Reason" [ref=e436]:
                        - textbox "Reason, Required" [ref=e438]: QA policy allowed coverage
                    - button "Policy Allowed Specimen" [ref=e439] [cursor=pointer]
                  - status [ref=e441]
            - group [ref=e442]:
              - group [ref=e443]:
                - heading "Permission required" [level=2] [ref=e444]
                - paragraph [ref=e445]: You do not have permission to Policy Denied Specimen.
          - region [ref=e446]:
            - heading "Expanded detail view" [level=2] [ref=e447]
            - group [ref=e448]:
              - generic "Open projection details for FC-1002" [ref=e449]
              - generic [ref=e450]:
                - term [ref=e451]: Correlation
                - definition [ref=e452]: fc-correlation-0002
                - term [ref=e453]: Last synchronized
                - definition [ref=e454]: 2026-01-15 13:45 UTC
          - navigation "Specimen multi-level navigation" [ref=e455]:
            - heading "Multi-level navigation" [level=2] [ref=e456]
            - list [ref=e457]:
              - listitem [ref=e458]:
                - link "Foundation" [ref=e459] [cursor=pointer]:
                  - /url: "#fc-type-ramp-title"
              - listitem [ref=e460]:
                - button "Generated surfaces" [expanded] [ref=e461]
                - list [ref=e462]:
                  - listitem [ref=e463]:
                    - link "Projection grid" [ref=e464] [cursor=pointer]:
                      - /url: "#fc-badge-grid-title"
                  - listitem [ref=e465]:
                    - link "Command lifecycle" [ref=e466] [cursor=pointer]:
                      - /url: "#fc-specimen-command-title"
    - generic [ref=e467]: Hexalith FrontComposer © 2026
```

# Test source

```ts
  1382 |           const large = parseFloat(generated.fontSize) >= 24 || parseFloat(generated.fontSize) >= 18.667 && parseInt(generated.fontWeight) >= 700;
  1383 |           sample(element, glyphPaint(generated), `${pseudo} generated text`, large ? 3 : 4.5, false, Number(generated.opacity));
  1384 |         }
  1385 |       } else if (sampleKind === 'icon' && element.matches('svg path,svg polygon,svg circle,svg rect,svg ellipse,svg line,svg polyline,svg use,svg text')) {
  1386 |         if (element.matches('use')) throw new Error('SVG use instance needs pixel evidence');
  1387 |         for (const property of ['fill', 'stroke']) {
  1388 |           const paint = css.getPropertyValue(property);
  1389 |           if (property === 'stroke' && parseFloat(css.strokeWidth) <= 0 || property === 'fill' && element.matches('line')) continue;
  1390 |           if (paint !== 'none') {
  1391 |             const opacity = css.getPropertyValue(`${property}-opacity`), bounds = element.getBoundingClientRect();
  1392 |             if (property === 'fill' && (!bounds.width || !bounds.height || !filledArea(element))) continue;
  1393 |             let boxes: PaintBox[] | undefined;
  1394 |             if (property === 'stroke') {
  1395 |               if (css.strokeDasharray !== 'none') throw new Error('SVG dashed stroke needs pixel evidence');
  1396 |               const matrix = (element as SVGGraphicsElement).getScreenCTM();
  1397 |               if (!matrix) throw new Error('Unresolved SVG stroke envelope needs pixel evidence');
  1398 |               const half = parseFloat(css.strokeWidth) / 2, fixed = css.vectorEffect === 'non-scaling-stroke';
  1399 |               const x = fixed ? half : half * Math.hypot(matrix.a, matrix.c), y = fixed ? half : half * Math.hypot(matrix.b, matrix.d);
  1400 |               boxes = [{ left: bounds.left - x, right: bounds.right + x, top: bounds.top - y, bottom: bounds.bottom + y }];
  1401 |             }
  1402 |             sample(element, paint, `icon ${property}`, 3, false, parseFloat(opacity) / (opacity.endsWith('%') ? 100 : 1), boxes);
  1403 |           }
  1404 |         }
  1405 |       } else if (sampleKind === 'border') {
  1406 |         for (const pseudo of [null, '::before', '::after']) {
  1407 |           const edge = getComputedStyle(element, pseudo);
  1408 |           if (pseudo && (edge.content === 'none' || edge.visibility === 'hidden' || Number(edge.opacity) === 0)) continue;
  1409 |           if (edge.transform !== 'none') { const matrix = new DOMMatrixReadOnly(edge.transform); if (matrix.a === 0 || matrix.d === 0) continue; }
  1410 |           audit.composition(edge);
  1411 |           if (pseudo && (parse(edge.backgroundColor)[3] > 0 || edge.backgroundImage !== 'none')) throw new Error('Pseudo border background composition needs pixel evidence');
  1412 |           for (const side of ['top', 'right', 'bottom', 'left']) if (parseFloat(edge.getPropertyValue(`border-${side}-width`)) > 0 && !['none', 'hidden'].includes(edge.getPropertyValue(`border-${side}-style`))) {
  1413 |             const outside = pseudo ? pseudoOutside(element, edge, side) : true;
  1414 |             if (outside !== null) sample(element, edge.getPropertyValue(`border-${side}-color`), `${pseudo ?? 'element'} border-${side}`, 3, outside, Number(edge.opacity));
  1415 |           }
  1416 |         }
  1417 |       } else if (sampleKind === 'focus' || sampleKind === 'cue') {
  1418 |         for (const pseudo of [null, '::before', '::after']) {
  1419 |           const paint = getComputedStyle(element, pseudo);
  1420 |           if (paint.visibility === 'hidden' || paint.display === 'none' || Number(paint.opacity) === 0 || pseudo && paint.content === 'none') continue;
  1421 |           if (paint.transform !== 'none') { const matrix = new DOMMatrixReadOnly(paint.transform); if (matrix.a === 0 || matrix.d === 0) continue; }
  1422 |           const qualifies = (keys: string[]) => {
  1423 |             const changed = sampleKind === 'cue' || audit.changed(element, pseudo, paint, resting, keys);
  1424 |             if (changed) {
  1425 |               audit.composition(paint);
  1426 |               if (pseudo && (parse(paint.backgroundColor)[3] > 0 || paint.backgroundImage !== 'none')) throw new Error('Pseudo focus background composition needs pixel evidence');
  1427 |             }
  1428 |             return changed;
  1429 |           };
  1430 |           if (qualifies(['outline-width','outline-style','outline-color','outline-offset']) && parseFloat(paint.outlineWidth) > 0 && paint.outlineStyle !== 'none') {
  1431 |             if (pseudo && audit.clip(paint, element.getBoundingClientRect()).kind !== 'none') throw new Error('Clipped pseudo focus paint needs pixel evidence');
  1432 |             const offset = parseFloat(paint.outlineOffset);
  1433 |             if (offset < 0 && offset + parseFloat(paint.outlineWidth) > 0) throw new Error('Straddling outline adjacency needs pixel evidence');
  1434 |             let outlineBoxes: PaintBox[] | undefined;
  1435 |             if (offset >= 0) {
  1436 |               const extent = offset + parseFloat(paint.outlineWidth);
  1437 |               outlineBoxes = (pseudo ? pseudoPaint(element, paint) : [element.getBoundingClientRect()]).flatMap(rect => [
  1438 |                 { left: rect.left - extent, right: rect.right + extent, top: rect.top - extent, bottom: rect.top - offset },
  1439 |                 { left: rect.left - extent, right: rect.right + extent, top: rect.bottom + offset, bottom: rect.bottom + extent },
  1440 |                 { left: rect.left - extent, right: rect.left - offset, top: rect.top - offset, bottom: rect.bottom + offset },
  1441 |                 { left: rect.right + offset, right: rect.right + extent, top: rect.top - offset, bottom: rect.bottom + offset },
  1442 |               ]);
  1443 |             }
  1444 |             sample(element, paint.outlineColor, `${pseudo ?? 'element'} outline`, 3, offset >= 0, Number(paint.opacity), outlineBoxes);
  1445 |           }
  1446 |           if (qualifies(['box-shadow'])) for (const shadow of audit.shadows(element, pseudo, paint, resting)) {
  1447 |             if (/\binset\b/u.test(shadow)) continue;
  1448 |             if (pseudo && audit.clip(paint, element.getBoundingClientRect()).kind !== 'none') throw new Error('Clipped pseudo focus paint needs pixel evidence');
  1449 |             const lengths = shadow.replace(/[a-z][a-z0-9-]*\([^()]*\)/giu, '').match(/-?[\d.]+px/gu)?.map(Number.parseFloat) ?? [];
  1450 |             const bounds = element.getBoundingClientRect(), spread = lengths[3] ?? 0;
  1451 |             if (bounds.width + 2 * spread <= 0 || bounds.height + 2 * spread <= 0 || lengths.length < 2 || (lengths[2] ?? 0) > 0 || lengths[0] === 0 && lengths[1] === 0 && (lengths[3] ?? 0) <= 0) continue;
  1452 |             const source = pseudo ? pseudoPaint(element, paint) : [bounds];
  1453 |             const shadowBoxes = source.flatMap(rect => {
  1454 |               const outer = { left: rect.left + lengths[0] - spread, right: rect.right + lengths[0] + spread, top: rect.top + lengths[1] - spread, bottom: rect.bottom + lengths[1] + spread };
  1455 |               // Only paint outside the opaque control contributes an outer ring.
  1456 |               return [
  1457 |                 { ...outer, bottom: Math.min(outer.bottom, bounds.top) },
  1458 |                 { ...outer, top: Math.max(outer.top, bounds.bottom) },
  1459 |                 { left: outer.left, right: Math.min(outer.right, bounds.left), top: Math.max(outer.top, bounds.top), bottom: Math.min(outer.bottom, bounds.bottom) },
  1460 |                 { left: Math.max(outer.left, bounds.right), right: outer.right, top: Math.max(outer.top, bounds.top), bottom: Math.min(outer.bottom, bounds.bottom) },
  1461 |               ].filter(box => box.right > box.left && box.bottom > box.top);
  1462 |             });
  1463 |             for (const color of shadow.match(/[a-z][a-z0-9-]*\([^()]*\)/giu) ?? []) sample(element, color, `${pseudo ?? 'element'} box-shadow${audit.shadowBound(element, pseudo, shadow) ? ' (conservative composite bound)' : ''}`, 3, true, Number(paint.opacity), shadowBoxes, audit.shadowBound(element, pseudo, shadow));
  1464 |           }
  1465 |           if (pseudo && qualifies(['border-top-width','border-right-width','border-bottom-width','border-left-width','border-top-color','border-right-color','border-bottom-color','border-left-color'])) for (const side of ['top', 'right', 'bottom', 'left']) if (parseFloat(paint.getPropertyValue(`border-${side}-width`)) > 0 && !['none', 'hidden'].includes(paint.getPropertyValue(`border-${side}-style`))) {
  1466 |             const outside = pseudoOutside(element, paint, side);
  1467 |             if (outside !== null) sample(element, paint.getPropertyValue(`border-${side}-color`), `${pseudo} border-${side}`, 3, outside, Number(paint.opacity), pseudoEdges.has(element) ? [pseudoEdges.get(element)!] : undefined);
  1468 |           }
  1469 |         }
  1470 |       }
  1471 |       if (element instanceof HTMLSlotElement) for (const child of element.assignedElements({ flatten: true })) visit(child);
  1472 |       if (element.shadowRoot) for (const child of element.shadowRoot.children) visit(child);
  1473 |       for (const child of element.children) if (!child.matches('style,script,template')) visit(child);
  1474 |     };
  1475 |     visit(scope);
  1476 |     return samples;
  1477 |   }, kind);
  1478 | }
  1479 | 
  1480 | export async function expectSurfaceContrast(root: Locator, kind: ContrastKind = 'text') {
  1481 |   const samples = await measureSurfaceContrast(root, kind);
> 1482 |   expect(samples.length, `Nonempty ${kind} contrast inventory`).toBeGreaterThan(0);
       |                                                                 ^ Error: Nonempty icon contrast inventory
  1483 |   // A Fluent control may paint a contrasting edge plus a two-color separator.
  1484 |   // Alternatives belong to that unique control; another control cannot mask a failure.
  1485 |   const owners = new Set(samples.map(sample => sample.owner));
  1486 |   const passingOwners = new Set([...owners].filter(owner => samples.some(sample => sample.owner === owner && sample.ratio >= sample.minimum)));
  1487 |   const failures = samples.filter(sample => sample.ratio < sample.minimum && (!(kind === 'focus' || kind === 'cue' || kind === 'border') || !passingOwners.has(sample.owner)));
  1488 |   expect(failures, `${kind} contrast failures`).toEqual([]);
  1489 |   return samples;
  1490 | }
  1491 | 
  1492 | /** Diagnostic element captures must leave the operator's scroll/focus geometry unchanged. */
  1493 | export async function screenshotPreservingScroll(target: Locator, options?: Parameters<Locator['screenshot']>[0]) {
  1494 |   const positions = await target.evaluateHandle(() => {
  1495 |     const elements = new Set<Element>([document.scrollingElement ?? document.documentElement]);
  1496 |     const visit = (root: Document | ShadowRoot) => { for (const element of root.querySelectorAll('*')) {
  1497 |       if (element.scrollHeight > element.clientHeight || element.scrollWidth > element.clientWidth || element.scrollTop || element.scrollLeft) elements.add(element);
  1498 |       if (element.shadowRoot) visit(element.shadowRoot);
  1499 |     } };
  1500 |     visit(document);
  1501 |     return [...elements].map(element => ({ element, left: element.scrollLeft, top: element.scrollTop }));
  1502 |   });
  1503 |   try { return await target.screenshot(options); }
  1504 |   finally {
  1505 |     await positions.evaluate(records => { for (const { element, left, top } of records) if (element.isConnected) element.scrollTo({ left, top, behavior: 'instant' }); });
  1506 |     await positions.dispose();
  1507 |   }
  1508 | }
  1509 | 
  1510 | /** Wait for finite theme/control paint transitions without changing the active styling. */
  1511 | export async function settlePaint(page: Page): Promise<void> {
  1512 |   await page.evaluate(async () => {
  1513 |     for (let pass = 0; pass < 5; pass++) {
  1514 |       await new Promise<void>(resolve => requestAnimationFrame(() => requestAnimationFrame(() => resolve())));
  1515 |       const roots: (Document | ShadowRoot)[] = [document];
  1516 |       const collect = (root: Document | ShadowRoot) => { for (const element of root.querySelectorAll('*')) if (element.shadowRoot) { roots.push(element.shadowRoot); collect(element.shadowRoot); } };
  1517 |       collect(document);
  1518 |       const animations = roots.flatMap(root => root.getAnimations()).filter(animation => animation.playState === 'running' && Number.isFinite(Number(animation.effect?.getComputedTiming().endTime)));
  1519 |       if (!animations.length) return;
  1520 |       await Promise.allSettled(animations.map(animation => animation.finished));
  1521 |     }
  1522 |   });
  1523 | }
  1524 | 
```