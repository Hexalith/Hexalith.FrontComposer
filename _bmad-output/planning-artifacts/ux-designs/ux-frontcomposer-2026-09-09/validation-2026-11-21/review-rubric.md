# FrontComposer UX rubric review

The six journeys and the visual and behavioral contracts can be traced to the D-8 sources. Two handoff errors affect what an operator would see: the fresh-row cue can be implemented in the wrong place with the wrong label, and the command retry example can report a degraded outcome before the lifecycle has established one. The navigation prototype also demonstrates a different keyboard focus destination from the shell contract.

## 1. Flow coverage — adequate

Both experience documents contain all six PRD journey titles in order, with the named people, numbered steps, a climax and a failure path. Each journey has a surface. One recovery instruction contradicts the implementation.

- **High · F1. An optional correlation-recovery failure can look like a command-state transition.** UJ-3 says “another failure degrades.” After the first response proves acceptance, the client can retry once with the same MessageId to recover unreadable correlation data. If that optional request fails, the client still returns Accepted with the original MessageId and unknown correlation. The confirmation deadline or later lifecycle evidence determines the visible outcome. **Fix:** replace the last sentence of the UJ-3 failure path with those outcomes and reuse the default or configured delay. [UJ-3](../EXPERIENCE.md#uj-3-marc-executes-a-command-safely) · [EventStoreCommandClient.cs, accepted result and RetryAcceptedCorrelationAsync](../../../../../src/Hexalith.FrontComposer.Shell/Infrastructure/EventStore/EventStoreCommandClient.cs)

## 2. Token completeness — strong

All named token references resolve. The color reference names the active Fluent 2 role `--colorBrandForeground1`. Theme inheritance, the configurable shell seed, measurable contrast criteria, and non-color fallbacks are explicit. A component can consume the inherited role across light and dark themes. [Colors](../DESIGN.md#colors) · [UX-VC-1](../../../ux-design-detailed-2026-07-05.md#ux-vc-1---contrast-and-forced-colors-matrix)

## 3. Component coverage — adequate

The same 21 component-pattern groups have visual and behavioral rows. The generated fresh-row treatment conflicts with a dated implementation decision.

- **High · F2. The generated grid’s fresh-row cue has conflicting placement and copy.** The visual spine calls for a row-level marker with visible “New/Updated” text, and VC-06 repeats that label. Story 13.5’s October 12 decision specifies one cue per entry **above** the grid, with the info icon and localized AM-21 text identifying the affected row. The generated default is “Updated row: {EntityKey}.” The entry can disappear when the row materializes, the filter or scope changes, or its ten-second lifetime ends. A tester following the visual row could reject the shipped cue or implement an extra in-row marker. **Fix:** align the component row and VC-06/RM-1 with the dated decision. Distinguish generated defaults from explicit display-copy overrides and the standalone component. Keep existing first-wins publication, one announcement per eligible transition, and silent removal. [Design components](../DESIGN.md#components) · [VC-06 and RM-1](../../../ux-design-detailed-2026-07-05.md) · [Story 13.5, human decision](../../../../implementation-artifacts/spec-13-5-preserve-fresh-row-meaning-across-visual-and-data-changes.md) · [FcNewItemIndicator, ResolvedText](../../../../../src/Hexalith.FrontComposer.Shell/Components/DataGrid/FcNewItemIndicator.razor.cs)

## 4. State coverage — adequate

The state tables cover human, developer, and compatibility-host surfaces. They distinguish missing tenant from empty data, mapped from unmapped rejection, and active from exhausted Degraded. Two configuration or entry cases need clearer acceptance criteria.

- **Medium · F3. Lifecycle timings read as unconditional.** The state matrix and UJ-3 specify one-second polling and a 250 ms retry. The shell allows independently disabling command-status polling or post-acceptance dispatch retry by setting the corresponding option to zero. A valid host could fail a test written from these rows, or announce ongoing checks while its polling driver is idle. **Fix:** identify the 1,000/10,000/120,000 ms values as the default profile, cover the zero-poll and zero-retry cases, and measure the configured local deadline from acknowledgment even when no polls run. Verify the value-zero cases separately from projection fallback. [Lifecycle state matrix](../EXPERIENCE.md#command-authorization-validation-and-lifecycle) · [Shell options](../../../../../src/Hexalith.FrontComposer.Shell/Options/FcShellOptions.cs) · [Zero-interval polling test](../../../../../tests/Hexalith.FrontComposer.Shell.Tests/Infrastructure/PendingCommands/PendingCommandPollingDriverTests.cs) · [FcLifecycleWrapper, ScheduleDeadline](../../../../../src/Hexalith.FrontComposer.Shell/Components/Lifecycle/FcLifecycleWrapper.razor.cs)
- **Low · F4. “An absent or unavailable tab” can include normal entry at the module root.** The supporting state row says to announce AM-31, while regular entry at `/{module}` silently selects the default tab. Only an explicitly requested unknown or disabled tab warrants the unavailable message. **Fix:** distinguish a missing URL segment (normal entry, SS-31) from an invalid requested tab (fallback, SS-32). [Supporting surface states](../EXPERIENCE.md#supporting-surface-states) · [SS-31 and SS-32](../../../ux-design.md#ux-ss-1---state-by-surface-acceptance-matrix) · [FcPageTabs, route selection](../../../../../src/Hexalith.FrontComposer.Shell/Components/Layout/FcPageTabs.razor.cs)

## 5. Visual reference coverage — adequate

Both spines link both HTML references at the relevant sections. Chromium exposed the expected visible announcement for each of the ten lifecycle snapshots, including distinct messages for the two Degraded states and the shared default confirmation message. These are selectable examples of state presentation. There are no imports or wireframes awaiting a link.

- **Medium · F5. The navigation prototype sends keyboard focus to the outer section on both tab and module activation.** In Chromium, activating Users with Enter focused `SECTION#tenants/workspace-users`. Activating Parties focused `SECTION#parties/overview`. The prototype’s tabs are ordinary fragment links. FM-02 keeps focus on the selected tab when its panel changes, and FM-01 moves module-route focus to its heading. This finding concerns the HTML reference alone. **Fix:** either implement those two keyboard paths and tab semantics in the interactive reference or identify it prominently as a layout-only preview and describe both production focus destinations beside it. [Navigation prototype](../mockups/key-responsive-navigation.html) · [FM-01 and FM-02](../../../ux-design.md#ux-fm-1---focus-matrix) · [Lifecycle prototype](../mockups/key-command-lifecycle.html)

## 6. Bloat and overspecification — strong

Exact values serve observable acceptance checks, including the 72/48 px rail widths, the compact grid metric and the shipped viewport tiers. The lifecycle timing exception is recorded once as F3. The dated receipts keep historical candidate acceptance distinguishable from open readiness decisions.

## 7. Inheritance discipline — adequate

All 19 source declarations across the pair resolve. The three D-8 documents have explicit precedence. The six PRD journey names, component groups and active Fluent role carry through. One default announcement is presented as an invariant despite a tested public override.

- **Medium · F6. The idempotent announcement contract omits an adopter override.** Canonical AM-13 assigns “Command confirmed.” to both confirmation states, and the spine describes the canonical announcement copy as final. The wrapper uses nonblank `IdempotentInfoMessage` as the idempotent live announcement, and the component test asserts the override on the shared status node. **Fix:** document the default and the public override at AM-13 and in the experience guidance, or record an owner decision to change the override’s effect on speech. Preserve one terminal announcement per operation. [AM-13](../../../ux-design.md#ux-am-1---announcement-matrix) · [Voice and tone](../EXPERIENCE.md#voice-and-tone) · [Override test](../../../../../tests/Hexalith.FrontComposer.Shell.Tests/Components/Lifecycle/FcLifecycleWrapperIdempotentTests.cs)

## 8. Shape fit — strong

The visual sections follow the eight-part design order. The experience spine includes the eight required sections, responsive and inspiration guidance, and the security/tenancy behavior needed by the documented surfaces.

## Mechanical notes

The static checks covered local source paths, relative links and heading anchors, frontmatter, token references, journey structure, and the 111 stable canonical matrix row identifiers. Source and existing test inspections establish what those files specify. The Chromium checks exercised the two HTML mockups. This review records no production browser, native assistive-technology, or candidate-wide acceptance run.

Review scope: working tree on FrontComposer `431aa71b`, 2026-11-21. SHA-256 of the reviewed file bytes:

| File | SHA-256 |
| --- | --- |
| DESIGN.md | `fd0e2b15dc0bbe1ca7c10aaea1bda5bbf1590fdffadabe2193e1f5d9365f5436` |
| EXPERIENCE.md | `6cd567798ef93ac7fc523872fef2b72b1b0950bc0ffa132b83251b84133937a5` |
| key-responsive-navigation.html | `28de35ce7b83610791b483204861aab745e32e65588b82135f32df4cd9b5cf24` |
| key-command-lifecycle.html | `c7a081da3c48d2580cfc49b61358415aeb8633e2c8714683b9ba8da4e9da885c` |
