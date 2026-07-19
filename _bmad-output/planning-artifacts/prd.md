---
title: Hexalith.FrontComposer Product Requirements Document
status: approved-for-v1-readiness
created: 2026-07-05
updated: 2026-07-19
---

# PRD: Hexalith.FrontComposer

## 0. Document Purpose

This PRD is for Product, Architecture, UX, developer agents, and downstream BMAD story workflows preparing Hexalith.FrontComposer for v1.0/package-release readiness. It consolidates the existing product baseline and the post-MVP backlog into one product requirements source of record. Requirements are source-derived from `_bmad-output/planning-artifacts/*.md` and `_bmad-output/project-docs/*.md`; no authored PRD existed before this draft. The readiness-discoverable canonical copy is `_bmad-output/planning-artifacts/prd.md`; the BMad run copy is `_bmad-output/planning-artifacts/prds/prd-frontcomposer-2026-07-05/prd.md`. Inline assumption callouts are inferred from the brownfield documentation, planning artifacts, or best-judgment scope selection and are indexed in §13.

## 1. Vision

Hexalith.FrontComposer is the Hexalith Blazor Front Shell: a .NET framework that turns annotated domain read models and commands into an operations-ready Blazor UI, an MCP tool/resource surface for AI agents, and developer tooling for inspection, migration, and testing.

The product bet is that domain teams should describe their operational surface once, in code, then get consistent human and AI access paths without hand-building every admin shell. FrontComposer makes the domain type the source of truth and uses source generation, schema fingerprints, and strict governance tests to keep UI, lifecycle state, MCP descriptors, and tooling aligned.

For v1.0 readiness, the product must be more than functional. It must be safe for adopters to package, consume, test, customize, and evolve. That means the existing baseline remains in scope, while post-MVP work focuses on row-level command feedback, tooling-governance cleanup, and architecture-quality remediation that removes known blind spots before package consumers depend on them.

## 2. Target Users

### 2.1 Primary Users

- **Adopter developer** — a developer on a Hexalith domain module such as Tenants, Parties, or future domain packages who wants an admin/operations shell without writing bespoke Blazor scaffolding.
- **Operator** — an authenticated admin or support user who needs to browse projections, understand status, and execute commands safely.
- **AI-agent integrator** — a maintainer exposing the same domain command/projection surface to MCP clients with fail-closed security.
- **Framework maintainer** — a FrontComposer contributor evolving the generator, Shell, MCP server, CLI, Testing package, and public contracts.
- **Release owner** — a maintainer responsible for semantic-release, NuGet package quality, public API baselines, docs validation, and evidence artifacts.

### 2.2 Jobs To Be Done

- Stand up a domain operations shell from annotated C# types and a small DI bootstrap sequence.
- Browse live projection data with accessible, filterable, and status-rich UI.
- Submit commands with lifecycle feedback, confirmation, authorization, and concurrency safeguards.
- Expose generated commands and projections to AI agents without leaking tenant, auth, or schema details.
- Inspect generated output, detect drift, migrate across version edges, and test generated UI in downstream packages.
- Preserve package, public API, and release quality under strict warnings, governance tests, and signed NuGet publication.

### 2.3 Non-Users (v1)

- Teams looking for a generic no-code CRUD builder or a non-Hexalith admin-template marketplace.
- Teams not using .NET, Blazor, Fluent UI, or the Hexalith EventStore-style command/query model.
- Consumer-facing product teams needing highly bespoke marketing or transactional UX rather than operations/admin workflows.
- Mobile-native, desktop-native, or hardware clients.

### 2.4 Key User Journeys

- **UJ-1. Nina boots a domain shell from annotated types.** Nina is adding an operations UI for a Hexalith domain module. She annotates projection and command types, calls `AddHexalithFrontComposerQuickstart()`, `AddHexalithDomain<TMarker>()`, and `AddHexalithEventStore(...)`, then renders `<FrontComposerShell>@Body</FrontComposerShell>`. The shell starts, generated registrations populate navigation, and the empty state is useful even before domain data exists. **Edge case:** if bootstrap calls are missing or misordered, startup fails fast with a named error instead of failing at first render.

- **UJ-2. Marc investigates a live projection.** Marc, an operator, opens the shell home page, sees bounded contexts ordered by urgency, opens a projection, filters a Fluent DataGrid, expands a row detail, and sees loading, empty, stale, reconnecting, slow-query, and max-items states without losing accessibility context. The value lands when Marc can identify the relevant row and trust whether the read model is current.

- **UJ-3. Marc executes a command safely.** Marc opens a generated command form, sees only editable fields, confirms destructive intent when required, submits, and follows the command through Submitting, Acknowledged, Syncing, Confirmed, Rejected, IdempotentConfirmed, or NeedsReview. The value lands when the UI distinguishes accepted command transport from projection-confirmed outcome.

- **UJ-4. Ravi exposes the domain surface to an AI agent.** Ravi hosts the MCP server, registers tenant and resource visibility gates, and allows an agent to discover visible generated tools and projection resources. The agent can call commands only after admission, schema negotiation, argument validation, and server-side injection of controlled fields. **Edge case:** auth, tenant, unknown resource, and schema mismatch failures do not reveal existence or internals.

- **UJ-5. Camille preserves generator/runtime compatibility.** Camille changes a generator contract. She updates diagnostics, generated-output snapshots, schema fingerprints, public API baselines, and CLI inspect/migrate behavior. The value lands when drift is detected intentionally and consumers get a clear migration path rather than silent mismatch.

- **UJ-6. Sophie tests a generated consumer experience.** Sophie writes bUnit tests using the Testing package host, deterministic fakes, evidence recorders, and assertion helpers. [ASSUMPTION A1: the target v1.0 Testing harness must cover success, rejection, timeout, paging/filter/sort, and auth-policy states so downstream modules such as Tenants can test realistic failure UX.]

## 3. Glossary

- **Annotated Domain Type** — a C# type marked with FrontComposer attributes such as `[Projection]`, `[Command]`, or `[BoundedContext]`.
- **Projection** — a read-model type annotated for generated browsing UI and MCP resource exposure.
- **Bounded Context** — the domain boundary that owns related projections and commands; in user-facing
  information architecture it is presented as a **Module**.
- **Module** — the operator-facing workspace for one bounded context. A module owns one primary shell
  entry and exposes its views as module tabs.
- **Module Tab** — a primary view inside a module, encoded by the canonical route `/{module}/{tab}`.
- **Projection Flyout** — secondary navigation that exposes projections without replacing the module
  workspace or its required default tab.
- **Command** — an operation type annotated for generated command form, lifecycle state, registration, and MCP tool exposure.
- **Source Generator** — `Hexalith.FrontComposer.SourceTools`, the Roslyn incremental generator that emits FrontComposer artifacts.
- **Generated Output** — files emitted under `obj/{Config}/{TFM}/generated/HexalithFrontComposer/`; the path is a public contract.
- **Domain Manifest** — generated registration data that describes a bounded context's projections, commands, routes, policies, and MCP descriptors.
- **FrontComposer Shell** — the Blazor runtime frame that composes navigation, layout, projection views, command forms, settings, lifecycle status, and EventStore clients.
- **MCP Surface** — the MCP server tools and resources generated from the same domain descriptors as the human UI.
- **Schema Fingerprint** — deterministic SHA-256 identity over contract material that binds producer and consumers.
- **Drift Baseline** — checked-in snapshot used to detect generated contract drift.
- **Command Lifecycle** — the state path from Idle through Submitting, Acknowledged, Syncing, and terminal outcomes.
- **Pending Command** — bounded local state for accepted commands awaiting projection evidence or terminal status.
- **FC-LYT** — page layout contract for full-width vs constrained content.
- **FC-A11Y** — accessibility primitives and enforcement expectations for generated/custom UI.
- **FC-L10N** — shell-vs-domain localized-string ownership.
- **FC-DOC** — component documentation contract.
- **FC-CMD** — command lifecycle identity, correlation, status, and reconciliation contract.
- **FC-CNC** — one-at-a-time command execution policy.
- **FC-NIP** — new-item producer contract for row-level fresh-item indicators.
- **Fluent UI v5 Policy** — project-wide requirement to use FrontComposer or Fluent UI Blazor v5 components and Fluent 2 tokens for interactive UI.

## 4. Product Form Factor

FrontComposer is a developer product distributed as signed NuGet packages and a `frontcomposer` .NET tool. It includes a source-generator/analyzer, Blazor component library, MCP server library, CLI, Testing package, sample host, and documentation/skill corpus. It is not a hosted SaaS and does not ship FrontComposer-owned containers. [ASSUMPTION A2: v1.0 readiness is judged primarily by package-consumer safety and Hexalith domain-module adoption, not by a public web launch funnel.]

## 5. Features And Functional Requirements

### 5.0 Requirement Status Map

Status labels are part of each requirement's downstream contract.

| FR | Status | Owner / gate |
| --- | --- | --- |
| FR-1 to FR-12 | Baseline / release verification | Framework maintainer verifies generated projection, shell, grid, freshness, and realtime behavior do not regress. |
| FR-13 | Complete / release verification | Product + Architecture approved the FC-NIP payload source on 2026-07-05; Stories 9.1 and 9.2 are done and their contract/runtime evidence remains the release baseline. |
| FR-14 to FR-23 | Baseline / release verification | Framework maintainer verifies command lifecycle, MCP, CLI, Testing, and documentation surfaces remain covered. |
| FR-24 | Release governance / publication gate | Release Owner proves the exact NuGet/GitHub artifacts were inventory- and consumer-validated, signed, timestamped, verified, checksummed, and bound with the complete defined dependency graph, immutable dependency policy, and authenticated CI/release workflow provenance in a valid sealed v2 manifest before classification and publication. |
| FR-25 | Baseline plus change-control gate | Framework maintainer owns public API, schema, CLI JSON, generated-output, diagnostic compatibility, and the staged built-in-analyzer policy/burn-down/activation evidence in Stories 11.20–11.23. |
| FR-26 | Complete / release verification | Epic 9 is done; the approved FC-NIP payload and producer/consumer evidence remain the baseline. |
| FR-27 | Complete / release verification | Epic 10 is done; its tooling-governance and Testing-redaction evidence may be consumed by later remediation. |
| FR-28 | Complete decision records | Epic 11.0 and 11.8 are done; completed dependent delivery retains the recorded contracts. |
| FR-29 | Active release-readiness program | Epic 11 remains organized into four workstreams. Story 11.17a is done; 11.17b–d, 11.18a–c, and 11.19a–d are in review. The approved Story 11.19d decision materialized 11.20–11.23 as sequential, separately approval-gated backlog phases, with 11.23 required before v1.0 publication authorization. |

### 5.1 Source Generation And Contract Vocabulary

**Description:** Adopter developers write Annotated Domain Types. The Source Generator emits Blazor views, command forms, Fluxor state, DI registrations, MCP manifests, diagnostics, and drift material. Generated Output is not hand-edited.

#### FR-1: Generate projection artifacts

For each valid `[Projection]` type, the Source Generator must emit a projection view, Fluxor feature/actions/reducers, and registration artifacts.

**Consequences:**
- A valid projection produces the documented five-file set under the public Generated Output path.
- A non-`partial` projection produces HFC1003 and fails under warnings-as-errors.
- Generated projection views handle Loading, Empty, and Data states according to `ProjectionRole`.

#### FR-2: Generate command artifacts

For each valid `[Command]` type, the Source Generator must emit command form, lifecycle, renderer, registration, subscriber, bridge, and optional full-page route artifacts.

**Consequences:**
- A command with no public parameterless constructor fails with HFC1009.
- A command missing `MessageId` fails with HFC1006.
- Full-page density emits a route host; inline and compact densities do not.

#### FR-3: Honor the attribute vocabulary

FrontComposer must support the documented vocabulary: projection roles, bounded contexts, badges, column priority, field groups, empty-state CTA, destructive confirmation, policy requirements, derived fields, icons, relative time, currency, display metadata, defaults, and projection templates.

**Consequences:**
- Unsupported or invalid attribute use emits the corresponding HFC diagnostic.
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
- Structural drift emits HFC1065; metadata drift emits HFC1066.
- Canonical schema material remains deterministic and bounded; encoder, sentinel, comparer, and baseline identity are treated as load-bearing.

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
- `Ctrl+,` opens settings and `Ctrl+K` opens the command palette.
- The framework-owned account menu is always rendered so adopter header customization cannot remove auth access.

#### FR-9: Manage layout, theme, density, and localized shell strings

The Shell must provide FC-LYT layout modes, shell-owned localized strings, and persisted theme/density preferences.

**Consequences:**
- Full-width is the default layout and constrained layout caps content at the documented max measure.
- Settings changes persist through `IStorageService` and update `data-fc-density`.
- Shell chrome strings resolve from shell resources; domain strings remain host/domain-owned.

### 5.3 Projection Operations Experience

**Description:** Operators browse generated projection pages with registry-driven discovery, Fluent DataGrid behavior, accessible status and detail states, and EventStore-backed query/realtime updates.

#### FR-10: Provide registry-driven discovery

The Shell must generate navigation, home directory cards, command palette entries, projection routes, badges, and counts from Domain Manifest data.

**Consequences:**
- Each bounded context appears to operators as one Module with one primary shell entry and one required
  default Module Tab; primary tab routes use `/{module}/{tab}`.
- Projection flyouts are secondary navigation and do not replace the module workspace or default tab.
- Navigation keeps exactly one active item.
- Home directory supports progressive empty/loading/data states and urgency ordering.
- Command palette search remains keyboard-accessible and authorization-aware.
- Generated command activation uses `/commands/{BoundedContext}/{CommandTypeName}`.

#### FR-11: Render projection grids and states

Generated projection pages must provide filtering, empty/loading states, status indicators, expand-in-row details, column prioritization, slow-query notices, and max-items notices.

**Consequences:**
- Column filters are debounced and resettable.
- Row detail regions remain accessible and announce filter-hidden expanded rows.
- Wide projections activate column prioritization when thresholds are met.
- Status values render as semantic icon-plus-text affordances with tooltip and `aria-label` support; color is never the only signal.

#### FR-12: Maintain projection freshness and realtime behavior

The Shell must query EventStore over HTTP and subscribe to projection changes over SignalR while surfacing reconnect/reconciliation state.

**Consequences:**
- Reconnect and fallback polling states are visible to operators.
- Projection updates do not treat SignalR nudges as proof of command success.
- Epic 11 realtime resilience remediation is release-readiness work when a long-lived circuit can permanently degrade after reconnect failure.

#### FR-13: Mark fresh rows only through FC-NIP

The product must not infer row-level fresh indicators from projection nudges that lack row identity. FC-NIP owns the row identity payload and producer wiring.

**Consequences:**
- `FcNewItemIndicator` remains a confirmed component.
- Automatic row marking uses only the approved FrontComposer-owned pending-command row metadata populated from generated grid/command runtime context.
- Story 9.1 recorded the approved row identity payload source in `_bmad-output/contracts/fc-nip-row-identity-producer-contract-2026-07-04.md`; completed Story 9.2 proves the producer/consumer wiring.

### 5.4 Command Authoring, Lifecycle, And Safety

**Description:** Operators submit generated command forms and receive lifecycle feedback that distinguishes transport acceptance from projection-confirmed results.

#### FR-14: Submit commands through generated forms

Generated command forms must validate input, parse supported field types, dispatch commands, and preserve form state on retryable pre-accept failures.

**Consequences:**
- Unsupported field types render placeholders rather than breaking the form.
- Nullable numeric fields compile and round-trip culture-aware formatting.
- `MessageId` is generated as a ULID and reused across pre-accept retry attempts.

#### FR-15: Surface command lifecycle states

The Shell must surface Submitting, Acknowledged, Syncing, Confirmed, Rejected, IdempotentConfirmed, NeedsReview, Warning, and Degraded states.

**Consequences:**
- Accepted HTTP transport is not displayed as projection-confirmed success.
- Polling binds to the confirmed EventStore status endpoint.
- Default deterministic budgets are confirming-to-Degraded after `10_000` ms, status polling every
  `1_000` ms for at most `120_000` ms, zero Epic 3 retries, and exactly one Epic 4 transient retry
  after `250` ms. Configuration changes require focused `FakeTimeProvider` evidence.

#### FR-16: Enforce command safety

Command execution must respect authorization, destructive confirmation, form-abandonment guard, and FC-CNC one-at-a-time execution.

**Consequences:**
- `[RequiresPolicy]` is evaluated before `BeforeSubmit` and again afterward for protected commands.
- The service boundary also enforces authorization through `AuthorizingCommandServiceDecorator`.
- FC-CNC v1 blocks later local submits rather than queueing or batching them, preserves the in-flight
  command, and presents localized, accessible feedback that a later submit was not queued.

### 5.5 MCP Agent Surface

**Description:** The MCP server exposes generated commands and projections to agents using the same domain descriptors while enforcing fail-closed security and schema compatibility.

#### FR-17: Expose generated command tools

Each visible generated command must appear as an MCP tool with descriptor-derived JSON schema and bounded acknowledgement output.

**Consequences:**
- Tools are built dynamically at each `tools/list`.
- Server-controlled fields cannot be accepted from tool input.
- Command invocation injects tenant/user/message/correlation fields server-side.

#### FR-18: Expose projection and skill resources

The MCP Surface must expose tenant-scoped projection resources and the embedded FrontComposer skill corpus.

**Consequences:**
- Projection resource URIs match generated descriptors exactly.
- Skill resources are served only from validated `agent-reference` sections.
- Oversized skill resources fail closed instead of truncating silently.

#### FR-19: Enforce MCP security and compatibility

MCP hosts must register tenant tool and resource visibility gates, negotiate schema fingerprints, and return hidden-equivalent failures for sensitive cases.

**Consequences:**
- Startup throws if required MCP gates are missing.
- Auth failed, tenant missing, unknown resource, and unknown tool cases do not become existence oracles.
- Incompatible schema fingerprints block side effects.
- Epic 11 MCP lifecycle cross-request remediation is v1.0-blocking because lifecycle subscribe/poll is part of the agent contract, not an optional diagnostic.

### 5.6 CLI, Testing, And Adopter Tooling

**Description:** FrontComposer includes developer tooling that makes generated artifacts inspectable, migratable, and testable by downstream packages.

#### FR-20: Provide `frontcomposer inspect`

The CLI must inspect generated output and diagnostics sidecars and report forms, grids, registrations, manifest entries, warnings, and errors.

**Consequences:**
- Output supports text and JSON using `frontcomposer.cli.inspect.v1`.
- Severity filtering and fail flags have deterministic ordering.
- Paths are sanitized when needed.

#### FR-21: Provide `frontcomposer migrate`

The CLI must plan and apply allowlisted Roslyn migrations across supported version edges.

**Consequences:**
- Dry-run is default.
- Apply mode is atomic and refuses unsafe paths, generated output, submodule roots, and out-of-root writes.
- JSON output uses `frontcomposer.cli.migrate.v1`.

#### FR-22: Provide adopter testing support

The Testing package must provide a bUnit host, deterministic command/query/projection fakes, evidence capture, redaction, builders, and assertion helpers.

**Consequences:**
- Public API drift updates `PublicAPI.Shipped.txt` intentionally.
- Evidence output is redacted by default.
- [ASSUMPTION A1: v1.0 Testing must include realistic failure and policy states, not only happy-path command/query outcomes.]

#### FR-23: Maintain component and skill documentation

FrontComposer must keep component docs, diagnostic docs, migration docs, and skill-corpus docs synchronized with the generated and runtime surfaces.

**Consequences:**
- Published docs under `docs/` pass the DocFX validation gate when changed.
- Skill-corpus docs satisfy required front matter and snippet/reference validation.
- Generated/scratch planning docs remain outside `docs/`.

### 5.7 Package Release And Brownfield Remediation

**Description:** v1.0 readiness depends on strict package, public API, release, and remediation quality.

#### FR-24: Ship signed package artifacts with evidence

FrontComposer must publish only the expected NuGet package set, using package artifacts that were signed, timestamped, verified, checksummed, manifest-bound, consumer-validated, and classified as publishable before any NuGet or GitHub Release side effect.

**Consequences:**
- Conventional commits determine version bump.
- The validated package bytes must be identical to the published bytes; rebuilding, repacking, or signing reconstructed packages after publication is not equivalent evidence.
- Package inventory, tests, package-consumer validation, symbols, SBOM, signature/timestamp verification, checksums, sealed-manifest verification, and `classify-release --require-publishable` all run before publication.
- The required `hexalith.release-evidence.v2` manifest seals the complete defined `hexalith.dependency-graph.v1`: every gitlink at the explicit FrontComposer commit (depth 1) and every gitlink in each exact root-selected repository commit (depth 2), with no deeper v1 edges. It also seals every selected Builds commit and raw catalog SHA-256/optional contract marker, the immutable dependency-policy coordinates and digest, and canonical authenticated CI/release workflow provenance. Legacy manifests are audit-only and never publishable or fallback-eligible.
- A blocked/invalid result or `publish_authorized=false` fails the release before any NuGet or GitHub side effect.
- Durable evidence is attached during initial GitHub Release creation or retained in an approved equivalent; a 30-day Actions artifact is supplemental and insufficient by itself.
- Historical releases whose evidence is blocked or invalid remain non-compliant in the release-evidence ledger.
- `REL-AI-1` can be marked done only after a real release records `classification=ready`, `publish_authorized=true`, verified signing/timestamping, a valid sealed manifest, exact published checksums, package-consumer validation, and durable evidence paths.
- `REL-2` delivered the accepted G1 post-publication evidence posture. Live v3.2.2 evidence proved G1 insufficient, so `REL-3` owns the pre-publication correction and historical reconciliation before the next publish-capable release.
- The caller-side REL-4 freeze guard is implemented in `release.yml` and is in review. Live frozen-run evidence remains outstanding, so the source control exists but is not yet accepted as operational proof; publication remains unauthorized. The guard keeps publication disabled by default and permits execution only through the exact Release Owner-controlled condition. Its common Hexalith.Builds counterpart remains upstream work, and neither control may be removed or bypassed before the permanent REL-3/GOV-1 gate is accepted.
- The shared-workflow dependency is the full opt-in governed release contract on Hexalith.Builds (`BUILD-REL-1`): protected release environment, signing secrets, RFC 3161 timestamp input, attestation permissions with a version-aware candidate phase, attestation-bundle handoff to manifest finalization, and no-repack publication. `REL-5` separates Release Owner enablement — production signing identity, certificate custody, timestamp-authority approval, upstream filing, first gated-release authorization, downloaded-asset verification, and `REL-AI-1` closure — from `REL-3` development work.

#### FR-25: Preserve public contracts and deprecation paths

Public API baselines, schema contracts, CLI JSON schemas, generated-output paths, and HFC diagnostics must evolve intentionally.

**Consequences:**
- Breaking public-surface changes update baselines, docs, and migration/deprecation plans.
- New diagnostics use the documented HFC bands and XML docs.
- Schema canonicalization changes are treated as baseline-invalidating.

#### FR-26: Complete FC-NIP producer wiring

FrontComposer must retain the completed row-level fresh-item producer/consumer wiring only through the approved FC-NIP payload source.

**Consequences:**
- Fresh-row indicators are never inferred from SignalR nudges or unrelated projection refreshes.
- The approved payload source is FrontComposer-owned pending-command row metadata populated from generated grid/command runtime context; EventStore status remains a lifecycle/status source by `MessageId`, not row identity.
- Completed Story 9.2 evidence proves runtime metadata and producer/consumer behavior and remains a release regression gate.

#### FR-27: Complete tooling-governance follow-through

FrontComposer must preserve the completed Epic 10 tooling-governance outcomes for evidence, labels, CLI parity, migration-emission decisioning, and Testing redaction.

**Consequences:**
- Evidence reconciliation proves that CLI, diagnostics, migration, Testing, and documentation artifacts agree on current labels and outcomes.
- HFCM9002 migration-emission behavior is decided and documented.
- Testing redaction coverage proves evidence output does not leak support-sensitive data.

#### FR-28: Govern Epic 11 decision gates

Epic 11 delivery follows the recorded route-contract and Contracts split decisions.

**Consequences:**
- Story 11.0 is the closed canonical generated-command-route decision record.
- Story 11.8 is the closed Contracts kernel split, compatibility, public API, and migration decision record.
- Completed Stories 11.7 and 11.11–11.14 retain their decision trace; neither decision record re-enters the queue.

#### FR-29: Remediate architecture-review release risks

FrontComposer must complete the remaining Epic 11 release-readiness remediation children that address runtime blind spots, maintainability, and enforcement before v1.0 release.

**Consequences:**
- Epic 11 is governed through four workstreams: runtime reliability/security; adopter testing/route integrity; contracts/package boundary; and maintainability/enforcement.
- Stories 11.17, 11.18, and 11.19 are nonimplementable decomposition parents. Their named children carry delivery state; Stories 11.20–11.23 are implementable staged-activation phases materialized by the approved 11.19d decision.
- Logging ownership remains exclusive and deterministic: 11.18a security/fail-closed sites, then 11.18c command-lifecycle/projection/polling hot paths, then 11.18b residual Warning/Error/Critical sites.
- The analyzer program executes 11.20 policy/exception ledger → 11.21 product/generator burn-down → 11.22 test/sample burn-down → 11.23 repository activation. Each phase requires separate Architecture/Product approval; 11.23 is a v1.0 publication gate.
- Acceptance criteria for Epic 11 implementation stories use Given/When/Then form before ready-for-dev.

## 6. Cross-Cutting Non-Functional Requirements

- **NFR-1 Build strictness:** .NET 10, `.slnx` only, nullable enabled, centralized package versions, and `TreatWarningsAsErrors=true` are required.
- **NFR-2 Dependency direction:** dependencies point down to Contracts; SourceTools references only Contracts; net10/Fluent-only code in multi-targeted projects is guarded.
- **NFR-3 Accessibility:** generated and hand-authored UI must conform to WCAG 2.2 AA and preserve accessible names, roles, focus, keyboard, live-region, reduced-motion, and forced-colors behavior.
- **NFR-4 Fluent UI governance:** UI uses FrontComposer/Fluent UI Blazor v5 components and Fluent 2 tokens; raw interactive HTML controls and legacy tokens are forbidden except documented carve-outs.
- **NFR-5 Security:** MCP and Shell security fail closed; server-controlled fields are never client-supplied; return paths, storage keys, tenant/user scope, auth state, and API keys require direct tests or documented controls.
- **NFR-6 Privacy and support safety:** UI, logs, telemetry, MCP responses, evidence, and snapshots must not expose raw tokens, JWT payloads, raw EventStore metadata, stack traces, raw event payloads, or unrestricted PII.
- **NFR-7 Schema determinism:** canonical schema material, fingerprint algorithms, baseline identity, and provenance validation are load-bearing public contracts.
- **NFR-8 Reliability:** command lifecycle and projection freshness must expose degraded/reconnecting/fallback states within configured budgets, recover when the backend recovers, and never convert a nudge or HTTP acceptance into confirmed success without projection or status evidence.
- **NFR-9 Performance:** palette scoring, generated rendering, and cache-backed hot paths must stay inside existing benchmark thresholds and cache caps; any threshold change requires benchmark evidence and release-owner approval.
- **NFR-10 Observability:** FrontComposer uses `FrontComposerActivitySource` and sanitized structured logs for operator-relevant failure paths, with tests or snapshots proving tokens, JWT payloads, raw EventStore metadata, raw event payloads, stack traces, and unrestricted PII are absent.
- **NFR-11 Testing:** the v1.0 release gate includes the default solution-level lane with `DiffEngine_Disabled=true`, Governance, Contract, snapshots, PublicAPI baselines, Pact checks, property tests where configured, docs validation, and e2e accessibility/visual lanes required by the changed surface.
- **NFR-12 Release evidence:** signed and timestamped NuGet packages, symbols, SBOM, exact package inventory, consumer validation, checksums, a valid sealed `hexalith.release-evidence.v2` manifest, and `publish_authorized=true` are blocking pre-publication requirements. The manifest binds the exact published bytes, complete defined v1 dependency graph, immutable dependency policy, authenticated CI run/handoff, and active-policy-authorized static CI/release evaluator closures. Its fallback digest binds the graph, policy, and canonical workflow-definition digest. Every Release attempt emits an authenticated verification handoff carrying the original CI candidate; post-release verification ignores the second-hop default-branch SHA and uses an independently authorized closure. Mutable or merely self-recorded workflow/action coordinates cannot authorize publication.
- **NFR-13 Dependency governance:** compatibility is established from versioned semantic shared-catalog profiles and affected-module standalone Release/NuGet restore/build evidence, never historical commit or fingerprint allowlists. `hexalith.dependency-graph.v1` is exactly depth 1-2 as defined in FR-24. Collection reads exact committed objects under the immutable base/before policy, emits deterministic graph diffs, never recursively initializes nested submodules, and fails closed above 4,096 edges, 64 MiB raw `ls-tree` output per owner commit, 1 MiB per committed `.gitmodules` blob, or 4 MiB per catalog blob. A materialized Builds contract tree permits only bounded regular files: at most 16,384 files, 16 MiB per blob, and 256 MiB total; unsafe paths/modes and exceeded limits fail before extraction.

## 7. Constraints And Dependencies

- **Runtime and framework:** .NET 10, C# latest, Blazor, Fluent UI Blazor v5 pinned centrally at `5.0.0-rc.4-26180.1`, Fluxor, Roslyn 5.6.0, ModelContextProtocol SDK, SignalR, OIDC, NUlid.
- **External systems:** Hexalith.EventStore for command/query/projection backend; Hexalith.Tenants and other Hexalith domain modules as key adopters.
- **Repository policy:** root-declared submodules under `references/` only; never recursive submodule initialization; never modify submodule files without explicit approval.
- **Dependency policy:** one closed, versioned FrontComposer-owned policy defines trusted repository identities/paths, semantic profiles, affected-module argv/evidence-only dispositions, and limits. PR evaluation uses the exact base-commit policy and push evaluation the exact non-zero before-commit policy for both graphs; candidate policy changes cannot authorize themselves and activate only from a later base, apart from the one-time frozen, digest-approved bootstrap. Missing/unknown mappings, profiles, commands, objects, or selected catalogs fail closed. Exact commits and raw fingerprints are provenance, not compatibility allowlists.
- **Published docs:** `docs/` is a CI-gated DocFX site and not scratch space.
- **Generated output:** generated files are not hand-edited; changes flow through SourceTools or Annotated Domain Types.

## 8. MVP And V1.0 Scope

### 8.1 Existing Baseline In Scope

- Shell foundation, bootstrap validation, layout, accessibility, localization, docs, settings, theme, density.
- Read-only projection experience: navigation, home, palette, generated projection rendering, DataGrid states, filtering, detail, realtime update handling.
- Command authoring and lifecycle: generated forms, density, pending identity, polling, budgets, safety, authorization, destructive confirmation, abandonment guard, FC-CNC.
- MCP surface: generated command tools, projection resources, skill corpus, fail-closed gates, schema negotiation.
- Customization levels and override diagnostics.
- CLI inspect/migrate, drift detection, Testing package, public API baselines.
- Aspire-grade visual refresh and Fluent governance policies.

### 8.2 Post-MVP Readiness Program Status

- **Epic 9:** done; FC-NIP decision and producer/consumer wiring are completed evidence.
- **Epic 10:** done; tooling-governance and Testing-redaction outcomes remain reusable evidence.
- **Epic 11 runtime reliability/security:** baseline stories done; 11.18a is in review.
- **Epic 11 adopter testing/route integrity:** done.
- **Epic 11 contracts/package boundary:** decision and delivery Stories 11.8 and 11.11–11.14 are done.
- **Epic 11 maintainability/enforcement:** 11.9, 11.15–11.16, and 11.17a are done; 11.17b–d, 11.18b–c, and 11.19a–d are in review. Stories 11.20–11.23 are sequential, separately approval-gated backlog phases due 2026-07-24 through 2026-09-11; 11.23 is required before v1.0 publication authorization.

### 8.3 Out Of Scope For V1

- Building rich `<AuditTimeline>` or `<ConsequencePreview>` components; approved fallbacks remain.
- Replacing EventStore as the backend integration model.
- Non-Blazor/mobile/native shell surfaces.
- General-purpose no-code CRUD builder behavior.
- Hand-authored domain-specific page bodies for Tenants, Parties, or EventStore Admin beyond what the FrontComposer framework must support.
- Recursive or nested submodule management.

## 9. Success Metrics

**Primary**

- **SM-1: Adopter bootstrap success** — before v1.0, at least one representative Hexalith adopter module, preferably Tenants, boots through the documented three-call path and renders at least one generated projection and one generated command without bespoke framework plumbing. Validates FR-1, FR-2, FR-7, FR-8.
- **SM-2: Release readiness** — before every publish-capable release, the exact artifacts intended for NuGet/GitHub pass inventory, consumer, signing/timestamp, symbol, SBOM, checksum, manifest, test, and readiness gates. Success requires `classification=ready`, `publish_authorized=true`, matching published artifact hashes, and durable evidence paths. Validates FR-24, FR-25.
- **SM-2a: Dependency provenance** — every publish-capable release seals and live-verifies the complete defined depth-1/2 `hexalith.dependency-graph.v1`, immutable active policy, and authenticated immutable CI/release workflow provenance. Compatible pointer advances pass semantic-profile and affected-module gates without product-test commit/fingerprint allowlist edits; malformed, incomplete, drifted, over-limit, or legacy evidence cannot authorize publication. Validates FR-24, NFR-12, and NFR-13.
- **SM-3: Contract drift visibility** — intentional generator or schema changes update baselines, diagnostics, migration/deprecation artifacts, or release notes; accidental generated-output or schema drift is caught by HFC1065/HFC1066, snapshots, or package/public API validation before release. Validates FR-6, FR-20, FR-21, FR-25.
- **SM-4: MCP fail-closed coverage** — tests cover missing gate startup failure, hidden-equivalent auth/tenant/unknown failures, schema mismatch blocking side effects, server-controlled field injection, lifecycle subscribe/poll behavior, and tenant-scoped projection resource access. Validates FR-17, FR-18, FR-19.

**Secondary**

- **SM-5: Testing harness usefulness** — adopter tests can simulate command success, rejection, timeout/stall, authorization denial, paging/filter/sort, and redacted evidence using the FrontComposer Testing package. Validates FR-22.
- **SM-6: UX governance stability** — release checks report zero new raw interactive controls, legacy Fluent tokens, unlinked CSS, dead scoped-CSS patterns, or accessibility-critical regressions in governed UI surfaces. Validates FR-8, FR-11, NFR-3, NFR-4.

**Counter-metrics**

- **SM-C1: Generated file count is not a success metric.** More generated artifacts are acceptable only when they reduce adopter work without weakening contract clarity.
- **SM-C2: Visual polish cannot outrank contract safety.** UI refinement must not bypass accessibility, public API, or package-consumer constraints.
- **SM-C3: CLI output volume is not a success metric.** Inspect/migrate output should be actionable, bounded, and sanitized, not exhaustive by default.

## 10. Risks And Mitigations

- **Risk: PRD traceability started from reverse-engineered artifacts.** Mitigation: keep source intake explicit, mirror this PRD to `_bmad-output/planning-artifacts/prd.md`, and reconcile it against `epics.md`, project docs, approved correction proposals, and readiness reports before finalization.
- **Risk: Epic 11 planning drifts behind delivery.** Mitigation: workstream and child-story status is reconciled against sprint artifacts; parents 11.17–11.19 never carry queue state; readiness is rerun after each canonical correction batch.
- **Risk: Contracts kernel leaks UI/runtime dependencies into consumers.** Mitigation: the approved Contracts/Contracts.UI split and Stories 11.11–11.14 are completed; package-consumer, public API, compatibility, and release-inventory evidence remain regression gates.
- **Risk: MCP lifecycle and projection realtime issues create silent degradation.** Mitigation: classify cross-request MCP lifecycle and SignalR reconnect remediation as release-readiness work, not optional cleanup.
- **Risk: Workflow success is confused with release readiness.** A green evidence workflow can contain `classification=blocked` and `publish_authorized=false`, as observed for v3.2.2; mutable reusable/action refs can also execute code different from the reviewed definition. Mitigation: authenticate the exact successful main-push CI run/head and its single canonical dependency handoff; require 40-hex-pinned CI and release reusable workflows plus their transitive action closure; seal their canonical definition digest; include it in fallback invalidation; and fail before publication on any metadata, provenance, manifest, or classification mismatch.
- **Risk: Gitlink identity is confused with shared-catalog compatibility.** Exact-SHA unit-test pins create false-red Governance failures on legitimate pointer advances while covering only selected graph edges. Mitigation: validate every Builds selector inside the complete defined depth-1/2 v1 graph against its closed semantic profile, graph-diff pointer changes, run each statically mapped affected module at most once with the exact bounded Builds contract tree, and seal exact graph/catalog identities solely as provenance.
- **Risk: UX requirements remain too compact for visual stories.** Mitigation: accept `_bmad-output/planning-artifacts/ux-design.md` as the v1.0 UX traceability artifact and require story-local design notes where layout choices are not already captured.

## 11. API Contracts / Public Surface

- Source-generator input attributes and Generated Output path are public contracts.
- HFC diagnostics are public contract signals and must remain documented.
- CLI JSON schemas `frontcomposer.cli.inspect.v1` and `frontcomposer.cli.migrate.v1` are public output contracts.
- MCP tool/resource schemas and Schema Fingerprints are public interoperability contracts.
- Testing package public API is baseline-locked.
- Release package inventory is an explicit publication contract.
- The approved `Contracts.UI` split is a package/public-API boundary change and must be evidenced before v1.0.
- Breaking changes require versioning, migration/deprecation notes, docs, and baseline updates.

## 12. Decision And Gate Register

| ID | Decision or gate | Owner | Default / current state | Blocks |
| --- | --- | --- | --- | --- |
| D-1 | Canonical PRD path | Product Owner | Resolved: `_bmad-output/planning-artifacts/prd.md` is the readiness-discoverable canonical copy; `_bmad-output/planning-artifacts/prds/prd-frontcomposer-2026-07-05/prd.md` remains the BMad run copy. | None. |
| D-2 | Architecture and UX discovery | Product Owner | Resolved: `_bmad-output/planning-artifacts/architecture.md` and `_bmad-output/planning-artifacts/ux-design.md` are canonical planning sources; `_bmad-output/project-docs` remains source depth. | None. |
| D-3 | Generated command route family | Product + Architecture | Resolved 2026-07-05: canonical generated command route family is `/commands/{BoundedContext}/{CommandTypeName}`; contract recorded in `_bmad-output/contracts/fc-route-generated-command-route-contract-2026-07-05.md`. | Stories 11.0 and 11.7 are done; the contract and e2e pin remain regression evidence. |
| D-4 | FC-NIP row identity payload source | Product + Architecture | Resolved 2026-07-05: approved source is FrontComposer-owned pending-command row metadata populated from generated grid/command runtime context; EventStore status remains lifecycle/status by `MessageId`, not row identity. Contract: `_bmad-output/contracts/fc-nip-row-identity-producer-contract-2026-07-04.md`. | Stories 9.1 and 9.2 are done; no remaining decision or implementation gate. |
| D-5 | Contracts kernel split release posture | Architecture + PM | Resolved and delivered: a netstandard2.0-clean `Contracts` kernel plus net10-only `Contracts.UI` for Blazor/Fluent rendering contracts, with package compatibility, public API, deprecation/migration, inventory, and docs evidence. | Stories 11.8 and 11.11–11.14 are done; regression evidence remains blocking for affected releases. |
| D-6 | FR-24 release-evidence ownership | Release owner | Amended 2026-07-15: REL-2 delivered reusable CI/CD alignment and G1 post-publication evidence. Live v3.2.2 evidence proved G1 insufficient: published packages were unsigned, the manifest was invalid, and readiness was blocked while the evidence workflow succeeded. `REL-3` owns exact-artifact pre-publication enforcement and the affected-release ledger. Truth-state 2026-07-19: REL-4's caller-side `release.yml` freeze guard is implemented and in review; live frozen-run evidence is still outstanding, so publication remains unauthorized. The common upstream freeze and full BUILD-REL-1 governed workflow contract remain blocking. GOV-1 further requires an owner-accepted immutable Builds revision for exact-candidate CI/release handoffs and active-policy-authorized static evaluator closure; accepted revision is pending. REL-5 owns Release Owner enablement and REL-AI-1 closure. | Blocks the next NuGet or GitHub package publication and REL-AI-1 closure. |
| D-7 | Success metric targets | Product Owner + Release owner | Resolved in §9 with minimum v1.0 evidence targets. | None unless targets are changed. |
| D-8 | Standalone UX spec need | Product + UX | Resolved for v1.0: `ux-design.md` is sufficient for traceability; stories with visual/layout choices need story-local design notes. | Blocks only stories whose visual decisions are not captured elsewhere. |
| D-9 | Final PRD status approval | Product Owner | Resolved 2026-07-05: Product approved D-1 through D-8 and the accepted assumption dispositions; PRD status promoted to `approved-for-v1-readiness`. | None. |
| D-10 | Built-in analyzer target and activation | Architecture + Product + Release Owner | Resolved 2026-07-16: target `AnalysisMode=Recommended` through staged Stories 11.20–11.23, preserving `TreatWarningsAsErrors=true`, built-in analyzers only, and narrow owner-bound exceptions. | Story 11.23 and v1.0 publication authorization unless a dated replacement decision provides equivalent diagnostic ownership. |
| D-11 | Shared-catalog compatibility and dependency provenance | Architecture + Product + Release Owner | Resolved and ratified 2026-07-19 by Administrator as Architect + Release Owner: compatibility is semantic-profile plus affected-module Release/NuGet proof; provenance is the exact complete defined depth-1/2 `hexalith.dependency-graph.v1`. The sealed v2 manifest additionally binds the immutable active policy, authenticated CI handoff, and immutable CI/release evaluator/action definition digest. Collection and Builds-tree extraction use the ratified fail-closed ceilings; exact commits and catalog fingerprints never become compatibility allowlists. Contract: `_bmad-output/contracts/shared-catalog-dependency-governance-2026-07-19.md`; spine: `_bmad-output/planning-artifacts/architecture/architecture-gov-1-2026-07-19/ARCHITECTURE-SPINE.md`. | GOV-1 implementation, Story 11.17d promotion, and the next accepted governed release manifest. |

### 12.1 Open Question Disposition

All PRD open questions identified by the 2026-07-05 readiness follow-up are now resolved, routed, or explicitly accepted:

- Canonical PRD path, architecture/UX discovery, generated command route family, Contracts split release posture, release-evidence ownership, success metric targets, and standalone UX source need are resolved in D-1, D-2, D-3, D-5, D-6, D-7, and D-8.
- FC-NIP row identity payload source and producer/consumer wiring are complete under D-4 and Stories 9.1–9.2.
- Final PRD status approval is resolved through D-9; Product approval promoted the PRD to `approved-for-v1-readiness`.
- Built-in analyzer target and activation are resolved through D-10; sequential Stories 11.20–11.23 carry the separately approval-gated implementation, and 11.23 is required before v1.0 publication authorization.
- Shared-catalog compatibility and dependency provenance are resolved through D-11; GOV-1 carries implementation, while BUILD-CAT-1 separately routes the semantic catalog-version marker to Hexalith.Builds.

## 13. Assumptions Index

- **A1 (§2.4, §5.6):** The v1.0 Testing harness must cover realistic failure and policy states, not only happy-path command/query outcomes. **Disposition:** accepted as a v1.0 requirement and routed to FR-22 / Story 11.6 testing-harness failure modes.
- **A2 (§4):** v1.0 readiness is judged primarily by package-consumer safety and Hexalith domain-module adoption, not by a public web launch funnel. **Disposition:** accepted as the v1.0 product-form-factor assumption; validated by SM-1 and SM-2 rather than a separate implementation story.
