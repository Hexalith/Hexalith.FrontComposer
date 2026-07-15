---
title: Sprint Change Proposal - Implementation Readiness Planning Reconciliation
status: implemented
date: 2026-07-15
approved: 2026-07-15
implemented: 2026-07-15
approvedBy: Administrator
project: frontcomposer
trigger: implementation-readiness-report-2026-07-15
mode: incremental
scope: moderate
recommendedApproach: direct-adjustment-and-backlog-reorganization
handoffStatus: applied
approvedEditProposals:
  - epic-11-program-restructure
  - canonical-and-sprint-state-reconciliation
  - missing-epic-11-child-materialization
  - canonical-fr-and-acceptance-repair
  - prd-and-architecture-reconciliation
  - ux-authority-and-readiness-gate
---

# Sprint Change Proposal: Implementation Readiness Planning Reconciliation

## 1. Issue Summary

### Problem Statement

The 2026-07-15 Implementation Readiness Assessment found complete semantic requirement coverage but an unsafe execution plan. All 29 canonical functional requirements have an implementation or release-governance path, yet canonical planning is behind the implementation, some release-required Epic 11 children do not have independent story artifacts, and Epic 11 is presented as one heterogeneous technical-remediation epic.

The issue is planning-state divergence and backlog structure, not missing product intent or a newly discovered code failure. Already implemented work must not be rebuilt merely because `epics.md` is stale.

### Trigger And Evidence

The trigger is `_bmad-output/planning-artifacts/implementation-readiness-report-2026-07-15.md`, which reports:

- overall status `NOT READY`;
- 100% semantic FR coverage (29 of 29);
- 43 findings or concerns;
- three critical blockers.

Concrete evidence:

| Evidence | Current conflict |
| --- | --- |
| Canonical `epics.md` | Describes Stories 11.11-11.14 partly as future implementation even though architecture, project context, published migration guidance, and sprint tracking record them as complete. |
| Epic 11 child planning | Full artifacts exist for 11.17a-d and 11.18a, but 11.18b-c and 11.19a-d remain bullet-level or parent-level backlog. |
| Epic 11 structure | Token/auth reliability, realtime, MCP lifetime, security testing, CSS, Testing, routes, package boundaries, layering, logging, and enforcement share one flat epic and one unsafe queue. |
| Release planning | `REL-2` is implemented and done; canonical planning represents it only indirectly. `REL-AI-1` correctly remains open for real-release evidence. |
| Sprint/story status | `sprint-status.yaml` records 11.18a as `in-progress`, while its story header still says `ready-for-dev`. |
| Epic status | Epic 9 remains `in-progress` although Stories 9.1 and 9.2 are done and its retrospective is optional. |
| Traceability | Story citations mix legacy Epic FR numbering with canonical PRD FR numbering. |
| UX provenance | Rich UX supplements are drafts that claim authority, contain broken source paths, and refer to a nonexistent FrontComposer `DESIGN.md`. |

### Change Classification

This is a significant planning correction caused by a misunderstanding of the current artifact state and by technical-debt-oriented epic decomposition. It does not change the product vision, MVP scope, public behavior, or already-approved implementation decisions.

## 2. Impact Analysis

### Epic Impact

| Epic or program | Impact |
| --- | --- |
| Epics 1-8 | No scope or implementation change. Repair canonical FR citations and the identified acceptance wording only. |
| Epic 9 | Product scope remains valid. Mark the epic `done` because both stories are done; keep the retrospective optional. |
| Epic 10 | Preserve as a closed historical delivery program. Correct canonical FR citations; do not retroactively repartition or recreate completed work. |
| Epic 11 | Reclassify as a Release Readiness Remediation Program with four outcome workstreams. Preserve all story IDs and implementation history. |
| Release workstream | Reference completed `REL-2`, superseded `REL-1`, and still-open `REL-AI-1` explicitly. |

No planned epic becomes obsolete. No new product-capability epic is required.

### Epic 11 Current-State Reconciliation

The corrected plan will use these current states as the reconciliation baseline:

| Workstream | Stories | Reconciled state |
| --- | --- | --- |
| Runtime reliability and security | 11.0-11.5 | Done. |
| Runtime reliability and security | 11.18a | In progress. |
| Adopter testing and route correctness | 11.6-11.7 | Done. |
| Contracts and package compatibility | 11.8, 11.11-11.14 | Done. Story 11.14 is documentation, inventory, compatibility, and release evidence. |
| Maintainability and enforcement | 11.9, 11.15-11.16 | Done. |
| Maintainability and enforcement | 11.17a | Done. |
| Maintainability and enforcement | 11.17b-d | Review. |
| Maintainability and enforcement | 11.18b-c | Missing independent story artifacts. |
| Maintainability and enforcement | 11.19a-d | Missing independent story artifacts. |
| Decomposition records | 11.17, 11.18, 11.19 | Non-implementable parents; never promote to `ready-for-dev`. |

The status snapshot must be refreshed from `sprint-status.yaml` when the approved edits are applied. The table above records the proposal baseline, not a permanent replacement for live sprint tracking.

### Story Impact

- Do not recreate or reopen completed stories merely to improve historical story sizing.
- Preserve active 11.18a implementation and the 11.17b-d review work.
- Create only the genuinely missing independent specifications: 11.18b-c and 11.19a-d.
- Move future-story ownership statements out of completed-story acceptance criteria.
- Incorporate admitted acceptance gaps into their owning story text.
- Require every new implementation candidate to carry canonical FR/NFR references, prerequisites, BDD acceptance criteria, and named validation lanes.

### Artifact Conflicts

| Artifact | Required correction |
| --- | --- |
| `prd.md` | Update metadata and dependency version, current implementation language, terminology, WCAG target, and timing traceability. |
| `architecture.md` | Add the missing canonical UX/IA/route invariants section and current package/remediation state. |
| `epics.md` | Reframe Epic 11, synchronize statuses, quarantine legacy FR numbering, replace the coverage map, and repair story acceptance text. |
| `ux-design.md` | Promote accepted IA/accessibility/interaction decisions into the canonical source. |
| Detailed UX supplements | Repair paths, resolve authority, identify the real visual supplement, and document FC-CNC feedback. |
| `epic-11-context.md` | Replace parent-only story inventory with the workstreams and current child artifacts. |
| Story artifacts | Align 11.18a status and create complete 11.18b-c/11.19a-d specifications. |
| `sprint-status.yaml` | Close Epic 9, add individual missing-child entries, reconcile live statuses, and add the readiness-rerun action. |
| Release planning | Reference `REL-2` as done, `REL-1` as superseded, and `REL-AI-1` as open until its evidence rule is met. |

### Technical, Infrastructure, And Deployment Impact

If approved, this proposal authorizes planning and story-artifact edits only. It does not authorize product-code, package, workflow, infrastructure, deployment, submodule, or release-state changes.

The remaining implementation stories retain their existing technical impact and validation requirements. The correction makes those impacts executable and traceable; it does not implement them.

## 3. Recommended Approach

### Options Evaluated

| Option | Viability | Effort | Risk | Result |
| --- | --- | --- | --- | --- |
| Direct adjustment | Viable | Medium | Medium | Correct canonical planning, reorganize the backlog, materialize missing children, and rerun readiness. |
| Potential rollback | Not viable | High | High | Would discard reviewed work, reintroduce defects, and worsen planning divergence. |
| MVP review/reduction | Not required | Medium | High product cost | All 29 FRs are covered; no product goal must be removed. |

### Selected Path

Use direct adjustment with backlog reorganization.

Epic 11 becomes a managed release-remediation program with four outcome workstreams rather than a flat user-value epic. Completed work stays completed, story identifiers remain stable, and only missing release-required children are materialized.

### Rationale

- Preserves implementation momentum and evidence.
- Removes the risk of duplicate or retroactive implementation.
- Makes each remaining work item independently reviewable.
- Aligns PRD, architecture, UX, epics, sprint state, and release evidence without changing product scope.
- Produces a safe input for a fresh implementation-readiness run.

### Timeline And Sequencing Impact

Estimated planning effort is one to two focused working days for artifact edits, child-story materialization, validation, and the readiness rerun. This estimate excludes implementation of the remaining stories.

Active work may finish: 11.17b-d review and 11.18a implementation are not rolled back. Do not start a new 11.18b-c or 11.19a-d implementation from the stale queue or a decomposition parent.

## 4. Detailed Change Proposals

### 4.1 Reframe Epic 11 As A Remediation Program

**Artifact:** `_bmad-output/planning-artifacts/epics.md`

**OLD:**

> `## Epic 11: Architecture Review Remediation`
>
> One flat epic combines unrelated runtime, security, UI, Testing, routing, package, layering, logging, and enforcement work and relies on a separate implementation-order table to overcome unsafe heading order.

**NEW:**

> `## Epic 11 Release Readiness Remediation Program`
>
> Epic 11 is a coordinated technical-risk program, not a single executable user-value epic. Implementation is organized into four independently governed outcome workstreams:
>
> 1. Runtime reliability and security.
> 2. Adopter testing and route correctness.
> 3. Contracts and package compatibility.
> 4. Maintainability and enforcement.
>
> Existing story IDs and history remain stable. The 11.17/11.18/11.19 parent entries are decomposition records and are never implementation candidates.

Replace the old implementation-order table with a current-state workstream table. Remove the claim that Epic 11 is independent of Epic 10 because Story 11.6 consumes Story 10.5 privacy evidence.

**Justification:** resolves the heterogeneous-portfolio blocker without renumbering or duplicating completed work.

### 4.2 Reconcile Canonical And Sprint State

**Artifacts:** `epics.md`, `epic-11-context.md`, `sprint-status.yaml`, affected story headers, release-governance references.

**OLD:**

- Stories 11.11-11.14 are partly future-tense.
- `epic-11-context.md` lists only the 11.17/11.18/11.19 parents.
- Story 11.18a header and sprint status disagree.
- Epic 9 remains in progress after both stories completed.
- `REL-2` is indirect in canonical planning.

**NEW:**

- Record 11.11-11.14 as done and identify 11.14 as documentation/release evidence.
- List 11.17a-d and 11.18a explicitly in `epic-11-context.md` under their workstreams.
- Align Story 11.18a's header to the live sprint state when applying this proposal.
- Mark Epic 9 done; retain its optional retrospective.
- Add a release-workstream record:

  - `REL-2`: done; implemented the FR-24 evidence machinery.
  - `REL-AI-1`: open; closes only on real-release evidence or an approved fallback with reopen criteria.
  - `REL-1`: superseded; never recreate it.

- Treat `sprint-status.yaml` as the live status authority while requiring story headers and canonical summaries to be reconciled on status changes.

**Justification:** completed work must not be rebuilt because canonical planning is stale.

### 4.3 Materialize Missing Epic 11 Children

The following files are created as full independent story specifications. Their parent entries remain non-implementable.

#### Story 11.18b - Warning-And-Above Log Sites

**File:** `_bmad-output/implementation-artifacts/11-18-warning-and-above-log-sites.md`

**Story:** As a FrontComposer operator, I want residual operational Warning+ logs to use source-generated, support-safe events so that warnings remain diagnosable and efficient without exposing sensitive data.

Minimum BDD criteria:

- Given the final 11.18a security census and the 11.18c hot-path ownership rule, when the remaining Warning+ census is frozen, then every site has exactly one owner and the denominator cannot silently shrink.
- Given an in-scope Warning+ branch, when migrated, then it uses `[LoggerMessage]`, preserves severity/branch behavior, and uses collision-free event IDs.
- Given adversarial token, payload, path, tenant/user, correlation, and exception values, when logs are captured, then raw support-sensitive values and stack traces are absent.
- Given the Shell unit and Governance lanes, when validation runs, then direct Warning+ calls in the frozen scope fail the guard and synthetic negatives prove non-vacuity.

Validation: Release build, Shell unit lane, Shell Governance lane, sanitization tests, artifact validation, and diff hygiene.

#### Story 11.18c - Hot-Path Log Sites

**File:** `_bmad-output/implementation-artifacts/11-18-hot-path-log-sites.md`

**Story:** As an operator, I want command-lifecycle, projection-refresh, and polling hot-path logging to be source-generated and bounded so that observability does not destabilize realtime behavior.

Minimum BDD criteria:

- Given command lifecycle, projection refresh, and polling sources, when the hot-path census is frozen, then it excludes 11.18a security sites and sites owned by 11.18b.
- Given an in-scope hot path, when logging is migrated, then cadence, retry, state transitions, cancellation, and exactly-once behavior remain unchanged.
- Given disabled log levels, when the hot path executes, then expensive formatting, hashing, joining, or serialization is not evaluated before an enabled check.
- Given the final direct-call ledger, when Governance runs, then every remainder is below threshold or has a documented rationale.

Validation: lifecycle/realtime/polling focused tests, LoggerMessage Governance guard, applicable benchmark evidence, Release build, and artifact validation.

The deterministic ownership precedence is:

`security/fail-closed (11.18a) -> hot paths (11.18c) -> residual Warning+ (11.18b)`.

#### Story 11.19a - Doc-Comment Enforcement Realignment

**File:** `_bmad-output/implementation-artifacts/11-19-doc-comment-enforcement-realignment.md`

**Story:** As a package maintainer, I want CS1591 enforced on the Contracts public API-freeze surface so that published API documentation policy is real rather than aspirational.

Minimum BDD criteria:

- Given the Contracts public API-freeze folders, when a public member lacks required documentation, then the scoped Release/TWAE build fails.
- Given non-freeze implementation folders, when they build, then this story does not broaden CS1591 unexpectedly.
- Given the policy guard, when a synthetic undocumented public API is supplied, then non-vacuous evidence proves enforcement.
- No global warning suppression or analyzer disable is allowed.

#### Story 11.19b - AppHost NuGet Audit Suppression

**File:** `_bmad-output/implementation-artifacts/11-19-apphost-nuget-audit-suppression.md`

**Story:** As a release owner, I want advisory-specific NuGet audit suppressions so that accepted vulnerabilities are explicit, reviewable, and removable.

Minimum BDD criteria:

- Given the blanket NU1902-NU1904 suppression, when the policy is corrected, then only named advisories use `NuGetAuditSuppress`.
- Given an unapproved advisory, when CI audit runs, then it is not hidden by a blanket `NoWarn` or disabled audit.
- Given every accepted suppression, then owner, rationale, review date, and removal condition are recorded.

#### Story 11.19c - Localization And Identifier Alignment

**File:** `_bmad-output/implementation-artifacts/11-19-localization-and-identifier-alignment.md`

**Story:** As an operator and adopter, I want shell accessibility copy, host language metadata, and diagnostic identifiers aligned so that localization and public diagnostic compatibility remain consistent.

Minimum BDD criteria:

- Localize `FcHomeCard` accessible copy and UI-host language/English strings through the existing resource model.
- Preserve EN/FR resource parity and accessible-name behavior.
- Rename `HFC2106_ThemeHydrationEmpty` while retaining the HFC2106 ID string and an obsolete alias if the constant is public.
- Validate Shell localization/Governance and diagnostic-catalog parity.

#### Story 11.19d - Analyzer Elevation Decision

**File:** `_bmad-output/implementation-artifacts/11-19-analyzer-elevation-decision.md`

**Story:** As an Architect, I want an evidence-backed decision on `AnalysisMode=Recommended` so that analyzer elevation is intentional and its burn-down cost is owned.

Minimum BDD criteria:

- Record the current warning census and affected projects under the candidate mode.
- Choose adopt, defer, or narrow; name owner, decision date, rationale, and reopen criteria.
- If adopted, create separately scoped implementation stories with validation lanes.
- Do not add third-party analyzers, enable broad churn in this decision story, or suppress resulting findings globally.

**Justification:** every release-required child becomes independently implementable, while parents remain safe decomposition records.

### 4.4 Repair Canonical Traceability And Acceptance Criteria

**Artifact:** `_bmad-output/planning-artifacts/epics.md`

**OLD:**

Legacy and canonical FR numbers share the same `FR-*` syntax, and admitted acceptance gaps remain in later tables.

**NEW:**

1. Relabel the historical capability map as `LEGACY-FR-*`; it is non-executable provenance.
2. Replace the old coverage map with the readiness report's canonical 29-row semantic matrix.
3. Apply canonical epic coverage:

| Epic/program | Canonical PRD coverage |
| --- | --- |
| Epic 1 | FR-7-FR-9, FR-23 |
| Epic 2 | FR-1, FR-3, FR-10-FR-13 |
| Epic 3 | FR-2, FR-4, FR-14-FR-15 |
| Epic 4 | FR-16 |
| Epic 5 | FR-17-FR-19 |
| Epic 6 | FR-3, FR-5 |
| Epic 7 | FR-6, FR-20-FR-22, FR-25 |
| Epic 8 | Refinements to FR-8-FR-11 |
| Epic 9 | FR-13, FR-26 |
| Epic 10 | FR-20-FR-23, FR-27 |
| Epic 11 program | FR-7, FR-10, FR-12, FR-19, FR-22, FR-25, FR-28, FR-29 |

4. Update the owning stories:

| Story | Required edit |
| --- | --- |
| 1.5 | Required component documentation is a completion gate; a gap needs a dated, owned blocking follow-up. |
| 2.6 | Move Epic 9 ownership out of ACs into a historical dependency/delivery note. |
| 3.1 | State seven non-page generated artifacts plus optional full-page `CommandPage`. |
| 3.4 | Name `IdempotentConfirmed`, `NeedsReview`, `Warning`, and `Degraded`. |
| 4.4 | Require authorization before `BeforeSubmit` and again afterward for protected commands. |
| 4.5 | Pin retry to `1 x 250 ms` and link the budget contract. |
| 5.2 | Add subscribe/poll behavior across separate request scopes. |
| 6.2 | Place HFC1038-HFC1041 at call-site/registration/startup/runtime rather than SourceTools build time. |
| 6.3 | State Level 4 -> Level 2 -> generated default; Level 3 composes only through delegated renderers. |
| 7.5 | Distinguish configurable command/query outcomes from the evidence-only fault recorder. |
| 8.3 | Add deterministic logo and no-logo BDD scenarios. |
| 8.4 | Replace approximate row height with the implemented compact `32px` grid metric and pinned sticky header. |
| 8.5 | Refer to the central Fluent version; mark the completed composite as history, not a future sizing template. |
| 9.1 | Convert open decision branches into a closed decision record. |

5. Resolve remaining `or equivalent`, approximate, and conditional alternatives before `ready-for-dev`. Completed stories state the implemented choice.

**Justification:** the canonical PRD becomes the sole executable traceability source.

### 4.5 Reconcile PRD And Architecture

#### PRD

**OLD:**

- `updated: 2026-07-05` despite later material changes.
- Runtime constraint names Roslyn 5.3.0.
- Completed REL-2 and 11.11-11.14 work is partly future-tense.
- Terminology and accessibility targets are incomplete.

**NEW:**

- Set `updated: 2026-07-15`.
- Use repository-pinned Roslyn 5.6.0.
- Record REL-2 and 11.11-11.14 as completed; retain REL-AI-1 and remaining 11.18/11.19 children as open.
- State WCAG 2.2 AA for the common shell and generated module pages.
- Add the terminology contract:

  - Module: primary application-navigation/workspace unit.
  - Bounded context: generated domain-registration and routing scope.
  - Default v1 mapping: one module per registered `DomainManifest`/bounded context unless the host explicitly supplies grouping.
  - Projection: secondary read-model destination inside a module workspace.
  - Module tab: route-backed page at `/{module}/{tab}`.

- Put the known command budgets into traceable requirements: degraded at 10,000 ms, polling every 1,000 ms up to 120,000 ms, and transient retry `1 x 250 ms`.
- Require other performance claims to cite named benchmark/test thresholds.

#### Architecture

**OLD:**

The planning source says UX/layout policy lives in architecture section 4, but no such section exists in the canonical planning artifact.

**NEW:**

Add `## UX, IA, And Route Invariants` containing:

- one primary shell entry per module;
- a required default module tab;
- `/{module}/{tab}` tab routes;
- projection flyouts as secondary navigation only;
- `/commands/{BoundedContext}/{CommandTypeName}` generated command routes;
- WCAG 2.2 AA and Fluent UI v5 governance;
- EventStore acceptance versus projection-confirmed truth;
- FC-CNC one-at-a-time execution.

Record the implemented Contracts/Contracts.UI boundary and completed 11.11-11.14 evidence as current architecture. Replace the flat Epic 11 narrative with the four approved workstreams.

**Justification:** readiness can use the canonical PRD and architecture without following missing sections or stale future-state text.

### 4.6 Resolve UX Authority And Add The Readiness Gate

**Artifacts:** `ux-design.md`, `ux-design-detailed-2026-07-05.md`, `ux-experience-2026-07-05.md`, `sprint-status.yaml`.

**OLD:**

- Two rich UX documents are drafts but claim conflict authority.
- Their `../../...` paths resolve incorrectly.
- `DESIGN.md` is named as visual authority but does not exist in FrontComposer.
- FC-CNC does not specify blocked-submit interaction.
- No tracked action requires a readiness rerun.

**NEW:**

- Promote accepted IA, accessibility, and interaction decisions into canonical `ux-design.md`.
- Mark the rich documents `accepted-supplement`; canonical `ux-design.md` wins conflicts.
- Replace broken paths with repository-root-relative planning paths.
- Name `ux-design-detailed-2026-07-05.md` as the visual/style supplement instead of `DESIGN.md`.
- Add FC-CNC behavior:

  - a second local submission never dispatches;
  - busy/disabled semantics and support-safe feedback are accessible;
  - feedback is announced without a noisy live region;
  - focus is preserved or restored predictably;
  - submission becomes available when the blocking command leaves its in-flight state.

- Reference PRD command budgets and require named test/benchmark evidence for other responsiveness claims.
- Add a sprint action owned by Product Owner + Architect + Developer to rerun implementation readiness after all approved planning edits, story materialization, and status reconciliation.

**Justification:** resolves UX provenance and authority and creates a verifiable exit from `NOT READY`.

## 5. Implementation Handoff

### Scope Classification

**Moderate.** This requires backlog reorganization and coordinated planning updates, but not a fundamental product replan or code rollback.

### Recipients And Responsibilities

| Recipient | Responsibilities |
| --- | --- |
| Product Owner | Own canonical PRD/epic scope, approve current-state language, ensure no completed work is recreated, and accept the readiness rerun result. |
| Developer | Apply approved planning edits, create the six child story artifacts, reconcile story headers/context/sprint state, and run artifact validation. |
| Architect | Confirm the four-workstream structure, terminology/route invariants, and Story 11.19d decision framing. |
| UX owner | Approve canonical UX authority, FC-IA-1 promotion, WCAG target, and FC-CNC interaction details. |
| Test Architect | Validate each child story's lane and rerun implementation readiness after reconciliation. |
| Release Owner | Keep REL-AI-1 open until every FR-24 evidence path or approved fallback/reopen rule is recorded. |

### Implementation Sequence

1. Snapshot current live sprint/story status without modifying product code.
2. Update `epics.md` into the four workstreams and canonical traceability model.
3. Reconcile PRD, architecture, and UX authority/current-state content.
4. Create full 11.18b-c and 11.19a-d story artifacts.
5. Reconcile `epic-11-context.md`, story headers, Epic 9, release records, and `sprint-status.yaml`.
6. Validate artifact paths, story/status agreement, BDD criteria, and YAML/Markdown integrity.
7. Rerun the Implementation Readiness workflow against the corrected canonical set.
8. Start new implementation only from an independent story whose status, prerequisites, acceptance criteria, and evidence contract agree across artifacts.

### Success Criteria

The correction is successful when:

- Canonical planning references completed REL-2 and retains open REL-AI-1 evidence closure.
- Every release-required Epic 11 item is either complete with evidence or represented by an independent implementable child story.
- 11.17/11.18/11.19 parents are not implementation candidates.
- PRD, architecture, UX, epics, project context, story headers, and sprint tracking agree on current state.
- Story-level citations use canonical PRD numbering; legacy numbering is isolated as history.
- The named acceptance gaps are repaired in their owning stories.
- UX provenance, authority, visual source, terminology, routes, WCAG target, and FC-CNC feedback are explicit.
- Active 11.17b-d and 11.18a work is preserved.
- A fresh readiness assessment no longer reports any of the three critical planning blockers.

## 6. Change Navigation Checklist Record

### Section 1 - Understand The Trigger And Context

- [x] 1.1 Trigger identified: 2026-07-15 Implementation Readiness Assessment.
- [x] 1.2 Core problem defined: planning-state divergence and unsafe remediation decomposition.
- [x] 1.3 Evidence collected from PRD, epics, architecture, UX, implementation artifacts, sprint status, and release stories.

### Section 2 - Epic Impact Assessment

- [x] 2.1 Current epic evaluated.
- [x] 2.2 Epic 11 program/workstream changes defined.
- [x] 2.3 Remaining epics reviewed.
- [x] 2.4 No obsolete product epic or new capability epic required.
- [x] 2.5 Priority/order changes defined.

### Section 3 - Artifact Conflict And Impact Analysis

- [x] 3.1 PRD impact assessed; MVP remains achievable.
- [x] 3.2 Architecture conflicts and missing canonical IA section identified.
- [x] 3.3 UX authority, provenance, terminology, accessibility, and concurrency gaps identified.
- [x] 3.4 Sprint, story, release, and readiness artifacts assessed.

### Section 4 - Path Forward Evaluation

- [x] 4.1 Direct adjustment viable: medium effort, medium risk.
- [N/A] 4.2 Rollback rejected as harmful and unnecessary.
- [N/A] 4.3 MVP reduction not required.
- [x] 4.4 Direct adjustment plus backlog reorganization selected.

### Section 5 - Proposal Components

- [x] 5.1 Issue summary complete.
- [x] 5.2 Epic and artifact impacts documented.
- [x] 5.3 Recommended approach and alternatives documented.
- [x] 5.4 MVP impact and action sequence documented.
- [x] 5.5 Moderate-scope handoff defined.

### Section 6 - Final Review And Handoff

- [x] 6.1 Applicable checklist sections addressed.
- [x] 6.2 Complete proposal reviewed; user selected Continue on 2026-07-15.
- [x] 6.3 Administrator explicitly approved the proposal for implementation on 2026-07-15.
- [x] 6.4 Sprint status, story headers, canonical summaries, and the readiness action were reconciled during approved downstream application.
- [x] 6.5 Moderate-scope handoff is recorded with owners, sequence, and success criteria.

## 7. Approval Record

The six incremental edit proposals were approved by Administrator on 2026-07-15. The complete proposal was reviewed and continued on 2026-07-15. Administrator explicitly approved the complete proposal for implementation on 2026-07-15.

The approved change is classified as **Moderate**. Approval authorizes the planning and story-artifact corrections defined by this proposal; it does not itself mark those downstream edits complete.

## 8. Workflow Execution Log

| Date | Event | Result |
| --- | --- | --- |
| 2026-07-15 | Readiness trigger accepted | Correct Course workflow opened for the three critical planning blockers. |
| 2026-07-15 | Repository and workflow instructions loaded | Project rules, BMad configuration, Correct Course checklist, and canonical/current artifacts were reviewed. |
| 2026-07-15 | Incremental edit review | All six edit proposals were individually approved. |
| 2026-07-15 | Complete proposal review | Administrator selected Continue. |
| 2026-07-15 | Final approval | Administrator answered `yes`; proposal status changed to `approved-for-implementation`. |
| 2026-07-15 | Scope classification | Moderate: direct adjustment plus backlog reorganization; no rollback or MVP reduction. |
| 2026-07-15 | Handoff | Routed to Product Owner and Developer for artifact execution, with Architect, UX owner, Test Architect, and Release Owner review/ownership as defined below. |
| 2026-07-15 | Approved changes applied | Canonical PRD, architecture, UX, epics, Epic 11 context, sprint state, and six missing child-story artifacts were reconciled without product-code or submodule edits. |
| 2026-07-15 | Post-correction readiness rerun | `READY`: 29/29 FR coverage, no critical or major findings, three controlled minor program/ordering concerns. |

### Handoff Record

- **Primary executors:** Product Owner and Developer.
- **Architect review:** Four-workstream structure, terminology and route invariants, and the Story 11.19d decision framing.
- **UX review:** Canonical UX authority, FC-IA-1 promotion, WCAG 2.2 AA, and FC-CNC interaction details.
- **Test Architect review:** Child-story lane validation and the post-reconciliation Implementation Readiness rerun.
- **Release Owner:** REL-AI-1 remains open until FR-24 evidence or an approved fallback/reopen rule is recorded.
- **Artifacts modified during approved application:** canonical `prd.md`, `architecture.md`, `epics.md`,
  `ux-design.md`, both accepted UX supplements, `epic-11-context.md`, `sprint-status.yaml`, the six
  11.18b-c/11.19a-d child stories, this proposal, and the post-correction readiness report.
- **Immediate next action:** Validate the next eligible story in a fresh workflow context; execute
  11.18c's semantic hot-path scope freeze before 11.18b production edits.
- **Completion gate:** Met. The rerun reports none of the three critical planning blockers. REL-AI-1
  remains open under its independent real-release evidence contract and the REL-4 freeze remains in
  force.

**Correct Course workflow status:** Implemented. Approved planning/story corrections and the
readiness rerun are complete; product implementation and release authorization remain separately
governed.

## 9. Application Record

The approved proposal was applied on 2026-07-15 as planning/story-artifact work only. It made no
product-code, workflow, infrastructure, package, release-state, or submodule change.

Applied outcomes:

- canonical planning and current delivery state are reconciled, including completed Epic 9, REL-2,
  Stories 11.11–11.14, and the active/review/future Epic 11 child states;
- legacy requirement identifiers are provenance-only and canonical FR-1–FR-29 traceability is
  explicit;
- Epic 11 is governed through four workstreams and parents 11.17–11.19 cannot enter the queue;
- Stories 11.18b–c and 11.19a–d exist as complete independent artifacts with BDD criteria and named
  validation lanes;
- UX authority, IA/routes, WCAG 2.2 AA, FC-CNC behavior, timing, package versions, and current
  Contracts.UI/remediation state agree across the canonical set;
- `_bmad-output/planning-artifacts/implementation-readiness-report-2026-07-15-post-correction.md`
  records `READY`, 100% FR coverage, zero critical/major findings, and three controlled minor
  structural/sequencing concerns.

Validation included YAML parsing, story structure/status/path checks, canonical identifier scans,
heading/authority checks, and the full BMad readiness workflow. The repository-wide story artifact
validator could not serve as a scoped creation check because its File List reconciliation includes
the shared pre-existing dirty worktree; blank Dev Record File Lists were intentionally not fabricated.
