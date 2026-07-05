---
project: frontcomposer
date: 2026-07-05
assessor: Codex via bmad-check-implementation-readiness
overallReadinessStatus: NEEDS_WORK
stepsCompleted:
  - step-01-document-discovery
  - step-02-prd-analysis
  - step-03-epic-coverage-validation
  - step-04-ux-alignment
  - step-05-epic-quality-review
  - step-06-final-assessment
documentsIncluded:
  prd:
    - _bmad-output/planning-artifacts/prd.md
    - _bmad-output/planning-artifacts/prd-addendum-2026-07-05.md
    - _bmad-output/planning-artifacts/sprint-change-proposal-2026-07-05-prd-ai-1.md
  architecture:
    - _bmad-output/planning-artifacts/architecture.md
  epics:
    - _bmad-output/planning-artifacts/epics.md
    - _bmad-output/planning-artifacts/sprint-change-proposal-2026-07-01-epic-1-retro-follow-through.md
    - _bmad-output/planning-artifacts/sprint-change-proposal-2026-07-01-epic-3-retro-followthrough.md
    - _bmad-output/planning-artifacts/sprint-change-proposal-2026-07-01-epic-5-retro-follow-through.md
    - _bmad-output/planning-artifacts/sprint-change-proposal-2026-07-01-epic-6-retro-follow-through.md
    - _bmad-output/planning-artifacts/sprint-change-proposal-2026-07-01-epic-7-retro-follow-through.md
    - _bmad-output/planning-artifacts/sprint-change-proposal-2026-07-01-epic-8-retro-follow-through.md
    - _bmad-output/planning-artifacts/sprint-change-proposal-2026-07-05-epic-1-residual-wording-decisions.md
  ux:
    - _bmad-output/planning-artifacts/ux-design.md
    - _bmad-output/planning-artifacts/ux-design-detailed-2026-07-05.md
    - _bmad-output/planning-artifacts/ux-experience-2026-07-05.md
documentsExcluded: []
---

# Implementation Readiness Assessment Report

**Date:** 2026-07-05
**Project:** frontcomposer

## Step 1: Document Discovery

### PRD Files Found

**Whole Documents:**
- `_bmad-output/planning-artifacts/prd.md` (42,333 bytes, modified 2026-07-05 10:37)
- `_bmad-output/planning-artifacts/prd-addendum-2026-07-05.md` (3,607 bytes, modified 2026-07-05 09:20)
- `_bmad-output/planning-artifacts/sprint-change-proposal-2026-07-05-prd-ai-1.md` (4,718 bytes, modified 2026-07-05 10:37)

**Sharded Documents:**
- None found.

**Selected for Assessment:**
- `_bmad-output/planning-artifacts/prd.md`
- `_bmad-output/planning-artifacts/prd-addendum-2026-07-05.md`
- `_bmad-output/planning-artifacts/sprint-change-proposal-2026-07-05-prd-ai-1.md`

### Architecture Files Found

**Whole Documents:**
- `_bmad-output/planning-artifacts/architecture.md` (5,053 bytes, modified 2026-07-05 10:16)

**Sharded Documents:**
- None found.

**Selected for Assessment:**
- `_bmad-output/planning-artifacts/architecture.md`

### Epics & Stories Files Found

**Whole Documents:**
- `_bmad-output/planning-artifacts/epics.md` (117,088 bytes, modified 2026-07-05 16:54)
- `_bmad-output/planning-artifacts/sprint-change-proposal-2026-07-01-epic-1-retro-follow-through.md` (6,457 bytes, modified 2026-07-01 17:49)
- `_bmad-output/planning-artifacts/sprint-change-proposal-2026-07-01-epic-3-retro-followthrough.md` (15,077 bytes, modified 2026-07-01 17:44)
- `_bmad-output/planning-artifacts/sprint-change-proposal-2026-07-01-epic-5-retro-follow-through.md` (16,744 bytes, modified 2026-07-01 17:43)
- `_bmad-output/planning-artifacts/sprint-change-proposal-2026-07-01-epic-6-retro-follow-through.md` (16,904 bytes, modified 2026-07-01 17:44)
- `_bmad-output/planning-artifacts/sprint-change-proposal-2026-07-01-epic-7-retro-follow-through.md` (18,331 bytes, modified 2026-07-01 17:45)
- `_bmad-output/planning-artifacts/sprint-change-proposal-2026-07-01-epic-8-retro-follow-through.md` (15,890 bytes, modified 2026-07-01 17:44)
- `_bmad-output/planning-artifacts/sprint-change-proposal-2026-07-05-epic-1-residual-wording-decisions.md` (5,981 bytes, modified 2026-07-05 11:01)

**Sharded Documents:**
- None found.

**Selected for Assessment:**
- `_bmad-output/planning-artifacts/epics.md`
- All listed sprint-change proposal files.

### UX Design Files Found

**Whole Documents:**
- `_bmad-output/planning-artifacts/ux-design.md` (3,960 bytes, modified 2026-07-05 09:23)
- `_bmad-output/planning-artifacts/ux-design-detailed-2026-07-05.md` (8,079 bytes, modified 2026-07-05 09:08)
- `_bmad-output/planning-artifacts/ux-experience-2026-07-05.md` (14,034 bytes, modified 2026-07-05 16:54)

**Sharded Documents:**
- None found.

**Selected for Assessment:**
- `_bmad-output/planning-artifacts/ux-design.md`
- `_bmad-output/planning-artifacts/ux-design-detailed-2026-07-05.md`
- `_bmad-output/planning-artifacts/ux-experience-2026-07-05.md`

### Issues Found

- No critical whole-plus-sharded duplicate format conflicts found.
- No required document type is missing.
- Multiple PRD, epics, and UX whole-document candidates exist; user confirmed continuation with the listed document set.

## Step 2: PRD Analysis

### Functional Requirements

FR-1: Generate projection artifacts. For each valid `[Projection]` type, the Source Generator must emit a projection view, Fluxor feature/actions/reducers, and registration artifacts. A valid projection produces the documented five-file set under the public Generated Output path; a non-`partial` projection produces HFC1003 and fails under warnings-as-errors; generated projection views handle Loading, Empty, and Data states according to `ProjectionRole`.

FR-2: Generate command artifacts. For each valid `[Command]` type, the Source Generator must emit command form, lifecycle, renderer, registration, subscriber, bridge, and optional full-page route artifacts. A command with no public parameterless constructor fails with HFC1009; a command missing `MessageId` fails with HFC1006; full-page density emits a route host while inline and compact densities do not.

FR-3: Honor the attribute vocabulary. FrontComposer must support projection roles, bounded contexts, badges, column priority, field groups, empty-state CTA, destructive confirmation, policy requirements, derived fields, icons, relative time, currency, display metadata, defaults, and projection templates. Unsupported or invalid attribute use emits the corresponding HFC diagnostic; server-controlled or derived command fields do not render as editable input; projection badge and status metadata remain accessible and not color-only.

FR-4: Apply the command density rule. Command form density is determined by non-derivable property count: `Inline` for 0-1, `CompactInline` for 2-4, and `FullPage` for 5 or more. Derivable fields such as `MessageId`, `CorrelationId`, `TenantId`, `UserId`, timestamps, and `[DerivedFrom]` fields are excluded; density behavior is covered by generator tests and snapshots; threshold changes require an explicit story/ADR.

FR-5: Support safe customization levels. Adopters can override generated projection UI through Level-2 templates, Level-3 field slots, and Level-4 full-view overrides. Resolution order is deterministic: Level 4, then Level 2, then generated default; Level 3 slots compose only when the selected body delegates to generated renderers; HFC1050-HFC1055 cover statically inspectable override accessibility risks; runtime mismatch panels are development-only under DEBUG and `IsDevelopment()`.

FR-6: Detect schema and generated-output drift. FrontComposer must bind producer and consumers through Schema Fingerprints and opt-in drift baselines. Drift detection compares current generated material to checked-in baseline `AdditionalText` files; structural drift emits HFC1065 and metadata drift emits HFC1066; canonical schema material remains deterministic and bounded, with encoder, sentinel, comparer, and baseline identity treated as load-bearing.

FR-7: Provide validated DI bootstrap. Adopter apps can wire FrontComposer through `AddHexalithFrontComposerQuickstart()`, optional `AddHexalithDomain<TMarker>()`, and `AddHexalithEventStore(...)`. Missing foundational quickstart or misordered calls fail at startup with a named error; empty-shell operation is valid when no domain registrations are present; scoped auth, storage, effects, and tenant accessors must not be captured by singleton services.

FR-8: Render the shell frame. The FrontComposer Shell must render a complete Blazor application frame with Fluent layout, skip links, providers, header, navigation, content, footer, and keyboard shortcuts. Adopter layout can reduce to `<FrontComposerShell>@Body</FrontComposerShell>`; `Ctrl+,` opens settings and `Ctrl+K` opens the command palette; the framework-owned account menu is always rendered so adopter header customization cannot remove auth access.

FR-9: Manage layout, theme, density, and localized shell strings. The Shell must provide FC-LYT layout modes, shell-owned localized strings, and persisted theme/density preferences. Full-width is the default layout and constrained layout caps content at the documented max measure; settings changes persist through `IStorageService` and update `data-fc-density`; shell chrome strings resolve from shell resources while domain strings remain host/domain-owned.

FR-10: Provide registry-driven discovery. The Shell must generate navigation, home directory cards, command palette entries, projection routes, badges, and counts from Domain Manifest data. Navigation groups entries by bounded context and keeps exactly one active item; home directory supports progressive empty/loading/data states and urgency ordering; command palette search remains keyboard-accessible and authorization-aware.

FR-11: Render projection grids and states. Generated projection pages must provide filtering, empty/loading states, status indicators, expand-in-row details, column prioritization, slow-query notices, and max-items notices. Column filters are debounced and resettable; row detail regions remain accessible and announce filter-hidden expanded rows; wide projections activate column prioritization when thresholds are met; status values render as semantic icon-plus-text affordances with tooltip and `aria-label` support, never color alone.

FR-12: Maintain projection freshness and realtime behavior. The Shell must query EventStore over HTTP and subscribe to projection changes over SignalR while surfacing reconnect/reconciliation state. Reconnect and fallback polling states are visible; projection updates do not treat SignalR nudges as proof of command success; Epic 11 realtime resilience remediation is release-readiness work when a long-lived circuit can permanently degrade after reconnect failure.

FR-13: Mark fresh rows only through FC-NIP. The product must not infer row-level fresh indicators from projection nudges that lack row identity. FC-NIP owns the row identity payload and producer wiring. `FcNewItemIndicator` remains a confirmed component; automatic row marking uses only the approved FrontComposer-owned pending-command row metadata populated from generated grid/command runtime context; Story 9.1 recorded the approved payload source and Story 9.2 implements and proves producer/consumer wiring.

FR-14: Submit commands through generated forms. Generated command forms must validate input, parse supported field types, dispatch commands, and preserve form state on retryable pre-accept failures. Unsupported field types render placeholders rather than breaking the form; nullable numeric fields compile and round-trip culture-aware formatting; `MessageId` is generated as a ULID and reused across pre-accept retry attempts.

FR-15: Surface command lifecycle states. The Shell must surface Submitting, Acknowledged, Syncing, Confirmed, Rejected, IdempotentConfirmed, NeedsReview, warnings, and degraded states. Accepted HTTP transport is not displayed as projection-confirmed success; polling binds to the confirmed EventStore status endpoint; numeric budgets for confirming-to-degraded, polling cadence, duration, and retry behavior remain configurable and tested.

FR-16: Enforce command safety. Command execution must respect authorization, destructive confirmation, form-abandonment guard, and FC-CNC one-at-a-time execution. `[RequiresPolicy]` is evaluated before `BeforeSubmit` and again afterward for protected commands; the service boundary also enforces authorization through `AuthorizingCommandServiceDecorator`; FC-CNC v1 blocks later local submits rather than queueing or batching them.

FR-17: Expose generated command tools. Each visible generated command must appear as an MCP tool with descriptor-derived JSON schema and bounded acknowledgement output. Tools are built dynamically at each `tools/list`; server-controlled fields cannot be accepted from tool input; command invocation injects tenant/user/message/correlation fields server-side.

FR-18: Expose projection and skill resources. The MCP Surface must expose tenant-scoped projection resources and the embedded FrontComposer skill corpus. Projection resource URIs match generated descriptors exactly; skill resources are served only from validated `agent-reference` sections; oversized skill resources fail closed instead of truncating silently.

FR-19: Enforce MCP security and compatibility. MCP hosts must register tenant tool and resource visibility gates, negotiate schema fingerprints, and return hidden-equivalent failures for sensitive cases. Startup throws if required MCP gates are missing; auth failed, tenant missing, unknown resource, and unknown tool cases do not become existence oracles; incompatible schema fingerprints block side effects; Epic 11 MCP lifecycle cross-request remediation is v1.0-blocking because lifecycle subscribe/poll is part of the agent contract.

FR-20: Provide `frontcomposer inspect`. The CLI must inspect generated output and diagnostics sidecars and report forms, grids, registrations, manifest entries, warnings, and errors. Output supports text and JSON using `frontcomposer.cli.inspect.v1`; severity filtering and fail flags have deterministic ordering; paths are sanitized when needed.

FR-21: Provide `frontcomposer migrate`. The CLI must plan and apply allowlisted Roslyn migrations across supported version edges. Dry-run is default; apply mode is atomic and refuses unsafe paths, generated output, submodule roots, and out-of-root writes; JSON output uses `frontcomposer.cli.migrate.v1`.

FR-22: Provide adopter testing support. The Testing package must provide a bUnit host, deterministic command/query/projection fakes, evidence capture, redaction, builders, and assertion helpers. Public API drift updates `PublicAPI.Shipped.txt` intentionally; evidence output is redacted by default; v1.0 Testing must include realistic failure and policy states, not only happy-path command/query outcomes.

FR-23: Maintain component and skill documentation. FrontComposer must keep component docs, diagnostic docs, migration docs, and skill-corpus docs synchronized with the generated and runtime surfaces. Published docs under `docs/` pass the DocFX validation gate when changed; skill-corpus docs satisfy required front matter and snippet/reference validation; generated/scratch planning docs remain outside `docs/`.

FR-24: Ship signed package artifacts with evidence. FrontComposer must release the expected NuGet package set through semantic-release with signed packages, symbols, SBOM, checksums, sealed release manifest/evidence chain, GitHub Release assets, and package-consumer validation evidence. Conventional commits determine version bump; release dry-run defaults to safe non-publish behavior and cannot publish package or GitHub Release side effects; package inventory, signing/timestamp verification, symbol package presence, SBOM presence, checksum coverage, manifest verification, release-readiness classification, and package-consumer validation gate publication are required; `REL-AI-1` can be marked done only when the Release Owner records evidence paths for every FR24 artifact or records an approved fallback with explicit reopen criteria.

FR-25: Preserve public contracts and deprecation paths. Public API baselines, schema contracts, CLI JSON schemas, generated-output paths, and HFC diagnostics must evolve intentionally. Breaking public-surface changes update baselines, docs, and migration/deprecation plans; new diagnostics use the documented HFC bands and XML docs; schema canonicalization changes are treated as baseline-invalidating.

FR-26: Complete FC-NIP producer wiring. FrontComposer must complete row-level fresh-item producer/consumer wiring only through the approved FC-NIP payload source. Fresh-row indicators are never inferred from SignalR nudges or unrelated projection refreshes; the approved payload source is FrontComposer-owned pending-command row metadata populated from generated grid/command runtime context; EventStore status remains a lifecycle/status source by `MessageId`, not row identity; Story 9.2 must prove complete runtime metadata and producer/consumer behavior before release.

FR-27: Complete tooling-governance follow-through. FrontComposer must close the Epic 10 tooling-governance gaps for evidence, labels, CLI parity, migration-emission decisioning, and Testing redaction. Evidence reconciliation proves CLI, diagnostics, migration, Testing, and documentation artifacts agree on current labels and outcomes; HFCM9002 migration-emission behavior is decided and documented before release; Testing redaction coverage proves evidence output does not leak support-sensitive data.

FR-28: Govern Epic 11 decision gates. Epic 11 implementation must not start dependent stories until route-contract and Contracts split decisions are recorded. Story 11.0 selects the canonical generated command route family before any Story 11.1+ create-story work starts; Story 11.8 records the approved Contracts kernel split decision, package compatibility posture, public API impact, and deprecation/migration plan before Stories 11.11-11.14 start; sprint status and story-creation workflows follow the suggested Epic 11 order rather than naive file order.

FR-29: Remediate architecture-review release risks. FrontComposer must complete the Epic 11 architecture remediation stories that address runtime blind spots and architecture boundaries before v1.0 release. Token lifecycle, realtime resilience, MCP lifecycle, security-validation tests, visual-conformance guards, Testing harness failure modes, shell layering, helper consolidation, logging, and enforcement-policy alignment each have focused stories or gates; Story 11.10 remains split into mechanical one-type-per-file, `LoggerMessage`, and enforcement/policy alignment work, not executed as one story; Epic 11 implementation stories use Given/When/Then acceptance criteria before ready-for-dev.

Total FRs: 29

### Non-Functional Requirements

NFR-1: Build strictness. .NET 10, `.slnx` only, nullable enabled, centralized package versions, and `TreatWarningsAsErrors=true` are required.

NFR-2: Dependency direction. Dependencies point down to Contracts; SourceTools references only Contracts; net10/Fluent-only code in multi-targeted projects is guarded.

NFR-3: Accessibility. Generated and hand-authored UI must preserve WCAG-relevant names, roles, focus, keyboard, live-region, reduced-motion, and forced-colors behavior.

NFR-4: Fluent UI governance. UI uses FrontComposer/Fluent UI Blazor v5 components and Fluent 2 tokens; raw interactive HTML controls and legacy tokens are forbidden except documented carve-outs.

NFR-5: Security. MCP and Shell security fail closed; server-controlled fields are never client-supplied; return paths, storage keys, tenant/user scope, auth state, and API keys require direct tests or documented controls.

NFR-6: Privacy and support safety. UI, logs, telemetry, MCP responses, evidence, and snapshots must not expose raw tokens, JWT payloads, raw EventStore metadata, stack traces, raw event payloads, or unrestricted PII.

NFR-7: Schema determinism. Canonical schema material, fingerprint algorithms, baseline identity, and provenance validation are load-bearing public contracts.

NFR-8: Reliability. Command lifecycle and projection freshness must expose degraded/reconnecting/fallback states within configured budgets, recover when the backend recovers, and never convert a nudge or HTTP acceptance into confirmed success without projection or status evidence.

NFR-9: Performance. Palette scoring, generated rendering, and cache-backed hot paths must stay inside existing benchmark thresholds and cache caps; any threshold change requires benchmark evidence and release-owner approval.

NFR-10: Observability. FrontComposer uses `FrontComposerActivitySource` and sanitized structured logs for operator-relevant failure paths, with tests or snapshots proving tokens, JWT payloads, raw EventStore metadata, raw event payloads, stack traces, and unrestricted PII are absent.

NFR-11: Testing. The v1.0 release gate includes the default solution-level lane with `DiffEngine_Disabled=true`, Governance, Contract, snapshots, PublicAPI baselines, Pact checks, property tests where configured, docs validation, and e2e accessibility/visual lanes required by the changed surface.

NFR-12: Release evidence. Signed NuGet packages, SBOM, package inventory, readiness classification, checksums, and release manifest evidence are required for publication.

Total NFRs: 12

### Additional Requirements

- The PRD is `approved-for-v1-readiness` and `_bmad-output/planning-artifacts/prd.md` is the readiness-discoverable canonical PRD.
- The product form factor is signed NuGet packages plus a `frontcomposer` .NET tool, not a hosted SaaS or FrontComposer-owned container product.
- Target user journeys require domain-shell bootstrap, live projection investigation, safe command execution, MCP exposure, generator/runtime compatibility preservation, and realistic downstream Testing-package coverage.
- Constraints include .NET 10, C# latest, Blazor, Fluent UI Blazor v5, Fluxor, Roslyn 5.3.0, ModelContextProtocol SDK, SignalR, OIDC, NUlid, Hexalith.EventStore integration, and Hexalith domain-module adoption.
- Repository constraints forbid recursive submodule initialization, forbid modifying submodule files without explicit approval, treat `docs/` as a CI-gated DocFX site rather than scratch space, and require generated output to be changed only through SourceTools or Annotated Domain Types.
- V1 scope includes the existing baseline plus Epic 9 FC-NIP work, Epic 10 tooling-governance follow-through, Epic 11 route/Contracts decision gates, and Epic 11 remediation.
- V1 explicitly excludes rich `<AuditTimeline>` or `<ConsequencePreview>` components, replacing EventStore, non-Blazor/mobile/native shell surfaces, general-purpose no-code CRUD builder behavior, bespoke domain-specific page bodies, and recursive/nested submodule management.
- Success metrics require adopter bootstrap evidence, release readiness evidence, contract drift visibility, MCP fail-closed coverage, Testing harness usefulness, and UX governance stability.
- Public contracts include source-generator input attributes, Generated Output path, HFC diagnostics, CLI JSON schemas, MCP schemas/fingerprints, Testing package public API, release package inventory, and the approved `Contracts.UI` split.
- Decision gates D-1 through D-9 are resolved except FR-24 evidence still blocks v1.0 publication through `REL-AI-1` / `REL-1`.
- Assumption A1 is accepted as a v1.0 Testing harness requirement routed to FR-22 / Story 11.6, and A2 is accepted as the v1.0 product-form-factor assumption validated by SM-1 and SM-2.
- The PRD addendum records source inventory and reconciliation notes; it explicitly keeps detailed implementation fix lists outside the PRD and in architecture-quality review, sprint-change proposals, and `epics.md`.
- PRD-AI-1 is approved with no source-code, package, architecture, UX, or test implementation change required by that proposal alone.

### PRD Completeness Assessment

The PRD is complete enough for readiness validation. It has approved status, named target users, user journeys, 29 numbered functional requirements, 12 numbered non-functional requirements, constraints, scope boundaries, success metrics, risks, public-surface contracts, resolved decision gates, and explicit assumption dispositions. The remaining readiness risk is not PRD ambiguity; it is whether the epics and stories preserve traceability to FR-24 release evidence, FC-NIP producer wiring, tooling-governance follow-through, and Epic 11 remediation order/decomposition.

## Step 3: Epic Coverage Validation

### Epic FR Coverage Extracted

The selected epics document includes a legacy FR Coverage Map plus a PRD v1 readiness addendum. The legacy map uses older FR labels, while the canonical PRD now has FR-1 through FR-29. The addendum bridges the v1-readiness additions and explicitly maps PRD FR-27 through FR-29. Change proposals for Epics 1, 3, 5, 6, 7, and 8 add evidence/process follow-through and do not introduce new product FRs.

- PRD FR-1: Covered by Epic 2, Story 2.1.
- PRD FR-2: Covered by Epic 3, Story 3.1.
- PRD FR-3: Covered by Epic 2 and Epic 6.
- PRD FR-4: Covered by Epic 3, Story 3.2.
- PRD FR-5: Covered by Epic 6, Stories 6.1-6.4.
- PRD FR-6: Covered by Epic 7, Stories 7.3-7.4.
- PRD FR-7: Covered by Epic 1, Stories 1.0-1.1.
- PRD FR-8: Covered by Epic 1, Story 1.1, and refined by Epic 8.
- PRD FR-9: Covered by Epic 1, Story 1.6, and refined by Epic 8.
- PRD FR-10: Covered by Epic 2, Stories 2.2 and 2.7, and refined by Epic 8.
- PRD FR-11: Covered by Epic 2, Stories 2.3-2.5.
- PRD FR-12: Covered by Epic 2, Story 2.6, Epic 3, Stories 3.5-3.6, and Epic 11, Stories 11.1-11.2.
- PRD FR-13: Covered by Epic 9, Stories 9.1-9.2.
- PRD FR-14: Covered by Epic 3, Stories 3.1-3.3, Epic 4, Story 4.5, and the command subclause traceability addendum.
- PRD FR-15: Covered by Epic 3, Stories 3.4-3.6, Epic 4, Story 4.5, and the command subclause traceability addendum.
- PRD FR-16: Covered by Epic 4, Stories 4.1-4.4, and the command subclause traceability addendum.
- PRD FR-17: Covered by Epic 5, Stories 5.1-5.2.
- PRD FR-18: Covered by Epic 5, Story 5.3.
- PRD FR-19: Covered by Epic 5, Stories 5.4-5.5, and refined by Epic 11, Story 11.3.
- PRD FR-20: Covered by Epic 7, Story 7.1, and Epic 10, Story 10.3.
- PRD FR-21: Covered by Epic 7, Story 7.2, and Epic 10, Story 10.4.
- PRD FR-22: Covered by Epic 7, Story 7.5, Epic 10, Story 10.5, and Epic 11, Story 11.6.
- PRD FR-23: Covered by Epic 1, Story 1.5; Epic 5, Story 5.3; Epic 7, Stories 7.1-7.3; Epic 10, Stories 10.2/10.4; and Epic 11, Story 11.14.
- PRD FR-24: Covered by Release Governance Gate RG-1 and `REL-AI-1` / proposed `REL-1`, not by a numbered product epic.
- PRD FR-25: Covered by Epic 7, Epic 10, and Epic 11, especially Stories 11.8 and 11.11-11.14/11.19.
- PRD FR-26: Covered by Epic 9, Story 9.2, after Story 9.1 decision completion.
- PRD FR-27: Covered by Epic 10.
- PRD FR-28: Covered by Epic 11 decision gates, especially Story 11.0 and Story 11.8.
- PRD FR-29: Covered by Epic 11 implementation stories 11.1 through 11.19.

Total PRD FRs found in coverage: 29

### Coverage Matrix

| FR Number | PRD Requirement | Epic Coverage | Status |
| --- | --- | --- | --- |
| FR-1 | Generate projection artifacts | Epic 2 / Story 2.1 | Covered |
| FR-2 | Generate command artifacts | Epic 3 / Story 3.1 | Covered |
| FR-3 | Honor the attribute vocabulary | Epic 2 + Epic 6 | Covered |
| FR-4 | Apply the command density rule | Epic 3 / Story 3.2 | Covered |
| FR-5 | Support safe customization levels | Epic 6 / Stories 6.1-6.4 | Covered |
| FR-6 | Detect schema and generated-output drift | Epic 7 / Stories 7.3-7.4 | Covered |
| FR-7 | Provide validated DI bootstrap | Epic 1 / Stories 1.0-1.1 | Covered |
| FR-8 | Render the shell frame | Epic 1 / Story 1.1; Epic 8 refinements | Covered |
| FR-9 | Manage layout, theme, density, and localized shell strings | Epic 1 / Story 1.6; Epic 8 refinements | Covered |
| FR-10 | Provide registry-driven discovery | Epic 2 / Stories 2.2, 2.7; Epic 8 refinements | Covered |
| FR-11 | Render projection grids and states | Epic 2 / Stories 2.3-2.5 | Covered |
| FR-12 | Maintain projection freshness and realtime behavior | Epic 2 / Story 2.6; Epic 3 / Stories 3.5-3.6; Epic 11 / Stories 11.1-11.2 | Covered |
| FR-13 | Mark fresh rows only through FC-NIP | Epic 9 / Stories 9.1-9.2 | Covered |
| FR-14 | Submit commands through generated forms | Epic 3 / Stories 3.1-3.3; Epic 4 / Story 4.5; command subclause traceability addendum | Covered |
| FR-15 | Surface command lifecycle states | Epic 3 / Stories 3.4-3.6; Epic 4 / Story 4.5; command subclause traceability addendum | Covered |
| FR-16 | Enforce command safety | Epic 4 / Stories 4.1-4.4; command subclause traceability addendum | Covered |
| FR-17 | Expose generated command tools | Epic 5 / Stories 5.1-5.2 | Covered |
| FR-18 | Expose projection and skill resources | Epic 5 / Story 5.3 | Covered |
| FR-19 | Enforce MCP security and compatibility | Epic 5 / Stories 5.4-5.5; Epic 11 / Story 11.3 | Covered |
| FR-20 | Provide `frontcomposer inspect` | Epic 7 / Story 7.1; Epic 10 / Story 10.3 | Covered |
| FR-21 | Provide `frontcomposer migrate` | Epic 7 / Story 7.2; Epic 10 / Story 10.4 | Covered |
| FR-22 | Provide adopter testing support | Epic 7 / Story 7.5; Epic 10 / Story 10.5; Epic 11 / Story 11.6 | Covered |
| FR-23 | Maintain component and skill documentation | Epic 1, Epic 5, Epic 7, Epic 10, Epic 11 / Story 11.14 | Covered |
| FR-24 | Ship signed package artifacts with evidence | Release Governance Gate RG-1; `REL-AI-1` / `REL-1` | Covered by governance gate; still evidence-open |
| FR-25 | Preserve public contracts and deprecation paths | Epic 7, Epic 10, Epic 11 / Stories 11.8, 11.11-11.14, 11.19 | Covered |
| FR-26 | Complete FC-NIP producer wiring | Epic 9 / Story 9.2 | Covered |
| FR-27 | Complete tooling-governance follow-through | Epic 10 | Covered |
| FR-28 | Govern Epic 11 decision gates | Epic 11 / Stories 11.0 and 11.8 | Covered |
| FR-29 | Remediate architecture-review release risks | Epic 11 / Stories 11.1-11.19 | Covered |

### Missing Requirements

No PRD FR is missing from the current epic/story/gate plan.

Coverage caveats:

- FR-24 is covered by Release Governance Gate RG-1 rather than a numbered feature epic. That is valid for a release-evidence requirement, but implementation readiness still depends on completing `REL-1` or equivalent evidence-producing work before v1.0 RC classification.
- The epics document has a legacy FR inventory whose numbers do not always match the canonical PRD. Future story creation and status reporting should cite canonical PRD FR numbers to avoid false traceability.
- Command FR-14, FR-15, and FR-16 are covered, but several subclauses are pinned through a later command subclause traceability addendum rather than original story AC. That is a quality/readiness concern for later steps, not a Step 3 coverage miss.

### Coverage Statistics

- Total PRD FRs: 29
- FRs covered in epics or explicit release governance gate: 29
- Coverage percentage: 100%

## Step 4: UX Alignment Assessment

### UX Document Status

Found.

Whole UX documents loaded:

- `_bmad-output/planning-artifacts/ux-design.md` — canonical planning source.
- `_bmad-output/planning-artifacts/ux-design-detailed-2026-07-05.md` — detailed visual identity / design discipline, status `draft`.
- `_bmad-output/planning-artifacts/ux-experience-2026-07-05.md` — common application experience, IA, flows, accessibility, and FC-IA-1 route/tab decision, status `draft`.

No sharded UX folder with `index.md` was found.

### UX To PRD Alignment

- UX-DR1 design tokens align with PRD FR-9, NFR-2, NFR-4, and the approved `Contracts.UI` split public-surface work.
- UX-DR2 semantic status slots align with PRD FR-3, FR-11, NFR-3, and NFR-4. The detailed UX and epics agree that status uses icon + tooltip + always-present `aria-label`, never color alone.
- UX-DR3 responsive layout and one active navigation item align with PRD FR-8, FR-10, and the Epic 8 visual refresh.
- UX-DR4 reusable interaction components align with PRD FR-8, FR-14, FR-15, and FR-16.
- UX-DR5 loading, empty, stale, reconnecting, and pending-command states align with PRD FR-11, FR-12, FR-15, and NFR-8.
- UX-DR6 accessibility patterns align with PRD NFR-3 and NFR-6.
- UX-DR7 page layout contract aligns with PRD FR-9 and the FC-LYT scope.
- UX-DR8 account control and server security align with PRD FR-8, FR-16, and NFR-5.
- The common experience flows align with PRD user journeys UJ-1 through UJ-6: shell bootstrap, projection investigation, safe command execution, lifecycle truth, stale data handling, and support-safe copy.
- The FC-IA-1 decision resolves the prior module-tab route encoding and projection-flyout IA uncertainty. It matches PRD D-3 route decision and Epic 11 Story 11.7.

UX requirements not explicitly prominent in the PRD but covered through PRD/epic decisions:

- The one-entry-per-module navigation rule is stronger in `ux-experience-2026-07-05.md` than in the PRD narrative, but it is supported by FR-10, FR-28/FR-29, and FC-IA-1.
- Module workspace tabs and route-backed tab selection are stronger in the detailed UX spine than the concise PRD, but they are now signed off through FC-IA-1 and Story 11.7.

### UX To Architecture Alignment

- Architecture supports the UX component system through the Shell/UI consumer layer, Fluent UI v5 policy, and the planned net10-only `Contracts.UI` assembly for Blazor/Fluent rendering contracts.
- Architecture supports generated projection grids, command forms, registry-driven discovery, MCP exposure, CLI, and Testing surfaces through the source-generator and consumer layering model.
- Architecture invariants align with UX safety rules: EventStore command acceptance is not projection-confirmed success, MCP security fails closed, raw interactive HTML controls are forbidden, and Shell state follows single-writer/scoped-lifetime discipline.
- Architecture review remediation aligns with the UX risk areas: realtime resilience, command/projection route unification, dead scoped CSS, visual-conformance guards, Testing harness failure modes, and package-boundary evidence.
- The concise architecture planning source delegates detailed UX/layout policy to the deeper project architecture and UX artifacts; this is acceptable, but implementation stories must cite the richer UX source where layout decisions are involved.

### Alignment Issues

No blocking PRD/UX/Architecture contradiction was found.

Non-blocking watch items:

1. The canonical `ux-design.md` is intentionally concise. Visual, route, navigation, toolbar, grid, or layout-sensitive stories need to cite `ux-design-detailed-2026-07-05.md`, `ux-experience-2026-07-05.md`, architecture section 4, or a story-local design note before development.
2. `ux-design-detailed-2026-07-05.md` and `ux-experience-2026-07-05.md` are still marked `draft`. They are usable as supplemental design references because FC-IA-1 is signed off, but Product/UX should promote or explicitly preserve their status before using them as normative cross-module design artifacts.
3. `ux-experience-2026-07-05.md` still phrases FC-NIP fresh-row indicators as blocked until row identity is confirmed. The PRD and epics now say Story 9.1 confirmed the payload source and Story 9.2 remains the implementation evidence gate. Future UX edits should update that wording to avoid treating an already resolved decision as still open.

### Warnings

- UX is clearly required and UX documentation exists, so there is no missing-UX warning.
- No architecture support gap blocks implementation readiness, but Epic 11 visual-conformance and route-implementation stories remain the planned closure path for known UX/runtime blind spots.

## Step 5: Epic Quality Review

### Epic Structure Validation

| Epic | User Value Assessment | Independence Assessment | Result |
| --- | --- | --- | --- |
| Epic 1: Shell Foundation & Bootstrap | Clear adopter-developer value: bootable shell, layout, a11y, localization, docs, settings. | Stands alone as an empty, accessible shell. | Pass |
| Epic 2: Read-Only Projection Experience | Clear operator value: browse projections, filter/search, inspect status, live updates. | Builds only on Epic 1 and explicitly delegates row-level fresh marking to Epic 9 without requiring it. | Pass |
| Epic 3: Command Authoring & Lifecycle | Clear operator value: generated command forms and lifecycle truth. | Builds on Epics 1-2; does not require Epic 4. | Pass |
| Epic 4: Safe & Concurrent Command Execution | Clear operator safety value: destructive confirmation, abandonment, authorization, concurrency, retry/degraded handling. | Correctly builds backward on Epic 3. | Pass |
| Epic 5: AI-Agent (MCP) Surface | Clear AI-agent integrator and platform-owner value: discoverable tools/resources with fail-closed behavior. | Builds on generated manifest; independent of human UI epics beyond shared descriptors. | Pass |
| Epic 6: Customization & Extensibility | Clear adopter-developer value: override generated UI safely. | Builds on generated baseline and does not require future work. | Pass |
| Epic 7: Authoring Tooling & Drift Safety | Clear adopter-developer value: inspect, migrate, test, and detect drift. | Usable against any generated output; independent of runtime epics. | Pass |
| Epic 8: Aspire-grade Visual Refresh | Clear operator/adopter value: shell chrome, density, toolbar, status, and navigation polish within Fluent governance. | Story-level slices ship independently; no new FRs. | Pass |
| Epic 9: Fresh-Row Producer and Row Identity | Clear operator value: discover rows changed by commands without guessing. | Builds on Epics 2-3; Story 9.1 decision and Story 9.2 implementation are now done per sprint status. | Pass, with status-ledger concern |
| Epic 10: Tooling Governance Follow-Through | Governance-heavy but has adopter-developer value: trustworthy CLI/testing/evidence. | Builds on Epic 7 and is done per sprint status. | Acceptable for framework release plan |
| Epic 11: Architecture Review Remediation | User value is indirect but real: closes operator/adopter/security blind spots. | Decision gates 11.0 and 11.8 are done; implementation order is explicit. | Needs strict decomposition discipline |

### Story Quality Assessment

Most stories use a proper role/value format and concrete Given/When/Then acceptance criteria. The strongest story-quality practices are visible in:

- Story 2.6, which explicitly prevents false fresh-row inference and delegates row-level marking to Epic 9.
- Story 4.3, which pins FC-CNC as block-not-queue rather than a vague concurrency promise.
- Story 5.4, which makes fail-closed MCP gates startup-verifiable.
- Story 9.2, which implements FC-NIP through approved row metadata and no diffing/broad-marking shortcuts.
- Epic 11 ordering notes, which prevent naive numeric/file-order implementation.

Quality defects and concerns:

1. Epic 11 Stories 11.17, 11.18, and 11.19 are explicitly marked `Split-before-dev`.
   - They are decomposition parents, not implementation-ready user stories.
   - They contain child-story breakdowns, but the parent headings still appear as stories in the backlog.
   - They must not move to ready-for-dev until split into independently reviewable child stories with validation lanes.

2. REL-AI-1 remains open even though FR-24 is covered by a release governance gate.
   - Sprint status says `REL-1` is still open with remaining certificate signing, publish gating, package-consumer validation, and Release Owner workflow evidence.
   - This is not a missing epic-coverage issue, but it is a release-readiness blocker.

3. Epic 9 status appears stale in `sprint-status.yaml`.
   - `9-1` and `9-2` are both `done`, but `epic-9` remains `in-progress`.
   - The ledger comments define epic completion as manual when all stories reach done; this should be reconciled before final readiness reporting.

4. Command FR subclause traceability is now documented, but some evidence remains addendum-based rather than original AC-based.
   - FR-14 parsing/form-state/MessageId reuse, FR-15 IdempotentConfirmed/NeedsReview/Warning, and FR-16 authorization sequencing are not missing, but should remain visible in evidence notes and tests before RC.

5. UX/design source maturity is mixed.
   - `ux-design.md` is canonical and concise.
   - The richer UX files are still `draft`, so stories with visual/IA impact must cite the signed-off FC-IA-1 or a story-local design note.

### Dependency Analysis

- No current Epic N requires Epic N+1 to function.
- Epic 2 correctly avoids depending on Epic 9 by limiting Story 2.6 to live refresh/reconnect/reconciliation and explicitly prohibiting row-level inference from projection nudges.
- Epic 9 correctly depends backward on command/projection runtime context from Epics 2-3.
- Epic 10 correctly depends backward on Epic 7 and is done.
- Epic 11 has an authoritative non-numeric implementation order. That order prevents forward-dependency mistakes, but it is a process hazard: story creation must follow the order table, not heading order or numeric sort.
- Story 11.7 is no longer blocked by FC-IA-1 because the IA decision is signed off; it remains backlog implementation work.
- Stories 11.11-11.14 correctly depend on completed Story 11.8, but they are deliberately last because they change package boundaries and public API evidence.

### Database / Entity Creation Timing

Not applicable. FrontComposer is a source-generation, Shell, MCP, CLI, Testing, and package-governance framework. It does not own application database tables or domain aggregates.

### Starter Template / Brownfield Checks

- Starter-template requirement: not applicable. This is a brownfield framework repository, not a greenfield app scaffold.
- Brownfield indicators are strong: existing generated output contracts, EventStore integration, package/public API compatibility, sprint-change proposals, architecture review remediation, and submodule boundaries are all explicit.
- Submodule boundaries are correctly treated as external dependencies under `references/`; no nested submodule work is planned.

### Critical Violations

None found.

No technical epic lacks any user/adopter/operator/release-owner value outright, and no forward dependency currently breaks an epic's ability to function.

### Major Issues

1. `REL-AI-1` / `REL-1` remains open for FR-24 release evidence.
   - Impact: v1.0 RC readiness cannot be claimed until package inventory, signing/timestamp, symbols, SBOM, checksums, release manifest/evidence chain, GitHub Release/dry-run evidence, package-consumer validation, and Release Owner workflow evidence are recorded.
   - Recommendation: complete `REL-1` or record an approved fallback with explicit reopen criteria before RC classification.

2. Story 11.17, Story 11.18, and Story 11.19 are decomposition parents.
   - Impact: moving these parent stories directly to dev would violate story sizing and independent-completion standards.
   - Recommendation: split them into the already listed child stories (`11.17a-d`, `11.18a-c`, `11.19a-d`) before ready-for-dev and ensure each child names its validation lane.

3. Epic 11 remains broad and heavily technical.
   - Impact: it is acceptable only because each story closes a named operator/adopter/security defect and has an explicit order. Without that discipline, it would degrade into a technical milestone bucket.
   - Recommendation: preserve the current order table and require each created story to state user/adopter/security value plus the architecture finding it closes.

### Minor Concerns

- Epic 9 status is stale in sprint status: both stories are done while `epic-9` remains `in-progress`.
- Some completed older stories use abbreviated Given/Then forms rather than strict Given/When/Then in every AC.
- Legacy FR numbering remains in `epics.md`; future story creation should cite canonical PRD FR numbers.
- The richer UX documents are still `draft`, even though FC-IA-1 is signed off.
- Story 9.2 has documented deferred low/optional work and uncommitted review-patch caveats in sprint status. These do not invalidate AC completion, but they should be tracked outside readiness claims.

### Best Practices Compliance Checklist

| Check | Result |
| --- | --- |
| Epics deliver user/adopter/operator/release-owner value | Pass, with Epic 10/Epic 11 noted as governance/remediation-heavy |
| Epics can function independently | Pass |
| Stories are appropriately sized | Mostly pass; 11.17-11.19 must split before dev |
| No forward dependencies | Pass |
| Database tables created when needed | N/A |
| Acceptance criteria clear and testable | Mostly pass; old stories/addenda need evidence citations |
| Traceability to FRs maintained | Pass, with legacy-numbering caveat |

## Summary and Recommendations

### Overall Readiness Status

NEEDS_WORK

The planning set is substantially usable: required PRD, architecture, epics, and UX artifacts exist; all 29 PRD FRs are covered by epics, stories, or an explicit release-governance gate; UX is present and aligned; and no critical forward-dependency violation was found.

It is not clean enough to mark READY. v1.0 RC readiness still depends on open release evidence, Epic 11 decomposition discipline, Epic 11 backlog implementation, and several traceability/status hygiene corrections.

### Critical Issues Requiring Immediate Action

No critical violations were found.

### Major Issues Requiring Action

1. Complete `REL-1` for the open `REL-AI-1` FR-24 release evidence gate.
   - Required evidence: package inventory, signed `.nupkg` verification with timestamp, `.snupkg` symbols, SBOM, checksums, sealed release manifest/evidence chain, GitHub Release or dry-run assets, package-consumer validation, and Release Owner workflow evidence.

2. Split Story 11.17, Story 11.18, and Story 11.19 before moving them to ready-for-dev.
   - Use the listed child stories: `11.17a-d`, `11.18a-c`, and `11.19a-d`.
   - Each child story must name its validation lane and preserve user/adopter/security value.

3. Keep Epic 11 on the authoritative implementation order.
   - Do not use heading order, file order, or numeric sort.
   - Story 11.7 can now proceed when selected because FC-IA-1 is signed off, but package-boundary stories 11.11-11.14 remain deliberately last.

4. Preserve explicit evidence for PRD FR-14, FR-15, and FR-16 subclauses.
   - The addendum covers them, but RC evidence should cite the tests or story records for command parsing/form-state preservation, IdempotentConfirmed/NeedsReview/warning states, and authorization sequencing around `BeforeSubmit`.

### Minor Issues And Hygiene

- Update `sprint-status.yaml` so `epic-9` no longer remains `in-progress` after both Epic 9 stories are done.
- Promote or explicitly preserve the `draft` status of `ux-design-detailed-2026-07-05.md` and `ux-experience-2026-07-05.md` before treating them as normative cross-module design sources.
- Update the FC-NIP wording in `ux-experience-2026-07-05.md` so it no longer reads as if the row-identity decision is still open.
- Use canonical PRD FR numbers in future story creation and status reporting to avoid legacy epics FR-number confusion.
- Track Story 9.2 deferred low/optional work and uncommitted review-patch caveats outside readiness claims.

### Recommended Next Steps

1. Finish `REL-1` and attach the required FR-24 release evidence paths before v1.0 RC classification.
2. Convert Epic 11.17-11.19 into child implementation stories before any related dev work starts.
3. Create the next Epic 11 story only from the authoritative order table, starting with the next unsatisfied backlog item appropriate for the current sprint.
4. Patch the sprint ledger and UX wording hygiene issues so the artifacts do not contradict the completed FC-NIP and FC-IA decisions.
5. Carry forward the command subclause evidence notes into RC evidence or release-readiness review.

### Final Note

This assessment identified 8 issues requiring attention across 5 categories: release governance, story decomposition, Epic 11 execution discipline, UX/status hygiene, and command-evidence traceability. Address the major issues before claiming v1.0 implementation readiness or release-candidate readiness.

Assessment completed on 2026-07-05 by Codex via `bmad-check-implementation-readiness`.
