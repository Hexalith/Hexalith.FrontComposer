---
title: Hexalith.FrontComposer Product Requirements Document
status: draft
created: 2026-07-05
updated: 2026-07-05
---

# PRD: Hexalith.FrontComposer

## 0. Document Purpose

This PRD is for Product, Architecture, UX, developer agents, and downstream BMAD story workflows preparing Hexalith.FrontComposer for v1.0/package-release readiness. It consolidates the existing product baseline and the post-MVP backlog into one product requirements source of record. Requirements are source-derived from `_bmad-output/planning-artifacts/*.md` and `_bmad-output/project-docs/*.md`; no authored PRD existed before this draft. Inline assumption callouts are inferred from the brownfield documentation, planning artifacts, or best-judgment scope selection and are indexed in §13.

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
- Navigation groups entries by bounded context and keeps exactly one active item.
- Home directory supports progressive empty/loading/data states and urgency ordering.
- Command palette search remains keyboard-accessible and authorization-aware.

#### FR-11: Render projection grids and states

Generated projection pages must provide filtering, empty/loading states, status indicators, expand-in-row details, column prioritization, slow-query notices, and max-items notices.

**Consequences:**
- Column filters are debounced and resettable.
- Row detail regions remain accessible and announce filter-hidden expanded rows.
- Wide projections activate column prioritization when thresholds are met.
- Status values render with the Epic 8 colored-icon status model when appropriate.

#### FR-12: Maintain projection freshness and realtime behavior

The Shell must query EventStore over HTTP and subscribe to projection changes over SignalR while surfacing reconnect/reconciliation state.

**Consequences:**
- Reconnect and fallback polling states are visible to operators.
- Projection updates do not treat SignalR nudges as proof of command success.
- [ASSUMPTION A3: v1.0 readiness requires the Epic 11 realtime resilience fixes before release if the current default reconnect ladder can permanently degrade a long-lived circuit.]

#### FR-13: Mark fresh rows only through FC-NIP

The product must not infer row-level fresh indicators from projection nudges that lack row identity. FC-NIP owns the row identity payload and producer wiring.

**Consequences:**
- `FcNewItemIndicator` remains a confirmed component.
- Automatic row marking is deferred until command outcome context carries `EntityKey` or an approved equivalent.
- Story 9.1 must confirm the payload before Story 9.2 implements producer/consumer wiring.

### 5.4 Command Authoring, Lifecycle, And Safety

**Description:** Operators submit generated command forms and receive lifecycle feedback that distinguishes transport acceptance from projection-confirmed results.

#### FR-14: Submit commands through generated forms

Generated command forms must validate input, parse supported field types, dispatch commands, and preserve form state on retryable pre-accept failures.

**Consequences:**
- Unsupported field types render placeholders rather than breaking the form.
- Nullable numeric fields compile and round-trip culture-aware formatting.
- `MessageId` is generated as a ULID and reused across pre-accept retry attempts.

#### FR-15: Surface command lifecycle states

The Shell must surface Submitting, Acknowledged, Syncing, Confirmed, Rejected, IdempotentConfirmed, NeedsReview, warnings, and degraded states.

**Consequences:**
- Accepted HTTP transport is not displayed as projection-confirmed success.
- Polling binds to the confirmed EventStore status endpoint.
- Numeric budgets for confirming-to-degraded, polling cadence, duration, and retry behavior remain configurable and tested.

#### FR-16: Enforce command safety

Command execution must respect authorization, destructive confirmation, form-abandonment guard, and FC-CNC one-at-a-time execution.

**Consequences:**
- `[RequiresPolicy]` is evaluated before `BeforeSubmit` and again afterward for protected commands.
- The service boundary also enforces authorization through `AuthorizingCommandServiceDecorator`.
- FC-CNC v1 blocks later local submits rather than queueing or batching them.

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
- [ASSUMPTION A4: Epic 11 MCP lifecycle cross-request fixes are v1.0-blocking because lifecycle subscribe/poll is part of the agent contract, not an optional diagnostic.]

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

FrontComposer must release the expected NuGet package set through semantic-release with signed packages, symbols, SBOM, evidence chain, and GitHub Release assets.

**Consequences:**
- Conventional commits determine version bump.
- Release dry-run defaults to safe non-publish behavior.
- Package inventory and readiness classification gate publication.

#### FR-25: Preserve public contracts and deprecation paths

Public API baselines, schema contracts, CLI JSON schemas, generated-output paths, and HFC diagnostics must evolve intentionally.

**Consequences:**
- Breaking public-surface changes update baselines, docs, and migration/deprecation plans.
- New diagnostics use the documented HFC bands and XML docs.
- Schema canonicalization changes are treated as baseline-invalidating.

#### FR-26: Resolve post-MVP hardening backlog

The product must carry Epics 9, 10, and 11 as explicit post-MVP readiness work rather than hiding them inside completed epics.

**Consequences:**
- Epic 9 owns FC-NIP row identity and fresh-row producer wiring.
- Epic 10 owns tooling-governance follow-through: evidence reconciliation, historical label cleanup, CLI parity, HFCM9002 decisioning, and Testing redaction coverage.
- Epic 11 owns architecture review remediation.
- [ASSUMPTION A5: the minimum v1.0 readiness bar includes resolving the 2026-07-05 readiness report's Epic 11 defects: route-contract gate/order contradiction, Story 11.10 split, and narrowing or splitting Stories 11.8 and 11.9.]

## 6. Cross-Cutting Non-Functional Requirements

- **NFR-1 Build strictness:** .NET 10, `.slnx` only, nullable enabled, centralized package versions, and `TreatWarningsAsErrors=true` are required.
- **NFR-2 Dependency direction:** dependencies point down to Contracts; SourceTools references only Contracts; net10/Fluent-only code in multi-targeted projects is guarded.
- **NFR-3 Accessibility:** generated and hand-authored UI must preserve WCAG-relevant names, roles, focus, keyboard, live-region, reduced-motion, and forced-colors behavior.
- **NFR-4 Fluent UI governance:** UI uses FrontComposer/Fluent UI Blazor v5 components and Fluent 2 tokens; raw interactive HTML controls and legacy tokens are forbidden except documented carve-outs.
- **NFR-5 Security:** MCP and Shell security fail closed; server-controlled fields are never client-supplied; return paths, storage keys, tenant/user scope, auth state, and API keys require direct tests or documented controls.
- **NFR-6 Privacy and support safety:** UI, logs, telemetry, MCP responses, evidence, and snapshots must not expose raw tokens, JWT payloads, raw EventStore metadata, stack traces, raw event payloads, or unrestricted PII.
- **NFR-7 Schema determinism:** canonical schema material, fingerprint algorithms, baseline identity, and provenance validation are load-bearing public contracts.
- **NFR-8 Reliability:** command lifecycle and projection freshness must degrade visibly, recover where feasible, and distinguish nudge/acceptance from confirmed state.
- **NFR-9 Performance:** palette scoring and generated UI paths must remain bounded; existing benchmarked hot paths and cache caps remain part of the readiness bar.
- **NFR-10 Observability:** FrontComposer uses `FrontComposerActivitySource` and sanitized structured logs for operator-relevant failure paths.
- **NFR-11 Testing:** default solution-level test lane, Governance, Contract, snapshots, PublicAPI baselines, Pact, property tests, and e2e accessibility/visual lanes remain release gates as applicable.
- **NFR-12 Release evidence:** signed NuGet packages, SBOM, package inventory, readiness classification, checksums, and release manifest evidence are required for publication.

## 7. Constraints And Dependencies

- **Runtime and framework:** .NET 10, C# latest, Blazor, Fluent UI Blazor v5 pinned to the repository's configured version, Fluxor, Roslyn 5.3.0, ModelContextProtocol SDK, SignalR, OIDC, NUlid.
- **External systems:** Hexalith.EventStore for command/query/projection backend; Hexalith.Tenants and other Hexalith domain modules as key adopters.
- **Repository policy:** root-declared submodules under `references/` only; never recursive submodule initialization; never modify submodule files without explicit approval.
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

### 8.2 Post-MVP Backlog In Scope For Readiness

- **Epic 9:** FC-NIP row identity and fresh-row indicator producer/consumer wiring.
- **Epic 10:** tooling-governance follow-through for evidence, labels, CLI parity, migration-emission decisioning, and Testing redaction.
- **Epic 11:** architecture review remediation: token lifecycle, realtime resilience, MCP lifecycle, security-validation tests, dead CSS and visual-conformance guards, Testing harness failure modes, route-contract unification, Contracts kernel split decision, shell layering, and convention alignment.

### 8.3 Out Of Scope For V1

- Building rich `<AuditTimeline>` or `<ConsequencePreview>` components; approved fallbacks remain.
- Replacing EventStore as the backend integration model.
- Non-Blazor/mobile/native shell surfaces.
- General-purpose no-code CRUD builder behavior.
- Hand-authored domain-specific page bodies for Tenants, Parties, or EventStore Admin beyond what the FrontComposer framework must support.
- Recursive or nested submodule management.

## 9. Success Metrics

**Primary**

- **SM-1: Adopter bootstrap success** — a representative Hexalith domain module can boot a shell through the documented three-call path and render at least one generated projection and one generated command without bespoke framework plumbing. Validates FR-1, FR-2, FR-7, FR-8. [ASSUMPTION A6: exact target should be "one working adopter module before v1.0".]
- **SM-2: Release readiness** — CI/release gates for build, Governance, Contract, docs, default tests, package inventory, signing/evidence, and package-consumer validation pass for the release candidate. Validates FR-24, FR-25.
- **SM-3: Contract drift visibility** — intentional generator or schema changes produce updated baselines, diagnostics, or migration/deprecation artifacts; accidental drift is caught before release. Validates FR-6, FR-20, FR-21, FR-25.
- **SM-4: MCP fail-closed coverage** — missing gates, hidden-equivalent failures, schema mismatch, server-controlled field injection, lifecycle subscription, and projection resource access are covered by tests. Validates FR-17, FR-18, FR-19.

**Secondary**

- **SM-5: Testing harness usefulness** — adopter tests can simulate command success, rejection, timeout/stall, authorization denial, paging/filter/sort, and redacted evidence. Validates FR-22. [ASSUMPTION A7: this is a v1.0 readiness metric because Tenants adoption is a named brownfield concern.]
- **SM-6: UX governance stability** — no raw interactive controls, legacy Fluent tokens, unlinked CSS, dead scoped-CSS patterns, or accessibility-critical regressions enter the release lane. Validates FR-8, FR-11, NFR-3, NFR-4.

**Counter-metrics**

- **SM-C1: Generated file count is not a success metric.** More generated artifacts are acceptable only when they reduce adopter work without weakening contract clarity.
- **SM-C2: Visual polish cannot outrank contract safety.** UI refinement must not bypass accessibility, public API, or package-consumer constraints.
- **SM-C3: CLI output volume is not a success metric.** Inspect/migrate output should be actionable, bounded, and sanitized, not exhaustive by default.

## 10. Risks And Mitigations

- **Risk: PRD traceability started from reverse-engineered artifacts.** Mitigation: keep source intake explicit, preserve indexed assumption callouts, and reconcile this PRD against `epics.md`, project docs, and sprint proposals before finalization.
- **Risk: Epic 11 is not implementation-ready as written.** Mitigation: convert the route-contract decision into a pre-epic gate or Story 11.0; split Story 11.10; split/narrow Stories 11.8 and 11.9 before story creation.
- **Risk: Contracts kernel leaks UI/runtime dependencies into consumers.** Mitigation: treat Contracts kernel split as a v1.0 architecture decision with package-compat planning.
- **Risk: MCP lifecycle and projection realtime issues create silent degradation.** Mitigation: classify cross-request MCP lifecycle and SignalR reconnect remediation as release-readiness work, not optional cleanup.
- **Risk: UX requirements remain distributed across epics and architecture notes.** Mitigation: either accept this PRD as the UX requirement index or create a standalone UX spec before final v1.0 readiness review.

## 11. API Contracts / Public Surface

- Source-generator input attributes and Generated Output path are public contracts.
- HFC diagnostics are public contract signals and must remain documented.
- CLI JSON schemas `frontcomposer.cli.inspect.v1` and `frontcomposer.cli.migrate.v1` are public output contracts.
- MCP tool/resource schemas and Schema Fingerprints are public interoperability contracts.
- Testing package public API is baseline-locked.
- Release package inventory is an explicit publication contract.
- Breaking changes require versioning, migration/deprecation notes, docs, and baseline updates.

## 12. Open Questions

1. Should this PRD become the canonical planning artifact referenced by future readiness checks, or should `epics.md` remain the primary planning artifact with this PRD as a synthesis?
2. Should `_bmad-output/project-docs` be included in future readiness discovery configuration so architecture and UX source documents are not falsely reported as missing?
3. Should Epic 11.7 be extracted to Story 11.0 or a pre-epic decision record before any 11.x implementation story starts?
4. What is the approved route family for generated command pages versus palette/CTA command links?
5. What is the approved FC-NIP row identity payload source: EventStore status, command outcome metadata, projection materialization event, or another payload?
6. What exact v1.0 release gate decides whether Contracts kernel split ships before v1.0 or is deferred with explicit breaking-change posture?
7. Which success metric targets should be quantitative before finalization?
8. Should a standalone UX spec be produced, or are the embedded UX-DRs plus this PRD sufficient for downstream work?

## 13. Assumptions Index

- **A1 (§2.4, §5.6):** The v1.0 Testing harness must cover realistic failure and policy states, not only happy-path command/query outcomes.
- **A2 (§4):** v1.0 readiness is judged primarily by package-consumer safety and Hexalith domain-module adoption, not by a public web launch funnel.
- **A3 (§5.3):** Epic 11 realtime resilience fixes are release-readiness work if the current reconnect behavior can permanently degrade long-lived circuits.
- **A4 (§5.5):** Epic 11 MCP lifecycle cross-request fixes are v1.0-blocking because lifecycle subscribe/poll is part of the agent contract.
- **A5 (§5.7):** Minimum v1.0 readiness includes resolving the 2026-07-05 readiness report's Epic 11 defects before implementation.
- **A6 (§9):** A concrete adopter bootstrap target should be one working adopter module before v1.0.
- **A7 (§9):** Testing harness usefulness is a v1.0 readiness metric because Tenants adoption is a named brownfield concern.
