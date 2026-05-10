# Story 10.2: Accessibility CI Gates & Visual Specimen Verification

Status: review

> **Epic 10** - Framework Quality & Adopter Confidence. Covers **FR76**, **FR77**, **UX-DR32**, **UX-DR33**, **UX-DR34**, **NFR37**, and **NFR38**. Turns the existing accessibility intent and early Playwright/axe helpers into a merge-blocking specimen gate. Applies lessons **L06**, **L07**, **L08**, **L10**, **L11**, **L13**, and **L15**.

---

## Executive Summary

Story 10-2 makes accessibility and visual consistency enforceable before merge:

- Add deterministic type and data-formatting specimen routes that exercise the generated UI surfaces that carry the framework's accessibility promise.
- Extend the existing `tests/e2e` Playwright workspace and `helpers/a11y.ts` instead of creating a parallel browser test harness.
- Run axe-core through Playwright against the type specimen and data-formatting specimen, blocking only `serious` and `critical` violations while reporting lower impacts.
- Add keyboard, focus visibility, forced-colors, reduced-motion, and zoom/reflow checks against the same specimen surface.
- Add visual baselines for Light/Dark x Compact/Comfortable/Roomy in v1 scope, with RTL and broader browser matrices deferred to named follow-up work.
- Require rationale and before/after evidence when specimen baselines change.

---

## Story

As a developer,
I want automated accessibility checks and visual specimen verification that block merge on violations,
so that every release maintains WCAG 2.1 AA conformance and visual consistency across themes and densities.

### Adopter Job To Preserve

An adopter should be able to trust generated FrontComposer UI without manually auditing every generated command form, projection grid, badge, empty state, density setting, theme, or lifecycle wrapper. The framework must prove its own baseline in CI while keeping custom adopter components governed by the existing custom-component accessibility contract.

---

## Dev Agent Cheat Sheet

| Area | Required outcome |
| --- | --- |
| Browser test home | Extend `tests/e2e`; do not create a second Playwright workspace. Keep `test:e2e`, `test:e2e:install`, and `test:e2e:report` as the npm entry points. |
| Specimen routes | Add shell-hosted type and data-formatting specimen views under the existing sample/test host surface. They must be deterministic, route-addressable, and not require live EventStore, DAPR, SignalR, or network calls. |
| Axe gate | Reuse and harden `tests/e2e/helpers/a11y.ts`. The gate scans WCAG 2.1 A/AA tags and fails the merge lane only when violation impact is `serious` or `critical`; `minor` and `moderate` are reported in artifacts. |
| Visual baseline scope | v1 compares 6 combinations: Light and Dark x Compact, Comfortable, Roomy. Store committed baselines under the e2e snapshot convention and keep browser/OS deterministic. RTL and additional zoom visual baselines are deferred. |
| Type specimen content | Every type ramp slot, semantic color token, both themes, all three densities, one DataGrid with column headers and six badge states, one flat command form with five-state lifecycle wrapper, one expanded detail view, and one multi-level nav group. |
| Data specimen content | One DataGrid row per formatting class: locale numbers, absolute and relative timestamps, truncated IDs, null em dash, collection counts, currency, boolean Yes/No, truncated enums, unsupported-field placeholder. |
| Keyboard/focus | Add Playwright tab-order assertions and focus-visible screenshot checks. Do not rely on CSS class names where role/name/data-testid selectors exist. |
| Media modes | Cover `forced-colors`, `prefers-reduced-motion`, and zoom/reflow at 100%, 200%, and 400%. Tests must assert behavior, not only take screenshots. |
| Manual audit logs | Add `docs/accessibility-verification/` templates and release-branch log requirements for NVDA+Firefox, JAWS+Chrome, and VoiceOver+Safari. This story creates the log shape; it does not fake manual verification results. |
| Scope boundaries | Do not implement Pact, mutation testing, flaky quarantine automation, LLM benchmark, SBOM/signing, or broad Fluent UI/Playwright package upgrades beyond what is required for this gate. |

Start here: T1 specimen host -> T2 deterministic state and selectors -> T3 axe gate -> T4 keyboard/focus/media checks -> T5 visual baselines -> T6 CI wiring -> T7 docs/manual evidence.

---

## Acceptance Criteria

| AC | Given | When | Then |
| --- | --- | --- | --- |
| AC1 | A pull request changes UI code | The accessibility CI lane runs | Playwright navigates to the type specimen and data-formatting specimen views and runs axe-core scans on both. |
| AC2 | axe-core returns violations | Results are evaluated | Any `serious` or `critical` WCAG violation fails CI, while `minor` and `moderate` findings are reported as artifacts without blocking. |
| AC3 | The accessibility scan runs | AxeBuilder is configured | It uses WCAG 2.1 A/AA tags (`wcag2a`, `wcag2aa`, `wcag21a`, `wcag21aa`) unless a documented rule-specific exception is added. |
| AC4 | The type specimen renders | It is loaded in CI | It displays every type ramp slot, every semantic color token, both Light and Dark themes, all three density levels, one DataGrid with column headers and six badge states, one flat command form with a five-state lifecycle wrapper, one expanded detail view, and one multi-level nav group. |
| AC5 | The data-formatting specimen renders | It is loaded in CI | A single DataGrid contains one deterministic row per formatting rule: locale-formatted numbers, absolute and relative timestamps, truncated IDs, null em dash, collection counts, currency, boolean Yes/No, truncated enums, and unsupported-field placeholder. |
| AC6 | Specimens render across theme and density | Screenshots are compared | v1 baselines cover exactly 6 combinations: Light/Dark x Compact/Comfortable/Roomy. |
| AC7 | A baseline screenshot changes | A developer updates snapshots | The PR includes a rationale paragraph plus before/after screenshot artifacts or links; unexplained drift blocks merge. |
| AC8 | Keyboard navigation tests run | The scripted tab order enters specimen controls | Focus order is deterministic, reaches command form fields/actions, DataGrid controls, nav groups, density/theme controls, and expanded details without traps. |
| AC9 | Focus visibility is tested | Playwright captures focused states | Focus-visible styling remains detectable and distinct from lifecycle sync or badge state visuals. |
| AC10 | Forced-colors mode is emulated | Specimens render | Important controls, focus indicators, badges, and lifecycle states remain perceivable without color-only signaling. |
| AC11 | Reduced-motion mode is emulated | Lifecycle and sync specimens render | Motion-dependent effects are disabled or reduced while state changes remain announced and visually understandable. |
| AC12 | Zoom/reflow tests run at 100%, 200%, and 400% | Specimens render in the configured viewport | Content remains reachable without two-dimensional scrolling for the required specimen surfaces, and no critical controls overlap. |
| AC13 | Density parity testing runs | Each density level is applied | Compact, Comfortable, and Roomy preserve accessible names, labels, landmarks, badge text, focus order, and minimum touch-target intent. |
| AC14 | Manual screen reader verification is required for release branches | A release branch is cut | A dated log is committed under `docs/accessibility-verification/` for NVDA+Firefox, JAWS+Chrome, and VoiceOver+Safari, with pass/fail status and unresolved issues. |
| AC15 | CI checks out repository submodules | Accessibility and visual jobs run | They use root-level submodules only and never initialize nested submodules recursively. |
| AC16 | The e2e suite runs in CI | Test artifacts are generated | Playwright HTML report, JUnit output, screenshots, visual diffs, and axe result summaries are retained on failure. |
| AC17 | Specimen routes are unavailable or blank | CI runs | The job fails with a clear message naming the missing route or empty specimen instead of passing with zero scanned nodes. |
| AC18 | Lower-impact or known temporary violations exist | A suppression is needed | Suppressions are scoped by selector/rule with owner, expiry, and linked story; blanket page-level suppressions are rejected. |
| AC19 | A specimen route is added | The route contract is reviewed | The exact type and data-formatting specimen route paths, route ownership, test/development visibility, auth independence, and expected landmark roots are documented in the story handoff. |
| AC20 | Visual or accessibility specimens render in CI | The test harness stabilizes the page | Culture/locale, timezone, viewport, device scale factor, fonts, seeded fixture data, storage state, network dependencies, animations, and ready markers are fixed before axe scans or screenshots run. |
| AC21 | Keyboard, focus, media, and zoom tests execute | Assertions are evaluated | Each non-axe check has an enforceable pass/fail assertion for focus order, visible focus, keyboard traps, active media emulation, no color-only state, reduced motion behavior, no horizontal document overflow, and reachable focused controls. |
| AC22 | Playwright or axe artifacts are expected | CI completes or fails | A validation step confirms named reports exist for Playwright HTML/JUnit, axe summaries, screenshots, visual diffs, and trace/video where enabled; missing expected artifacts fail the job or fail the artifact-validation step. |
| AC23 | Snapshot baselines are missing or changed | The visual lane runs | Missing baselines fail outside the documented intentional baseline-update command; changed baselines require review rationale and before/after evidence instead of automatic CI regeneration. |
| AC24 | Generated specimens pass this gate | Adopters interpret the result | The docs state that this gate proves the committed generated/Shell specimen surfaces only; arbitrary adopter-provided custom components remain governed by the existing custom-component accessibility contract. |
| AC25 | Data-formatting specimens exercise locale-sensitive output | The specimen renders | At least one fixed non-default culture is covered for formatting text assertions; full localization layout and RTL visual matrices remain deferred to named follow-up work. |
| AC26 | Specimen coverage is reviewed | The Playwright lane starts | It loads a committed specimen manifest that names required routes, section/landmark roots, theme/density combinations, expected artifact names, and selector or role-count invariants; missing, blank, or unowned manifest entries fail before axe or visual comparison. |
| AC27 | Specimen routes are registered | The host runs outside the test/development specimen mode | Type and data-formatting specimen routes are not exposed by default, and a production-style route smoke test proves the routes fail closed unless the explicit specimen host configuration is enabled. |
| AC28 | CI retries or reruns occur | An accessibility, media, zoom, or visual assertion fails | The first failing evidence remains attached to the failed run, retry success cannot erase blocking violations or snapshot diffs, and no retry path may auto-update baselines or create suppressions. |
| AC29 | Accessibility and visual artifacts are serialized | Reports are retained by CI | Artifact payloads are deterministic, bounded, and redacted; they include route, rule, impact, selector, artifact path, and truncation markers where needed, but never persist full DOM dumps, local machine paths, tokens, cookies, or environment secrets. |

---

## Tasks / Subtasks

- [x] T1. Add deterministic specimen host surfaces (AC4, AC5, AC12, AC17, AC19, AC20, AC24)
  - [x] Add route-addressable type and data-formatting specimen views under the existing Shell/sample test surface, and document the exact route paths in the story handoff.
  - [x] Keep specimen routes test/development-owned, auth-independent, and unavailable as adopter product pages unless an existing host convention already exposes test specimens.
  - [x] Gate specimen route registration behind an explicit test/development specimen-host configuration, and add a production-style smoke test that proves the routes are not exposed by default.
  - [x] Keep specimen data local, deterministic, and independent of live EventStore, DAPR, SignalR, local network, wall-clock time, random IDs, browser storage, user preferences, or machine-specific paths.
  - [x] Freeze culture/locale, timezone, seeded IDs, viewport, device scale factor, fonts, and animations before screenshot or axe checks.
  - [x] Mount specimens inside the real `FrontComposerShell` so theme, density, localization, navigation, lifecycle, DataGrid, and badge behavior are exercised in context.
  - [x] Add stable `data-testid` attributes only where role/name locators cannot identify a specimen element reliably.
  - [x] Add a committed specimen manifest that names every required specimen route, expected landmark/section roots, theme/density combinations, route-ready markers, and artifact names consumed by the tests.
  - [x] Add a blank/partial-specimen guard that fails if required specimen sections, rows, controls, landmark roots, theme/density variants, local assets, or route-ready markers are missing.
  - [x] Validate the manifest before axe scans or screenshots run, and fail on missing, blank, duplicate, stale, or unowned entries with the owning story key in the failure message.
  - [x] Fail on unhandled browser console errors, hydration/runtime exceptions, and unexpected network calls from default specimen routes.

- [x] T2. Build the type specimen content (AC4, AC6, AC9-AC13)
  - [x] Render every type ramp slot and semantic color token in both Light and Dark themes.
  - [x] Render Compact, Comfortable, and Roomy density states through the same density service/state used by the shell.
  - [x] Include one DataGrid with canonical headers and six badge states using existing badge components.
  - [x] Include one flat command form with all five lifecycle states: Idle, Submitting, Acknowledged, Syncing, Confirmed/Rejected.
  - [x] Include one expanded detail view and one multi-level nav group using existing layout/navigation components.
  - [x] Ensure specimen state is deterministic across reruns and does not depend on current locale except where explicitly tested.

- [x] T3. Build the data-formatting specimen content (AC5, AC6, AC12, AC13, AC25)
  - [x] Add one DataGrid row per formatting category from UX-DR34/UX-DR35.
  - [x] Cover locale numbers, absolute timestamps, relative timestamps with frozen time, truncated IDs, null em dash, collection counts, currency, boolean Yes/No, truncated enum labels, and unsupported-field placeholder.
  - [x] Include one fixed non-default culture for text assertions without expanding into a full localization visual matrix.
  - [x] Preserve the existing label precedence rule: explicit `[Display(Name=...)]` beats humanization and formatting fallbacks.
  - [x] Assert generated formatting text and accessible names so visual baselines are not the only oracle.
  - [x] Do not move formatting ownership out of SourceTools/Shell just to build the specimen.

- [x] T4. Harden axe-core accessibility checks (AC1-AC3, AC16-AC18, AC21, AC22)
  - [x] Extend `tests/e2e/helpers/a11y.ts` to separate blocking impacts (`serious`, `critical`) from report-only impacts (`minor`, `moderate`).
  - [x] Keep WCAG 2.1 A/AA tag configuration explicit.
  - [x] Scan the full rendered specimen page or explicitly named landmark roots; fail if the scan includes zero target nodes or a required specimen section is absent.
  - [x] Add helper regression coverage proving `serious`/`critical` violations fail while `minor`/`moderate` violations are still written to artifacts.
  - [x] Emit a concise artifact containing rule id, impact, help URL, affected selectors, and specimen route.
  - [x] Keep axe artifacts deterministic, bounded, and redacted: include rule metadata, route, selector, owner/suppression status, and truncation markers where needed, but do not write full DOM dumps, cookies, tokens, local paths, or environment values.
  - [x] Require suppressions to name route, WCAG/axe rule id, selector, rationale, owner, expiry or review date, linked story/issue, and evidence that the issue is third-party or intentionally deferred.
  - [x] Avoid blanket exclusions for Fluent UI shadow DOM. Exclude only a named element when the underlying issue is documented; broad rule disables fail CI.

- [x] T5. Add keyboard, focus, forced-colors, reduced-motion, and zoom/reflow tests (AC8-AC13, AC20, AC21)
  - [x] Add tab-order tests for nav, DataGrid controls, command form fields/actions, expanded detail, theme/density controls, and skip links.
  - [x] Assert focus order, visible focus indicator, Enter/Space activation where relevant, Escape behavior for dismissible surfaces, and no keyboard traps instead of only sending key presses.
  - [x] Add focus-visible screenshots for representative controls in each specimen and assert focus is not obscured by lifecycle sync visuals.
  - [x] Use Playwright media emulation for dark/light color scheme and reduced motion; use browser/context support for forced-colors where available and document fallback behavior when not supported.
  - [x] Assert media emulation is active before checking forced-colors or reduced-motion behavior.
  - [x] Verify forced-colors and reduced-motion through behavior/token assertions: perceivable focus, non-color-only state indicators, disabled/reduced motion effects, and understandable lifecycle state changes.
  - [x] Test 100%, 200%, and 400% zoom/reflow with deterministic viewport sizes.
  - [x] Assert no critical controls overlap, no horizontal document overflow at 200% and 400%, required content remains reachable, and focused elements scroll into view.

- [x] T6. Add visual baselines and baseline-governance checks (AC6, AC7, AC16, AC20, AC22, AC23)
  - [x] Use Playwright `expect(page).toHaveScreenshot(...)` or equivalent Playwright snapshot conventions already supported by the e2e workspace.
  - [x] Generate and commit baselines for the 6 v1 combinations only: Light/Dark x Compact/Comfortable/Roomy.
  - [x] Keep screenshots on a single OS/browser baseline lane to avoid font/rendering drift; broader browser coverage may run functional accessibility checks without visual baselines.
  - [x] Add a documented baseline-update command or npm script; missing baselines fail in CI unless that intentional update workflow is being run locally.
  - [x] Add a CI check that detects changed specimen snapshots and requires a rationale file or PR-body marker plus before/after artifacts.
  - [x] Ensure baseline updates are committed intentionally and are never regenerated automatically by CI.
  - [x] Mask or stabilize only genuinely dynamic elements; do not hide the UI surfaces this story exists to verify.

- [x] T7. Wire the CI gate without expanding unrelated governance (AC1, AC2, AC6, AC15, AC16, AC22, AC23)
  - [x] Add a dedicated accessibility/visual job or lane to `.github/workflows/ci.yml`.
  - [x] Define the trigger split explicitly: PR gates must run axe, blank/partial route checks, keyboard/focus, media/zoom assertions, artifact validation, and either the full visual matrix or a documented minimal visual smoke subset; main/nightly may own the full visual matrix if PR runtime is too high.
  - [x] Use existing npm scripts where possible; add narrowly scoped scripts such as `test:e2e:a11y` or `test:e2e:visual` only if they simplify CI.
  - [x] Install Playwright browsers in the job and preserve reports on failure with stable artifact names and a configured retention period.
  - [x] Add artifact-validation logic that confirms Playwright HTML report, JUnit XML, screenshots, visual diffs, axe summaries, and trace/video outputs exist when expected.
  - [x] Ensure retries, reruns, or shard replays preserve the first failing accessibility/visual evidence and cannot turn a blocking violation into a pass by overwriting artifacts, suppressions, or snapshots.
  - [x] Keep root-level submodule checkout behavior only; do not use recursive nested submodule update commands.
  - [x] Keep full flaky quarantine, CI diet governance, mutation, Pact, SBOM, and release signing out of this story.

- [x] T8. Add manual verification log templates and docs (AC14, AC18, AC22, AC24)
  - [x] Create `docs/accessibility-verification/README.md` or template documentation for release-branch manual verification.
  - [x] Include required fields: release branch/tag, date, tester, OS, browser, screen reader, version, specimen route, pass/fail, issue links, and resolution status.
  - [x] Document that manual screen reader evidence is required before release/package promotion for NVDA+Firefox, JAWS+Chrome, and VoiceOver+Safari, but is not faked or CI-synthesized by this story.
  - [x] Document the reviewer/sign-off expectation and minimum evidence attachment for manual screen reader logs.
  - [x] Document that this story creates the evidence path and requirement; it must not invent pass results for audits that were not performed.
  - [x] Document adopter-facing evidence paths so adopters can inspect accessibility summaries, specimen screenshots or diffs, and manual verification logs without understanding Playwright internals.
  - [x] Document suppression governance for temporary axe exceptions.

- [x] T9. Final verification and handoff (AC1-AC25)
  - [x] Run `npm --prefix tests/e2e install`.
  - [x] Run `npm --prefix tests/e2e run typecheck`.
  - [x] Run the new accessibility/specimen Playwright lane locally where browser dependencies are available.
  - [x] Run the default .NET test lane touched by specimen host changes.
  - [x] Run `dotnet build Hexalith.FrontComposer.sln --configuration Release`.
  - [x] Verify the specimen manifest, production route-exposure smoke test, artifact redaction/bounding behavior, retry evidence preservation, and no automatic snapshot or suppression creation in CI.
  - [x] Record specimen routes, screenshot baseline locations, specimen manifest path, CI trigger split, CI job name, accessibility artifact paths, baseline-update command, manual evidence path, and any temporary suppressions.

---

## Dev Notes

### Current Repository State

- `tests/e2e` already exists with Playwright config, fixtures, page objects, smoke, lifecycle, density, and responsive specs.
- `tests/e2e/helpers/a11y.ts` already wraps `@axe-core/playwright` with WCAG 2.1 A/AA tags. This story should harden that helper rather than replacing it.
- Root `package.json` already exposes `test:e2e`, `test:e2e:install`, `test:e2e:ui`, and `test:e2e:report` by delegating to `tests/e2e`.
- `.github/workflows/ci.yml` currently runs npm install, .NET build/test lanes, coverage collection, and CLI packaging smoke. Add the accessibility/visual lane without collapsing unrelated gates.
- `tests/Hexalith.FrontComposer.Shell.Tests/Generated/AxeCoreA11yTests.cs` is only a bUnit markup contract check. It explicitly states real axe DOM walking belongs at the Playwright browser layer; do not treat the bUnit tests as sufficient for this story.
- Shell components already contain accessibility-critical surfaces to exercise: `FrontComposerShell`, `FrontComposerNavigation`, `FcThemeToggle`, `FcDensityApplier`, `FcDensityAnnouncer`, `FcStatusBadge`, `FcDesaturatedBadge`, DataGrid helpers, command renderers, and lifecycle UI.
- `Directory.Packages.props` uses current .NET/xUnit/bUnit pins for .NET tests. Browser-package pins live in `tests/e2e/package.json`.
- At story creation time, npm reports latest `@playwright/test` as `1.59.1`, latest `@axe-core/playwright` as `4.11.3`, and latest `axe-core` as `4.11.4`. Do not make a broad upgrade unless implementation verifies compatibility and lockfile impact.

### Architecture and Package Boundaries

| Surface | Story 10-2 responsibility |
| --- | --- |
| `src/Hexalith.FrontComposer.Shell` | Specimen components/routes and any runtime accessibility fixes required for generated UI surfaces. |
| `tests/e2e` | Playwright axe, keyboard, media, zoom/reflow, visual baseline, and artifact tests. Primary browser gate home. |
| `.github/workflows/ci.yml` | CI lane wiring, browser install, artifact upload, and merge-blocking behavior. |
| `docs/accessibility-verification/` | Manual release-branch screen reader verification template/log path. |
| `src/Hexalith.FrontComposer.Testing` | May provide helper setup from Story 10-1 if already implemented, but Story 10-2 must not depend on a live adopter package to render internal specimens. |
| `src/Hexalith.FrontComposer.SourceTools` | Formatting and generated-output contracts remain producer-owned. Use them; do not create a second formatting pipeline for specimens. |
| `Hexalith.EventStore` submodule | Not required for default specimen rendering. Do not initialize nested submodules or scan submodule internals. |

### Accessibility Contract Details

- Baseline is WCAG 2.1 AA for auto-generated output, verified through specimen routes plus targeted generated component assertions.
- Color cannot be the only signal. Badge and lifecycle specimen content must include visible text or accessible labels that survive forced-colors mode.
- Focus visibility outranks lifecycle animation. If a focused element is also syncing, the focus ring must remain detectable and separate from the sync effect.
- Custom components remain governed by the Level 2-4 custom-component accessibility contract: accessible name, keyboard reachability, focus visibility, state announcement, reduced-motion support, and forced-colors support.
- Manual screen reader checks are required because automated scans cannot prove announcement quality.
- Specimen routes are test infrastructure. They must require an explicit test/development specimen-host configuration and must not become adopter product routes by default.
- CI evidence is part of the accessibility contract. Reports may summarize bounded selectors and rule metadata, but must not persist secrets, cookies, local paths, full DOM dumps, or environment-specific values.

### Visual Baseline Contract

| Dimension | v1 scope |
| --- | --- |
| Theme | Light, Dark |
| Density | Compact, Comfortable, Roomy |
| Direction | LTR only; RTL deferred to a named v1.x/v2 story |
| Browser baseline | One deterministic CI browser/OS lane for screenshots. Other browsers may run functional a11y checks without committed baselines. |
| Update protocol | Snapshot changes require rationale plus before/after evidence. Missing baselines fail in CI except during the documented local baseline-update command. |
| Dynamic content | Freeze time, IDs, locale, timezone, storage state, data, viewport, device scale factor, fonts, and animations; mask only unavoidable external noise. |

### Cross-Story Contract Table

| Producer | Consumer | Contract |
| --- | --- | --- |
| Story 10-1 | Story 10-2 | Test-host utilities may help render deterministic generated views, but this story owns browser-level CI gates and specimen routes. |
| Stories 2-3 through 2-5 | Story 10-2 | Command lifecycle states, rejection/idempotency presentation, form labels, aria-live behavior, and focus behavior must appear in specimens. |
| Stories 3-1 through 3-6 | Story 10-2 | Shell layout, navigation, theme, density, persistence, localization, skip links, and responsive behavior must be exercised. |
| Stories 4-1 through 4-6 | Story 10-2 | DataGrid roles, headers, formatting, badges, empty states, unsupported placeholders, virtualization/detail behavior must be covered. |
| Stories 5-3 through 5-7 | Story 10-2 | Reconnection/sync visuals and reduced-motion behavior must not degrade focus or announcements. |
| Stories 6-1 through 6-4 | Story 10-2 | Customization accessibility contract must remain testable; specimens may include one representative override only if deterministic. |
| Stories 7-1 through 7-3 | Story 10-2 | Tenant/user/auth test setup must fail closed and avoid cross-tenant state leakage in browser tests. |
| Stories 9-1 through 9-5 | Story 10-2 | Diagnostic HelpLinkUri/docs should reference the accessibility evidence path and specimen routes when relevant. |
| `tests/e2e/helpers/a11y.ts` | Story 10-2 and later e2e suites | Shared helper defaults must remain explicit and versioned in code comments or docs; changes to impact thresholds, include/exclude behavior, and artifact schema are cross-suite contract changes. |

### Latest Technical Notes

- Playwright's accessibility guide recommends `@axe-core/playwright` for axe-powered tests and warns that automated tests must be combined with manual accessibility assessment.
- Playwright's visual comparison support uses `expect(page).toHaveScreenshot()` and stores committed snapshots; stable screenshot environments are required because browser rendering varies by OS, settings, hardware, and headless mode.
- Playwright supports media emulation such as `colorScheme` and `page.emulateMedia(...)`; implementation must verify the exact supported API for reduced-motion and forced-colors in the installed package version before relying on it.
- `@axe-core/playwright` and `axe-core` are npm/browser test dependencies, not .NET packages. Keep them scoped to the e2e workspace.

### Scope Guardrails

Do not implement these in Story 10-2:

- Story 10-3 Pact consumer/provider contracts.
- Story 10-4 Stryker mutation testing or FsCheck idempotency governance.
- Story 10-5 flaky quarantine automation, reintroduction PRs, or CI diet governance.
- Story 10-6 LLM benchmark, release signing, SBOM, or package provenance.
- Full adopter-facing Testing package creation if Story 10-1 is not implemented yet.
- A new browser-test framework alongside Playwright.
- A cloud visual-regression service requirement.
- A live EventStore, DAPR sidecar, SignalR hub, or Aspire topology dependency for default specimen rendering.
- Recursive or nested submodule initialization.
- A broad Fluent UI, .NET SDK, Playwright, or axe package upgrade without explicit compatibility evidence.
- Fake manual screen reader pass results.
- Broad component-level accessibility remediation outside the specimen infrastructure. Defects discovered by the gate may be fixed only when they are required for the committed specimen surfaces; otherwise file or defer them with owner and evidence.
- Retry or shard behavior that overwrites the first failing accessibility or visual evidence before artifact upload.
- Automatic CI creation of snapshot baselines, suppressions, or specimen manifest entries.

### Known Gaps / Follow-Ups

| Gap | Owner |
| --- | --- |
| RTL visual baseline matrix and direction-specific keyboard assertions. | Future v1.x/v2 accessibility story |
| Pact REST-to-generated-UI contracts and provider verification. | Story 10-3 |
| Stryker.NET mutation score gates and FsCheck command idempotency suites. | Story 10-4 |
| Flaky quarantine automation and CI duration governance. | Story 10-5 |
| Release package signing, SBOM, and test package provenance evidence. | Story 10-6 |
| Browser/device matrix expansion beyond one screenshot baseline lane. | Product/architecture decision after first stable baseline |

---

## References

- [Source: `_bmad-output/planning-artifacts/epics/epic-10-framework-quality-adopter-confidence.md#Story-10.2`] - story statement and acceptance criteria foundation.
- [Source: `_bmad-output/planning-artifacts/epics/requirements-inventory.md#FR76-FR77`] - accessibility and visual specimen functional requirements.
- [Source: `_bmad-output/planning-artifacts/epics/requirements-inventory.md#NFR37-NFR38`] - axe-core and visual baseline CI requirements.
- [Source: `_bmad-output/planning-artifacts/ux-design-specification/visual-design-foundation.md#Type-Specimen`] - type specimen content and baseline discipline.
- [Source: `_bmad-output/planning-artifacts/ux-design-specification/visual-design-foundation.md#Accessibility-Considerations`] - WCAG 2.1 AA commitments and manual verification requirement.
- [Source: `_bmad-output/planning-artifacts/ux-design-specification/ux-consistency-patterns.md#Data-Formatting-Specimen`] - data-formatting specimen requirements.
- [Source: `_bmad-output/planning-artifacts/ux-design-specification/responsive-design-accessibility.md#Validation-Matrix`] - forced-colors, zoom/reflow, and verification log guidance.
- [Source: `_bmad-output/planning-artifacts/architecture.md#Accessibility-Enforcement`] - Playwright-level axe testing strategy.
- [Source: `tests/e2e/package.json`] - current browser-test dependencies and npm scripts.
- [Source: `tests/e2e/playwright.config.ts`] - current browser projects, reporters, artifacts, and snapshot environment.
- [Source: `tests/e2e/helpers/a11y.ts`] - existing axe-core helper to extend.
- [Source: `tests/Hexalith.FrontComposer.Shell.Tests/Generated/AxeCoreA11yTests.cs`] - bUnit accessibility contract checks and browser-layer handoff note.
- [Source: `.github/workflows/ci.yml`] - current CI lanes and artifact behavior.
- [Source: Playwright accessibility testing](https://playwright.dev/docs/next/accessibility-testing) - current `@axe-core/playwright` guidance and WCAG tag usage.
- [Source: Playwright visual comparisons](https://playwright.dev/docs/next/test-snapshots) - current screenshot baseline behavior and determinism warning.
- [Source: Playwright emulation](https://playwright.dev/docs/emulation) - current media and color-scheme emulation guidance.
- [Source: npm `@axe-core/playwright`](https://www.npmjs.com/package/%40axe-core/playwright) - browser-test dependency source.

---

## Party-Mode Review

- Date/time: 2026-05-08T03:01:53+02:00
- Selected story key: `10-2-accessibility-ci-gates-and-visual-specimen-verification`
- Command/skill invocation used: `/bmad-party-mode 10-2-accessibility-ci-gates-and-visual-specimen-verification; review;`
- Participating BMAD agents: Winston (System Architect), Amelia (Senior Software Engineer), John (Product Manager), Murat (Master Test Architect and Quality Advisor)
- Findings summary:
  - Route ownership, route visibility, auth independence, and expected landmark roots needed to be explicit before implementation.
  - Specimen determinism needed stronger invariants for culture, timezone, viewport, device scale factor, fonts, storage, network calls, animations, and ready markers.
  - Axe-only checks risked false-green results without enforceable keyboard, focus, media, zoom, blank-route, console-error, and artifact assertions.
  - Visual baseline governance needed missing-baseline behavior, an intentional local update workflow, and no automatic CI snapshot regeneration.
  - Suppression governance needed route/rule/selector/rationale/owner/review-date evidence, plus rejection of broad rule disables.
  - Adopter-facing evidence, manual screen reader sign-off expectations, localization scope, and custom-component boundaries needed clearer acceptance language.
- Changes applied:
  - Added AC19-AC25 for specimen route contracts, deterministic harness setup, enforceable non-axe checks, artifact validation, baseline governance, adopter interpretation boundaries, and fixed non-default culture coverage.
  - Hardened T1, T3, T4, T5, T6, T7, T8, and T9 with concrete route, selector, media, zoom, artifact, baseline, manual-evidence, and handoff requirements.
  - Strengthened the visual baseline contract with missing-baseline and stabilization rules.
  - Added a cross-suite `tests/e2e/helpers/a11y.ts` contract row for impact thresholds, include/exclude behavior, and artifact schema changes.
  - Added a scope guardrail preventing broad component-level accessibility remediation from being silently absorbed into this story.
- Findings deferred:
  - Full RTL and direction-specific visual matrices remain deferred to the existing v1.x/v2 accessibility follow-up.
  - Full localization layout verification remains deferred; this story only requires one fixed non-default culture for data-formatting text assertions.
- Final recommendation: ready-for-dev

## Advanced Elicitation

- Date/time: 2026-05-08T05:11:47+02:00
- Selected story key: `10-2-accessibility-ci-gates-and-visual-specimen-verification`
- Command/skill invocation used: `/bmad-advanced-elicitation 10-2-accessibility-ci-gates-and-visual-specimen-verification`
- Batch 1 method names: Pre-mortem Analysis; Failure Mode Analysis; Red Team vs Blue Team; Security Audit Personas; Self-Consistency Validation
- Reshuffled Batch 2 method names: Chaos Monkey Scenarios; Hindsight Reflection; Occam's Razor Application; Comparative Analysis Matrix; Architecture Decision Records
- Findings summary:
  - A false-green run could still pass if the test silently scans the wrong route, a partial specimen, or an unowned selector set; a committed specimen manifest gives the gate an oracle before axe and screenshots run.
  - Test-only specimen routes needed an executable fail-closed check so they do not become adopter product routes by accident.
  - Retry and shard behavior could erase first-failure evidence or mask blocking accessibility and visual defects unless artifact preservation is explicit.
  - Axe and Playwright artifacts needed redaction and bounding requirements to avoid leaking local paths, cookies, tokens, full DOM, or environment-specific data.
  - Baseline and suppression governance remained sound only if CI cannot generate baselines, suppressions, or manifest entries automatically.
- Changes applied:
  - Added AC26-AC29 for specimen manifest validation, production route-exposure fail-closed behavior, retry evidence preservation, and artifact redaction/bounding.
  - Hardened T1 with explicit specimen-host configuration, manifest creation, and pre-scan manifest validation.
  - Hardened T4 and T7 with redacted bounded artifacts and retry/shard evidence preservation.
  - Hardened T9 with final verification for manifest, production exposure, artifact redaction, retry preservation, and no automatic baseline or suppression creation.
  - Added scope guardrails preventing retry evidence overwrite and automatic CI creation of snapshots, suppressions, or manifest entries.
- Findings deferred:
  - No new product, architecture, or cross-story scope changes were accepted.
  - Detailed manifest file naming and JSON schema shape remain implementation choices as long as AC26 and the T1/T9 handoff are satisfied.
- Final recommendation: ready-for-dev

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

- `npm --prefix tests/e2e install` - passed after removing unused unavailable `@seontechnologies/playwright-utils` dependency.
- `npm --prefix tests/e2e run typecheck` - passed.
- `dotnet test tests/Hexalith.FrontComposer.Shell.Tests/Hexalith.FrontComposer.Shell.Tests.csproj --configuration Release --filter "FullyQualifiedName~FcStatusBadgeTests|FullyQualifiedName~FrontComposerSpecimenRouteGateTests"` - passed 20 tests.
- `npm --prefix tests/e2e run test:a11y` - passed 18 Chromium specimen tests.
- `npm --prefix tests/e2e run validate:a11y-artifacts` - passed.
- `npm --prefix tests/e2e run validate:visual-governance` - passed for 6 snapshot baselines.
- `dotnet build Hexalith.FrontComposer.sln --configuration Release` - passed.
- `dotnet test Hexalith.FrontComposer.sln --configuration Release --no-build --filter "Category!=Performance&Category!=e2e-palette" --results-directory ./TestResults --logger "trx;LogFileName=test-results-story-10-2.trx"` - passed default lane.

### Completion Notes List

- 2026-05-07: Story created via `/bmad-create-story 10-2-accessibility-ci-gates-and-visual-specimen-verification` during recurring pre-dev hardening job. Ready for BMAD review in a later run.
- 2026-05-08: Party-mode review applied route, determinism, artifact, baseline, suppression, manual-evidence, and adopter-boundary hardening. Ready for advanced elicitation in a later run.
- 2026-05-10: Added explicit specimen route gate (`Hexalith:FrontComposer:Specimens:Enabled`) and sample-only `Counter.Specimens` route assembly so `/__frontcomposer/specimens/type` and `/__frontcomposer/specimens/data-formatting` fail closed outside Development/Test specimen mode.
- 2026-05-10: Added deterministic type and data-formatting specimens, committed manifest, blank/partial guards, stable selectors, local fixture data, fixed culture/timezone expectations, and exact handoff paths.
- 2026-05-10: Hardened Playwright axe helper to keep WCAG 2.1 A/AA tags explicit, fail only `serious`/`critical`, report lower impacts to bounded/redacted JSON artifacts, and fail zero-node or missing-section scans.
- 2026-05-10: Added keyboard, focus-visible, forced-colors, reduced-motion, zoom/reflow, data-formatting text, manifest, route-exposure, and visual baseline Playwright coverage.
- 2026-05-10: Added six v1 visual baselines: Light/Dark x Compact/Comfortable/Roomy, plus local baseline update and governance validation scripts.
- 2026-05-10: Fixed `FcStatusBadge` browser axe finding by adding a valid `role="status"` host role for contextual `aria-label`.
- 2026-05-10: Added CI accessibility/visual lane with root-level submodule checkout only, Chromium install, e2e typecheck, specimen gate, artifact validation, baseline governance, and artifact upload.
- 2026-05-10: Added manual screen-reader verification docs/templates and suppression/baseline governance docs under `docs/accessibility-verification/`.

### File List

- `.github/workflows/ci.yml`
- `Hexalith.FrontComposer.sln`
- `_bmad-output/implementation-artifacts/10-2-accessibility-ci-gates-and-visual-specimen-verification.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `docs/accessibility-verification/README.md`
- `docs/accessibility-verification/baseline-change-rationale.md`
- `docs/accessibility-verification/manual-log-template.md`
- `package.json`
- `samples/Counter/Counter.Specimens/Counter.Specimens.csproj`
- `samples/Counter/Counter.Specimens/FrontComposerDataFormattingSpecimen.razor`
- `samples/Counter/Counter.Specimens/FrontComposerDataFormattingSpecimen.razor.css`
- `samples/Counter/Counter.Specimens/FrontComposerTypeSpecimen.razor`
- `samples/Counter/Counter.Specimens/FrontComposerTypeSpecimen.razor.css`
- `samples/Counter/Counter.Web/Components/Routes.razor`
- `samples/Counter/Counter.Web/Counter.Web.csproj`
- `samples/Counter/Counter.Web/Program.cs`
- `src/Hexalith.FrontComposer.Shell/Components/Badges/FcStatusBadge.razor`
- `src/Hexalith.FrontComposer.Shell/Components/Specimens/FrontComposerSpecimenRoutes.cs`
- `tests/Hexalith.FrontComposer.Shell.Tests/Components/Badges/FcStatusBadgeTests.cs`
- `tests/Hexalith.FrontComposer.Shell.Tests/Specimens/FrontComposerSpecimenRouteGateTests.cs`
- `tests/e2e/helpers/a11y.ts`
- `tests/e2e/helpers/specimen-manifest.ts`
- `tests/e2e/package-lock.json`
- `tests/e2e/package.json`
- `tests/e2e/playwright.config.ts`
- `tests/e2e/scripts/validate-a11y-artifacts.mjs`
- `tests/e2e/scripts/validate-visual-baseline-governance.mjs`
- `tests/e2e/specimens/frontcomposer-specimen-manifest.json`
- `tests/e2e/specs/a11y-helper.spec.ts`
- `tests/e2e/specs/specimen-accessibility.spec.ts`
- `tests/e2e/specs/specimen-accessibility.spec.ts-snapshots/frontcomposer-type-dark-comfortable-chromium-win32.png`
- `tests/e2e/specs/specimen-accessibility.spec.ts-snapshots/frontcomposer-type-dark-compact-chromium-win32.png`
- `tests/e2e/specs/specimen-accessibility.spec.ts-snapshots/frontcomposer-type-dark-roomy-chromium-win32.png`
- `tests/e2e/specs/specimen-accessibility.spec.ts-snapshots/frontcomposer-type-light-comfortable-chromium-win32.png`
- `tests/e2e/specs/specimen-accessibility.spec.ts-snapshots/frontcomposer-type-light-compact-chromium-win32.png`
- `tests/e2e/specs/specimen-accessibility.spec.ts-snapshots/frontcomposer-type-light-roomy-chromium-win32.png`

### Change Log

- 2026-05-10: Completed Story 10.2 implementation and moved status to review.
