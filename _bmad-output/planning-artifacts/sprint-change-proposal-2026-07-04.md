# Sprint Change Proposal — Architecture & Quality Review Remediation (2026-07-04)

- **Workflow:** Correct Course (batch mode, autonomous per user directive "create change proposal all these changes and fix")
- **Trigger artifact:** `_bmad-output/project-docs/architecture-quality-review-2026-07-04.md` (full-repo architecture/engineering-quality review, five parallel deep-dive analyses)
- **Author:** Developer agent (Claude) · **User:** Administrator
- **Approval:** explicitly approved by Administrator on 2026-07-04 ("approve"). Minor-scope fixes committed on branch `fix/architecture-review-quick-wins`; Moderate/Major items routed as Epic 11 backlog stories below.

---

## Section 1 — Issue Summary

A comprehensive architecture and engineering-quality review of Hexalith.FrontComposer (all 9 src projects, 7 test projects, ci-governance, e2e) found **no Critical defects** but 12 High-severity findings and ~28 Medium ones. The review's headline: the codebase is exceptionally well governed, and the real defects cluster in exactly the blind spots its test machinery cannot see — silent Blazor parameter splats, CSS that never applies, codegen paths without fixtures, and cross-request DI lifetimes. Additional themes: Contracts-kernel scope creep (the main obstacle to clean reuse by Hexalith.Tenants), convention drift at scale (one-type-per-file, LoggerMessage), and consolidation debt (security-adjacent helpers duplicated 2–7× with hardening applied unevenly).

This proposal (a) records the immediate Minor-scope fix batch applied under this Correct Course, and (b) creates **Epic 11 — Architecture Review Remediation** carrying the Moderate/Major work as stories.

### Corrections to the review discovered during fixing (evidence-verified)

1. **H8 severity was understated and half-inverted.** Empirical probe (2026-07-04): Roslyn `SymbolDisplay.FormatLiteral(value, quote: false)` escapes control characters and backslashes but does **not** escape embedded double quotes. The three emitters the review scored as "correct" (`CommandFormEmitter`, `CommandPageEmitter`, `CommandRendererEmitter`) therefore had a **latent quote-injection bug** (a `[Display(Name = "Say \"Hi\"")]` label would emit uncompilable generated code), while the "weak" `RegistrationEmitter` chain escaped quotes but missed Unicode line terminators. Both defect classes are now fixed via one shared `GeneratedLiteral.Escape` helper (`FormatLiteral(quote: true)` + strip outer quotes). The existing test `RunGenerators_BoundedContextXmlCharacters_EscapesRegistrationXmlDoc` caught the naive first attempt — evidence the fix is now on the right contract.
2. **H4's one-line recommendation ("register Singleton") is unsafe as stated.** `FrontComposerMcpLifecycleTracker` constructor-injects the **Scoped** `FrontComposerMcpToolAdmissionService` (used on the lifecycle read path for admission re-validation) — a naive Singleton flip is a captive-dependency error under scope validation. The correct fix is a state-store split (Singleton store + Scoped facade). Deferred to Story 11.3.
3. **H10 is worse than a slug divergence.** There are **three** route families: nav/home/palette projection links (`/{bc-lower}/{proj-kebab}`), palette/CTA command links (`CommandRouteBuilder.BuildRoute` → `/domain/{kebab}/{kebab}`), and generated command pages (`[Route("/commands/{BC}/{TypeName}")]`, sanitized not kebab-cased). **No page in the repo resolves the `/domain/…` command routes the palette navigates to.** The slug algorithm itself was unified now (behavior-preserving — the algorithms differed only on acronym runs, which no current consumer uses); the route-family unification is a contract decision routed to Story 11.7.

---

## Section 2 — Impact Analysis (Change-Navigation Checklist outcome)

| Checklist item | Status | Notes |
|---|---|---|
| 1.1 Triggering story | [N/A] | Not story-triggered — triggered by the 2026-07-04 architecture review (all epics 1–8 done; 9–10 backlog) |
| 1.2 Problem statement | [x] | Technical-debt/defect findings from systematic review (category: technical limitation discovered post-implementation) |
| 1.3 Evidence | [x] | Review doc with file:line evidence; fix-phase verification runs recorded below |
| 2.1–2.5 Epic impact | [x] | No completed epic is invalidated; no epic reopened. New **Epic 11** added (same pattern as Epics 8–10, all added via Correct Course). Epic 9/10 unaffected; Epic 11 ordering: independent of 9/10, Story 11.1/11.2 first (production-circuit risk) |
| 3.1 PRD conflicts | [x] | None — requirements inventory (epics.md) untouched; MVP shipped and unaffected |
| 3.2 Architecture conflicts | [x] | Story 11.8 (Contracts kernel split) amends the documented multi-TFM decision in `project-context.md` ("Multi-TFM split … guard net10/Fluent-only code with #if") — that decision traded purity for convenience; the review shows the cost lands on every net10 consumer (Fluent RC pin inheritance). Requires Architect sign-off; `architecture.md` §layers + `project-context.md` update on completion |
| 3.3 UI/UX conflicts | [x] | None user-visible beyond bug fixes (Forbidden/no-party pages now actually render their text; empty states now styled) |
| 3.4 Secondary artifacts | [x] | `sprint-status.yaml` updated (Epic 11); governance tests to be extended per Story 11.5; release.yml package-validation enablement check → Story 11.10 |
| 4.1 Direct adjustment | **Viable — SELECTED** | Effort: Medium (spread over 10 stories); Risk: Low-Medium — additive fixes and split-out stories, no rollback, no MVP change |
| 4.2 Rollback | Not viable | Nothing to roll back — findings are cumulative debt, not a bad recent change |
| 4.3 MVP review | Not viable/needed | MVP delivered; scope unchanged |
| 5.1–5.5, 6.x | [x] | This document; handoff in Section 5; sprint-status updated |

---

## Section 3 — Recommended Approach

**Direct Adjustment**: apply the Minor batch immediately (done, verified below), add Epic 11 with ten stories ordered by production risk, and route two items to explicit PM/Architect decisions (11.7 route contract, 11.8 kernel split). Rationale: no finding invalidates shipped behavior contracts; everything is additive hardening or structured refactoring; the team's own story/review machinery is the right vehicle, and the Epic 8/9/10 precedent shows Correct Course epics integrate cleanly.

---

## Section 4 — Detailed Change Proposals

### 4A — Applied now (Minor scope, this Correct Course) — all verified

1. **UI host pages render no text (review H1).**
   `src/Hexalith.FrontComposer.UI/Components/Pages/AdminLanding.razor`, `Pages/NoPartyBinding.razor`, `Components/Routes.razor`
   OLD: `<FcPageHeader Title="Forbidden" Subtitle="@ForbiddenMessage" />` (nonexistent parameters silently splatted as HTML attributes; `<h1>` suppressed by the blank-heading fail-safe; `FocusOnNavigate Selector="h1"` had no target)
   NEW: `<FcPageHeader Heading="Forbidden" Description="@ForbiddenMessage" />` (×3 pages; `ResolvedPageTitle` falls back to `Heading`, restoring the document title too)

2. **Orphaned stylesheet — empty states shipped unstyled (H5).**
   `git mv` `wwwroot/css/fc-empty-state.scoped.css` → `fc-empty-state.css` (the `.scoped` suffix was misleading — it is a global stylesheet, not Blazor scoped CSS; zero references existed, so the rename is safe) + new `EmptyStateStylesheetPath` property in `FrontComposerShell.razor.cs` + fourth `<link>` in the shell `HeadContent`.

3. **Generated command forms fail to compile for nullable numerics (H3).**
   `src/Hexalith.FrontComposer.SourceTools/Emitters/CommandFormEmitter.cs` `EmitNumericInput`:
   OLD: `_model.{Prop}.ToString(CultureInfo.CurrentCulture)` (CS1501 for `int?`/`decimal?` — `Nullable<T>` has no `ToString(IFormatProvider)`)
   NEW: branches on `field.IsNullable` → `_model.{Prop}?.ToString(CultureInfo.CurrentCulture)`.
   Tests: new fixture `CommandTestSources.NullableNumericCommand` (`int?` + `decimal?`), plus `Emit_NullableNumericField_LiftsCultureToStringThroughNullConditional`, `Emit_NonNullableNumericField_KeepsDirectCultureToString`, `Emit_EndToEnd_NullableNumericCommand_CompilesSuccessfully`.

4. **One escaping helper for all generated-literal emission (H8, upgraded — see Section 1 correction #1).**
   New `src/Hexalith.FrontComposer.SourceTools/Emitters/GeneratedLiteral.cs` (`FormatLiteral(quote: true)` + strip outer quotes). `RegistrationEmitter`, `CommandFormEmitter`, `CommandPageEmitter`, `CommandRendererEmitter` all delegate to it. Fixes the naive chain's missing line-terminator escapes AND the latent unescaped-double-quote bug in the three `quote: false` emitters. (`RoleBodyHelpers`' hand-rolled variant is correct today; consolidating it is Story 11.9.)

5. **Nav slug unified on the canonical D21 kebab contract (H10, narrow part).**
   `FrontComposerNavigation.ToKebab` now delegates to `CommandRouteBuilder.KebabCase` (boundary-aware: `XMLReportView` → `xml-report-view`, never `x-m-l-report-view`); blank-input tolerance preserved. Acronym pinning tests added to `FrontComposerNavigationTests.BuildRouteProducesExpectedHref` (`XMLReportView`, `SKUList`). Behavior-preserving for every existing consumer (no acronym-run type names exist today).

6. **`FcSystemThemeWatcher` disposal-race hardening (M7) + JS rejection guard (L2).**
   Ported the `FcLayoutBreakpointWatcher` pattern: `_disposed` re-checks after each await, `_selfRef` assigned before awaits, `OperationCanceledException`/`JSDisconnectedException` tolerated, `SafeUnsubscribeAsync`/`SafeDisposeAsync` helpers. `fc-prefers-color-scheme.js`: `.catch(() => { })` on both `invokeMethodAsync` calls.

7. **`@key` on runtime-reordering loops (M8).**
   `FcHomeDirectory.razor`: all four card loops keyed by `card.Manifest.BoundedContext` (cards re-sort live as counts arrive; skeleton↔card identity churn eliminated). `FrontComposerNavigation.razor`: manifest and orphan tile/tooltip/flyout trios keyed (`fc-nav-tile-…`/`fc-nav-tooltip-…`/`fc-nav-flyout-…` — id-anchored menus no longer risk positional mispairing) and projection `FluentMenuItem`s keyed by route.

8. **Dead `catch { throw; }` removed** in `ProjectionSubscriptionService.UnsubscribeAsync` (intent comment retained; semantics identical).

9. **Hygiene:** 19 tracked `*.lscache` IDE cache artifacts untracked (`git rm --cached`) + `*.lscache` gitignored; stale `Directory.Packages.props` SDK-pin comment corrected (said 10.0.302; global.json is authoritative at 10.0.302).

**Verification (all Release, direct xUnit v3 in-process runner, `DiffEngine_Disabled=true`):**
- SourceTools.Tests build 0W/0E; full suite **1045 total / 2 failed — both pre-existing** (`settings.md` docs-governance drift; `PackableProjects_UsePackageValidationBaselinePolicy` SourceTools-packability drift — neither file touched). `CommandFormEmitterTests` 33/33 (+3 new); the previously-failing-under-first-attempt `RunGenerators_BoundedContextXmlCharacters_EscapesRegistrationXmlDoc` now green.
- Shell.Tests build 0W/0E; focused lanes green: nav/routing/shell 103/103, home/watcher/page-header 37/37, Fluent+Infrastructure governance 39/39. Broad default lane **2038 total / 5 failed — all 5 pre-existing and verified unrelated** (2× Story 12-4 red-phase ATDD defs expecting a not-yet-written release.yml attestation step; AuthBoundary drift flagging two test files not touched here; 2× CI-workflow governance drift).
- UI host build (Debug/project-references, per the documented dependency-mode split) 0W/0E. Release-mode UI build requires published `Hexalith.Parties.*` NuGets — pre-existing repo condition, unrelated.

### 4B — Deferred to Epic 11 (Moderate/Major)

**Story 11.1 — Token lifecycle and circuit-safe EventStore auth** *(High: H2, M1 — silent production-circuit degradation)*
`FrontComposerUserTokenStore`: store token+expiry, evict expired, wire `Remove` (currently dead code) into the sign-out endpoint. `FrontComposerAccessTokenProvider`: add the `CircuitServicesAccessor`/token-store fallback its sibling seams already have (it currently throws HFC2013 whenever `HttpContext` is null — the normal interactive-circuit state), or fail fast at registration when no circuit-safe source is configured. AC: expired/sign-out eviction pinned; circuit-context token acquisition pinned; no raw-token logging.

**Story 11.2 — Projection realtime resilience** *(High: H6; Medium: M2, M3, M4)*
Unbounded jittered `IRetryPolicy` on the projection hub + restart-on-`Closed` gated by the fallback driver (today realtime dies permanently after the ~42 s default ladder and silently degrades to 15 s polling). Bound `ProjectionSubscriptionService.DisposeAsync`'s gate wait; align the two polling drivers' disposal; lock `FrontComposerRegistry`'s live-list reads; fix the `ETagCacheService` seeding race (`Lazy<Task>`/semaphore, reset on failure). AC: >42 s-outage reconnection pinned; wire method-name literals (`ProjectionChanged`, `JoinGroupScoped`, …) pinned; `SignalRProjectionHubConnectionFactory` gets direct unit tests.

**Story 11.3 — MCP cross-request lifecycle and operability** *(High: H4 corrected; Medium: M9, M10, M12)*
Split `FrontComposerMcpLifecycleTracker` into a Singleton state store + Scoped facade (naive Singleton flip is a captive-dependency error — see Section 1 correction #2); remove the test-side Singleton re-registrations that mask the bug; cross-scope hosting test. Add sanitized `[LoggerMessage]` logging at the zero-signal fail-closed sites (`FrontComposerMcpProjectionReader` bare catch, tools-list, lifecycle auth). Remove `BuildServiceProvider()` from `AddFrontComposerMcp` (ASP0000). Store API-key hashes (or document dev-only). AC: agent lifecycle subscribe→poll across two requests returns real transitions; every fail-closed branch logs one sanitized event.

**Story 11.4 — Security-validation hardening** *(High: H7, H9; Medium: M11)*
`ReturnPathValidator` exhaustive theory per documented attack class (protocol-relative, backslash prefixes, percent-decode bypass, traversal, BiDi/zero-width, Unix file-scheme carve-out, non-root base href) — today this open-redirect funnel has zero direct tests. Converge the two storage-key builders on the canonicalizing `FrontComposerStorageKey` semantics + FsCheck equivalence property (whitespace/colon/NFD-NFC/mixed-case-email). Golden-JSON wire pins (or `[JsonPropertyName]`) for `ProjectionChangedDetail`, `CommandResult`, `ProblemDetailsPayload`; `CommandResultStatus` string constants.

**Story 11.5 — Dead-CSS remediation and visual-conformance guards** *(Medium: M6, M5; governance gap closure)*
Fix the seven scoped-CSS files whose rules are dead because the class sits on a Fluent component (`FcProjectionConnectionStatus` — all rules incl. the reconnect pulse, `FcColumnPrioritizer` gear pinning, `FcSettingsDialog` mobile Done, `FcDensityPreviewPanel`, three DevMode files) via raw scoped root + `::deep` or inline Style, per the Story 8.6 precedent. Fix undefined/FAST-era tokens (`--error`, `--error-foreground-rest`). New governance guards: every `wwwroot/css` file referenced by a `<link>`; scoped-CSS class-on-Fluent-component detector; `error-` added to the legacy-token regex. AC: rendered-DOM/computed-style proof per E8-AI-1.

**Story 11.6 — Testing harness failure modes** *(Medium: M21 — the key Tenants-adoption unblock)*
`TestCommandService`: configurable rejection/timeout/stall-at-Syncing outcomes. `TestQueryService`/`TestProjectionPageLoader`: per-request callbacks (`SucceedWith(Func<QueryRequest, QueryResult<T>>)`) so paging/filter/sort are genuinely testable. `TestFaultInjectionProvider`: actually inject (or rename to evidence recorder). Authorization-policy toggles promoted from the Counter sample into the harness. Replace the constructor `GetAwaiter().GetResult()` with an async factory. Update `PublicAPI.Shipped.txt` intentionally; direct surface tests for `Builders`/`Assertions`/fakes (currently 2 test files for 11 shipped files).

**Story 11.7 — Command/projection route-contract unification (decision + implementation)** *(High: H10 remainder + new finding — see Section 1 correction #3)*
Decide the canonical route families: palette/CTA command links go to `/domain/{kebab}/{kebab}` but generated pages register `/commands/{BC}/{TypeName}` — nothing resolves `/domain/…`. Either CommandPageEmitter emits the `/domain` route (as `[Route]` alias or replacement) or the palette/CTA/`EmptyStateCtaResolver` target the generated route. Add an e2e pin: palette command activation lands on the generated page. Owner: Architect + Product (URL contract is adopter-facing).

**Story 11.8 — Contracts kernel split** *(Major — Architect + PM; High: H11; Medium: M24, M25)*
Split the net10/Blazor surface (`Typography`/`FcTypoToken`, `RenderFragment` contexts, `KeyboardEventArgs` members) into a net10-only `Contracts.UI` assembly so referencing Contracts stops inheriting the pinned Fluent RC; move `InMemoryStorageService` → Testing, `InlinePopoverRegistry` impl → Shell (interface stays), `FcShellOptions` → Shell/options package, Fluxor action records (incl. the `TaskCompletionSource`-bearing `LoadPageAction`) → Shell; decompose the 19-parameter `QueryRequest` into UI query + transport envelope using the existing HFC0001 deprecation pipeline. Amends the documented multi-TFM decision — requires `project-context.md` + `architecture.md` updates and package-compat planning (pre-v1.0 window).

**Story 11.9 — Shell layering and duplication consolidation** *(Medium: M18, M19, clusters)*
Declare the real layering (Telemetry cross-cutting; connection/polling workers → Infrastructure); move `BuildRoute`/`ProjectionLabel` off the nav component into `Routing/`; converge on one non-Fluxor observer primitive and drop `System.Reactive` (sole consumer: `BadgeCountService`). Extract: `StorageScopeResolver` (6× `TryResolveScope` copies), `SnapshotPublisher<T>` (~7 hand-rolled pub/subs with uneven hardening), `ExceptionGuard.IsFatal` (3 filter variants), single `HydrationState` enum (5 copies), `FcJson` options (3 sites); consolidate `RoleBodyHelpers` escaping onto `GeneratedLiteral`. Add an architecture test pinning folder dependency directions.

**Story 11.10 — Convention-alignment program** *(Medium: M14, M15, M16, H12; plus small items)*
One-type-per-file split of the worst offenders (`MigrationCommand.cs` 23 types, `SkillCorpus.cs` ~45 — move the LLM benchmark harness out of the runtime package, `DriftDetection.cs` 17, `InspectCommand.cs` 14, plus the interface+impl+DTO bundles in Shell; document the Fluxor-action-group exception if retained). LoggerMessage migration for Warning-and-above and hot paths (206 direct sites / 50 files in Shell alone). Un-dead the CS1591 config for Contracts public folders (the `.editorconfig` re-raise is inert under the src-wide NoWarn — the documented enforcement is not in force). Replace the AppHost blanket `NU1902-04` NoWarn with per-advisory `NuGetAuditSuppress` (deferred from the fix batch: NuGet-audit network access is blocked locally, so the change is only verifiable in CI). Localize the `FcHomeCard` aria-label and the UI host's hardcoded `lang="en"`/English strings; rename the `HFC2106_ThemeHydrationEmpty` constant (ID string unchanged) with an obsolete alias if the constant is public. Decision item (Architect): whether elevating built-in analyzers (`AnalysisMode Recommended`) is compatible with the documented no-third-party-analyzer policy — it adds no packages, but the burn-down cost must be owned.

---

## Section 5 — Implementation Handoff

| Scope | Items | Route to | Status |
|---|---|---|---|
| **Minor** | Section 4A fix batch (9 items) | Developer agent | **Applied + verified** in working tree; commit as `fix:`/`refactor:`/`test:`/`chore:` conventional commits on a `fix/` branch (no direct commits to `main`) |
| **Moderate** | Stories 11.1–11.6, 11.9, 11.10 | PO/Developer via create-story → dev-story → code-review cycle | Backlog in `sprint-status.yaml` |
| **Major** | Story 11.7 (route contract decision), 11.8 (kernel split) | Product Manager + Solution Architect first (decision), then Developer | Backlog; blocked on decision |

**Suggested order:** 11.1 → 11.2 (silent production-circuit risks) → 11.4 (security test debt) → 11.3 → 11.5 → 11.6 (Tenants unblock; can run parallel to 11.3) → 11.7 decision → 11.9 → 11.10 → 11.8 last (largest API surface, pre-v1.0).

**Success criteria:** each story lands with the review finding IDs it closes referenced in its Change Log; the four blind-spot guard classes (unlinked stylesheets, dead scoped CSS, parameter-splat surfaces, cross-request lifetimes) each gain a durable governance test; Shell broad default lane stays at (or below) the current 5-failure pre-existing baseline; no new NoWarn/pragma without written justification.

**MVP impact:** none — MVP epics 1–7 remain done; Epic 11 is post-MVP hardening like Epics 8–10.
