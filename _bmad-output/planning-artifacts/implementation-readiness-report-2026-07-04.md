---
stepsCompleted: ['step-01-document-discovery', 'step-02-prd-analysis', 'step-03-epic-coverage-validation', 'step-04-ux-alignment', 'step-05-epic-quality-review', 'step-06-final-assessment']
status: complete
overallReadiness: 'READY for Epics 9–10 (imminent wave) · NEEDS WORK for Epic 11 planning material — write the Epic 11 epics.md section + assign 11.7/11.8 decision owners before any 11.x create-story run.'
assessmentScope: 'Whole-plan readiness audit focused on the un-audited additions since the 2026-06-21 report: Epics 9, 10, and 11 (created today via Correct Course), plus epics.md ↔ sprint-status.yaml alignment.'
priorReport: 'implementation-readiness-report-2026-06-21.md (verdict: READY — minor cleanups only; covered Epics 1–7 with Epic 8 in flight)'
requirementsBaseline:
  - '_bmad-output/planning-artifacts/epics.md (Epics 1–10; updated 2026-07-01)'
  - '_bmad-output/project-docs/project-overview.md'
  - '_bmad-output/planning-artifacts/sprint-change-proposal-2026-07-04.md (§4B = Epic 11 story definitions)'
  - '_bmad-output/implementation-artifacts/spec-frontcomposer-ui-tenants-parties-modules-ui.md'
  - '_bmad-output/implementation-artifacts/spec-tenants-ui-menu-icon-label-stack.md'
architectureBaseline:
  - '_bmad-output/project-docs/architecture.md (updated 2026-07-01)'
  - '_bmad-output/project-docs/architecture-quality-review-2026-07-04.md (Epic 11 trigger artifact; §1 corrections in the 07-04 proposal)'
  - '_bmad-output/project-docs/api-contracts.md'
  - '_bmad-output/project-docs/data-models.md'
  - '_bmad-output/project-docs/component-inventory.md'
  - '_bmad-output/project-docs/source-tree-analysis.md'
epicsAndStories:
  - '_bmad-output/planning-artifacts/epics.md (Epics 1–10)'
  - '_bmad-output/planning-artifacts/sprint-change-proposal-2026-07-04.md (Epic 11, stories 11.1–11.10)'
  - '_bmad-output/implementation-artifacts/ (47 story files 1-0..8-7 + deferred-work.md + 8 epic retros)'
  - '_bmad-output/implementation-artifacts/sprint-status.yaml (updated 2026-07-04)'
uxBaseline:
  - '_bmad-output/project-docs/architecture.md §4 (Fluent UI rules)'
  - '_bmad-output/contracts/ (27 fc-* contract specs, incl. fc-a11y, fc-lyt, fc-tbl)'
documentsUnderReview:
  - 'epics.md'
  - 'sprint-change-proposal-2026-07-04.md'
  - 'project-overview.md'
  - 'architecture.md'
  - 'architecture-quality-review-2026-07-04.md'
---

# Implementation Readiness Assessment Report

**Date:** 2026-07-04
**Project:** Hexalith.FrontComposer

---

## Step 1 — Document Discovery

**Context:** Brownfield, largely-implemented project. Epics 1–8 are done with retrospectives; Epics 9–10 are backlog (added via Correct Course, no story files yet); **Epic 11 was added today** (2026-07-04) by an approved Correct Course from the architecture quality review. Requirements are not captured as a classic standalone PRD/UX spec — they live in epics + project overview + 27 formal `fc-*` contracts (same accepted substitute model as the 2026-06-21 audit).

### Inventory

| Type | Status | Source |
|------|--------|--------|
| PRD | ⚠️ Missing standalone (accepted) | Substitutes: `project-docs/project-overview.md` + `epics.md` requirements sections + 2 spec files (07-01/07-03) |
| Architecture | ✅ Whole | `project-docs/architecture.md` (07-01) + api-contracts, data-models, component-inventory, source-tree-analysis + `architecture-quality-review-2026-07-04.md` (today) |
| Epics & Stories | ⚠️ Split across two sources | `planning-artifacts/epics.md` (**Epics 1–10 only**) + `sprint-change-proposal-2026-07-04.md` §4B (**Epic 11**) + 47 story files (1-0..8-7) + 8 retros + `sprint-status.yaml` (today) |
| UX | ⚠️ Missing standalone (accepted) | Substitutes: `architecture.md §4` + `fc-a11y`/`fc-lyt`/`fc-tbl` and 24 more `fc-*` contracts |
| Contracts | ✅ | `contracts/` — 27 `fc-*` specs |

### Discovery Notes / Issues

1. **No whole-vs-sharded duplicates** across any document type.
2. ⚠️ **No standalone PRD or UX document** — carried forward as accepted for this brownfield audit (same as 06-21).
3. 🔴 **Epic 11 is registered in `sprint-status.yaml` (backlog, stories 11-1..11-10) but absent from `epics.md`.** Its authoritative definitions live only in `sprint-change-proposal-2026-07-04.md` §4B. The epics document and sprint status have diverged; traceability chain runs review-doc → proposal → sprint-status, bypassing `epics.md`.
4. ⚠️ **No story files yet for Epics 9, 10, 11** — expected for backlog epics (pre-`create-story`), but the readiness of their epic-level definitions is in scope for this audit.
5. 📌 **Stories 11.7 and 11.8 are explicitly blocked on PM/Architect decisions** (route-contract unification; Contracts kernel split amending the documented multi-TFM decision).

**Baseline confirmed by user (including today's sprint-change proposal) — proceeding to Step 2 (PRD / Requirements analysis).**

---

## Step 2 — PRD Analysis

**PRD substitute set read completely:** `epics.md` Requirements Inventory (1299 lines), `project-overview.md`, `spec-frontcomposer-ui-tenants-parties-modules-ui.md`, `spec-tenants-ui-menu-icon-label-stack.md`, `sprint-change-proposal-2026-07-04.md` (§4B Epic 11 definitions). The epics.md frontmatter itself declares: *"No authored PRD … Requirements below are REVERSE-ENGINEERED from the brownfield documentation set"* — FR/NFR sections are a capability inventory + acceptance baseline; the `FC-*`/AR items are the forward roadmap.

### Functional Requirements Extracted

**Source generator (`Hexalith.FrontComposer.SourceTools`)**

- FR1: From each `[Projection]`-annotated `partial` type, generate 5 files — projection view (`{T}.g.razor.cs` with Loading/Empty/Data states dispatched by `ProjectionRole`), `{T}Feature.g.cs`, `{T}Actions.g.cs`, `{T}Reducers.g.cs`, `{T}Registration.g.cs`.
- FR2: From each `[Command]`-annotated type (public parameterless ctor + `MessageId`), generate 6–7 files (`CommandForm`, `CommandActions`, `CommandLifecycleFeature`, `CommandRegistration`, `CommandRenderer`, `CommandLastUsedSubscriber`, `CommandLifecycleBridge`, plus `CommandPage` when density = `FullPage`).
- FR3: Apply the spec-locked command density rule — non-derivable property count ≤1 → `Inline`, 2–4 → `CompactInline`, ≥5 → `FullPage` — excluding derivable fields (`MessageId`, `CommandId`, `CorrelationId`, `TenantId`, `UserId`, `Timestamp`, `CreatedAt`, `ModifiedAt`, `[DerivedFrom]`).
- FR4: Emit compilation-level `FrontComposerMcpManifest.g.cs` and `FrontComposerProjectionTemplateManifest.g.cs`, each carrying schema fingerprints.
- FR5: Honor the full attribute vocabulary: `[BoundedContext]`, `[ProjectionRole]`, `[ProjectionBadge]`, `[ColumnPriority]`, `[ProjectionFieldGroup]`, `[ProjectionEmptyStateCta]`, `[Destructive]`, `[RequiresPolicy]`, `[DerivedFrom]`, `[Icon]`, `[RelativeTime]`, `[Currency]`, `[ProjectionTemplate]` (plus `[Display]`, `[Description]`, `[DefaultValue]`, `[Flags]`).
- FR6: Emit the HFC1001–HFC1070 diagnostic catalog (build-time `HFC1xxx`) for invalid annotation/usage, with severities as cataloged.
- FR7: Provide opt-in drift detection (`HfcDriftDetectionEnabled=true`) comparing the current snapshot to a checked-in JSON baseline `AdditionalText` → structural HFC1065 / metadata HFC1066; pipeline must not depend on `CompilationProvider`.
- FR8: Support 4-level customization (Level-2 `ProjectionTemplate`, Level-3 field-slot, Level-4 full-view overrides) so external assemblies can inject alternate render fragments.

**Blazor Shell (`Hexalith.FrontComposer.Shell`)**

- FR9: Compose generated UI into a complete app frame via `<FrontComposerShell>@Body</FrontComposerShell>` — `FluentLayout` Header/Navigation/Content/Footer, skip links, `FluentProviders`, global shortcuts (`Ctrl+,` settings, `Ctrl+K` palette).
- FR10: Provide the DI bootstrap path: `AddHexalithFrontComposerQuickstart()` → `AddHexalithDomain<TMarker>()` → `AddHexalithEventStore(...)`.
- FR11: Render projections in `FluentDataGrid` with column filtering, expand-in-row detail, status badges, empty/loading states, slow-query/max-items notices, and column prioritization for >15-column projections.
- FR12: Drive the command lifecycle UI (`Idle→Submitting→Acknowledged→Syncing→Confirmed/Rejected`) with form-abandonment guard and destructive-command confirmation dialog.
- FR13: Connect to EventStore via SignalR (projection subscriptions) and HTTP (commands/queries), surfacing reconnect/reconciliation status.
- FR14: Provide registry-driven navigation, home directory (urgency-sorted bounded-context cards), command palette (ARIA combobox), and badge counts.
- FR15: Manage theme, density, and settings, persisted via `IStorageService` (`LocalStorageService`).

**MCP server (`Hexalith.FrontComposer.Mcp`)**

- FR16: Expose each generated command as an MCP tool (built dynamically at every `tools/list`) plus a fixed `frontcomposer.lifecycle.subscribe` polling tool.
- FR17: Expose projections (`frontcomposer://<bounded-context>/projections/<projection-name>`, tenant-scoped Markdown) and skill-corpus docs (`frontcomposer://skills/<id>`) as MCP resources.
- FR18: Enforce fail-closed security — both `IFrontComposerMcpTenantToolGate` and `IFrontComposerMcpResourceVisibilityGate` required or startup throws; opaque error shape; server-controlled fields (`TenantId`/`UserId`/`MessageId`/`CorrelationId`) blocked from tool input.
- FR19: Negotiate schema compatibility (`McpSchemaNegotiator`: Exact / CompatibleAdditive / CompatibleWarning / Incompatible) and block side-effects on mismatch.

**CLI (`Hexalith.FrontComposer.Cli`)**

- FR20: `frontcomposer inspect` reads generated output + `*.diagnostics.json` sidecars and reports forms/grids/registrations/manifest entries/warnings/errors in text or JSON (`frontcomposer.cli.inspect.v1`).
- FR21: `frontcomposer migrate` plans/applies allowlisted Roslyn code-fixes across catalog version edges (dry-run default, atomic apply, path-safety refusals), JSON `frontcomposer.cli.migrate.v1`.

**Testing library (`Hexalith.FrontComposer.Testing`)**

- FR22: Provide a pre-wired bUnit host + deterministic fakes (command/query/projection/fault-injection), evidence recorders, and assertion helpers for adopters testing generated components.

**Total FRs: 22**

### Non-Functional Requirements Extracted

- NFR1: `TreatWarningsAsErrors=true` everywhere; built-in .NET/Roslyn analyzers only (no Sonar/StyleCop/Roslynator).
- NFR2: ULIDs (26-char Crockford base32) via `IUlidFactory` — never GUIDs — for `messageId`/`correlationId`.
- NFR3: Incremental-cache invariant — pure, fully-equatable IR; no `ISymbol` escapes the parse stage; `EquatableArray<T>` for collections.
- NFR4: Schema fingerprint determinism — `CanonicalSchemaMaterial` pins `JavaScriptEncoder.Create(UnicodeRanges.All)`, STJ source-gen context, `AbsentValueSentinel="<absent>"`, `StringComparer.Ordinal`; changing any invalidates all baselines.
- NFR5: Multi-TFM — Contracts & SourceTools target `net10.0`+`netstandard2.0`; net10/FluentUI code guarded by `#if NET10_0_OR_GREATER`.
- NFR6: Accessibility (WCAG) — `aria-label`/`role`/`aria-live`/`data-testid` on every interactive element; focus visibility, reduced-motion and forced-colors fallbacks; override-accessibility diagnostics HFC1050–HFC1055.
- NFR7: Generated-output path is a public contract (`GeneratedOutputPathContract.Template`) validated in Debug and Release.
- NFR8: Ships as signed NuGet packages (`.nupkg`+`.snupkg`); semantic-release from Conventional Commits; no Dockerfiles/containers.
- NFR9: Fluxor single-writer discipline per slice (ADR-007); scoped-lifetime discipline for storage/effects/auth/tenant accessors (ADR-030).
- NFR10: Test discipline — solution-level `dotnet test` + trait filters, `DiffEngine_Disabled=true`, Governance + Contract lanes blocking; committed `.verified.txt`, `PublicAPI.Shipped.txt`, pacts updated intentionally.
- NFR11: Telemetry via `FrontComposerActivitySource` (OpenTelemetry `ActivitySource`). Owned cross-cutting (not per-AC traced).
- NFR12: Dependency direction points down to `Contracts`; `SourceTools` references only `Contracts` to stay netstandard2.0-clean.
- NFR13: Confirmed (2026-06-21) Trim/AOT readiness — `PublishTrimmed`/`PublishAot` enable the HFC1070 advisory; reflection projection catalog needs an `IActionQueueProjectionCatalog` override.
- NFR14: Root-declared Hexalith submodules live under `references/Hexalith.*`; initialize only root `.gitmodules` entries, never recurse into nested submodules, never modify submodule files without explicit approval. Debug/source builds use local `ProjectReference`s; Release/package builds use published NuGet packages.

**Total NFRs: 14**

### Additional Requirements (AR — forward roadmap)

- AR1 (🔴 FC-LYT): Confirm full-width vs constrained `<PageLayout>` contract. ✅ Confirmed 2026-06-21 (FullWidth default + 75rem max-measure, per UX-DR7).
- AR2 (🔴 FC-A11Y): Confirm accessibility primitives as a reusable, documented contract — every story's ready-gate.
- AR3 (🔴 FC-L10N): Confirm shell-vs-Tenants localized-string ownership (`FcShellResources.resx`).
- AR4 (🔴 FC-DOC): Confirm component documentation contract for shell components.
- AR5 (🔴 Shell-integration spike): Verify `AddHexalithFrontComposer*` / manifest / projection-routing / FC-TBL APIs (Story 1.0).
- AR6 (🟠 FC-CMD): Confirm command-lifecycle contract — pending-identity/correlation-key shape, uniqueness scope, lifecycle ownership, `alreadyApplied` semantics, reconciliation.
- AR7 (🟠 FC-CNC): Confirm one-at-a-time command execution as the v1 contract (fallback approved; batching fast-follow).
- AR8 (🟠 Numeric budgets): ✅ Confirmed 2026-06-21 — confirming→degraded (`TimeoutActionThresholdMs=10_000`), polling (cadence 1_000 / max 120_000), retry (Epic 3 `0`; Epic 4 `1×250ms`) ratified in fc-cmd contracts.
- AR9 (🟡 EventStore status contract): Confirm-stable `GET /api/v1/commands/status/{id}` as the polling coordinator's binding.
- AR10 (Out of scope v1/fast-follow): Do NOT build `<AuditTimeline>`/`<ConsequencePreview>` now; approved fallbacks stand.
- AR11 (FC-NIP): Confirm + implement the row-level new-item producer contract for `FcNewItemIndicator` — producer from command outcome context with precise row identity (`EntityKey` or approved equivalent), not the projection nudge seam.
- AR12 (FC-TOOL-GOV): Preserve Epic 7 authoring-tooling follow-through as explicit backlog work — mechanical story evidence reconciliation, historical-label cleanup, CLI text/JSON parity, HFCM9002 production-emission decisioning, default-lane Testing redaction coverage.

**Process requirement:** Contract-confirmation Definition-of-Done (2026-06-21 amendment) — a `Confirm…`/`Establish…` story MUST NOT reach Done on "escalated with an owner" alone; Done requires the decision confirmed OR a tracked, dated, owned blocking follow-up.

### UX Design Requirements (UX-DR — confirmed 2026-06-21; DR2/DR3 amended since)

- UX-DR1: Design tokens — `Typography` (9 `FcTypoToken` roles → Fluent v5 `TextSize`/`TextWeight`/`TextTag`, pinned `TypographyMappingVersion="3.1.0"`); `DensityLevel`/`DensitySurface` via `<body data-fc-density>`.
- UX-DR2 (amended 2026-06-25, Epic 8/Story 8.7): status members render as a colored Fluent icon (green check / red cross / grey question) with label on hover AND keyboard focus via `FluentTooltip`, always-present `aria-label`; numeric count slots keep the `FluentBadge` pill. Supersedes the pill-only status model.
- UX-DR3 (amended): responsive breakpoint behaviour; unified `FrontComposerNavigation` rail at 72px labelled / 48px icon-only; `FcHamburgerToggle` always visible, Desktop toggles labelled↔icon-only; exactly one active nav item (longest segment-prefix).
- UX-DR4: Reusable interaction components — `FcCommandPalette` (ARIA combobox), `FcSettingsDialog`, `FcDestructiveConfirmationDialog`, `FcFormAbandonmentGuard`, `FcLifecycleWrapper`.
- UX-DR5: Status & empty/loading UX — `FcProjectionLoadingSkeleton` (Card/Timeline/Grid), `FcProjectionEmptyPlaceholder`, `FcProjectionConnectionStatus`, `FcPendingCommandSummary` (`aria-live`).
- UX-DR6: Accessibility patterns — skip links, focus indicators, `role="region"` row-detail + live-region for filter-hidden expansions (WCAG 4.1.2), keyboard reachability, reduced-motion/forced-colors fallbacks.
- UX-DR7 (FC-LYT): Page layout contract — ✅ confirmed 2026-06-21 (FullWidth default + 75rem max-measure).
- UX-DR8: Account control & server security — framework-owned `FcAccountMenu` rendered always; `AddHexalithFrontComposerServerSecurity` framework-owned server wiring. Traceability is change-proposal-of-record (no numbered story; documented note in epics.md).

### Requirements Introduced OUTSIDE the epics.md Inventory (new since 06-21)

1. **Combined UI host (spec, done 07-03):** `Hexalith.FrontComposer.UI` Blazor Server host composing Tenants + Parties modules under one FrontComposer shell, with AppHost `frontcomposer-ui` resource, bearer-token relay, OIDC fail-closed degraded modes. **Not represented by any FR** — the FR inventory still describes 7 source projects; the UI host + AppHost wiring exist in code and a done spec only.
2. **Tenants UI menu icon/label stack (spec, done 07-01):** rail tiles stack icon above label via vertical FluentStack; badges stay outside the stack — refines UX-DR3/Story 8.5 behaviour.
3. **Epic 11 remediation requirements (proposal §4B, 2026-07-04):** ten stories (11.1–11.10) carrying High/Medium architecture-review findings — token lifecycle/circuit-safe auth, projection realtime resilience, MCP cross-request lifecycle, security-validation hardening, dead-CSS remediation + guards, testing-harness failure modes, route-contract unification (decision), Contracts kernel split (decision, amends NFR5/NFR12 boundary), Shell layering consolidation, convention alignment. **Absent from epics.md entirely.**

### PRD Completeness Assessment

**Strengths:** The reverse-engineered inventory is exceptionally disciplined for a brownfield project — 22 FRs and 14 NFRs each carry component ownership, a coverage map to epics, and confirmation-status annotations; UX-DRs record their amendment history with source-of-record proposals; the contract-confirmation DoD closes the "escalated = done" loophole; out-of-scope items (AR10) are explicit.

**Gaps (carried into Step 3 coverage validation):**
1. 🔴 **Epic 11's requirements exist only in a change proposal.** No FR/NFR/AR entry and no epics.md section traces them; the FR Coverage Map ends at Epic 10.
2. 🟠 **The `Hexalith.FrontComposer.UI` combined host has no requirement-level home** — a shipped, spec'd product surface (host project + AppHost resources + Parties integration) invisible to the FR inventory (which still says "7 source projects"; the 07-04 review counts 9).
3. 🟡 Story 11.8 (kernel split) would **amend NFR5/NFR12's documented boundary** — flagged in the proposal as requiring Architect sign-off; the NFR text does not yet carry a pending-amendment marker (same pattern UX-DR2/DR3 used).
4. 🟡 `project-overview.md` version table has drifted from `project-context.md` (e.g. MCP 1.3.0 vs 1.4.0, SDK 10.0.300 vs 10.0.301, bUnit/NSubstitute versions) — cosmetic for this audit, but it is the PRD-substitute's tech-stack section.

---

## Step 3 — Epic Coverage Validation

The epics document carries an explicit **FR Coverage Map** (epics.md, "FR Coverage Map" section) plus an Additional-requirement coverage line and a cross-cutting NFR statement. Story-key registration was cross-checked against `sprint-status.yaml` (2026-07-04): Epic 9 (9-1, 9-2), Epic 10 (10-1..10-5), and Epic 11 (11-1..11-10) keys all match their defining documents exactly.

### Coverage Matrix

| FR | Requirement (abbrev.) | Epic Coverage | Status |
|----|----------------------|---------------|--------|
| FR1 | Projection → 5 generated files | Epic 2 (Story 2.1) | ✓ Covered (done + retro) |
| FR2 | Command → 6–7 generated files | Epic 3 (Story 3.1) | ✓ Covered (done + retro) |
| FR3 | Density rule | Epic 3 (Story 3.2) | ✓ Covered (done + retro) |
| FR4 | MCP/template manifests | Epic 5 (Story 5.1) | ✓ Covered (done + retro) |
| FR5 | Attribute vocabulary | Epic 2 (projection attrs) + Epic 6 (template/slot/override attrs) | ✓ Covered (done + retro) |
| FR6 | HFC1001–1070 diagnostics | Epic 7 (Story 7.3; emitted per-epic) | ✓ Covered (done + retro) |
| FR7 | Opt-in drift detection | Epic 7 (Story 7.4) | ✓ Covered (done + retro) |
| FR8 | 4-level customization | Epic 6 (Stories 6.1–6.3) | ✓ Covered (done + retro) |
| FR9 | Shell frame | Epic 1 (Story 1.1); refined by Epic 8 (8.1, 8.3) | ✓ Covered (done + retro) |
| FR10 | DI bootstrap path | Epic 1 (Story 1.1) | ✓ Covered (done + retro) |
| FR11 | DataGrid surface | Epic 2 (Stories 2.3–2.5) | ✓ Covered (done + retro) |
| FR12 | Command lifecycle UI | Epic 3 (3.4) + Epic 4 (4.1, 4.2, 4.4) | ✓ Covered (done + retro) |
| FR13 | EventStore SignalR/HTTP clients | Epic 2 (2.6) + Epic 3 (3.5) + **Epic 9** (fresh-row producer evidence) | ✓ Covered (9 = backlog) |
| FR14 | Nav/home/palette/badges | Epic 2 (2.2, 2.7) + **Epic 9** (row-level new-item producer); refined by Epic 8 (8.5) | ✓ Covered (9 = backlog) |
| FR15 | Theme/density/settings | Epic 1 (Story 1.6); refined by Epic 8 (8.4) | ✓ Covered (done + retro) |
| FR16 | MCP command tools | Epic 5 (5.1, 5.2) | ✓ Covered (done + retro) |
| FR17 | MCP projection/skill resources | Epic 5 (5.3) | ✓ Covered (done + retro) |
| FR18 | Fail-closed MCP security | Epic 5 (5.4) | ✓ Covered (done + retro) |
| FR19 | MCP schema negotiation | Epic 5 (5.5) | ✓ Covered (done + retro) |
| FR20 | `frontcomposer inspect` | Epic 7 (7.1) + **Epic 10** (10.3 text-parity guard) | ✓ Covered (10 = backlog) |
| FR21 | `frontcomposer migrate` | Epic 7 (7.2) + **Epic 10** (10.4 HFCM9002 decision) | ✓ Covered (10 = backlog) |
| FR22 | Testing library | Epic 7 (7.5) + **Epic 10** (10.5 redaction guard) | ✓ Covered (10 = backlog) |

**Additional requirements:** AR1–AR5 → Epic 1 ✓ · AR6 → Epic 3 ✓ · AR7 → Epic 4 ✓ · AR8 → Epics 3+4 ✓ (confirmed 06-21) · AR9 → Epic 3 ✓ · AR10 → explicitly out of scope ✓ · AR11 → Epic 9 ✓ · AR12 → Epic 10 ✓. **UX-DRs:** all eight map to Epic 1/2 stories or carry the documented change-proposal-of-record traceability note (UX-DR3 refinements, UX-DR8). **NFRs:** declared cross-cutting ready-gates anchored by FC-A11Y/FC-DOC in Epic 1; NFR11 explicitly owned cross-cutting.

### Missing Requirements

**No PRD FR lacks epic coverage.** The gaps run in the REVERSE direction — implemented or planned work without a requirements anchor:

1. 🔴 **Epic 11 (11.1–11.10) traces to no FR/NFR/AR and does not appear in epics.md.** Its requirement basis is the 12 High + ~28 Medium review findings (H1–H12, M-series) in `architecture-quality-review-2026-07-04.md`, carried only by the change proposal. Several stories are requirement-bearing, not just refactoring: 11.7 decides an **adopter-facing URL contract** (proposal evidence: *no page resolves the `/domain/…` command routes the palette navigates to* — arguably a latent FR14 defect); 11.8 **amends the NFR5/NFR12 boundary** (Contracts kernel split); 11.4 hardens an **open-redirect funnel with zero direct tests** (NFR-security-relevant, and no security NFR exists in the inventory to trace it to).
   - Impact: the FR Coverage Map is no longer the single traceability surface; future audits (and `create-story` context assembly) will miss Epic 11's requirement links.
   - Recommendation: append an Epic 11 section to epics.md (same pattern as Epics 8–10: source-of-record header + story list + "FRs covered/refined" line, e.g. "Refines: FR13/FR14 routes (11.7), NFR5/NFR12 (11.8), NFR6 (11.5), FR22 (11.6); introduces review-finding requirements H1–H12/M-series"), and extend the FR Coverage Map footer.
2. 🟠 **The shipped `Hexalith.FrontComposer.UI` combined host (spec done 07-03) has no FR and no epic/story.** It is a product surface (host + AppHost resources + Parties integration) delivered via the spec/quick-dev lane outside the epic plan.
   - Impact: coverage statistics overstate completeness; Epic 11 fix #1 (H1, FcPageHeader params) already landed in this host, so it is production-relevant surface with no requirements anchor.
   - Recommendation: add an FR (e.g. FR23: combined-host composition of module UIs) or an epics.md note designating the spec as change-proposal-of-record — mirroring the UX-DR8 precedent.
3. 🟡 **Story 2.6's "new item indicator" AC** (*"the grid updates and a 'new item' indicator marks fresh rows"*) reads as satisfied in the done Epic 2, while the epics.md Epic 9 header records it as the **accepted-deferred AC1(b) gap**. Consistent with the 07-01 proposal, but a reader of Story 2.6 alone would over-count delivered scope. Non-blocking; the Epic 9 header note is the accepted mitigation.

### Coverage Statistics

- Total PRD FRs: **22** — covered in epics: **22** → **100%**
- Additional requirements: **12/12** mapped (incl. 1 explicit out-of-scope)
- UX-DRs: **8/8** traced (2 via documented change-proposal-of-record notes)
- Reverse-traceability exceptions: **2** (Epic 11 → no requirements anchor; UI host spec → no FR) + 1 informational (Story 2.6 AC wording vs deferred gap)

---

## Step 4 — UX Alignment Assessment

### UX Document Status

**Not found (standalone)** — accepted substitute set, unchanged from the 06-21 audit: `architecture.md` §4 (§4.1 Fluent-only + no-theme-redefinition + accent-as-thread policies with three named Governance guards; §4.2 FluentAccordion guideline; §4.3 layout-component guideline), the UX-DR1–UX-DR8 block in epics.md, and the UX-bearing `fc-*` contracts (`fc-a11y`, `fc-lyt`, `fc-tbl`, `fc-doc`, `fc-l10n`, `fc-settings`, `fc-lst-dtl`). This project is unambiguously UI-centric, so the substitute set is load-bearing — and it is unusually strong: guard-enforced policies, documented carve-out allowlists, and amendment history with sources of record.

### Alignment Findings

**Aligned (verified):**
1. **UX-DR ↔ Architecture §4 is bidirectionally consistent.** The UX-DR2 amendment (colored-icon status, 06-25) and UX-DR3 refinements (always-visible hamburger, single-active-item, icon-over-label rail) are recorded in BOTH epics.md and architecture.md §4.1 with the same supersession language and the same source-of-record proposals. Epic 8's refinements (accent-as-thread + Story 8.2 guard, `FcPageToolbar`, compact density) are fully architected in §4.1.
2. **The 07-01 Tenants-UI menu spec conforms** to Story 8.5/§4.3: icon-over-label via a Fluent layout primitive, badges outside the stack, layout-only CSS — with pinning tests.
3. **Accessibility (NFR6/fc-a11y) remains the universal ready-gate**; Epic 11's UX-touching story (11.5) explicitly requires rendered-DOM/computed-style proof, consistent with the Epic 8 retro's E8-AI-1 action.

**Misaligned / gaps:**
4. 🟠 **UX contract vs rendered reality — the review exposed enforcement blind spots.** Architecture §4's guard system asserts *source-level* conformance, but the 07-04 review found the *delivered* UX diverged: seven scoped-CSS files are dead because their selectors target Fluent components (so `FcProjectionConnectionStatus` — a UX-DR5 component — ships with ALL its styling including the reconnect pulse inert; `FcColumnPrioritizer` gear pinning, `FcSettingsDialog` mobile Done, `FcDensityPreviewPanel` similarly affected); UI-host pages rendered **no heading/description text** at all (H1, silent parameter splat — fixed in the Minor batch); the empty-state stylesheet was never linked (H5 — fixed). Story 11.5 closes the remainder and adds durable guards (link-reference guard, scoped-CSS-on-Fluent detector, `error-` legacy-token regex). Until 11.5 lands, UX-DR5's visual guarantees are partially unmet in production.
5. 🟠 **Broken UX journey: palette command activation navigates to routes nothing resolves.** Per the proposal's evidence-verified correction #3, palette/CTA command links target `/domain/{kebab}/{kebab}` while generated pages register `/commands/{BC}/{TypeName}` — the FR14/UX-DR4 "jump to any action" journey dead-ends for command targets. Story 11.7 (decision: Architect + Product) owns unification + an e2e pin. This is the single most user-visible open defect in the plan.
6. 🟡 **Architecture document drift (staleness, not conflict):** `.gitmodules` now declares **8** root submodules (incl. `references/Hexalith.Parties`) but architecture.md §8 lists 7 (no Parties); the layer diagram/project lists predate the `Hexalith.FrontComposer.UI` combined host; NFR14's submodule enumeration in epics.md is likewise short by one. The 07-04 quality review (which counts "all 9 src projects") is now the more current structural description.
7. 🟡 **Story 11.8 (Contracts kernel split) will move UX-owned types** (`Typography`/`FcTypoToken` — the UX-DR1 token surface) into a new `Contracts.UI` assembly. UX-DR1 doesn't need to change semantically, but its documented home ("Contracts") will — the story's AC should include updating UX-DR1/architecture §4 references.

### Warnings

- **No standalone UX spec** — remains acceptable ONLY because architecture §4 + fc-* contracts are actively guard-enforced and amendment-tracked. Keep treating §4 as the UX source of record; when Story 11.7's route decision lands, record it in a contract (fc-* or §4) rather than only in the story.
- **Do not start Story 11.5's dependent visual work before its guards exist** — the same blind spot (bUnit cannot detect dead CSS or silent splats) will regenerate the defect class otherwise.

---

## Step 5 — Epic Quality Review

**Scope discipline:** Epics 1–8 are done, retro'd, and were quality-validated in the 06-21 audit (Epic 4's backward 4→3 dependency accepted; consolidation withdrawn). They are not re-litigated. Rigor is applied to the **backlog epics 9, 10, 11** whose stories are about to enter `create-story`/dev — confirmed imminent by today's story-automator preflight (`preflight-9-20260704-175514.md`), which selected **9.1, 9.2, 10.1–10.5** for the next automated build cycle.

### Epic 9 — Fresh-Row Producer and Row Identity ✅ sound

- **User value:** genuine operator outcome ("see newly materialized rows marked after command outcomes"). ✓
- **Independence:** builds backward on done Epics 2/3; explicitly forbids reopening them or fabricating identity from the nudge seam (matching architecture.md's FC-NIP paragraph). ✓
- **Story structure:** 9.1 (contract confirmation) → 9.2 (producer + consumer wiring) is a backward within-epic dependency; 9.1 carries the Contract-confirmation DoD escape hatch correctly (blocking follow-up with owner/date instead of fabrication). ACs are Given/When/Then, testable, with negative paths (imprecise-identity case) and intentional-snapshot/public-surface hygiene. ✓
- ⚠️ **One watch item:** 9.1's outcome depends on a **cross-repo contract** (EventStore command-outcome payload). If the EventStore side cannot supply `EntityKey`, the epic's only implementable story (9.2) is blocked by design. The AC handles this honestly, but sequencing 9.1 early and treating its follow-up as epic-gating is essential — the preflight scores 9.2 High complexity (8), consistent with this risk.

### Epic 10 — Tooling Governance Follow-Through ✅ sound, with a framing note

- **User value:** 🟡 borderline-technical framing ("adopter developer can trust the evidence"). Acceptable under the AR12 anchor and the established post-MVP-hardening pattern (Epic 8 precedent), and each story names a concrete beneficiary persona (QA maintainer, tech writer, Test Architect, PO/Architect, developer). Not a violation, but this epic is process/governance value, not end-user capability — correctly scheduled after MVP.
- **Independence & dependencies:** no forward references; stories are mutually independent (10.4 is a clean two-path decision story with ACs for both outcomes). ✓
- **ACs:** BDD-structured, testable, include the "not adopter-facing → may remain" carve-outs. ✓

### Epic 11 — Architecture Review Remediation 🟠 structurally incomplete as planning material

- **User value:** technically-framed by nature (remediation), but justified per story by user/operational impact (silent production degradation 11.1/11.2, security exposure 11.4, broken palette journey 11.7, adopter unblock 11.6). Consistent with the Epic 8/9/10 Correct-Course precedent. Acceptable.
- **Ordering & independence:** explicitly risk-ordered (11.1 → 11.2 → 11.4 → …), independent of Epics 9/10, decisions (11.7/11.8) properly gated, 11.8 deliberately last (largest API surface, pre-v1.0 window). 11.9's `GeneratedLiteral` consolidation depends only on the already-landed Minor batch (PR #48 merged — backward ✓). No forward dependencies. ✓
- 🔴 **Not consumable by the story pipeline.** Epic 11 stories exist only as §4B prose paragraphs in the proposal — no epic section in epics.md, no persona/so-that framing, no Given/When/Then ACs (single-line "AC:" digests at best). Today's preflight proves the operational consequence: the story automator's epic source is epics.md (story count 54 = Epics 1–10 only) — **Epic 11's ten stories are invisible to the tooling that will build them.** Remediation: write the Epic 11 section into epics.md (header pattern of Epics 8–10 + per-story ACs) before any 11.x `create-story` run.
- 🟠 **Story 11.10 is a program, not a story.** It bundles ≥6 concerns: one-type-per-file splits (~99 types across 4+ files), LoggerMessage migration (206 call sites / 50 files), un-deadening CS1591 enforcement, per-advisory NuGet audit suppressions (only verifiable in CI), localization fixes, a constant rename, AND an embedded Architect decision (analyzer elevation vs the no-third-party-analyzer policy). Recommendation: split at `create-story` time (mechanical-split story · LoggerMessage story · policy/decision story), or the story will stall on its slowest concern.
- 🟡 **Story 11.4 bundles three security concerns** (ReturnPathValidator theory, storage-key convergence + FsCheck property, wire-format pins). Cohesive enough to keep, but the story file should structure them as independently verifiable task groups.
- 🟡 **Two stories are decision-blocked (11.7, 11.8)** and correctly routed to PM+Architect — but no owner or due date is recorded, which is exactly the "escalated without a tracked, dated, owned follow-up" anti-pattern the 2026-06-21 DoD amendment prohibits for contract stories. Assign owners/dates when the epics.md section is written.

### Best-Practices Checklist (backlog epics)

| Check | Epic 9 | Epic 10 | Epic 11 |
|---|---|---|---|
| Delivers user value | ✓ | 🟡 governance value | 🟡 remediation value |
| Independent (no forward epic deps) | ✓ | ✓ | ✓ |
| Stories independently completable | ✓ (9.2 after 9.1) | ✓ | 🟠 11.10 oversized; 11.7/11.8 decision-gated |
| No forward story deps | ✓ | ✓ | ✓ |
| Clear, testable ACs | ✓ | ✓ | 🔴 not yet in story-grade form |
| FR/requirement traceability | ✓ (FR13/FR14/AR11) | ✓ (FR20–22/AR12) | 🔴 review-finding IDs only, no epics.md anchor |
| Brownfield integration points named | ✓ (EventStore payload) | ✓ | ✓ (file:line evidence) |

**Database/starter-template checks:** N/A (brownfield framework; no DB layer; no starter template mandated by architecture).

---

## Step 6 — Summary and Recommendations

### Overall Readiness Status

## 🟢 READY — Epics 9 & 10 (the imminent wave) · 🟡 NEEDS WORK — Epic 11 planning material

The plan that is about to enter implementation (story-automator preflight: 9.1, 9.2, 10.1–10.5) is **ready**: 100% FR coverage, sound epic structure, BDD-grade ACs, no forward dependencies, and honest handling of the one real risk (9.1's cross-repo EventStore payload dependency). Epics 1–8 remain done/retro'd with no reopened scope.

**Epic 11 is approved work with real requirements behind it — but it is not yet implementation-grade planning material.** It must not enter `create-story` until its epics.md section exists.

### Critical Issues Requiring Immediate Action

1. 🔴 **Epic 11 is invisible to the plan of record and to the tooling.** Its ten stories exist only as prose in `sprint-change-proposal-2026-07-04.md` §4B; epics.md ends at Epic 10, and today's story-automator preflight (story count 54) confirms the automation cannot see 11.x. **Action:** append an "Epic 11: Architecture Review Remediation" section to epics.md using the Epic 8–10 pattern — source-of-record header, per-story persona/so-that + Given/When/Then ACs (each story citing the review finding IDs it closes, per the proposal's success criteria), and a "Refines: FR13/FR14 (11.7), NFR5/NFR12 (11.8), NFR6 (11.5), FR22 (11.6)" traceability line + FR Coverage Map footer update.
2. 🔴 **Stories 11.7 and 11.8 are decision-blocked with no named owner or date** — the exact anti-pattern the 2026-06-21 Contract-confirmation DoD amendment was written to kill. **Action:** when writing the epics.md section, record owner + due date for the route-contract decision (11.7, Architect+PM) and the kernel-split sign-off (11.8, Architect) — 11.7 first; it owns the only user-visible broken journey (palette command links navigating to unresolvable `/domain/…` routes).

### Major Issues (fix soon, not blocking Epics 9–10)

3. 🟠 **The shipped `Hexalith.FrontComposer.UI` combined host has no requirements anchor** — add FR23 (combined-host composition) or a change-proposal-of-record note in epics.md (UX-DR8 precedent).
4. 🟠 **UX-DR5's visual guarantees are partially unmet in production until Story 11.5 lands** (dead scoped CSS on `FcProjectionConnectionStatus` incl. the reconnect pulse, `FcColumnPrioritizer`, `FcSettingsDialog` mobile, `FcDensityPreviewPanel`). Keep 11.5 guard-first: build the three governance guards before/with the CSS fixes.
5. 🟠 **Story 11.10 is a program, not a story** (~6 concerns, 206 log sites, ~99 type splits, an embedded Architect decision). Split it at `create-story` time.
6. ⚠️ **Epic 9 hinges on a cross-repo contract** (EventStore command-outcome row identity). Sequence 9.1 first; if EventStore cannot supply `EntityKey`, treat 9.1's blocking follow-up as epic-gating rather than letting 9.2 start.

### Minor Issues (hygiene)

7. 🟡 Architecture/document currency drift: architecture.md §8 lists 7 submodules (`.gitmodules` has 8 — `Hexalith.Parties` missing), layer diagram predates the UI host; epics.md NFR14 submodule list likewise short; project-overview.md version table stale (MCP 1.3.0→1.4.0, SDK 10.0.300→10.0.301, etc.).
8. 🟡 NFR5/NFR12 carry no pending-amendment marker despite approved Story 11.8 planning to amend the Contracts boundary (use the UX-DR2/DR3 amendment pattern when 11.8's decision lands).
9. 🟡 Story 2.6's AC wording reads as delivered while Epic 9's header records the accepted deferral — the header note is the accepted mitigation; no action unless epics.md is edited anyway.

### Recommended Next Steps

1. **Proceed with the Epic 9/10 story wave now** — no blocking findings; enforce 9.1-before-9.2 gating.
2. **Write the Epic 11 epics.md section** (fixes #1, #2, and half of #7 in one edit) — a Correct Course/PM task, ~1 focused session, before any 11.x `create-story` run.
3. **Schedule the 11.7 route-contract decision** (Architect+PM) independently of Epic 11 dev start — it is a decision, not code, and unblocks the highest-visibility defect.
4. **Fold the documentation-currency fixes** (#3, #7, #8) into the same PR as the Epic 11 section or the next docs pass.
5. **When 11.x stories are created:** split 11.10; structure 11.4 as three verifiable task groups; require finding-ID references in each Change Log (already the proposal's success criterion).

### Final Note

This assessment identified **12 findings** across five categories (traceability, decision governance, story quality, UX delivery integrity, documentation currency): 2 critical, 4 major, 6 minor/watch. None invalidates shipped scope; both critical items are resolvable with a single planning edit to epics.md plus two decision assignments. The plan's core discipline — guard-enforced contracts, retro-driven correct-course epics, evidence-verified proposals — remains exemplary; the gap is that Epic 11's velocity outran the plan-of-record for one day. Close that gap before the 11.x wave and the plan is fully READY.

**Assessor:** Implementation Readiness workflow (BMAD) · **Date:** 2026-07-04 · **Baseline:** see frontmatter.

