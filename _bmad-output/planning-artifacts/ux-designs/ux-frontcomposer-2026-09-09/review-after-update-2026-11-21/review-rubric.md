# FrontComposer UX rubric review

## Overall verdict

The updated handoff covers all six journeys and preserves D-8 authority. The 13 earlier findings each have a correction or a documented runtime gap. Authors also corrected the prototype’s handling of an explicitly unknown tab during this review. The shell’s narrow drawer, generated status labels and zero-poll pending message still need implementation work.

## 1. Flow coverage — strong

All six PRD journey titles match both experience documents. Each has a named protagonist, ordered steps, a climax and a failure path. In Marc’s UJ-3, failure to recover optional correlation data preserves acceptance and the original message ID. The confirmation deadline starts at acknowledgement (F1). [UJ-3](../EXPERIENCE.md#uj-3-marc-executes-a-command-safely) · [Transport implementation](../../../../../src/Hexalith.FrontComposer.Shell/Infrastructure/EventStore/EventStoreCommandClient.cs)

## 2. Token completeness — strong

All 11 distinct token references used by the pair resolve. The visual supplement distinguishes the configurable shell brand seed from the inherited Fluent foreground role and supplies contrast and forced-colors criteria. Both previews use non-color cues for selection or state. [Design tokens](../DESIGN.md#colors) · [Visual acceptance matrix](../../../ux-design-detailed-2026-07-05.md#ux-vc-1---contrast-and-forced-colors-matrix)

## 3. Component coverage — strong

The same 21 component and pattern rows have visual and behavioral guidance. The generated fresh-row cue sits above the grid, names the affected entity, permits a visible-text override and clears silently (F2). The documents identify the generated status cell’s tooltip-only visible label as an outstanding implementation gap (FL-2). Settings distinguishes saved and effective density, including forced Comfortable on Phone and Tablet (FL-3). [Component patterns](../EXPERIENCE.md#component-patterns) · [Fresh-row behavior](../EXPERIENCE.md#fresh-row-state)

## 4. State coverage — strong

All 18 IA surfaces have state or recovery coverage. Command-status polling and optional correlation recovery can be disabled independently. The mounted wrapper retains its deadline with polling off. The pending summary still claims to be checking status in that configuration (F3). Normal Module entry selects the default tab silently (SS-31). An explicit unavailable tab falls back and announces AM-31 (F4). [Lifecycle matrix](../EXPERIENCE.md#command-authorization-validation-and-lifecycle) · [Supporting states](../EXPERIENCE.md#supporting-surface-states)

## 5. Visual reference coverage — strong

Both spines link both self-contained HTML references. In Chromium, the drawer contained forward and reverse Tab, and Escape restored the opener (A1). Under forced colors, focused current rail and tab controls had visibly different paint (A2). Activating Parties focused the route heading (A3). Right Arrow selected Users, kept focus on its tab and exposed its associated panel (A4). All ten lifecycle snapshots showed a single polite message. The closed header concealed the display name, which was available in the account disclosure (FL-4). [Navigation reference](../mockups/key-responsive-navigation.html) · [Lifecycle reference](../mockups/key-command-lifecycle.html)

## 6. Bloat and overspecification — strong

The working pair links the canonical matrices and dated receipts where a reader needs the underlying rule or evidence. Default timings, configuration exceptions and candidate-specific approvals are labelled at the point of use. The mockups illustrate two requested surfaces. The documents cover the remaining surfaces.

## 7. Inheritance discipline — strong

All 19 sources named in the pair resolve. AM-13 documents the localized confirmation default and the nonblank `IdempotentInfoMessage` override for idempotent outcomes (F6). The responsive table distinguishes the intended narrow drawer from the default shell’s missing Module list at the observed widths (FL-1). Both the working pair and the three D-8 documents preserve the current Product and accessibility gate decisions. [Announcement matrix](../../../ux-design.md#ux-am-1---announcement-matrix) · [Responsive behavior](../EXPERIENCE.md#responsive--platform)

## 8. Shape fit — strong

The eight design sections are in the prescribed order. The experience spine has every required section plus responsive, security and evidence guidance. All 111 canonical AM, VR, FM, OF, SS and AE identifiers retain their committed order. The five documents’ declared sources, local links and heading anchors resolve.

## Verification and remaining work

The review compared the five documents and two mockups with the [13 original findings](../validation-2026-11-21/validation-report.md#findings-by-severity). Ten findings are closed at their reported surface. Three retain a runtime obligation: FL-1 for authorized Module destinations in the narrow drawer, FL-2 for persistent text in generated enum cells and F3 for truthful pending copy with polling disabled. Their expected outcomes and acceptance checks are in the updated UX contract.

Prototype checks used Chromium with local HTML and blocked external requests at 320, 900, 1024 and 1366 CSS px. A direct unknown-tab check caught a silent fallback in the prototype. On recheck, a bare Module route remained silent and an explicit unknown tab within that Module produced one AM-31 announcement. Cold direct links to the default Module, an unknown tab and a known tab settled on their route heading after the browser resolved the fragment. These prototype results apply to the reviewed HTML. Native speech, physical devices and hosted runtime qualification retain their separate evidence requirements.
