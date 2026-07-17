---
stepsCompleted: ['step-01-validate-prerequisites', 'step-02-design-epics', 'step-03-create-stories', 'step-04-final-validation']
status: 'complete'
inputDocuments:
  - _bmad-output/project-docs/project-overview.md
  - _bmad-output/project-docs/architecture.md
  - _bmad-output/project-docs/api-contracts.md
  - _bmad-output/project-docs/data-models.md
  - _bmad-output/project-docs/component-inventory.md
  - _bmad-output/planning-artifacts/frontcomposer-readiness-request-2026-06-03.md
sourceNote: >-
  Canonical planning sources now exist under _bmad-output/planning-artifacts:
  prd.md, architecture.md, ux-design.md, and epics.md. The PRD remains
  brownfield-derived from _bmad-output/project-docs plus the 2026-06-03
  readiness request, and every requirement must retain a source trace.
  Epics consume those canonical requirements instead of serving as the only
  requirements inventory.
---

# Hexalith.FrontComposer - Epic Breakdown

## Overview

This document provides the complete epic and story breakdown for Hexalith.FrontComposer, decomposing the requirements from the available inputs into implementable stories.

> **Source note.** Canonical planning sources now exist under
> `_bmad-output/planning-artifacts`: `prd.md`, `architecture.md`,
> `ux-design.md`, and this `epics.md`. The PRD is brownfield-derived from
> `_bmad-output/project-docs/*` plus `frontcomposer-readiness-request-2026-06-03.md`.
> Treat the FR/NFR sections as a capability inventory and acceptance baseline,
> and the Additional Requirements (`FC-*`) as the forward roadmap to plan epics
> around. Requirements must retain source traceability back to the PRD and
> brownfield source artifacts.

## Requirements Inventory

### Legacy Functional Requirements (Provenance Only)

> These identifiers predate the canonical PRD and are retained only to explain brownfield
> provenance. They are not planning identifiers. New and corrected traceability uses the canonical
> `FR-1` through `FR-29` requirements in `prd.md` and the canonical coverage map below.

**Source generator (`Hexalith.FrontComposer.SourceTools`)**

- LEGACY-FR-1: From each `[Projection]`-annotated `partial` type, generate 5 files — projection view (`{T}.g.razor.cs` with Loading/Empty/Data states dispatched by `ProjectionRole`), `{T}Feature.g.cs`, `{T}Actions.g.cs`, `{T}Reducers.g.cs`, `{T}Registration.g.cs`.
- LEGACY-FR-2: From each `[Command]`-annotated type (public parameterless ctor + `MessageId`), generate seven non-page files (`CommandForm`, `CommandActions`, `CommandLifecycleFeature`, `CommandRegistration`, `CommandRenderer`, `CommandLastUsedSubscriber`, `CommandLifecycleBridge`), plus `CommandPage` when density = `FullPage`.
- LEGACY-FR-3: Apply the spec-locked command **density rule** — non-derivable property count ≤1 → `Inline`, 2–4 → `CompactInline`, ≥5 → `FullPage` — excluding derivable fields (`MessageId`, `CommandId`, `CorrelationId`, `TenantId`, `UserId`, `Timestamp`, `CreatedAt`, `ModifiedAt`, `[DerivedFrom]`).
- LEGACY-FR-4: Emit compilation-level `FrontComposerMcpManifest.g.cs` and `FrontComposerProjectionTemplateManifest.g.cs`, each carrying schema fingerprints.
- LEGACY-FR-5: Honor the full attribute vocabulary: `[BoundedContext]`, `[ProjectionRole]`, `[ProjectionBadge]`, `[ColumnPriority]`, `[ProjectionFieldGroup]`, `[ProjectionEmptyStateCta]`, `[Destructive]`, `[RequiresPolicy]`, `[DerivedFrom]`, `[Icon]`, `[RelativeTime]`, `[Currency]`, `[ProjectionTemplate]` (plus `[Display]`, `[Description]`, `[DefaultValue]`, `[Flags]`).
- LEGACY-FR-6: Emit the HFC1001–HFC1070 diagnostic catalog (build-time `HFC1xxx`) for invalid annotation/usage, with severities as cataloged.
- LEGACY-FR-7: Provide opt-in **drift detection** (`HfcDriftDetectionEnabled=true`) comparing the current snapshot to a checked-in JSON baseline `AdditionalText` → structural HFC1065 / metadata HFC1066; pipeline must not depend on `CompilationProvider`.
- LEGACY-FR-8: Support 4-level customization (Level-2 `ProjectionTemplate`, Level-3 field-slot, Level-4 full-view overrides) so external assemblies can inject alternate render fragments.

**Blazor Shell (`Hexalith.FrontComposer.Shell`)**

- LEGACY-FR-9: Compose generated UI into a complete app frame via `<FrontComposerShell>@Body</FrontComposerShell>` — `FluentLayout` Header/Navigation/Content/Footer, skip links, `FluentProviders`, global shortcuts (`Ctrl+,` settings, `Ctrl+K` palette).
- LEGACY-FR-10: Provide the DI bootstrap path: `AddHexalithFrontComposerQuickstart()` → `AddHexalithDomain<TMarker>()` → `AddHexalithEventStore(...)`.
- LEGACY-FR-11: Render projections in `FluentDataGrid` with column filtering, expand-in-row detail, status badges, empty/loading states, slow-query/max-items notices, and column prioritization for >15-column projections.
- LEGACY-FR-12: Drive the command lifecycle UI (`Idle→Submitting→Acknowledged→Syncing→Confirmed/Rejected`) with form-abandonment guard and destructive-command confirmation dialog.
- LEGACY-FR-13: Connect to EventStore via SignalR (projection subscriptions) and HTTP (commands/queries), surfacing reconnect/reconciliation status.
- LEGACY-FR-14: Provide registry-driven navigation, home directory (urgency-sorted bounded-context cards), command palette (ARIA combobox), and badge counts.
- LEGACY-FR-15: Manage theme, density, and settings, persisted via `IStorageService` (`LocalStorageService`).

**MCP server (`Hexalith.FrontComposer.Mcp`)**

- LEGACY-FR-16: Expose each generated command as an MCP tool (built dynamically at every `tools/list`) plus a fixed `frontcomposer.lifecycle.subscribe` polling tool.
- LEGACY-FR-17: Expose projections (`frontcomposer://<bounded-context>/projections/<projection-name>`, tenant-scoped Markdown) and skill-corpus docs (`frontcomposer://skills/<id>`) as MCP resources.
- LEGACY-FR-18: Enforce fail-closed security — both `IFrontComposerMcpTenantToolGate` and `IFrontComposerMcpResourceVisibilityGate` required or startup throws; opaque error shape; server-controlled fields (`TenantId`/`UserId`/`MessageId`/`CorrelationId`) blocked from tool input.
- LEGACY-FR-19: Negotiate schema compatibility (`McpSchemaNegotiator`: Exact / CompatibleAdditive / CompatibleWarning / Incompatible) and block side-effects on mismatch.

**CLI (`Hexalith.FrontComposer.Cli`)**

- LEGACY-FR-20: `frontcomposer inspect` reads generated output + `*.diagnostics.json` sidecars and reports forms/grids/registrations/manifest entries/warnings/errors in text or JSON (`frontcomposer.cli.inspect.v1`).
- LEGACY-FR-21: `frontcomposer migrate` plans/applies allowlisted Roslyn code-fixes across catalog version edges (dry-run default, atomic apply, path-safety refusals), JSON `frontcomposer.cli.migrate.v1`.

**Testing library (`Hexalith.FrontComposer.Testing`)**

- LEGACY-FR-22: Provide a pre-wired bUnit host + deterministic fakes (command/query/projection/configurable outcomes), evidence recorders, and assertion helpers for adopters testing generated components.

### Legacy Nonfunctional Requirements (Provenance Only)

> These `LEGACY-NFR-*` identifiers are likewise provenance-only. Canonical nonfunctional
> requirements are the `NFR-*` entries in `prd.md`.

- LEGACY-NFR-1: `TreatWarningsAsErrors=true` everywhere; built-in .NET/Roslyn analyzers only (no Sonar/StyleCop/Roslynator).
- LEGACY-NFR-2: ULIDs (26-char Crockford base32) via `IUlidFactory` — never GUIDs — for `messageId`/`correlationId`.
- LEGACY-NFR-3: Incremental-cache invariant — pure, fully-equatable IR; no `ISymbol` escapes the parse stage; `EquatableArray<T>` for collections.
- LEGACY-NFR-4: Schema fingerprint determinism — `CanonicalSchemaMaterial` pins `JavaScriptEncoder.Create(UnicodeRanges.All)`, STJ source-gen context, `AbsentValueSentinel="<absent>"`, `StringComparer.Ordinal`; changing any invalidates all baselines.
- LEGACY-NFR-5: Contracts kernel split — `SourceTools` and the `Contracts` kernel stay netstandard2.0-clean; net10/Blazor/Fluent rendering contracts move to `Contracts.UI`.
- LEGACY-NFR-6: **Accessibility (WCAG)** — `aria-label`/`role`/`aria-live`/`data-testid` on every interactive element; focus visibility, reduced-motion and forced-colors fallbacks; override-accessibility diagnostics HFC1050–HFC1055.
- LEGACY-NFR-7: Generated-output path is a public contract (`GeneratedOutputPathContract.Template`) validated in Debug **and** Release.
- LEGACY-NFR-8: Ships as signed NuGet packages (`.nupkg`+`.snupkg`); semantic-release from Conventional Commits; no Dockerfiles/containers.
- LEGACY-NFR-9: Fluxor single-writer discipline per slice (ADR-007); scoped-lifetime discipline for storage/effects/auth/tenant accessors (ADR-030).
- LEGACY-NFR-10: Test discipline — solution-level `dotnet test` + trait filters, `DiffEngine_Disabled=true`, Governance + Contract lanes blocking; committed `.verified.txt`, `PublicAPI.Shipped.txt`, pacts updated intentionally.
- LEGACY-NFR-11: Telemetry via `FrontComposerActivitySource` (OpenTelemetry `ActivitySource`).
- LEGACY-NFR-12: Dependency direction points down to the `Contracts` kernel; `SourceTools` references only `Contracts`, while Shell/UI consumers may reference `Contracts.UI`.
- LEGACY-NFR-13: **Confirmed (2026-06-21)** Trim/AOT readiness — `PublishTrimmed`/`PublishAot` enable the HFC1070 advisory; reflection projection catalog needs an `IActionQueueProjectionCatalog` override.
- LEGACY-NFR-14: Root-declared Hexalith submodules live under `references/Hexalith.*`; initialize only those root `.gitmodules` entries, never recurse into nested submodules, and never modify submodule files without explicit approval. Debug/source builds consume Hexalith libraries through local `ProjectReference`s, while Release/package builds consume published NuGet packages.

### Additional Requirements

> **This is the forward roadmap** — drawn from `frontcomposer-readiness-request-2026-06-03.md`.
> Priorities: 🔴 1 = blocks read-only MVP / bootstrap · 🟠 2 = blocks command epics (3–5) ·
> 🟡 3 = confirm-stable (existing surface), not build-new.

- AR1 (🔴 **FC-LYT**): Confirm the full-width vs constrained `<PageLayout>` contract (`Shell/Components/Layout/FrontComposerShell.razor`). Blocks even the read-only MVP.
- AR2 (🔴 **FC-A11Y**): Confirm accessibility primitives (the WCAG attribute/role/live-region patterns) as a reusable, documented contract — part of every story's ready-gate.
- AR3 (🔴 **FC-L10N**): Confirm shell-vs-Tenants localized-string ownership (`FcShellResources.resx`).
- AR4 (🔴 **FC-DOC**): Confirm component documentation contract for the shell components.
- AR5 (🔴 **Shell-integration spike**): Verify `AddHexalithFrontComposer*` / manifest / projection-routing / `FC-TBL` (table) APIs — the bootstrap spike (Story 1.0) that unblocks Story 1.1.
- AR6 (🟠 **FC-CMD**): Confirm the command-lifecycle contract — pending-identity / correlation-key shape (the 26-char checkout shape **not yet approved**), uniqueness scope (per-tenant / user / circuit?), lifecycle ownership, `alreadyApplied` semantics, reconciliation. Blocks all command epics.
- AR7 (🟠 **FC-CNC**): Confirm one-at-a-time command execution is the v1 contract (fallback approved; batching = fast-follow).
- AR8 (🟠 **Numeric budgets**): ✅ **Confirmed (2026-06-21).** confirming→degraded (`TimeoutActionThresholdMs=10_000`), polling (cadence `1_000` / max `120_000`), and retry (Epic 3 `0`; Epic 4 `1×250ms`) budgets ratified in `fc-cmd-command-budget-contract` + `fc-cmd-retry-degraded-state-contract`.
- AR9 (🟡 **EventStore status contract**): Confirm-stable `GET /api/v1/commands/status/{id}` as the command-status query the polling coordinator binds to (exists already — confirm, don't build).
- AR10 (**Out of scope for v1 / fast-follow**): Do **not** build `<AuditTimeline>` or `<ConsequencePreview>` rich components now — approved fallbacks stand; track as fast-follow.
- AR11 (**FC-NIP**): Confirm and implement the row-level new-item producer contract for `FcNewItemIndicator`. The producer must come from command outcome context with precise row identity (`EntityKey` or an approved equivalent), not from the current projection nudge seam that carries only projection type and tenant id.
- AR12 (**FC-TOOL-GOV**): Preserve Epic 7 authoring-tooling follow-through as explicit backlog work: mechanical story evidence reconciliation, adopter-facing historical-label cleanup, CLI text/JSON parity coverage, HFCM9002 production-emission decisioning, and default-lane Testing redaction coverage.

> 📋 **Contract-confirmation Definition-of-Done (2026-06-21 process amendment).** A contract-confirmation
> story (the `Confirm …`/`Establish …` stories: 1.2, 1.3, 1.4, 1.5, 2.8, 3.3, 3.5, 3.6, 4.3) MUST NOT reach
> **Done** on *"escalated with an owner"* alone. "Escalated" is a valid intermediate state, but Done requires
> either (a) the decision **confirmed**, or (b) a **tracked, dated, owned blocking follow-up** in the sprint
> backlog. This amends the AC2-style *"confirmed OR escalated with an owner"* wording that previously let
> decisions close silently — the root cause of the FC-LYT / AR8 / UX-DR confirmation debt closed in
> `sprint-change-proposal-2026-06-21`.

> **Epic 1 residual wording disposition (2026-07-05).** The residual FC-A11Y / FC-L10N / FC-DOC /
> FC-SETTINGS wording action is closed by
> `sprint-change-proposal-2026-07-05-epic-1-residual-wording-decisions.md`. FC-L10N confirms that density
> preview sample strings are out of localization scope and domain labels are host-owned with no shell
> fallback. FC-DOC confirms the inline-summary + published-sibling link convention and records that
> DataGrid/settings docs are authored. FC-SETTINGS confirms the AC3 reading as one persistence writer per
> slice plus one DOM writer per side-effect. FC-A11Y confirms the three-layer automated story ready-gate
> and routes visual/manual release sign-off to Product/UX + Release Owner, due before v1.0 RC readiness
> classification.

### UX Design Requirements

> **Confirmed 2026-06-21** (sprint-change-proposal-2026-06-21). Originally reverse-engineered from the
> implemented component catalog (`component-inventory.md`) + readiness request, these UX contracts are now
> confirmed against the shipped, `FluentConformanceTests`-guarded, bUnit/e2e-tested behaviour and refreshed
> to match `architecture.md` §4.

- UX-DR1: **Design tokens** — `Typography` (9 `FcTypoToken` role constants → FluentUI v5 `TextSize`/`TextWeight`/`TextTag`, pinned `TypographyMappingVersion="3.1.0"`); `DensityLevel`/`DensitySurface` density tokens applied via `<body data-fc-density>`.
- UX-DR2: **Semantic status slots** — `[ProjectionBadge]` enum-member → a status indicator with a mandatory accessible name. **Amended 2026-06-25 (Epic 8 / Story 8.7 — `sprint-change-proposal-2026-06-25-aspire-grade-visual-refresh.md`):** *status* members render as a **colored Fluent icon** (success = green checkmark, error = red cross, unknown/neutral = grey question; warning/info as extensions) with the status label revealed **on hover _and_ keyboard focus** via `FluentTooltip`, plus an always-present `aria-label` so the accessible name is never hover-only (NFR-3 / WCAG 2.2 AA preserved). Numeric **count** slots keep the `FluentBadge` pill (`FcDesaturatedBadge` desaturated variant for non-urgent counts). This **supersedes the prior pill-only status model** (`FcStatusBadge` `FluentBadge` Color/Appearance) and is a contract amendment that touches the `[ProjectionBadge]` generator emit.
- UX-DR3: **Responsive layout** — breakpoint behaviour (`FcLayoutBreakpointWatcher`) with the unified `FrontComposerNavigation` rail rendered at 72px labelled or 48px icon-only width. `FcHamburgerToggle` is **always visible** and at Desktop toggles labelled ↔ icon-only rail (`SidebarToggledAction`) — **supersedes the earlier "D9 / no Desktop hamburger" decision** (architecture §4). The framework sidebar keeps **exactly one active item** (longest segment-prefix, `NavLinkMatch.Prefix`).
- UX-DR4: **Reusable interaction components** — `FcCommandPalette` (ARIA combobox, keyboard nav), `FcSettingsDialog`, `FcDestructiveConfirmationDialog`, `FcFormAbandonmentGuard`, `FcLifecycleWrapper`.
- UX-DR5: **Status & empty/loading UX** — `FcProjectionLoadingSkeleton` (Card/Timeline/Grid), `FcProjectionEmptyPlaceholder`, `FcProjectionConnectionStatus`, `FcPendingCommandSummary` (`aria-live`).
- UX-DR6: **Accessibility patterns** — skip links, focus indicators, `role="region"` row-detail with live-region for filter-hidden expansions (WCAG 4.1.2), keyboard reachability, reduced-motion/forced-colors fallbacks.
- UX-DR7 (FC-LYT): **Page layout contract** — full-width vs constrained `<PageLayout>` (ties to AR1). ✅ Confirmed 2026-06-21 (FullWidth default + `75rem` max-measure).
- UX-DR8: **Account control & server security** (architecture §4) — a framework-owned `FcAccountMenu` (`FluentAvatar` → Sign in/Sign out, wired to `/authentication/{challenge,sign-out}`) rendered **always** so it survives adopter `HeaderEnd` customization; backed by framework-owned server-side security wiring (`AddHexalithFrontComposerServerSecurity`). Domain modules supply only domain-specific security *configuration*.

> 🔗 **UX-DR story-traceability note (added 2026-06-21).** Two UX-DR refinements shipped through
> sprint-change-proposals rather than numbered Epic stories. They are **accepted as
> change-proposal-of-record** — architected in `architecture.md` §4 and enforced by
> `FluentConformanceTests` + bUnit/e2e coverage — so **no synthetic backfill story is created**; this
> note is their requirement-level traceability record:
> - **UX-DR3** — the *always-visible Desktop hamburger* (superseding the "D9 / no Desktop hamburger"
>   decision) and the *single-active-nav-item* rule shipped via
>   `sprint-change-proposal-2026-06-09-shell-account-hamburger` and
>   `sprint-change-proposal-2026-06-19-nav-single-active-item`. The **base** responsive
>   rail/breakpoint/hamburger-collapse behaviour remains traced to **Story 2.2** (AC `*(UX-DR3)*`).
> - **UX-DR8** — `FcAccountMenu` (always-rendered account control) + framework-owned server security
>   (`AddHexalithFrontComposerServerSecurity`) shipped via
>   `sprint-change-proposal-2026-06-09-shell-account-hamburger` and
>   `sprint-change-proposal-2026-06-14-shell-security-helper`. It has **no dedicated numbered story**;
>   this note is its sole story-level traceability link.

### FR Coverage Map

This is the sole planning coverage map. Requirement semantics and identifiers come from canonical
`prd.md`; the legacy inventory above is provenance only.

| Canonical requirement | Planning ownership |
| --- | --- |
| FR-1 | Epic 2: Stories 2.1 and 7.3 diagnostic support |
| FR-2 | Epic 3: Stories 3.1 and 3.2 |
| FR-3 | Epic 2: Stories 2.1, 2.3, 2.5; Epic 4: Stories 4.1, 4.4; Epic 6: Stories 6.1–6.4 |
| FR-4 | Epic 3: Story 3.2 |
| FR-5 | Epic 6: Stories 6.1–6.4 |
| FR-6 | Epic 7: Stories 7.3 and 7.4; Epic 5: Story 5.5 |
| FR-7 | Epic 1: Stories 1.0 and 1.1; Epic 11: scoped-lifetime remediation |
| FR-8 | Epic 1: Stories 1.1 and 1.3; UX-DR8; Epic 8 refinements |
| FR-9 | Epic 1: Stories 1.2, 1.4, 1.6; Epic 8: Story 8.4 |
| FR-10 | Epic 2: Stories 2.2 and 2.7; Epic 8: Story 8.5; Epic 11: Stories 11.0 and 11.7 |
| FR-11 | Epic 2: Stories 2.3–2.5; Epic 8: Stories 8.4 and 8.7 |
| FR-12 | Epic 2: Story 2.6; Epic 11: Story 11.2 |
| FR-13 | Epic 9: Stories 9.1 and 9.2; Story 2.6 preserves the ownership boundary |
| FR-14 | Epic 3: Stories 3.1–3.3; Epic 4: Story 4.5 |
| FR-15 | Epic 3: Stories 3.4–3.6 |
| FR-16 | Epic 4: Stories 4.1–4.5 |
| FR-17 | Epic 5: Stories 5.1 and 5.2 |
| FR-18 | Epic 5: Story 5.3 |
| FR-19 | Epic 5: Stories 5.4 and 5.5; Epic 11: Story 11.3 |
| FR-20 | Epic 7: Stories 7.1 and 7.3; Epic 10: Story 10.3 |
| FR-21 | Epic 7: Story 7.2; Epic 10: Stories 10.3 and 10.4 |
| FR-22 | Epic 7: Story 7.5; Epic 10: Story 10.5; Epic 11: Story 11.6 |
| FR-23 | Stories 1.5, 5.3, 7.2–7.4, 10.2, 10.4, and 11.14 |
| FR-24 | Release Governance Gate RG-1; REL-AI-1 remains open; REL-3 owns correction, REL-4 the technical freeze, REL-5 Release Owner enablement; REL-2 is completed evidence, not closure |
| FR-25 | Epics 7 and 10; Epic 11: Stories 11.8, 11.11–11.14, the 11.19 children, and staged analyzer-policy/burn-down/activation Stories 11.20–11.23 |
| FR-26 | Epic 9: Story 9.2; Story 2.6 preserves the ownership boundary |
| FR-27 | Epic 10: Stories 10.1–10.5 |
| FR-28 | Epic 11: completed decision records 11.0 and 11.8 |
| FR-29 | Epic 11: Stories 11.1–11.23, with 11.17–11.19 represented only through their materialized children |

**Release Governance Gate RG-1 (FR-24):** before any NuGet or GitHub package publication, the Release
Owner must prove that the exact expected package artifacts passed inventory, tests, package-consumer
validation, symbol/SBOM generation, signature and RFC 3161 timestamp verification, checksum coverage,
sealed-manifest verification, and `classify-release --require-publishable`. Passing evidence requires
`classification=ready` and `publish_authorized=true`; the same authorized bytes must be published and
then independently verified from NuGet and GitHub. Durable release evidence is required. Product work
may continue while the gate is open, but automated package publication may not.

**Update (correct-course 2026-07-13):** FR-24 implementation is now owned by **`REL-2`** (Tenants
reusable-workflow alignment), and `REL-1` is closed as superseded. Because the release trigger moves to
`workflow_run`-after-CI on the shared reusable `domain-release.yml` (which has no evidence hook and is a
non-editable submodule), FR-24 evidence is re-homed via 3-layer split-homing: package inventory +
consumer validation in shared CI (`domain-ci.yml` + FrontComposer `scripts/`), publish on the pristine
reusable release, and a supplemental FrontComposer `release-evidence.yml` for signing, SBOM, checksums,
manifest, `classify-release`, and evidence assets. Inventory + consumer validation run against the final
post-2.0-split package set (`Contracts.UI` packable @ 2.0.0). Gating posture: G1 (post-publish +
next-release fail-closed) now, G2 (optional Hexalith.Builds inline gate) as a durable follow-up. See
`_bmad-output/planning-artifacts/sprint-change-proposal-2026-07-13-rel-ai-1-fr24-rehome-into-rel-2.md`.

**Update (correct-course 2026-07-15):** keep `REL-2` done against its accepted G1 criteria, but do not
use it to close FR-24. Live v3.2.2 evidence reported unsigned packages, an invalid manifest,
`classification=blocked`, and `publish_authorized=false` while the evidence workflow concluded
successfully. **`REL-3: Enforce FR-24 before publication and reconcile affected releases`** now owns
the correction. Hexalith.Builds must forward signing credentials into semantic-release, or the Release
Owner must explicitly approve a bounded FrontComposer-owned gated workflow. REL-3 packs once, validates
and signs the exact candidates, seals/verifies/classifies them before publication, publishes those same
bytes with durable evidence, verifies downloaded NuGet/GitHub bytes, handles partial publication, and
records v3.2.1/v3.2.2 in the compliance ledger. This stop-the-line gate blocks the next publish-capable
release; no new product epic is required. See
`_bmad-output/planning-artifacts/sprint-change-proposal-2026-07-15-rel-ai-1-prepublish-enforcement.md`.

**Update (correct-course 2026-07-16, freeze truth-state):** `REL-4` is the approved stop-the-line predecessor to `REL-3`, but remains ready-for-dev. Until its fail-closed publish gate is implemented and verified in `release.yml`, the freeze is administrative and the current workflow remains publish-capable. REL-4 must land before REL-3 development or any other change may authorize package publication. The same gate contract remains a required Hexalith.Builds upstream item; after REL-4 lands, update this section with the live frozen-run evidence. Follow-up: `_bmad-output/planning-artifacts/sprint-change-proposal-2026-07-15-release-freeze-enforcement.md`.

**Update (correct-course 2026-07-15, upstream governed contract):** the Hexalith.Builds dependency
is corrected from signing-secret forwarding to the full **BUILD-REL-1 opt-in governed NuGet release
contract** (protected release environment, signing secrets, RFC 3161 timestamp input,
`id-token`/`attestations` permissions, a version-aware pre-publication candidate phase,
`actions/attest-build-provenance` over the exact candidates with the bundle bound into manifest
finalization, no-repack publication, backward compatibility, root-only submodule init). A live
upstream search found no matching issue or PR; filing is a Release Owner action. `REL-3` is amended
in place (attestation-before-classification AC, failed-run verification AC, approval-mechanism
resolution — the REL-4 variable remains the caller-side authorization; no approval tokens enter
`release.yml`). New **`REL-5: Provision the production signing identity and prove the first
governed release`** separates operational authority (identity/trust model, certificate custody,
timestamp-authority approval, upstream filing, first-release authorization, download verification,
ledger sign-off, REL-AI-1 closure) from REL-3 development work. See
`_bmad-output/planning-artifacts/sprint-change-proposal-2026-07-15-governed-release-upstream-contract.md`.

**Additional-requirement coverage:** AR1–AR5 → Epic 1 · AR6 (FC-CMD) → Epic 3 · AR7 (FC-CNC) → Epic 4 · AR8 (budgets) → Epic 3 + Epic 4 · AR9 (EventStore status) → Epic 3 · AR10 (rich components) → out of scope (fast-follow, tracked, not an epic) · AR11 (FC-NIP) → Epic 9 · AR12 (FC-TOOL-GOV) → Epic 10.
**Cross-cutting canonical NFRs** apply to every epic as ready-gate constraints, anchored by FC-A11Y (AR2) and FC-DOC (AR4) in Epic 1. Telemetry is owned cross-cutting rather than per-AC — emitting through `FrontComposerActivitySource` on Shell command-lifecycle/projection paths and MCP tool/resource paths.
**Epic 11 (Release Readiness Remediation Program)** traces canonical FR-7, FR-10, FR-12, FR-19, FR-22, FR-25, FR-28, and FR-29 plus the 2026-07-04 architecture-quality-review findings. Story 11.0 and Story 11.8 are completed decision records. Stories 11.17, 11.18, and 11.19 are decomposition parents, not implementation candidates; their child stories carry delivery status. Story 11.19d approved staged adoption of `AnalysisMode=Recommended` and materialized sequential, separately approval-gated Stories 11.20–11.23; Story 11.23 is a v1.0 publication gate.

## Epic List

### Epic 1: Shell Foundation & Bootstrap
An **adopter developer** can stand up a FrontComposer admin shell that boots through the
`Quickstart → AddHexalithDomain → AddHexalithEventStore` path, renders the full-width/constrained
page layout, and is accessible, localized, and documented from day one. Delivers the read-only-MVP
enabler: the confirmed `FC-LYT` layout contract, `FC-A11Y` accessibility primitives, `FC-L10N`
string ownership, `FC-DOC` component docs, and the Shell-integration spike (Story 1.0) + bootstrap
(Story 1.1).
**Canonical FRs covered:** FR-7–FR-9, FR-23 · **ARs:** AR1 (FC-LYT), AR2 (FC-A11Y), AR3 (FC-L10N), AR4 (FC-DOC), AR5 (spike)
**Standalone:** a bootable, accessible, empty shell — no later epic required to function.

### Epic 2: Read-Only Projection Experience  *(the read-only MVP)*
An **operator** can browse domain read-models: registry-driven navigation, an urgency-sorted home
directory, the command palette, and projections rendered from `[Projection]` types into a
`FluentDataGrid` with filtering, expand-in-row detail, status badges, and column prioritization —
fed live from EventStore over SignalR/HTTP, with row-level fresh-item indicators delegated to
Epic 9 / FC-NIP. Confirms the `FC-TBL` table API.
**Canonical FRs covered:** FR-1, FR-3, FR-10–FR-13
**UX-DRs:** UX-DR1, UX-DR2, UX-DR3, UX-DR5, UX-DR6, UX-DR7
**Standalone:** complete read-only operations console; builds on Epic 1, needs no command epic.

### Epic 3: Command Authoring & Lifecycle
An **operator** can submit a command from a generated form and watch it through its full lifecycle
(`Submitting → Acknowledged → Syncing → Confirmed/Rejected`), with the form shape driven by the
density rule. Pins the **FC-CMD** contract (pending-identity / correlation-key shape, uniqueness
scope, `alreadyApplied`, reconciliation), binds the polling coordinator to the confirmed EventStore
status endpoint, and applies the agreed numeric budgets.
**Canonical FRs covered:** FR-2, FR-4, FR-14, FR-15
**ARs:** AR6 (FC-CMD), AR8 (budgets — confirming→degraded, polling), AR9 (EventStore status contract)
**Standalone:** single-command submit→confirm works end-to-end; builds on Epics 1–2.

### Epic 4: Safe & Concurrent Command Execution
An **operator** can run destructive and rapid command sequences safely: destructive-confirmation
dialogs, unsaved-form abandonment guard, the **one-at-a-time** (`FC-CNC`) v1 execution policy with
approved fallback, policy-gated authorization (`[RequiresPolicy]`), and degraded/retry behavior from
the numeric budgets.
**Canonical FRs covered:** FR-16
**ARs:** AR7 (FC-CNC), AR8 (retry/degraded budgets)
**Standalone:** safe command UX layered on Epic 3.
> ✅ *Split accepted (2026-06-21).* Epics 3 & 4 both touch the generated command pipeline + `FcAuthorizedCommandRegion`, split on a genuine **risk boundary** (FC-CMD identity contract vs. FC-CNC concurrency policy). This is the **final v1 structure**: both epics shipped and retro'd (2026-06-04 / 2026-06-05), the dependency is backward (4→3, allowed), and retro-consolidating completed work would be churn with no benefit. Consolidation offer withdrawn.

### Epic 5: AI-Agent (MCP) Surface
An **AI agent** can discover every generated command as an MCP tool (rebuilt at each `tools/list`),
read tenant-scoped projections and the skill corpus as resources, poll command lifecycle via
`frontcomposer.lifecycle.subscribe`, and operate within fail-closed tenant/resource security with
schema-fingerprint negotiation blocking side-effects on mismatch.
**Canonical FRs covered:** FR-17–FR-19
**Standalone:** the same domain surface exposed to agents; builds on the generated manifest, independent of the human UI epics.

### Epic 6: Customization & Extensibility
An **adopter developer** can override the generated UI without forking: Level-2 `ProjectionTemplate`,
Level-3 field slots, and Level-4 full-view overrides from external assemblies, with the
override-accessibility diagnostics (HFC1050–HFC1055) and customization-contract version checks
keeping overrides safe.
**Canonical FRs covered:** FR-3 and FR-5
**Standalone:** extension surface on top of the generated baseline; builds on Epics 2–3.

### Epic 7: Authoring Tooling & Drift Safety
An **adopter developer** can inspect generated output and diagnostics (`frontcomposer inspect`),
migrate across version edges (`frontcomposer migrate`), test generated components with the Testing
library's bUnit host + deterministic fakes, and catch structural/metadata drift against a checked-in
baseline (HFC1065/66) before it ships.
**Canonical FRs covered:** FR-6, FR-20–FR-22, FR-25
**Standalone:** the developer-confidence toolchain; usable against any annotated domain, independent of runtime epics.

### Epic 8: Aspire-grade Visual Refresh  *(post-MVP chrome parity)*
An **operator** experiences shell chrome matching the polish of the **.NET Aspire Dashboard** while the
codebase stays strictly on **Fluent UI v5 components + Fluent 2 tokens**: a **neutral header/footer** (brand
accent demoted to a *thread* — active nav, focus, primary, links, badges — never a surface fill), an
**icon+label navigation rail** with outline→filled active swap + a projection flyout, **compact default
density** + sticky-header grids, a reusable **`FcPageToolbar`** (search + filter + view-menu + underline
tabs), and **colored-icon status** (green check / red cross / grey question, hover+focus label). Aspire runs
Fluent v4/FAST tokens that §4.1 bans here, so every pattern is **translated**, not copied.
**Canonical refinements:** FR-8–FR-11 · **UX-DRs:** UX-DR1, UX-DR2 (amended), UX-DR3 (refined) · **introduces no new FRs**
**Standalone:** each story (8.1–8.7) ships independently; Story 8.1 (header/footer) is a Minor change shippable on its own.
**Source of record:** `sprint-change-proposal-2026-06-25-aspire-grade-visual-refresh.md` (Correct Course, 2026-06-25).
**Out of framework scope:** Tenants.UI page-body adoption (neutral page titles, `FcPageToolbar` adoption) is a separate **Host-A** Tenants correct-course under submodule approval.

### Epic 9: Fresh-Row Producer and Row Identity  *(post-MVP follow-up)*
An **operator** can see newly materialized projection rows marked after command outcomes, using a
framework-controlled row identity payload and the confirmed `FcNewItemIndicator` component.
**Canonical FRs covered:** FR-13 and FR-26
**ARs:** AR11 (FC-NIP)
**Standalone:** post-MVP enhancement; builds on Epics 2 and 3, and does not reopen the projection nudge seam.
**Source of record:** `sprint-change-proposal-2026-07-01.md` (Correct Course, 2026-07-01).

### Epic 10: Tooling Governance Follow-Through *(post-MVP quality hardening)*
An **adopter developer** can trust FrontComposer's authoring-tooling evidence because story file
lists are mechanically reconciled, CLI text output is covered like JSON output, migration sidecar
promises stay honest, and Testing package evidence remains redacted by default.
**Canonical FRs covered:** FR-20–FR-23 and FR-27
**ARs:** AR12 (FC-TOOL-GOV)
**Standalone:** post-MVP quality hardening; builds on Epic 7 and does not reopen completed stories.
**Source of record:** `sprint-change-proposal-2026-07-01-epic-7-retro-follow-through.md` (Correct Course, 2026-07-01).

### Epic 11: Release Readiness Remediation Program  *(post-MVP quality hardening)*
An **adopter developer / operator** gets a FrontComposer whose worst blind-spot defects are closed:
circuit-safe EventStore auth and self-healing projection realtime (no silent production-circuit
degradation), fail-closed MCP paths that log and survive across requests, a hardened
open-redirect/storage-key security surface, dead scoped-CSS remediated behind durable
visual-conformance guards, a genuinely fault-injectable Testing harness (the key Tenants-adoption
unblock), a unified command/projection route contract (so palette command activation lands on a page
that exists), a leaner Contracts kernel, and consolidated shell layering + convention alignment.
Remediation-framed, but each story is justified by operator/adopter/security impact and organized into four coherent release workstreams.
**Canonical FRs covered:** FR-7, FR-10, FR-12, FR-19, FR-22, FR-25, FR-28, FR-29 · **Introduces:** architecture-review-finding requirements H1–H12 / M-series · **no net-new user-facing FRs**
**Delivery model:** four workstreams govern implementation and current state. Stories 11.17, 11.18, and 11.19 are nonimplementable decomposition parents; only their named children enter the queue. The approved Story 11.19d analyzer decision materialized sequential, separately approval-gated Stories 11.20–11.23, with Story 11.23 gating v1.0 publication. Epic 11 consumes completed Epic 10 evidence where referenced and does not reopen completed Epics 1–10.
**Source of record:** `sprint-change-proposal-2026-07-04.md` (Correct Course, 2026-07-04), triggered by `_bmad-output/project-docs/architecture-quality-review-2026-07-04.md`. A Minor-scope quick-win fix batch was applied in-tree under the same proposal (PR #48).
**Decisions (contract-confirmation DoD — tracked, owned, dated blocking gates):** **11.0** route-contract decision → **Architect + Product**, assigned 2026-07-05, resolved 2026-07-05 with `/commands/{BoundedContext}/{CommandTypeName}`; **11.8** Contracts kernel split decision and compatibility plan → **Architect + PM**, assigned 2026-07-04, resolved 2026-07-05 with the approved `Contracts` kernel + `Contracts.UI` target. Stories 11.11–11.14 are completed delivery records for that package-boundary change.

> **Out of scope (fast-follow, not an epic):** `<AuditTimeline>` and `<ConsequencePreview>` rich
> components (AR10) — approved fallbacks stand; tracked for a later cycle.

## Epic 1: Shell Foundation & Bootstrap

An adopter developer can stand up a FrontComposer admin shell that boots, lays out, and is accessible, localized, and documented from day one — delivering the read-only-MVP enabler (FC-LYT, FC-A11Y, FC-L10N, FC-DOC, the Shell-integration spike, and the bootstrap). Covers canonical FR-7–FR-9 and FR-23; AR1–AR5; ready-gated by canonical accessibility and scoped-lifetime NFRs.

### Story 1.0: Shell-integration spike — verify the bootstrap & table APIs

As an adopter developer (with FrontComposer support),
I want a time-boxed spike that exercises the `AddHexalithFrontComposer*` registration, the generated manifest, projection routing, and the `FC-TBL` table API against a throwaway host,
So that the bootstrap story starts from confirmed, answered API questions instead of assumptions.

**Acceptance Criteria:**

**Given** a throwaway consuming host project referencing the Shell,
**When** the spike wires `AddHexalithFrontComposerQuickstart()`, `AddHexalithDomain<TMarker>()`, and a stub `AddHexalithEventStore(...)`,
**Then** the app starts, the registry is populated from at least one generated `*Registration` type,
**And** each open API question (manifest discovery, projection-route reachability, `FC-TBL` column/filter surface) is recorded as answered/blocked in a short spike note under `_bmad-output/`.

**Given** the spike note,
**When** review completes,
**Then** every 🔴-priority API question from the readiness request (AR5) is marked resolved or escalated with an owner,
**And** the throwaway host is discarded (no spike code merged into `src/`).

### Story 1.1: Bootstrap a minimal, bootable shell

As an adopter developer,
I want my app's `MainLayout` to reduce to `<FrontComposerShell>@Body</FrontComposerShell>` with the three-call DI bootstrap,
So that I get the complete Header/Navigation/Content/Footer frame with zero hand-written layout.

**Acceptance Criteria:**

**Given** an app calling `AddHexalithFrontComposerQuickstart()` → `AddHexalithDomain<TMarker>()` → `AddHexalithEventStore(...)` in that order,
**When** the app starts,
**Then** Fluxor (with `StoreInitializer`), `IStorageService`, `IFrontComposerRegistry`, command/query stubs, and badge/lifecycle/slot/template/view registries are all registered,
**And** the shell renders `FluentLayout` with skip links, `FluentProviders`, and the global shortcuts `Ctrl+,` (settings) and `Ctrl+K` (palette) active. *(FR-7, FR-8)*

**Given** the registration calls are made out of order or one is missing,
**When** the app starts,
**Then** startup fails fast with a message naming the missing/mis-ordered registration rather than failing later at first render.

**Given** the empty shell (no domain types yet),
**When** it renders,
**Then** the content area shows the home directory in an empty state without throwing.

### Story 1.2: Confirm and apply the FC-LYT page-layout contract

As an adopter developer,
I want a confirmed full-width vs. constrained `<PageLayout>` contract on `FrontComposerShell`,
So that every page renders at the correct measure without per-page layout hacks.

**Acceptance Criteria:**

**Given** the `FC-LYT` contract is documented (full-width vs constrained, default, opt-in mechanism),
**When** a page declares constrained layout,
**Then** content renders within the constrained max-measure; full-width pages span the content area. *(AR1, FR-8)*

**Given** the contract document,
**When** Product/UX reviews it,
**Then** it is marked confirmed — or, per the Contract-confirmation Definition-of-Done (2026-06-21), the open question is recorded as a tracked, dated, owned blocking follow-up ("escalated with an owner" alone is **not** Done) — and linked from the component docs (FC-DOC).

**Given** a bUnit render of `FrontComposerShell` in each layout mode,
**Then** the rendered DOM exposes the expected layout container/data attribute for each mode.

### Story 1.3: Establish FC-A11Y accessibility primitives as a ready-gate

As an adopter developer,
I want the shell's accessibility primitives (skip links, focus visibility, `aria-label`/`role`/`aria-live` patterns, keyboard reachability) confirmed and documented as a reusable contract,
So that every later story can satisfy a single, testable accessibility ready-gate.

**Acceptance Criteria:**

**Given** the shell frame,
**When** rendered,
**Then** skip links target the content region, every interactive element carries an accessible name, and focus indicators are visible (no suppressed focus). *(AR2, NFR-3)*

**Given** the documented FC-A11Y primitive set,
**When** a custom override violates one (missing accessible name, keyboard trap, suppressed focus, missing `aria-live` parity, motion without reduced-motion, color without forced-colors),
**Then** the corresponding HFC1050–HFC1055 diagnostic is the agreed enforcement mechanism referenced by the contract.

**Given** the e2e a11y lane (`npm run test:a11y`),
**When** run against the bootstrapped shell,
**Then** it passes with no critical violations.

### Story 1.4: Establish FC-L10N shell-string ownership

As an adopter developer,
I want clear ownership of localized strings between the shell (`FcShellResources.resx`) and the Tenants layer,
So that shell text is localizable without colliding with host-owned strings.

**Acceptance Criteria:**

**Given** the FC-L10N ownership map,
**When** a string is shell-chrome (nav, settings, status, palette),
**Then** it resolves from `FcShellResources.resx` via `AddHexalithShellLocalization(...)`; host/domain strings stay host-owned. *(AR3)*

**Given** a non-default culture is configured,
**When** the shell renders,
**Then** shell-chrome strings display in that culture and no hard-coded English chrome string remains.

**Given** the ownership map,
**When** the Tenants author reviews it,
**Then** it is confirmed — or, per the Contract-confirmation Definition-of-Done (2026-06-21), the boundary question is recorded as a tracked, dated, owned blocking follow-up ("escalated with an owner" alone is **not** Done).

### Story 1.5: Produce the FC-DOC component documentation contract

As an adopter developer,
I want each shell-facing component documented to a confirmed FC-DOC contract,
So that I can adopt components without reading their source.

**Acceptance Criteria:**

**Given** the FC-DOC documentation contract (required sections per component),
**When** a shell component is published,
**Then** its doc page satisfies the contract and is validated by `eng/validate-docs.ps1` (Gate 2d) under `docs/`. *(AR4)*

**Given** the read-only-MVP component set (layout, navigation, DataGrid surface, settings),
**When** Epic 1 closes,
**Then** each has a conforming doc page, or the gap is a dated, owned, blocking backlog item that
names the missing page, owner, due date, and release gate it blocks; an undated owner note is not Done.

### Story 1.6: Theme, density, and settings persistence

As an operator,
I want to set theme and density in a settings dialog and have it persist,
So that the shell remembers my display preferences across sessions.

**Acceptance Criteria:**

**Given** the shell is running,
**When** I press `Ctrl+,` or activate the settings button,
**Then** `FcSettingsDialog` opens with a density radio group, theme toggle, and a density preview panel. *(FR-9, UX-DR4)*

**Given** I change theme or density and confirm,
**When** I reload the app,
**Then** the chosen theme and `data-fc-density` are restored from `IStorageService` (`LocalStorageService`),
**And** density changes are announced via the `aria-live` density announcer. *(NFR-3)*

**Given** the Theme and Density Fluxor slices,
**When** a preference changes,
**Then** exactly one effect owns persistence + JS interop (single-writer discipline, ADR-007).

## Epic 2: Read-Only Projection Experience *(the read-only MVP)*

An operator can browse domain read-models through registry-driven navigation, an urgency-sorted home directory, the command palette, and projections rendered into a filterable, accessible `FluentDataGrid` fed live from EventStore, with row-level fresh-item indicators delegated to Epic 9 / FC-NIP. Covers canonical FR-1, FR-3, and FR-10–FR-13; UX-DR1, 2, 3, 5, 6, 7; confirms FC-TBL.

### Story 2.1: Render a projection from a `[Projection]` type

As an adopter developer,
I want a `[Projection]`-annotated `partial` type to generate a complete projection view,
So that operators get a working read-model page with no hand-written UI.

**Acceptance Criteria:**

**Given** a `partial` class annotated `[Projection]` with a `[ProjectionRole]`,
**When** the project builds,
**Then** the 5 generated files appear under the public generated-output path, and the view dispatches Loading / Empty / Data states per the role. *(FR-1, FR-25)*

**Given** the projection declares `[ProjectionRole.WhenState]`, `[ProjectionEmptyStateCta]`, and badge/format attributes,
**When** rendered,
**Then** the role strategy, empty-state CTA, and Level-1 display formats (`[RelativeTime]`, `[Currency]`) apply. *(FR-3, UX-DR1)*

**Given** a `[Projection]` type that is not `partial`,
**When** built,
**Then** HFC1003 is reported and the build fails under TWAE. *(FR-1, FR-25, NFR-1)*

### Story 2.2: Registry-driven navigation and home directory

As an operator,
I want a navigation tree and a home landing page generated from the registered domain manifests,
So that I can find every bounded context and projection without a hand-built menu.

**Acceptance Criteria:**

**Given** registered `DomainManifest`s,
**When** the shell renders,
**Then** `FrontComposerNavigation` shows a `FluentNav` tree grouped by bounded context with per-projection count and "New" badges. *(FR-10, UX-DR2)*

**Given** the home route (`/`, `/home`),
**When** loaded,
**Then** `FcHomeDirectory` shows urgency-sorted bounded-context cards across its four progressive states. *(FR-10)*

**Given** a compact viewport,
**When** the shell renders,
**Then** navigation collapses to the 48px `FcCollapsedNavRail` / hamburger per the breakpoint watcher. *(UX-DR3)*

### Story 2.3: DataGrid filtering, status, and empty/loading states

As an operator,
I want to filter projection rows and see clear loading/empty/status feedback,
So that I can narrow large read-models and always know the grid's state.

**Acceptance Criteria:**

**Given** a projection grid,
**When** I type in a column filter,
**Then** a debounced `ColumnFilterChangedAction` filters rows, with a filter summary and reset button shown. *(FR-11)*

**Given** the query is loading or returns no rows,
**When** rendered,
**Then** `FcProjectionLoadingSkeleton` (Card/Timeline/Grid) or `FcProjectionEmptyPlaceholder` shows respectively. *(FR-11, UX-DR5)*

**Given** status-enum columns mapped via `[ProjectionBadge]`,
**When** rendered,
**Then** status members render as colored Fluent icons with hover and keyboard-focus tooltip labels plus
an always-present `aria-label`; numeric count slots remain `FluentBadge` / `FcDesaturatedBadge` pills.
*(UX-DR2, NFR-3)*

**Given** a query exceeding the slow-query threshold or the max-items cap,
**When** rendered,
**Then** a non-blocking slow-query / max-items-truncation notice is surfaced above the grid. *(FR-11)*

### Story 2.4: Accessible expand-in-row detail

As an operator using assistive technology,
I want row-detail panels that are always announced correctly,
So that expanded content and filter-hidden expansions are perceivable.

**Acceptance Criteria:**

**Given** a row with detail,
**When** I expand it,
**Then** the detail renders in an always-present `role="region"` panel. *(FR-11, NFR-3, UX-DR6)*

**Given** an expanded row that a filter then hides,
**When** the filter applies,
**Then** a live region announces the hidden expansion (WCAG 4.1.2) via `FcExpandedRowHiddenBanner`.

**Given** the e2e a11y lane,
**When** run against the grid,
**Then** no critical violations are reported.

### Story 2.5: Column prioritization for wide projections

As an operator,
I want wide projections to prioritize the most important columns,
So that >15-column grids stay usable without horizontal overload.

**Acceptance Criteria:**

**Given** a projection with more than 15 columns,
**When** rendered,
**Then** `FcColumnPrioritizer` activates and HFC1029 is reported as info at build. *(FR-11, FR-25)*

**Given** `[ColumnPriority(n)]` annotations,
**When** rendered,
**Then** columns order by priority; a priority collision reports HFC1028 (info).

### Story 2.6: Live projection updates with reconnect & reconciliation

As an operator,
I want projection grids to update live and recover gracefully from connection loss,
So that I see current data and know when the stream is degraded.

**Acceptance Criteria:**

**Given** an active projection subscription over SignalR,
**When** the backend emits a projection change,
**Then** the grid refreshes or reconciles the affected projection lane and surfaces read-path freshness
without marking individual rows as new. *(PRD FR-12, UX-DR5)*

**Given** automatic row-level fresh-item marking is required,
**When** a command outcome carries the confirmed FC-NIP row metadata,
**Then** Story 2.6 does not infer row identity from projection nudges. *(PRD FR-13, FR-26)*

**Given** the SignalR connection drops,
**When** it reconnects,
**Then** `FcProjectionConnectionStatus` surfaces reconnect/reconciliation state and the grid reconciles missed changes. *(UX-DR5)*

**Historical delivery dependency:** Epic 9 / Story 9.2, now done, supplied the row-level producer and
consumer evidence. This is delivery provenance, not acceptance work owned by Story 2.6.

### Story 2.7: Command palette discovery and global search

As an operator,
I want a keyboard-driven command palette,
So that I can jump to any projection or action quickly.

**Acceptance Criteria:**

**Given** the shell is focused,
**When** I press `Ctrl+K`,
**Then** `FcCommandPalette` opens as an ARIA combobox with a search input and keyboard-navigable results. *(FR-10, UX-DR4)*

**Given** a search query,
**When** I type,
**Then** results filter live from the registry and `FcProjectionGlobalSearch` surfaces matching projections.

### Story 2.8: Confirm the FC-TBL table API contract

As an adopter developer,
I want the table/column/filter API surface (`FC-TBL`) confirmed stable,
So that I can build on the DataGrid without breaking-change risk.

**Acceptance Criteria:**

**Given** the FC-TBL API surface exercised by the Story 1.0 spike,
**When** documented and reviewed,
**Then** the column/filter/expand API is marked confirmed-stable — or, per the Contract-confirmation Definition-of-Done (2026-06-21), any open items are recorded as tracked, dated, owned blocking follow-ups ("escalated with owners" alone is **not** Done) — and reflected in `PublicAPI.Shipped.txt` if public. *(NFR-11)*

## Epic 3: Command Authoring & Lifecycle

An operator can submit a command from a generated form and watch it through its full lifecycle, with form shape driven by the density rule and confirmation bound to the EventStore status endpoint. Covers canonical FR-2, FR-4, FR-14, and FR-15; AR6 (FC-CMD), AR8 (budgets), AR9 (status contract).

### Story 3.1: Generate a command form from a `[Command]` type

As an adopter developer,
I want a `[Command]`-annotated type to generate a complete command form and registration,
So that operators get a working submit form with no hand-written UI.

**Acceptance Criteria:**

**Given** a `[Command]` type with a public parameterless ctor and a `MessageId`,
**When** built,
**Then** exactly seven non-page files appear: `CommandForm`, `CommandActions`,
`CommandLifecycleFeature`, `CommandRegistration`, `CommandRenderer`, `CommandLastUsedSubscriber`, and
`CommandLifecycleBridge`; `CommandPage` is additionally emitted only when density is `FullPage`.
*(PRD FR-14)*

**Given** a `[Command]` missing the parameterless ctor or `MessageId`,
**When** built,
**Then** HFC1009 / HFC1006 are reported respectively. *(FR-2, FR-25)*

**Given** an unsupported field type,
**When** rendered,
**Then** `FcFieldPlaceholder` renders it and HFC1002 is reported. *(FR-14)*

### Story 3.2: Apply the density rule to command forms

As an operator,
I want command forms sized to their field count,
So that simple commands are inline and complex ones get a full page.

**Acceptance Criteria:**

**Given** a command's non-derivable property count,
**When** generated,
**Then** ≤1 → `Inline`, 2–4 → `CompactInline`, ≥5 → `FullPage` (with `CommandPage`). *(FR-4)*

**Given** derivable fields (`MessageId`, `CorrelationId`, `TenantId`, `UserId`, timestamps, `[DerivedFrom]`),
**When** the form renders,
**Then** they are excluded from the form and injected server/infrastructure-side. *(FR-4, FR-14)*

**Given** a command exceeding the property thresholds,
**When** built,
**Then** HFC1007 (warn >30 / error >100) and HFC1011 (error >200) apply. *(FR-2, FR-25)*

### Story 3.3: Confirm the FC-CMD pending-identity and correlation contract

As a FrontComposer maintainer,
I want the command-lifecycle identity contract pinned,
So that all command epics share one agreed pending-identity / correlation model.

**Acceptance Criteria:**

**Given** the FC-CMD contract draft,
**When** reviewed,
**Then** the correlation-key shape (the 26-char checkout shape, ASCII ULID), uniqueness scope (per-tenant / user / circuit), lifecycle ownership, `alreadyApplied` semantics, and reconciliation responsibility are each decided — or, per the Contract-confirmation Definition-of-Done (2026-06-21), recorded as a tracked, dated, owned blocking follow-up ("escalated with an owner" alone is **not** Done). *(AR6)*

**Given** the confirmed contract,
**When** a command is dispatched,
**Then** its `messageId`/`correlationId` are ULIDs generated via `IUlidFactory` (never GUIDs). *(FR-14)*

### Story 3.4: Command lifecycle UI

As an operator,
I want to see a command progress through its lifecycle,
So that I know whether it was acknowledged, confirmed, or rejected.

**Acceptance Criteria:**

**Given** a submitted command,
**When** it progresses,
**Then** `FcLifecycleWrapper` surfaces `Submitting → Acknowledged → Syncing` and the terminal or
degraded outcomes `Confirmed`, `IdempotentConfirmed`, `Rejected`, `NeedsReview`, `Warning`, and
`Degraded` via badge/message-bar without treating HTTP acceptance as projection confirmation.
*(PRD FR-15, UX-DR4)*

**Given** a rejection,
**When** received,
**Then** the typed rejection (errorCode/reasonCategory/suggestedAction/docsCode) is shown and the form remains correctable.

**Given** the lifecycle Fluxor slice,
**When** state transitions,
**Then** a single dispatch source owns each transition (single-writer, architecture invariant).

### Story 3.5: Bind the polling coordinator to the EventStore status endpoint

As an operator,
I want command confirmation driven by the real EventStore status query,
So that confirmed/rejected outcomes reflect backend truth.

**Acceptance Criteria:**

**Given** an acknowledged command,
**When** the coordinator polls,
**Then** it binds to `GET /api/v1/commands/status/{id}` (confirmed-stable, not newly built) and transitions to Confirmed/Rejected on the result. *(FR-15, AR9)*

**Given** the endpoint contract,
**When** EventStore maintainers review,
**Then** it is marked confirm-stable — or, per the Contract-confirmation Definition-of-Done (2026-06-21), the gap is recorded as a tracked, dated, owned blocking follow-up ("escalated with an owner" alone is **not** Done).

### Story 3.6: Apply confirming→degraded and polling budgets

As an operator,
I want sensible timing budgets for confirmation,
So that slow commands degrade gracefully instead of hanging.

**Acceptance Criteria:**

**Given** agreed numeric budgets,
**When** a command stays unconfirmed past the confirming→degraded threshold,
**Then** the UI shows a degraded state while continuing to poll within the polling budget. *(AR8)*

**Given** the AR8 budgets confirmed on 2026-06-21,
**When** Product/UX and EventStore evidence is reviewed,
**Then** the implementation verifies the recorded values: confirming-to-degraded threshold `10_000` ms,
polling cadence `1_000` ms, polling max `120_000` ms, Epic 3 retry budget `0`, and Epic 4 retry
budget `1 x 250` ms, all deterministic and testable via `FakeTimeProvider`. *(NFR-8, NFR-11)*

## Epic 4: Safe & Concurrent Command Execution

An operator can run destructive and rapid command sequences safely — confirmation, abandonment guard, one-at-a-time execution, policy-gated authorization, and degraded/retry handling. Covers canonical FR-16; AR7 (FC-CNC), AR8 (retry/degraded).

### Story 4.1: Destructive-command confirmation

As an operator,
I want destructive commands to require explicit confirmation,
So that I can't trigger irreversible actions by accident.

**Acceptance Criteria:**

**Given** a `[Destructive]` command,
**When** I submit it,
**Then** `FcDestructiveConfirmationDialog` shows the configured title/body and requires confirm before dispatch. *(FR-16, UX-DR4)*

**Given** a destructive-verb-named command without `[Destructive]`,
**When** built,
**Then** HFC1020 (info) advises adding it; a `[Destructive]` command with zero non-derivable properties reports HFC1021 (error). *(FR-16, FR-25)*

### Story 4.2: Unsaved-form abandonment guard

As an operator,
I want to be warned before navigating away from an unsaved command form,
So that I don't lose in-progress input.

**Acceptance Criteria:**

**Given** a dirty command form,
**When** I attempt to navigate away,
**Then** `FcFormAbandonmentGuard` (`NavigationLock`) intercepts and shows a `FluentMessageBar` to confirm or cancel. *(FR-16, UX-DR4)*

**Given** a clean form,
**When** I navigate,
**Then** no guard interrupts.

### Story 4.3: One-at-a-time execution policy (FC-CNC)

As an operator,
I want commands to execute one at a time in v1,
So that rapid sequences stay predictable while batching is deferred.

**Acceptance Criteria:**

**Given** an in-flight command,
**When** I submit another local command,
**Then** FC-CNC v1 blocks the later local submit with support-safe feedback rather than queueing,
batching, or racing. *(AR7, PRD FR-16)*

**Given** the FC-CNC contract,
**When** reviewed,
**Then** one-at-a-time is confirmed as the v1 contract and batching is recorded as fast-follow.

### Story 4.4: Policy-gated command authorization

As an operator,
I want commands gated by authorization policy,
So that I only see and run actions I'm permitted to.

**Acceptance Criteria:**

**Given** a `[RequiresPolicy]` command,
**When** the region renders,
**Then** `FcAuthorizedCommandRegion` shows Pending/Authorized/NotAuthorized per `CommandAuthorizationDecisionKind`. *(FR-16)*

**Given** an invalid or duplicate `[RequiresPolicy]`,
**When** built,
**Then** HFC1056 / HFC1057 (errors) are reported. *(FR-16, FR-25)*

**Given** the command service,
**When** dispatching,
**Then** authorization is evaluated before `BeforeSubmit`, `BeforeSubmit` runs only when authorized,
and protected commands are authorized again after `BeforeSubmit` immediately before dispatch; the
`AuthorizingCommandServiceDecorator` remains the service-boundary fail-closed enforcement.
*(PRD FR-16)*

### Story 4.5: Retry and degraded-state handling

As an operator,
I want failed or slow commands to retry within budget and surface a clear degraded state,
So that transient faults recover without manual resubmission.

**Acceptance Criteria:**

**Given** a transient dispatch fault,
**When** it occurs,
**Then** the command performs exactly one retry after exactly `250` ms using the same pre-accept
`MessageId`; a second failure surfaces a retryable degraded state. *(AR8, PRD FR-14)*

**Given** pending/rejected commands,
**When** present,
**Then** `FcPendingCommandSummary` lists them in an `aria-live` region. *(NFR-3)*

### Command FR subclause traceability (PRD FR-14 / FR-15 / FR-16)

Added by correct course 2026-07-05 to make the partial-trace subclauses explicit. Epics 3 and 4 are
done and these behaviors are implemented; this addendum pins each subclause to its owning story and
named symbol. Before v1.0 RC classification, the owning story's evidence/change-log must cite the exact
passing test method(s), or add a short AC-refinement note. This addendum does not reopen any done story.

| PRD subclause | Owning story | Implementation symbol | AC status | Evidence action before RC |
| --- | --- | --- | --- | --- |
| FR-14 unsupported field types render placeholders, do not break the form | 3.1 | `FcFieldPlaceholder` + HFC1002 | In AC | Cite generator/`CommandRenderer*` test. |
| FR-14 supported field-type parsing | 3.1 / 3.2 | generated `CommandForm` parsers | Implicit | Cite `Generated/Level1FormatRuntimeTests.cs`, `CommandRendererTestFixtures.cs`. |
| FR-14 nullable numeric fields compile + round-trip culture-aware | 3.1 | nullable-numeric codegen (PR #48 minor batch) | Implicit | Cite `Generated/Level1FormatRuntimeTests.cs`. |
| FR-14 form state preserved on retryable pre-accept failures | 4.5 | retry/degraded path | Implicit | Cite retry test + `FcFormAbandonmentGuardTests`. |
| FR-14 `MessageId` is a ULID reused across pre-accept retry attempts | 3.3 + 4.5 | FC-CMD identity + `IUlidFactory` | Implicit | Cite `LifecycleStateServiceTests` / pending-command tests. |
| FR-15 Submitting / Acknowledged / Syncing / Confirmed / Rejected | 3.4 | `FcLifecycleWrapper` | In AC | Covered. |
| FR-15 IdempotentConfirmed, NeedsReview, Warning | 3.4 (+ runtime) | `ILifecycleStateService` + `LifecycleStateService` | In AC | Cite `FcLifecycleWrapperRejectionTests` / `FcLifecycleWrapperThresholdTests` in delivery evidence. |
| FR-15 Degraded / accepted-HTTP is not projection-confirmed | 3.5 / 3.6 | `GET /api/v1/commands/status/{id}` confirmed-stable | In AC | Covered (3.5 + 3.6 budgets). |
| FR-16 `[RequiresPolicy]` evaluated before `BeforeSubmit` and again after for protected commands | 4.4 | `AuthorizingCommandServiceDecorator` + `CommandDispatchAuthorizationGate` | In AC | Cite `RequiresPolicyAttributeTests` + authorization tests in delivery evidence. |
| FR-16 service boundary enforces authorization | 4.4 | `AuthorizingCommandServiceDecorator` | In AC | Covered. |
| FR-16 FC-CNC v1 blocks later local submits (no queue/batch) | 4.3 | FC-CNC one-at-a-time | In AC | Covered. |
| FR-16 destructive confirmation / abandonment guard | 4.1 / 4.2 | `FcDestructiveConfirmationDialog` / `FcFormAbandonmentGuard` | In AC | Covered. |

The AC refinements are applied in Stories 3.4 and 4.4: the lifecycle terminals and the
`[RequiresPolicy]` before/after `BeforeSubmit` sequence are now explicit. Both reference existing code
and tests; neither changes implemented behavior.

## Epic 5: AI-Agent (MCP) Surface

An AI agent can discover generated commands as MCP tools, read projections and skill docs as resources, poll lifecycle, and operate within fail-closed security with schema negotiation. Covers canonical FR-17–FR-19.

### Story 5.1: Expose generated commands as MCP tools

As an AI agent,
I want each generated command available as an MCP tool,
So that I can invoke domain commands through the protocol.

**Acceptance Criteria:**

**Given** a generated `McpManifest`,
**When** I call `tools/list`,
**Then** one tool per `McpCommandDescriptor` is built dynamically with its per-descriptor JSON schema. *(FR-17)*

**Given** a `tools/call`,
**When** the args pass admission → schema negotiation → validation,
**Then** the command instantiates, derivable values inject server-side, and dispatch returns an `McpCommandAcknowledgement`. *(FR-17)*

**Given** server-controlled fields (`TenantId`/`UserId`/`MessageId`/`CorrelationId`) in tool input,
**When** received,
**Then** they are blocked/ignored. *(FR-19)*

### Story 5.2: Lifecycle subscription tool

As an AI agent,
I want to poll a command's lifecycle,
So that I can await confirmation after invoking it.

**Acceptance Criteria:**

**Given** the fixed `frontcomposer.lifecycle.subscribe` tool,
**When** I pass a `correlationId`/`messageId` (ULID, ≤64 ASCII),
**Then** I receive an `McpLifecycleSnapshot` (state, terminal, outcome, bounded transitions, nested `retry.retryAfterMs` and `retry.maxLongPollMs`). *(FR-17)*

**Given** a command is invoked in one MCP request/service scope,
**When** `frontcomposer.lifecycle.subscribe` is called from a later, separate request/service scope,
**Then** it resolves the same lifecycle snapshot from cross-request storage without relying on scoped
in-memory state, and the hosting test creates and disposes both scopes independently. *(PRD FR-17)*

**Given** a malformed identifier,
**When** passed,
**Then** the call is rejected without leaking internal state.

### Story 5.3: Projection and skill-corpus resources

As an AI agent,
I want projections and skill docs as MCP resources,
So that I can read tenant data and reference material.

**Acceptance Criteria:**

**Given** a projection resource URI `frontcomposer://<bounded-context>/projections/<projection-name>`,
**When** read,
**Then** tenant-scoped results render as Markdown via `McpMarkdownProjectionRenderer`. *(FR-18)*

**Given** a skill resource `frontcomposer://skills/<id>`,
**When** read,
**Then** only the `agent-reference` section of the conforming doc is served, within the 32 KB cap (oversized → `SkillResourceTooLarge`). *(FR-18)*

### Story 5.4: Fail-closed security gates

As a platform owner,
I want the MCP server to fail closed,
So that missing gates or auth failures never leak the domain surface.

**Acceptance Criteria:**

**Given** startup without both `IFrontComposerMcpTenantToolGate` and `IFrontComposerMcpResourceVisibilityGate`,
**When** the server starts,
**Then** startup throws. *(FR-19)*

**Given** an auth/tenant/unknown failure,
**When** it occurs,
**Then** a single opaque shape is returned (callers can't fingerprint the cause); `tools/list` returns an empty list, not an error. *(FR-19)*

### Story 5.5: Schema fingerprint negotiation

As an AI agent,
I want schema compatibility checked before side-effects,
So that incompatible clients can't dispatch commands.

**Acceptance Criteria:**

**Given** a client `x-frontcomposer-schema-fingerprint` header,
**When** a `tools/call` arrives,
**Then** `McpSchemaNegotiator` classifies the pair (Exact / CompatibleAdditive / CompatibleWarning / Incompatible). *(FR-19)*

**Given** an Incompatible classification,
**When** the call would cause a side-effect,
**Then** it is blocked. *(FR-19, NFR-7)*

## Epic 6: Customization & Extensibility

An adopter developer can override the generated UI at three levels from external assemblies, with accessibility-safety diagnostics keeping overrides correct. Covers canonical FR-3 and FR-5 plus the canonical accessibility NFR.

### Story 6.1: Level-2 ProjectionTemplate overrides

As an adopter developer,
I want to register a custom view template for a projection,
So that I can replace the generated layout without forking.

**Acceptance Criteria:**

**Given** a Blazor component annotated `[ProjectionTemplate]` with a typed `Context` parameter,
**When** registered via `AddHexalithProjectionTemplates<TMarker>`,
**Then** it renders in place of the generated view for its projection+role. *(FR-3, FR-5)*

**Given** an invalid template (bad projection type, missing context, duplicate, version mismatch),
**When** built,
**Then** HFC1033 / HFC1034 / HFC1037 / HFC1035–HFC1036 are reported respectively. *(FR-5, FR-25)*

### Story 6.2: Level-3 field-slot overrides

As an adopter developer,
I want to override individual field rendering,
So that I can customize one field without replacing the whole view.

**Acceptance Criteria:**

**Given** a registered field-slot override with a valid selector and component,
**When** the projection renders,
**Then** the slot's custom fragment replaces the default field render via the slot registry. *(FR-5)*

**Given** an invalid/duplicate slot selector or component,
**When** the corresponding registration or render phase executes,
**Then** HFC1038 is reported at adopter call-site/startup for an invalid selector; HFC1039 is reported
at startup or render for an incompatible component/field type; HFC1040 is reported at startup for a
duplicate projection/role/field tuple; and HFC1041 is reported at startup for an incompatible slot
contract version. Call-site tests, registry-startup tests, and `FcFieldSlotHost` render tests pin those
phases; these are not claimed as SourceTools build diagnostics. *(PRD FR-5)*

### Story 6.3: Level-4 full-view overrides

As an adopter developer,
I want to override an entire projection view from an external assembly,
So that I have a final escape hatch for bespoke pages.

**Acceptance Criteria:**

**Given** a registered Level-4 view override,
**When** the projection route resolves,
**Then** the override registry supplies the full custom view in place of the generated one. *(FR-5)*

**Given** both a Level-2 template and a Level-4 override exist,
**When** resolved,
**Then** precedence is deterministic: Level-4 full-view override, then Level-2 projection template,
then the generated default. Level-3 field slots are consulted only inside a renderer that delegates
field rendering; they do not outrank or compose into a Level-4 replacement. *(PRD FR-5)*

### Story 6.4: Override-accessibility safety diagnostics

As an adopter developer,
I want overrides checked for accessibility regressions,
So that customization can't silently break a11y.

**Acceptance Criteria:**

**Given** a custom override,
**When** built,
**Then** HFC1050–HFC1055 flag missing accessible name, keyboard reachability, suppressed focus, missing `aria-live` parity, motion-without-reduced-motion, and color-without-forced-colors. *(FR-5, NFR-3)*

**Given** DEBUG + `IsDevelopment()`,
**When** a customization-contract mismatch exists,
**Then** `FcCustomizationDiagnosticPanel` displays it.

## Epic 7: Authoring Tooling & Drift Safety

An adopter developer can inspect generated output, migrate across version edges, test generated components, and catch drift before it ships. Covers canonical FR-6, FR-20–FR-22, and FR-25.

### Story 7.1: `frontcomposer inspect`

As an adopter developer,
I want to inspect generated output and diagnostics from the CLI,
So that I can verify what the generator produced without opening `obj/`.

**Acceptance Criteria:**

**Given** generated files + `*.diagnostics.json` sidecars,
**When** I run `frontcomposer inspect [--build] [--format json]`,
**Then** it reports generatedFiles/forms/grids/registrations/mcpManifestEntries/warnings/errors (schema `frontcomposer.cli.inspect.v1`). *(FR-20)*

**Given** `--fail-on-warning` / `--fail-on-error`,
**When** matching diagnostics exist,
**Then** the exit code reflects ActionableFindings (1); unavailable output → 3. *(FR-20)*

### Story 7.2: `frontcomposer migrate`

As an adopter developer,
I want to apply allowlisted code-fix migrations across version edges,
So that I can upgrade safely with a dry-run preview.

**Acceptance Criteria:**

**Given** `--from`/`--to` matching a `MigrationCatalog` edge,
**When** I run `migrate` (dry-run default),
**Then** it plans entries (safe-fix/unchanged/skipped/failed/manual-only/conflict) without writing; `--apply` writes atomically. *(FR-21)*

**Given** a target inside `bin`/`obj`/`.git`/`/generated/`/submodule roots,
**When** `--apply` runs,
**Then** the write is refused (path-safety) and out-of-root paths are `[redacted-path]`. *(FR-21)*

### Story 7.3: Surface the HFC diagnostic catalog

As an adopter developer,
I want generator diagnostics surfaced consistently at build and via inspect,
So that I can act on annotation/usage problems.

**Acceptance Criteria:**

**Given** any HFC1001–HFC1070 condition,
**When** built under TWAE,
**Then** the diagnostic appears with its cataloged severity (errors break the build). *(FR-20, FR-25, NFR-1)*

**Given** `inspect --severity`,
**When** filtered,
**Then** only diagnostics at/above the level are reported.

### Story 7.4: Opt-in drift detection vs. a baseline

As an adopter developer,
I want to detect structural/metadata drift against a checked-in baseline,
So that unintended generated-surface changes are caught before release.

**Acceptance Criteria:**

**Given** `HfcDriftDetectionEnabled=true` and a valid baseline `AdditionalText`,
**When** the generated surface changes structurally or in metadata,
**Then** HFC1065 / HFC1066 are reported at the configured severity. *(FR-6)*

**Given** a missing/malformed/oversized/unsupported baseline,
**When** built,
**Then** HFC1058–HFC1064 are reported per the catalog; the drift pipeline does not depend on `CompilationProvider`. *(FR-6, FR-25)*

### Story 7.5: Testing library — bUnit host and deterministic fakes

As an adopter developer,
I want a pre-wired test host with deterministic fakes,
So that I can unit-test generated components reliably.

**Acceptance Criteria:**

**Given** `FrontComposerTestBase` / `AddFrontComposerTestHost()`,
**When** I write a bUnit test,
**Then** the host auto-registers fakes (`TestCommandService`/`TestQueryService`/`TestProjectionPageLoader`) with `JSInterop.Mode = Loose`. *(FR-22)*

**Given** the configurable command/query/projection fakes,
**When** I drive a scenario,
**Then** rejection, timeout, stall, paging, filtering, sorting, authorization, and async-initialization
outcomes are deterministic and assertable. `TestFaultEvidenceRecorder` records redacted
Drop/Delay/PartialDelivery/Reorder/ReconnectNudge evidence only; it does not claim to inject those
faults. *(PRD FR-22)*

**Given** the Testing library's `PublicAPI.Shipped.txt`,
**When** its exported surface drifts,
**Then** `PackageBoundaryTests` fails until the baseline is intentionally updated. *(NFR-11)*

## Epic 8: Aspire-grade Visual Refresh *(post-MVP chrome parity)*

> **Source of record:** `sprint-change-proposal-2026-06-25-aspire-grade-visual-refresh.md` (Correct Course,
> 2026-06-25). Raises shell chrome to .NET Aspire Dashboard polish using **Fluent v5 components + Fluent 2
> tokens only** (Aspire's v4/FAST tokens are §4.1-banned, so every pattern is translated). **Framework-only;**
> Tenants.UI page-body adoption is a separate **Host-A** Tenants correct-course. Suggested order:
> 8.1 → 8.2 → 8.3 → 8.4 → 8.5 → 8.6 → 8.7. Each story keeps both light AND dark themes verified and the
> `FluentConformanceTests` Governance lane green (no legacy v4/FAST tokens).

### Story 8.1: Neutral header chrome + footer framing *(Minor — ship first)*

As an operator,
I want the shell header and footer to be neutral chrome with the brand accent used only as an accent,
So that the app looks modern instead of a saturated colored band.

**Acceptance Criteria:**

**Given** the shell header,
**When** rendered in light or dark theme,
**Then** the header band uses `--colorNeutralBackground2` with a `--colorNeutralStroke2` bottom divider, the
app title + action icons read in neutral foreground with sufficient contrast, and no brand-accent surface
fill remains. *(FR-8; §4.1)*

**Given** the shell footer,
**Then** it renders a matching top divider + subtle (`Color.Lightweight`) text on the same neutral chrome.

**Given** the §4.1 Fluent-token guard,
**Then** no legacy v4/FAST token is introduced and the Governance lane stays green; `FrontComposerShellTests`
+ Verify snapshots + a11y/visual baselines are updated intentionally.

### Story 8.2: Accent-as-thread policy + regression guard

As an adopter developer,
I want a documented + guarded rule that the brand accent is never a chrome surface fill,
So that the neutral-header design cannot silently regress.

**Acceptance Criteria:**

**Given** architecture.md §4.1,
**Then** it states the accent (`FcShellOptions.AccentColor`, default `#0097A7`) is a *thread* (active nav,
focus, primary, links, badges) and MUST NOT fill header/nav/footer surfaces (which stay `--colorNeutralBackground*`).

**Given** a `…FluentConformanceTests` Governance guard,
**When** Shell chrome CSS uses `--fc-color-accent`/`--fc-accent-base-color` in a `background`/`background-color`,
**Then** the build fails; the guard ships with an empty, shrink-only allowlist (§4.1 discipline).

### Story 8.3: Brand/logo cell in header-start

As an operator,
I want a proper brand lockup at the top-left,
So that the header reads as a branded product surface like the Aspire logo cell.

**Acceptance Criteria:**

**Given** the header-start cluster,
**When** an adopter supplies a logo-mark fragment,
**Then** it renders exactly once before `AppTitle` with tightened lockup spacing and an accessible name.

**Given** no logo-mark fragment is supplied,
**When** the header renders,
**Then** no logo placeholder or default icon is injected, `AppTitle` remains aligned, and the no-logo
DOM/accessibility baseline is deterministic.

### Story 8.4: Compact default density + grid polish

As an operator,
I want a compact default density and Aspire-dense projection grids,
So that more data is readable at a glance, while I can still change density.

**Acceptance Criteria:**

**Given** a fresh session with no stored preference,
**Then** the default `data-fc-density` is **Compact**, and the choice remains changeable in `FcSettingsDialog`. *(FR-9)*

**Given** a projection grid,
**Then** Compact density resolves to an exact `32px` row metric through
`DataGridDensityMetrics.ResolveRowHeightPx(DensityLevel.Compact)`, row hover uses
`--colorSubtleBackgroundHover`, and the generated `FluentDataGrid` header remains sticky while the
grid body scrolls; rendered DOM/computed-style evidence and regenerated Verify snapshots pin both.

### Story 8.5: Icon+label navigation rail + projection flyout

As an operator,
I want an icon+label navigation rail with an outline→filled active state and a projection flyout,
So that navigation is compact and scannable like the Aspire app-bar while keeping the registry hierarchy.

**Acceptance Criteria:**

**Given** Desktop,
**Then** the primary nav is one rail rendered at **72px labeled** or **48px icon-only**, toggled by the
always-visible hamburger via the existing `SidebarToggledAction`/`SidebarCollapsed`; Mobile/Compact opens the drawer. *(UX-DR3)*

**Given** a bounded-context tile in the 72px labeled rail,
**Then** the tile content stacks the `FluentIcon` above the short label through a Fluent layout
primitive, while aggregate count and "New" badges render outside that icon/label stack as an overlay
indicator row; the active context uses the filled icon, accent left-bar, and `aria-current`.

**Given** a bounded-context tile in the 48px icon-only rail,
**Then** the icon remains centered and the tile keeps an accessible name through `aria-label`/tooltip;
badges remain outside the icon content stack.

**Given** a tile is activated (click/Enter),
**Then** a flyout (`FluentMenu`/`FluentPopover`) lists that context's projections (count + "New" badges); the
single-active-item rule lights the current projection; the flyout is fully keyboard-navigable (Enter/Space,
arrows, Esc, focus-return) with `role="menu"`. *(UX-DR6)*

**Historical composite delivery record:** Story 8.5 is done. The rail, badges, flyout, keyboard/focus
behavior, and accessibility pins were delivered as one composite story. Fluent UI version authority is
the repository’s central package declaration (currently `5.0.0-rc.4-26180.1`), not this historical
story; upgrades must re-run flyout anchoring/keyboard and `data-testid`/`role`/`aria-*` splatting tests.

### Story 8.6: Reusable `FcPageToolbar`

As an adopter developer,
I want a reusable page-toolbar component matching the Aspire toolbar pattern,
So that every page presents a consistent search/filter/view/tab strip.

**Acceptance Criteria:**

**Given** `FcPageToolbar`,
**Then** it renders a `FluentToolbar` with leading `FluentSearch`, a filter `FluentButton`→`FluentPopover`, a
view/overflow `FluentMenuButton`, and a right-aligned actions slot, plus an optional underline `FluentTabs`
strip for multi-view pages; it composes under `FcPageHeader`.

**Given** the FC-DOC contract (Story 1.5),
**Then** the component has a conforming doc page. *(AR4)*

### Story 8.7: Status as colored icon *(UX-DR2 amendment)*

As an operator,
I want status shown as a colored icon with the label on hover/focus,
So that statuses are scannable and lightweight like the Aspire dashboard, without losing accessibility.

**Acceptance Criteria:**

**Given** a `[ProjectionBadge]` status member,
**When** rendered,
**Then** it shows a colored Fluent icon (success = green checkmark, error = red cross, unknown/neutral = grey
question; warning/info extensions) emitted by the generator (`[ProjectionBadge]` emit; regenerated Verify snapshots). *(UX-DR2 amended)*

**Given** the status icon,
**Then** the label is revealed on **hover and keyboard focus** via `FluentTooltip`, and an `aria-label` is
**always** present (never hover-only), preserving NFR-3/WCAG 2.2 AA; numeric count slots keep the `FluentBadge` pill.

**Given** architecture.md §4.1 + epics.md UX-DR2,
**Then** both are amended to record the colored-icon status model superseding the pill-only model.

## Epic 9: Fresh-Row Producer and Row Identity *(post-MVP follow-up)*

> **Source of record:** `sprint-change-proposal-2026-07-01.md` (Correct Course, 2026-07-01). This epic
> resolves the accepted-deferred Story 2.6 AC1(b) gap by giving the row-level new-item producer a current
> backlog home. It does not reopen completed Epics 2 or 3, and it must not fabricate row identity from the
> current projection nudge seam.
> Story 9.1 is done as of 2026-07-05: the approved source is FrontComposer-owned pending-command row
> metadata populated from generated grid/command runtime context. Story 9.2 is also done; its
> implementation evidence remains the release regression baseline.

### Story 9.1: FC-NIP row-identity producer decision record

Decision status: **done 2026-07-05**. Approved payload source is FrontComposer-owned pending-command row
metadata populated from generated grid/command runtime context. EventStore status remains lifecycle/status by
`MessageId`; it is not the row-identity source. Contract:
`_bmad-output/contracts/fc-nip-row-identity-producer-contract-2026-07-04.md`.

As a FrontComposer maintainer,
I want the closed row-identity payload decision retained for fresh-row indicators,
So that future work does not reopen the source or guess from projection nudges.

**Acceptance Criteria:**

**Given** the approved 2026-07-05 contract,
**When** its payload is consumed,
**Then** FrontComposer-owned pending-command row metadata supplies the exact fields required to call
`INewItemIndicatorStateService.Add(...)`: `ViewKey` or lane key, row `EntityKey`, command `MessageId`,
projection type, and any status-slot metadata needed to avoid ambiguity.

**Given** the current EventStore status endpoint and projection nudge contracts,
**When** row identity is resolved,
**Then** neither is used as the producer: EventStore remains lifecycle/status by `MessageId`, and
projection nudges must not be diffed or used for broad row marking.

**Given** the closed decision record,
**Then** the contract artifact and completed Story 9.2 evidence remain the authority; this story has no
open decision branch and must not return to implementation status.

### Story 9.2: Wire `FcNewItemIndicator` producer and generated-grid consumer

Delivery status: **done**. Retained as the completed producer/consumer acceptance and release
regression contract; it is not a queue candidate.

As an operator,
I want rows created or materially changed by a confirmed command outcome to be marked as new,
So that live command results are discoverable in projection grids.

**Acceptance Criteria:**

**Given** the FC-NIP payload contract from Story 9.1,
**When** a command reaches the relevant terminal outcome,
**Then** the command outcome path calls `INewItemIndicatorStateService.Add(...)` with the confirmed
view/lane, `EntityKey`, `MessageId`, and timestamp.

**Given** a generated projection grid for that view/lane,
**When** `INewItemIndicatorStateService.Snapshot(viewKey)` contains entries,
**Then** the grid or shell-level grid wrapper renders `FcNewItemIndicator` with localized copy,
`role="status"`, and `aria-live="polite"` for the matching lane only.

**Given** the row materializes, the filter changes, the TTL expires, or tenant/user scope changes,
**Then** the indicator is dismissed through the existing state-service semantics.

**Given** SourceTools output changes,
**Then** generated Verify snapshots and FC-TBL public-surface tests are updated intentionally.

## Epic 10: Tooling Governance Follow-Through

> **Source of record:** `sprint-change-proposal-2026-07-01-epic-7-retro-follow-through.md` (Correct
> Course, 2026-07-01). This epic carries forward Epic 7 retrospective actions without reopening
> completed Stories 7.1-7.5.

### Story 10.1: Mechanical story evidence reconciliation

As a QA automation maintainer,
I want changed-file, story File List, and task-completion reconciliation to run before review promotion,
So that story review no longer discovers omitted story-owned files or stale completion claims.

**Acceptance Criteria:**

**Given** a story has a `baseline_commit`,
**When** the reconciliation check runs,
**Then** it compares story-owned changed files against the story File List and reports omitted,
extra, or undocumented files before the story can move to review.

**Given** a workspace has pre-existing unrelated changes,
**When** they predate the story baseline or are explicitly documented as unrelated,
**Then** the check reports them separately without forcing the story to claim ownership.

**Given** story tasks are marked complete,
**When** the check runs,
**Then** it verifies task claims against changed files, test summaries, or explicit documented blockers.

### Story 10.2: Adopter-facing historical-label cleanup

As a technical writer,
I want adopter-facing CLI, diagnostics, and Testing docs free of stale historical story ownership labels,
So that adopters are not sent to obsolete Story 9 provenance when Epic 7 owns the current contract.

**Acceptance Criteria:**

**Given** CLI, migration, diagnostics, Testing README, and published how-to docs,
**When** they describe current Epic 7 behavior,
**Then** adopter-facing text names the current contract or feature, not stale historical story ownership.

**Given** source comments or generated diagnostic registry metadata retain old Story 9 labels as
provenance,
**When** they are not adopter-facing and do not misstate current ownership,
**Then** they may remain documented as brownfield provenance.

### Story 10.3: CLI text-output parity guard

As a Test Architect,
I want text output covered at the same behavioral boundary as JSON output for CLI commands,
So that summaries, filtering, and budgets cannot drift between machine and human output.

**Acceptance Criteria:**

**Given** a CLI command has JSON summary, filtering, fail-flag, or diff-budget behavior,
**When** tests are added or changed,
**Then** text-output pins cover the same shared behavior unless the story explicitly documents why text
does not expose that field.

**Given** a migration or inspect output budget changes,
**When** JSON caps are updated,
**Then** text output caps and omitted-budget markers are updated and tested intentionally.

### Story 10.4: HFCM9002 production-emission decision

As a Product Owner and Architect,
I want an explicit decision on production HFCM9002 migration sidecar emission,
So that adopter docs either promise a real SourceTools emitter or clearly keep HFCM9002 synthetic-only.

**Acceptance Criteria:**

**Given** the current CLI migrate contract,
**When** Product and Architecture review HFCM9002,
**Then** they choose one of two paths: implement a SourceTools production sidecar emitter with tests, or
remove/de-emphasize adopter-facing promises beyond synthetic/manual sidecar evidence.

**Given** production emission is approved,
**Then** SourceTools emits the sidecar, CLI migrate reads it, docs describe it, and tests prove path
safety, redaction, and text/JSON output parity.

**Given** production emission is not approved,
**Then** CLI README and contract docs keep the synthetic-only boundary prominent.

### Story 10.5: Testing evidence redaction default-lane guard

As a developer,
I want Testing package evidence redaction to stay in the default lane,
So that assertion helpers cannot leak tenant, user, token, secret, password, oversized, or
punctuation-heavy secret values.

**Acceptance Criteria:**

**Given** Testing package evidence formatters or fakes change,
**When** the default Testing lane runs,
**Then** it includes redaction cases for tenant/user IDs, token/secret/password keys, oversized payloads,
and punctuation-heavy string secret values.

**Given** a new public Testing helper emits evidence,
**Then** `PublicAPI.Shipped.txt`, README guidance, and redaction tests are updated intentionally.

## Epic 11: Release Readiness Remediation Program *(post-MVP quality hardening)*

> **Source of record:** `sprint-change-proposal-2026-07-04.md` (Correct Course, 2026-07-04), triggered by the
> full-repo architecture/engineering-quality review (`_bmad-output/project-docs/architecture-quality-review-2026-07-04.md`:
> no Critical, 12 High, ~28 Medium). A Minor-scope quick-win fix batch (UI-host `FcPageHeader` params, orphaned
> empty-state stylesheet, nullable-numeric codegen, `GeneratedLiteral` escaping incl. a latent quote-injection
> bug, nav-slug unification, theme-watcher disposal race, `@key` on reordering loops, hygiene) was applied
> directly under the proposal (PR #48); the stories below carry the Moderate/Major remainder. **Does not reopen
> completed Epics 1–10.** Epic 11 consumes completed Epic 10 governance evidence where a story cites it. Each story references the review finding IDs it closes in
> its Change Log (proposal success criterion), and the four blind-spot guard classes (unlinked stylesheets,
> dead scoped CSS, parameter-splat surfaces, cross-request lifetimes) each gain a durable Governance test.
> The workstream/current-state table below is authoritative. Do not infer an implementation candidate
> from file order, numeric sort, or a decomposition-parent heading. Stories 11.0 and 11.8 are completed
> decision records; Stories 11.11–11.14 are completed delivery records. Stories 11.17, 11.18, and
> 11.19 are nonimplementable decomposition parents. Only their materialized children carry queue state.
> Story 11.19d approved staged adoption of `AnalysisMode=Recommended` and materialized implementable
> Stories 11.20–11.23 as sequential, separately approval-gated backlog phases; Story 11.23 is a v1.0
> publication gate.
>
> **Decision gates (contract-confirmation DoD, 2026-06-21 amendment - tracked, owned, dated):** **Story 11.0**
> (command/projection route contract) - owner **Architect + Product**, assigned **2026-07-05**, resolved
> **2026-07-05** with `/commands/{BoundedContext}/{CommandTypeName}` as the canonical generated command route
> family, recorded in `_bmad-output/contracts/fc-route-generated-command-route-contract-2026-07-05.md`.
> **Story 11.8** (Contracts kernel split
> decision and compatibility plan, amends the multi-TFM decision) - owner **Architect + PM**, assigned **2026-07-04**,
> resolved **2026-07-05** by approving the split and recording package-compat requirements in
> `_bmad-output/contracts/fc-contracts-kernel-split-compatibility-plan-2026-07-05.md`.
> **IA gate FC-IA-1** (module-tab route encoding + projection-flyout IA) was resolved and signed off
> on **2026-07-05** by **Product/UX + Architect**. Canonical module/tab routes are `/{module}/{tab}`;
> the projection flyout is secondary IA. The decision is recorded in
> `_bmad-output/contracts/fc-ia-1-module-tab-ia-decision-2026-07-05.md`.

### Epic 11 Workstreams And Current State

| Workstream | Stories | Current state on 2026-07-17 |
| --- | --- | --- |
| Runtime reliability and security | 11.0–11.5, 11.18a, 11.24 | 11.0–11.5 done; 11.18a in review; 11.24 is blocked backlog pending EventStore Story 1.20 migration authority. |
| Adopter testing and route integrity | 11.6–11.7 | Done; 11.6 consumes completed Story 10.5 privacy evidence. |
| Contracts and package boundary | 11.8, 11.11–11.14 | Done; retained as decision/delivery history, not queue candidates. |
| Maintainability and enforcement | 11.9, 11.15–11.16, 11.17a–d, 11.18b–c, 11.19a–d, 11.20–11.23 | 11.9, 11.15–11.16, and 11.17a done; 11.17b–d, 11.18b–c, and 11.19a–d in review; 11.20–11.23 are sequential, separately approval-gated backlog phases. |

Within logging remediation, ownership precedence is deterministic: 11.18a security/fail-closed sites
first, 11.18c command-lifecycle/projection/polling hot paths second, and 11.18b residual
Warning/Error/Critical sites last. A site belongs to exactly one child. Parent Stories 11.17, 11.18,
and 11.19 must never receive backlog or ready-for-dev status.

### Story 11.0: Command/projection route-contract decision gate

Decision status: **done 2026-07-05**. Canonical generated command route family is
`/commands/{BoundedContext}/{CommandTypeName}`. Contract:
`_bmad-output/contracts/fc-route-generated-command-route-contract-2026-07-05.md`.

As a Product Owner and Architect,
I want the command route family selected before Epic 11 implementation starts,
So that command activation from the palette and empty-state CTA targets real generated pages.

**Acceptance Criteria:**

**Given** the current route families — projection links `/{bc-lower}/{proj-kebab}`, palette/CTA command links (`/domain/{kebab}/{kebab}`), and generated command pages (`/commands/{BC}/{TypeName}`),
**When** Architect + Product review the route contract,
**Then** they select one canonical command route family and record the decision in a contract artifact or `architecture.md` section. *(H10 remainder; refines FR-10 / UX-DR4.)*

**Given** the route decision is recorded,
**When** Story 11.7 is created,
**Then** it implements only the selected route contract and adds the e2e route-activation pin.

**Given** Story 11.0 is not done,
**When** any Story 11.1+ `create-story` request is made,
**Then** the request is blocked with the dated owner and decision status.

### Story 11.1: Token lifecycle and circuit-safe EventStore auth

As a FrontComposer operator,
I want EventStore auth tokens stored, expired, and evicted on sign-out, and acquired safely from an interactive Blazor circuit,
So that the app does not silently lose its EventStore connection whenever there is no `HttpContext`.

**Acceptance Criteria:**

**Given** `FrontComposerUserTokenStore`,
**When** a token is stored,
**Then** its expiry is retained, expired entries are evicted, and the currently-dead `Remove` path is wired into the sign-out endpoint. *(H2)*

**Given** `FrontComposerAccessTokenProvider` running inside an interactive circuit (`HttpContext` null),
**When** it acquires a token,
**Then** it falls back to the `CircuitServicesAccessor`/token-store seam its siblings already have — or, if no circuit-safe source is configured, it fails fast at registration instead of throwing HFC2013 at read time. *(H2, M1)*

**Given** any token path,
**When** token storage, acquisition, eviction, or sign-out paths execute,
**Then** no raw token value is logged, and expired/sign-out eviction and circuit-context acquisition are pinned by tests.
*(Refines FR-7 and FR-12; closes H2, M1.)*

### Story 11.2: Projection realtime resilience

As an operator,
I want projection realtime to recover from outages instead of silently degrading to slow polling,
So that live grids keep updating after a hub disconnect longer than the default retry ladder.

**Acceptance Criteria:**

**Given** the projection hub,
**When** the connection drops for longer than the ~42 s default retry ladder,
**Then** an unbounded jittered `IRetryPolicy` plus restart-on-`Closed` (gated by the fallback driver) reconnects, instead of dying permanently and silently degrading to 15 s polling. *(H6)*

**Given** `ProjectionSubscriptionService.DisposeAsync`,
**When** the service is disposed while startup, polling, or cache seeding work is in flight,
**Then** its gate wait is bounded, the two polling drivers' disposal is aligned, `FrontComposerRegistry` live-list reads are locked, and the `ETagCacheService` seeding race is fixed (`Lazy<Task>`/semaphore, reset on failure). *(M2, M3, M4)*

**Given** the realtime wire contract,
**When** SignalR projection subscriptions are created and messages are handled,
**Then** hub method-name literals (`ProjectionChanged`, `JoinGroupScoped`, …) are pinned and `SignalRProjectionHubConnectionFactory` gains direct unit tests.
*(Refines FR-12; closes H6, M2, M3, M4.)*

### Story 11.3: MCP cross-request lifecycle and operability

As an AI agent,
I want the MCP lifecycle tracker to work across separate requests and every fail-closed branch to leave a trace,
So that a `subscribe → poll` sequence returns real transitions and operators can diagnose silent denials.

**Acceptance Criteria:**

**Given** `FrontComposerMcpLifecycleTracker`,
**When** it is registered,
**Then** it is split into a **Singleton state store + Scoped facade** (a naive Singleton flip is a captive-dependency error — it constructor-injects the Scoped admission service), and the test-side Singleton re-registrations that masked the bug are removed. *(H4, corrected per proposal §1)*

**Given** an agent lifecycle `subscribe` then `poll` across two requests,
**When** the MCP lifecycle tools are invoked through separate service scopes,
**Then** real transitions are returned (cross-scope hosting test).

**Given** the zero-signal fail-closed sites (`FrontComposerMcpProjectionReader` bare catch, tools-list, lifecycle auth),
**When** those branches deny, hide, or downgrade a request,
**Then** each logs exactly one sanitized `[LoggerMessage]` event, `BuildServiceProvider()` is removed from `AddFrontComposerMcp` (ASP0000), and API-key hashes are stored (or dev-only is documented). *(M9, M10, M12)*
*(Refines FR-17 and FR-19; closes H4, M9, M10, M12.)*

### Story 11.4: Security-validation hardening

As a FrontComposer maintainer,
I want the open-redirect funnel and storage-key builders exhaustively tested and the wire formats pinned,
So that a redirect-validation gap or storage-key collision cannot slip through untested.

**Acceptance Criteria:**

> Structure the story file as **three independently verifiable task groups** (redirect theory · storage-key convergence · wire-format pins).

**Given** `ReturnPathValidator` (today with zero direct tests),
**When** the security theory runs,
**Then** it covers every documented attack class — protocol-relative, backslash prefixes, percent-decode bypass, traversal, BiDi/zero-width, the Unix file-scheme carve-out, and non-root base href. *(H7)*

**Given** the two storage-key builders,
**When** scope keys are produced for whitespace, colon, NFD/NFC, and mixed-case-email inputs,
**Then** they converge on the canonicalizing `FrontComposerStorageKey` semantics with an FsCheck equivalence property (whitespace/colon/NFD-NFC/mixed-case-email). *(H9)*

**Given** the SignalR/HTTP wire DTOs,
**When** they serialize to or deserialize from JSON,
**Then** `ProjectionChangedDetail`, `CommandResult`, and `ProblemDetailsPayload` gain golden-JSON pins (or `[JsonPropertyName]`) and `CommandResultStatus` gets string constants. *(M11)*
*(Anchored to PRD NFR-5 Security and NFR-6 Privacy/support safety; closes H7, H9, M11.)*

### Story 11.5: Dead-CSS remediation and visual-conformance guards

As an operator,
I want components whose styling is silently dead to actually render their styles, guarded so the defect class cannot regenerate,
So that connection status (incl. the reconnect pulse), the column prioritizer, settings-dialog mobile controls, and density preview look as designed.

**Acceptance Criteria:**

**Given** the seven scoped-CSS files whose rules are dead because the class sits on a Fluent component (`FcProjectionConnectionStatus` — all rules incl. the reconnect pulse, `FcColumnPrioritizer` gear pinning, `FcSettingsDialog` mobile Done, `FcDensityPreviewPanel`, three DevMode files),
**When** they are fixed via a raw scoped root + `::deep` or inline Style (Story 8.6 precedent),
**Then** the intended styling applies, proven by rendered-DOM / computed-style evidence per E8-AI-1. *(M6)*

**Given** the undefined/FAST-era tokens (`--error`, `--error-foreground-rest`),
**When** Shell component CSS is scanned and migrated,
**Then** they are replaced with Fluent 2 tokens. *(M5)*

**Given** three new Governance guards,
**When** the Governance lane scans Shell stylesheet references and scoped CSS patterns,
**Then** every `wwwroot/css` file must be referenced by a `<link>`, a scoped-CSS-class-on-Fluent-component detector fails the build, and `error-` is added to the legacy-token regex.

> **Guard-first:** build the three guards before/with the CSS fixes — bUnit cannot detect dead CSS or silent splats, so the defect class regenerates otherwise.
*(Amends NFR-3 and NFR-4 visual conformance; closes M5, M6 + the unlinked-stylesheet and dead-scoped-CSS guard gaps.)*

### Story 11.6: Testing harness failure modes

As an adopter developer (starting with Hexalith.Tenants),
I want the Testing harness to model rejection/timeout/stall and per-request query outcomes,
So that adopters can genuinely test failure paths and paging/filter/sort of generated components.

**Acceptance Criteria:**

**Given** `TestCommandService`,
**When** adopter tests configure command and query outcomes,
**Then** it exposes configurable rejection / timeout / stall-at-`Syncing` outcomes;
`TestQueryService` / `TestProjectionPageLoader` accept per-request callbacks
(`SucceedWith(Func<QueryRequest, QueryResult<T>>)`) so paging/filter/sort are testable; and the
evidence-only `TestFaultEvidenceRecorder` records redacted observations without claiming fault
injection. *(M21)*

**Completed delivery clarification (2026-07-15):** configurable fake outcomes own rejection,
timeout, stall, query paging/filter/sort, authorization states, and async initialization.
`TestFaultEvidenceRecorder` is deliberately evidence-only: it captures redacted fault observations and
does not claim to inject runtime faults. The former `TestFaultInjectionProvider` name is retired.

**Given** the Counter sample's authorization-policy toggles,
**When** those scenarios are promoted into the Testing harness,
**Then** the harness exposes equivalent configurable authorization-policy states, and the constructor
`GetAwaiter().GetResult()` is replaced with an async factory.

**Given** the shipped Testing surface (currently 2 test files for 11 files),
**When** builders, assertions, or fakes are changed,
**Then** `Builders` / `Assertions` / fakes get direct surface tests and `PublicAPI.Shipped.txt` is updated intentionally.

**Given** Story 10.5's Testing evidence privacy findings and the Testing host contract,
**When** Story 11.6 changes fake services, per-request callbacks, builders, assertions, or fault/evidence
paths that emit diagnostic or assertion evidence,
**Then** the default Testing lane preserves redaction for configured tenant/user identifiers in JSON values
and property names, including dictionary keys, preserves structural redaction of token/secret/password keyed
values, and proves raw external/local paths are absent or replaced with bounded repository-relative or redacted
markers wherever the harness emits paths.
*(Refines FR-22; closes M21 — the key Tenants-adoption unblock.)*

### Story 11.7: Command/projection route-contract implementation

As an operator,
I want command activation from the palette/CTA to land on a page that actually exists,
So that the "jump to any action" journey does not dead-end on an unresolvable route.

**Acceptance Criteria:**

**Given** Story 11.0 has selected `/commands/{BoundedContext}/{CommandTypeName}` as the canonical generated command route family, and FC-IA-1 has fixed module-page/tab routes to `/{module}/{tab}` with the projection flyout strictly secondary (`_bmad-output/contracts/fc-ia-1-module-tab-ia-decision-2026-07-05.md`),
**When** palette command entries, projection empty-state CTAs, and generated command pages are rendered,
**Then** every generated command activation targets a route that exists and uses the selected route contract. *(H10 remainder; proposal §1 correction #3.)*

**Given** the selected route contract is implemented,
**When** the e2e command-palette activation pin runs,
**Then** an e2e pin asserts palette command activation lands on the generated page, and the route contract is recorded in a contract (`fc-*` or architecture.md §4), not only in the story.

**Given** the contract-confirmation DoD,
**When** Story 11.0 is not done **or** the FC-IA-1 module-tab route encoding / projection-flyout IA gate
is not Product/UX-signed-off,
**Then** this story remains blocked and may not move to ready-for-dev.
*(Refines FR-10 / UX-DR4; closes the H10 remainder + the unresolvable-route finding — the single most user-visible open defect in the plan.)*

### Story 11.8: Contracts kernel split decision and compatibility plan

Decision status: **done 2026-07-05**. Approved package-boundary target: keep `Contracts` as the
netstandard2.0-clean wire/attribute/schema/diagnostic kernel, move the net10/Blazor/Fluent rendering
surface to `Contracts.UI`, and complete package compatibility/public API/deprecation evidence in the
pre-v1.0 window before Stories 11.11-11.14 are marked done. Contract:
`_bmad-output/contracts/fc-contracts-kernel-split-compatibility-plan-2026-07-05.md`.

As an Architect and Product Manager,
I want the Contracts kernel split and compatibility path explicitly approved,
So that package-impacting implementation stories do not start without a v1.0 migration plan.

**Acceptance Criteria:**

**Given** the net10/Blazor surface (`Typography`/`FcTypoToken`, `RenderFragment` contexts, `KeyboardEventArgs` members),
**When** Architect + PM review the kernel split,
**Then** they approve, defer, or narrow the split and record the compatibility plan before Story 11.11 starts. *(H11)*

**Given** the decision is recorded,
**When** implementation stories are created,
**Then** they are split into Contracts.UI assembly work, misplaced-type relocation, `QueryRequest` migration, and documentation/package-compat updates.

**Given** the decision amends the documented multi-TFM decision,
**When** the plan is approved,
**Then** the decision names the affected packages, expected public API baseline changes, deprecation path, and release compatibility posture.
*(Amends NFR-2 and FR-25; gates H11, M24, M25 implementation.)*

### Story 11.11: Create Contracts.UI assembly and migrate Blazor rendering surface

Delivery status: **done**. Retained as the historical acceptance contract for the completed
Contracts.UI split; it is not a queue candidate.

As an adopter developer (Hexalith.Tenants first),
I want the net10/Blazor rendering surface split out of the netstandard Contracts kernel,
So that referencing Contracts stops inheriting the pinned Fluent RC.

**Acceptance Criteria:**

**Given** the approved Story 11.8 decision,
**When** the net10/Blazor surface is moved,
**Then** `Typography`/`FcTypoToken`, `RenderFragment` contexts, and `KeyboardEventArgs` members live in a net10-only Contracts.UI assembly or approved equivalent. *(H11)*

**Given** a consumer references only the Contracts kernel,
**When** package and project-reference validation runs,
**Then** it no longer inherits the pinned Fluent RC through Contracts.

**Given** public surfaces move,
**When** package boundary tests run,
**Then** public API baselines and docs are updated intentionally.

### Story 11.12: Relocate runtime and testing-owned types out of Contracts

Delivery status: **done**. Retained as the historical acceptance contract for completed type
relocation; it is not a queue candidate.

As a framework maintainer,
I want runtime services and test fakes removed from the Contracts kernel,
So that Contracts remains a stable wire/attribute package instead of a runtime grab bag.

**Acceptance Criteria:**

**Given** misplaced runtime and testing types,
**When** relocation is implemented,
**Then** `InMemoryStorageService` moves to Testing, `InlinePopoverRegistry` implementation moves to Shell while its contract remains where approved, `FcShellOptions` moves to Shell/options, and Fluxor action records move to Shell. *(M24)*

**Given** `LoadPageAction` currently carries a `TaskCompletionSource`,
**When** the action records move,
**Then** value-semantics and serialization concerns no longer leak into Contracts.

**Given** the relocation changes package surfaces,
**When** tests and package validation run,
**Then** consumer-facing packages still build and public API deltas are intentional.

### Story 11.13: Decompose `QueryRequest` through the HFC0001 migration path

Delivery status: **done**. Retained as the historical acceptance contract for the completed
QueryRequest migration; it is not a queue candidate.

As an adopter developer,
I want UI query concerns separated from transport and caching concerns,
So that query contracts are stable, composable, and migratable before v1.0.

**Acceptance Criteria:**

**Given** the 19-parameter `QueryRequest`,
**When** decomposition is implemented,
**Then** UI-facing query criteria and EventStore transport/caching envelope concerns are separated via the existing HFC0001 deprecation pipeline. *(M25)*

**Given** existing generated and runtime consumers,
**When** they compile against the migrated contracts,
**Then** source compatibility, obsolete diagnostics, and migration guidance are covered by tests.

**Given** the CLI and MCP surfaces serialize query-related shapes,
**When** wire-shape tests run,
**Then** public JSON shape changes are either avoided or explicitly versioned.

### Story 11.14: Update architecture, project context, UX trace, and package compatibility docs

Delivery status: **done**. The package-boundary, public-API, planning, and compatibility evidence was
completed with Stories 11.11–11.13. This section is release history, not open documentation work.

As a release owner,
I want the kernel split and query migration reflected in planning and published references,
So that adopters understand the new package boundaries and compatibility story.

**Acceptance Criteria:**

**Given** Stories 11.11-11.13 change package boundaries or public contracts,
**When** documentation is updated,
**Then** `project-context.md`, architecture layer documentation, UX-DR1's home for `Typography`/`FcTypoToken`, release notes, and package-compat guidance reflect the approved shape.

**Given** docs are changed,
**When** documentation validation runs,
**Then** generated planning docs remain under `_bmad-output/` and published docs under `docs/` are updated only where product references require it.

### Story 11.9: Shell layering declaration and route/label relocation

As a FrontComposer maintainer,
I want the real Shell layering declared and route/label helpers moved out of the render layer,
So that dependency direction is visible and enforceable.

**Acceptance Criteria:**

**Given** shell layering,
**When** architecture boundaries are updated,
**Then** Telemetry is declared cross-cutting, connection/polling workers move to Infrastructure, and folder dependency direction is documented. *(M18)*

**Given** navigation route and label helpers live on the Razor component,
**When** the helpers are relocated,
**Then** `BuildRoute` and `ProjectionLabel` move into `Routing/` and render components call the shared helpers.

**Given** the declared layering,
**When** architecture tests run,
**Then** folder dependency directions are pinned.
*(closes M18 subset; depends only on the already-landed Minor batch — PR #48 — so it is backward, not forward.)*

### Story 11.15: Storage scope and snapshot publisher consolidation

As a FrontComposer maintainer,
I want duplicated scope-resolution and snapshot-publisher helpers consolidated,
So that tenant/user hardening and subscription behavior are applied uniformly.

**Acceptance Criteria:**

**Given** multiple `TryResolveScope` implementations exist,
**When** storage scope resolution is consolidated,
**Then** all Shell persisted features use a single `StorageScopeResolver` and tests cover tenant/user fail-closed behavior. *(M19)*

**Given** several hand-rolled snapshot pub/sub containers exist,
**When** `SnapshotPublisher<T>` or an approved equivalent is introduced,
**Then** subscription, disposal, fault isolation, and snapshot behavior are covered once and reused by former duplicate sites. *(M19)*

**Given** duplicated call sites are removed,
**When** the story validation runs,
**Then** before/after call-site reduction is documented in the story File List or Change Log.

### Story 11.16: Fatal, hydration, JSON, and generated-literal helper consolidation

As a FrontComposer maintainer,
I want small duplicated helpers consolidated by defect class,
So that hardening fixes do not depend on remembering every copy.

**Acceptance Criteria:**

**Given** fatal-exception filters exist in multiple variants,
**When** `ExceptionGuard.IsFatal` or an approved equivalent is introduced,
**Then** all former fatal-filter sites use the same helper and focused tests cover cancellation, fatal, and non-fatal cases. *(M19)*

**Given** repeated hydration enums and JSON options exist,
**When** they are consolidated,
**Then** a single `HydrationState` enum and shared `FcJson` options are used where the semantics match.

**Given** `RoleBodyHelpers` still owns an escaping path,
**When** generated literal escaping is consolidated,
**Then** it delegates to the shared `GeneratedLiteral` path without regressing generated-source parsing.

### Story 11.17: Mechanical one-type-per-file split

> **Nonimplementable decomposition parent.** Queue state belongs only to 11.17a–d; this parent must
> never move to backlog, ready-for-dev, or review.

**Decomposition (correct course 2026-07-05).** Split by package into independently reviewable child
stories. Each keeps the parent constraint: mechanical only — behavior and public-API shape unchanged
except intentional file organization and any documented API-baseline update. A durable one-type-per-file
Governance guard (the "multi-type file" blind-spot guard class) is added or extended so the convention
is enforced, not merely applied.

- **11.17a — CLI package split (`11-17-cli-package-split.md`, done).** `MigrationCommand.cs` (23 types), `InspectCommand.cs` (14 types) →
  one-type-per-file. Validation lane: CLI in-process xUnit lane + `frontcomposer.cli.inspect.v1` /
  `frontcomposer.cli.migrate.v1` contract pins + CLI `PublicAPI.Shipped.txt` unchanged.
- **11.17b — SourceTools package split (`11-17-sourcetools-package-split.md`, review).** `DriftDetection.cs` (17 types) → one-type-per-file.
  Validation lane: SourceTools drift lane + HFC parity + generated-output byte stability (P12
  no-`CompilationProvider` isolation preserved).
- **11.17c — MCP/runtime split + benchmark-harness relocation (`11-17-mcp-runtime-split-and-benchmark-relocation.md`, review).** `SkillCorpus.cs` (~45 types) →
  one-type-per-file, and move the LLM benchmark harness out of the runtime package into
  `Shell.Tests.Bench` (`[Trait("Category","Performance")]`). Validation lane: MCP in-process lane +
  Testing package-boundary tests + `Shell.Tests.Bench` builds; the runtime package no longer ships the
  benchmark harness.
- **11.17d — Shell interface+impl+DTO bundle split (`11-17-shell-bundle-split.md`, review).** Shell multi-type files (interface + impl + DTO
  bundles) → one-type-per-file, retaining the documented Fluxor action-group exception. Validation
  lane: focused Shell one-type-per-file Governance guard + broad Shell non-Contract lane +
  `PublicAPI.FcTbl.Shipped.txt` unchanged.

As a FrontComposer maintainer,
I want the worst multi-type files split mechanically,
So that the codebase matches the documented one-type-per-file convention before broader refactors.

**Acceptance Criteria:**

**Given** the worst multi-type files (`MigrationCommand.cs` 23 types, `SkillCorpus.cs` ~45 — move the LLM benchmark harness out of the runtime package, `DriftDetection.cs` 17, `InspectCommand.cs` 14, plus the Shell interface+impl+DTO bundles),
**When** the mechanical split runs,
**Then** they are split one-type-per-file (the Fluxor-action-group exception documented if retained). *(M14)*

**Given** the split is mechanical,
**When** tests and generated-output checks run,
**Then** behavior and public API shape remain unchanged except for intentional file organization and any documented API baseline updates.

### Story 11.18: LoggerMessage migration for warnings and hot paths

> **Nonimplementable decomposition parent.** Queue state belongs only to 11.18a–c; this parent must
> never move to backlog, ready-for-dev, or review.

**Decomposition (correct course 2026-07-05).** Split by defect class, security-adjacent work first.
Each child preserves the parent's sanitization constraint: no raw token, tenant-secret, payload, stack
trace, or sensitive identifier is emitted.

- **11.18a — Fail-closed / security log sites (`11-18-fail-closed-security-log-sites.md`, review).** MCP + Shell fail-closed branches →
  `[LoggerMessage]`. Validation lane: MCP + Shell Governance sanitized-logging lane (ties to
  NFR-6/NFR-11); sanitization tests prove no sensitive value is emitted.
- **11.18b — Residual warning-and-above log sites (`11-18-warning-and-above-log-sites.md`, review).** After 11.18a security and 11.18c hot-path ownership is frozen, all residual Warning/Error/Critical direct sites in the 49-file census →
  `[LoggerMessage]`. Validation lane: Shell unit lane + a guard that Warning+ sites use
  source-generated logging.
- **11.18c — Hot-path log sites (`11-18-hot-path-log-sites.md`, review).** Command-lifecycle, projection-refresh, and polling hot-path sites →
  `[LoggerMessage]`. Validation lane: LoggerMessage guard; remaining direct calls are below the
  migration threshold or documented intentional.

As a FrontComposer maintainer,
I want warnings and hot logging paths migrated to source-generated logging,
So that logging follows the project's performance and analyzer conventions.

**Acceptance Criteria:**

**Given** the post-11.18a census has 208 direct log calls across exactly 49 Shell files — 117 at
Warning/Error/Critical and 91 at Trace/Debug/Information —
**When** warning-and-above and hot-path log sites are migrated,
**Then** the exact inventory is frozen before edits, each site is assigned once by the security → hot
path → residual Warning+ precedence, and owned sites migrate to `[LoggerMessage]`. *(M15)*

**Given** MCP and Shell fail-closed branches log sanitized details,
**When** the logging tests run,
**Then** no raw token, tenant-secret, payload, stack trace, or sensitive identifier values are emitted.

**Given** direct logger calls remain,
**When** review checks run,
**Then** remaining direct calls are either below the migration threshold or documented as intentional.

### Story 11.19: Enforcement and policy alignment

> **Nonimplementable decomposition parent.** Queue state belongs only to 11.19a–d; this parent must
> never move to backlog, ready-for-dev, or review.

**Decomposition (correct course 2026-07-05).** Split by defect class. Each child names its validation
lane and does not disable warnings or analyzer findings globally.

- **11.19a — Doc-comment (CS1591) enforcement realignment (`11-19-doc-comment-enforcement-realignment.md`, review).** Restore documented CS1591 enforcement on
  the Contracts public API-freeze folders (the `.editorconfig` re-raise is currently dead under the
  src-wide NoWarn). Validation lane: Release build under `TreatWarningsAsErrors=true` + a guard proving
  CS1591 is enforced on the API-freeze surface.
- **11.19b — AppHost NuGet audit suppression (`11-19-apphost-nuget-audit-suppression.md`, review).** Replace the blanket `NU1902-04` NoWarn with
  per-advisory `NuGetAuditSuppress` (CI-verifiable). Validation lane: CI audit lane / Governance test.
- **11.19c — Localization + identifier alignment (`11-19-localization-and-identifier-alignment.md`, review).** Localize the `FcHomeCard` aria-label and the UI
  host `lang="en"`/English strings; rename `HFC2106_ThemeHydrationEmpty` (ID string unchanged; obsolete
  alias if the constant is public). Validation lane: Shell localization/Governance lane +
  diagnostic-catalog parity.
- **11.19d — Analyzer-elevation decision gate (`11-19-analyzer-elevation-decision.md`, review).** Architecture and Product approved staged
  adoption of `AnalysisMode=Recommended` with unchanged TWAE, built-in analyzers only, and narrow
  owner-bound exceptions. The decision is recorded in
  `_bmad-output/contracts/analyzer-elevation-decision-2026-07-16.md` and materialized sequential,
  separately approval-gated Stories 11.20–11.23. This decision story does not activate policy.

As a release owner,
I want documented enforcement policies to match what the build and governance lanes actually enforce,
So that readiness claims are verifiable instead of aspirational.

**Acceptance Criteria:**

**Given** the inert CS1591 config for Contracts public folders (the `.editorconfig` re-raise is dead under the src-wide NoWarn),
**When** enforcement policy is aligned,
**Then** documented enforcement is put back in force; the AppHost blanket `NU1902-04` NoWarn is replaced with per-advisory `NuGetAuditSuppress` (CI-verifiable only); `FcHomeCard` aria-label + the UI host `lang="en"`/English strings are localized; `HFC2106_ThemeHydrationEmpty` is renamed (ID string unchanged; obsolete alias if the constant is public). *(M16, H12)*

**Given** the no-third-party-analyzer policy,
**When** Architect reviews elevating built-in analyzers (`AnalysisMode Recommended`),
**Then** a decision is recorded — it adds no packages, but the burn-down cost must be owned.

**Given** enforcement changes can create broad churn,
**When** implementation stories are created from this policy story,
**Then** each story names its validation lane and does not disable warnings or analyzer findings globally.
*(closes M16, H12 + policy-decision parts of the convention-drift cluster.)*

### Story 11.20: Recommended analyzer policy and exception ledger

**Status:** backlog. **Owner:** Architect + Framework Maintainer. **Due:** 2026-07-24.
**Approval gate:** separate Architecture/Product approval.

As an Architect and Framework Maintainer,
I want every current analyzer suppression and Naming diagnostic classified into a narrow exception or an actionable fix,
So that `AnalysisMode=Recommended` can be adopted without breaking public compatibility or hiding findings globally.

**Given** the Story 11.19d census,
**When** the policy audit runs,
**Then** all 2,958 Naming findings and every effective warning control are recorded in a versioned, owner-bound exception/fix ledger.

**Given** CA1707 conflicts with required underscore-separated test names and public diagnostic constants,
**When** dispositions are recorded,
**Then** compatibility is preserved with the narrowest supported mechanism and no repository/category-wide CA suppression.

**Given** a Naming finding lacks an approved exception,
**When** the candidate lane runs,
**Then** it is fixed or moved to a separately approved owner-bound defect story.

**Given** warning controls have different sources and owners,
**When** the audit completes,
**Then** each is classified as remain, narrow, move, or fix without absorbing Story 11.19a documentation or package-audit policy.

**Given** the built-in-analyzers-only policy,
**When** Governance runs,
**Then** no analyzer package or broad CA disable exists, TWAE remains unchanged, and the ledger matches effective configuration.

**Given** this phase changes policy boundaries rather than product behavior,
**When** validation completes,
**Then** normal Release, focused policy, and default lanes pass and baselines change only when explicitly approved.

### Story 11.21: Recommended analyzer product and generator burn-down

**Status:** backlog. **Depends on:** 11.20. **Owner:** Framework Maintainer + SourceTools Maintainer.
**Due:** 2026-08-14. **Approval gate:** separate Architecture/Product approval.

As a Framework and SourceTools Maintainer,
I want product-source and generator-emission findings fixed by defect class,
So that every shipped package and generated consumer can build cleanly under the approved `Recommended` policy.

**Given** Story 11.20's approved ledger,
**When** product projects build under Recommended with unchanged TWAE,
**Then** all 367 product findings are fixed or covered by pre-approved narrow compatibility exceptions.

**Given** 503 findings occur in SourceTools output,
**When** generator findings are remediated,
**Then** fixes occur in emitters or annotated source, never `obj/`, and generated consumers prove the correction.

**Given** CA1848/CA1873 dominate logging findings,
**When** logging work runs,
**Then** it follows the source-generated `LoggerMessage` convention without reopening Story 11.18 ownership.

**Given** remaining product findings span several CA categories,
**When** grouped,
**Then** every change has a named diagnostic/package scope and preserves public API, schema, wire, lifecycle, MCP, and artifact contracts.

**Given** netstandard2.0 compiler-host compatibility is load-bearing,
**When** kernel/analyzer projects are validated,
**Then** the Contracts/Schema/SourceTools TFM boundaries and netstandard gate remain intact.

**Given** the burn-down is complete,
**When** validation runs,
**Then** owned product/generated consumers have zero actionable findings and all required Release, focused, default, Governance, Contract, and baseline gates pass.

### Story 11.22: Recommended analyzer test and sample burn-down

**Status:** backlog. **Depends on:** 11.21. **Owner:** Test Architect + Framework Maintainer.
**Due:** 2026-09-04. **Approval gate:** separate Architecture/Product approval.

As a Test Architect and Framework Maintainer,
I want test and sample analyzer debt burned down without weakening intentional fixture semantics,
So that the complete repository can approach `Recommended` activation with trustworthy verification.

**Given** the original 3,500 test and 203 sample findings,
**When** this phase starts after 11.20-11.21,
**Then** every remaining diagnostic is assigned by project, ID, and approved disposition.

**Given** underscore-separated test names are required,
**When** CA1707 is handled,
**Then** the approved narrow 11.20 mechanism is used without mass rename or global suppression.

**Given** tests intentionally contain invalid code and specialized fixtures,
**When** a suppression remains necessary,
**Then** it is minimal and ledgered with rationale, owner, review date, and revalidation trigger.

**Given** generated Shell specimens contributed findings,
**When** Story 11.21 emitter fixes are consumed,
**Then** test projects validate the output without editing `obj/` or duplicating fixes.

**Given** samples are adopter guidance,
**When** sample findings are fixed,
**Then** samples still teach supported APIs and security/package boundaries without hiding genuine warnings.

**Given** the phase is complete,
**When** validation runs,
**Then** test/sample projects have zero actionable findings and default, Governance, Contract, snapshot, compatibility, and Release gates pass without unapproved drift.

### Story 11.23: Recommended analyzer repository activation

**Status:** backlog. **Depends on:** 11.22. **Owner:** Architect + Framework Maintainer + Release Owner.
**Due:** 2026-09-11. **Approval gate:** separate Architecture/Product approval. **Release gate:** v1.0.

As an Architect, Framework Maintainer, and Release Owner,
I want the approved `AnalysisMode=Recommended` posture activated and governed repository-wide,
So that analyzer strictness becomes a durable v1.0 build invariant.

**Given** Stories 11.20-11.22 are done with zero actionable findings,
**When** activation begins,
**Then** `AnalysisMode=Recommended` is declared centrally without new analyzer packages, weaker TWAE, or broad CA suppression.

**Given** netstandard2.0 compiler-host compatibility is explicit,
**When** the property is evaluated across Contracts, Schema, and SourceTools,
**Then** their TFM/analyzer boundaries remain preserved and documented.

**Given** the benchmark project has a warning-policy exception,
**When** the repository gate is finalized,
**Then** it is reconciled and the forced Release solution build reports zero warnings and zero errors.

**Given** analyzer policy can regress,
**When** Governance runs,
**Then** it proves the central setting, built-in-only rule, unchanged TWAE, no broad suppression, ledger/config parity, and candidate/current build parity.

**Given** activation can affect emitted/public surfaces,
**When** validation runs,
**Then** all required default, Governance, Contract, package/PublicAPI, schema, generated-output, Verify, Pact, docs, and artifact lanes pass without unapproved drift.

**Given** this is a v1.0 release gate,
**When** the story reaches review,
**Then** Release Owner evidence is linked and rollback requires a separately approved policy change that cannot weaken TWAE or hide diagnostics globally.

### Story 11.24: Adopt the Owner-Approved EventStore Runtime Identity

**Status:** backlog. **Owner:** FrontComposer Maintainer + EventStore Maintainer.
**Activation gate:** EventStore Story 1.20 must durably record `final_decision: available`,
`authorize_consumer_migration: true`, a 40-hex `tested_runtime_sha`, named owner approval, and the
approved package version and SHA-256 inventory. Until then this story has no implementation file and
must not move to `ready-for-dev`.

As a FrontComposer maintainer,
I want source and package modes aligned to the owner-approved EventStore runtime identity,
So that FrontComposer validates and releases against one auditable backend contract without mixed or
unapproved dependency identities.

**Given** EventStore Story 1.20 remains blocked, non-authorizing, incomplete, or lacks any required
source/package/approval identity,
**When** FrontComposer backlog selection runs,
**Then** this story remains `backlog`, no EventStore or Builds gitlink is changed, and current command,
query, projection, realtime, rollback, and topology behavior remains intact.

**Given** Story 1.20 authorizes consumer migration and names the approved EventStore source SHA,
**When** Debug/source mode is adopted,
**Then** `references/Hexalith.EventStore` gitlink and checkout both equal that SHA, the EventStore
submodule is not edited, and only FrontComposer-root-declared submodules are initialized.

**Given** Story 1.20 names the approved 14-package version and hashes,
**When** Release/package mode restores from an isolated cache,
**Then** `Hexalith.EventStore.Aspire` and every resolved `Hexalith.EventStore*` asset use that exact
version, the fetched package bytes match the approved hashes, no EventStore project reference enters
the Release asset graph, and the selected `Hexalith.Builds` gitlink already exposes that version.

**Given** FrontComposer's committed EventStore consumer pacts,
**When** provider verification runs against the exact approved EventStore SHA over real loopback TCP,
**Then** every interaction passes with deterministic provider-state setup/teardown and a bounded,
redaction-clean report, and `eng/validate-contract-artifacts.ps1 -RequireProviderVerification` passes
using the real EventStore provider-test project rather than the obsolete solution handoff.

**Given** source and package identities are aligned,
**When** adoption validation runs,
**Then** Debug/source AppHost build, Release/package build, Governance, the default solution lane with
`DiffEngine_Disabled=true`, and an Aspire smoke covering EventStore health, command submit/status,
query/provenance, and projection SignalR all pass.

**Given** this story only adopts an approved runtime identity,
**When** compatibility evidence is reviewed,
**Then** it does not remove or redesign FrontComposer adapters, rollback paths, topology, or deploy an
EventStore container; any behavioral migration is routed to a separately approved compatibility story.
