# Epic 12: Release Certification & Evidence Alignment

Release owners can certify v1 readiness from row-level ledger state, provider-backed runtime evidence, trusted release-context evidence, manual accessibility logs, and stakeholder acceptance rather than inferring readiness from completed story statuses.

This epic is the release-certification route for the Epic 11 retrospective findings recorded in `_bmad-output/implementation-artifacts/epic-11-retro-2026-05-13.md` and the approved sprint change proposal `_bmad-output/planning-artifacts/sprint-change-proposal-2026-05-13.md`.

It does not reopen completed Epics 1-11 by default. It aligns the evidence needed to decide whether the v1 release is ready, blocked, or ready with explicitly accepted constraints.

### Story 12.1: Ledger Marker Parity and Epic Status Decision

As a release owner,
I want current deferred-work ledger markers reconciled with completed story evidence,
So that top-level epic status reflects the real release-readiness state.

**Acceptance Criteria:**

**Given** `_bmad-output/implementation-artifacts/deferred-work.md`
**When** Story 12.1 audits current reconciliation markers
**Then** current `Reconciliation:` markers for Stories 11.2, 11.4, and 11.5 are counted from the detailed rows
**And** the count is compared against the Epic 11 story completion notes and retrospective findings.

**Given** a current marker still names a completed Story 11.x owner
**When** the row is reconciled
**Then** the row is converted to exactly one current final state: `Resolved`, `Accepted constraint`, `Split to named story`, `Superseded`, `Non-action decision`, or deliberately open release gate
**And** the evidence, owner, downstream impact, and reopen trigger are recorded where the state is not a fix.

**Given** `epic-11` remains `in-progress` in sprint status
**When** ledger parity is complete
**Then** the story records a decision to mark `epic-11` as `done` or keep it `in-progress`
**And** any blocking rows are named explicitly.

**References:** epic-11-retro-2026-05-13.md, deferred-work.md, sprint-status.yaml.

---

### Story 12.2: MCP Ledger Closure and Contract Snapshot Decisions

As an agent integrator,
I want Story 11.5 ledger closure to match MCP contract evidence,
So that schema negotiation and agent contract readiness are not overstated.

**Acceptance Criteria:**

**Given** Story 11.5 is marked `done`
**When** Story 12.2 reviews all remaining Story 11.5 current ledger markers
**Then** the markers are reconciled row by row or through an explicit row-scoped closure matrix
**And** no broad range-only closure is accepted without row-addressable evidence.

**Given** an MCP row is accepted as a v1 constraint
**When** the release evidence is recorded
**Then** the acceptance names owner, likelihood, impact, downstream consumer impact, evidence, and reopen trigger.

**Given** a row represents genuine unresolved MCP work
**When** the row cannot be closed in Story 12.2
**Then** it is split to a named backlog item or release gate instead of remaining hidden under Story 11.5.

**References:** 11-5-mcp-schema-negotiation-and-agent-contract-hardening.md, deferred-work.md.

---

### Story 12.3: EventStore Pending-Command Provider Release Gate

As a release owner,
I want pending-command status provider readiness resolved,
So that command lifecycle confidence is based on provider-backed behavior.

**Acceptance Criteria:**

**Given** the current runtime has the `IPendingCommandStatusQuery` seam/null provider
**When** release readiness is assessed
**Then** the project decides whether to implement provider-backed pending-command status before v1 or accept a named release constraint.

**Given** provider-backed status is implemented
**When** provider status behavior is validated
**Then** 202, 200 terminal, 304, 429, 503, malformed, duplicate, stale, and provider-exception cases are covered with redacted evidence.

**Given** provider-backed status is accepted as a constraint
**When** release documentation is prepared
**Then** release notes and docs state the limitation, owner, downstream impact, and reopen trigger.

**References:** 11-7-eventstore-reliability-and-ci-governance-follow-ups.md, epic-11-retro-2026-05-13.md.

---

### Story 12.4: Trusted Release Evidence Dry Run

As a maintainer,
I want release evidence proven in a trusted context,
So that signing, SBOM, checksums, symbols, attestations, package inventory, and publication ordering cannot produce a false release-ready record.

**Acceptance Criteria:**

**Given** release workflows can trigger irreversible side effects
**When** trusted-context validation runs
**Then** blocking CI evidence, package inventory, SBOM, checksum, signing, symbol, attestation, redaction, and ordering checks complete before publish/tag/release mutation.

**Given** a credential, signing, attestation, or publication path is unavailable
**When** the dry run completes
**Then** the missing path is recorded as an explicit release blocker or approved fallback with owner and evidence.

**Given** release evidence is generated
**When** evidence is committed or attached to release-readiness notes
**Then** it is bounded, sanitized, and free of tokens, credentials, tenant/user values, local absolute paths, raw response bodies, and unbounded workflow logs.

**References:** epic-10-retro-2026-05-10.md, 11-7-eventstore-reliability-and-ci-governance-follow-ups.md.

---

### Story 12.5: Accessibility and Stakeholder Acceptance Evidence Pack

As a product and quality owner,
I want manual accessibility and stakeholder acceptance evidence captured,
So that release readiness includes the non-automated gates promised by the PRD and UX spec.

**Acceptance Criteria:**

**Given** the UX spec requires manual screen-reader and real-device verification before release branches
**When** Story 12.5 prepares the evidence pack
**Then** NVDA, JAWS, VoiceOver, and real-device verification status is recorded as completed, blocked, or explicitly accepted with release impact.

**Given** broader accessibility scope includes cross-AT, localization, RTL, zoom, forced-colors, and reduced-motion evidence
**When** the release pack is reviewed
**Then** each area is classified as v1 blocker, accepted v1 constraint, or post-v1 roadmap with owner and evidence.

**Given** stakeholder acceptance is required before v1 readiness is claimed
**When** Story 12.5 closes
**Then** stakeholder acceptance status is recorded in repository artifacts with any open feedback or release conditions.

**References:** ux-design-specification/responsive-design-accessibility.md, epic-11-retro-2026-05-13.md.

---

**Epic 12 Summary:**

- 5 backlog stories converting post-Epic-11 retrospective findings into release-certification work.
- No completed story is reopened by default.
- Primary inputs are `_bmad-output/implementation-artifacts/deferred-work.md`, `_bmad-output/implementation-artifacts/epic-11-retro-2026-05-13.md`, and `_bmad-output/planning-artifacts/sprint-change-proposal-2026-05-13.md`.
