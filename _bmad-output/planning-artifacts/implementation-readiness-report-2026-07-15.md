---
stepsCompleted:
  - step-01-document-discovery
  - step-02-prd-analysis
  - step-03-epic-coverage-validation
  - step-04-ux-alignment
  - step-05-epic-quality-review
  - step-06-final-assessment
status: complete
overallReadiness: NOT_READY
assessmentDate: 2026-07-15
assessor: Codex
inputDocuments:
  prd:
    - prd.md
    - prd-addendum-2026-07-05.md
  architecture:
    - architecture.md
  epics:
    - epics.md
  ux:
    - ux-design.md
    - ux-design-detailed-2026-07-05.md
    - ux-experience-2026-07-05.md
---

# Implementation Readiness Assessment Report

**Date:** 2026-07-15
**Project:** frontcomposer

## Document Discovery

### Selected Assessment Documents

**PRD:**

- `prd.md` (42,996 bytes; modified 2026-07-13)

- `prd-addendum-2026-07-05.md` (3,607 bytes; modified 2026-07-05)

**Architecture:**

- `architecture.md` (7,154 bytes; modified 2026-07-12)

**Epics and Stories:**

- `epics.md` (118,117 bytes; modified 2026-07-13)

**UX Design:**

- `ux-design.md` (4,183 bytes; modified 2026-07-11)

- `ux-design-detailed-2026-07-05.md` (8,428 bytes; modified 2026-07-11)

- `ux-experience-2026-07-05.md` (14,034 bytes; modified 2026-07-05)

### Discovery Resolution

- All required document types were found.

- No sharded `index.md` documents or whole-versus-sharded duplicates were found.

- `sprint-change-proposal-2026-07-05-prd-ai-1.md` and the seven filename-matched `sprint-change-proposal-*epic*.md` files were excluded from the primary assessment set as confirmed by the user.

## PRD Analysis

### Functional Requirements

#### FR-1: Generate projection artifacts

For each valid `[Projection]` type, the Source Generator must emit a projection view, Fluxor feature/actions/reducers, and registration artifacts.

Consequences:

- A valid projection produces the documented five-file set under the public Generated Output path.

- A non-`partial` projection produces HFC1003 and fails under warnings-as-errors.

- Generated projection views handle Loading, Empty, and Data states according to `ProjectionRole`.

#### FR-2: Generate command artifacts

For each valid `[Command]` type, the Source Generator must emit command form, lifecycle, renderer, registration, subscriber, bridge, and optional full-page route artifacts.

Consequences:

- A command with no public parameterless constructor fails with HFC1009.

- A command missing `MessageId` fails with HFC1006.

- Full-page density emits a route host; inline and compact densities do not.

#### FR-3: Honor the attribute vocabulary

FrontComposer must support the documented vocabulary: projection roles, bounded contexts, badges, column priority, field groups, empty-state CTA, destructive confirmation, policy requirements, derived fields, icons, relative time, currency, display metadata, defaults, and projection templates.

Consequences:

- Unsupported or invalid attribute use emits the corresponding HFC diagnostic.

- Server-controlled or derived command fields do not render as editable input.

- Projection badge and status metadata remain accessible, not color-only.

#### FR-4: Apply the command density rule

Command form density is determined by non-derivable property count: `Inline` for 0-1, `CompactInline` for 2-4, and `FullPage` for 5 or more.

Consequences:

- Derivable fields such as `MessageId`, `CorrelationId`, `TenantId`, `UserId`, timestamps, and `[DerivedFrom]` fields are excluded from the count.

- Density behavior is covered by generator tests and snapshots.

- Density thresholds are changed only through an explicit story/ADR.

#### FR-5: Support safe customization levels

Adopters can override generated projection UI through Level-2 templates, Level-3 field slots, and Level-4 full-view overrides.

Consequences:

- Resolution order is deterministic: Level 4, then Level 2, then generated default.

- Level 3 slots compose only when the selected body delegates to generated field/row/section/default renderers.

- HFC1050-HFC1055 cover statically inspectable override accessibility risks.

- Runtime mismatch panels are development-only under DEBUG and `IsDevelopment()`.

#### FR-6: Detect schema and generated-output drift

FrontComposer must bind producer and consumers through Schema Fingerprints and opt-in drift baselines.

Consequences:

- Drift detection compares current generated material to checked-in baseline `AdditionalText` files.

- Structural drift emits HFC1065; metadata drift emits HFC1066.

- Canonical schema material remains deterministic and bounded; encoder, sentinel, comparer, and baseline identity are treated as load-bearing.

#### FR-7: Provide validated DI bootstrap

Adopter apps can wire FrontComposer through `AddHexalithFrontComposerQuickstart()`, optional `AddHexalithDomain<TMarker>()`, and `AddHexalithEventStore(...)`.

Consequences:

- Missing foundational quickstart or misordered calls fail at startup with a named error.

- Empty-shell operation is valid when no domain registrations are present.

- Scoped auth, storage, effects, and tenant accessors must not be captured by singleton services.

#### FR-8: Render the shell frame

The FrontComposer Shell must render a complete Blazor application frame with Fluent layout, skip links, providers, header, navigation, content, footer, and keyboard shortcuts.

Consequences:

- Adopter layout can reduce to `<FrontComposerShell>@Body</FrontComposerShell>`.

- `Ctrl+,` opens settings and `Ctrl+K` opens the command palette.

- The framework-owned account menu is always rendered so adopter header customization cannot remove auth access.

#### FR-9: Manage layout, theme, density, and localized shell strings

The Shell must provide FC-LYT layout modes, shell-owned localized strings, and persisted theme/density preferences.

Consequences:

- Full-width is the default layout and constrained layout caps content at the documented max measure.

- Settings changes persist through `IStorageService` and update `data-fc-density`.

- Shell chrome strings resolve from shell resources; domain strings remain host/domain-owned.

#### FR-10: Provide registry-driven discovery

The Shell must generate navigation, home directory cards, command palette entries, projection routes, badges, and counts from Domain Manifest data.

Consequences:

- Navigation groups entries by bounded context and keeps exactly one active item.

- Home directory supports progressive empty/loading/data states and urgency ordering.

- Command palette search remains keyboard-accessible and authorization-aware.

#### FR-11: Render projection grids and states

Generated projection pages must provide filtering, empty/loading states, status indicators, expand-in-row details, column prioritization, slow-query notices, and max-items notices.

Consequences:

- Column filters are debounced and resettable.

- Row detail regions remain accessible and announce filter-hidden expanded rows.

- Wide projections activate column prioritization when thresholds are met.

- Status values render as semantic icon-plus-text affordances with tooltip and `aria-label` support; color is never the only signal.

#### FR-12: Maintain projection freshness and realtime behavior

The Shell must query EventStore over HTTP and subscribe to projection changes over SignalR while surfacing reconnect/reconciliation state.

Consequences:

- Reconnect and fallback polling states are visible to operators.

- Projection updates do not treat SignalR nudges as proof of command success.

- Epic 11 realtime resilience remediation is release-readiness work when a long-lived circuit can permanently degrade after reconnect failure.

#### FR-13: Mark fresh rows only through FC-NIP

The product must not infer row-level fresh indicators from projection nudges that lack row identity. FC-NIP owns the row identity payload and producer wiring.

Consequences:

- `FcNewItemIndicator` remains a confirmed component.

- Automatic row marking uses only the approved FrontComposer-owned pending-command row metadata populated from generated grid/command runtime context.

- Story 9.1 recorded the approved row identity payload source in `_bmad-output/contracts/fc-nip-row-identity-producer-contract-2026-07-04.md`; Story 9.2 implements and proves the producer/consumer wiring.

#### FR-14: Submit commands through generated forms

Generated command forms must validate input, parse supported field types, dispatch commands, and preserve form state on retryable pre-accept failures.

Consequences:

- Unsupported field types render placeholders rather than breaking the form.

- Nullable numeric fields compile and round-trip culture-aware formatting.

- `MessageId` is generated as a ULID and reused across pre-accept retry attempts.

#### FR-15: Surface command lifecycle states

The Shell must surface Submitting, Acknowledged, Syncing, Confirmed, Rejected, IdempotentConfirmed, NeedsReview, warnings, and degraded states.

Consequences:

- Accepted HTTP transport is not displayed as projection-confirmed success.

- Polling binds to the confirmed EventStore status endpoint.

- Numeric budgets for confirming-to-degraded, polling cadence, duration, and retry behavior remain configurable and tested.

#### FR-16: Enforce command safety

Command execution must respect authorization, destructive confirmation, form-abandonment guard, and FC-CNC one-at-a-time execution.

Consequences:

- `[RequiresPolicy]` is evaluated before `BeforeSubmit` and again afterward for protected commands.

- The service boundary also enforces authorization through `AuthorizingCommandServiceDecorator`.

- FC-CNC v1 blocks later local submits rather than queueing or batching them.

#### FR-17: Expose generated command tools

Each visible generated command must appear as an MCP tool with descriptor-derived JSON schema and bounded acknowledgement output.

Consequences:

- Tools are built dynamically at each `tools/list`.

- Server-controlled fields cannot be accepted from tool input.

- Command invocation injects tenant/user/message/correlation fields server-side.

#### FR-18: Expose projection and skill resources

The MCP Surface must expose tenant-scoped projection resources and the embedded FrontComposer skill corpus.

Consequences:

- Projection resource URIs match generated descriptors exactly.

- Skill resources are served only from validated `agent-reference` sections.

- Oversized skill resources fail closed instead of truncating silently.

#### FR-19: Enforce MCP security and compatibility

MCP hosts must register tenant tool and resource visibility gates, negotiate schema fingerprints, and return hidden-equivalent failures for sensitive cases.

Consequences:

- Startup throws if required MCP gates are missing.

- Auth failed, tenant missing, unknown resource, and unknown tool cases do not become existence oracles.

- Incompatible schema fingerprints block side effects.

- Epic 11 MCP lifecycle cross-request remediation is v1.0-blocking because lifecycle subscribe/poll is part of the agent contract, not an optional diagnostic.

#### FR-20: Provide `frontcomposer inspect`

The CLI must inspect generated output and diagnostics sidecars and report forms, grids, registrations, manifest entries, warnings, and errors.

Consequences:

- Output supports text and JSON using `frontcomposer.cli.inspect.v1`.

- Severity filtering and fail flags have deterministic ordering.

- Paths are sanitized when needed.

#### FR-21: Provide `frontcomposer migrate`

The CLI must plan and apply allowlisted Roslyn migrations across supported version edges.

Consequences:

- Dry-run is default.

- Apply mode is atomic and refuses unsafe paths, generated output, submodule roots, and out-of-root writes.

- JSON output uses `frontcomposer.cli.migrate.v1`.

#### FR-22: Provide adopter testing support

The Testing package must provide a bUnit host, deterministic command/query/projection fakes, evidence capture, redaction, builders, and assertion helpers.

Consequences:

- Public API drift updates `PublicAPI.Shipped.txt` intentionally.

- Evidence output is redacted by default.

- Assumption A1 requires v1.0 Testing to include realistic failure and policy states, not only happy-path command/query outcomes.

#### FR-23: Maintain component and skill documentation

FrontComposer must keep component docs, diagnostic docs, migration docs, and skill-corpus docs synchronized with the generated and runtime surfaces.

Consequences:

- Published docs under `docs/` pass the DocFX validation gate when changed.

- Skill-corpus docs satisfy required front matter and snippet/reference validation.

- Generated/scratch planning docs remain outside `docs/`.

#### FR-24: Ship signed package artifacts with evidence

FrontComposer must release the expected NuGet package set through semantic-release with signed packages, symbols, SBOM, checksums, sealed release manifest/evidence chain, GitHub Release assets, and package-consumer validation evidence.

Consequences:

- Conventional commits determine version bump.

- Release dry-run defaults to safe non-publish behavior and cannot publish package or GitHub Release side effects.

- Package inventory, signing/timestamp verification, symbol package presence, SBOM presence, checksum coverage, manifest verification, release-readiness classification, and package-consumer validation gate publication.

- `REL-AI-1` can be marked done only when the Release Owner records evidence paths for every FR24 artifact or records an approved fallback with explicit reopen criteria.

- Release is triggered by `workflow_run` after a successful `CI` push (Tenants-aligned reusable `domain-release.yml`), not a direct push. Because the shared reusable release workflow provides no evidence hook and cannot be modified from this repo, FR24 evidence is produced by a supplemental FrontComposer workflow (`release-evidence.yml`) reusing `eng/release_evidence.py`, plus package-consumer validation in shared CI. Implemented by `REL-2` (correct-course 2026-07-13); gating posture G1 (post-publish + next-release fail-closed) now, with an optional Hexalith.Builds inline gate (G2) as a durable follow-up.

#### FR-25: Preserve public contracts and deprecation paths

Public API baselines, schema contracts, CLI JSON schemas, generated-output paths, and HFC diagnostics must evolve intentionally.

Consequences:

- Breaking public-surface changes update baselines, docs, and migration/deprecation plans.

- New diagnostics use the documented HFC bands and XML docs.

- Schema canonicalization changes are treated as baseline-invalidating.

#### FR-26: Complete FC-NIP producer wiring

FrontComposer must complete row-level fresh-item producer/consumer wiring only through the approved FC-NIP payload source.

Consequences:

- Fresh-row indicators are never inferred from SignalR nudges or unrelated projection refreshes.

- The approved payload source is FrontComposer-owned pending-command row metadata populated from generated grid/command runtime context; EventStore status remains a lifecycle/status source by `MessageId`, not row identity.

- Story 9.2 must prove complete runtime metadata and producer/consumer behavior before release.

#### FR-27: Complete tooling-governance follow-through

FrontComposer must close the Epic 10 tooling-governance gaps for evidence, labels, CLI parity, migration-emission decisioning, and Testing redaction.

Consequences:

- Evidence reconciliation proves that CLI, diagnostics, migration, Testing, and documentation artifacts agree on current labels and outcomes.

- HFCM9002 migration-emission behavior is decided and documented before release.

- Testing redaction coverage proves evidence output does not leak support-sensitive data.

#### FR-28: Govern Epic 11 decision gates

Epic 11 implementation must not start dependent stories until route-contract and Contracts split decisions are recorded.

Consequences:

- Story 11.0 selects the canonical generated command route family before any Story 11.1+ create-story work starts.

- Story 11.8 records the approved Contracts kernel split decision, package compatibility posture, public API impact, and deprecation/migration plan before Stories 11.11-11.14 start.

- Sprint status and story-creation workflows follow the suggested Epic 11 order rather than naive file order.

#### FR-29: Remediate architecture-review release risks

FrontComposer must complete the Epic 11 architecture remediation stories that address runtime blind spots and architecture boundaries before v1.0 release.

Consequences:

- Token lifecycle, realtime resilience, MCP lifecycle, security-validation tests, visual-conformance guards, Testing harness failure modes, shell layering, helper consolidation, logging, and enforcement-policy alignment each have focused stories or gates.

- Story 11.10 remains split into mechanical one-type-per-file, `LoggerMessage`, and enforcement/policy alignment work; it is not executed as one story.

- Acceptance criteria for Epic 11 implementation stories use Given/When/Then form before ready-for-dev.

**Total FRs: 29**

### Non-Functional Requirements

NFR-1: **Build strictness.** .NET 10, `.slnx` only, nullable enabled, centralized package versions, and `TreatWarningsAsErrors=true` are required.

NFR-2: **Dependency direction.** Dependencies point down to Contracts; SourceTools references only Contracts; net10/Fluent-only code in multi-targeted projects is guarded.

NFR-3: **Accessibility.** Generated and hand-authored UI must preserve WCAG-relevant names, roles, focus, keyboard, live-region, reduced-motion, and forced-colors behavior.

NFR-4: **Fluent UI governance.** UI uses FrontComposer/Fluent UI Blazor v5 components and Fluent 2 tokens; raw interactive HTML controls and legacy tokens are forbidden except documented carve-outs.

NFR-5: **Security.** MCP and Shell security fail closed; server-controlled fields are never client-supplied; return paths, storage keys, tenant/user scope, auth state, and API keys require direct tests or documented controls.

NFR-6: **Privacy and support safety.** UI, logs, telemetry, MCP responses, evidence, and snapshots must not expose raw tokens, JWT payloads, raw EventStore metadata, stack traces, raw event payloads, or unrestricted PII.

NFR-7: **Schema determinism.** Canonical schema material, fingerprint algorithms, baseline identity, and provenance validation are load-bearing public contracts.

NFR-8: **Reliability.** Command lifecycle and projection freshness must expose degraded/reconnecting/fallback states within configured budgets, recover when the backend recovers, and never convert a nudge or HTTP acceptance into confirmed success without projection or status evidence.

NFR-9: **Performance.** Palette scoring, generated rendering, and cache-backed hot paths must stay inside existing benchmark thresholds and cache caps; any threshold change requires benchmark evidence and release-owner approval.

NFR-10: **Observability.** FrontComposer uses `FrontComposerActivitySource` and sanitized structured logs for operator-relevant failure paths, with tests or snapshots proving tokens, JWT payloads, raw EventStore metadata, raw event payloads, stack traces, and unrestricted PII are absent.

NFR-11: **Testing.** The v1.0 release gate includes the default solution-level lane with `DiffEngine_Disabled=true`, Governance, Contract, snapshots, PublicAPI baselines, Pact checks, property tests where configured, docs validation, and e2e accessibility/visual lanes required by the changed surface.

NFR-12: **Release evidence.** Signed NuGet packages, SBOM, package inventory, readiness classification, checksums, and release manifest evidence are required for publication.

**Total NFRs: 12**

### Additional Requirements

#### User-Journey Requirements

UJ-1: Nina can boot a domain shell from annotated types using the documented registration sequence and `<FrontComposerShell>@Body</FrontComposerShell>`; generated registrations populate navigation and the empty state remains useful. Missing or misordered bootstrap calls must fail fast with a named error.

UJ-2: Marc can browse live projection data, see bounded contexts ordered by urgency, filter a Fluent DataGrid, expand row details, and distinguish loading, empty, stale, reconnecting, slow-query, and max-items states without losing accessibility context.

UJ-3: Marc can execute a generated command safely, see only editable fields, confirm destructive intent, and distinguish Submitting, Acknowledged, Syncing, Confirmed, Rejected, IdempotentConfirmed, and NeedsReview, including the distinction between accepted transport and projection-confirmed outcome.

UJ-4: Ravi can expose generated tools and projection resources through MCP only after tenant/resource gates, admission, schema negotiation, argument validation, and server-side controlled-field injection; auth, tenant, unknown-resource, and schema-mismatch failures must not reveal existence or internals.

UJ-5: Camille can change a generator contract while updating diagnostics, snapshots, fingerprints, public API baselines, and inspect/migrate behavior, so drift is intentional and consumers receive a migration path.

UJ-6: Sophie can test generated consumer experiences with the Testing package host, deterministic fakes, evidence recorders, and assertions across success, rejection, timeout, paging/filter/sort, and authorization-policy states.

#### Constraints and Dependencies

- Runtime and framework: .NET 10, C# latest, Blazor, the repository-pinned Fluent UI Blazor v5 version, Fluxor, Roslyn 5.3.0, ModelContextProtocol SDK, SignalR, OIDC, and NUlid.

- External integrations: Hexalith.EventStore is the command/query/projection backend; Hexalith.Tenants and other Hexalith domain modules are key adopters.

- Repository policy: initialize only root-declared submodules under `references/`; never initialize recursively or modify submodule files without explicit approval.

- Published documentation: `docs/` is a CI-gated DocFX site and not scratch space.

- Generated output is not hand-edited; changes flow through SourceTools or annotated domain types.

#### Public Contract Requirements

- Source-generator input attributes and the Generated Output path are public contracts.

- HFC diagnostics are public contract signals and remain documented.

- `frontcomposer.cli.inspect.v1` and `frontcomposer.cli.migrate.v1` are public CLI output schemas.

- MCP tool/resource schemas and Schema Fingerprints are public interoperability contracts.

- The Testing package public API is baseline-locked.

- The release package inventory is an explicit publication contract.

- The approved `Contracts.UI` split is a package/public-API boundary change that requires evidence before v1.0.

- Breaking changes require versioning, migration/deprecation notes, documentation, and baseline updates.

#### Accepted Assumptions

- A1: The v1.0 Testing harness must cover realistic failure and policy states, not only happy-path command/query outcomes. This is accepted and routed to FR-22 and Story 11.6.

- A2: v1.0 readiness is judged primarily by package-consumer safety and Hexalith domain-module adoption, not by a public web launch funnel. This is accepted and validated through SM-1 and SM-2.

#### Scope Boundaries

- Rich `<AuditTimeline>` and `<ConsequencePreview>` components are out of v1 scope; approved fallbacks remain.

- Replacing EventStore, non-Blazor/mobile/native shell surfaces, generic no-code CRUD behavior, domain-specific page bodies beyond framework support, and recursive/nested submodule management are out of scope.

#### Addendum Findings

The PRD addendum introduces no additional numbered functional or non-functional requirements. It records the source inventory, explains that the canonical PRD was reconciled from brownfield and correction artifacts, and explicitly leaves exact commands, file/line architecture findings, detailed Epic 11 implementation splits, public API baselines, snapshots, Pact files, and release evidence in their owning artifacts.

### PRD Completeness Assessment

The PRD is structurally strong for readiness analysis: it has an approved status, a canonical path, 29 consecutively numbered FRs, 12 consecutively numbered NFRs, explicit consequences, status ownership, scope boundaries, success metrics, risks, public-contract declarations, resolved decision gates, and accepted assumption dispositions. The addendum provides clear provenance.

Initial clarity concerns remain:

- Several FRs are broad capability groups whose consequences act as separate acceptance obligations; downstream traceability must therefore cover the consequences, not only the top-level FR label.

- Reliability and performance requirements refer to configured budgets, existing benchmark thresholds, and cache caps without placing the numeric targets in the PRD itself. Readiness depends on traceable owning artifacts and tests for those values.

- The PRD frontmatter says `updated: 2026-07-05`, while FR-24 includes a correction and implementation disposition dated 2026-07-13. The metadata does not reflect the latest material revision.

- The runtime constraint names Roslyn 5.3.0, which requires reconciliation against the repository's current dependency baseline during architecture alignment.

- Some requirements retain story-state language such as “implements,” “must prove,” and “remains” rather than an explicit current verified state. Epic and evidence validation must distinguish planned coverage from completed, test-backed coverage.

## Epic Coverage Validation

### Epic FR Coverage Extracted

The epics document claims coverage for canonical PRD FR-1 through FR-29 when its legacy coverage map, command-subclause traceability addendum, release-governance gate, and PRD V1 Readiness Coverage Addendum are read together.

The document retains an older FR-1 through FR-26 capability inventory whose numbering is not semantically aligned one-to-one with the canonical PRD. For example, legacy FR-3 is the command-density rule while canonical PRD FR-3 is the attribute vocabulary. The matrix below therefore reconciles by requirement meaning and concrete epic/story ownership rather than by matching the legacy number alone.

### Coverage Matrix

| FR Number | Canonical PRD Requirement | Epic and Story Coverage | Status |
| --- | --- | --- | --- |
| FR-1 | For each valid `[Projection]` type, emit the projection view, Fluxor feature/actions/reducers, and registration artifacts. | Epic 2, Story 2.1; generator/diagnostic support in Story 7.3. | ✓ Covered |
| FR-2 | For each valid `[Command]` type, emit form, lifecycle, renderer, registration, subscriber, bridge, and optional full-page route artifacts. | Epic 3, Stories 3.1 and 3.2. | ✓ Covered |
| FR-3 | Support the documented projection, command, display, policy, derived-field, badge, grouping, CTA, icon, formatting, default, and template attribute vocabulary. | Epic 2, Stories 2.1, 2.3, and 2.5; Epic 4, Stories 4.1 and 4.4; Epic 6, Stories 6.1-6.4. | ✓ Covered |
| FR-4 | Apply `Inline`/`CompactInline`/`FullPage` density from non-derivable property count. | Epic 3, Story 3.2. | ✓ Covered |
| FR-5 | Support Level-2 templates, Level-3 field slots, and Level-4 full-view overrides with deterministic precedence and safety checks. | Epic 6, Stories 6.1-6.4. | ✓ Covered |
| FR-6 | Bind producer and consumers through schema fingerprints and opt-in generated-output drift baselines. | Epic 7, Stories 7.3 and 7.4; schema compatibility also appears in Epic 5, Story 5.5. | ✓ Covered |
| FR-7 | Provide validated Quickstart → Domain → EventStore DI bootstrap, fail-fast ordering, empty-shell support, and safe lifetimes. | Epic 1, Stories 1.0 and 1.1; lifetime constraints are cross-cutting and refined by Epic 11. | ✓ Covered |
| FR-8 | Render the complete Fluent shell frame, skip links, providers, header/navigation/content/footer, shortcuts, and persistent framework account access. | Epic 1, Stories 1.1 and 1.3; account-control trace recorded under UX-DR8 change-proposal-of-record; Epic 8 refines chrome. | ✓ Covered |
| FR-9 | Provide layout modes, shell localization ownership, and persisted theme/density preferences. | Epic 1, Stories 1.2, 1.4, and 1.6; Epic 8, Story 8.4 refines default density. | ✓ Covered |
| FR-10 | Generate navigation, home cards, palette entries, routes, badges, and counts from Domain Manifest data. | Epic 2, Stories 2.2 and 2.7; Epic 8, Story 8.5; Epic 11, Stories 11.0 and 11.7 for command route activation. | ✓ Covered |
| FR-11 | Render projection filtering, loading/empty states, status affordances, row details, prioritization, slow-query, and max-items notices. | Epic 2, Stories 2.3-2.5; Epic 8, Stories 8.4 and 8.7 refine grid/status presentation. | ✓ Covered |
| FR-12 | Query EventStore over HTTP, subscribe over SignalR, expose reconnect/reconciliation, and recover realtime behavior without treating nudges as success. | Epic 2, Story 2.6; Epic 11, Story 11.2. | ✓ Covered |
| FR-13 | Mark fresh rows only through the approved FC-NIP row-identity payload and FrontComposer-owned pending-command metadata. | Epic 9, Stories 9.1 and 9.2; ownership boundary also pinned in Story 2.6. | ✓ Covered |
| FR-14 | Validate, parse, dispatch, and safely retry generated command forms, including unsupported fields, nullable numerics, and stable ULID `MessageId`. | Epic 3, Stories 3.1-3.3; Epic 4, Story 4.5; explicit command-subclause traceability table maps each behavior. | ✓ Covered |
| FR-15 | Surface all required lifecycle states and distinguish HTTP acceptance from status/projection-confirmed outcomes under configured budgets. | Epic 3, Stories 3.4-3.6; explicit command-subclause traceability maps IdempotentConfirmed, NeedsReview, Warning, and Degraded behavior. | ✓ Covered |
| FR-16 | Enforce authorization, destructive confirmation, abandonment protection, and one-at-a-time command execution. | Epic 4, Stories 4.1-4.5; command-subclause traceability explicitly maps policy sequencing and service-boundary authorization. | ✓ Covered |
| FR-17 | Expose each visible generated command as a descriptor-derived MCP tool with bounded acknowledgement and server-controlled field injection. | Epic 5, Story 5.1; lifecycle continuation in Story 5.2. | ✓ Covered |
| FR-18 | Expose tenant-scoped projection resources and validated, bounded skill-corpus resources. | Epic 5, Story 5.3. | ✓ Covered |
| FR-19 | Require MCP visibility gates, hidden-equivalent failures, schema negotiation, and side-effect blocking; preserve cross-request lifecycle behavior. | Epic 5, Stories 5.4 and 5.5; Epic 11, Story 11.3. | ✓ Covered |
| FR-20 | Provide deterministic text/JSON `frontcomposer inspect` with filtering, fail flags, diagnostics, and sanitized paths. | Epic 7, Stories 7.1 and 7.3; Epic 10, Story 10.3 for text/JSON parity. | ✓ Covered |
| FR-21 | Provide safe, allowlisted, dry-run-first, atomic `frontcomposer migrate` with versioned JSON output. | Epic 7, Story 7.2; Epic 10, Stories 10.3 and 10.4. | ✓ Covered |
| FR-22 | Provide the adopter Testing host, deterministic fakes, evidence capture/redaction, builders, and assertions across realistic failure and policy states. | Epic 7, Story 7.5; Epic 10, Story 10.5; Epic 11, Story 11.6. | ✓ Covered |
| FR-23 | Keep component, diagnostic, migration, and skill-corpus documentation synchronized and validated. | Epic 1, Story 1.5; Epic 5, Story 5.3; Epic 7, Stories 7.2-7.4; Epic 10, Stories 10.2 and 10.4; Epic 11, Story 11.14. | ✓ Covered |
| FR-24 | Release signed NuGet packages, symbols, SBOM, checksums, sealed manifest/evidence, GitHub Release assets, and consumer-validation evidence. | Release Governance Gate RG-1; action `REL-AI-1`; corrected implementation owner `REL-2` with shared CI, reusable release, and supplemental `release-evidence.yml`. | ✓ Covered by release-governance path |
| FR-25 | Evolve public APIs, schema contracts, CLI schemas, generated paths, and diagnostics intentionally with baselines and migration/deprecation plans. | Epics 7 and 10; Epic 11, Stories 11.8, 11.11-11.14, and 11.19. | ✓ Covered |
| FR-26 | Complete FC-NIP producer/consumer wiring through approved pending-command row metadata and prove runtime behavior. | Epic 9, Story 9.2; explicit PRD FR-26 reference in Story 2.6. | ✓ Covered |
| FR-27 | Close tooling-governance gaps for evidence, labels, CLI parity, HFCM9002 decisioning, and Testing redaction. | Epic 10, Stories 10.1-10.5. | ✓ Covered |
| FR-28 | Enforce the Epic 11 route-contract and Contracts-split decision gates before dependent implementation. | Epic 11, completed decision Stories 11.0 and 11.8; authoritative implementation-order table. | ✓ Covered |
| FR-29 | Remediate architecture-review runtime blind spots and architecture boundaries before release. | Epic 11, Stories 11.1-11.19 and decomposed 11.17a-d, 11.18a-c, and 11.19a-d work. | ✓ Covered |

### Missing Requirements

No canonical PRD functional requirement is absent from the epics document after semantic reconciliation.

There are no orphan epic capabilities that lack a PRD home: legacy standalone entries for MCP/template manifests and HFC diagnostics are represented within the canonical generator, MCP, drift, attribute-validation, and public-contract requirements. However, the legacy numeric map is itself a traceability hazard and should not be used mechanically without the semantic reconciliation above.

### Coverage Statistics

- Total canonical PRD FRs: 29

- Canonical FRs covered in epics, stories, or an explicit release-governance implementation path: 29

- Missing canonical FRs: 0

- Coverage percentage: 100%

## UX Alignment Assessment

### UX Document Status

UX documentation is present and substantial:

- `ux-design.md` is the canonical planning source, status `canonical-planning-source`, updated 2026-07-11. It defines UX-DR1 through UX-DR8 and the core governance rules.

- `ux-design-detailed-2026-07-05.md` is a draft visual/design-system usage specification, updated 2026-07-11.

- `ux-experience-2026-07-05.md` is a draft information-architecture, behavior, state, interaction, accessibility, and journey spine, updated 2026-07-05.

The product is explicitly user-facing, so UX documentation is required; the missing-UX warning does not apply.

### UX ↔ PRD Alignment

Strong alignment exists across the principal operator and adopter journeys:

- The shell bootstrap, complete frame, valid empty shell, shortcuts, settings, layout, theme, density, and framework-owned account access align with PRD FR-7 through FR-9 and UJ-1.

- Registry-driven discovery, projection grids, filtering, row details, loading/empty/stale/reconnecting states, semantic status affordances, and responsive navigation align with FR-10 through FR-12 and UJ-2.

- Generated command forms, editable-field filtering, destructive confirmation, structured lifecycle truth, preserved retryable input, support-safe rejection, and projection-confirmed success align with FR-14 through FR-16 and UJ-3.

- FC-NIP prohibits broad or diff-inferred row marking and requires approved row identity, aligning with FR-13 and FR-26.

- Accessibility, Fluent UI v5, Fluent 2 token governance, reduced motion, forced colors, keyboard access, live regions, and support-safe copy align with NFR-3, NFR-4, and NFR-6.

Alignment gaps or under-specified relationships:

1. **Module workspace/tab IA is stronger than the PRD.** The experience spine requires exactly one primary shell entry per module, route-backed module tabs, `/{module}/{tab}` encoding, and secondary-only projection flyouts. PRD FR-10 says navigation is generated from Domain Manifest data and grouped by bounded context, but it does not state whether “module” and “bounded context” are equivalent or explicitly require the one-entry-per-module/tab model. FC-IA-1 resolves this in UX/contracts and Epic 11, but the canonical PRD does not carry the decision directly.

2. **The UX accessibility target is more precise.** `ux-experience-2026-07-05.md` sets WCAG 2.2 AA for common shell and generated module pages; PRD NFR-3 names WCAG-relevant behavior without an explicit conformance level or version.

3. **Several behavioral rules exist only in the UX spine.** `/` search focus when enabled, `Esc` closing and focus return, one-level modal stacking, required default module tab, and deep-link rules are not explicit PRD requirements.

4. **FR-16 concurrency feedback is absent from UX.** The PRD requires FC-CNC one-at-a-time local command execution, but the UX documents do not specify the blocked-submit message, disabled-state behavior, focus/announcement behavior, or recovery interaction for a second submission.

5. **Timing expectations are indirect.** UX defines visible degraded/reconnecting states, while PRD FR-15/NFR-8 refers to configured budgets and PRD NFR-9 refers to existing thresholds. Neither UX nor PRD centralizes concrete responsiveness or load-time targets for shell startup, search/filter response, palette response, grid loading, or route transitions.

### UX ↔ Architecture Alignment

The architecture supports the main UX needs through:

- `Contracts.UI` ownership of Blazor/Fluent rendering contracts and the preserved typography surface;

- explicit Shell component, Routing, State, Infrastructure, and telemetry boundaries;

- pure route derivation, Fluxor single-writer/scoped-lifetime rules, EventStore truth-state separation, and realtime remediation;

- Fluent UI v5 as the mandatory component system with raw interactive controls forbidden;

- public generated-output/schema contracts and fail-closed MCP/Shell security constraints that support consistent human and agent surfaces.

Architecture alignment gaps:

1. **The canonical architecture delegates its UX policy to a missing section.** It says “UX/layout policy lives in architecture section 4,” but the selected canonical `architecture.md` contains no section 4 and only points to `_bmad-output/project-docs/architecture.md`. The canonical planning artifact therefore does not itself record the module-tab IA, one-entry-per-module rule, `FcPageToolbar`, accordion policy, status tooltip behavior, WCAG 2.2 AA target, or responsive interaction rules.

2. **FC-IA-1 is not represented in the canonical architecture.** The signed route decision `/{module}/{tab}`, required default tab, and secondary-only projection flyout are in the UX experience and contract/story artifacts, but not in the architecture planning source's Routing invariants.

3. **No UX performance architecture is explicit.** The architecture does not map UX responsiveness, loading, filtering, palette, or route-transition targets to caching, debounce, rendering, measurement, or test mechanisms. It supports realtime and state boundaries but does not define measurable experience budgets.

### Warnings

- **Broken UX source links:** both draft UX artifacts use `../../prd.md`, `../../architecture.md`, `../../ux-design.md`, `../../epics.md`, and similarly shaped paths. From `_bmad-output/planning-artifacts`, these resolve to repository-root locations that do not exist, so their declared provenance links are broken.

- **Missing visual source:** `ux-experience-2026-07-05.md` declares that `DESIGN.md` owns visual identity, but no FrontComposer `DESIGN.md` exists. Only referenced submodules contain files with that name. The detailed UX document appears to carry the intended visual rules but is not named or linked as that declared source.

- **Draft authority ambiguity:** the two rich UX spines both have `status: draft`, yet the experience spine states that it wins on conflict with older sources and FC-IA-1 is signed off. Their authority should be made explicit or their accepted decisions should be promoted into the canonical UX/architecture sources.

- **Terminology ambiguity:** “module,” “bounded context,” “projection,” “module workspace,” and “module tab” are used across PRD, epics, UX, and architecture without one shared mapping. This can reintroduce primary-navigation or route-contract divergence despite the FC-IA-1 sign-off.

## Epic Quality Review

### Epic Structure Validation

| Epic | User-value focus | Independence and dependency result | Quality result |
| --- | --- | --- | --- |
| Epic 1 — Shell Foundation & Bootstrap | Valid adopter outcome: bootable, accessible shell. | Stands alone. Brownfield integration spike and bootstrap are appropriate; no future-epic dependency. | Mostly compliant; Story 1.5 contains a completion loophole. |
| Epic 2 — Read-Only Projection Experience | Strong operator outcome and valid read-only MVP. | Uses Epic 1 only. Epic 9 is referenced for deferred row marking, but baseline read-only behavior can function without it. | Mostly compliant; Story 2.6 embeds a future-story ownership statement as an AC. |
| Epic 3 — Command Authoring & Lifecycle | Strong operator outcome. | Uses Epics 1-2 only. | Structurally valid; artifact-count wording and lifecycle-state AC gaps remain. |
| Epic 4 — Safe & Concurrent Command Execution | Strong operator safety outcome. | Backward dependency on Epic 3 is valid. | Structurally valid; policy sequencing and retry criteria need refinement. |
| Epic 5 — AI-Agent (MCP) Surface | Valid AI-agent user outcome, not merely an API milestone. | Relies on existing/generated manifest capability; no future epic is required for the stated baseline. | Mostly valid; Story 5.2 lacks the cross-request scenario later required by Story 11.3. |
| Epic 6 — Customization & Extensibility | Valid adopter customization outcome. | Builds on existing generated UI; no forward dependency. | Mostly valid; one diagnostic-phase AC is technically incompatible with the documented architecture. |
| Epic 7 — Authoring Tooling & Drift Safety | Valid adopter-developer outcome. | Can operate against annotated domains independently of later runtime epics. | Mostly valid; Story 7.5's claimed fault behavior conflicts with later remediation scope. |
| Epic 8 — Aspire-grade Visual Refresh | Valid operator-facing visual and interaction outcome. | Refines completed Epics 1-2; stories are intended to ship independently. | Story 8.5 is oversized; several ACs are vague or incomplete; dependency version is stale. |
| Epic 9 — Fresh-Row Producer and Row Identity | Valid operator outcome. | Backward dependencies on Epics 2-3 are valid; Story 9.2 correctly follows 9.1. | Story 9.2 is well structured; Story 9.1 retains stale decision-branch wording after completion. |
| Epic 10 — Tooling Governance Follow-Through | Outcome is framed as adopter trust, but the epic is organized around unrelated governance debt. | Backward dependency on Epic 7 is valid. | 🟠 Technical/catch-all epic: evidence reconciliation, copy cleanup, CLI parity, migration policy, and redaction do not form one independently consumable user capability. |
| Epic 11 — Architecture Review Remediation | The introduction asserts adopter/operator benefit, but the epic is explicitly organized by architecture-review defect classes. | No forbidden future-epic dependency, but the claimed independence from Epic 10 is weakened by Story 11.6's reliance on Story 10.5 findings. | 🔴 Heterogeneous technical-remediation portfolio, with multiple oversized and explicitly unready stories. It violates the workflow's user-value/cohesion standard. |

### Story Quality and Dependency Findings

#### 🔴 Critical Violations

1. **Epic 11 is a technical remediation portfolio rather than a cohesive user-value epic.** It combines token lifecycle, realtime resilience, MCP lifetime, redirect/storage security, CSS, Testing, routes, package boundaries, layering, helper consolidation, file layout, logging, and analyzer policy. These capabilities have different users, release risks, validation lanes, and independent value. Remediation: split it into outcome-oriented epics or managed technical-risk workstreams, such as runtime reliability/security, adopter testing and route correctness, contract/package compatibility, and maintainability/enforcement.

2. **The canonical epics document does not contain implementation-ready children for 11.17, 11.18, and 11.19.** Each is a decomposition parent that prohibits its own promotion to ready-for-dev and describes children only as bullets. The current sprint artifacts show that full implementation stories now exist for 11.17a-d and 11.18a, while 11.18b-c and 11.19 remain backlog/unmaterialized. FR-29 treats this work as release-readiness scope. Remediation: synchronize the existing child stories and statuses into canonical planning, create the remaining child specifications before promotion, and keep the parents non-implementable.

3. **Release-blocking FR-24 is not represented by a complete story in the canonical epics document.** RG-1, `REL-AI-1`, and `REL-2` describe the path, while the current sprint artifacts show that `rel-2-align-frontcomposer-cicd-with-tenants.md` was created, implemented, reviewed, and marked done on 2026-07-13. `REL-AI-1` remains open until the Release Owner records real-release evidence. Remediation: reference and reconcile the existing `REL-2` story and evidence contract in canonical planning; do not create a duplicate story. Keep the release action open until every FR-24 artifact has an evidence path or approved fallback/reopen rule.

#### 🟠 Major Issues

1. **Canonical FR citations are pervasively stale.** Story-level markers mostly use the legacy epics FR numbering, not the canonical PRD. Examples: Story 1.1 cites FR-9/FR-10 for shell/bootstrap instead of canonical FR-8/FR-7; Story 3.2 cites FR-3 for density instead of FR-4; Stories 4.1/4.4 cite FR-12 for safety instead of FR-16; Story 5.1 cites FR-16 for MCP tools instead of FR-17; Story 5.3 cites FR-17 instead of FR-18. Remediation: replace every story citation with canonical PRD IDs and retain legacy IDs only in an explicitly labeled historical crosswalk.

2. **Story 2.6 contains a future-story statement as acceptance criteria.** Its FC-NIP AC says Epic 9/Story 9.2 owns future production/rendering. That is a deferral/dependency note, not a verifiable outcome of Story 2.6. Remediation: move it to an explicit out-of-scope/dependency section and keep Story 2.6 ACs limited to realtime behavior it can complete.

3. **Story 3.1's generated artifact count is inconsistent.** It promises “6-7 generated files” while listing seven non-page artifacts and omitting the optional full-page host; current architecture expects seven non-page files plus an optional command page. Remediation: state the exact required set and conditions unambiguously.

4. **Known command AC refinements remain outside the owning stories.** The command traceability addendum admits Story 3.4 does not name `IdempotentConfirmed`, `NeedsReview`, and `Warning`, and Story 4.4 does not pin `[RequiresPolicy]` evaluation before and after `BeforeSubmit`. Remediation: incorporate those criteria directly into the stories rather than relying on a later table.

5. **Story 5.2 omits cross-request lifecycle behavior.** It tests the lifecycle tool conceptually but not separate service scopes/requests; Story 11.3 later identifies this as a release defect. Remediation: require a cross-scope hosting scenario or clearly mark the baseline limitation and its blocking remediation.

6. **Story 6.2 assigns HFC1038-HFC1041 to build time.** The persistent architecture facts define these as call-site/startup/runtime registry diagnostics, not proven SourceTools build diagnostics. “When built, diagnostics are reported” is therefore not testable against the documented mechanism. Remediation: place each diagnostic at its actual phase and specify the corresponding test surface.

7. **Story 7.5 conflicts with Story 11.6.** Story 7.5 says `TestFaultInjectionProvider` drives deterministic fault scenarios, while Story 11.6 says it must actually inject faults or be renamed to an evidence recorder. Remediation: reconcile the accepted baseline and ensure one story owns the missing behavior rather than claiming it twice.

8. **Story 8.5 is too large for an independently reviewable story.** It combines desktop/mobile navigation modes, icon-label layout, badge overlays, active-state behavior, a flyout, full keyboard interaction, accessibility semantics, Fluent RC verification, and tests. Remediation: split navigation rail presentation from projection flyout behavior and route/keyboard integration.

9. **Stories 11.2, 11.3, 11.4, 11.6, 11.12, 11.13, 11.15, and 11.16 are multi-defect bundles.** Examples include Story 11.2 combining reconnect policy, disposal, locking, cache races, wire constants, and factory tests; Story 11.4 explicitly containing three independently verifiable groups; Story 11.16 combining fatal guards, hydration/JSON consolidation, and generated-literal escaping. Remediation: split by defect class and validation lane before implementation.

10. **Epic 11's file/heading order is unsafe for automation.** The document places 11.11-11.14 before 11.9 and then relies on a separate authoritative table to override numeric and file order. Remediation: make structural order match implementation order or move each workstream to independently ordered story artifacts with explicit prerequisites.

11. **Planning status conflicts with the current architecture source.** The epic text presents Stories 11.11-11.14 as remaining implementation work, while canonical architecture says 11.11-11.13 are implemented and 11.14 is evidence/documentation, and persistent project context describes 11.11-11.14 as implemented. Remediation: reconcile story statuses and past/future wording before any implementation workflow consumes the plan.

12. **Story 8.5 pins an obsolete Fluent UI build.** Its AC names `5.0.0-rc.3-26138.1`, while current project context pins `5.0.0-rc.4-26180.1`. Remediation: reference the centrally pinned repository version or update the explicit acceptance value.

13. **Epic 10 is organized as a technical backlog bucket.** The five stories target separate internal processes and audiences, and no single demonstrable user journey depends on the bundle. Remediation: distribute the stories into their owning tooling/testing/documentation outcomes or define a concrete adopter evidence journey with an end-to-end acceptance gate.

#### 🟡 Minor Concerns

1. Story 1.5 permits “a conforming doc page, or a tracked gap with an owner.” The global contract-confirmation rule requires a tracked, dated, owned blocking follow-up; the local wording is weaker and permits incomplete closure.

2. Story 4.5 refers to an “agreed retry budget” without repeating or directly linking the exact deterministic values already recorded in Story 3.6.

3. Story 6.3 refers to the “documented override-resolution order” without naming Level 4 → Level 2 → generated default and the Level-3 delegation condition.

4. Story 8.3 lacks a full Given/When/Then scenario, named validation evidence, and a precise no-logo behavior test.

5. Story 8.4 uses approximate and conditional terms such as “toward ~46px,” “where supported,” and “if not already set,” which do not produce a deterministic pass/fail criterion.

6. Story 9.1 remains written as an open decision story even though its status is done and the payload source is approved; the blocking-follow-up branch is stale.

7. Story 10.2 does not define the exact adopter-facing corpus or an automated test that proves historical labels are removed.

8. Static contract ACs throughout Epics 8-11 often omit an explicit `When` clause. This is readable but does not fully comply with the workflow's requested Given/When/Then structure.

9. Several ACs retain unresolved alternatives after decisions were supposedly made: “or approved equivalent,” “actually injects (or is renamed),” and “Contracts.UI assembly or approved equivalent.” These should be resolved before ready-for-dev unless the choice itself is the story's explicit decision outcome.

### Story Review Summary by Epic

- **Epic 1:** Stories 1.0-1.4 and 1.6 are appropriately sized; Story 1.5 needs a strict completion gate.

- **Epic 2:** Stories 2.1-2.5, 2.7, and 2.8 are implementable; Story 2.6 must remove the Epic 9 deferral from its ACs.

- **Epic 3:** Stories 3.2-3.3 and 3.5-3.6 are generally testable; Story 3.1 needs exact artifact counts and Story 3.4 needs all terminal states.

- **Epic 4:** Stories 4.1-4.3 are focused; Story 4.4 needs exact authorization sequencing and Story 4.5 needs explicit retry values and fault classifications.

- **Epic 5:** Stories 5.1 and 5.3-5.5 are focused; Story 5.2 needs the cross-request lifecycle scenario.

- **Epic 6:** Stories 6.1, 6.3, and 6.4 are focused; Story 6.2 must align diagnostic phase and evidence.

- **Epic 7:** Stories 7.1-7.4 are focused; Story 7.5 must be reconciled with Story 11.6.

- **Epic 8:** Stories 8.1-8.2, 8.6, and 8.7 are focused; Stories 8.3-8.4 need sharper ACs and Story 8.5 must be split.

- **Epic 9:** Story 9.2 is implementation-ready; Story 9.1 should be converted to a closed decision record rather than an active story.

- **Epic 10:** Individual stories are mostly bounded, but the epic is not cohesive; Story 10.2 needs an objective corpus/evidence definition.

- **Epic 11:** Stories 11.0 and 11.8 are closed decisions; 11.1, 11.5, 11.7, 11.9, and 11.14 are reasonably bounded; 11.2-11.4, 11.6, 11.11-11.13, 11.15-11.16 need splitting or narrowing; 11.17-11.19 and their bullet children are not ready-for-dev.

### Dependency Analysis

- No epic requires a numerically later epic to deliver its explicitly stated baseline outcome.

- The principal future reference is Story 2.6 → Epic 9/Story 9.2; it is framed as deferred scope, but its placement as an AC violates independent-story structure.

- Epic 11 claims independence from Epics 9/10, yet Story 11.6 directly assumes Story 10.5 privacy findings. This is a backward numeric dependency, not a forbidden forward dependency, but the independence claim should be corrected.

- Epic 11's internal dependency order is documented, but heading order and unresolved decomposition make tool-driven sequencing unsafe.

- Database/entity creation timing is not applicable: FrontComposer is a brownfield framework and these stories do not propose a database schema rollout.

- Starter-template setup is not required by the selected architecture. The brownfield integration spike (Story 1.0) and existing-host bootstrap (Story 1.1) are the appropriate equivalents.

### Actionable Quality Recommendations

1. Reconcile the existing completed `REL-2` story into canonical planning and retain the open `REL-AI-1` real-release-evidence closure gate for FR-24; do not recreate `REL-2`.

2. Replace legacy story-level FR markers with canonical PRD identifiers using the semantic matrix in this report.

3. Synchronize the existing full 11.17a-d/11.18a story specifications into canonical planning, create full independent specifications for 11.18b-c/11.19a-d, and split the other multi-defect Epic 11 bundles.

4. Reorganize Epic 11 and preferably Epic 10 into cohesive user/outcome workstreams rather than source-of-debt buckets.

5. Apply the already-documented AC refinements directly to Stories 3.4 and 4.4; correct Stories 3.1, 6.2, 7.5, and 8.5.

6. Reconcile story statuses, package pins, and implemented-versus-planned language with the canonical architecture and current project context.

7. Resolve all “or equivalent” choices and replace approximate/conditional AC wording with measurable pass/fail outcomes before ready-for-dev.

## Summary and Recommendations

### Overall Readiness Status

**NOT READY**

The requirement inventory is complete and semantic coverage is strong: all 29 canonical functional requirements have an identified implementation or release-governance path. That is not sufficient for Phase 4 readiness. The canonical plan does not represent the existing `REL-2` implementation story or current decomposed Epic 11 children, still contains unmaterialized release-required children, retains a heterogeneous technical-remediation epic, uses stale story-level requirement IDs, and contradicts already-implemented work.

Do not use the current `epics.md` as an unattended implementation queue. Already implemented behavior does not need to be rebuilt merely because the planning document is stale; reconcile artifact state first, then implement only independently validated, current stories.

### Critical Issues Requiring Immediate Action

1. **Reconcile the existing FR-24 / `REL-2` story and close evidence only when proven.** `REL-2` is already done in sprint tracking; canonical planning must reference its complete acceptance/evidence contract. `REL-AI-1` must remain open until package inventory, consumer validation, signing/timestamp verification, symbols, SBOM, checksums, sealed manifest/evidence, readiness classification, and GitHub Release evidence are recorded from a real release or an approved fallback with reopen criteria.

2. **Synchronize existing Epic 11 children and create only the missing ones.** Sprint artifacts already contain full 11.17a-d stories and 11.18a; canonical planning must reflect them. Stories 11.18b-c and 11.19a-d still need their own user/outcome statements, prerequisites, BDD criteria, validation lanes, and statuses before promotion. The 11.17/11.18/11.19 parents remain non-implementable.

3. **Restructure the Epic 11 remediation portfolio.** Split runtime reliability/security, adopter testing and route behavior, contract/package compatibility, and maintainability/enforcement into cohesive outcome-oriented workstreams. Narrow the other multi-defect stories before implementation.

### Required Readiness Exit Criteria

The status can move from **NOT READY** only when all of the following are true:

- Canonical planning references the existing completed `REL-2` story, and `REL-AI-1` is closed only after every FR-24 consequence has recorded real-release evidence or an approved fallback/reopen rule.

- Every release-required Epic 11 work item is either verified complete with evidence or represented by an independently implementable child story; existing 11.17a-d/11.18a artifacts are synchronized, missing 11.18b-c/11.19a-d artifacts are created, and no split-before-dev parent is an implementation candidate.

- `epics.md`, architecture, project context, and sprint status agree on which Stories 11.11-11.14 and other remediation items are complete, pending, superseded, or evidence-only.

- Story-level FR references use the canonical PRD numbering, with any legacy numbering isolated in a historical crosswalk.

- The admitted AC gaps in Stories 3.4 and 4.4 are incorporated into the owning stories, and the specific defects in Stories 3.1, 6.2, 7.5, and 8.5 are corrected.

- UX provenance is repaired: draft authority is resolved, broken source links are fixed, the visual source is identified, and FC-IA-1/WCAG/navigation terminology is promoted into canonical PRD/architecture artifacts.

### Recommended Next Steps

1. Run a focused planning correction that treats this report as the defect ledger. Start by reconciling completed `REL-2`, existing 11.17a-d/11.18a stories, current sprint statuses, and the still-missing 11.18b-c/11.19a-d story specifications.

2. Rebuild the canonical FR-to-story trace directly in `epics.md` from the 29-row semantic matrix in this report; remove or clearly quarantine the legacy numeric map.

3. Split the oversized Epic 11 stories by defect class and validation lane, then reorganize Epic 11 and Epic 10 around demonstrable user/adopter outcomes.

4. Promote signed UX decisions into the canonical UX and architecture sources, repair provenance links, define the module/bounded-context/tab terminology map, and add measurable experience budgets where they are release-relevant.

5. Re-run implementation readiness after the planning corrections. A code implementation run should begin only from a story whose current status, dependencies, ACs, and evidence contract agree across the canonical artifacts.

### Finding Summary

This assessment recorded **43 findings or concerns across four categories**:

- PRD completeness and currency: 5

- FR coverage traceability: 1

- UX/PRD/architecture alignment and provenance: 12

- Epic/story structure, sizing, dependencies, and acceptance quality: 25 (3 critical, 13 major, 9 minor)

Some findings overlap by design, particularly the legacy FR numbering problem, because it affects both traceability and story quality. The three critical violations are the readiness blockers.

### Final Note

The product intent is well covered, and the canonical PRD is considerably stronger than the historical brownfield inventory. The remaining problem is execution safety, not missing vision: the plan must be made current, cohesive, and story-complete before it can reliably drive implementation or release work.

**Assessment date:** 2026-07-15  
**Assessor:** Codex — BMAD Implementation Readiness workflow
