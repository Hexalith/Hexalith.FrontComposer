# FrontComposer UX update and recheck

Reviewed 2026-11-21T13:41+01:00 · FrontComposer `431aa71b`

Ten of the 13 findings from the November 21 validation closed. The revised UX contracts cover the other three, which still reproduce in the local Counter.Web sample. One is high severity and two are medium. Each has an acceptance check below.

All three lenses ran again on the revised handoff. [Original validation](../validation-2026-11-21/validation-report.md)

## Open implementation findings

### High (1)

**FL-1 · The narrow hamburger offers no Module destination.** Counter.Web showed only the drawer title and Close at 320 CSS px, including after a desktop-to-phone resize. At 900 CSS px the hamburger exposed no list. The same account reached Counter from the 1440 px rail.

**Acceptance.** Provide the authorized list in the active drawer. From cold phone and tablet loads and after resize, select Counter by keyboard and verify its default tab, heading focus and single current Module. [Shell contract](../EXPERIENCE.md#responsive--platform)

### Medium (2)

**FL-2 · Generated status cells omit the visible label at rest.** All six rendered enum cells had a named glyph and empty visible cell text in ordinary and forced colors. Focusing Warning exposed a tooltip.

**Acceptance.** Render the localized status text alongside the glyph. Check the cell before hover or focus and keep its accessible name. [Status contract](../../../ux-design.md#ux-dr2---semantic-status-slots)

**F3 · The pending summary says it is checking status when polling is disabled.** In the local Counter sample with the polling interval set to zero, a submitted command displayed “EventStore accepted it and FrontComposer is checking status.” The visible pending summary has aria-live set to off. The lifecycle announcements already describe the unconfirmed outcome.

**Acceptance.** Use truthful pending text when the interval or per-tick limit is zero. Verify the visible and accessible summary and the independent confirmation deadline for each setting. [Configured lifecycle behavior](../EXPERIENCE.md#command-authorization-validation-and-lifecycle)

## Closed findings

| Finding | Subject | Verified change |
| --- | --- | --- |
| F1 | Optional correlation recovery | A failed optional recovery request keeps the accepted MessageId and unknown correlation. The lifecycle determines any later transition. |
| F2 | Fresh-row cue | The generated grid places one localized “Updated row: {EntityKey}.” cue per eligible entry above the grid. The contracts specify dismissal and silent removal. |
| F4 | Module entry and invalid tab | A bare Module route selects its default tab silently. An explicit unavailable tab uses AM-31 and falls back. |
| F6 | Idempotent confirmation | AM-13 records the localized default, nonblank adopter override and one terminal announcement per operation. |
| A1 | Drawer focus | The modal drawer keeps keyboard focus inside while open. Escape returns focus to the opener. |
| A2 | Focus in forced colors | Current Module and tab indicators stay distinguishable from keyboard focus. |
| A3 | Route focus | Module activation moves focus to the route heading. |
| A4 | Module Tab behavior | Arrow, Home and End keys select a tab. The selected tab retains focus and labels its panel. |
| FL-3 | Density in Settings | Phone and Tablet force Comfortable. Settings preserves the operator’s choice and explains the viewport override. Effective density changes govern the announcement. |
| FL-4 | Account in lifecycle preview | The closed header shows the avatar. Opening the account control reveals Marc. |

## UX rubric

- **Flow coverage** — strong. All six PRD journey titles match both experience documents. Each has a named protagonist, ordered steps, a climax and a failure path. In Marc’s UJ-3, failure to recover optional correlation data preserves acceptance and the original message ID. The confirmation deadline starts at acknowledgement (F1). [UJ-3](../EXPERIENCE.md#uj-3-marc-executes-a-command-safely) · [Transport implementation](../../../../../src/Hexalith.FrontComposer.Shell/Infrastructure/EventStore/EventStoreCommandClient.cs)
- **Token completeness** — strong. All 11 distinct token references used by the pair resolve. The visual supplement distinguishes the configurable shell brand seed from the inherited Fluent foreground role and supplies contrast and forced-colors criteria. Both previews use non-color cues for selection or state. [Design tokens](../DESIGN.md#colors) · [Visual acceptance matrix](../../../ux-design-detailed-2026-07-05.md#ux-vc-1---contrast-and-forced-colors-matrix)
- **Component coverage** — strong. The same 21 component and pattern rows have visual and behavioral guidance. The generated fresh-row cue sits above the grid, names the affected entity, permits a visible-text override and clears silently (F2). The documents identify the generated status cell’s tooltip-only visible label as an outstanding implementation gap (FL-2). Settings distinguishes saved and effective density, including forced Comfortable on Phone and Tablet (FL-3). [Component patterns](../EXPERIENCE.md#component-patterns) · [Fresh-row behavior](../EXPERIENCE.md#fresh-row-state)
- **State coverage** — strong. All 18 IA surfaces have state or recovery coverage. Command-status polling and optional correlation recovery can be disabled independently. The mounted wrapper retains its deadline with polling off. The pending summary still claims to be checking status in that configuration (F3). Normal Module entry selects the default tab silently (SS-31). An explicit unavailable tab falls back and announces AM-31 (F4). [Lifecycle matrix](../EXPERIENCE.md#command-authorization-validation-and-lifecycle) · [Supporting states](../EXPERIENCE.md#supporting-surface-states)
- **Visual reference coverage** — strong. Both spines link both self-contained HTML references. In Chromium, the drawer contained forward and reverse Tab, and Escape restored the opener (A1). Under forced colors, focused current rail and tab controls had visibly different paint (A2). Activating Parties focused the route heading (A3). Right Arrow selected Users, kept focus on its tab and exposed its associated panel (A4). All ten lifecycle snapshots showed a single polite message. The closed header concealed the display name, which was available in the account disclosure (FL-4). [Navigation reference](../mockups/key-responsive-navigation.html) · [Lifecycle reference](../mockups/key-command-lifecycle.html)
- **Bloat and overspecification** — strong. The working pair links the canonical matrices and dated receipts where a reader needs the underlying rule or evidence. Default timings, configuration exceptions and candidate-specific approvals are labelled at the point of use. The mockups illustrate two requested surfaces. The documents cover the remaining surfaces.
- **Inheritance discipline** — strong. All 19 sources named in the pair resolve. AM-13 documents the localized confirmation default and the nonblank `IdempotentInfoMessage` override for idempotent outcomes (F6). The responsive table distinguishes the intended narrow drawer from the default shell’s missing Module list at the observed widths (FL-1). Both the working pair and the three D-8 documents preserve the current Product and accessibility gate decisions. [Announcement matrix](../../../ux-design.md#ux-am-1---announcement-matrix) · [Responsive behavior](../EXPERIENCE.md#responsive--platform)
- **Shape fit** — strong. The eight design sections are in the prescribed order. The experience spine has every required section plus responsive, security and evidence guidance. All 111 canonical AM, VR, FM, OF, SS and AE identifiers retain their committed order. The five documents’ declared sources, local links and heading anchors resolve.

## Checks and scope

- All five UX documents passed source, YAML, token and internal link checks. The six named journeys retain their steps, climax and failure path. The 111 canonical matrix IDs retain their order.
- Both HTML references worked offline without external requests and reflowed at nine widths from 320 to 1440 CSS px. All ten lifecycle previews exposed their expected default messages. At 400% Chromium page zoom, a 1280 px viewport reflowed to 320 CSS px on both previews. Module activation reached the visible Parties heading, and the lifecycle selector retained focus. The accessibility review records the forced-colors and keyboard checks.
- axe-core 4.14.0 reported zero automated violations across 30 preview cases. Some rules returned incomplete results. Manual keyboard interaction and paint comparisons supplied evidence for focus behavior.
- Implementation checks used the local Counter.Web Release/Test fixture at FrontComposer 431aa71b with Fluent UI 5.1.0. Native screen-reader speech, deployed tenants and the hosted Quality lane were outside this review.

## Reviewer files

[UX rubric](review-rubric.md) · [Accessibility](review-accessibility.md) · [Fluent UI implementation alignment](review-fluent-ui-v5.md)

## Reviewed artifacts

| Artifact | SHA-256 of reviewed bytes |
| --- | --- |
| [ux-design.md](../../../ux-design.md) | `3c43741adcd577b52ac586ccdfb142f88aec5a395f2bf95a54c5a2c972b4a2f3` |
| [ux-design-detailed-2026-07-05.md](../../../ux-design-detailed-2026-07-05.md) | `c365b93a1a973a2468d161b017251e93fa2fe69a6a51dcd3a9908b075537e4b7` |
| [ux-experience-2026-07-05.md](../../../ux-experience-2026-07-05.md) | `7190639da921a633153615990e6b2dc7cc348f974d860225cfcf6d5283f4d57e` |
| [DESIGN.md](../DESIGN.md) | `2a883220397c411bfcbcfe6619beeffaff4820af55ab3208e73d0dd33238b185` |
| [EXPERIENCE.md](../EXPERIENCE.md) | `1881268da62cf25079f33a22d861dcddc42536dbac9e1d6a0f10ebc026e84034` |
| [key-responsive-navigation.html](../mockups/key-responsive-navigation.html) | `7abd4ebbacefd9d0428b238a57531849d92c31156dc7124ac244beb0979adfd1` |
| [key-command-lifecycle.html](../mockups/key-command-lifecycle.html) | `771e6ae0b40d90423f10d360f6b2a01b08b919416f880d30b84217e47eee68de` |
