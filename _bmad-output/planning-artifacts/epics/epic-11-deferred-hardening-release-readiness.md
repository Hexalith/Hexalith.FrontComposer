# Epic 11: Deferred Hardening & Release Readiness

Product, engineering, and quality stakeholders can close the deferred-work ledger through owned backlog stories instead of leaving release-critical hardening scattered across review notes.

This epic is the backlog route for unresolved items recorded in `_bmad-output/implementation-artifacts/deferred-work.md` as of 2026-05-10. It does not reopen completed Epics 1-10; it groups carry-forward work into release-readiness stories that can be prioritized, split, or closed with evidence.

### Story 11.1: Deferred Work Ledger Reconciliation and Ownership

As a product owner,
I want the deferred-work ledger reconciled into owned backlog entries,
So that completed stories do not leave ambiguous future work behind.

**Acceptance Criteria:**

**Given** `_bmad-output/implementation-artifacts/deferred-work.md`
**When** Story 11.1 is complete
**Then** every unresolved deferred item has one of: a linked active backlog story, a superseded/resolved marker with evidence, or a documented non-action decision
**And** entries that say "future", "follow-up", or "v1.x" without an owner are tightened to a concrete owner surface

**Given** story and retrospective records
**When** deferred items are reconciled
**Then** duplicate entries are merged or cross-referenced
**And** resolved duplicates keep the original review source for auditability

**References:** process-notes/story-creation-lessons.md, deferred-work.md

---

### Story 11.2: Diagnostic Registry and Documentation Governance Follow-ups

As a framework maintainer,
I want diagnostic registry, docs stub, and deprecation governance follow-ups closed,
So that diagnostic IDs remain a reliable release surface.

**Acceptance Criteria:**

**Given** deferred items from Story 9.4 and related Epic 9 reviews
**When** governance hardening is implemented
**Then** registry schema validation covers suppression row shape, unsupported schema failure flow, external boundary/range policy, docs slug containment, sample fixture schema, related IDs, introduced-in accuracy, and diagnostic title/prose authoring
**And** HFCM migration IDs have a documented registry/release-row strategy that does not require broad Roslyn release-tracking suppression

**Given** diagnostic docs and registry data
**When** release validation runs
**Then** docs host canonicalization, schema version policy, cross-package range exceptions, package validation placement, and compatibility suppression governance are test-backed

**References:** deferred-work.md sections for 9-4, 9-5, diagnostic registry, and docs site follow-ups.

---

### Story 11.3: CLI, Migration, and IDE Edge-Case Hardening

As a developer,
I want the CLI migration and IDE parity surfaces hardened against documented edge cases,
So that release tooling behaves predictably outside the happy path.

**Acceptance Criteria:**

**Given** deferred items from Stories 9.2 and 9.3
**When** CLI/IDE hardening is complete
**Then** path canonicalization, symlink/junction behavior, solution parsing, sidecar path normalization, strict manifest parsing, duplicate JSON key handling, and generator-debug guidance enforcement are either fixed or explicitly rejected with evidence
**And** README/help text documents flag precedence, JSON payload meaning, and known non-applicable diff limitations

**Given** migration tooling writes or rewrites files
**When** edge cases are exercised
**Then** write safety, encoding behavior, import limitations, and user-facing error messages are covered by focused tests or documented fail-closed behavior

**References:** deferred-work.md sections for 9-2 and 9-3.

---

### Story 11.4: Drift Detection and Source Generator Coverage Hardening

As a framework maintainer,
I want drift detection and generator coverage gaps closed or explicitly accepted,
So that build-time diagnostics keep their release promises.

**Acceptance Criteria:**

**Given** deferred items from Story 9.1 and earlier SourceTools stories
**When** source-generator hardening is complete
**Then** PublishAot-only HFC1070 behavior, AC11 performance coverage, metadata drift tests, structural diagnostic comparisons, baseline path checks, badge/destructive/role metadata drift coverage, and parser/transform edge cases have owner decisions and validation evidence

**Given** generated output contract tests
**When** the test suite runs
**Then** deterministic ordering, no local absolute paths, no raw sensitive payloads, and netstandard2.0-safe source-generator behavior remain guarded

**References:** deferred-work.md sections for 9-1, 1-4, 1-5, 1-8, and SourceTools follow-ups.

---

### Story 11.5: MCP Schema Negotiation and Agent Contract Hardening

As an agent integrator,
I want MCP schema negotiation and agent contract deferrals closed,
So that agent command/query behavior is predictable, tenant-safe, and version-aware.

**Acceptance Criteria:**

**Given** deferred items from Epic 8 and Story 8-6a
**When** MCP/schema hardening is complete
**Then** compatible-additive revalidation, corpus aggregate production usage, mixed-algorithm fingerprint handling, descriptor correlation on schema rejection, constructor selection, hidden/stale precedence documentation, and schema-drift diagnostic categorization are addressed or explicitly deferred beyond v1 with rationale

**Given** MCP runtime and build-time artifacts
**When** cross-surface validation runs
**Then** tenant scoping, hallucination rejection, schema mismatch behavior, lifecycle result categories, and skill corpus fingerprint assumptions are covered by tests or documented release constraints

**References:** deferred-work.md sections for 8-1 through 8-6a.

---

### Story 11.6: Shell UX, Accessibility, and Sample Coverage Follow-ups

As a UX and quality owner,
I want shell, accessibility, and sample-domain deferrals routed into one release-readiness stream,
So that user-facing polish and sample guidance do not remain scattered across old reviews.

**Acceptance Criteria:**

**Given** deferred items from Epics 2, 3, 4, 6, and 10
**When** shell UX follow-ups are complete
**Then** prerender-to-interactive CTA behavior, command palette/palette E2E follow-ups, field-slot sample guidance, analyzer reference hygiene, visual/localization/RTL/accessibility matrices, and Counter/sample onboarding gaps have owner decisions and evidence

**Given** visual and accessibility specimens
**When** release readiness is assessed
**Then** accepted v1 scope and v1.x/v2 deferrals are explicit, named, and not hidden in individual story review notes

**References:** deferred-work.md sections for 2-2, 3-4, 3-5, 4-6, 6-3, 6-5, 10-2, and sample guidance follow-ups.

---

### Story 11.7: EventStore Reliability and CI Governance Follow-ups

As a release owner,
I want EventStore integration, realtime reliability, and CI governance deferrals closed,
So that release readiness is based on tested provider behavior rather than review-note intent.

**Acceptance Criteria:**

**Given** deferred items from Epic 5 and release/CI reviews
**When** reliability governance is complete
**Then** provider-backed command status integration, EventStore response parity, ETag/429/503 behavior, SignalR/reconnect follow-ups, telemetry/exporter guidance, CI race conditions, submodule checkout constraints, and release-pipeline credential concerns are addressed or explicitly documented as accepted constraints

**Given** release readiness checks
**When** the release candidate is prepared
**Then** deferred CI/test governance items that could block shipping have a pass/fail decision, owner, and evidence location

**References:** deferred-work.md sections for Epic 5, Story 1-7, Story 5-6, Epic 10, and CI/release follow-ups.

---

**Epic 11 Summary:**
- 7 backlog stories converting deferred review findings into owned release-readiness work.
- No completed story is reopened by default; each follow-up can be implemented, split, marked superseded, or consciously deferred with evidence.
- Primary input is `_bmad-output/implementation-artifacts/deferred-work.md`; sprint tracking lives in `_bmad-output/implementation-artifacts/sprint-status.yaml`.
