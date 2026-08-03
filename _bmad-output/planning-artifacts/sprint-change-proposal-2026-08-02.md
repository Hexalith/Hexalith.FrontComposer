---
project: frontcomposer
date: 2026-08-02
workflow: bmad-correct-course
mode: Batch
trigger: "GOV-1: Replace historical SHA compatibility assertions with semantic catalog profiles for every Builds selector in the defined depth-1/2 v1 graph; add exact-revision graph diff, bounded affected-module proof, policy-authorized evaluator closure, both exact-candidate handoffs, and sealed graph/policy/workflow provenance."
status: approved
approved: 2026-08-02
approvedBy: Administrator
planningChangesApplied: 2026-08-02
scope: Moderate
recommendedApproach: Direct Adjustment
supersedes: null
refines: _bmad-output/planning-artifacts/sprint-change-proposal-2026-07-19.md
externalGateState: Hexalith.Builds issue 17 closed without a recorded qualifying GOV-1 revision
handoffStatus: completed
handoff:
  - Product Owner
  - Architect
  - Developer
  - Release Owner
---

# Sprint Change Proposal: Unblock and Complete GOV-1

Approval: approved by Administrator on 2026-08-02.

This proposal does not replace the approved 2026-07-19 GOV-1 architecture. It corrects the execution
state, ownership boundaries, and artifact drift discovered after that approval.

## 1. Issue Summary

GOV-1's approved target remains necessary and internally consistent: compatibility must be determined
from semantic catalog profiles for every Builds selector in the defined depth-1/2 v1 graph, while exact
commit identities belong in graph diffs and sealed release provenance. The current sprint plan cannot
reliably reach that target because it treats all of Tasks 4 and 5 as blocked by Hexalith.Builds issue
17 (`BUILD-REL-1`).

That dependency state is no longer truthful. Issue 17 was manually closed on 2026-07-20 without a
comment, linked closing pull request, accepted immutable workflow revision, or recorded acceptance of
the GOV-1 amendment added to FrontComposer planning on 2026-07-19. The issue body still describes the
original opt-in release and common-freeze request; it does not specify the exact-revision graph diff,
bounded affected-module proof, evaluator closure, exact-candidate handoffs, or sealed graph/policy/
workflow provenance now required by GOV-1.

Meanwhile, FrontComposer can implement and test most of those controls locally. Keeping both tasks
wholly blocked conceals available work, leaves mutable workflow references in critical paths, and
allows the planning record to drift from repository and upstream reality.

### Current Evidence

- The approved 2026-07-19 proposal and architecture already define the intended GOV-1 controls.
- GOV-1 is `in-progress`; Tasks 1-3 are complete, while Tasks 4-5 and their workflow/manifest fixtures
  remain unchecked and are collectively labelled blocked on issue 17.
- Review-baseline validation at `d9f0d526...` succeeded with 43 edges and seven evaluated Builds
  selectors. Application-time validation at exact HEAD `4302301ac88c23bfb7b97838dfd26cd6d9c9440f`
  produced the same 43-edge/seven-selector shape and graph digest
  `58fa3d657c4aef979e84f2cd6b2ddf1a868fa5225f94a28d1e7390c2a3a78472`. Counts and
  digests are observed evidence, not acceptance constants.
- The seven selectors currently resolve to two distinct Builds commits: six select
  `e69891f67578...`, and PolymorphicSerializations selects `10af541...`. Semantic profile evaluation
  therefore already works across non-identical exact revisions.
- `eng/dependency_graph.py` exposes only `graph` and `validate`; exact-revision `diff`, bounded affected
  module evaluation, and handoff production are not implemented.
- `eng/dependency-graph-policy.json` has empty `ci`, `release`, and `post_release`
  `evaluator_authorizations`; there is no active authorized evaluator closure.
- `.github/workflows/ci.yml` still consumes the shared CI workflow through `@main`. Other governance
  paths also retain mutable `@main` references.
- `.github/workflows/release-evidence.yml` derives its second-hop checkout from
  `workflow_run.head_sha` and permits a successful no-op when no tag is found. That contradicts the
  exact-candidate and fail-closed post-release requirements.
- `eng/release_evidence.py` is schema/tool version 1.2.0 and the valid fixture has no manifest-v2
  dependency graph, policy, authenticated handoff, or workflow provenance fields.
- FrontComposer's release workflow pins Hexalith.Builds `domain-release.yml` at
  `79f82acc...`, but the inspected reusable contract has no exact release-candidate or authenticated
  CI/Release handoff interface required by GOV-1.
- Hexalith.Builds issue 17 is closed. Its current `main` revision is `e69891f67578...`; neither the
  issue record nor the inspected reusable workflow supplies an accepted GOV-1 integration revision.
- Sprint status waived GOV-1 only for Story 11.17d promotion. Story 11.17d is now done, so the action's
  older due wording is stale; the governed-release gate remains in force.
- The focused architecture's copied `frontcomposer-catalog-v1` package values have drifted from the
  executable policy. The architecture currently duplicates volatile policy data without a guard.

### Problem Classification

This is an execution-state and dependency-boundary failure discovered during implementation, not a
failure of the approved product requirement or architecture. The correction must:

1. preserve GOV-1's accepted semantic-versus-provenance model;
2. resume all locally executable controls immediately;
3. constrain the external block to the actual reusable-workflow integration seam;
4. restore one authoritative policy source and truthful planning status; and
5. keep governed release publication frozen until the complete evidence chain is proven.

## 2. Impact Analysis

### Epic and Story Impact

No product epic, runtime story, API, or UX capability changes. GOV-1 remains a cross-cutting governance
story associated with Epic 11 and FR-24/NFR-12/NFR-13.

The story stays `in-progress`. Its acceptance criteria remain intact, but Tasks 4 and 5 must be
restructured into local implementation work and a narrowly defined upstream integration gate. Story
11.17d remains done under its recorded one-story waiver and is not reopened.

### Artifact Impact

| Artifact | Current conflict | Correction after approval |
| --- | --- | --- |
| `gov-1-validate-shared-catalog-compatibility-and-seal-dependency-provenance.md` | All of Tasks 4-5 are marked blocked, although most work is local. | Split local work from external integration; update issue evidence and acceptance-gate language. |
| `epics.md` | GOV-1 is still recorded as `ready-for-dev`. | Set GOV-1 to `in-progress` and record the refined execution boundary without changing its outcome. |
| `prd.md` | D-11 says issue 17 is pending, but it is closed without a qualifying revision. | Preserve the decision and correct the external-gate state and successor-request requirement. |
| `architecture.md` and GOV-1 spine | AD-16 records a pending issue and duplicates volatile catalog-profile values. | Record the closed/non-qualifying issue state; make the versioned executable policy the sole profile-value authority and architecture the structural contract. |
| `sprint-status.yaml` | The action remains open with pre-waiver Story 11.17d due wording. | Keep it open, preserve the historical waiver, and make the remaining due condition “before the next governed release.” |
| Deferred-work ledger | It separately reports the stale due field and policy-seed drift. | Link both findings to this approved correction and close them only when their target artifacts are reconciled. |
| Upstream request | Issue 17 is closed and cannot serve as an accepted delivery record. | Reopen it with the precise amendment or create a successor request; record its URL and accepted 40-hex revision. |
| UX artifacts | No journey, screen, accessibility, content, or interaction change. | No change. |

### Technical and Release Impact

- Compatibility policy remains semantic and applies to every Builds selector discovered in the
  defined graph; exact SHAs and catalog fingerprints remain evidence, not compatibility allowlists.
- CI gains deterministic base/candidate graph comparison and a bounded proof that every and only
  affected modules receive their policy-selected disposition.
- CI, Release, and post-release verification gain independently authorized, statically closed
  evaluator identities from the active policy.
- Release evidence moves to manifest v2 and seals the exact graph, active policy, authenticated
  handoffs, and immutable workflow/evaluator provenance.
- Both handoffs bind the same exact candidate. No consumer may reconstruct the candidate from a
  default branch, mutable ref, tag lookup, or second-hop `workflow_run.head_sha`.
- No submodule pointer, nested submodule, dependency, generated output, package inventory, runtime API,
  or product behavior changes merely by approving this proposal.
- Governed publication remains blocked until external workflow integration and end-to-end evidence
  verification pass at a recorded immutable revision.

### Scope and Estimate

Classification: **Moderate**. Product scope is stable, but the correction crosses Product Owner,
architecture, implementation, and release ownership.

- FrontComposer local completion: approximately 7-12 engineer-days.
- Hexalith.Builds reusable-workflow contract: approximately 3-5 engineer-days, owned upstream.
- Calendar risk remains high at the integration seam, but local progress no longer waits for it.

## 3. Recommended Approach

Use **Option 1 - Direct Adjustment**. Preserve the ratified GOV-1 architecture and change its execution
plan as follows.

### Track A: Resume FrontComposer-Local Work

Implement locally without waiting for an upstream revision:

1. Add exact-revision graph diff for the ratified PR and push revision models, including explicit
   zero-before/bootstrap handling and fail-closed missing-object diagnostics.
2. Compute the affected-module set from changed in-boundary graph edges and the active policy. Apply
   root-subsumes-descendant behavior, cycle-safe traversal, stable ordering, fixed bounds, and at-most-
   once module execution.
3. Materialize only the exact bounded source/catalog objects required by the selected static
   disposition. Do not recursively initialize nested submodules or move working-tree gitlinks.
4. Activate policy-authorized evaluator identities for CI, Release, and post-release stages. Compute a
   static transitive closure across local callers, reusable workflows, actions, and composite
   descendants under fixed source/depth/count/blob limits. Fail on mutable, missing, dynamic,
   unauthorized, or closure-mismatched coordinates.
5. Define and test the authenticated CI-to-Release handoff. It binds repository, event/run/attempt,
   immutable base and exact candidate, normalized graph and diff digests, affected-module proof,
   policy identity/digest, evaluator closure, conclusions, and raw handoff digest.
6. Define and test the authenticated Release-to-post-release handoff emitted under `if: always()`. It
   preserves the original CI identity/candidate and binds the Release run/attempt, conclusion,
   version/tag/release/manifest/assets when present, authorized evaluator closure, and raw handoff
   digest. Failed or partial attempts remain verifiable evidence, not green no-ops.
7. Implement manifest v2 producer/verifier/fixtures that seal the complete normalized depth-1/2 graph,
   catalog profile/fingerprint evidence, active policy, both authenticated handoffs as applicable, and
   immutable workflow/evaluator provenance. Legacy manifests stay audit-only and non-publishable.
8. Add workflow-governance and hostile fixtures for revision mismatch, graph drift, false omission or
   over-selection, closure mutation, mutable coordinates, handoff substitution/replay, second-hop SHA
   substitution, missing tag/assets, and failed/partial Release attempts.

### Track B: Replace the Stale External Gate

The Release Owner must either reopen Hexalith.Builds issue 17 with the complete GOV-1 amendment or file
a successor request. The request is not satisfied by issue state alone. Acceptance requires:

- an upstream change that exposes the exact-candidate and authenticated evidence contract required at
  the reusable CI/Release boundary;
- immutable full-SHA coordinates for every consumed upstream workflow/action and its statically closed
  transitive evaluator graph;
- successful upstream contract tests, including failure paths and `if: always()` evidence behavior;
- a recorded accepted 40-hex Hexalith.Builds revision and policy authorization for its exact closure;
- a FrontComposer integration proof showing that both handoffs, manifest v2, and post-release
  verification agree on the same candidate and policy; and
- no successful governed release path that depends on `@main`, second-hop inferred SHA, tag search, or
  a missing-evidence no-op.

Only Track B's reusable-workflow wiring, end-to-end proof, GOV-1 completion, release eligibility, and
freeze removal remain externally blocked.

### Track C: Eliminate Policy-Source Drift

Treat `eng/dependency-graph-policy.json` as the sole executable authority for profile IDs, required
catalog properties/versions, module dispositions, limits, and evaluator authorizations. Architecture
documents define the invariant, schema, ownership, and trust boundaries; they do not duplicate
volatile profile values.

Seal the policy's immutable repository path, schema version, canonical SHA-256 digest, and policy
revision in both handoffs and manifest v2. Add a Governance check that the architecture's named policy
coordinate exists and that no second executable profile seed is maintained in planning prose.

### Alternatives Considered

- **Keep all Tasks 4-5 blocked on closed issue 17:** rejected. It misstates current external state and
  delays independent local controls.
- **Declare issue 17 sufficient because it is closed:** rejected. Closure supplies neither the amended
  contract nor an accepted qualifying revision.
- **Remove the external dependency entirely:** rejected. The reusable workflow boundary still needs an
  immutable producer/consumer contract and end-to-end proof.
- **Duplicate exact profile values in architecture and executable policy:** rejected. It has already
  drifted and creates competing authorities.
- **Reduce GOV-1 or permit legacy release evidence:** rejected. That would weaken FR-24/NFR-12/NFR-13
  at the publication boundary.
- **Rollback or change submodule pointers:** not required by this correction and unsupported as a
  compatibility remedy.

## 4. Detailed Change Proposals

The following edits were explicitly approved and applied on 2026-08-02.

### 4.1 GOV-1 Story: Execution Boundary

**OLD**

```markdown
Tasks 4 and 5 are blocked pending Hexalith.Builds issue 17 / BUILD-REL-1 accepted immutable revision.
```

**NEW**

```markdown
Tasks 4 and 5 are split into FrontComposer-local and upstream-integration work.

- Local graph diff, affected-module proof, evaluator authorization/closure, handoff schemas and
  consumers, manifest-v2 producer/verifier, policy governance, and hostile fixtures proceed now.
- Only reusable-workflow integration, end-to-end exact-candidate evidence proof, story completion,
  governed release eligibility, and freeze removal await a qualifying Hexalith.Builds revision.
- Hexalith.Builds issue 17 closed on 2026-07-20 without the GOV-1 amendment or a recorded qualifying
  revision. Reopen it with the amendment or replace it with a successor request, and record the
  accepted URL plus 40-hex revision before clearing the external gate.
```

Add explicit task checkboxes for Track A items 1-8 and leave only the Track B integration checkboxes
blocked. Preserve all completed Tasks 1-3 and their evidence.

### 4.2 GOV-1 Acceptance Clarification

Append these testable clarifications without weakening the ratified criteria:

```markdown
**Given** an exact immutable base and candidate under the ratified event model,
**When** their defined depth-1/2 graphs are compared,
**Then** the normalized diff and policy projection prove a deterministic, bounded, cycle-safe,
root-subsumed, at-most-once set of affected modules, including explicit bootstrap handling.

**Given** a CI, Release, or post-release evaluator,
**When** its authority is checked,
**Then** the active policy independently authorizes its local blobs and literal-40-hex external
coordinates, and the bounded static transitive closure matches before execution evidence is trusted.

**Given** a Release attempt for a CI-approved candidate,
**When** Release and post-release verification run,
**Then** the authenticated CI-to-Release and Release-to-verifier handoffs bind that same exact
candidate, policy, and evaluator closure; failed or partial attempts emit verifiable evidence and
cannot pass through a missing-evidence no-op.

**Given** a publishable manifest v2,
**When** it is prepared, consumed, or independently verified,
**Then** it seals and verifies the normalized dependency graph, semantic catalog evidence, active
policy coordinate/digest, authenticated handoff chain, and immutable workflow/evaluator provenance.
```

### 4.3 Epics and Sprint Status

**OLD**

```yaml
GOV-1: ready-for-dev
due: before Story 11.17d promotion and the next governed release
external_gate: Hexalith.Builds issue 17 accepted immutable revision pending
```

**NEW**

```yaml
GOV-1: in-progress
due: before the next governed release
waiver_history:
  - Story 11.17d promotion only; completed 2026-08-02
external_gate: reopened Hexalith.Builds issue 17 or successor request; qualifying immutable revision pending
local_work: unblocked
```

The exact sprint-status schema and existing narrative conventions must be preserved when applying the
semantic change; the snippet is not permission to invent unsupported YAML keys.

### 4.4 PRD Decision Record

**OLD**

```markdown
BUILD-REL-1 / Hexalith.Builds issue 17 accepted immutable revision is pending and blocks GOV-1
Tasks 4/5, completion, release, and unfreeze.
```

**NEW**

```markdown
Hexalith.Builds issue 17 closed on 2026-07-20 without a recorded qualifying GOV-1 revision.
FrontComposer-local GOV-1 work is not blocked. A reopened issue 17 or successor request and its
accepted immutable revision gate only reusable-workflow integration, end-to-end evidence proof,
GOV-1 completion, governed release eligibility, and unfreeze.
```

FR-24, NFR-12, NFR-13, and the semantic-compatibility decision remain unchanged.

### 4.5 Architecture and Policy Authority

**OLD**

```markdown
AD-16 treats issue 17 as pending and the focused architecture carries a copied closed-policy seed
with exact package values.
```

**NEW**

```markdown
AD-16 records issue 17 as closed without a qualifying GOV-1 integration revision and restricts the
replacement external gate to the reusable-workflow seam and end-to-end proof.

`eng/dependency-graph-policy.json` is the sole executable source for semantic profile values, module
dispositions, graph/materialization bounds, and evaluator authorizations. Architecture owns the
schema, invariants, authority boundaries, and fail-closed rules and references the executable policy
without duplicating volatile values. Handoffs and manifest v2 seal the policy path, schema version,
canonical digest, and revision.
```

The architecture must also state that an exact candidate originates in the authenticated CI handoff
and is preserved through Release and post-release verification. Neither a tag nor any later event SHA
is candidate authority.

### 4.6 Upstream Handoff Request

Replace the stale “issue 17 pending” handoff with an actionable request owned by the Release Owner:

```markdown
Reopen Hexalith.Builds issue 17 with the GOV-1 reusable-workflow amendment or create a successor.
The upstream owner returns: request URL, accepted commit SHA, immutable workflow/action coordinates,
contract-test evidence, exact-candidate interface, always-emitted Release evidence behavior, and the
static evaluator closure required for FrontComposer policy authorization.
```

No external issue is reopened or created by this proposal itself.

### 4.7 UX Artifacts

No change. GOV-1 affects repository governance and release evidence only.

## 5. Handoff and Success Criteria

### Change Scope

**Moderate**: backlog and execution-plan correction with Product Owner, Architect, Developer, and
Release Owner coordination. No fundamental product replan is required.

### Recipients and Responsibilities

| Recipient | Responsibility |
| --- | --- |
| Product Owner | Approve the refined scope/gate; reconcile epics, sprint status, waiver history, and deferred-work entries. |
| Architect | Update AD-16 and the GOV-1 spine; remove duplicated volatile policy values; preserve the ratified invariants. |
| Developer | Implement Track A, focused tests, hostile fixtures, and local documentation without changing gitlinks. |
| Release Owner | Reopen issue 17 or file the successor, obtain the qualifying revision, authorize its exact closure, and run the end-to-end proof. |

### Completion Criteria

GOV-1 may be marked done and the governed-release freeze removed only when all of the following are
true:

1. Every Builds selector in the exact defined depth-1/2 v1 graph passes its active semantic profile;
   no historical SHA or fingerprint compatibility allowlist exists.
2. Exact base/candidate graph diff and bounded affected-module proof pass positive, negative,
   bootstrap, cycle, root-subsumption, and limit tests.
3. CI, Release, and post-release evaluator closures are non-empty where required, independently
   policy-authorized, immutable, statically complete, bounded, and digest-verified.
4. The authenticated CI-to-Release handoff and always-emitted Release-to-verifier handoff bind the
   same exact candidate and active policy through success, failure, and partial-attempt fixtures.
5. Manifest v2 seals and independently verifies graph, semantic catalog evidence, policy, handoffs,
   and immutable workflow/evaluator provenance; legacy manifests cannot publish.
6. The post-release verifier uses the authenticated handoff candidate and cannot green-no-op missing,
   failed, mismatched, or partial evidence.
7. The upstream request records an accepted 40-hex Hexalith.Builds revision, FrontComposer consumes it
   immutably, and the end-to-end governed-release evidence test passes.
8. PRD, epics, architecture/spine, story, sprint status, and deferred-work ledger agree on the final
   state and policy authority.

## 6. Correct-Course Checklist Results

| Checklist area | Result | Notes |
| --- | --- | --- |
| 1.1 Triggering story identified | Done | GOV-1 implementation exposed a stale external gate and execution deadlock. |
| 1.2 Core problem defined | Done | Approved design is sound; gate truth, sequencing, and policy authority drifted. |
| 1.3 Evidence gathered | Done | Local code/config/tests, planning artifacts, workflow definitions, Git state, and issue 17 were inspected. |
| 2.1 Current epic impact | Done | Epic 11 outcome remains valid; GOV-1 stays cross-cutting and in progress. |
| 2.2 Future epic impact | Done | No product epic is invalidated or added. |
| 2.3 Epic validity/order | Done | No epic resequencing; governance work must finish before governed release. |
| 2.4 New/removed epics | N/A | No product-scope change. |
| 3.1 PRD conflict | Done | Only the stale external-gate decision text needs correction. |
| 3.2 Architecture conflict | Done | AD-16 gate state and duplicated policy values require reconciliation. |
| 3.3 UX conflict | N/A | No user-facing impact. |
| 3.4 Other artifacts | Done | Story, epics, sprint status, workflows, manifest fixtures, and ledger are affected. |
| 4.1 Direct adjustment | Recommended | Preserves approved GOV-1 while unblocking local execution. |
| 4.2 Rollback | Rejected | Does not solve the missing evidence chain or stale gate. |
| 4.3 PRD/MVP reduction | Rejected | Would weaken required release governance. |
| 4.4 Recommended path selected | Done | Direct Adjustment, Moderate scope. |
| 5.1 Issue summary | Done | Includes problem, context, evidence, and classification. |
| 5.2 Impact and artifact edits | Done | Includes explicit old/new corrections and UX N/A. |
| 5.3 Recommended approach | Done | Includes rationale, estimates, risks, and alternatives. |
| 5.4 Handoff plan | Done | Names recipients, ownership, external deliverable, and completion proof. |
| 6.1 Checklist completeness | Done | All applicable sections evaluated in Batch mode. |
| 6.2 Proposal accuracy | Done | Administrator continued the complete proposal and approved it. |
| 6.3 Explicit approval | Done | Administrator explicitly approved on 2026-08-02. |
| 6.4 Apply approved changes | Done | PRD, epics, architecture/spine, story, sprint status, upstream request, and deferred ledger reconciled. |
| 6.5 Handoff confirmation | Done | Product Owner, Architect, Developer, and Release Owner responsibilities are recorded below. |

## 7. Approval and Application Record

Administrator continued the complete Batch-mode proposal and explicitly approved it on 2026-08-02.
As a Moderate correction, the implementation handoff is routed to the Product Owner and Developer;
the Architect owns the policy/architecture boundary and the Release Owner owns the upstream request,
immutable revision acceptance, authorization, and end-to-end release proof.
The approved planning changes were applied to:

- `_bmad-output/planning-artifacts/prd.md`;
- `_bmad-output/planning-artifacts/epics.md`;
- `_bmad-output/planning-artifacts/architecture.md`;
- `_bmad-output/planning-artifacts/architecture/architecture-gov-1-2026-07-19/ARCHITECTURE-SPINE.md`;
- `_bmad-output/planning-artifacts/g2-hexalith-builds-inline-pre-publish-gate-request.md`;
- `_bmad-output/implementation-artifacts/gov-1-validate-shared-catalog-compatibility-and-seal-dependency-provenance.md`;
- `_bmad-output/implementation-artifacts/sprint-status.yaml`; and
- `_bmad-output/implementation-artifacts/deferred-work.md`.

Validation on 2026-08-02:

- sprint-status YAML parsed successfully;
- `python3 eng/validate-story-artifacts.py` passed;
- `git diff --check` returned exit code 0;
- `python3 eng/dependency_graph.py validate --commit 4302301ac88c23bfb7b97838dfd26cd6d9c9440f`
  returned `ok: true`, 43 edges, seven semantically validated Builds selectors, and graph digest
  `58fa3d657c4aef979e84f2cd6b2ddf1a868fa5225f94a28d1e7390c2a3a78472`.

No implementation code, workflow, dependency, gitlink, commit, release, or external GitHub issue was
changed by this correct-course application.
