# FrontComposer UX validation

Working UX documents on FrontComposer 431aa71b · 2026-11-21T12:45+01:00

**13 distinct findings · 0 critical · 5 high · 6 medium · 2 low**

All six named journeys and all 21 component groups are present. Two handoff instructions could change what an operator sees. UJ-3 describes a failed optional retry as a degraded command, and the grid specification places the fresh-row cue inside the row even though the approved design puts it above the grid.

In the default Counter shell, the hamburger offered no Module destination at phone or tablet width. The responsive navigation mockup contains Module links, but keyboard focus can move behind its open drawer, and its current items lose visible focus in forced colors. Each of the ten command lifecycle previews displayed the expected default announcement.

The rubric and accessibility reviews independently observed two keyboard behaviors. Module-route focus and Module Tab focus have separate acceptance criteria (FM-01 and FM-02). Each appears once in the counts.

## Category verdicts

| Category | Verdict | Coverage |
| --- | --- | --- |
| Flow coverage | adequate | Both experience documents contain all six PRD journey titles in order, with the named people, numbered steps, a climax and a failure path. Each journey has a surface. One recovery instruction contradicts the implementation. |
| Token completeness | strong | All named token references resolve. The color reference names the active Fluent 2 role `--colorBrandForeground1`. Theme inheritance, the configurable shell seed, measurable contrast criteria, and non-color fallbacks are explicit. A component can consume the inherited role across light and dark themes. |
| Component coverage | adequate | The same 21 component-pattern groups have visual and behavioral rows. The generated fresh-row treatment conflicts with a dated implementation decision. |
| State coverage | adequate | The state tables cover human, developer, and compatibility-host surfaces. They distinguish missing tenant from empty data, mapped from unmapped rejection, and active from exhausted Degraded. Two configuration or entry cases need clearer acceptance criteria. |
| Visual reference coverage | adequate | Both spines link both HTML references at the relevant sections. Chromium exposed the expected visible announcement for each of the ten lifecycle snapshots, including distinct messages for the two Degraded states and the shared default confirmation message. These are selectable examples of state presentation. There are no imports or wireframes awaiting a link. |
| Bloat and overspecification | strong | Exact values serve observable acceptance checks, including the 72/48 px rail widths, the compact grid metric and the shipped viewport tiers. The lifecycle timing exception is recorded once as F3. The dated receipts keep historical candidate acceptance distinguishable from open readiness decisions. |
| Inheritance discipline | adequate | All 19 source declarations across the pair resolve. The three D-8 documents have explicit precedence. The six PRD journey names, component groups and active Fluent role carry through. One default announcement is presented as an invariant despite a tested public override. |
| Shape fit | strong | The visual sections follow the eight-part design order. The experience spine includes the eight required sections, responsive and inspiration guidance, and the security/tenancy behavior needed by the documented surfaces. |

## Additional lenses

**Accessibility**. The responsive navigation preview hides keyboard focus in two reproducible interactions. The review also reproduced both focus destinations noted in the rubric. The offline lifecycle selector retained focus through all ten snapshots.

**Fluent UI implementation alignment**. The most consequential gap is the empty drawer in the default narrow shell. Two additional findings affect status readability and the Settings density contract. The lifecycle mockup also differs from the account control.

## Findings by severity

### Critical (0)

No critical findings.

### High (5)

#### F1 · A failed optional retry can be mistaken for a degraded command

**Flow coverage.** UJ-3 says a second failure degrades the command. Once the service has acknowledged the original MessageId, a failed retry to recover unreadable correlation data still returns Accepted with that ID and unknown correlation. The example could lead an operator to treat accepted work as degraded prematurely.

**Suggested fix.** Correct the UJ-3 failure path. Keep the accepted outcome after this retry and identify the later lifecycle evidence or confirmation deadline that can move the command to Degraded.

[UJ-3](../EXPERIENCE.md#uj-3-marc-executes-a-command-safely) · [EventStoreCommandClient.cs, accepted result and RetryAcceptedCorrelationAsync](../../../../../src/Hexalith.FrontComposer.Shell/Infrastructure/EventStore/EventStoreCommandClient.cs)

#### F2 · The grid handoff gives the wrong location and label for a fresh row

**Component coverage.** The visual component row and VC-06 describe a row-level “New/Updated” marker. Story 13.5 records an October 12 decision to put one cue per entry above the grid. The generated default names the affected row with “Updated row: {EntityKey}.”

**Suggested fix.** Align DESIGN.md, VC-06 and RM-1 with the dated decision. Specify the generated label, adopter overrides, and silent removal on row materialization, filter or scope change, or expiry.

[Design components](../DESIGN.md#components) · [VC-06 and RM-1](../../../ux-design-detailed-2026-07-05.md) · [Story 13.5, human decision](../../../../implementation-artifacts/spec-13-5-preserve-fresh-row-meaning-across-visual-and-data-changes.md) · [FcNewItemIndicator, ResolvedText](../../../../../src/Hexalith.FrontComposer.Shell/Components/DataGrid/FcNewItemIndicator.razor.cs)

#### A1 · Keyboard focus moves behind the open navigation drawer

**Accessibility.** In the navigation mockup at 320 and 1024 CSS px, Tab can move past the drawer into workspace links that the drawer fully covers. Escape leaves the drawer open. The active control is hidden in this sequence (WCAG 2.2 SC 2.4.11).

**Suggested fix.** Keep a disclosure in document flow, or give the overlay modal keyboard behavior with inert background controls. Support dismissal with focus return and verify forward and reverse Tab.

[Drawer styles and controls](../mockups/key-responsive-navigation.html) · [Canonical UX-AE-1](../../../ux-design.md#ux-ae-1---responsive-and-accessibility-evidence-matrix)

#### A2 · Current navigation items lose visible focus in forced colors

**Accessibility.** At desktop width, the mockup paints the same outline on a current Module link or workspace link before and after keyboard focus. A pixel comparison showed a visible focus change on an inactive link only (WCAG SC 2.4.7).

**Suggested fix.** Give current items a distinct forced-colors focus outline and offset. Test the current, focused and current-plus-focused states on both controls.

[Navigation style rules](../mockups/key-responsive-navigation.html) · [Visual focus contract](../../../ux-design-detailed-2026-07-05.md#ux-vc-1---contrast-and-forced-colors-matrix)

#### FL-1 · Phone and tablet navigation has no Module list in the default drawer

**Fluent UI implementation alignment.** The default shell removes its only navigation area below 1024 CSS px. Its hamburger supplies a title but no child content. Fluent UI 5.1.0 falls back to the registered navigation area, which is absent on these tiers. In a local build of Counter.Web, at 320 CSS px the opened drawer contained only its title and Close button, including after a resize from 1440. At 900 CSS px neither Enter nor a click produced a visible dialog or a Module list. Counter was available on Home at both widths and the desktop rail reached /counter/counter-projection.

**Suggested fix.** Pass the authorized Module list to the active drawer exactly once. Cover cold phone and tablet entry plus a desktop-to-phone resize. Open the drawer by keyboard, activate Counter and verify its default tab, route heading and current entry.

[Responsive specification](../EXPERIENCE.md#responsive--platform) · [Shell composition](../../../../../src/Hexalith.FrontComposer.Shell/Components/Layout/FrontComposerShell.razor) · [Viewport guard](../../../../../src/Hexalith.FrontComposer.Shell/Components/Layout/FrontComposerShell.razor.cs) · [Hamburger](../../../../../src/Hexalith.FrontComposer.Shell/Components/Layout/FcHamburgerToggle.razor) · [Responsive sidebar tests](../../../../../tests/e2e/specs/sidebar-responsive.spec.ts) · [Named-drawer and focus tests](../../../../../tests/e2e/specs/responsive-accessibility.spec.ts) · [Navigation mockup](../mockups/key-responsive-navigation.html)

### Medium (6)

#### F3 · Lifecycle timing examples omit supported zero settings

**State coverage.** UJ-3 and the state matrix describe polling and the post-acknowledgment retry as unconditional. A host can disable either independently. The local confirmation deadline still runs from acknowledgment when polling is disabled.

**Suggested fix.** Label the timing profile as the default. Add acceptance cases for zero polling and zero post-acceptance retry, including the expected state and copy.

[Lifecycle state matrix](../EXPERIENCE.md#command-authorization-validation-and-lifecycle) · [Shell options](../../../../../src/Hexalith.FrontComposer.Shell/Options/FcShellOptions.cs) · [Zero-interval polling test](../../../../../tests/Hexalith.FrontComposer.Shell.Tests/Infrastructure/PendingCommands/PendingCommandPollingDriverTests.cs) · [FcLifecycleWrapper, ScheduleDeadline](../../../../../src/Hexalith.FrontComposer.Shell/Components/Lifecycle/FcLifecycleWrapper.razor.cs)

#### F6 · AM-13 omits the public idempotent announcement override

**Inheritance discipline.** The canonical copy “Command confirmed.” is the default for both confirmation states. The wrapper and its test allow a nonblank IdempotentInfoMessage to replace the spoken text for an idempotent result.

**Suggested fix.** Document the default and the tested override in AM-13 and the voice guidance. Preserve one terminal announcement per operation.

[AM-13](../../../ux-design.md#ux-am-1---announcement-matrix) · [Voice and tone](../EXPERIENCE.md#voice-and-tone) · [Override test](../../../../../tests/Hexalith.FrontComposer.Shell.Tests/Components/Lifecycle/FcLifecycleWrapperIdempotentTests.cs)

#### A3 · Module navigation focuses the preview frame

**Accessibility.** Enter on Parties in the navigation mockup changes route but focuses the outer section containing the shell and address bar. FM-01 sends focus to the new route heading.

**Suggested fix.** Move focus to the destination workspace heading on Module activation, or mark the fragment interaction as a layout preview and identify the product focus destination alongside it.

[Module links and destinations, lines 136–202](../mockups/key-responsive-navigation.html) · [FM-01](../../../ux-design.md#ux-fm-1---focus-matrix)

#### A4 · The Module Tab selector behaves as ordinary links

**Accessibility.** Right Arrow on the active workspace tab has no effect. Enter on Users focuses the destination section. The mockup omits tab roles, selection and panel relationships. FM-02 retains focus on the selected tab.

**Suggested fix.** Implement arrow-key navigation and selected-tab focus with a labelled panel, or explicitly identify the route links as a preview and link the tab interaction contract.

[Workspace selector, lines 151–153 and 177–179](../mockups/key-responsive-navigation.html) · [FM-02](../../../ux-design.md#ux-fm-1---focus-matrix)

#### FL-2 · Generated status cells hide the visible label until interaction

**Fluent UI implementation alignment.** UX-DR2 requires persistent icon-plus-text status. An annotated enum cell renders an FcStatusIcon with an accessible name, but its localized label is in the tooltip. A sighted reader must hover or focus to read it.

**Suggested fix.** Render persistent text next to the glyph in the generated presentation, preserve the accessible name and check the cell at rest and in forced colors.

[UX-DR2](../../../ux-design.md#ux-dr2---semantic-status-slots) · [PRD](../../../prd.md#fr-11-render-projection-grids-and-states) · [Generated column](../../../../../src/Hexalith.FrontComposer.SourceTools/Emitters/ColumnEmitter.cs) · [FcStatusIcon](../../../../../src/Hexalith.FrontComposer.Shell/Components/Badges/FcStatusIcon.razor) · [Component tests](../../../../../tests/Hexalith.FrontComposer.Shell.Tests/Components/Badges/FcStatusIconTests.cs)

#### FL-3 · Settings omits the difference between selected and effective density

**Fluent UI implementation alignment.** The working pair implies that every choice updates the page and that Settings stays silent. Phone and Tablet force Comfortable even when Compact is saved. Initial hydration and opening Settings are silent. A later effective density change can announce the new value once.

**Suggested fix.** Document the narrow-viewport override, the saved choice and preview, and the announcement trigger in Settings and the responsive guidance.

[Working component and state patterns](../EXPERIENCE.md#component-patterns) · [Density precedence](../../../../../src/Hexalith.FrontComposer.Shell/State/Density/DensityPrecedence.cs) · [Settings dialog](../../../../../src/Hexalith.FrontComposer.Shell/Components/Layout/FcSettingsDialog.razor) · [Density announcer](../../../../../src/Hexalith.FrontComposer.Shell/Components/Layout/FcDensityAnnouncer.razor.cs) · [Forced-density tests](../../../../../tests/Hexalith.FrontComposer.Shell.Tests/Components/Layout/FcSettingsDialogTests.cs)

### Low (2)

#### F4 · Normal Module entry can be read as an unavailable tab

**State coverage.** The supporting state row groups an absent tab with an unavailable tab. Opening the bare Module route selects its default tab silently. An explicit unknown or disabled tab triggers AM-31.

**Suggested fix.** Distinguish normal Module entry (SS-31) from an invalid requested tab (SS-32) in the working experience document.

[Supporting surface states](../EXPERIENCE.md#supporting-surface-states) · [SS-31 and SS-32](../../../ux-design.md#ux-ss-1---state-by-surface-acceptance-matrix) · [FcPageTabs, route selection](../../../../../src/Hexalith.FrontComposer.Shell/Components/Layout/FcPageTabs.razor.cs)

#### FL-4 · The lifecycle preview exposes the name in the closed header

**Fluent UI implementation alignment.** The closed lifecycle mockup header displays “Marc”. The working contract and account component show an avatar in the closed header and the display name inside the account menu.

**Suggested fix.** Use the same closed avatar treatment as the responsive mockup.

[Lifecycle mockup](../mockups/key-command-lifecycle.html) · [Design components](../DESIGN.md#components) · [Account menu](../../../../../src/Hexalith.FrontComposer.Shell/Components/Layout/FcAccountMenu.razor)

## Checks and limits

- Reviewed the D-8 UX authority, its visual and behavioral supplements, DESIGN.md, EXPERIENCE.md and both offline HTML mockups. All declared sources, local links, checked heading anchors and named token references resolve. The 111 canonical matrix row IDs retain their order. The six journeys include their expected actors, steps, climax and failure path.
- Chromium exercised both previews offline from 320 to 1440 CSS px. The navigation rail matched its specified width at eight boundary widths. All ten lifecycle snapshots exposed their expected default message, including both Degraded outcomes. Reflow, keyboard input, forced colors and reduced motion were exercised on the previews.
- axe-core 4.14.0 reported zero automated violations across 20 lifecycle cases and eight navigation cases with the configured WCAG A/AA tags through 2.2 AA. Some rules returned incomplete results. The focused-item paint comparison and obscured-focus checks required keyboard interaction.
- The local Counter.Web sample used FrontComposer 431aa71b, its recorded Builds revision e1380b81, Fluent Components and Icons 5.1.0, the .NET 11.0.100 SDK and the repository’s Release/Test fixture configuration with specimens enabled. The drawer and desktop control were exercised in Chromium. Other implementation comparisons used source, test assertions and the selected package.
- Native screen-reader speech, 400% browser zoom, physical devices, an authenticated deployment and the hosted Quality lane were outside this run. OI-4, OI-16/SM-6 and G-4 retain their recorded ownership and evidence requirements.

## Reviewed versions

| Artifact | SHA-256 |
| --- | --- |
| [ux-design.md](../../../ux-design.md) | `1f87c1c1c2c5ce8463a9a502a48f3be1a03c995294d40d6851ef39cac908f826` |
| [ux-design-detailed-2026-07-05.md](../../../ux-design-detailed-2026-07-05.md) | `6353689cf83f19d96aab8a852d1865efba8a0fb5361941b3effd6909b49a95b2` |
| [ux-experience-2026-07-05.md](../../../ux-experience-2026-07-05.md) | `13c17b0d511f721a20ad94418e4e09251a0dd25875865d55fc17ad3d34c56daf` |
| [DESIGN.md](../DESIGN.md) | `fd0e2b15dc0bbe1ca7c10aaea1bda5bbf1590fdffadabe2193e1f5d9365f5436` |
| [EXPERIENCE.md](../EXPERIENCE.md) | `6cd567798ef93ac7fc523872fef2b72b1b0950bc0ffa132b83251b84133937a5` |
| [key-command-lifecycle.html](../mockups/key-command-lifecycle.html) | `c7a081da3c48d2580cfc49b61358415aeb8633e2c8714683b9ba8da4e9da885c` |
| [key-responsive-navigation.html](../mockups/key-responsive-navigation.html) | `28de35ce7b83610791b483204861aab745e32e65588b82135f32df4cd9b5cf24` |

## Reviewer files

- [UX rubric](review-rubric.md)
- [Accessibility](review-accessibility.md)
- [Fluent UI implementation alignment](review-fluent-ui-v5.md)
