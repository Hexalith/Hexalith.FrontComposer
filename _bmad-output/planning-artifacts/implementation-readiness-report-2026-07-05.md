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
    - _bmad-output/planning-artifacts/sprint-change-proposal-2026-07-05-story-2-6-fresh-row-independence.md
  ux:
    - _bmad-output/planning-artifacts/ux-design.md
    - _bmad-output/planning-artifacts/ux-design-detailed-2026-07-05.md
    - _bmad-output/planning-artifacts/ux-experience-2026-07-05.md
documentsExcluded:
  archive:
    - _bmad-output/planning-artifacts/archive/prds/
    - _bmad-output/planning-artifacts/archive/ux-designs/
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
- None found after resolving duplicate artifact folders.

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
- `_bmad-output/planning-artifacts/epics.md` (108,644 bytes, modified 2026-07-05 14:38)
- `_bmad-output/planning-artifacts/sprint-change-proposal-2026-07-01-epic-1-retro-follow-through.md` (6,457 bytes, modified 2026-07-01 17:49)
- `_bmad-output/planning-artifacts/sprint-change-proposal-2026-07-01-epic-3-retro-followthrough.md` (15,077 bytes, modified 2026-07-01 17:44)
- `_bmad-output/planning-artifacts/sprint-change-proposal-2026-07-01-epic-5-retro-follow-through.md` (16,744 bytes, modified 2026-07-01 17:43)
- `_bmad-output/planning-artifacts/sprint-change-proposal-2026-07-01-epic-6-retro-follow-through.md` (16,904 bytes, modified 2026-07-01 17:44)
- `_bmad-output/planning-artifacts/sprint-change-proposal-2026-07-01-epic-7-retro-follow-through.md` (18,331 bytes, modified 2026-07-01 17:45)
- `_bmad-output/planning-artifacts/sprint-change-proposal-2026-07-01-epic-8-retro-follow-through.md` (15,890 bytes, modified 2026-07-01 17:44)
- `_bmad-output/planning-artifacts/sprint-change-proposal-2026-07-05-epic-1-residual-wording-decisions.md` (5,981 bytes, modified 2026-07-05 11:01)
- `_bmad-output/planning-artifacts/sprint-change-proposal-2026-07-05-story-2-6-fresh-row-independence.md` (19,903 bytes, modified 2026-07-05 14:36)

**Sharded Documents:**
- None found.

**Selected for Assessment:**
- `_bmad-output/planning-artifacts/epics.md`
- All listed sprint-change proposal files.

### UX Design Files Found

**Whole Documents:**
- `_bmad-output/planning-artifacts/ux-design.md` (3,960 bytes, modified 2026-07-05 09:23)
- `_bmad-output/planning-artifacts/ux-design-detailed-2026-07-05.md` (8,079 bytes, modified 2026-07-05 09:08)
- `_bmad-output/planning-artifacts/ux-experience-2026-07-05.md` (12,350 bytes, modified 2026-07-05 09:18)

**Sharded Documents:**
- None found after resolving duplicate artifact folders.

**Selected for Assessment:**
- `_bmad-output/planning-artifacts/ux-design.md`
- `_bmad-output/planning-artifacts/ux-design-detailed-2026-07-05.md`
- `_bmad-output/planning-artifacts/ux-experience-2026-07-05.md`

### Issues Found

- Duplicate PRD and UX artifact folders were archived under `_bmad-output/planning-artifacts/archive/`.
- No required document type is missing.

## Step 2: PRD Analysis

### Functional Requirements

FR-1: Generate projection artifacts. For each valid `[Projection]` type, the Source Generator must emit a projection view, Fluxor feature/actions/reducers, and registration artifacts. Consequences: a valid projection produces the documented five-file set under the public Generated Output path; a non-`partial` projection produces HFC1003 and fails under warnings-as-errors; generated projection views handle Loading, Empty, and Data states according to `ProjectionRole`.

FR-2: Generate command artifacts. For each valid `[Command]` type, the Source Generator must emit command form, lifecycle, renderer, registration, subscriber, bridge, and optional full-page route artifacts. Consequences: a command with no public parameterless constructor fails with HFC1009; a command missing `MessageId` fails with HFC1006; full-page density emits a route host, while inline and compact densities do not.

FR-3: Honor the attribute vocabulary. FrontComposer must support the documented vocabulary: projection roles, bounded contexts, badges, column priority, field groups, empty-state CTA, destructive confirmation, policy requirements, derived fields, icons, relative time, currency, display metadata, defaults, and projection templates. Consequences: unsupported or invalid attribute use emits the corresponding HFC diagnostic; server-controlled or derived command fields do not render as editable input; projection badge and status metadata remain accessible and are not color-only.

FR-4: Apply the command density rule. Command form density is determined by non-derivable property count: `Inline` for 0-1, `CompactInline` for 2-4, and `FullPage` for 5 or more. Consequences: derivable fields such as `MessageId`, `CorrelationId`, `TenantId`, `UserId`, timestamps, and `[DerivedFrom]` fields are excluded from the count; density behavior is covered by generator tests and snapshots; density thresholds change only through an explicit story/ADR.

FR-5: Support safe customization levels. Adopters can override generated projection UI through Level-2 templates, Level-3 field slots, and Level-4 full-view overrides. Consequences: resolution order is deterministic, Level 4 then Level 2 then generated default; Level 3 slots compose only when the selected body delegates to generated renderers; HFC1050-HFC1055 cover statically inspectable override accessibility risks; runtime mismatch panels are development-only under DEBUG and `IsDevelopment()`.

FR-6: Detect schema and generated-output drift. FrontComposer must bind producer and consumers through Schema Fingerprints and opt-in drift baselines. Consequences: drift detection compares current generated material to checked-in baseline `AdditionalText` files; structural drift emits HFC1065 and metadata drift emits HFC1066; canonical schema material remains deterministic and bounded, and encoder, sentinel, comparer, and baseline identity are load-bearing.

FR-7: Provide validated DI bootstrap. Adopter apps can wire FrontComposer through `AddHexalithFrontComposerQuickstart()`, optional `AddHexalithDomain<TMarker>()`, and `AddHexalithEventStore(...)`. Consequences: missing foundational quickstart or misordered calls fail at startup with a named error; empty-shell operation is valid with no domain registrations; scoped auth, storage, effects, and tenant accessors must not be captured by singletons.

FR-8: Render the shell frame. The FrontComposer Shell must render a complete Blazor application frame with Fluent layout, skip links, providers, header, navigation, content, footer, and keyboard shortcuts. Consequences: adopter layout can reduce to `<FrontComposerShell>@Body</FrontComposerShell>`; `Ctrl+,` opens settings and `Ctrl+K` opens the command palette; the framework-owned account menu is always rendered so adopter header customization cannot remove auth access.

FR-9: Manage layout, theme, density, and localized shell strings. The Shell must provide FC-LYT layout modes, shell-owned localized strings, and persisted theme/density preferences. Consequences: full-width is default and constrained layout caps content at the documented max measure; settings persist through `IStorageService` and update `data-fc-density`; shell chrome strings resolve from shell resources and domain strings remain host/domain-owned.

FR-10: Provide registry-driven discovery. The Shell must generate navigation, home directory cards, command palette entries, projection routes, badges, and counts from Domain Manifest data. Consequences: navigation groups entries by bounded context and keeps exactly one active item; home directory supports progressive empty/loading/data states and urgency ordering; command palette search remains keyboard-accessible and authorization-aware.

FR-11: Render projection grids and states. Generated projection pages must provide filtering, empty/loading states, status indicators, expand-in-row details, column prioritization, slow-query notices, and max-items notices. Consequences: column filters are debounced and resettable; row detail regions remain accessible and announce filter-hidden expanded rows; wide projections activate column prioritization when thresholds are met; status values render as semantic icon-plus-text affordances with tooltip and `aria-label`, never color alone.

FR-12: Maintain projection freshness and realtime behavior. The Shell must query EventStore over HTTP and subscribe to projection changes over SignalR while surfacing reconnect/reconciliation state. Consequences: reconnect and fallback polling states are visible; projection updates do not treat SignalR nudges as proof of command success; Epic 11 realtime resilience remediation is release-readiness work when a long-lived circuit can permanently degrade after reconnect failure.

FR-13: Mark fresh rows only through FC-NIP. The product must not infer row-level fresh indicators from projection nudges that lack row identity. FC-NIP owns the row identity payload and producer wiring. Consequences: `FcNewItemIndicator` remains a confirmed component; automatic row marking uses only the approved FrontComposer-owned pending-command row metadata populated from generated grid/command runtime context; Story 9.1 recorded the approved payload source and Story 9.2 implements and proves wiring.

FR-14: Submit commands through generated forms. Generated command forms must validate input, parse supported field types, dispatch commands, and preserve form state on retryable pre-accept failures. Consequences: unsupported field types render placeholders rather than breaking the form; nullable numeric fields compile and round-trip culture-aware formatting; `MessageId` is generated as a ULID and reused across pre-accept retry attempts.

FR-15: Surface command lifecycle states. The Shell must surface Submitting, Acknowledged, Syncing, Confirmed, Rejected, IdempotentConfirmed, NeedsReview, warnings, and degraded states. Consequences: accepted HTTP transport is not displayed as projection-confirmed success; polling binds to the confirmed EventStore status endpoint; numeric budgets for confirming-to-degraded, polling cadence, duration, and retry behavior remain configurable and tested.

FR-16: Enforce command safety. Command execution must respect authorization, destructive confirmation, form-abandonment guard, and FC-CNC one-at-a-time execution. Consequences: `[RequiresPolicy]` is evaluated before `BeforeSubmit` and again afterward for protected commands; the service boundary also enforces authorization through `AuthorizingCommandServiceDecorator`; FC-CNC v1 blocks later local submits rather than queueing or batching them.

FR-17: Expose generated command tools. Each visible generated command must appear as an MCP tool with descriptor-derived JSON schema and bounded acknowledgement output. Consequences: tools are built dynamically at each `tools/list`; server-controlled fields cannot be accepted from tool input; command invocation injects tenant/user/message/correlation fields server-side.

FR-18: Expose projection and skill resources. The MCP Surface must expose tenant-scoped projection resources and the embedded FrontComposer skill corpus. Consequences: projection resource URIs match generated descriptors exactly; skill resources are served only from validated `agent-reference` sections; oversized skill resources fail closed instead of truncating silently.

FR-19: Enforce MCP security and compatibility. MCP hosts must register tenant tool and resource visibility gates, negotiate schema fingerprints, and return hidden-equivalent failures for sensitive cases. Consequences: startup throws if required MCP gates are missing; auth failed, tenant missing, unknown resource, and unknown tool cases do not become existence oracles; incompatible schema fingerprints block side effects; Epic 11 MCP lifecycle cross-request remediation is v1.0-blocking.

FR-20: Provide `frontcomposer inspect`. The CLI must inspect generated output and diagnostics sidecars and report forms, grids, registrations, manifest entries, warnings, and errors. Consequences: output supports text and JSON using `frontcomposer.cli.inspect.v1`; severity filtering and fail flags have deterministic ordering; paths are sanitized when needed.

FR-21: Provide `frontcomposer migrate`. The CLI must plan and apply allowlisted Roslyn migrations across supported version edges. Consequences: dry-run is default; apply mode is atomic and refuses unsafe paths, generated output, submodule roots, and out-of-root writes; JSON output uses `frontcomposer.cli.migrate.v1`.

FR-22: Provide adopter testing support. The Testing package must provide a bUnit host, deterministic command/query/projection fakes, evidence capture, redaction, builders, and assertion helpers. Consequences: public API drift updates `PublicAPI.Shipped.txt` intentionally; evidence output is redacted by default; v1.0 Testing must include realistic failure and policy states, not only happy-path command/query outcomes.

FR-23: Maintain component and skill documentation. FrontComposer must keep component docs, diagnostic docs, migration docs, and skill-corpus docs synchronized with the generated and runtime surfaces. Consequences: published docs under `docs/` pass the DocFX validation gate when changed; skill-corpus docs satisfy required front matter and snippet/reference validation; generated/scratch planning docs remain outside `docs/`.

FR-24: Ship signed package artifacts with evidence. FrontComposer must release the expected NuGet package set through semantic-release with signed packages, symbols, SBOM, checksums, sealed release manifest/evidence chain, GitHub Release assets, and package-consumer validation evidence. Consequences: Conventional commits determine version bump; release dry-run defaults to safe non-publish behavior and cannot publish package or GitHub Release side effects; package inventory, signing/timestamp verification, symbol package presence, SBOM presence, checksum coverage, manifest verification, release-readiness classification, and package-consumer validation gate publication are required; `REL-AI-1` can be marked done only when the Release Owner records evidence paths for every FR24 artifact or records an approved fallback with explicit reopen criteria.

FR-25: Preserve public contracts and deprecation paths. Public API baselines, schema contracts, CLI JSON schemas, generated-output paths, and HFC diagnostics must evolve intentionally. Consequences: breaking public-surface changes update baselines, docs, and migration/deprecation plans; new diagnostics use documented HFC bands and XML docs; schema canonicalization changes are baseline-invalidating.

FR-26: Complete FC-NIP producer wiring. FrontComposer must complete row-level fresh-item producer/consumer wiring only through the approved FC-NIP payload source. Consequences: fresh-row indicators are never inferred from SignalR nudges or unrelated projection refreshes; the approved payload source is FrontComposer-owned pending-command row metadata populated from generated grid/command runtime context; EventStore status remains lifecycle/status by `MessageId`, not row identity; Story 9.2 must prove complete runtime metadata and producer/consumer behavior before release.

FR-27: Complete tooling-governance follow-through. FrontComposer must close the Epic 10 tooling-governance gaps for evidence, labels, CLI parity, migration-emission decisioning, and Testing redaction. Consequences: evidence reconciliation proves CLI, diagnostics, migration, Testing, and documentation artifacts agree on current labels and outcomes; HFCM9002 migration-emission behavior is decided and documented before release; Testing redaction coverage proves evidence output does not leak support-sensitive data.

FR-28: Govern Epic 11 decision gates. Epic 11 implementation must not start dependent stories until route-contract and Contracts split decisions are recorded. Consequences: Story 11.0 selects the canonical generated command route family before any Story 11.1+ create-story work starts; Story 11.8 records the approved Contracts kernel split decision, package compatibility posture, public API impact, and deprecation/migration plan before Stories 11.11-11.14 start; sprint status and story-creation workflows follow suggested Epic 11 order rather than naive file order.

FR-29: Remediate architecture-review release risks. FrontComposer must complete the Epic 11 architecture remediation stories that address runtime blind spots and architecture boundaries before v1.0 release. Consequences: token lifecycle, realtime resilience, MCP lifecycle, security-validation tests, visual-conformance guards, Testing harness failure modes, shell layering, helper consolidation, logging, and enforcement-policy alignment each have focused stories or gates; Story 11.10 remains split into mechanical one-type-per-file, `LoggerMessage`, and enforcement/policy alignment work; acceptance criteria for Epic 11 implementation stories use Given/When/Then form before ready-for-dev.

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

- Source inventory must remain explicit: the PRD was drafted and updated from local planning artifacts and brownfield documents, including `epics.md`, `architecture.md`, `ux-design.md`, readiness reports, sprint-change proposals, and project docs.
- The canonical planning source for PRD traceability is `_bmad-output/planning-artifacts/prd.md`; generated/BMad run artifacts are not the canonical source of truth.
- Detailed implementation fix lists remain outside the PRD and are held in architecture-quality review, sprint-change proposals, and `epics.md`.
- Public API baselines, generated snapshots, Pact files, and release evidence are validation artifacts, not PRD content.
- PRD-AI-1 is approved: D-4 is resolved to the approved FC-NIP payload source; D-9 confirms Product approval and promotes PRD status to `approved-for-v1-readiness`; A1 and A2 are explicitly accepted; no source-code, package, architecture, UX, or test implementation change is required by that proposal alone.
- V1 scope includes the existing baseline plus Epic 9 FC-NIP work, Epic 10 tooling-governance follow-through, Epic 11 route/Contracts gates, and Epic 11 remediation.
- V1 explicitly excludes rich `<AuditTimeline>` or `<ConsequencePreview>` components, replacing EventStore, non-Blazor/mobile/native shell surfaces, generic no-code CRUD builder behavior, bespoke domain-specific page bodies, and recursive/nested submodule management.
- Success metrics require adopter bootstrap evidence, release readiness evidence, contract drift visibility, MCP fail-closed coverage, Testing harness usefulness, and UX governance stability.
- Public contracts include source-generator input attributes, the Generated Output path, HFC diagnostics, CLI JSON schemas, MCP schemas/fingerprints, Testing package public API, release package inventory, and the approved `Contracts.UI` split.

### PRD Completeness Assessment

The PRD is structurally complete for readiness validation: it has approved status, named target users, journeys, 29 numbered FRs, 12 numbered NFRs, constraints, scope, success metrics, risks, public-surface contracts, decisions, and assumption dispositions. The main completeness risk is not PRD absence; it is whether epics and stories preserve traceability to release-governance evidence, FC-NIP producer wiring, tooling-governance gaps, and Epic 11 remediation gates.

## Step 3: Epic Coverage Validation

### Epic FR Coverage Extracted

The epics document contains a legacy FR inventory and coverage map, then a PRD v1 readiness addendum. The legacy map covers FR1-FR26 by brownfield numbering; the addendum maps current PRD FR-27 through FR-29. This validation matches by requirement meaning against the current PRD FR-1 through FR-29, not by legacy number alone.

FR-1: Covered by Epic 2 / Story 2.1.
FR-2: Covered by Epic 3 / Story 3.1.
FR-3: Covered by Epic 2 / Story 2.1 and Epic 6 / Stories 6.1-6.4.
FR-4: Covered by Epic 3 / Story 3.2.
FR-5: Covered by Epic 6 / Stories 6.1-6.4.
FR-6: Covered by Epic 7 / Story 7.4, with schema/manifests also anchored in Epic 5.
FR-7: Covered by Epic 1 / Stories 1.0 and 1.1.
FR-8: Covered by Epic 1 / Story 1.1.
FR-9: Covered by Epic 1 / Stories 1.2, 1.4, and 1.6, with density/chrome refinements in Epic 8.
FR-10: Covered by Epic 2 / Stories 2.2 and 2.7, plus Epic 8 navigation refinements.
FR-11: Covered by Epic 2 / Stories 2.3, 2.4, and 2.5.
FR-12: Covered by Epic 2 / Story 2.6 and Epic 11 / Story 11.2.
FR-13: Covered by Epic 9 / Stories 9.1 and 9.2, with Story 2.6 cleanup explicitly preventing projection-nudge inference.
FR-14: Covered by Epic 3 / Story 3.1 and Epic 4 / Story 4.5, but not all PRD subclauses are explicit in story AC.
FR-15: Covered by Epic 3 / Stories 3.4-3.6 and Epic 4 / Story 4.5, but not every named lifecycle terminal/outcome is explicit in story AC.
FR-16: Covered by Epic 4 / Stories 4.1-4.5, but the exact `[RequiresPolicy]` before/after `BeforeSubmit` sequencing is not explicit.
FR-17: Covered by Epic 5 / Story 5.1.
FR-18: Covered by Epic 5 / Story 5.3.
FR-19: Covered by Epic 5 / Stories 5.4 and 5.5, plus Epic 11 / Story 11.3.
FR-20: Covered by Epic 7 / Story 7.1 and Epic 10 / Story 10.3.
FR-21: Covered by Epic 7 / Story 7.2 and Epic 10 / Story 10.4.
FR-22: Covered by Epic 7 / Story 7.5, Epic 10 / Story 10.5, and Epic 11 / Story 11.6.
FR-23: Covered by Epic 1 / Story 1.5, Epic 5 skill-resource work, Epic 7 docs, Epic 10 docs cleanup, and Epic 11 / Story 11.14.
FR-24: Covered by Release Governance Gate RG-1 and `sprint-status.yaml` action `REL-AI-1`; epics note that a focused release-governance story `REL-1` must be created if workflow/governance/package-consumer changes are needed before RC.
FR-25: Covered by Epic 7, Epic 10, and Epic 11 / Stories 11.8 and 11.11-11.14 plus 11.19.
FR-26: Covered by Epic 9 / Stories 9.1 and 9.2.
FR-27: Covered by Epic 10.
FR-28: Covered by Story 11.0 and Story 11.8 decision gates, both recorded as done on 2026-07-05.
FR-29: Covered by Epic 11 / Stories 11.1-11.19.

Total FRs in epics or release gates: 29

### Coverage Matrix

| FR Number | PRD Requirement | Epic Coverage | Status |
| --- | --- | --- | --- |
| FR-1 | Generate projection artifacts | Epic 2 / Story 2.1 | Covered |
| FR-2 | Generate command artifacts | Epic 3 / Story 3.1 | Covered |
| FR-3 | Honor the attribute vocabulary | Epic 2 / Story 2.1; Epic 6 / Stories 6.1-6.4 | Covered |
| FR-4 | Apply the command density rule | Epic 3 / Story 3.2 | Covered |
| FR-5 | Support safe customization levels | Epic 6 / Stories 6.1-6.4 | Covered |
| FR-6 | Detect schema and generated-output drift | Epic 7 / Story 7.4; Epic 5 manifest/schema work | Covered |
| FR-7 | Provide validated DI bootstrap | Epic 1 / Stories 1.0-1.1 | Covered |
| FR-8 | Render the shell frame | Epic 1 / Story 1.1 | Covered |
| FR-9 | Manage layout, theme, density, and localized shell strings | Epic 1 / Stories 1.2, 1.4, 1.6; Epic 8 | Covered |
| FR-10 | Provide registry-driven discovery | Epic 2 / Stories 2.2, 2.7; Epic 8 | Covered |
| FR-11 | Render projection grids and states | Epic 2 / Stories 2.3-2.5 | Covered |
| FR-12 | Maintain projection freshness and realtime behavior | Epic 2 / Story 2.6; Epic 11 / Story 11.2 | Covered |
| FR-13 | Mark fresh rows only through FC-NIP | Epic 9 / Stories 9.1-9.2 | Covered |
| FR-14 | Submit commands through generated forms | Epic 3 / Story 3.1; Epic 4 / Story 4.5 | Partial |
| FR-15 | Surface command lifecycle states | Epic 3 / Stories 3.4-3.6; Epic 4 / Story 4.5 | Partial |
| FR-16 | Enforce command safety | Epic 4 / Stories 4.1-4.5 | Partial |
| FR-17 | Expose generated command tools | Epic 5 / Story 5.1 | Covered |
| FR-18 | Expose projection and skill resources | Epic 5 / Story 5.3 | Covered |
| FR-19 | Enforce MCP security and compatibility | Epic 5 / Stories 5.4-5.5; Epic 11 / Story 11.3 | Covered |
| FR-20 | Provide `frontcomposer inspect` | Epic 7 / Story 7.1; Epic 10 / Story 10.3 | Covered |
| FR-21 | Provide `frontcomposer migrate` | Epic 7 / Story 7.2; Epic 10 / Story 10.4 | Covered |
| FR-22 | Provide adopter testing support | Epic 7 / Story 7.5; Epic 10 / Story 10.5; Epic 11 / Story 11.6 | Covered |
| FR-23 | Maintain component and skill documentation | Epic 1 / Story 1.5; Epic 5; Epic 7; Epic 10; Epic 11 / Story 11.14 | Covered |
| FR-24 | Ship signed package artifacts with evidence | Release Governance Gate RG-1; `REL-AI-1`; possible `REL-1` story | Gate Covered |
| FR-25 | Preserve public contracts and deprecation paths | Epic 7; Epic 10; Epic 11 / Stories 11.8, 11.11-11.14, 11.19 | Covered |
| FR-26 | Complete FC-NIP producer wiring | Epic 9 / Stories 9.1-9.2 | Covered |
| FR-27 | Complete tooling-governance follow-through | Epic 10 | Covered |
| FR-28 | Govern Epic 11 decision gates | Story 11.0; Story 11.8 | Covered |
| FR-29 | Remediate architecture-review release risks | Epic 11 / Stories 11.1-11.19 | Covered |

### Missing Requirements

No PRD FR is wholly missing from the epics, stories, or release-governance gate. Three FRs have partial traceability where the epic/story path exists but PRD subclauses are not fully explicit:

- FR-14: Story 3.1 covers generated forms and unsupported field placeholders, and Story 4.5 covers retry/degraded handling, but the PRD subclauses for supported field parsing, nullable numeric culture-aware round-trip, and preserving form state on retryable pre-accept failures are not all explicit in the story AC.
- FR-15: Stories 3.4-3.6 and 4.5 cover Submitting, Acknowledged, Syncing, Confirmed, Rejected, retry, polling, and degraded behavior, but `IdempotentConfirmed`, `NeedsReview`, and warning-state coverage are not explicit in the story AC.
- FR-16: Epic 4 covers destructive confirmation, abandonment guard, FC-CNC, policy authorization, retry/degraded behavior, and the service decorator, but the PRD's exact authorization sequencing around `BeforeSubmit` is not explicit.

### Coverage Statistics

- Total PRD FRs: 29
- FRs with an implementation path or release gate: 29
- Fully explicit FR coverage: 26
- Partial FR coverage: 3
- Wholly missing FR coverage: 0
- Trace coverage percentage: 100%
- Fully explicit coverage percentage: 89.7%

## Step 4: UX Alignment Assessment

### UX Document Status

Found.

Loaded UX planning artifacts:

- `_bmad-output/planning-artifacts/ux-design.md`
- `_bmad-output/planning-artifacts/ux-design-detailed-2026-07-05.md`
- `_bmad-output/planning-artifacts/ux-experience-2026-07-05.md`

Architecture artifacts used for alignment:

- `_bmad-output/planning-artifacts/architecture.md`
- `_bmad-output/project-docs/architecture.md`

### UX to PRD Alignment

- UX-DR1 design tokens aligns with PRD NFR-4, NFR-7, and FR-25, plus the approved `Contracts.UI` split path.
- UX-DR2 semantic status slots aligns with PRD FR-3, FR-11, NFR-3, and NFR-4.
- UX-DR3 responsive layout aligns with PRD FR-8, FR-10, and the shell/navigation journey expectations.
- UX-DR4 reusable interaction components aligns with PRD FR-8, FR-10, FR-14, FR-15, and FR-16.
- UX-DR5 loading/empty/status/pending states aligns with PRD FR-11, FR-12, FR-15, and NFR-8.
- UX-DR6 accessibility patterns aligns with PRD NFR-3, NFR-4, NFR-6, FR-5, FR-11, and FR-23.
- UX-DR7 page layout aligns with PRD FR-9 and the FC-LYT layout contract.
- UX-DR8 account control and server security aligns with PRD FR-8, FR-16, and NFR-5.
- UX command-truth language aligns with PRD's core distinction that command acceptance is not projection-confirmed success.
- UX FC-NIP language aligns with PRD FR-13 and FR-26: broad row marking and diff-based inference are not allowed.

### UX to Architecture Alignment

- Architecture supports the shell frame through `FrontComposerShell`, `FluentLayout`, skip links, global shortcuts, Fluent providers, account control, and the framework-owned server security helpers.
- Architecture supports generated projection UX through `FluentDataGrid`, filter/expand/status components, live EventStore SignalR/HTTP paths, and explicit reconnect/reconciliation state.
- Architecture supports command UX through generated forms, `FcAuthorizedCommandRegion`, `FcLifecycleWrapper`, policy authorization, destructive confirmation, FC-CNC one-at-a-time behavior, command polling, and EventStore status binding.
- Architecture supports UX governance through FrontComposer/Fluent v5-only component policy, Fluent 2 token policy, accordion/page-section guidelines, layout-component guidelines, accent-as-thread guard, and visual-conformance guard plans.
- Architecture supports account/security UX through always-rendered `FcAccountMenu`, framework-owned authentication/token relay wiring, and domain-owned security configuration.
- Architecture supports FC-NIP separation by explicitly stating fresh-row indicators are not produced from projection nudges and require row identity owned by FC-NIP.
- Architecture supports design-token/package-boundary needs through the approved `Contracts` kernel plus net10-only `Contracts.UI` target.

### Alignment Issues

- The detailed experience UX file still has open questions for module default tab naming, tab route encoding, whether the shell rail should keep a projection flyout, and which module becomes the first visual reference implementation. These questions do not invalidate current readiness, but route/navigation implementation stories need a story-local decision before development.
- The UX experience spine says application-level navigation should keep one primary menu entry per module, while architecture/Epic 8 still describe a projection flyout as a secondary navigation pattern. This is reconcilable if the flyout routes into module workspace tabs and remains secondary, but it should be pinned before future navigation work.
- The root UX document is canonical, while the promoted detailed design/experience files still carry `status: draft`. They are useful supplemental inputs, but the readiness decision should treat `ux-design.md` as canonical unless Product/UX explicitly promotes the detailed files.

### Warnings

- No missing-UX warning: UX is clearly required and present.
- Warning: route/module-tab IA remains the main UX planning risk. It affects PRD FR-10, FR-23, FR-28, and Epic 11 route work more than it affects already completed baseline shell/projection stories.
- Warning: visual/layout-sensitive stories should cite the richer UX source or add story-local design notes, matching the UX planning file's own instruction.

## Step 5: Epic Quality Review

### Summary

The epics are generally strong for a brownfield framework plan: each epic names a user or maintainer outcome, maintains requirement traceability, and most stories use testable Given/When/Then acceptance criteria. The major quality risk is no longer the earlier Story 2.6 forward dependency; that has been corrected. Remaining issues are concentrated in command-story traceability detail, Epic 11 decomposition/readiness, UX route/IA open questions, and release-governance evidence.

### Epic Structure Validation

| Epic | User Value Focus | Independence | Quality Finding |
| --- | --- | --- | --- |
| Epic 1 Shell Foundation & Bootstrap | Strong adopter-developer value | Stands alone as bootable empty shell | Compliant |
| Epic 2 Read-Only Projection Experience | Strong operator value | Works on Epic 1 only; FC-NIP is delegated without making Epic 2 depend on Epic 9 | Compliant after Story 2.6 cleanup |
| Epic 3 Command Authoring & Lifecycle | Strong operator value | Builds on Epics 1-2 | Compliant with minor AC-detail gaps |
| Epic 4 Safe & Concurrent Command Execution | Strong operator safety value | Builds on Epic 3; no forward dependency | Compliant |
| Epic 5 AI-Agent MCP Surface | Strong AI-agent integrator value | Uses generated manifest; independent of human UI epics where appropriate | Compliant |
| Epic 6 Customization & Extensibility | Strong adopter-developer value | Builds on generated baseline; no future dependency | Compliant |
| Epic 7 Authoring Tooling & Drift Safety | Strong adopter-developer value | Independent tooling layer | Compliant |
| Epic 8 Aspire-grade Visual Refresh | Operator/adopter UX value | Stories are independently shippable refinements | Compliant, though several ACs are more design-governance than user-flow oriented |
| Epic 9 Fresh-Row Producer and Row Identity | Operator value | Builds on Epics 2-3; Story 9.2 is done per sprint status | Compliant |
| Epic 10 Tooling Governance Follow-Through | Adopter-developer/release confidence value | Builds on Epic 7; complete per sprint status | Acceptable for framework governance, not an end-user feature epic |
| Epic 11 Architecture Review Remediation | Adopter/operator/security value, but heavily technical | Decision gates 11.0 and 11.8 are done; implementation order is explicit | Needs decomposition discipline before dev for 11.17-11.19 and several broad remediation stories |

### Dependency Analysis

- No remaining Epic N requiring Epic N+1 was found after the Story 2.6 correction.
- Epic 2 explicitly avoids depending on Epic 9 by limiting Story 2.6 to live refresh/reconnect/reconciliation and assigning row-level fresh indicators to Epic 9 / Story 9.2.
- Epic 9 correctly depends backward on command/projection runtime context from Epics 2-3.
- Epic 10 correctly depends backward on Epic 7 and is complete.
- Epic 11 has a non-heading implementation order. The order table is necessary and correct, but it remains a process hazard: story creation must follow the table, not file order or numeric heading order.
- Release Governance Gate RG-1 is not an epic dependency, but `REL-AI-1` remains open with implementation story `REL-1`; it blocks v1.0 RC evidence readiness.

### Critical Violations

None found in the current corrected planning set.

The earlier critical Story 2.6 forward-dependency issue appears resolved: Story 2.6 no longer claims automatic row-level fresh marking from projection nudges, and Story 9.2 is marked done in sprint status.

### Major Issues

1. Epic 11 contains decomposition-parent stories that are not ready for direct implementation.

Affected stories:
- Story 11.17 Mechanical one-type-per-file split
- Story 11.18 LoggerMessage migration for warnings and hot paths
- Story 11.19 Enforcement and policy alignment

Issue: these are explicitly marked "Split-before-dev" and contain multiple independently reviewable workstreams. They are valid backlog containers, but not implementation-ready user stories.

Recommendation: split them by package or defect class before moving any child work to ready-for-dev. Keep each child story tied to a specific validation lane.

2. Some command PRD subclauses are not explicit enough in story AC.

Affected PRD requirements:
- FR-14 command form validation/parsing/form-state preservation
- FR-15 IdempotentConfirmed, NeedsReview, warning-state lifecycle outcomes
- FR-16 exact authorization sequencing around `BeforeSubmit`

Issue: architecture and sprint status show some of these behaviors are implemented or documented, but the epic/story AC does not consistently pin each PRD subclause.

Recommendation: before creating new command lifecycle or safety stories, add a trace note or acceptance refinement that explicitly pins these subclauses. If already covered by implementation tests, cite the tests in the story or release evidence.

3. Release evidence is a gate, not yet a completed story.

Affected requirement:
- FR-24

Issue: FR-24 is mapped to RG-1 / `REL-AI-1`, and sprint status correctly records `REL-AI-1` as open with implementation story `REL-1`. This is not an epic-coverage miss, but it is not implementation-ready for RC until evidence exists.

Recommendation: create or complete `REL-1` before v1.0 RC classification. Evidence must include package inventory, signed packages, symbols, SBOM, checksums, sealed release manifest, GitHub Release or dry-run assets, and package-consumer validation.

4. UX route/module-tab decisions remain open for future navigation and route work.

Affected areas:
- UX Experience open questions 1-3
- Epic 8 projection flyout / one-entry-per-module reconciliation
- Epic 11 route implementation Story 11.7

Issue: current architecture supports the existing navigation rail and route contract, but the detailed UX spine still asks how module tabs are named, how tab selection is encoded in routes, and whether projection flyout remains.

Recommendation: require Story 11.7 or the next navigation story to record a story-local IA decision before dev starts.

### Minor Concerns

- Some completed Epic 8 and Epic 10 acceptance criteria use abbreviated Given/Then forms rather than strict Given/When/Then. They are mostly already implemented, but future stories should restore strict BDD form.
- Epic 10 is governance/process-heavy. It is acceptable for a framework release plan because the beneficiary is the adopter developer and release owner, but future product-facing plans should avoid letting process automation masquerade as user product capability.
- Story 11.4 and Story 11.5 are broad but still have coherent defect-class boundaries. They should keep their "independently verifiable task groups" and guard-first constraints when created.

### Special Implementation Checks

- Starter-template requirement: not applicable. This is a brownfield framework repository, not a greenfield app scaffold.
- Database/entity creation timing: not applicable. FrontComposer is source-generation, Shell, MCP, CLI, and Testing tooling; it does not own application data tables.
- Brownfield indicators: present and strong. The plan explicitly covers existing generated output, EventStore integration, package/public API compatibility, sprint-change proposals, and architecture review remediation.
- Submodule boundaries: correctly treated as external dependencies; no nested submodule work is planned.

### Best Practices Compliance Checklist

| Check | Result |
| --- | --- |
| Epics deliver user/adopter/operator/release-owner value | Pass, with governance-heavy Epic 10 noted |
| Epics can function independently | Pass after Story 2.6 cleanup |
| Stories are appropriately sized | Mostly pass; Epic 11.17-11.19 must be split before dev |
| No forward dependencies | Pass |
| Database tables created when needed | N/A |
| Acceptance criteria are clear and testable | Mostly pass; partial command AC traceability and some abbreviated BDD forms remain |
| Traceability to FRs maintained | Pass with 3 partial FR traceability notes |

## Summary and Recommendations

### Overall Readiness Status

NEEDS WORK

The planning set is substantially improved and no longer has a critical document-discovery or Story 2.6 forward-dependency blocker. It is not clean enough to call READY because release evidence remains open, Epic 11 still has backlog stories that need split/readiness discipline, and several command PRD subclauses are only partially explicit in story AC.

### Critical Issues Requiring Immediate Action

No critical violations remain in the corrected planning set.

### Major Issues Requiring Action

1. Complete or create `REL-1` for the open `REL-AI-1` release evidence gate before v1.0 RC readiness classification.
2. Split Story 11.17, Story 11.18, and Story 11.19 before moving them to ready-for-dev; they are decomposition parents, not implementable stories.
3. Add explicit story trace or test-evidence citations for PRD FR-14, FR-15, and FR-16 subclauses that are currently only partially visible in AC.
4. Resolve module-tab route encoding and projection-flyout IA decisions before Story 11.7 or future navigation-route implementation starts.

### Recommended Next Steps

1. Route `REL-1` and define the required evidence outputs: package inventory, signed package verification, symbols, SBOM, checksums, sealed manifest, GitHub Release/dry-run assets, and package-consumer validation.
2. Before Epic 11 implementation, create child stories from 11.17-11.19 by package or defect class, each with its validation lane named.
3. Patch `epics.md` or the next command-related story files with explicit AC trace for nullable/culture-aware command fields, pre-accept form-state preservation, IdempotentConfirmed/NeedsReview/warning states, and authorization sequencing.
4. Record a story-local UX/architecture decision for module tab route encoding and whether projection flyout remains as a secondary navigation pattern.
5. Keep using `ux-design.md` as the canonical UX source and the promoted detailed UX files as supplemental design references until Product/UX promotes them.

### Final Note

This assessment identified 7 issues requiring attention across 4 categories: release governance, story decomposition, FR traceability, and UX/route alignment. Address the major issues before claiming implementation readiness for the v1.0 release candidate. The artifacts are usable for continued backlog/story work if those constraints are carried forward explicitly.

Assessment completed on 2026-07-05 by Codex via `bmad-check-implementation-readiness`.
