---
title: Hexalith.FrontComposer Product Requirements Document
status: draft
product_approval: pending-reapproval (D-9 amendment required after the 2026-09-08 reconciliation)
v1_readiness_milestone_reached: false
created: 2026-07-05
updated: 2026-09-08
---

# PRD: Hexalith.FrontComposer

## 0. Document Purpose

This PRD is for Product, Architecture, UX, developer agents, and downstream BMAD story workflows preparing Hexalith.FrontComposer for the v1.0 readiness milestone. It consolidates the delivered product baseline, the completed post-MVP remediation program, and the remaining milestone gates into one product requirements source of record.

- **Single canonical copy.** `_bmad-output/planning-artifacts/prd.md` is the only source of record. The 2026-07-05 BMad run copy is archived and immutable under `_bmad-output/planning-artifacts/archive/prds/prd-frontcomposer-2026-07-05/`. The 2026-09-08 run folder `_bmad-output/planning-artifacts/prds/prd-frontcomposer-2026-09-08/` holds the validation reviews and the decision memlog (`.memlog.md`).
- **Addendum.** Mechanism, rejected alternatives, qualitative UX rules, and the source inventory live in `_bmad-output/planning-artifacts/prd-addendum-2026-09-08.md`. `architecture.md` and `ux-design.md` remain the canonical technical and UX planning sources (D-2).
- **"v1.0 readiness" is a milestone label, not a semantic version and not a publication switch.** Packages ship continuously under semantic-release and are at 4.x (v4.4.0 tagged 2026-09-07). Each 4.x release is authorized individually by the governed-release controls in FR-24 (exact-SHA operator `workflow_dispatch` plus protected production-environment approval); nothing in this document blocks or authorizes a 4.x release. "Reaching the milestone" means the Release Owner classifies one release as v1.0-ready after every gate in §0.1 is closed.
- **Document approval is not milestone classification.** Frontmatter `product_approval` records whether Product has re-approved this text (D-9). Frontmatter `v1_readiness_milestone_reached` mirrors §0.1 and can be `true` only when §0.1 has no open rows.
- Requirements were source-derived from `_bmad-output/planning-artifacts/*.md`, `_bmad-output/contracts/*.md`, `_bmad-output/implementation-artifacts/*`, and `_bmad-output/project-docs/*.md`; the latter is provenance only and is superseded by `_bmad-output/contracts/` and `architecture.md` where they disagree (§11). Inline `[ASSUMPTION]` callouts are indexed in §13; open items are listed in §12.2. Identifier families used in this document (H/M, AD-n, DW-n, E9-AI-n, OI-n, G-n) are defined in §3.

### 0.1 V1.0 Readiness Milestone Gates

This table is the only authority on what still separates the product from the readiness milestone. D-9 may not read "Blocks: None" while any row is open. Evidence gates are closed by an artifact; approval gates are closed by a named approver citing the artifact they read. Anything not listed here is a regression baseline or a closed record (§5.0).

**Evidence gates**

| Gate | Pass condition | Owner | Evidence artifact | State on 2026-09-08 |
| --- | --- | --- | --- | --- |
| G-1 Release ledger closure | (a) The Release Owner has confirmed the published byte hashes and signed off the FR-24 ledger row for v4.1.1. (b) v4.2.0 (2026-08-30), v4.3.0 (2026-09-05), and v4.4.0 (2026-09-07) — tagged while the ledger and `sprint-status.yaml` still read "further publication remains unauthorized" — each have a dated disposition recording who dispatched, which manifest and classification, `publish_authorized`, whether the GOV-1 "before the next accepted governed release manifest" condition was met or waived, and a compliant / fallback-approved / non-compliant verdict. (c) The stale "unauthorized" sentences in the ledger and `sprint-status.yaml` are corrected to the REL-5 model. | Release Owner | `_bmad-output/implementation-artifacts/rel-ai-1-release-evidence-ledger.md` | Open. v4.1.1 row is `published-byte-verified / fallback-approved`, pending sign-off; three tags have no rows. |
| G-2 GOV-1 external seam | One table names the authoritative Hexalith.Builds SHA per purpose — execution pin in `.github/workflows/release.yml` (`4eb33928a1d8c7775f97221cf9edc171db0cb5f8`), gitlink `references/Hexalith.Builds` (`35c3d1e5b8a55a74a440b9c2cad4c5e18747b241`), owner-accepted BUILD-REL-1 revision (`a8a50859fa2f27f511a9470dfe1e3ae54d0ebc1a`, 2026-08-08) — and the Release Owner records a dated acceptance that the execution pin satisfies BUILD-REL-1; the v4.2.0 Release and Release-Evidence runs are cited by run ID as the AD-13/AD-15 end-to-end proof, or the proof is declared not yet produced; the GOV-1 sprint action is closed. `evaluator_authorizations` in `eng/dependency-graph-policy.json` are already populated (7 ci / 7 release / 5 post_release). | Architecture + Release Owner | `release.yml`; `eng/dependency-graph-policy.json`; v4.2.0 run IDs | Open on acceptance and proof citation; no code work identified. |
| G-3 EventStore runtime identity | `frontcomposer-eventstore-approved-runtime-identity-v1.json` records a dated migration approval to the built identity (EventStore `3.103.0`, source `059f6a89…`, Builds `35c3d1e5…`) with `migrationApprovalClaimed: true`, signed by the EventStore maintainer role — or, if that role is the same person as the FrontComposer maintainer, the record says so explicitly; the live Pact provider evidence at that identity is linked. Rollback to `3.91.1` is not a path (D-12). | FrontComposer maintainer + EventStore maintainer | Identity contract; `_bmad-output/implementation-artifacts/evidence/pact-provider-reconciliation/` | Open on the approval record only: live-compatibility Pact provider evidence for `3.103.0` passed 2026-09-08 (19/19 interactions; AppHost smoke passed). |
| G-6 Named adopter bootstrap proof | Hexalith.Tenants boots through the documented three-call path and renders at least one generated projection and one generated command; the evidence file exists at the named path with a date and candidate SHA. Fallback if Tenants cannot be evidenced: Product decides between Parties as the substitute adopter and holding the milestone (D-7; no default). | Product Owner + Tenants maintainer | `_bmad-output/implementation-artifacts/tests/tenants-bootstrap-acceptance.md` (path reserved; file does not exist yet) | Open. No evidence artifact exists. |
| G-7 MCP leak/oracle audit | The SM-4 suite exists as a named test class in the Governance lane and covers: byte-identical hidden-vs-absent shapes per caller for tools; list-vs-call and skill-vs-projection oracles; an unregistered resource URI exercised through the SDK dispatch (not the reader directly); the non-Development ban on `AllowAll*` gates (OI-3); endpoint authentication on `MapFrontComposerMcp` (FR-19). Timing side channels are out of scope for the milestone (FR-19). | Security reviewer (a person distinct from the PRD author) + Framework maintainer | Test class path to be named in the OI-3 story | Open. Admission/taxonomy/redaction tests exist; the audit suite, the SDK-dispatch test, and the allow-all check do not. |

**Approval gates**

| Gate | Pass condition | Approver | Must cite | State on 2026-09-08 |
| --- | --- | --- | --- | --- |
| G-4 Product re-approval | Product records a dated D-9 amendment approving this text. | Product Owner | This PRD at its `updated` date; `review-*-post.md` in the run folder | Open. |
| G-5 Epic 9 acceptance | Product records acceptance of the Story 9.8 proof packet as composed/live FC-NIP evidence, and `sprint-status.yaml` closes `epic-9` and E9-AI-1…E9-AI-6 or rewrites them as residuals. | Product Owner | `_bmad-output/implementation-artifacts/tests/9-8-live-acceptance.md` | Open on the record; Stories 9.1–9.8 are done and the live proof passed 2026-08-27. |

## 1. Vision

Hexalith.FrontComposer is the Hexalith Blazor Front Shell: a .NET framework that turns annotated domain read models and commands into an operations-ready Blazor UI, an MCP tool/resource surface for AI agents, and developer tooling for inspection, migration, and testing.

The product bet is that domain teams should describe their operational surface once, in code, then get consistent human and AI access paths without hand-building every admin shell. FrontComposer makes the domain type the source of truth and uses source generation, schema fingerprints, and strict governance tests to keep UI, lifecycle state, MCP descriptors, and tooling aligned.

The v1.0 readiness milestone means two things at once and this PRD commits to both: a **developer framework that is safe to package, consume, test, customize, and evolve**, and an **operations shell an operator can trust** — fresh rows are marked only from real command outcomes, projection freshness recovers after transport failure, and command state never overstates what the backend confirmed. The post-MVP remediation program (Epics 9–11) that delivered those properties is complete. What remains is the closure in §0.1: mostly evidence and ownership records, plus two items that still require engineering — the MCP leak/oracle audit (G-7) and the Tenants bootstrap proof (G-6).

## 2. Target Users

### 2.1 Primary Users

- **Adopter developer** — a developer on a Hexalith domain module such as Tenants, Parties, or future domain packages who wants an admin/operations shell without writing bespoke Blazor scaffolding.
- **Operator** — an authenticated admin or support user who needs to browse projections, understand status, and execute commands safely.
- **AI-agent integrator** — a maintainer exposing the same domain command/projection surface to MCP clients with fail-closed security.
- **Framework maintainer** — a FrontComposer contributor evolving the generator, Shell, MCP server, CLI, Testing package, and public contracts.
- **Release owner** — a maintainer responsible for semantic-release, NuGet package quality, public API baselines, docs validation, and evidence artifacts.

### 2.2 Jobs To Be Done

- Stand up a domain operations shell from annotated C# types and a small DI bootstrap sequence.
- Browse live projection data with accessible, filterable, and status-rich UI, and know whether a row is fresh and whether the read model is current.
- Submit commands with lifecycle feedback, confirmation, authorization, and concurrency safeguards.
- Expose generated commands and projections to AI agents without leaking tenant, auth, or schema details.
- Inspect generated output, detect drift, migrate across version edges, and test generated UI in downstream packages.
- Preserve package, public API, and release quality under strict warnings, governance tests, and evidence-classified NuGet publication.

### 2.3 Non-Users (v1)

- Teams looking for a generic no-code CRUD builder or a non-Hexalith admin-template marketplace.
- Teams not using .NET, Blazor, Fluent UI, or the Hexalith EventStore-style command/query model.
- Consumer-facing product teams needing highly bespoke marketing or transactional UX rather than operations/admin workflows.
- Mobile-native, desktop-native, or hardware clients.

### 2.4 Key User Journeys

All six journeys describe delivered baseline behaviour; this PRD adds no new journey. Named non-goals inside a journey are called out where they matter.

- **UJ-1. Nina boots a domain shell from annotated types.** Nina is adding an operations UI for a Hexalith domain module. She annotates projection and command types, calls `AddHexalithFrontComposerQuickstart()`, `AddHexalithDomain<TMarker>()`, and `AddHexalithEventStore(...)`, then renders `<FrontComposerShell>@Body</FrontComposerShell>`. The shell starts, generated registrations populate navigation, and the empty state is useful even before domain data exists. **Edge case:** if bootstrap calls are missing or misordered, startup fails fast with a named error instead of failing at first render.

- **UJ-2. Marc investigates a live projection.** Marc, an operator, opens the shell home page and sees Modules ordered by urgency (ready modules first, then by descending actionable count, then by name). He opens a Module, lands on its default Module Tab, switches to a projection through the flyout, filters the Fluent DataGrid, and expands a row detail. He sees loading, empty, stale, reconnecting, fallback-polling, slow-query, and max-items states without losing accessibility context, and a fresh-row indicator appears on rows his own recent commands materially changed. The value lands when Marc can identify the relevant row and trust whether the read model is current. **Non-goal:** commands whose target key is allocated server-side (create-style commands without a declared target) produce no fresh-row marker in v1.0 (FR-13, DW-679). **Edge case:** if his tenant context is missing, the shell fails closed with an explicit state rather than showing empty-looking data.

- **UJ-3. Marc executes a command safely.** Marc opens a generated command form, sees only editable fields, confirms destructive intent when required, submits, and follows the command through Submitting, Acknowledged, Syncing, and a terminal or degraded landing: Confirmed, Rejected, IdempotentConfirmed, NeedsReview, Warning, or Degraded. The value lands when the UI distinguishes accepted command transport from projection-confirmed outcome, and a stalled confirmation becomes Degraded after the documented budget instead of spinning.

- **UJ-4. Ravi exposes the domain surface to an AI agent.** Ravi hosts the MCP server behind host authentication, registers tenant and resource visibility gates, and allows an agent to discover visible generated tools and projection resources. The agent can call commands only after admission, schema negotiation, argument validation, and server-side injection of controlled fields, and can follow lifecycle across requests through `frontcomposer.lifecycle.subscribe`. **Edge case:** auth, tenant, and hidden-tool failures collapse to the opaque public shapes in FR-19; for tools the agent cannot tell "hidden" from "absent". **Non-goal:** the resource catalog (`resources/list`) is static and not tenant-filtered; FR-19 states exactly what it discloses.

- **UJ-5. Camille preserves generator/runtime compatibility.** Camille changes a generator contract. She updates diagnostics, generated-output snapshots, schema fingerprints, public API baselines, and CLI inspect/migrate behavior. The value lands when drift is detected intentionally and consumers get a clear migration path rather than silent mismatch.

- **UJ-6. Sophie tests a generated consumer experience.** Sophie writes bUnit tests using the Testing package host, deterministic fakes, evidence recorders, and assertion helpers, and simulates command success, rejection, timeout/stall, authorization denial, and paging/filter/sort so Tenants can test realistic failure UX (FR-22).

## 3. Glossary

- **Annotated Domain Type** — a C# type marked with FrontComposer attributes such as `[Projection]`, `[Command]`, or `[BoundedContext]`.
- **Projection** — a read-model type annotated for generated browsing UI and MCP resource exposure.
- **ProjectionRole** — the `[ProjectionRole]` attribute value (for example list, timeline, card) that selects the generated view shape and its Loading/Empty/Data rendering.
- **Bounded Context** — the adopter/generator noun for the domain boundary that owns related projections and commands. Operators see it as a **Module**; command routes keep `{BoundedContext}` as a technical segment, not a chrome label.
- **Module** — the operator-facing workspace for one bounded context. A module owns one primary shell entry and exposes its views as module tabs.
- **Module Tab** — a primary view inside a module, encoded by the canonical route `/{module}/{tab}`. The default tab is named after the module plural label (fallback `Overview`), and `/{module}` is an alias for `/{module}/{default}`.
- **Projection Flyout** — secondary navigation that exposes projections without replacing the module workspace; flyout links resolve to `/{module}/{tab}`.
- **Urgency ordering** — home-directory sort: ready modules first, then descending actionable badge count, then module name (ordinal).
- **Command** — an operation type annotated for generated command form, lifecycle state, registration, and MCP tool exposure.
- **Command Target Identity** — the immutable row identity snapshot captured before dispatch from `[CommandTarget]`, a typed `ICommandTargetIdentityProvider<TCommand>`, or `SameAsSource`; the only source for automatic fresh-row marking.
- **Source Generator** — `Hexalith.FrontComposer.SourceTools`, the Roslyn incremental generator that emits FrontComposer artifacts.
- **Generated Output** — files emitted under `obj/{Config}/{TFM}/generated/HexalithFrontComposer/`; the path is a public contract with the compatibility window stated in §11.
- **Domain Manifest** — generated registration data that describes a bounded context's projections, commands, routes, policies, and MCP descriptors.
- **FrontComposer Shell** — the Blazor runtime frame that composes navigation, layout, projection views, command forms, settings, lifecycle status, and EventStore clients.
- **EventStore** — Hexalith.EventStore, the command/query/projection backend the Shell talks to over HTTP and SignalR; its runtime identity is governed by D-12.
- **MCP Surface** — the MCP server tools and resources generated from the same domain descriptors as the human UI.
- **Opaque Failure Token** — the fixed public shape an MCP failure collapses to (`unknown_tool`, `unknown_resource`, `Request failed.`, empty `tools/list`); the exact scope of the guarantee is FR-19.
- **Schema Fingerprint** — deterministic SHA-256 identity over contract material that binds producer and consumers; algorithms are named in §11.
- **Drift Baseline** — checked-in snapshot used to detect generated contract drift.
- **Command Lifecycle** — the state path Idle → Submitting → Acknowledged → Syncing → terminal (Confirmed, Rejected, IdempotentConfirmed, NeedsReview) or degraded (Warning, Degraded).
- **Pre-accept retry / transient retry** — the two retry classes in FR-15: zero retries before the backend acknowledges a command (formerly "Epic 3 retries"), and exactly one retry after `250` ms on a transient transport failure after acknowledgement (formerly "Epic 4 transient retry").
- **Pending Command** — bounded local state for accepted commands awaiting projection evidence or terminal status (`MaxPendingCommandEntries=100`).
- **Customization Levels** — Level-2 projection templates, Level-3 field slots, Level-4 full-view overrides (FR-5).
- **FC-LYT / FC-A11Y / FC-L10N / FC-DOC / FC-CMD / FC-CNC / FC-NIP / FC-IA-1** — the confirmed contracts for layout, accessibility, localization ownership, component docs, command lifecycle, one-at-a-time execution, new-item producer, and Module/Tab information architecture.
- **Fluent UI v5 Policy** — project-wide requirement to use FrontComposer or Fluent UI Blazor v5 components and Fluent 2 tokens for interactive UI.
- **Governed Release** — a release produced by the Hexalith.Builds reusable release workflow through exact-SHA operator `workflow_dispatch` and protected production-environment approval, whose candidate bytes are classified by a `hexalith.release-evidence.v3` manifest before any NuGet or GitHub side effect.
- **Publication Authorization** — `publish_authorized=true` in the classified manifest for one exact candidate, granted per release by the governed-release controls; distinct from document approval (D-9) and from the readiness milestone (§0.1).
- **REL-1…REL-5 / REL-AI-1 / GOV-1 / BUILD-REL-1** — release-governance work items: REL-* delivered the governed-release model, REL-AI-1 is the post-publication ledger sign-off action, GOV-1 is shared-catalog compatibility and dependency provenance, BUILD-REL-1 is the upstream Hexalith.Builds contract.
- **H1–H12 / M-series** — finding IDs of `_bmad-output/project-docs/architecture-quality-review-2026-07-04.md`.
- **AD-n** — architecture decisions of the GOV-1 spine, `_bmad-output/planning-artifacts/architecture/architecture-gov-1-2026-07-19/ARCHITECTURE-SPINE.md` (AD-13 CI handoff, AD-15 Release handoff, AD-16 upstream Builds seam).
- **DW-n** — rows of `_bmad-output/implementation-artifacts/deferred-work.md`.
- **E9-AI-n** — Epic 9 action items in `_bmad-output/implementation-artifacts/sprint-status.yaml`.
- **G-n / OI-n / D-n / SM-n** — this document's gates (§0.1), open items (§12.2), decisions (§12), and success metrics (§9). All identifier families in this PRD, including SM numbering, are frozen for cross-reference; new entries are appended, never renumbered.

## 4. Product Form Factor

FrontComposer is a developer product distributed as NuGet packages and a `frontcomposer` .NET tool. Candidates are published unsigned by the author and repository-signed by NuGet.org; author signing and RFC 3161 timestamping are deliberately not release requirements (REL-5, 2026-08-04). It includes a source-generator/analyzer, Blazor component library, MCP server library, CLI, Testing package, and documentation/skill corpus. It is not a hosted SaaS. The `Hexalith.FrontComposer.UI` sample host and `Hexalith.FrontComposer.AppHost` Aspire host are reference/e2e hosts and are never published product artifacts. [ASSUMPTION A3: the UI sample host may be built as a local container image for e2e lanes only; no FrontComposer-owned image is published to a registry. Confirmation is OI-9.]

The readiness milestone is judged by package-consumer safety **and** by a named Hexalith domain-module adoption (Tenants, G-6), not by a public web launch funnel.

## 5. Features And Functional Requirements

### 5.0 Requirement Status Map

Status labels are part of each requirement's downstream contract. Three tables, three meanings.

**Table A — evidence still gated by §0.1.** The requirements are delivered; what is open is the evidence or approval named in the gate. No row here reopens implementation unless the gate says so.

| Gated item | Open gate | Pass/fail sentence |
| --- | --- | --- |
| FR-24 ledger and provenance evidence | G-1, G-2 | Ledger rows and dispositions exist for every published tag; the Builds execution pin is accepted and the AD-13/AD-15 proof is cited. |
| FR-29.7 EventStore identity | G-3 | The migration approval to `3.103.0` is recorded with `migrationApprovalClaimed: true`. |
| SM-1 adoption proof (for FR-1, FR-2, FR-7, FR-8) | G-6 | The Tenants bootstrap evidence file exists. |
| SM-4 audit and FR-19 host-enforcement consequences | G-7 | The leak/oracle suite and the `AllowAll*` non-Development check exist and run in the Governance lane. |
| D-9 and D-4 approvals | G-4, G-5 | Dated approval records exist. |

**Table B — regression baselines.** Delivered; the Framework maintainer verifies no regression on every release.

| FR | Note |
| --- | --- |
| FR-1 to FR-12 | Generated projection, shell, grid, freshness, and realtime behavior (FR-12 bounds in NFR-8). |
| FR-13 | Complete: Stories 9.3–9.8 done; live proof 2026-08-27 on candidate `7a573763`. Residuals and the DW-679 non-goal in FR-13/D-4. |
| FR-14 to FR-19a | Command lifecycle and MCP surface, including cross-request lifecycle (Story 11.3). FR-19's two host-enforcement consequences are Table A items until G-7 closes. |
| FR-20 to FR-23 | CLI, Testing, docs. |
| FR-25 | Public contracts, incl. `AnalysisMode=Recommended` active since 2026-08-08 (Story 11.23). |
| FR-26 | Complete with FR-13. |
| FR-29.1 to FR-29.6 | Architecture-review defect classes closed by Stories 11.1–11.5, 11.7, 11.8, 11.11–11.19 and PR #48. |
| FR-30 | Shell tenant scope. |

**Table C — closed decision records.** Retained for traceability; nothing in them re-enters the queue.

| FR | Record |
| --- | --- |
| FR-27 | Epic 10 tooling-governance follow-through, incl. HFCM9002 decision (D-15). |
| FR-28 | Epic 11 route-contract (Story 11.0) and Contracts split (Story 11.8) decisions. |

### 5.1 Source Generation And Contract Vocabulary

**Description:** Adopter developers write Annotated Domain Types. The Source Generator emits Blazor views, command forms, Fluxor state, DI registrations, MCP manifests, diagnostics, and drift material. Generated Output is not hand-edited.

#### FR-1: Generate projection artifacts

For each valid `[Projection]` type, the Source Generator must emit a projection view, Fluxor feature/actions/reducers, and registration artifacts.

**Consequences:**
- A valid projection produces the five-file set `{T}.g.razor.cs`, `{T}Feature.g.cs`, `{T}Actions.g.cs`, `{T}Reducers.g.cs`, `{T}Registration.g.cs` under the public Generated Output path.
- A non-`partial` projection produces HFC1003 and fails under warnings-as-errors.
- Generated projection views handle Loading, Empty, and Data states according to `ProjectionRole`.

#### FR-2: Generate command artifacts

For each valid `[Command]` type, the Source Generator must emit command form, lifecycle, renderer, registration, subscriber, bridge, and optional full-page route artifacts.

**Consequences:**
- A command with no public parameterless constructor fails with HFC1009.
- A command missing `MessageId` fails with HFC1006.
- Full-page density emits a route host; inline and compact densities do not.
- An invalid `[CommandTarget]` declaration fails with HFC1005 (invalid attribute argument); no separate diagnostic ID was allocated for the FC-NIP successor.

#### FR-3: Honor the attribute vocabulary

FrontComposer must support the documented vocabulary: projection roles, bounded contexts, badges, column priority, field groups, empty-state CTA, destructive confirmation, policy requirements, derived fields, icons, relative time, currency, display metadata, defaults, projection templates, and command target declarations.

**Consequences:**
- Unsupported or invalid attribute use emits the HFC diagnostic cataloged in `docs/diagnostics/` (build-time `HFC1001–HFC1070`, migration `HFC0001`, runtime `HFC2xxx`).
- Server-controlled or derived command fields do not render as editable input.
- Projection badge and status metadata remain accessible, not color-only.

#### FR-4: Apply the command density rule

Command form density is determined by non-derivable property count: `Inline` for 0-1, `CompactInline` for 2-4, and `FullPage` for 5 or more.

**Consequences:**
- Derivable fields such as `MessageId`, `CorrelationId`, `TenantId`, `UserId`, timestamps, and `[DerivedFrom]` fields are excluded from the count.
- Density behavior is covered by generator tests and snapshots.
- Density thresholds are changed only through an explicit story/ADR.

#### FR-5: Support safe customization levels

Adopters can override generated projection UI through Level-2 templates, Level-3 field slots, and Level-4 full-view overrides.

**Consequences:**
- Resolution order is deterministic: Level 4, then Level 2, then generated default.
- Level 3 slots compose only when the selected body delegates to generated field/row/section/default renderers.
- HFC1050-HFC1055 cover statically inspectable override accessibility risks.
- Runtime mismatch panels are development-only under DEBUG and `IsDevelopment()`.

#### FR-6: Detect schema and generated-output drift

FrontComposer must bind producer and consumers through Schema Fingerprints and opt-in drift baselines.

**Consequences:**
- Drift detection compares current generated material to checked-in baseline `AdditionalText` files.
- Structural drift emits HFC1065; metadata drift emits HFC1066; baseline validity problems emit HFC1060–HFC1064 and HFC1067–HFC1069.
- Canonical schema material is deterministic and bounded by the drift-baseline size and truncation limits enforced by HFC1063/HFC1068; encoder, sentinel, comparer, and baseline identity are load-bearing.

### 5.2 Shell Adoption And Runtime Frame

**Description:** Adopter apps use the FrontComposer Shell as the operations frame. The shell provides layout, navigation, settings, theme/density persistence, account controls, EventStore clients, and generated content hosting.

#### FR-7: Provide validated DI bootstrap

Adopter apps can wire FrontComposer through `AddHexalithFrontComposerQuickstart()`, optional `AddHexalithDomain<TMarker>()`, and `AddHexalithEventStore(...)`.

**Consequences:**
- Missing foundational quickstart or misordered calls fail at startup with a named error.
- Empty-shell operation is valid when no domain registrations are present.
- Scoped auth, storage, effects, and tenant accessors must not be captured by singleton services.

#### FR-8: Render the shell frame

The FrontComposer Shell must render a complete Blazor application frame with Fluent layout, skip links, providers, header, navigation, content, footer, and keyboard shortcuts.

**Consequences:**
- Adopter layout can reduce to `<FrontComposerShell>@Body</FrontComposerShell>`.
- `Ctrl+,` opens settings, `Ctrl+K` opens the command palette, and `/` focuses page search where that shortcut is enabled.
- The framework-owned account menu is always rendered so adopter header customization cannot remove auth access.
- The hamburger toggle is always visible; on desktop it switches the navigation rail between labelled and icon-only modes.
- Shell chrome follows the Aspire-dashboard language: neutral chrome, accent as thread, compact density, sticky grid headers, lightweight status icons (details in the addendum).

#### FR-9: Manage layout, theme, density, and localized shell strings

The Shell must provide FC-LYT layout modes, shell-owned localized strings, and persisted theme/density preferences.

**Consequences:**
- Full-width is the default layout; constrained layout caps content at a `75rem` max measure.
- Settings changes persist through `IStorageService` (at most `LocalStorageMaxEntries=500`) and update `data-fc-density`; compact projection grids use the exact `32px` row metric from `DataGridDensityMetrics`.
- Shell chrome strings resolve from shell resources; domain strings remain host/domain-owned with no shell fallback.

### 5.3 Projection Operations Experience

**Description:** Operators browse generated projection pages with registry-driven discovery, Fluent DataGrid behavior, accessible status and detail states, and EventStore-backed query/realtime updates.

#### FR-10: Provide registry-driven discovery

The Shell must generate navigation, home directory cards, command palette entries, projection routes, badges, and counts from Domain Manifest data.

**Consequences:**
- Each bounded context appears to operators as one Module with one primary shell entry and one required default Module Tab; primary tab routes use `/{module}/{tab}`.
- The default tab is named after the module plural label (fallback `Overview`); `/{module}` renders the default tab.
- Projection flyouts are secondary navigation, resolve to `/{module}/{tab}`, and never create a new top-level entry.
- Navigation keeps exactly one active item.
- Home directory supports progressive empty/loading/data states and urgency ordering (ready first, then descending actionable count, then name).
- Command palette search is keyboard-accessible, authorization-aware, and debounced at `150` ms.
- Generated command activation uses `/commands/{BoundedContext}/{CommandTypeName}`.

#### FR-11: Render projection grids and states

Generated projection pages must provide filtering, empty/loading states, status indicators, expand-in-row details, column prioritization, slow-query notices, and max-items notices.

**Consequences:**
- Column filters are debounced and resettable.
- Row detail regions remain accessible and announce filter-hidden expanded rows; live regions are used only where the state change is useful and non-noisy.
- Projections with more than 15 columns activate column prioritization.
- The projection state set is Loading, Empty, Data, Stale, Reconnecting, FallbackPolling, SlowQuery (after `SlowQueryThresholdMs=2_000`), and MaxItems (`MaxUnfilteredItems=10_000`, server-side virtualization from `500` rows); each is operator-visible and accessible.
- Status values render as semantic icon-plus-text affordances with tooltip and `aria-label` support; color is never the only signal.

#### FR-12: Maintain projection freshness and realtime behavior

The Shell must query EventStore over HTTP and subscribe to projection changes over SignalR while surfacing reconnect/reconciliation state and recovering when the backend recovers.

**Consequences:**
- Reconnect and fallback-polling states are visible to operators.
- Projection updates do not treat SignalR nudges as proof of command success.
- A long-lived circuit never permanently degrades: reconnect retries are unbounded with jittered exponential backoff capped at `30_000` ms, a closed connection restarts within `10` s, fallback polling runs every `15` s across at most `8` lanes while disconnected, a reconnected notice shows for `3_000` ms, and the subscription resumes automatically on reconnect (Story 11.2).

#### FR-13: Mark fresh rows only through FC-NIP

The product must not infer row-level fresh indicators from projection nudges that lack row identity. FC-NIP owns the row identity payload and producer wiring.

**Consequences:**
- `FcNewItemIndicator` remains a confirmed component.
- Automatic row marking uses only an immutable Command Target Identity captured before dispatch from `[CommandTarget]` with a typed `ICommandTargetIdentityProvider<TCommand>` or a declared `SameAsSource` snapshot. Terminal `Material`, `NoOp`, or `Unknown` classification is independent; unknown identity or materiality suppresses publication.
- **[NON-GOAL for v1.0]** Commands whose target key is allocated server-side cannot declare a pre-dispatch target and therefore produce no fresh-row marker; a typed post-dispatch identity proof is tracked as DW-679. Operators see no indicator for those rows rather than a wrong one.
- Exactly one terminal producer boundary publishes indicators; indicator state is observable by generated grids and scope-safe per tenant and user; publication is atomic first-wins per `ViewKey`/`EntityKey` across distinct message IDs.
- Target-resolution outcomes are observable as EventId 5912 (`CommandFormTargetResolutionFailed`, category only) and 5913 (`CommandFormTargetResolutionSucceeded`, payload-free); the suppression rate `5912 / (5912 + 5913)` is the operator-facing health signal.
- Contracts: `_bmad-output/contracts/fc-nip-row-identity-producer-contract-2026-07-04.md` (base) and `_bmad-output/contracts/fc-nip-command-target-identity-contract-2026-08-12.md` (successor). Composed/live acceptance evidence: `_bmad-output/implementation-artifacts/tests/9-8-live-acceptance.md`.

### 5.4 Command Authoring, Lifecycle, And Safety

**Description:** Operators submit generated command forms and receive lifecycle feedback that distinguishes transport acceptance from projection-confirmed results.

#### FR-14: Submit commands through generated forms

Generated command forms must validate input, parse supported field types, dispatch commands, and preserve form state on retryable pre-accept failures (transport timeout, connection reset, HTTP 5xx before acknowledgement).

**Consequences:**
- Unsupported field types render placeholders rather than breaking the form.
- Nullable numeric fields compile and round-trip culture-aware formatting.
- `MessageId` is generated as a ULID and reused across pre-accept retry attempts.

#### FR-15: Surface command lifecycle states

The Shell must surface Submitting, Acknowledged, Syncing, Confirmed, Rejected, IdempotentConfirmed, NeedsReview, Warning, and Degraded states.

**Consequences:**
- Accepted HTTP transport is not displayed as projection-confirmed success.
- Polling binds to the confirmed EventStore status endpoint.
- Default deterministic budgets are confirming-to-Degraded after `10_000` ms (`TimeoutActionThresholdMs`), status polling every `1_000` ms for at most `120_000` ms (`MaxPendingCommandPollingDurationMs`), zero pre-accept retries, and exactly one transient retry after `250` ms once acknowledged. Configuration changes require focused `FakeTimeProvider` evidence.

#### FR-16: Enforce command safety

Command execution must respect authorization, destructive confirmation, form-abandonment guard, and FC-CNC one-at-a-time execution.

**Consequences:**
- `[RequiresPolicy]` is evaluated before `BeforeSubmit` and again afterward for protected commands.
- The service boundary also enforces authorization through `AuthorizingCommandServiceDecorator`.
- FC-CNC v1 blocks later local submits rather than queueing or batching them, preserves the in-flight command, and presents localized, accessible feedback that a later submit was not queued.
- The form-abandonment guard activates after `FormAbandonmentThresholdSeconds=30` of edits.

### 5.5 MCP Agent Surface

**Description:** The MCP server exposes generated commands and projections to agents using the same domain descriptors while enforcing fail-closed security and schema compatibility.

#### FR-17: Expose generated command tools

Each visible generated command must appear as an MCP tool with descriptor-derived JSON schema and bounded acknowledgement output.

**Consequences:**
- Tools are built dynamically at each `tools/list` and named `{BoundedContext}.{CommandType}.Execute` (namespace-qualified when ambiguous).
- Server-controlled fields (`TenantId`, `UserId`, `MessageId`, `CorrelationId`) cannot be accepted from tool input.
- Command invocation injects tenant/user/message/correlation fields server-side.

#### FR-18: Expose projection and skill resources

The MCP Surface must expose tenant-scoped projection resources and the embedded FrontComposer skill corpus.

**Consequences:**
- Projection resource URIs are `frontcomposer://{bounded-context}/projections/{projection-name}` and match generated descriptors exactly.
- Skill resources (`frontcomposer://skills/{id}`, manifest `frontcomposer://skills/manifest`) are framework-global reference material, are served only from validated `agent-reference` sections, and intentionally bypass `IFrontComposerMcpResourceVisibilityGate` because they carry no tenant data.
- Oversized skill resources fail closed (`response_too_large`) instead of truncating silently.

#### FR-19: Enforce MCP fail-closed admission and compatibility

MCP hosts must register tenant tool and resource visibility gates, negotiate schema fingerprints, and collapse admission failures to the opaque public shapes below. The guarantee is exact, not blanket: **for tools, hidden and absent are indistinguishable for the same caller; for resources, reads of registered-but-hidden resources are indistinguishable from unauthorized reads, while the catalog itself is disclosed and unregistered URIs are answered by the SDK.**

| Request class | Public shape | Guarantee |
| --- | --- | --- |
| `tools/list` with no auth, missing tenant, or catalog failure | Successful `ListToolsResult` with an empty `Tools` collection | An authenticated caller always sees at least the lifecycle tool, so an empty list is a credential-validity signal. Accepted as a residual; part of the OI-2 security sign-off. |
| `tools/call` for an unknown, hidden, unauthorized, tenant-less, or policy-denied tool (including the lifecycle tool) | Structured `category: "unknown_tool"` plus `docsCode: "HFC-MCP-UNKNOWN-TOOL"`, `suggestion`, the caller's own `visibleTools` (possibly `continuation: "visible-list-truncated"`); text `Request failed.` | Byte-identical for hidden and absent for the same caller. |
| `tools/call` for a visible tool with an incompatible client schema | Structured `category: "schema-mismatch"`; no side effect | Distinct from hidden by design (the tool is visible). |
| `resources/list` | The static generated descriptor catalog (URI, name, title, description of every projection and skill resource), identical for every caller | Not tenant-filtered and not gated. Discloses bounded-context and projection names. Accepted only together with host authentication (below); security sign-off is OI-2. |
| `resources/read` for a registered projection that is hidden, unauthorized, tenant-less, or policy-denied | Text token `unknown_resource`; hidden takes precedence over schema failures | Indistinguishable across those causes. |
| `resources/read` for an unregistered URI | ModelContextProtocol SDK default not-found error, not `unknown_resource` | Absent is distinguishable from hidden for resources; stated, not hidden. Wire-level test is part of G-7. |
| `resources/read` for a visible projection with an incompatible schema | Distinct schema category (for example `schema-mismatch`) | Distinct by design. |
| Skill resource read failure | `unknown_resource`, `malformed_request`, `canceled`, or `response_too_large` | Skills are global; no tenant guarantee applies. |

**Consequences:**
- Startup throws `InvalidOperationException` naming the missing `IFrontComposerMcpTenantToolGate` or `IFrontComposerMcpResourceVisibilityGate` registration.
- `MapFrontComposerMcp` hosts must place the MCP endpoint behind host authentication (`RequireAuthorization()` or an equivalent host policy); the endpoint itself applies none. Verified by G-7.
- `AllowAllMcpTenantToolGate` and `AllowAllResourceVisibilityGate` are sample/dev escape hatches only and must not be registered in a non-Development host; the enforcement mechanism and its test are OI-3, verified by G-7.
- Schema negotiation kinds `Exact`, `CompatibleAdditive`, and `CompatibleWarning` allow side effects (D-14); `Incompatible`, `UnknownClientVersion`, `UnknownServerBaseline`, `HiddenOrUnknown`, `StaleDescriptor`, `UnsupportedAlgorithm`, `Unavailable`, and `SchemaIntegrityMismatch` block them.
- No public MCP response, log, or exception message echoes the requested tool name, fingerprint material, tenant identifiers, or internal exception text.
- Timing side channels (visible-vs-hidden evaluation cost) are out of scope for the readiness milestone; hidden and unknown tools share the coarse resolution path, and no timing bound is claimed.

#### FR-19a: Preserve command lifecycle across MCP requests

The lifecycle tool `frontcomposer.lifecycle.subscribe` is a polling read contract: an agent that invokes a command in one request must be able to observe its lifecycle in later requests and DI scopes.

**Consequences:**
- Lifecycle state lives in a singleton store with a scoped tracker; command-then-poll across scopes is covered by tests (Story 11.3, done).
- Hidden or unknown lifecycle lookups use the `unknown_tool` shape from FR-19.

### 5.6 CLI, Testing, And Adopter Tooling

**Description:** FrontComposer includes developer tooling that makes generated artifacts inspectable, migratable, and testable by downstream packages.

#### FR-20: Provide `frontcomposer inspect`

The CLI must inspect generated output and diagnostics sidecars and report forms, grids, registrations, manifest entries, warnings, and errors.

**Consequences:**
- Output supports text and JSON using `frontcomposer.cli.inspect.v1`.
- Severity filtering and fail flags have deterministic ordering.
- Absolute paths are rewritten relative to the inspected root in JSON output.

#### FR-21: Provide `frontcomposer migrate`

The CLI must plan and apply allowlisted Roslyn migrations across supported version edges.

**Consequences:**
- Dry-run is default.
- Apply mode is atomic and refuses writes to generated output, submodule roots, symlinked or out-of-root paths.
- JSON output uses `frontcomposer.cli.migrate.v1`.

#### FR-22: Provide adopter testing support

The Testing package must provide a bUnit host, deterministic command/query/projection fakes, evidence capture, redaction, builders, and assertion helpers that cover realistic failure and policy states, not only happy paths.

**Consequences:**
- Public API drift updates `PublicAPI.Shipped.txt` intentionally.
- Evidence output is redacted by default.
- Adopter tests can simulate command success, rejection, timeout/stall, authorization denial, and paging/filter/sort (Story 11.6, done; formerly A1).

#### FR-23: Maintain component and skill documentation

FrontComposer must keep component docs, diagnostic docs, migration docs, and skill-corpus docs synchronized with the generated and runtime surfaces.

**Consequences:**
- Published docs under `docs/` pass the DocFX validation gate when changed.
- Skill-corpus docs satisfy required front matter and snippet/reference validation.
- Generated/scratch planning docs remain outside `docs/`.
- `docs/fluent-ui-v5-contingency.md` still cites an rc.2 pin and is corrected under OI-10.

### 5.7 Package Release And Brownfield Remediation

**Description:** the readiness milestone depends on strict package, public API, release, and remediation quality.

#### FR-24: Publish only evidence-classified package artifacts

FrontComposer must publish only the expected NuGet package set (§11), using exact candidate bytes that were inventory-validated, consumer-validated, checksummed, manifest-bound, and classified as publishable before any NuGet or GitHub Release side effect.

**Consequences:**
- Conventional commits determine the version bump. There is exactly one publication path: exact-SHA operator `workflow_dispatch` of the Hexalith.Builds reusable `domain-release.yml` (pinned in `release.yml` at `4eb33928…`), after a successful exact-source main-push CI run and approval in the protected production environment. That approval is the human pre-publication control. The transitional `workflow_run` path, the caller-side freeze guard, and the `HEXALITH_RELEASE_PUBLISH_ENABLED` variable are retired (REL-5); none is a product control.
- Candidates are published unsigned and repository-signed by NuGet.org; the validated bytes must be identical to the published bytes, and rebuilding or repacking after classification is never equivalent evidence.
- Package inventory, tests, package-consumer validation, symbols, SBOM, checksums, sealed-manifest verification, and `classify-release --require-publishable` all run before publication; `classification=blocked` or `publish_authorized=false` fails the release before any side effect.
- The `hexalith.release-evidence.v3` manifest binds the exact published bytes, the complete depth-1/2 `hexalith.dependency-graph.v1`, the immutable dependency policy, and authenticated CI/release workflow provenance; legacy manifests are audit-only. Manifest mechanics are in the addendum and `architecture.md`.
- Durable evidence is attached to the GitHub Release; a 30-day Actions artifact is supplemental. Every published tag must have a ledger row. This consequence is currently breached for v4.2.0, v4.3.0, and v4.4.0 (G-1). Historical releases with blocked or invalid evidence (v3.2.1, v3.2.2, v4.0.0, v4.0.1) stay non-compliant.
- REL-AI-1 is post-publication audit closure — the Release Owner confirms published hashes and signs the ledger row — not a pre-publication control and not a pre-condition of any release. The circular reading in the 2026-08-12 text is withdrawn.
- The shared-workflow dependency is Hexalith.Builds `BUILD-REL-1`. The owner-accepted revision is `a8a50859…` (issue 17 reopened 2026-08-08); FrontComposer's execution pin has since advanced (`3f0e3595…` on 2026-08-11, then `4eb33928…`). Acceptance of the execution pin and the AD-13/AD-15 proof citation are G-2.

#### FR-25: Preserve public contracts and deprecation paths

Public API baselines, schema contracts, CLI JSON schemas, generated-output paths, HFC diagnostics, and analyzer policy must evolve intentionally.

**Consequences:**
- Breaking public-surface changes update baselines, docs, and migration/deprecation plans. The ApiCompat baseline constant is `PUBLISHED_BASELINE_VERSION = "4.3.0"` (`eng/release_compatibility.py`) while v4.4.0 is already tagged; advancing the baseline after each publication is OI-11.
- New diagnostics use the documented HFC bands and XML docs.
- Schema canonicalization changes are treated as baseline-invalidating.
- `AnalysisMode=Recommended` with `TreatWarningsAsErrors=true` and built-in analyzers only is the repository baseline (Stories 11.20–11.23, done); exceptions are narrow, owner-bound entries in the policy ledger.

#### FR-26: Complete FC-NIP producer wiring

FrontComposer composes row-level fresh-item producer and consumer only through the approved and successor-amended FC-NIP contract.

**Consequences:**
- Fresh-row indicators are never inferred from SignalR nudges or unrelated projection refreshes.
- The base source is FrontComposer-owned pending-command row metadata; the successor requires explicit command-to-projection declaration, typed target-provider resolution or `SameAsSource`, pre-dispatch capture, and independent typed terminal materiality. EventStore status remains a lifecycle source keyed by `MessageId`, not a row identity source.
- Composition is complete: Stories 9.3–9.8 are done and the 9.8 live proof is the acceptance artifact. Residuals: DW-679 (server-allocated keys; the FR-13 non-goal) and the 9.8 deferred AppHost-fallback rebuild items are deferred work, not gates. DW-671–DW-678 and DW-680–DW-681 closed 2026-08-27.

#### FR-27: Preserve tooling-governance follow-through

FrontComposer preserves the completed Epic 10 tooling-governance outcomes for evidence, labels, CLI parity, migration-emission decisioning, and Testing redaction.

**Consequences:**
- Evidence reconciliation proves that CLI, diagnostics, migration, Testing, and documentation artifacts agree on current labels and outcomes.
- HFCM9002 production emission is **not approved**; it stays synthetic/manual sidecar evidence only (D-15).
- Testing redaction coverage proves evidence output does not leak support-sensitive data.

#### FR-28: Govern Epic 11 decision gates

Epic 11 delivery follows the recorded route-contract and Contracts split decisions.

**Consequences:**
- Story 11.0 is the closed canonical generated-command-route decision record.
- Story 11.8 is the closed Contracts kernel split, compatibility, public API, and migration decision record.
- Completed Stories 11.7 and 11.11–11.14 retain their decision trace; neither decision record re-enters the queue.

#### FR-29: Remediate architecture-review release risks

Each defect class named by the 2026-07-04 architecture quality review must have an operator-visible or adopter-visible outcome, a fail-closed rule, and a verification artifact. Rows are the requirement; story IDs are references.

| Row | Outcome required | Review IDs | Story | State |
| --- | --- | --- | --- | --- |
| FR-29.1 Auth token lifecycle | Sign-out invalidates tokens; the token store evicts; no singleton captures scoped auth. | H2, M1 | 11.1 | Done |
| FR-29.2 Projection realtime resilience | Bounds in FR-12/NFR-8; a circuit never stays degraded after the backend recovers. | H6 | 11.2 | Done |
| FR-29.3 MCP cross-request lifecycle | FR-19a. | H4 | 11.3 | Done |
| FR-29.4 Return-path and storage-key safety | `ReturnPathValidator` and `StorageKeys` have direct tests; one key builder. | H7, H9 | 11.4 | Done |
| FR-29.5 Generated-code hygiene | One escaping implementation (`GeneratedLiteral`), one slug algorithm, linked stylesheets, nullable numerics compile. | H1, H3, H5, H8, H10 | PR #48, 11.0, 11.5, 11.7, 11.16 | Done |
| FR-29.6 Contracts boundary and enforcement | netstandard2.0-clean `Contracts`; `Contracts.UI` for Blazor/Fluent; no blanket NuGet-audit `NoWarn`; analyzer activation; exclusive logging ownership per site. | H11, H12, M-series | 11.8, 11.11–11.19, 11.20–11.23 | Done |
| FR-29.7 EventStore runtime identity | The identity FrontComposer builds and tests against equals the owner-approved tuple, with live Pact provider evidence at that identity. | — | 11.24, Pact reconciliation specs | Story done 2026-08-29; live evidence at `3.103.0` passed 2026-09-08; approval record open (G-3, D-12) |

**Consequences:**
- Acceptance criteria for any new Epic 11 implementation story use Given/When/Then form before ready-for-dev.
- Post-11.18 logging hardening (for example `spec-low-logging-governance-hardening.md`, 2026-09-06) is NFR-10 follow-through, not a reopened FR-29 row.

#### FR-30: Enforce shell tenant scope

The Shell must scope every operator-facing read, subscription, count, and persisted preference to the resolved tenant and user, and fail closed when tenant context is missing.

**Consequences:**
- Projection queries resolve tenant through `IFrontComposerTenantContextAccessor`; a missing or stale tenant raises `TenantContextException` instead of querying.
- SignalR projection groups are keyed by `(projectionType, tenantId, scope)`; a stale tenant marks the group `Blocked`.
- Storage keys are `{tenant}:{user}:{feature}[:{discriminator}]`; persistence is skipped (HFC2105) when tenant or user is missing — there is no default tenant.
- Badge counts and home-directory counts read `0` and reconciliation is skipped without a tenant; `StorageReady` is not dispatched without tenant and user.
- The operator-visible result of missing tenant context is an explicit fail-closed state, never empty-looking data.

## 6. Cross-Cutting Non-Functional Requirements

- **NFR-1 Build strictness:** .NET 10 (SDK `10.0.400`, `rollForward: latestPatch`), `.slnx` only, nullable enabled, centralized package versions, `TreatWarningsAsErrors=true`, and `AnalysisMode=Recommended` with built-in analyzers only.
- **NFR-2 Dependency direction:** dependencies point down to Contracts; SourceTools references only Contracts; net10/Fluent-only code in multi-targeted projects is guarded.
- **NFR-3 Accessibility:** generated and hand-authored UI must conform to WCAG 2.2 AA and preserve accessible names, roles, focus, keyboard, live-region, reduced-motion, and forced-colors behavior.
- **NFR-4 Fluent UI governance:** UI uses FrontComposer/Fluent UI Blazor v5 components and Fluent 2 tokens; raw interactive HTML controls and legacy tokens are forbidden except documented carve-outs.
- **NFR-5 Security:** MCP and Shell security fail closed within the exact guarantees of FR-19 and FR-30; server-controlled fields are never client-supplied; return paths, storage keys, tenant/user scope, auth state, and API keys have direct tests.
- **NFR-6 Privacy and support safety:** UI, logs, telemetry, MCP responses, evidence, and snapshots must not expose raw tokens, JWT payloads, raw EventStore metadata, stack traces, raw event payloads, or unrestricted PII.
- **NFR-7 Schema determinism:** canonical schema material, fingerprint algorithms, baseline identity, and provenance validation are load-bearing public contracts.
- **NFR-8 Reliability:** command lifecycle budgets are FR-15's (`10_000` ms to Degraded, `1_000` ms polling for at most `120_000` ms, one `250` ms transient retry); projection realtime bounds are FR-12's (jittered exponential reconnect capped at `30_000` ms, unbounded attempts, `10` s closed-restart, `15` s fallback polling over at most `8` lanes, `3_000` ms reconnected notice). Degraded, reconnecting, and fallback states are visible within those budgets, recover when the backend recovers, and never convert a nudge or HTTP acceptance into confirmed success.
- **NFR-9 Performance:** palette scoring stays inside the `PaletteScorerBench` thresholds (`tests/Hexalith.FrontComposer.Shell.Tests.Bench`), generated rendering inside `RazorEmitterPerformanceTests` (`tests/Hexalith.FrontComposer.SourceTools.Tests/Emitters`), and cache-backed hot paths inside the `FcShellOptions` caps (`MaxPendingCommandEntries=100`, `MaxProjectionFallbackPollingLanes=8`, `LocalStorageMaxEntries=500`, `MaxUnfilteredItems=10_000`); any threshold change requires benchmark evidence and release-owner approval.
- **NFR-10 Observability:** FrontComposer uses `FrontComposerActivitySource`, source-generated `LoggerMessage` sites bound after `IsEnabled`, and sanitized structured logs for operator-relevant failure paths (including EventIds 5912/5913), with tests or snapshots proving tokens, JWT payloads, raw EventStore metadata, raw event payloads, stack traces, and unrestricted PII are absent.
- **NFR-11 Testing:** the milestone's mandatory lanes are: solution-level default lane with `DiffEngine_Disabled=true`; Governance (`FluentConformanceTests`, `InfrastructureGovernanceTests`, drift tests, and the G-7 audit suite once it exists); Contract; snapshot verification; PublicAPI baselines; ApiCompat against the published baseline; Pact consumer checks plus live-compatibility Pact provider evidence for the approved EventStore identity (D-12); property tests where configured; DocFX docs validation; e2e accessibility and visual lanes for changed UI surfaces; the Epic 9 live-proof lane for FC-NIP changes. Test projects run individually; the `.slnx` is for restore/build.
- **NFR-12 Release evidence:** a valid sealed `hexalith.release-evidence.v3` manifest with `publish_authorized=true`, exact package inventory, consumer validation, symbols, SBOM, and checksums of the exact candidate bytes are blocking pre-publication requirements. Author signing and RFC 3161 timestamps are not requirements; NuGet.org repository signing is the published-signature model. Mutable or self-recorded workflow coordinates cannot authorize publication.
- **NFR-13 Dependency governance:** compatibility is established from versioned semantic shared-catalog profiles and affected-module standalone Release/NuGet restore/build evidence, never from historical commit or fingerprint allowlists. `hexalith.dependency-graph.v1` is exactly depth 1–2; collection never recursively initializes nested submodules and fails closed above the ratified ceilings recorded in the addendum and `eng/dependency-graph-policy.json`.

## 7. Constraints And Dependencies

- **Runtime and framework:** .NET 10, C# latest, Blazor, Fluxor, Roslyn, ModelContextProtocol SDK, SignalR, OIDC, NUlid — all versions pinned centrally by the selected Hexalith.Builds catalog (`references/Hexalith.Builds/Props/Directory.Packages.props` at gitlink `35c3d1e5…`; currently Fluent UI Blazor `5.0.0-rc.5-26219.1`, Roslyn `5.9.0`, Fluxor `6.11.0`, ModelContextProtocol `2.2.0`). The Fluent RC posture is governed by D-13.
- **Packable target frameworks:** `Contracts` and `Schema` multi-target `net10.0;netstandard2.0`; `SourceTools` is `netstandard2.0`; `Contracts.UI`, `Shell`, `Mcp`, `Testing`, and `Cli` are `net10.0`.
- **External systems:** Hexalith.EventStore for command/query/projection backend at the D-12 identity; Hexalith.Tenants as the obligated first adopter (G-6); other Hexalith domain modules as adopters.
- **Repository policy:** root-declared submodules under `references/` only; never recursive submodule initialization; never modify submodule files without explicit approval.
- **Dependency policy:** one closed, versioned FrontComposer-owned policy (`eng/dependency-graph-policy.json`) defines trusted repository identities/paths, semantic profiles, affected-module dispositions, evaluator authorizations, and limits; candidate policy changes cannot authorize themselves (mechanics in the addendum).
- **Published docs:** `docs/` is a CI-gated DocFX site and not scratch space.
- **Generated output:** generated files are not hand-edited; changes flow through SourceTools or Annotated Domain Types.

## 8. MVP And Milestone Scope

### 8.1 Existing Baseline In Scope

- Shell foundation, bootstrap validation, layout, accessibility, localization, docs, settings, theme, density.
- Read-only projection experience: navigation, home, palette, generated projection rendering, DataGrid states, filtering, detail, realtime update handling with recovery.
- Command authoring and lifecycle: generated forms, density, pending identity, polling, budgets, safety, authorization, destructive confirmation, abandonment guard, FC-CNC.
- Row-level fresh-item indicators through FC-NIP command target identity.
- MCP surface: generated command tools, projection resources, skill corpus, fail-closed gates, schema negotiation, cross-request lifecycle.
- Customization levels and override diagnostics.
- CLI inspect/migrate, drift detection, Testing package with failure-state fakes, public API baselines.
- Aspire-grade visual refresh and Fluent governance policies.

### 8.2 Readiness Program Status (sprint-status truth, 2026-08-29)

- **Epic 9:** Stories 9.1–9.8 and the retrospective are done; the 9.8 live proof passed 2026-08-27. Sprint bookkeeping (`epic-9: in-progress`, E9-AI-1…6 open) lags the story files and is closed by G-5.
- **Epic 10:** done; tooling-governance and Testing-redaction outcomes remain reusable evidence.
- **Epic 11:** Stories 11.0–11.9 and 11.11–11.24 are done (there is no 11.10; it was split into 11.11–11.14). Analyzer activation phases 11.20–11.23 landed (11.23 done 2026-08-08, `AnalysisMode=Recommended` active); 11.24 EventStore identity adoption is done (2026-08-29). `sprint-status.yaml` still shows `epic-11: in-progress` pending epic-level closure, and `epics.md` still shows the 2026-07-17 workstream table and calls 11.24 "blocked backlog"; both are superseded by this section (OI-6).
- **Release governance:** REL-1…REL-5 done; REL-AI-1 open (G-1); GOV-1 local work done, external AD-16 acceptance and proof citation open (G-2).

### 8.3 Out Of Scope For The Milestone

- Building rich `<AuditTimeline>` or `<ConsequencePreview>` components; the approved fallbacks (inline lifecycle status via `FcLifecycleWrapper` and the destructive-confirmation dialog) stand per readiness-request item AR10 (`epics.md`).
- Fresh-row markers for commands with server-allocated target keys (FR-13 non-goal; DW-679).
- Replacing EventStore as the backend integration model.
- Non-Blazor/mobile/native shell surfaces.
- General-purpose no-code CRUD builder behavior.
- Hand-authored domain-specific page bodies for Tenants, Parties, or EventStore Admin beyond what the FrontComposer framework must support.
- Recursive or nested submodule management.
- Author-signed or RFC 3161-timestamped packages (deliberately dropped, REL-5).
- Publishing FrontComposer-owned container images.
- MCP timing-side-channel bounds (FR-19).

## 9. Success Metrics

Each metric names its evidence and its state. "Tests exist" or "workflow green" is never sufficient on its own (SM-C4). SM numbering is frozen (§3); SM-5 and SM-6 are secondary by design.

**Primary**

- **SM-1: Adopter bootstrap success** — Hexalith.Tenants boots through the documented three-call path and renders at least one generated projection and one generated command without bespoke framework plumbing. **Evidence:** `_bmad-output/implementation-artifacts/tests/tenants-bootstrap-acceptance.md` (reserved path). **State: unmet** (G-6). Validates FR-1, FR-2, FR-7, FR-8.
- **SM-2: Release ledger integrity** — every published tag has a ledger row whose `hexalith.release-evidence.v3` manifest records `publish_authorized=true`, matching published byte hashes, inventory/consumer/SBOM/checksum results, and Release Owner sign-off. **Evidence:** `rel-ai-1-release-evidence-ledger.md`. **State: unmet** — v4.1.1 pending sign-off; v4.2.0–v4.4.0 have no rows (G-1). Validates FR-24, FR-25, NFR-12.
- **SM-2a: Dependency provenance** — every publish-capable release seals and live-verifies the complete depth-1/2 `hexalith.dependency-graph.v1`, immutable active policy, and authenticated CI→Release handoff; compatible pointer advances pass semantic-profile and affected-module gates without allowlist edits. **Evidence:** `eng/dependency-graph-policy.json`; v4.2.0 Release/Release-Evidence run IDs. **State: local proof done; acceptance and run citation open** (G-2). Validates FR-24, NFR-13.
- **SM-3: Contract drift visibility** — intentional generator or schema changes update baselines, diagnostics, migration/deprecation artifacts, or release notes; accidental drift is caught by HFC1065/HFC1066, snapshots, PublicAPI, or ApiCompat before release. **Evidence:** `tests/Hexalith.FrontComposer.SourceTools.Tests/Drift/`, `eng/release_compatibility.py` lane. **State: met on the current lanes; baseline lag noted (OI-11).** Validates FR-6, FR-20, FR-21, FR-25.
- **SM-4: MCP leak/oracle audit** — an independent suite proves, for every request class in FR-19, exactly the guarantee stated there: byte-identical hidden-vs-absent tool shapes per caller; no list-vs-call or skill-vs-projection oracle beyond the disclosed catalog; an unregistered URI through SDK dispatch behaves as stated; no response or log echoes tool names, fingerprints, tenant identifiers, or exception text; `AllowAll*` gates absent from non-Development hosts; endpoint authentication present. **Evidence:** the G-7 test class (to be named). **State: unmet** — admission/taxonomy/redaction tests exist (`ToolAdmissionTests`, `ProjectionReaderTaxonomyTests`, `SchemaNegotiationPrecedenceMatrixTests`, `AuthRedactionStressTests`); the audit suite does not (G-7). Validates FR-17, FR-18, FR-19, FR-19a.
- **SM-7: Operator freshness trust** — the composed/live Epic 9 lane proves that a fresh-row indicator appears for rows the operator's own command materially changed, is never produced by a nudge, and clears per tenant/user; suppression rate `5912/(5912+5913)` is observable. **Evidence:** `tests/9-8-live-acceptance.md` (candidate `7a573763`, 2026-08-27). **State: met, excluding server-allocated-key commands (FR-13 non-goal).** Validates FR-13, FR-26.
- **SM-8: Projection recovery** — after a forced SignalR disconnect the shell shows Reconnecting then FallbackPolling within one polling interval, and resumes live updates with a reconnected notice once the hub returns, with no permanently degraded circuit. **Evidence:** `spec-11-2-projection-realtime-resilience.md` acceptance and the Shell.Tests realtime suites it names. **State: met.** Validates FR-12, NFR-8.
- **SM-9: Architecture-review closure** — every H1–H12 row in FR-29 has a done story and a regression test or governance check. **Evidence:** FR-29 table; `sprint-status.yaml` story keys 11-1…11-24. **State: H1–H12 closed; FR-29.7 approval record open** (G-3). Validates FR-29.
- **SM-10: Command outcome trust** — no generated form ever renders Confirmed on transport acceptance alone; a stalled confirmation lands in Degraded at `10_000` ms; one-at-a-time execution blocks a second submit with accessible feedback. **Evidence:** `FakeTimeProvider` lifecycle tests and FC-CNC tests in Shell.Tests; Story 11.6 harness failure modes. **State: met.** Validates FR-15, FR-16.

**Secondary**

- **SM-5: Testing harness usefulness** — adopter tests simulate command success, rejection, timeout/stall, authorization denial, paging/filter/sort, and redacted evidence using the Testing package. **Evidence:** `11-6-testing-harness-failure-modes.md`; `Hexalith.FrontComposer.Testing` `PublicAPI.Shipped.txt`. **State: met.** Validates FR-22.
- **SM-6: UX governance stability** — the Governance lane and e2e accessibility/visual lanes report zero new raw interactive controls, legacy Fluent tokens, unlinked CSS, dead scoped-CSS patterns, or accessibility-critical regressions. **Evidence:** `FluentConformanceTests`, `InfrastructureGovernanceTests`, e2e a11y/visual lanes. **State: met on the current main; re-checked per release.** Validates FR-8, FR-11, NFR-3, NFR-4.

**Counter-metrics**

- **SM-C2: Visual polish cannot outrank contract safety.** UI refinement must not bypass accessibility, public API, or package-consumer constraints.
- **SM-C4: A green workflow is not release success.** A successful evidence workflow with `classification=blocked` or `publish_authorized=false` (observed for v3.2.2) counts as a failed release; CI lane greenness never substitutes for the classified manifest, the ledger row, or the leak/oracle audit.
- **SM-C5: Backfilled ledger rows without downloaded-byte verification do not close G-1.** A row that records a tag but not the verified published hashes and a disposition is bookkeeping, not evidence.
- SM-C1 and SM-C3 (generated-file count, CLI output volume) are retired; they guarded pressures the program never exhibited.

## 10. Risks And Mitigations

- **Risk: planning artifacts drift behind delivery.** This PRD was stale by four weeks on 2026-09-08, and `epics.md` still carries a 2026-07-17 table. Mitigation: §8.2 is restated from `sprint-status.yaml` on every update; OI-6 syncs `epics.md`; readiness reruns after each correction batch.
- **Risk: the milestone table is mistaken for a publication switch.** 4.x releases ship weekly under the governed-release controls regardless of §0.1. Mitigation: §0 says so; §0.1 is named "milestone gates"; frontmatter carries `v1_readiness_milestone_reached`, not a publication flag; D-9 cannot read "Blocks: None" while §0.1 has open rows.
- **Risk: published tags outrun the evidence ledger.** v4.2.0–v4.4.0 are tagged while the ledger still says publication is unauthorized. Mitigation: G-1 requires a dated disposition per release and correction of the stale sentences; SM-C5 forbids paper backfill.
- **Risk: EventStore identity approval is a formality.** The approver role may be the same person as the FrontComposer maintainer. Mitigation: D-12 requires the record to say so; live Pact evidence at the built identity is already captured, so the record is the only remaining act.
- **Risk: MCP "fail-closed" is read as an isolation guarantee.** Mitigation: FR-19 states the exact guarantee per request class including what is disclosed; G-7 audits it; OI-2 requires a security sign-off by someone other than the author.
- **Risk: Fluent UI RC breaks under a stable release.** Mitigation: D-13 dated exception with catalog-profile control and `FluentConformanceTests` plus e2e lanes as the regression pack; a Fluent v5 GA triggers a D-13 re-decision.
- **Risk: gitlink identity is confused with shared-catalog compatibility.** Mitigation: semantic profiles plus affected-module proof (NFR-13); exact SHAs are provenance only; G-2 names one SHA per purpose.
- **Risk: UX requirements remain too compact for visual stories.** Mitigation: qualitative UX rules were lifted into FR-8/FR-9/FR-10/FR-11; `ux-design.md` stays the traceability artifact; story-local design notes are required where layout choices are not captured.

## 11. API Contracts / Public Surface

**Package inventory** (`eng/release-package-inventory.json` is the executable authority):

| Package ID | Packable | TFM | Public API baseline | Breaking-change policy |
| --- | --- | --- | --- | --- |
| `Hexalith.FrontComposer.Contracts` | yes | `net10.0;netstandard2.0` | ApiCompat vs published baseline | Major bump + migration notes |
| `Hexalith.FrontComposer.Contracts.UI` | yes | `net10.0` | `PublicAPI.Shipped.txt` (baseline override) | Major bump + migration notes |
| `Hexalith.FrontComposer.Schema` | yes | `net10.0;netstandard2.0` | ApiCompat | Canonicalization changes invalidate baselines (NFR-7) |
| `Hexalith.FrontComposer.SourceTools` | yes | `netstandard2.0` | ApiCompat; generated-output snapshots | Diagnostic/emit changes need snapshot + docs update |
| `Hexalith.FrontComposer.Shell` | yes | `net10.0` | ApiCompat; `PublicAPI.FcTbl.Shipped.txt` for the table contract | Major bump + migration notes |
| `Hexalith.FrontComposer.Mcp` | yes | `net10.0` | ApiCompat | Wire tokens and tool names in this section are frozen for v1 |
| `Hexalith.FrontComposer.Testing` | yes | `net10.0` | `PublicAPI.Shipped.txt` | Major bump + migration notes |
| `Hexalith.FrontComposer.Cli` | yes (tool) | `net10.0` | JSON schemas below; excluded from library ApiCompat | Schema version suffix bump |
| `Hexalith.FrontComposer.UI` | no | `net10.0` | — | Sample host; never published (A3) |
| `Hexalith.FrontComposer.AppHost` | no | `net10.0` | — | Aspire reference host; never published |

**Other public contracts:**

- Source-generator input attributes (including `[CommandTarget]`) and the Generated Output path `obj/{Config}/{TFM}/generated/HexalithFrontComposer/`. The path may change only with a major version and a `frontcomposer migrate` edge; the supported machine-readable surface is the inspect/migrate JSON, not the folder layout.
- HFC diagnostics: build-time `HFC1001–HFC1070`, migration `HFC0001`, runtime `HFC2xxx`, migration tooling `HFCM9xxx`; all documented under `docs/diagnostics/`.
- CLI JSON schemas `frontcomposer.cli.inspect.v1` and `frontcomposer.cli.migrate.v1`.
- MCP identifiers: tool name `{BoundedContext}.{CommandType}.Execute`, lifecycle tool `frontcomposer.lifecycle.subscribe`, resource URIs `frontcomposer://{bounded-context}/projections/{projection-name}` and `frontcomposer://skills/{id}`; opaque tokens and their exact guarantee per FR-19.
- Schema fingerprint algorithms `frontcomposer.schema.sha256.canonical-json.v1` and `frontcomposer.schema.sha256.v1.sourcetools-blob`.
- Release evidence manifest `hexalith.release-evidence.v3` and dependency graph `hexalith.dependency-graph.v1`.
- `_bmad-output/project-docs/api-contracts.md` (2026-06-02) is superseded by `_bmad-output/contracts/*` and `architecture.md`; it is provenance only. `LEGACY-FR-*` / `LEGACY-NFR-*` identifiers in `epics.md` are provenance only and must keep their prefix in any new trace.

## 12. Decision And Gate Register

| ID | Decision or gate | Owner | Default / current state | Blocks |
| --- | --- | --- | --- | --- |
| D-1 | Canonical PRD path | Product Owner | Amended 2026-09-08: `_bmad-output/planning-artifacts/prd.md` is the only source of record; `prd-addendum-2026-09-08.md` is its addendum; the 2026-07-05 run copy is archived and immutable. | No §0.1 gate. |
| D-2 | Architecture and UX discovery | Product Owner | Resolved: `architecture.md` and `ux-design.md` are canonical planning sources and the overflow homes for mechanism and visual rules; `project-docs` is provenance. | No §0.1 gate. |
| D-3 | Generated command route family | Product + Architecture | Resolved 2026-07-05: `/commands/{BoundedContext}/{CommandTypeName}`; contract `_bmad-output/contracts/fc-route-generated-command-route-contract-2026-07-05.md`. | No §0.1 gate; regression evidence only. |
| D-4 | FC-NIP row identity payload source | Product + Architecture | Decision recorded 2026-08-12 (successor contract); composition delivered by Stories 9.3–9.8 with live proof 2026-08-27. Non-goal: server-allocated keys (DW-679). Residual: 9.8 deferred AppHost-fallback rebuild. | G-5 (approval record). |
| D-5 | Contracts kernel split release posture | Architecture + PM | Resolved and delivered (Stories 11.8, 11.11–11.14). | No §0.1 gate; regression evidence only. |
| D-6 | FR-24 release model and ownership | Release Owner | Amended 2026-09-08 to delivery truth: unsigned candidates repository-signed by NuGet.org, `hexalith.release-evidence.v3`, exact-SHA `workflow_dispatch` plus protected-environment approval as the only publication path; `workflow_run`, caller freeze guard, and `HEXALITH_RELEASE_PUBLISH_ENABLED` retired (REL-5). Author signing/RFC 3161 dropped 2026-08-04. REL-AI-1 is post-publication audit closure. Stale "publication remains unauthorized" sentences in the ledger and sprint-status are corrected under G-1. | G-1. |
| D-7 | Success metric targets and adopter fallback | Product Owner + Release Owner | Amended 2026-09-08: §9 states each metric's evidence and state. **Open sub-decision:** if Tenants cannot produce the G-6 evidence, Product chooses between Parties as substitute adopter and holding the milestone; no default. | G-6. |
| D-8 | Standalone UX spec need | Product + UX | Resolved: `ux-design.md` is sufficient; qualitative rules lifted into FR-8–FR-11; story-local design notes where needed. | No §0.1 gate. |
| D-9 | PRD status approval | Product Owner | Resolved 2026-07-05 for D-1…D-8. **Re-approval pending** for the 2026-09-08 register. Document approval never authorizes publication or the milestone. | G-4. |
| D-10 | Built-in analyzer target and activation | Architecture + Product + Release Owner | Resolved 2026-07-16 and **delivered**: `AnalysisMode=Recommended` active since Story 11.23 (2026-08-08). | No §0.1 gate. |
| D-11 | Shared-catalog compatibility and dependency provenance | Architecture + Product + Release Owner | Ratified 2026-07-19; corrected 2026-09-08. History of the Hexalith.Builds identity: issue 17 closed 2026-07-20 without a qualifying revision; reopened 2026-08-08 with owner-accepted revision `a8a50859…`; FrontComposer execution pin advanced to `3f0e3595…` (2026-08-11) and then `4eb33928…` (current `release.yml`); gitlink is `35c3d1e5…`. `evaluator_authorizations` are populated. Local GOV-1 work is done; the GOV-1 story body and addendum checklist that say otherwise are stale. `eng/dependency-graph-policy.json` is the executable authority. Contract: `_bmad-output/contracts/shared-catalog-dependency-governance-2026-07-19.md` (frontmatter and the g2 request frontmatter still say "revision pending" — OI-7). | G-2. |
| D-12 | EventStore runtime identity | EventStore maintainer (approver) + FrontComposer maintainer + Release Owner | New 2026-09-08. Owner-approved tuple recorded by Story 11.24: source `bb94d93e…`, package `3.91.1`, Builds `a8a50859…`; its `approvalRecord` points at a FrontComposer CI-repair spec, not an EventStore-maintainer decision. Built identity is `3.103.0` / `059f6a89…` / Builds `35c3d1e5…`; live-compatibility Pact provider evidence at that identity passed 2026-09-08. **Decision:** the milestone requires a dated migration approval to the built identity (`migrationApprovalClaimed: true`) signed by the EventStore maintainer role; if that role is held by the FrontComposer maintainer, the record must state that the approval is FrontComposer-owned. Rollback to `3.91.1` is rejected (cost: two gitlinks and twelve minor versions of Pact rework for no consumer benefit). | G-3. |
| D-13 | Fluent UI Blazor v5 RC posture | Product + Architecture | New 2026-09-08. RC pins are accepted for the milestone because the version is owned by the Builds catalog profile (`Directory.Packages.props` at gitlink `35c3d1e5…`, currently rc.5); a breaking Fluent update is a FrontComposer major with `FluentConformanceTests` and e2e accessibility/visual lanes as the regression pack — that pack detects breakage after a pin change, it does not predict GA changes. A Fluent UI Blazor v5 GA release triggers a re-decision. Product confirmation is OI-4 (no default). | No §0.1 gate. |
| D-14 | MCP schema side-effect classes, permissive gates, resource disclosure | Architecture + Security reviewer | New 2026-09-08: `Exact`, `CompatibleAdditive`, `CompatibleWarning` are approved side-effect-allowed classes; all other kinds block. `AllowAll*` gates are forbidden in non-Development hosts; enforcement is undelivered (OI-3) and therefore gated by G-7, not treated as a baseline. `resources/list` catalog disclosure, the unregistered-URI SDK error, and the `tools/list` credential oracle are disclosed in FR-19 and require a dated sign-off by a security reviewer other than the PRD author (OI-2) — they are not pre-accepted. | G-7 (OI-3); OI-2 sign-off. |
| D-15 | HFCM9002 production emission | Product + Framework maintainer | Resolved 2026-07-05 (Story 10.4): production emission not approved; synthetic/manual sidecar evidence only. Contract: `_bmad-output/contracts/hfcm9002-production-emission-decision-2026-07-05.md`. | No §0.1 gate. |

### 12.1 Question Disposition

**Closed:** D-1, D-2, D-3, D-5, D-8, D-10, D-15; FC-NIP composition (D-4 decision and delivery); analyzer activation; author-signing requirement (dropped); rollback of the EventStore identity (rejected in D-12).

**Routed with a named external owner and date:** Hexalith.Builds `BUILD-REL-1` revision accepted 2026-08-08 (owner: Builds maintainer; FrontComposer acceptance of the execution pin is G-2); EventStore migration approval for the `3.103.0` identity (owner: EventStore maintainer; G-3).

**Awaiting confirmation (answer proposed, not decided):** OI-1 (Epic 9 acceptance record), OI-4 (D-13 Fluent RC posture), OI-9 (A3 sample-host container).

**Accepted residuals:** DW-679 and the 9.8 AppHost-fallback deferred items; MCP timing side channels out of scope. Nothing about `resources/list` or the `tools/list` credential oracle is accepted until OI-2 is signed.

### 12.2 Open Items

No open item carries a default; each names the decision or artifact that closes it.

| ID | Item | Owner | Unblock condition | Gate |
| --- | --- | --- | --- | --- |
| OI-1 | Record Product acceptance of the Story 9.8 proof packet; close `epic-9` and E9-AI-1…6 in `sprint-status.yaml` or rewrite them as residuals. | Product Owner | Dated record plus sprint-status update. | G-5 |
| OI-2 | Security sign-off, by a named reviewer other than the PRD author, of the FR-19 disclosures: static `resources/list` catalog, unregistered-URI SDK error, `tools/list` credential-validity signal, host-authentication requirement. Alternative: a story that gates `resources/list`/`resources/read` through the visibility gate. | Security reviewer | Dated sign-off or story. | G-7 |
| OI-3 | Story for the non-Development ban on `AllowAllMcpTenantToolGate` / `AllowAllResourceVisibilityGate` (startup check or analyzer), the endpoint-authentication check, the SDK-dispatch unregistered-URI test, and the SM-4 audit class; names the test class for G-7. | Framework maintainer | Story done; test class named in G-7. | G-7 |
| OI-4 | Product decision on the D-13 Fluent RC posture. | Product Owner | D-13 marked confirmed or revised. | — |
| OI-5 | Capture the Tenants bootstrap evidence at `_bmad-output/implementation-artifacts/tests/tenants-bootstrap-acceptance.md`; if Tenants cannot, invoke the D-7 fallback decision. | Product Owner + Tenants maintainer | File exists, or D-7 fallback decided. | G-6 |
| OI-6 | Sync `epics.md` (Epic 11 table, Story 11.24 status) and `architecture.md` Epic 11 section with §8.2; close `epic-11` in `sprint-status.yaml`. | Framework maintainer | Documents updated. | — |
| OI-7 | Update `shared-catalog-dependency-governance-2026-07-19.md` and `g2-hexalith-builds-inline-pre-publish-gate-request.md` frontmatter to the accepted revision, note the advanced execution pin, and collapse the GOV-1 story's done/in-progress/open status into one sentence. | Architecture | Documents updated. | G-2 (documentation part) |
| OI-8 | Write ledger rows and dispositions for v4.2.0, v4.3.0, v4.4.0 with downloaded-byte verification (SM-C5); correct the stale "unauthorized" sentences. | Release Owner | Rows and dispositions exist. | G-1 |
| OI-9 | Release Owner confirmation of A3 (UI sample host container is local/e2e only). | Release Owner | Confirmed or corrected in §4/§11. | — |
| OI-10 | Correct `docs/fluent-ui-v5-contingency.md` (cites an rc.2 pin) to the catalog-owned pin. | Framework maintainer | Doc updated. | — |
| OI-11 | Advance `PUBLISHED_BASELINE_VERSION` after each publication (currently `4.3.0` with v4.4.0 tagged) or document the intended one-release lag. | Release Owner | Constant or docs updated. | — |

## 13. Assumptions Index

- **A1 (retired):** the Testing harness must cover realistic failure and policy states. **Disposition:** delivered by Story 11.6; now FR-22 text.
- **A2 (retired):** readiness is judged by package-consumer safety and domain-module adoption. **Disposition:** promoted to §4 with Tenants named as the obligated adopter (G-6).
- **A3 (§4, §11):** the `Hexalith.FrontComposer.UI` sample host may be built as a local container image for e2e lanes only; no FrontComposer-owned image is published. **Disposition:** inferred from `eng/release-package-inventory.json` ("container image", not packable) versus the no-containers product statement; confirmation is OI-9.
- **A4 (§0.1 G-1, FR-25):** tags v4.2.0 and v4.3.0 correspond to published NuGet packages (compatibility gates and the Builds catalog pin `4.3.0` treat them so); v4.4.0 is tagged (2026-09-07) but its NuGet publication is unverified from this repository. **Disposition:** resolved by the G-1 dispositions (OI-8).
