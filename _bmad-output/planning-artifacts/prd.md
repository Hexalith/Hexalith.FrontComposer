---
title: Hexalith.FrontComposer Product Requirements Document
status: final
product_approval: pending-reapproval (D-9 amendment required after the 2026-09-09 reconciliation)
v1_readiness_milestone_reached: false
created: 2026-07-05
updated: 2026-09-09
---

# PRD: Hexalith.FrontComposer

## 0. Document Purpose

This PRD is for Product, Architecture, UX, developer agents, and downstream BMAD story workflows preparing Hexalith.FrontComposer for the v1.0 readiness milestone. It consolidates the delivered product baseline, the completed post-MVP remediation program, and the remaining milestone gates into one product requirements source of record.

- **Single canonical copy.** `_bmad-output/planning-artifacts/prd.md` is the only source of record. The 2026-07-05 BMad run copy is archived and immutable under `_bmad-output/planning-artifacts/archive/prds/prd-frontcomposer-2026-07-05/`. The 2026-09-08 run folder `_bmad-output/planning-artifacts/prds/prd-frontcomposer-2026-09-08/` holds the validation reviews and the decision memlog (`.memlog.md`).
- **Addendum.** Mechanism, rejected alternatives, qualitative UX rules, and the source inventory live in `_bmad-output/planning-artifacts/prd-addendum-2026-09-08.md`. `architecture.md` and `ux-design.md` remain the canonical technical and UX planning sources (D-2).
- **"v1.0 readiness" is a milestone label, not a semantic version and not a publication switch.** Packages are at 4.x (v4.4.0 tagged 2026-09-07). The checked-in caller currently selects the legacy Hexalith.Builds reusable release job and its deny-by-default `HEXALITH_RELEASE_PUBLISH_ENABLED` gate; it does not yet select the intended BUILD-REL-1 governed mode. Protected-environment approval remains a human control, but the 2026-09-09 GOV-1 validation found that the current job executes candidate-controlled code while holding publication authority. The adopted interim posture halts production releases and names `HEXALITH_RELEASE_PUBLISH_ENABLED=false` as the deny-only emergency stop; no bounded risk exception is currently authorized. A release cannot be called FR-24-compliant or v1.0-ready until G-8 closes. This document records that state; it neither performs nor authorizes a publication.
- **Document approval is not milestone classification.** Frontmatter `product_approval` records whether Product has re-approved this text (D-9). Frontmatter `v1_readiness_milestone_reached` mirrors §0.1 and can be `true` only when §0.1 has no open rows.
- Requirements were source-derived from `_bmad-output/planning-artifacts/*.md`, `_bmad-output/contracts/*.md`, `_bmad-output/implementation-artifacts/*`, and `_bmad-output/project-docs/*.md`; the latter is provenance only and is superseded by `_bmad-output/contracts/` and `architecture.md` where they disagree (§11). Inline `[ASSUMPTION]` callouts are indexed in §13; open items are listed in §12.2. Identifier families used in this document (H/M, AD-n, DW-n, E9-AI-n, OI-n, G-n) are defined in §3.

### 0.1 V1.0 Readiness Milestone Gates

This table is the only authority on what still separates the product from the readiness milestone. D-9 may not read "Blocks: None" while any row is open. Evidence gates are closed by an artifact; approval gates are closed by a named approver citing the artifact they read. Anything not listed here is one of the following: a regression baseline or closed record (§5.0), an explicitly non-gating residual (§12.2), or an assumption (§13). These non-gating items cannot change milestone truth; each retains its owner and closure condition.

**Evidence gates**

| Gate | Pass condition | Owner | Evidence artifact | State on 2026-09-09 |
| --- | --- | --- | --- | --- |
| G-1 Release ledger closure | The Release Owner maintains an append-only, attempt-keyed ledger for every publication-capable attempt, including failed, cancelled, deferred, no-releasable, and partial-publication paths. v4.1.1 retains its published-byte verification and pending sign-off; v4.2.0, v4.3.0, and v4.4.0 receive downloaded-byte evidence and permanent dispositions without relabelling historical incidents green. Verification reruns append observations rather than replacing an attempt state, and the stale "unauthorized" sentences are corrected to current release truth. | Release Owner | `_bmad-output/implementation-artifacts/rel-ai-1-release-evidence-ledger.md`; governed-attempt classifier and ledger schema accepted under G-8 | Open. v4.1.1 awaits sign-off; three later tags have no rows; the ledger state model is not yet accepted. |
| G-2 GOV-1 provenance and external seam | One table names the authoritative Hexalith.Builds SHA per purpose — execution pin `4eb33928…`, current root gitlink `a32cb422…`, owner-accepted BUILD-REL-1 lineage anchor `a8a50859…`, and the future owner-accepted split-reusable revision. The implementation nonconformance register is closed or carries owner-accepted dispositions; authenticated Release and Release-Evidence run IDs prove the AD-13/AD-15 path; Architecture and the Release Owner accept the selected Builds contract; and the GOV-1 sprint action closes. Missing proof cannot satisfy this evidence gate. | Architecture + Release Owner | `.github/workflows/release.yml`; `eng/dependency-graph-policy.json`; `_bmad-output/planning-artifacts/architecture/architecture-gov-1-2026-07-19/nonconformance-register-2026-09-08.md`; authenticated run coordinates; passing GOV-1 revalidation | Open on implementation conformance, proof, and acceptance. The graph/catalog core remains implemented. |
| G-3 EventStore runtime identity | `frontcomposer-eventstore-approved-runtime-identity-v1.json` records a dated migration approval to EventStore `3.103.0` / source `059f6a89…` and reconciles the current Builds gitlink `a32cb422…` with the `35c3d1e5…` provenance of the 2026-09-08 identity contract and live Pact evidence. The record has `migrationApprovalClaimed: true`, is signed by the EventStore maintainer, and links evidence captured or accepted under the reconciliation rule. If no distinct EventStore maintainer owns this approval, Product and Architecture must first record OI-18 as a separate dated ownership transfer, remove EventStore from the required approver set, and identify the accountable FrontComposer role; the PRD cannot infer equivalence from the signer. | EventStore maintainer + FrontComposer maintainer + Release Owner; Product + Architecture for any ownership transfer | Identity contract; `_bmad-output/implementation-artifacts/evidence/pact-provider-reconciliation/`; D-12/OI-18 reconciliation record | Open on provenance reconciliation, ownership, and approval; the 19/19 Pact interactions and AppHost smoke passed at the older `35c3d1e5…` Builds provenance on 2026-09-08. |
| G-6 Named adopter bootstrap proof | Hexalith.Tenants boots through the documented three-call path and renders at least one generated projection and one generated command; the evidence file has a date and candidate SHA. If Product selects Parties as the substitute adopter under D-7, Parties must produce the same proof. Choosing to hold the milestone records why this gate remains open; it does not close it. | Product Owner + selected adopter maintainer | `_bmad-output/implementation-artifacts/tests/tenants-bootstrap-acceptance.md`, or an equivalently bound Parties artifact after a dated D-7 decision | Open. No adopter evidence artifact exists. |
| G-7 MCP leak/oracle audit and acceptance | The SM-4 suite exists as a named Governance-lane test class and covers: byte-identical hidden-vs-absent shapes per caller for tools; list-vs-call and skill-vs-projection oracles; an unregistered resource URI through SDK dispatch; the non-Development ban on `AllowAll*` gates; and endpoint authentication on `MapFrontComposerMcp`. A security reviewer distinct from the PRD author records OI-2's disposition of every remaining disclosure/oracle residual after any gated-list change; implementation evidence cannot substitute for that independent acceptance. Timing side channels are out of scope (FR-19). | Security reviewer distinct from the author + Framework maintainer | OI-2 sign-off plus test/enforcement evidence named by OI-3 | Open. The audit suite, SDK-dispatch test, allow-all enforcement, endpoint-authentication check, and independent disposition are missing. |
| G-8 GOV-1 publication-safety conformance | Product/Architecture/Release accept the spine's adopted AD-19 boundary: a secretless, read-only builder produces one authenticated run-bound publication-candidate artifact and a distinct protected publisher treats packages as data, executes only pinned owner-controlled code, and never checks out or executes candidate source. Candidate-controlled code receives no publication secret, write scope, OIDC/attestation authority, or signing material. Canonical `architecture.md`, FC-DEP-1, the GOV-1 story, and the G2 request remain aligned with the spine. The exact spine-defined GOV-1 split implementation gate accepts `frontcomposer.gov1-split-conformance.v1` only after all closure rows, authenticated runs/checks, five review lenses, the unchanged evidence PR, and the distinct approval-projection PR pass. | Architecture + Release Owner + Product Owner | Adopted AD-19 spine; reconciled `architecture.md`, `_bmad-output/contracts/shared-catalog-dependency-governance-2026-07-19.md`, `_bmad-output/implementation-artifacts/gov-1-validate-shared-catalog-compatibility-and-seal-dependency-provenance.md`, and `_bmad-output/planning-artifacts/g2-hexalith-builds-inline-pre-publish-gate-request.md`; accepted Builds contract; closed nonconformance register; canonical conformance packet and approval projection | Open. Source propagation completed 2026-09-09; Product/Release acceptance, implementation convergence, authenticated proof, incident readiness, and the split implementation gate remain open. No current release is FR-24-compliant on protected approval alone. |

**Approval gates**

| Gate | Pass condition | Approver | Must cite | State on 2026-09-09 |
| --- | --- | --- | --- | --- |
| G-4 Product re-approval | Product records a dated D-9 amendment approving the exact PRD and addendum SHA-256 pair after OI-4, OI-10, OI-16, and OI-19 close and the synthesized postfix reviewer gate has no unresolved Critical/High finding. Any later digest change requires a fresh gate report. | Product Owner | `_bmad-output/planning-artifacts/prds/prd-frontcomposer-2026-09-08/review-gate-postfix-2026-09-09.md`; D-13 disposition; corrected UX authority/supplements and UX revalidation; FR-23 parity evidence | Open. |
| G-5 Epic 9 acceptance | Product records acceptance of the Story 9.8 proof packet as composed/live FC-NIP evidence, and `sprint-status.yaml` closes `epic-9` and E9-AI-1…E9-AI-6 or rewrites them as residuals. | Product Owner | `_bmad-output/implementation-artifacts/tests/9-8-live-acceptance.md` | Open on the record; Stories 9.1–9.8 are done and the live proof passed 2026-08-27. |

## 1. Vision

Hexalith.FrontComposer is the Hexalith Blazor Front Shell: a .NET framework that turns annotated domain read models and commands into an operations-ready Blazor UI, an MCP tool/resource surface for AI agents, and developer tooling for inspection, migration, and testing.

The product bet is that domain teams should describe their operational surface once, in code, then get consistent human and AI access paths without hand-building every admin shell. FrontComposer makes the domain type the source of truth and uses source generation, schema fingerprints, and strict governance tests to keep UI, lifecycle state, MCP descriptors, and tooling aligned.

The v1.0 readiness milestone commits to two outcomes: a **developer framework that is safe to package, consume, test, customize, and evolve** and an **operations shell an operator can trust**. In that shell, fresh rows are marked only from real command outcomes, projection freshness recovers after transport failure, and command state never overstates what the backend confirmed. Epics 9–11 delivered the runtime remediation baseline, but the 2026-09-09 validation reopened release-safety conformance and UX handoff completeness. The remaining work is listed in §0.1: release architecture, implementation, evidence, and ownership under G-1/G-2/G-8; the MCP audit under G-7; adopter proof under G-6; EventStore provenance reconciliation under G-3; and Product approvals under G-4/G-5.

## 2. Target Users

### 2.1 Primary Users

- **Adopter developer** — a developer on a Hexalith domain module such as Tenants, Parties, or future domain packages who wants an admin/operations shell without writing bespoke Blazor scaffolding.
- **Operator** — an authenticated admin or support user who needs to browse projections, understand status, and execute commands safely.
- **AI-agent integrator** — a maintainer exposing the same domain command/projection surface to MCP clients with fail-closed security.
- **Framework maintainer** — a FrontComposer contributor evolving the generator, Shell, MCP server, CLI, Testing package, and public contracts.
- **Release owner** — a maintainer responsible for governed release operation, NuGet package quality, public API baselines, docs validation, credentials, incident containment, and evidence artifacts.

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
- **Pre-accept retry / transient retry** — FR-15 defines two retry stages: the pre-accept stage allows zero retries before the backend acknowledges a command (formerly "Epic 3 retries"); the transient stage allows exactly one retry after `250` ms for a transport failure after acknowledgement (formerly "Epic 4 transient retry").
- **Pending Command** — bounded local state for accepted commands awaiting projection evidence or terminal status (`MaxPendingCommandEntries=100`).
- **Customization Levels** — Level-2 projection templates, Level-3 field slots, Level-4 full-view overrides (FR-5).
- **FC-LYT / FC-A11Y / FC-L10N / FC-DOC / FC-CMD / FC-CNC / FC-NIP / FC-IA-1** — the confirmed contracts for layout, accessibility, localization ownership, component docs, command lifecycle, one-at-a-time execution, new-item producer, and Module/Tab information architecture.
- **Fluent UI v5 Policy** — project-wide requirement to use FrontComposer or Fluent UI Blazor v5 components and Fluent 2 tokens for interactive UI.
- **Governed Release** — the target D-16/G-8 release contract: a secretless, read-only candidate builder hands one authenticated run-bound artifact to a distinct protected publisher that executes only pinned owner-controlled code and treats candidate packages as data. Exact-SHA dispatch, protected-environment approval, or manifest classification alone does not satisfy this term.
- **Publication Authorization** — permission to publish one exact candidate under the D-16/G-8 privilege boundary, including a valid classified manifest and protected approval; distinct from document approval (D-9), post-publication audit, and the readiness milestone (§0.1).
- **REL-1…REL-5 / REL-AI-1 / GOV-1 / BUILD-REL-1** — release-governance work items: REL-* are delivery history for the earlier model, REL-AI-1 is post-publication ledger observation, GOV-1 owns dependency provenance and publication-safety architecture, and BUILD-REL-1 is the upstream Hexalith.Builds contract being amended for the G-8 boundary.
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

**Table A — work still gated by §0.1.** Each row states whether implementation, evidence, or approval remains. No unlisted row is silently reopened.

| Gated item | Open gate | Pass/fail sentence |
| --- | --- | --- |
| FR-24 release safety, ledger, and provenance | G-1, G-2, G-8 | The append-only attempt ledger is complete; authenticated provenance is accepted; candidate code is isolated from publication authority; and GOV-1 architecture plus implementation pass revalidation. |
| FR-29.7 EventStore identity | G-3 | The current Builds advance is reconciled with the existing identity/Pact provenance and migration approval is recorded. |
| SM-1 adoption proof (for FR-1, FR-2, FR-7, FR-8) | G-6 | Tenants, or a dated Product-selected Parties fallback, supplies equivalent candidate-bound bootstrap evidence. |
| SM-4 audit and FR-19 host-enforcement consequences | G-7 | The audit/enforcement evidence and the independent OI-2 security disposition both exist. |
| FR-8, FR-10–FR-16, FR-22, FR-23 UX deltas and SM-6 | G-4 through OI-16 | The 2026-09-09 focus, announcement, validation/rejection, state, responsive-accessibility, component/source, testing, and evidence obligations are implemented and pass UX revalidation. The previously delivered runtime baseline remains intact. |
| D-9, D-4, and UX-contract approvals | G-4, G-5 | Dated approval records exist; OI-4, OI-10, OI-16, OI-19, and the digest-bound postfix reviewer gate are closed before D-9 re-approval. |

**Table B — regression baselines.** Delivered; the Framework maintainer verifies no regression on every release.

| FR | Note |
| --- | --- |
| FR-1 to FR-12 | Generated projection, shell, grid, freshness, and realtime runtime baseline (FR-12 bounds in NFR-8). The 2026-09-09 UX deltas to FR-8/FR-10–FR-12 are Table A work. |
| FR-13 | Complete: Stories 9.3–9.8 done; live proof 2026-08-27 on candidate `7a573763`. Residuals and the DW-679 non-goal in FR-13/D-4. |
| FR-14 to FR-19a | Command lifecycle and MCP runtime baseline, including cross-request lifecycle (Story 11.3). The 2026-09-09 UX deltas to FR-14–FR-16 and FR-19's host-enforcement consequences are Table A work. |
| FR-20 to FR-23 | CLI, Testing, and documentation baseline. FR-22/FR-23 UX assertion and semantic-documentation deltas are Table A work. |
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
- Projection roles and templates select the generated view shape; bounded-context metadata selects the operator Module, route family, and registration boundary; column priority determines responsive retention.
- Badges, icons, relative time, currency, and display metadata produce localized semantic text and status cues that do not rely on color alone. Field groups, defaults, derived/server-controlled fields, destructive confirmation, and policy requirements determine form grouping, editability, validation, and guards. Empty-state CTA metadata produces a valid authorized action or no action. Command-target declarations feed FR-13 identity resolution.
- The versioned vocabulary-to-behavior matrix in Addendum §4.5 is the extractable acceptance surface for each attribute family; changes update that matrix, the diagnostic registry, generated snapshots, and public documentation together.
- Unsupported or invalid attribute use emits the HFC diagnostic governed by `docs/diagnostics/diagnostic-registry.json`, the executable diagnostic authority. Allocated bands are `HFC0001–HFC0999` Contracts, `HFC1000–HFC1999` SourceTools, `HFC2000–HFC2999` Shell, `HFC3000–HFC3999` EventStore, `HFC4000–HFC4999` MCP, and `HFC5000–HFC5999` Aspire; migration tooling uses the separately governed `HFCM9xxx` family. Registry-approved cross-package exceptions such as `HFC1601` remain explicit.

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
- Successful client-side route activation focuses the route-level `h1`; failed navigation preserves a usable focus location and announces the failure.
- At 320 CSS-pixel reflow and 400% zoom, with resilient text spacing, generated shell surfaces preserve meaning and operation, keep focus unobscured, and meet WCAG 2.2 AA target-size requirements subject to the standard's exceptions.

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
- Keyboard tab selection keeps focus on the active tab while its labelled tabpanel changes. Palette activation that navigates lands on the route heading; closing a palette or dialog returns focus to its invoker.

#### FR-11: Render projection grids and states

Generated projection pages must provide filtering, empty/loading states, status indicators, expand-in-row details, column prioritization, slow-query notices, and max-items notices.

**Consequences:**
- Column filters are debounced and resettable.
- Row detail regions remain accessible and announce filter-hidden expanded rows; live regions are used only where the state change is useful and non-noisy.
- Projections with more than 15 columns activate column prioritization.
- The projection state set is Loading, Empty, Data, Stale, Reconnecting, FallbackPolling, SlowQuery (after `SlowQueryThresholdMs=2_000`), and MaxItems (`MaxUnfilteredItems=10_000`, server-side virtualization from `500` rows); each is operator-visible and accessible.
- Each state has defined entry evidence, user-visible meaning, permitted actions, recovery/timeout behavior, announcement behavior, and terminal/non-terminal classification in the state-by-surface acceptance matrix.
- Meaningful transitions are announced once through the appropriate polite status channel, deduplicated by operation/entity and state, with rapid intermediate progress coalesced and polling/retry ticks suppressed.
- Status values render as semantic icon-plus-text affordances with tooltip and `aria-label` support; color is never the only signal.

#### FR-12: Maintain projection freshness and realtime behavior

The Shell must query EventStore over HTTP and subscribe to projection changes over SignalR while surfacing reconnect/reconciliation state and recovering when the backend recovers.

**Consequences:**
- Reconnect and fallback-polling states are visible to operators.
- Reconnecting, fallback, recovery, and terminal freshness transitions follow FR-11's deduplicated announcement contract; repeated retry/poll ticks are silent.
- Projection updates do not treat SignalR nudges as proof of command success.
- A long-lived circuit never permanently degrades: reconnect retries are unbounded with jittered exponential backoff capped at `30_000` ms, a closed connection restarts within `10` s, fallback polling runs every `15` s across at most `8` lanes while disconnected, a reconnected notice shows for `3_000` ms, and the subscription resumes automatically on reconnect (Story 11.2).

#### FR-13: Mark fresh rows only through FC-NIP

The product must not infer row-level fresh indicators from projection nudges that lack row identity. FC-NIP owns the row identity payload and producer wiring.

**Consequences:**
- `FcNewItemIndicator` remains a confirmed component.
- Automatic row marking uses only an immutable Command Target Identity captured before dispatch from `[CommandTarget]` with a typed `ICommandTargetIdentityProvider<TCommand>` or a declared `SameAsSource` snapshot. Terminal `Material`, `NoOp`, or `Unknown` classification is independent; unknown identity or materiality suppresses publication.
- **[NON-GOAL for v1.0]** Commands whose target key is allocated server-side cannot declare a pre-dispatch target and therefore produce no fresh-row marker; a typed post-dispatch identity proof is tracked as DW-679. Operators see no indicator for those rows rather than a wrong one.
- Exactly one terminal producer boundary publishes indicators; indicator state is observable by generated grids and scope-safe per tenant and user; publication is atomic first-wins per `ViewKey`/`EntityKey` across distinct message IDs.
- A newly material fresh-row indicator is announced at most once per tenant/user/entity transition; expiry is silent. Forced-colors and reduced-motion modes preserve its meaning without relying on color or animation.
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
- Client validation associates each error with its Fluent input, exposes a summary linked to invalid controls, focuses the summary after failed submit, preserves useful input, and supports keyboard navigation to the first invalid field.
- Asynchronous server rejection remains a lifecycle outcome and is not recast as field validation unless a support-safe field mapping exists.

#### FR-15: Surface command lifecycle states

The Shell must surface Submitting, Acknowledged, Syncing, Confirmed, Rejected, IdempotentConfirmed, NeedsReview, Warning, and Degraded states.

**Consequences:**
- Accepted HTTP transport is not displayed as projection-confirmed success.
- Polling binds to the confirmed EventStore status endpoint.
- Default deterministic budgets are as follows: the confirming-to-Degraded transition occurs after `10_000` ms (`TimeoutActionThresholdMs`); status polling occurs every `1_000` ms for at most `120_000` ms (`MaxPendingCommandPollingDurationMs`); the pre-accept retry count is zero; and one transient retry occurs `250` ms after acknowledgement. Configuration changes require focused `FakeTimeProvider` evidence.
- Each state has defined entry evidence, user-visible meaning, permitted actions, recovery/timeout, announcement, and terminal/non-terminal classification. Progress uses a polite status channel; submit-blocking validation or rejection uses the focused error-summary/alert path. Each meaningful terminal transition is announced once and intermediate progress is coalesced.

#### FR-16: Enforce command safety

Command execution must respect authorization, destructive confirmation, form-abandonment guard, and FC-CNC one-at-a-time execution.

**Consequences:**
- `[RequiresPolicy]` is evaluated before `BeforeSubmit` and again afterward for protected commands.
- The service boundary also enforces authorization through `AuthorizingCommandServiceDecorator`.
- FC-CNC v1 blocks later local submits rather than queueing or batching them, preserves the in-flight command, and presents localized, accessible feedback that a later submit was not queued.
- A blocked second submit keeps focus usable, announces the reason once, and leaves the in-flight command as the only operation whose lifecycle may advance.
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

MCP hosts must register tenant tool and resource visibility gates, negotiate schema fingerprints, and collapse admission failures to the opaque public shapes below. The guarantee is exact but limited. **For tools, hidden and absent are indistinguishable for the same caller. For resources, reads of registered-but-hidden resources are indistinguishable from unauthorized reads. The resource catalog itself is disclosed, and the SDK answers unregistered URIs.**

| Request class | Public shape | Guarantee |
| --- | --- | --- |
| `tools/list` with no auth, missing tenant, or catalog failure | Successful `ListToolsResult` with an empty `Tools` collection | An authenticated caller always sees at least the lifecycle tool, so an empty list is a credential-validity signal. Observed residual pending OI-2; not accepted. |
| `tools/call` for an unknown, hidden, unauthorized, tenant-less, or policy-denied tool (including the lifecycle tool) | Structured `category: "unknown_tool"` plus `docsCode: "HFC-MCP-UNKNOWN-TOOL"`, `suggestion`, the caller's own `visibleTools` (possibly `continuation: "visible-list-truncated"`); text `Request failed.` | Byte-identical for hidden and absent for the same caller. |
| `tools/call` for a visible tool with an incompatible client schema | Structured `category: "schema-mismatch"`; no side effect | Distinct from hidden by design (the tool is visible). |
| `resources/list` | The static generated descriptor catalog (URI, name, title, description of every projection and skill resource), identical for every caller | Not tenant-filtered and not gated. Discloses bounded-context and projection names. Observed residual pending OI-2; not accepted. |
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
- The host and helpers can verify first-invalid navigation, summary-to-field links, input preservation, client-validation versus server-rejection behavior, state announcements and deduplication, blocked-submit feedback, route/tab/palette/dialog focus, keyboard-only recovery, and silent fresh-row expiry.

#### FR-23: Maintain component and skill documentation

FrontComposer must keep component docs, diagnostic docs, migration docs, and skill-corpus docs synchronized with the generated and runtime surfaces.

**Consequences:**
- Published docs under `docs/` pass the DocFX validation gate when changed.
- Skill-corpus docs satisfy required front matter and snippet/reference validation.
- `_bmad-output/contracts/fc-doc-component-documentation-2026-06-03.md` governs component-page conformance and its status obligations; `docs/reference/components/index.md` inventories published pages. `tests/Hexalith.FrontComposer.SourceTools.Tests/Docs/FcDocComponentDocumentationContractTests.cs` checks authored-page conformance, page-to-index inclusion, and the four currently enumerated status-map areas; complete public-surface parity remains OI-19.
- Diagnostic documentation is authoritative in `docs/diagnostics/diagnostic-registry.json`, checked by `tests/Hexalith.FrontComposer.SourceTools.Tests/Diagnostics/DiagnosticRegistryTests.cs`. `src/Hexalith.FrontComposer.Cli/MigrationCatalog.cs` is authoritative for CLI-supported executable migration edges; `docs/migrations/index.md` inventories published guides and may include explicitly classified manual-only package/API edges. `tests/Hexalith.FrontComposer.Cli.Tests/MigrationCommandTests.cs` checks executable catalog behavior and its guide link; catalog/index classification parity remains OI-19.
- Skill-corpus documentation is authoritative under `docs/skills/frontcomposer/**/*.md`, embedded by `src/Hexalith.FrontComposer.Mcp/Hexalith.FrontComposer.Mcp.csproj`, and checked by `tests/Hexalith.FrontComposer.Mcp.Tests/Skills/SkillCorpusTests.cs`. Cross-surface drift is enforced by `eng/validate-docs.ps1` and `docs/validation/producer-fingerprints.json`.
- These inventories are content-checked, not merely syntax-checked; documented FrontComposer identifiers and Fluent APIs must resolve against the selected catalog pin. `FcPageToolbar` is the public product contract; its internal Fluent composition is implementation-owned unless deliberately promoted to public API.
- Known stale public documentation is either corrected or listed as an owner-bound, time-bounded exception with a revisit condition.
- Generated/scratch planning docs remain outside `docs/`.
- `docs/fluent-ui-v5-contingency.md` still cites an rc.2 pin and is corrected under OI-10.

### 5.7 Package Release And Brownfield Remediation

**Description:** the readiness milestone depends on strict package, public API, release, and remediation quality.

#### FR-24: Publish only evidence-classified package artifacts

FrontComposer must publish only the expected NuGet package set (§11), using exact candidate bytes that were inventory-validated, consumer-validated, checksummed, manifest-bound, and classified as publishable before any NuGet or GitHub Release side effect.

**Consequences:**
- Conventional commits determine the version bump. Current execution truth is a legacy reusable job selected by `.github/workflows/release.yml`; its deny-by-default repository-variable gate and protected-production-environment approval are controls, but they do not isolate candidate-controlled code from publication authority and therefore do not satisfy this requirement. The target contract is D-16/G-8; the caller must not switch to an allegedly governed mode until its split-phase boundary is accepted and implemented.
- In the target contract, a secretless/read-only builder produces one authenticated run-bound artifact. A distinct protected publisher/attester treats packages as data, executes only pinned owner-controlled code, never checks out or executes candidate source, and exposes no publication secret, write scope, OIDC/attestation authority, or signing material to candidate-controlled code.
- Candidate GitHub assets are byte-identical to descriptor files. NuGet.org repository signing may add only the root `.signature.p7s`; verification requires a valid repository signature and byte-equivalent normalized ZIP members for every other entry. Any other addition, removal, or content drift is an incident; rebuilding or repacking is never equivalent evidence.
- The secretless builder runs inventory, exact-package tests, consumer validation, checksums, SBOM, symbols, and diagnostic early-denial checks against one prepared set, then uploads one authenticated `publication-candidate-<run_id>-<run_attempt>` artifact. Builder-side manifest/classification output is never a seal or authorization consumed by publication. The protected publisher independently validates the descriptor/policy/evaluator/file inventory, mints and verifies provenance attestation or the approved fallback, seals the final manifest, performs the final offline/live classification, and requires `publish_authorized=true` before side effects. The total classifier assigns every failed, cancelled, deferred, no-releasable, partial-publication, blocked, and successful attempt one disposition and inspects every started publication for partial effects. `deferred-no-ci-handoff` is valid only as the sole deferred sentinel; it is a terminal, incident-bearing, permanent disposition. A malformed, duplicate, missing, or unauthenticated handoff maps to `missing-artifact`, never to deferral.
- The `hexalith.release-evidence.v4` manifest uses byte-unique canonical representations and binds the exact authenticated candidate/GitHub asset bytes, the NuGet repository-signature and normalized-member equivalence rule, selected quality run, complete depth-1/2 `hexalith.dependency-graph.v1`, immutable dependency policy, authenticated run coordinates, and exact active-policy-authorized evaluator identities. Post-publication verification records the actual downloaded signed package and proves equivalence without rewriting the sealed authorization. A later default-branch helper cannot evaluate or relabel an earlier candidate; v1–v3 manifests are audit-only.
- Durable evidence is attached to the GitHub Release; a 30-day Actions artifact is supplemental. The attempt-keyed ledger is append-only: reruns append observations and can never replace or weaken an incident. v4.2.0, v4.3.0, and v4.4.0 still lack rows (G-1); historical blocked, invalid, or non-compliant releases remain permanently visible.
- REL-AI-1 is post-publication audit closure — the Release Owner confirms downloaded GitHub-asset hashes and the NuGet repository-signature/normalized-member equivalence result, then signs the ledger row — not a pre-publication control and not a pre-condition of any release. The circular reading in the 2026-08-12 text is withdrawn.
- The shared-workflow dependency is Hexalith.Builds `BUILD-REL-1`. The owner-accepted lineage anchor/predecessor is `a8a50859…`, the current execution pin is `4eb33928…`, and the current root gitlink is `a32cb422…`; the exact owner-accepted split-reusable revision does not yet exist in the PRD evidence. Its acceptance and authenticated proof are G-2; publication-safety conformance is G-8.

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

#### FR-27: Retired/closed — tooling-governance follow-through

This stable ID is a completed Epic 10 traceability record and creates no implementation work. Its enduring outcomes are enforced by FR-20–FR-23, NFR-6, NFR-10, and D-15.

**Consequences:**
- Evidence reconciliation proves that CLI, diagnostics, migration, Testing, and documentation artifacts agree on current labels and outcomes.
- HFCM9002 production emission is **not approved**; it stays synthetic/manual sidecar evidence only (D-15).
- Testing redaction coverage proves evidence output does not leak support-sensitive data.

#### FR-28: Retired/closed — Epic 11 decision gates

This stable ID is a closed Epic 11 traceability record and creates no implementation work. Its enduring route and Contracts-boundary invariants are enforced by FR-2, FR-10, NFR-2, D-3, and D-5.

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
- **NFR-3 Accessibility:** generated and hand-authored UI must conform to WCAG 2.2 AA and preserve accessible names, roles, keyboard operation, deterministic route/tab/palette/dialog focus, linked validation summaries, non-noisy live-region behavior, 320 CSS-pixel reflow/400% zoom, resilient text spacing, target size subject to the standard's exceptions, unobscured focus, reduced motion, and forced-colors meaning. Status and lifecycle meaning never depends on color, motion, or hover alone.
- **NFR-4 Fluent UI governance:** UI uses FrontComposer/Fluent UI Blazor v5 components and Fluent 2 tokens; raw interactive HTML controls and legacy tokens are forbidden except documented carve-outs.
- **NFR-5 Security:** MCP and Shell security fail closed within the exact guarantees of FR-19 and FR-30; server-controlled fields are never client-supplied; return paths, storage keys, tenant/user scope, auth state, and API keys have direct tests.
- **NFR-6 Privacy and support safety:** UI, logs, telemetry, MCP responses, evidence, and snapshots must not expose raw tokens, JWT payloads, raw EventStore metadata, stack traces, raw event payloads, or unrestricted PII.
- **NFR-7 Schema and evidence determinism:** canonical schema material, release-evidence values, fingerprint algorithms, baseline identity, and provenance validation are load-bearing public contracts. Every accepted evidence value has one byte-unique representation backed by cross-language hostile and golden vectors; independently built producers and verifiers cannot disagree on run identity, conclusion, or encoded bytes.
- **NFR-8 Reliability:** command lifecycle budgets are FR-15's (`10_000` ms to Degraded, `1_000` ms polling for at most `120_000` ms, one `250` ms transient retry); projection realtime bounds are FR-12's (jittered exponential reconnect capped at `30_000` ms, unbounded attempts, `10` s closed-restart, `15` s fallback polling over at most `8` lanes, `3_000` ms reconnected notice). Degraded, reconnecting, and fallback states are visible within those budgets, recover when the backend recovers, and never convert a nudge or HTTP acceptance into confirmed success.
- **NFR-9 Performance:** palette scoring stays inside the `PaletteScorerBench` thresholds (`tests/Hexalith.FrontComposer.Shell.Tests.Bench`), generated rendering inside `RazorEmitterPerformanceTests` (`tests/Hexalith.FrontComposer.SourceTools.Tests/Emitters`), and cache-backed hot paths inside the `FcShellOptions` caps (`MaxPendingCommandEntries=100`, `MaxProjectionFallbackPollingLanes=8`, `LocalStorageMaxEntries=500`, `MaxUnfilteredItems=10_000`); any threshold change requires benchmark evidence and release-owner approval.
- **NFR-10 Observability:** FrontComposer uses `FrontComposerActivitySource`, source-generated `LoggerMessage` sites bound after `IsEnabled`, and sanitized structured logs for operator-relevant failure paths (including EventIds 5912/5913), with tests or snapshots proving tokens, JWT payloads, raw EventStore metadata, raw event payloads, stack traces, and unrestricted PII are absent.
- **NFR-11 Testing:** the milestone's mandatory lanes are: solution-level default lane with `DiffEngine_Disabled=true`; Governance (`FluentConformanceTests`, `InfrastructureGovernanceTests`, drift tests, the G-7 audit, and G-8 release-governance checks); Contract; snapshot verification; PublicAPI baselines; ApiCompat against the published baseline; Pact consumer checks plus live-compatibility Pact provider evidence reconciled to the approved EventStore identity (D-12); property tests where configured; semantic DocFX/inventory validation; the Epic 9 live-proof lane; and e2e/bUnit accessibility evidence for state announcements, validation/rejection, focus, keyboard recovery, reflow/zoom, text spacing, target size, focus-not-obscured, forced colors, and reduced motion. Test projects run individually; the `.slnx` is for restore/build.
- **NFR-12 Release evidence:** only the protected candidate-free publisher may authenticate the publication candidate, mint/verify provenance attestation or the approved fallback, seal `hexalith.release-evidence.v4`, perform final offline/live classification, and require `publish_authorized=true`. The final manifest binds exact candidate asset checksums, inventory, consumer validation, symbols, SBOM, selected-run coordinates, evaluator identities, and attestation/fallback. Candidate GitHub assets remain byte-identical; NuGet downloads may add only a valid root `.signature.p7s`, with every other normalized ZIP member byte-equivalent. Candidate code is isolated from publication authority; every attempt receives a total disposition; append-only evidence cannot be weakened by reruns or later-branch code. Author signing and RFC 3161 timestamps are not requirements.
- **NFR-13 Dependency governance:** compatibility is established from versioned semantic shared-catalog profiles and affected-module standalone Release/NuGet restore/build evidence, never from historical commit or fingerprint allowlists. `hexalith.dependency-graph.v1` is exactly depth 1–2; collection never recursively initializes nested submodules and fails closed above the ratified ceilings recorded in the addendum and `eng/dependency-graph-policy.json`.

## 7. Constraints And Dependencies

- **Runtime and framework:** .NET 10, C# latest, Blazor, Fluxor, Roslyn, ModelContextProtocol SDK, SignalR, OIDC, NUlid — versions are pinned centrally by the selected Hexalith.Builds catalog. The current root gitlink is `a32cb422…`; the 2026-09-08 identity/Pact and Fluent-pin evidence was captured at `35c3d1e5…` and must not be projected onto the newer gitlink without D-12/D-13 reconciliation. The Fluent RC posture remains proposed under D-13/OI-4.
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
- **Release governance:** REL-1…REL-5 remain delivery history; REL-AI-1 is open (G-1). The 2026-09-09 GOV-1 gate failed publication-boundary architecture and implementation conformance. Its graph/catalog core remains implemented, but G-2 proof/acceptance and G-8 architecture-plus-code work are open.

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

- **SM-1: Adopter bootstrap success** — Hexalith.Tenants, or Parties after a dated D-7 fallback decision, boots through the documented three-call path and renders at least one generated projection and one generated command without bespoke framework plumbing. **Evidence:** `_bmad-output/implementation-artifacts/tests/tenants-bootstrap-acceptance.md` or equivalently bound Parties evidence. **State: unmet** (G-6). Validates FR-1, FR-2, FR-7, FR-8.
- **SM-2: Release ledger integrity** — every publication-capable attempt has one immutable attempt identity and append-only disposition covering success, block, failure, cancellation, deferral, no releasable artifact, and partial publication. `deferred-no-ci-handoff` is valid only as the sole deferred sentinel and remains a terminal, incident-bearing, permanent disposition; a malformed, duplicate, missing, or unauthenticated handoff is `missing-artifact`. Compliant publications additionally retain publisher-sealed `publish_authorized=true`, exact candidate/GitHub asset hashes, valid NuGet repository signatures with byte-equivalent non-signature normalized ZIP members, inventory/consumer/SBOM evidence, and Release Owner sign-off; incidents are never relabelled green. **Evidence:** `_bmad-output/implementation-artifacts/rel-ai-1-release-evidence-ledger.md` plus the accepted G-8 classifier/schema. **State: unmet** — v4.1.1 awaits sign-off; v4.2.0–v4.4.0 lack rows; the state model is open (G-1/G-8). Validates FR-24, FR-25, NFR-12.
- **SM-2a: Dependency provenance and release trust** — every publish-capable release seals and live-verifies the complete depth-1/2 graph, immutable active policy, authenticated selected CI/quality/Release/verifier coordinates, exact authorized evaluator closure, and split builder/publisher boundary; compatible pointer advances pass semantic-profile and affected-module gates without allowlist edits. **Evidence:** `eng/dependency-graph-policy.json`, authenticated run IDs, accepted split-phase Builds contract, and passing GOV-1 revalidation. **State: graph/catalog core met; release trust chain and implementation conformance unmet** (G-2/G-8). Validates FR-24, NFR-7, NFR-12, NFR-13.
- **SM-3: Contract drift visibility** — intentional generator or schema changes update baselines, diagnostics, migration/deprecation artifacts, or release notes; accidental drift is caught by HFC1065/HFC1066, snapshots, PublicAPI, or ApiCompat before release. **Evidence:** `tests/Hexalith.FrontComposer.SourceTools.Tests/Drift/`, `eng/release_compatibility.py` lane. **State: met on the current lanes; baseline lag noted (OI-11).** Validates FR-6, FR-20, FR-21, FR-25.
- **SM-4: MCP leak/oracle audit** — an independent suite proves, for every request class in FR-19, exactly the guarantee stated there: byte-identical hidden-vs-absent tool shapes per caller; no list-vs-call or skill-vs-projection oracle beyond the disclosed catalog; an unregistered URI through SDK dispatch behaves as stated; no response or log echoes tool names, fingerprints, tenant identifiers, or exception text; `AllowAll*` gates absent from non-Development hosts; endpoint authentication present. **Evidence:** the G-7 test class (to be named). **State: unmet** — admission/taxonomy/redaction tests exist (`ToolAdmissionTests`, `ProjectionReaderTaxonomyTests`, `SchemaNegotiationPrecedenceMatrixTests`, `AuthRedactionStressTests`); the audit suite does not (G-7). Validates FR-17, FR-18, FR-19, FR-19a.
- **SM-7: Operator freshness trust** — the composed/live Epic 9 lane proves that a fresh-row indicator appears for rows the operator's own command materially changed, is never produced by a nudge, and clears per tenant/user; suppression rate `5912/(5912+5913)` is observable. **Evidence:** `_bmad-output/implementation-artifacts/tests/9-8-live-acceptance.md` (candidate `7a573763`, 2026-08-27). **State: runtime behavior met, excluding server-allocated-key commands; announcement/forced-colors evidence is tracked by SM-6.** Validates FR-13, FR-26.
- **SM-8: Projection recovery** — after a forced SignalR disconnect the shell shows Reconnecting then FallbackPolling within one polling interval, and resumes live updates with a reconnected notice once the hub returns, with no permanently degraded circuit. **Evidence:** `_bmad-output/implementation-artifacts/spec-11-2-projection-realtime-resilience.md` and its named Shell.Tests suites. **State: recovery met; state-announcement evidence is tracked by SM-6.** Validates FR-12, NFR-8.
- **SM-9: Architecture-review closure** — every H1–H12 row in FR-29 has a done story and regression check, the EventStore identity is reconciled, and the publication-safety trust boundary passes GOV-1 revalidation. **Evidence:** FR-29 table; `_bmad-output/implementation-artifacts/sprint-status.yaml`; G-3 and G-8 artifacts. **State: H1–H12 closed; G-3 and G-8 open.** Validates FR-24, FR-29.
- **SM-10: Command outcome trust** — no generated form renders Confirmed on transport acceptance alone; a stalled confirmation lands in Degraded at `10_000` ms; one-at-a-time execution blocks a second submit with accessible feedback; and each meaningful state transition follows the announcement contract. **Evidence:** Shell.Tests `FakeTimeProvider` lifecycle/FC-CNC suites and `_bmad-output/implementation-artifacts/11-6-testing-harness-failure-modes.md`. **State: lifecycle semantics met; validation/focus/announcement evidence is tracked by SM-6.** Validates FR-15, FR-16.

**Secondary**

- **SM-5: Testing harness usefulness** — adopter tests simulate command success, rejection, timeout/stall, authorization denial, paging/filter/sort, and redacted evidence using the Testing package. **Evidence:** `_bmad-output/implementation-artifacts/11-6-testing-harness-failure-modes.md`; `src/Hexalith.FrontComposer.Testing/PublicAPI.Shipped.txt`. **State: core failure-state harness met; UX assertion expansion is OI-16.** Validates FR-22.
- **SM-6: UX governance stability** — the Governance and e2e/bUnit lanes report zero raw interactive-control, legacy-token, unlinked/dead CSS, unresolved public-component-name, journey-coverage, or accessibility-critical findings; state announcements, linked validation/rejection, route/tab/palette/dialog focus, keyboard recovery, 320 CSS-pixel reflow/400% zoom, text spacing, target size, focus-not-obscured, forced colors, and reduced motion all have deterministic evidence. **Evidence:** `FluentConformanceTests`, `InfrastructureGovernanceTests`, UX validation, and e2e/bUnit accessibility/visual lanes. **State: unmet for the added 2026-09-09 contract; OI-16.** Validates FR-8, FR-10–FR-16, FR-22, FR-23, NFR-3, NFR-4.

**Counter-metrics**

- **SM-C2: Visual polish cannot outrank contract safety.** UI refinement must not bypass accessibility, public API, or package-consumer constraints.
- **SM-C4: A green workflow, protected-environment approval, or manifest classification alone is not release success.** None proves candidate code was isolated from publication authority. A successful evidence workflow with `classification=blocked` or `publish_authorized=false` counts as a failed release; lane greenness never substitutes for G-8, the classified manifest, append-only ledger, or leak/oracle audit.
- **SM-C5: Backfilled ledger rows without downloaded-byte verification do not close G-1.** A row that records a tag but not the verified published hashes and a disposition is bookkeeping, not evidence.
- SM-C1 and SM-C3 (generated-file count, CLI output volume) are retired; they guarded pressures the program never exhibited.

## 10. Risks And Mitigations

- **Risk: planning artifacts drift behind delivery.** This PRD was stale by four weeks on 2026-09-08, and `epics.md` still carries a 2026-07-17 table. Mitigation: §8.2 is restated from `sprint-status.yaml` on every update; OI-6 syncs `epics.md`; readiness reruns after each correction batch.
- **Risk: the milestone table is mistaken for a publication switch.** Mitigation: §0 distinguishes documentation from authorization, but it no longer calls the active legacy path conforming. A release counts as FR-24-compliant only after G-8; frontmatter carries `v1_readiness_milestone_reached`, not a publication flag.
- **Risk: candidate-controlled code receives publication authority.** Protected-environment approval does not sandbox the approved job. Mitigation: D-16/G-8 require a secretless builder and pinned data-only publisher; no current release is described as compliant on approval alone.
- **Risk: published tags outrun or rewrite the evidence ledger.** v4.2.0–v4.4.0 are tagged while the ledger lacks rows, and an underspecified backfill could relabel incidents. Mitigation: G-1 requires immutable attempt identities, downloaded-byte evidence, permanent dispositions, partial-side-effect inspection, and append-only rerun observations; SM-C5 forbids paper backfill.
- **Risk: later-branch evaluator code changes historical truth.** Mitigation: FR-24/NFR-12/G-8 bind evaluation to the exact active-policy-authorized closure and original authenticated candidate; post-release verification is read-only and cannot authorize retroactively.
- **Risk: incident detection has no containment contract.** Mitigation: D-16/OI-15 assign the Release Owner an acknowledgement/containment target, authoritative emergency-stop control, immutable evidence preservation, credential rotation when exposure is plausible, unlisting preference, and correction only under a new version.
- **Risk: EventStore identity approval is mistaken for the only remaining act.** The current Builds gitlink advanced after the 2026-09-08 evidence. Mitigation: D-12/G-3 preserve the old provenance and require explicit semantic-compatibility acceptance or recaptured evidence before approval.
- **Risk: MCP "fail-closed" is read as an isolation guarantee.** Mitigation: FR-19 states the exact guarantee per request class including what is disclosed; G-7 audits it; OI-2 requires a security sign-off by someone other than the author.
- **Risk: Fluent UI RC breaks under a stable release.** Mitigation: D-13 remains proposed pending Product confirmation and current-gitlink reconciliation; public component names must resolve against the selected catalog, and a Fluent v5 GA triggers re-decision.
- **Risk: gitlink identity is confused with shared-catalog compatibility.** Mitigation: semantic profiles plus affected-module proof (NFR-13); exact SHAs are provenance only; G-2 names one SHA per purpose.
- **Risk: UX requirements are present but not deterministic enough for independent implementation and testing.** Mitigation: FR-8 and FR-10–FR-16 now define focus, state, validation, and announcement outcomes; OI-16 repairs the UX authority/supplements and closes high findings; SM-6 carries measurable accessibility evidence.

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
- HFC diagnostics: `docs/diagnostics/diagnostic-registry.json` is the executable authority for the allocated Contracts (`HFC0001–HFC0999`), SourceTools (`HFC1000–HFC1999`), Shell (`HFC2000–HFC2999`), EventStore (`HFC3000–HFC3999`), MCP (`HFC4000–HFC4999`), and Aspire (`HFC5000–HFC5999`) bands, including public `HFC4001` and registry-approved cross-package exceptions such as `HFC1601`; migration tooling uses the separately governed `HFCM9xxx` family.
- CLI JSON schemas `frontcomposer.cli.inspect.v1` and `frontcomposer.cli.migrate.v1`.
- MCP identifiers: tool name `{BoundedContext}.{CommandType}.Execute`, lifecycle tool `frontcomposer.lifecycle.subscribe`, resource URIs `frontcomposer://{bounded-context}/projections/{projection-name}` and `frontcomposer://skills/{id}`; opaque tokens and their exact guarantee per FR-19.
- Schema fingerprint algorithms `frontcomposer.schema.sha256.canonical-json.v1` and `frontcomposer.schema.sha256.v1.sourcetools-blob`.
- Release evidence manifest `hexalith.release-evidence.v4`, publication candidate `hexalith.publication-candidate.v1`, verification handoff `hexalith.release-verification-handoff.v3`, release ledger `frontcomposer.release-ledger-record.v2`, and dependency graph `hexalith.dependency-graph.v1`.
- `_bmad-output/project-docs/api-contracts.md` (2026-06-02) is superseded by `_bmad-output/contracts/*` and `architecture.md`; it is provenance only. `LEGACY-FR-*` / `LEGACY-NFR-*` identifiers in `epics.md` are provenance only and must keep their prefix in any new trace.

## 12. Decision And Gate Register

| ID | Decision or gate | Owner | Default / current state | Blocks |
| --- | --- | --- | --- | --- |
| D-1 | Canonical PRD path | Product Owner | Amended 2026-09-08: `_bmad-output/planning-artifacts/prd.md` is the only source of record; `prd-addendum-2026-09-08.md` is its addendum; the 2026-07-05 run copy is archived and immutable. | No §0.1 gate. |
| D-2 | Architecture and UX discovery | Product Owner | Resolved: `architecture.md` and `ux-design.md` are canonical planning sources and the overflow homes for mechanism and visual rules; `project-docs` is provenance. | No §0.1 gate. |
| D-3 | Generated command route family | Product + Architecture | Resolved 2026-07-05: `/commands/{BoundedContext}/{CommandTypeName}`; contract `_bmad-output/contracts/fc-route-generated-command-route-contract-2026-07-05.md`. | No §0.1 gate; regression evidence only. |
| D-4 | FC-NIP row identity payload source | Product + Architecture | Decision recorded 2026-08-12 (successor contract); composition delivered by Stories 9.3–9.8 with live proof 2026-08-27. Non-goal: server-allocated keys (DW-679). Residual: 9.8 deferred AppHost-fallback rebuild. | G-5 (approval record). |
| D-5 | Contracts kernel split release posture | Architecture + PM | Resolved and delivered (Stories 11.8, 11.11–11.14). | No §0.1 gate; regression evidence only. |
| D-6 | FR-24 release model and ownership | Release Owner | Amended 2026-09-09 to current execution truth: unsigned candidates remain repository-signed by NuGet.org and the target evidence manifest is `hexalith.release-evidence.v4`, but `.github/workflows/release.yml` currently selects the legacy reusable job and its `HEXALITH_RELEASE_PUBLISH_ENABLED` gate. Exact-SHA dispatch and protected-environment approval are controls, not proof of candidate/credential isolation. REL-AI-1 remains post-publication audit; the target publication boundary and halt posture are D-16/G-8. | G-1, G-8. |
| D-7 | Success metric targets and adopter fallback | Product Owner + Release Owner | Amended 2026-09-09: §9 states each metric's evidence and state. If Tenants cannot produce G-6 evidence, Product chooses Parties as substitute adopter or holds the milestone; Parties inherits the identical evidence obligation and a hold leaves G-6 open. | G-6. |
| D-8 | UX artifact shape and completeness | Product + UX | Amended 2026-09-09: the three-file authority chain is `_bmad-output/planning-artifacts/ux-design.md` (canonical), `_bmad-output/planning-artifacts/ux-design-detailed-2026-07-05.md` (visual supplement), and `_bmad-output/planning-artifacts/ux-experience-2026-07-05.md` (behavior/journey supplement). FC-IA-1 is supporting decision history. This shape is sufficient but not handoff-ready; OI-16 repairs the 2026-09-09 gaps before re-approval. | G-4 through OI-16. |
| D-9 | PRD status approval | Product Owner | Resolved 2026-07-05 for D-1…D-8. **Re-approval pending** for the 2026-09-09 register after OI-4, OI-10, OI-16, OI-19, and the digest-bound postfix reviewer gate close. Document approval never authorizes publication or the milestone. | G-4. |
| D-10 | Built-in analyzer target and activation | Architecture + Product + Release Owner | Resolved 2026-07-16 and **delivered**: `AnalysisMode=Recommended` active since Story 11.23 (2026-08-08). | No §0.1 gate. |
| D-11 | Shared-catalog compatibility and dependency provenance | Architecture + Product + Release Owner | Ratified 2026-07-19; corrected 2026-09-09. The owner-accepted BUILD-REL-1 lineage anchor/predecessor is `a8a50859…`; the future owner-accepted split-reusable revision remains open. The execution pin is `4eb33928…`; current root gitlink is `a32cb422…`; the 2026-09-08 identity/Pact evidence remains bound to prior gitlink `35c3d1e5…`. The graph/catalog core remains implemented, but GOV-1 implementation conformance is open. `eng/dependency-graph-policy.json` is executable authority; the parent/source projection was reconciled on 2026-09-09 without closing Product/Release acceptance. | G-2, G-8. |
| D-12 | EventStore runtime identity | EventStore maintainer + FrontComposer maintainer + Release Owner | Amended 2026-09-09. EventStore `3.103.0` / source `059f6a89…` passed live Pact evidence on 2026-09-08 at Builds provenance `35c3d1e5…`; the current root gitlink is `a32cb422…`. Owners must recapture/extend evidence or approve a semantic-compatibility rule, then the EventStore maintainer signs `migrationApprovalClaimed: true`. If approval is to become FrontComposer-owned, Product and Architecture first record the separate OI-18 ownership transfer; role equivalence is never inferred. Rollback remains rejected. | G-3. |
| D-13 | Fluent UI Blazor v5 RC posture | Product + Architecture | Amended 2026-09-09. The RC posture is proposed, not accepted, pending OI-4 and current-gitlink reconciliation. Versions remain catalog-owned; documented component/API names must resolve against the selected pin. A breaking Fluent update is a FrontComposer major with conformance and accessibility/visual evidence; GA triggers re-decision. | G-4 through OI-4. |
| D-14 | MCP schema side-effect classes, permissive gates, resource disclosure | Architecture + Security reviewer | New 2026-09-08: `Exact`, `CompatibleAdditive`, `CompatibleWarning` are approved side-effect-allowed classes; all other kinds block. `AllowAll*` gates are forbidden in non-Development hosts; enforcement is undelivered (OI-3) and therefore gated by G-7, not treated as a baseline. `resources/list` catalog disclosure, the unregistered-URI SDK error, and the `tools/list` credential oracle are disclosed in FR-19 and require a dated sign-off by a security reviewer other than the PRD author (OI-2) — they are not pre-accepted. | G-7 (OI-3); OI-2 sign-off. |
| D-15 | HFCM9002 production emission | Product + Framework maintainer | Resolved 2026-07-05 (Story 10.4): production emission not approved; synthetic/manual sidecar evidence only. Contract: `_bmad-output/contracts/hfcm9002-production-emission-decision-2026-07-05.md`. | No §0.1 gate. |
| D-16 | Publication privilege boundary and incident posture | Architecture + Release Owner + Product Owner | Amended 2026-09-09: the GOV-1 spine has adopted AD-19's secretless builder and pinned candidate-free data-only publisher, and companion sources now project it. The interim posture halts production releases, names `HEXALITH_RELEASE_PUBLISH_ENABLED=false` as the deny-only emergency stop, and authorizes no current bounded risk exception. Product/Release acceptance, the incident runbook, acknowledgement/containment target, and implementation gate remain pending. The caller cannot select the new mode before an owner-accepted Builds revision enters AD-16 lineage. | G-8. |

### 12.1 Question Disposition

**Closed:** D-1, D-2, D-3, D-5, D-10, D-15; FC-NIP composition (D-4 decision and delivery); analyzer activation; author-signing requirement (dropped); rollback of the EventStore identity (rejected in D-12). D-8's artifact shape is retained, but completeness is reopened through OI-16.

**Routed with named owners:** the Hexalith.Builds `BUILD-REL-1` lineage anchor/predecessor was accepted 2026-08-08, while the split-reusable revision and FrontComposer acceptance remain routed through G-2/G-8. EventStore `3.103.0` migration and current-Builds provenance reconciliation are routed through G-3. Publication privilege and incident posture are routed through D-16/G-8.

**Awaiting confirmation (answer proposed, not decided):** OI-1 (Epic 9 acceptance record), OI-4 (D-13 Fluent RC posture), OI-9 (A3 sample-host container), OI-12 (Product/Release acceptance of D-16 and its halt posture).

**Accepted residuals:** DW-679 and the 9.8 AppHost-fallback deferred items; MCP timing side channels out of scope. Nothing about `resources/list` or the `tools/list` credential oracle is accepted until OI-2 is signed.

### 12.2 Open Items

No open item carries a default; each names the decision or artifact that closes it.

| ID | Item | Owner | Unblock condition | Gate |
| --- | --- | --- | --- | --- |
| OI-1 | Record Product acceptance of the Story 9.8 proof packet; close `epic-9` and E9-AI-1…6 in `sprint-status.yaml` or rewrite them as residuals. | Product Owner | Dated record plus sprint-status update. | G-5 |
| OI-2 | Security sign-off, by a named reviewer other than the PRD author, of every remaining FR-19 disclosure/oracle: static or gated `resources/list`, unregistered-URI SDK error, `tools/list` credential-validity signal, and host-authentication requirement. A gated-list story may change the residual set but cannot replace the dated independent disposition. | Security reviewer | Dated sign-off after the final G-7 behavior and evidence are available. | G-7 |
| OI-3 | Story for the non-Development ban on `AllowAllMcpTenantToolGate` / `AllowAllResourceVisibilityGate` (startup check or analyzer), the endpoint-authentication check, the SDK-dispatch unregistered-URI test, and the SM-4 audit class; names the test class for G-7. | Framework maintainer | Story done; test class named in G-7. | G-7 |
| OI-4 | Product decision on the D-13 Fluent RC posture. | Product Owner | D-13 marked confirmed or revised. | G-4 |
| OI-5 | Capture the Tenants bootstrap evidence at `_bmad-output/implementation-artifacts/tests/tenants-bootstrap-acceptance.md`; if Tenants cannot, invoke D-7. A Parties fallback must produce equivalent dated, candidate-SHA-bound three-call/bootstrap/projection/command evidence; holding the milestone leaves the item open. | Product Owner + selected adopter maintainer | Equivalent adopter proof exists. | G-6 |
| OI-6 | Sync `epics.md` (Epic 11 table, Story 11.24 status) and `architecture.md` Epic 11 section with §8.2; close `epic-11` in `sprint-status.yaml`. | Framework maintainer | Documents updated. | — |
| OI-7 | Keep the reconciled FC-DEP-1, G2 request, GOV-1 story, and parent architecture aligned with the spine and the four distinct Builds identities: owner revision `a8a50859…`, execution pin `4eb33928…`, current gitlink `a32cb422…`, and prior evidence provenance `35c3d1e5…`. | Architecture | Reconciled 2026-09-09; reopen on source drift. Product/Release acceptance remains OI-12/OI-13. | — |
| OI-8 | Adopt the append-only attempt-keyed ledger schema/classifier, then write permanent downloaded-byte-backed rows and dispositions for v4.2.0, v4.3.0, and v4.4.0; correct stale "unauthorized" sentences without relabelling incidents. | Release Owner + Architecture | State model accepted; rows and dispositions exist. | G-1, G-8 |
| OI-9 | Release Owner confirmation of A3 (UI sample host container is local/e2e only). | Release Owner | Confirmed or corrected in §4/§11. | — |
| OI-10 | Correct `docs/fluent-ui-v5-contingency.md` (cites an rc.2 pin) to the catalog-owned pin, or record a dated Product-owned exception with expiry and revisit condition. | Framework maintainer + Product Owner | Doc updated or exception accepted. | G-4 |
| OI-11 | Advance `PUBLISHED_BASELINE_VERSION` after each publication (currently `4.3.0` with v4.4.0 tagged) or document the intended one-release lag. | Release Owner | Constant or docs updated. | — |
| OI-12 | Complete D-16 acceptance: Product and Release Owner accept adopted AD-19, the production-release halt, and `HEXALITH_RELEASE_PUBLISH_ENABLED=false` as the deny-only emergency stop. No current exception exists; any future bounded risk exception requires a separate dated Product + Release + Architecture decision naming scope, expiry, evidence, and compensating controls. | Architecture + Release Owner + Product Owner | Dated acceptance cites AD-19, the current caller, and the halt/no-exception posture. | G-8 |
| OI-13 | Obtain Product/Release acceptance of the 2026-09-09 reconciled `architecture.md`, FC-DEP-1, GOV-1 story, and G2 request without weakening publisher-exclusive authorization, normalized-ZIP equivalence, attempt-state, evaluator identity, incident recovery, or the split implementation gate. | Product Owner + Release Owner | Dated acceptance cites the reconciled sources and finalized spine. | G-8 |
| OI-14 | Implement all eight accepted closure bundles with delayed activation: (1) owner-accepted split reusable contract, (2) caller switch and exact two-job topology, (3) removal of the production environment from candidate/build jobs, (4) production of the publication candidate plus authentication and hostile-candidate fixtures, (5) handoff-v3 and ledger migration, (6) candidate-free final classification and publication, (7) a pinned post-release helper without ambient or candidate helpers, and (8) duplicate destination asset-name rejection. Close or accept dispositions for NC-31, NC-32, and the post-release-helper identity row. | Framework + Builds maintainers | All eight bundles have authenticated evidence; the implementation register has no unresolved Critical/High item and the caller uses the accepted mode. | G-2, G-8 |
| OI-15 | Publish `docs/release-incident-response.md`. Before any retry, the runbook must require acknowledgement and recorded containment, disable the publish gate, preserve immutable evidence, rotate possibly exposed credentials, and inventory every external effect. It must prefer unlisting and permit correction only under a new version. Re-enable only after documented cause and remediation, required rotation, complete external-effect inventory, independent verification, and Release Owner approval. | Release Owner + Security | Runbook and targets are approved and exercised or table-topped. | G-8 |
| OI-16 | Repair the three-file UX contract and evidence: deterministic state announcements; accessible validation/rejection; route/tab/palette/dialog focus; complete state-by-surface coverage; measurable reflow/zoom, text spacing, target size, unobscured focus, forced-colors, and reduced-motion behavior; dead PRD paths; unresolved component/API names; and UJ coverage. | Product + UX + Framework maintainer | Corrected UX sources and PRD IDs pass UX validation with no Critical/High finding; SM-6 evidence exists. | G-4 |
| OI-17 | Re-run the deterministic plus five-lens GOV-1 reviewer gate against the amended architecture and implementation, with authenticated end-to-end run evidence. | Architecture + Release Owner + Security | Gate passes and artifacts are cited by G-2/G-8. | G-2, G-8 |
| OI-18 | If no distinct EventStore maintainer owns the D-12 approval, record a dated Product + Architecture decision transferring approval to a named FrontComposer role and update G-3's required owners; otherwise retain the EventStore-maintainer signature. | Product + Architecture + Release Owner | Ownership record exists before migration approval. | G-3 |
| OI-19 | Complete FR-23 cross-source documentation parity: reconcile the FC-DOC status map and public component index/surface (including page toolbar and page tabs), and classify every migration guide as CLI-supported executable or manual-only package/API while proving catalog/index agreement. | Framework maintainer + Documentation owner | Named component and migration tests enforce complete parity and pass; no unclassified page, public component, catalog edge, or indexed guide remains. | G-4 |

## 13. Assumptions Index

- **A1 (retired):** the Testing harness must cover realistic failure and policy states. **Disposition:** delivered by Story 11.6; now FR-22 text.
- **A2 (retired):** readiness is judged by package-consumer safety and domain-module adoption. **Disposition:** promoted to §4 with Tenants named as the obligated adopter (G-6).
- **A3 (§4, §11):** the `Hexalith.FrontComposer.UI` sample host may be built as a local container image for e2e lanes only; no FrontComposer-owned image is published. **Disposition:** inferred from `eng/release-package-inventory.json` ("container image", not packable) versus the no-containers product statement; confirmation is OI-9.
