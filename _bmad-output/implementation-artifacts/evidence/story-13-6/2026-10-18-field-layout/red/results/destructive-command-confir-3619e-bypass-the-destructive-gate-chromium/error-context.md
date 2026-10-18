# Instructions

- Following Playwright test failed.
- Explain why, be concise, respect Playwright best practices.
- Provide a snippet of code with the fix, if possible.

# Test info

- Name: destructive-command-confirmation.spec.ts >> Story 4.1: destructive command confirmation >> validation failure and rapid clicks cannot bypass the destructive gate
- Location: specs/destructive-command-confirmation.spec.ts:108:3

# Error details

```
Error: Persistent field descriptions stay below their editors, including validation errors, without overflowing.

Persistent field descriptions stay below their editors, including validation errors, without overflowing.

expect(received).toEqual(expected) // deep equality

- Expected  - 1
+ Received  + 5

- Array []
+ Array [
+   "Record to purge.: beside editor",
+   "Why this purge is required.: beside editor",
+   "How long the purged record stays recoverable.: beside editor",
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
    - navigation "Primary navigation" [ref=e39]:
      - generic [ref=e40]:
        - button "Counter" [ref=e41] [cursor=pointer]
        - button "More Counter pages" [ref=e47] [cursor=pointer]
        - button "Default" [ref=e54] [cursor=pointer]
        - button "Specimens" [ref=e59] [cursor=pointer]
        - button "More Specimens pages" [ref=e65] [cursor=pointer]
    - generic [ref=e72]:
      - status [ref=e73]: Density set to Compact.
      - main [ref=e74]:
        - status [ref=e75]
        - status [ref=e76]
        - main [ref=e78]:
          - generic [ref=e79]:
            - paragraph [ref=e80]: FrontComposer accessibility specimen
            - heading "Type, theme, density, lifecycle, and navigation specimen" [active] [level=1] [ref=e81]
            - paragraph [ref=e82]: Generated shell surfaces rendered with deterministic local data.
            - link "Skip to command form" [ref=e83] [cursor=pointer]:
              - /url: "#fc-specimen-command-form"
          - region [ref=e84]:
            - heading "Theme and density controls" [level=2] [ref=e85]
            - generic [ref=e86]:
              - link "light" [ref=e87] [cursor=pointer]:
                - /url: /__frontcomposer/specimens/type?theme=light&density=comfortable
              - link "dark" [ref=e88] [cursor=pointer]:
                - /url: /__frontcomposer/specimens/type?theme=dark&density=comfortable
              - link "compact" [ref=e89] [cursor=pointer]:
                - /url: /__frontcomposer/specimens/type?theme=light&density=compact
              - link "comfortable" [ref=e90] [cursor=pointer]:
                - /url: /__frontcomposer/specimens/type?theme=light&density=comfortable
              - link "roomy" [ref=e91] [cursor=pointer]:
                - /url: /__frontcomposer/specimens/type?theme=light&density=roomy
          - region [ref=e92]:
            - heading "Type ramp" [level=2] [ref=e93]
            - generic [ref=e94]:
              - article [ref=e95]:
                - paragraph [ref=e96]: Display
                - generic [ref=e97]: FrontComposer Display
              - article [ref=e98]:
                - paragraph [ref=e99]: Title
                - generic [ref=e100]: Generated command title
              - article [ref=e101]:
                - paragraph [ref=e102]: Subtitle
                - generic [ref=e103]: Projection subtitle and context
              - article [ref=e104]:
                - paragraph [ref=e105]: Body
                - generic [ref=e106]: Readable generated body text
              - article [ref=e107]:
                - paragraph [ref=e108]: Caption
                - generic [ref=e109]: Supplemental metadata caption
              - article [ref=e110]:
                - paragraph [ref=e111]: Code
                - generic [ref=e112]: FC-STATIC-0001
          - region [ref=e113]:
            - heading "Semantic color tokens" [level=2] [ref=e114]
            - generic [ref=e115]:
              - article [ref=e116]:
                - heading "light theme" [level=3] [ref=e117]
                - generic [ref=e118]:
                  - generic [ref=e120]: Neutral
                  - strong [ref=e121]: Available
                - generic [ref=e122]:
                  - generic [ref=e124]: Informational
                  - strong [ref=e125]: Queued
                - generic [ref=e126]:
                  - generic [ref=e128]: Success
                  - strong [ref=e129]: Confirmed
                - generic [ref=e130]:
                  - generic [ref=e132]: Warning
                  - strong [ref=e133]: Needs review
                - generic [ref=e134]:
                  - generic [ref=e136]: Danger
                  - strong [ref=e137]: Rejected
                - generic [ref=e138]:
                  - generic [ref=e140]: Accent
                  - strong [ref=e141]: Selected
              - article [ref=e142]:
                - heading "dark theme" [level=3] [ref=e143]
                - generic [ref=e144]:
                  - generic [ref=e146]: Neutral
                  - strong [ref=e147]: Available
                - generic [ref=e148]:
                  - generic [ref=e150]: Informational
                  - strong [ref=e151]: Queued
                - generic [ref=e152]:
                  - generic [ref=e154]: Success
                  - strong [ref=e155]: Confirmed
                - generic [ref=e156]:
                  - generic [ref=e158]: Warning
                  - strong [ref=e159]: Needs review
                - generic [ref=e160]:
                  - generic [ref=e162]: Danger
                  - strong [ref=e163]: Rejected
                - generic [ref=e164]:
                  - generic [ref=e166]: Accent
                  - strong [ref=e167]: Selected
          - region [ref=e168]:
            - heading "Density states" [level=2] [ref=e169]
            - generic [ref=e170]:
              - article [ref=e171]:
                - heading "compact density" [level=3] [ref=e172]
                - button "Primary action" [ref=e173]
                - generic [ref=e174]:
                  - text: Label
                  - textbox "compact density text input" [ref=e175]: compact sample
              - article [ref=e176]:
                - heading "comfortable density" [level=3] [ref=e177]
                - button "Primary action" [ref=e178]
                - generic [ref=e179]:
                  - text: Label
                  - textbox "comfortable density text input" [ref=e180]: comfortable sample
              - article [ref=e181]:
                - heading "roomy density" [level=3] [ref=e182]
                - button "Primary action" [ref=e183]
                - generic [ref=e184]:
                  - text: Label
                  - textbox "roomy density text input" [ref=e185]: roomy sample
          - region [ref=e186]:
            - heading "Generated projection DataGrid and badge states" [level=2] [ref=e187]
            - status [ref=e188]
            - paragraph [ref=e189]: 6 specimen status
            - group "Status filter chips" [ref=e190]:
              - button "Neutral, currently inactive" [ref=e191] [cursor=pointer]:
                - generic [ref=e192]: Neutral
              - button "Info, currently inactive" [ref=e193] [cursor=pointer]:
                - generic [ref=e194]: Info
              - button "Success, currently inactive" [ref=e195] [cursor=pointer]:
                - generic [ref=e196]: Success
              - button "Warning, currently inactive" [ref=e197] [cursor=pointer]:
                - generic [ref=e198]: Warning
              - button "Danger, currently inactive" [ref=e199] [cursor=pointer]:
                - generic [ref=e200]: Danger
              - button "Accent, currently inactive" [ref=e201] [cursor=pointer]:
                - generic [ref=e202]: Accent
            - text: 6 specimen status
            - region "specimen status" [ref=e203]:
              - table [ref=e204]:
                - rowgroup [ref=e205]:
                  - row [ref=e206]:
                    - cell [ref=e207]:
                      - generic [ref=e208]:
                        - text: FC-1000Neutral projection
                        - 'img "Status: Neutral" [ref=e210]'
                        - text: Shell
                  - row [ref=e213]:
                    - cell [ref=e214]:
                      - generic [ref=e215]:
                        - text: FC-1001Info projection
                        - 'img "Status: Info" [ref=e217]'
                        - text: SourceTools
                  - row [ref=e220]:
                    - cell [ref=e221]:
                      - generic [ref=e222]:
                        - text: FC-1002Success projection
                        - 'img "Status: Success" [ref=e224]'
                        - text: Contracts
                  - row [ref=e227]:
                    - cell [ref=e228]:
                      - generic [ref=e229]:
                        - text: FC-1003Warning projection
                        - 'img "Status: Warning" [ref=e231]'
                        - text: Testing
                  - row [ref=e234]:
                    - cell [ref=e235]:
                      - generic [ref=e236]:
                        - text: FC-1004Danger projection
                        - 'img "Status: Danger" [ref=e238]'
                        - text: CI
                  - row [ref=e241]:
                    - cell [ref=e242]:
                      - generic [ref=e243]:
                        - text: FC-1005Accent projection
                        - 'img "Status: Accent" [ref=e245]'
                        - text: Docs
              - region "Details for"
          - region [ref=e248]:
            - heading "Generated Counter projection grid" [level=2] [ref=e249]
            - status [ref=e250]
            - paragraph [ref=e251]: 1 counters
            - text: 1 counters
            - region "counters" [ref=e252]:
              - table [ref=e253]:
                - rowgroup [ref=e254]:
                  - row [ref=e255]:
                    - cell [ref=e256]:
                      - generic [ref=e257]:
                        - text: fc-correlation-0002421/15/2026
                        - generic "Metadata with unsupported type System.Collections.Generic.Dictionary requires a custom renderer" [ref=e259]:
                          - strong [ref=e260]: Metadata
                          - text: (System.Collections.Generic.Dictionary) This field requires a custom renderer.
                          - link "Learn how to customize this field" [ref=e261] [cursor=pointer]:
                            - /url: https://hexalith.github.io/FrontComposer/diagnostics/HFC1002
              - region "Details for"
          - region [ref=e262]:
            - heading "Command form and lifecycle states" [level=2] [ref=e263]
            - form "Specimen command form" [ref=e264]:
              - generic [ref=e265]:
                - text: Counter name
                - textbox "Counter name" [ref=e266]: Specimen counter
              - generic [ref=e267]:
                - text: Amount
                - spinbutton "Amount" [ref=e268]: "3"
              - button "Submit command" [ref=e269]
              - button "Cancel" [ref=e270]
            - list "Five command lifecycle states" [ref=e271]:
              - listitem [ref=e272]:
                - strong [ref=e273]: Idle
                - text: Ready to submit.
              - listitem [ref=e274]:
                - strong [ref=e275]: Submitting
                - text: Submitting command.
              - listitem [ref=e276]:
                - strong [ref=e277]: Acknowledged
                - text: Command accepted by the host.
              - listitem [ref=e278]:
                - strong [ref=e279]: Syncing
                - text: Waiting for projection synchronization.
              - listitem [ref=e280]:
                - strong [ref=e281]: Confirmed or rejected
                - text: Terminal confirmation and rejection states remain text-visible.
          - region [ref=e282]:
            - heading "Destructive command confirmation" [level=2] [ref=e283]
            - group [ref=e285]:
              - generic "Purge Specimen Record command form" [ref=e286]:
                - generic [ref=e287]:
                  - generic [ref=e288]:
                    - group "Purge details" [ref=e289]:
                      - generic [ref=e291]:
                        - generic [ref=e292]:
                          - generic: Record Id
                          - generic "Record Id" [ref=e294]:
                            - textbox "Record Id, Required" [ref=e296]: FC-1002
                        - generic: Record to purge.
                      - generic [ref=e297]:
                        - generic [ref=e298]:
                          - generic: Reason
                          - generic "Reason" [ref=e300]:
                            - textbox "Reason, Required" [ref=e302]: QA destructive confirmation coverage
                        - generic: Why this purge is required.
                    - generic [ref=e303]:
                      - generic [ref=e304]:
                        - generic: Retention
                        - generic "Retention" [ref=e306] [cursor=pointer]:
                          - combobox "Retention" [ref=e308]
                      - generic: How long the purged record stays recoverable.
                    - button "Purge Specimen Record" [ref=e312] [cursor=pointer]
                  - status [ref=e314]
          - region [ref=e315]:
            - heading "Policy-gated command authorization" [level=2] [ref=e316]
            - group [ref=e318]:
              - generic "Policy Allowed Specimen command form" [ref=e319]:
                - generic [ref=e320]:
                  - generic [ref=e321]:
                    - generic [ref=e323]:
                      - generic: Record Id
                      - generic "Record Id" [ref=e325]:
                        - textbox "Record Id, Required" [ref=e327]: FC-AUTH-ALLOW
                    - generic [ref=e329]:
                      - generic: Reason
                      - generic "Reason" [ref=e331]:
                        - textbox "Reason, Required" [ref=e333]: QA policy allowed coverage
                    - button "Policy Allowed Specimen" [ref=e334] [cursor=pointer]
                  - status [ref=e336]
            - group [ref=e337]:
              - group [ref=e338]:
                - heading "Permission required" [level=2] [ref=e339]
                - paragraph [ref=e340]: You do not have permission to Policy Denied Specimen.
          - region [ref=e341]:
            - heading "Expanded detail view" [level=2] [ref=e342]
            - group [ref=e343]:
              - generic "Open projection details for FC-1002" [ref=e344]
              - generic [ref=e345]:
                - term [ref=e346]: Correlation
                - definition [ref=e347]: fc-correlation-0002
                - term [ref=e348]: Last synchronized
                - definition [ref=e349]: 2026-01-15 13:45 UTC
          - navigation "Specimen multi-level navigation" [ref=e350]:
            - heading "Multi-level navigation" [level=2] [ref=e351]
            - list [ref=e352]:
              - listitem [ref=e353]:
                - link "Foundation" [ref=e354] [cursor=pointer]:
                  - /url: "#fc-type-ramp-title"
              - listitem [ref=e355]:
                - button "Generated surfaces" [expanded] [ref=e356]
                - list [ref=e357]:
                  - listitem [ref=e358]:
                    - link "Projection grid" [ref=e359] [cursor=pointer]:
                      - /url: "#fc-badge-grid-title"
                  - listitem [ref=e360]:
                    - link "Command lifecycle" [ref=e361] [cursor=pointer]:
                      - /url: "#fc-specimen-command-title"
    - generic [ref=e362]: Hexalith FrontComposer © 2026
```

# Test source

```ts
  507 |           <button id="first-invalid-input" data-fc-validation-field data-fc-invalid="true">First invalid field</button>
  508 |           <button id="next-invalid-input" data-fc-validation-field>Next invalid field</button>
  509 |         </div>`;
  510 |       document.body.append(fixture);
  511 | 
  512 |       focus.focusValidationTarget('fallback-summary', 'removed-input');
  513 |       const missingTargetFallback = document.activeElement?.id;
  514 | 
  515 |       document.getElementById('fallback-summary')?.remove();
  516 |       focus.focusValidationOutcome('fallback-summary');
  517 |       await new Promise<void>((resolve) => requestAnimationFrame(() => resolve()));
  518 |       const missingSummaryFallback = document.activeElement?.id;
  519 |       fixture.remove();
  520 | 
  521 |       return { missingTargetFallback, missingSummaryFallback };
  522 |     }, '/_content/Hexalith.FrontComposer.Shell/js/fc-focus.js');
  523 | 
  524 |     expect(outcome.missingTargetFallback).toBe('next-invalid-input');
  525 |     expect(outcome.missingSummaryFallback).toBe('first-invalid-input');
  526 |   });
  527 | 
  528 |   test('removed destructive invoker restores focus to the route heading', async ({ page, lifecycle, tenant }) => {
  529 |     expect(tenant.tenantId).toBeTruthy();
  530 | 
  531 |     await gotoTypeSpecimen(page);
  532 | 
  533 |     const form = destructiveForm(page);
  534 |     await submitDestructiveCommand(form);
  535 |     const dialog = destructiveDialog(page);
  536 |     await expect(dialog).toBeVisible();
  537 | 
  538 |     await form.getByRole('button', { name: ACTION_LABEL }).evaluate((invoker) => invoker.remove());
  539 |     await page.getByTestId('fc-destructive-cancel').click();
  540 | 
  541 |     await expect(dialog).toHaveCount(0);
  542 |     await lifecycle.expectState(COMMAND_ID, 'idle');
  543 |     await expect(page.getByRole('heading', {
  544 |       name: 'Type, theme, density, lifecycle, and navigation specimen',
  545 |     })).toBeFocused();
  546 |   });
  547 | });
  548 | 
  549 | // Reads the Chromium accessibility tree (not the DOM) for the focusable control of one generated
  550 | // editor, found from the editor host selector: the input inside a text-like editor's shadow root, or
  551 | // the control a Fluent select slots into the light DOM.
  552 | const axField = async (
  553 |   page: Page,
  554 |   hostSelector: string,
  555 | ): Promise<{ role?: string; invalid?: string; description?: string }> => {
  556 |   const session = await page.context().newCDPSession(page);
  557 |   try {
  558 |     const { result } = await session.send('Runtime.evaluate', {
  559 |       expression: `(() => {
  560 |         const host = document.querySelector(${JSON.stringify(hostSelector)});
  561 |         return host?.shadowRoot?.querySelector('input, textarea')
  562 |           ?? host?.querySelector(':scope > [slot="control"]')
  563 |           ?? host;
  564 |       })()`,
  565 |     }) as { result: { objectId?: string } };
  566 |     if (!result.objectId) return {};
  567 |     const { nodes } = await session.send('Accessibility.getPartialAXTree', {
  568 |       objectId: result.objectId,
  569 |       fetchRelatives: false,
  570 |     }) as {
  571 |       nodes: Array<{
  572 |         role?: { value?: string };
  573 |         description?: { value?: string };
  574 |         properties?: Array<{ name: string; value?: { value?: unknown } }>;
  575 |       }>;
  576 |     };
  577 |     const node = nodes[0];
  578 |     const invalid = node?.properties?.find((property) => property.name === 'invalid')?.value?.value;
  579 |     return {
  580 |       role: node?.role?.value,
  581 |       invalid: invalid === undefined || invalid === 'false' ? undefined : String(invalid),
  582 |       description: node?.description?.value,
  583 |     };
  584 |   }
  585 |   finally {
  586 |     await session.detach();
  587 |   }
  588 | };
  589 | 
  590 | const expectFieldDescriptionsBelowEditors = async (form: Locator): Promise<void> => {
  591 |   const descriptions = form.locator('.fc-command-field > fluent-text.fc-command-field-description');
  592 |   await expect(descriptions).toHaveCount(3);
  593 |   for (const description of await descriptions.all()) await expect(description).toBeVisible();
  594 |   await expect.poll(() => descriptions.evaluateAll(nodes => nodes.flatMap(description => {
  595 |     const field = description.parentElement;
  596 |     const editor = field?.querySelector(':scope > fluent-field');
  597 |     if (!field || !editor) return ['Missing generated field/editor for description.'];
  598 |     const bounds = description.getBoundingClientRect();
  599 |     const editorBounds = editor.getBoundingClientRect();
  600 |     const fieldBounds = field.getBoundingClientRect();
  601 |     const failures: string[] = [];
  602 |     if (bounds.top < editorBounds.bottom - 1) failures.push(`${description.textContent}: beside editor`);
  603 |     if (bounds.left < fieldBounds.left - 1 || bounds.right > fieldBounds.right + 1) {
  604 |       failures.push(`${description.textContent}: outside field width`);
  605 |     }
  606 |     return failures;
> 607 |   })), { message: 'Persistent field descriptions stay below their editors, including validation errors, without overflowing.' }).toEqual([]);
      |                                                                                                                                  ^ Error: Persistent field descriptions stay below their editors, including validation errors, without overflowing.
  608 | };
  609 | 
  610 | const gotoTypeSpecimen = async (page: Page): Promise<void> => {
  611 |   const route = getSpecimenRoute('type');
  612 |   await page.goto(route.path);
  613 |   await expect(page.locator(route.readySelector)).toBeVisible();
  614 |   await expect(page.getByTestId('fc-destructive-command-specimen')).toBeVisible();
  615 | };
  616 | 
  617 | const destructiveForm = (page: Page): Locator =>
  618 |   page.locator(`.fc-command-form[aria-label="${FORM_LABEL}"]`);
  619 | 
  620 | const destructiveDialog = (page: Page): Locator => page.getByTestId('fc-destructive-dialog');
  621 | 
  622 | const fillDestructiveFields = async (form: Locator, recordId: string, reason: string): Promise<void> => {
  623 |   await fillFieldByLabel(form, 'Record Id', recordId);
  624 |   await fillFieldByLabel(form, 'Reason', reason);
  625 | };
  626 | 
  627 | // The required Retention select starts unchosen; a valid purge picks a retention first.
  628 | const chooseRetention = async (form: Locator, option = 'Thirty Days'): Promise<void> => {
  629 |   await waitForGeneratedFormReady(form);
  630 |   const control = form.locator('fluent-dropdown[name="Retention"] > [slot="control"]');
  631 |   if ((await control.textContent())?.trim() === option) return;
  632 |   await control.click();
  633 |   await form.locator('fluent-dropdown[name="Retention"] fluent-option', { hasText: option }).click();
  634 |   await expect(control).toHaveText(option);
  635 | };
  636 | 
  637 | const submitDestructiveCommand = async (
  638 |   form: Locator,
  639 |   { chooseRetention: withRetention = true }: { chooseRetention?: boolean } = {},
  640 | ): Promise<void> => {
  641 |   await waitForGeneratedFormReady(form);
  642 |   if (withRetention) await chooseRetention(form);
  643 |   await form.getByRole('button', { name: ACTION_LABEL }).click();
  644 | };
  645 | 
```