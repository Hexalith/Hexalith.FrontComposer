---
project: frontcomposer
date: 2026-07-16
workflow: bmad-correct-course
mode: Incremental
trigger: "REL-AI-1: Own the FR24 exact-artifact pre-publication gate for signed/timestamped packages, symbols, SBOM, checksums, package inventory, consumer validation, sealed manifest/readiness, durable GitHub Release evidence, published-byte verification, and historical reconciliation."
status: approved-and-applied
approved: 2026-07-16
scope: Moderate
implementationRisk: High
accountableOwner: Release Owner
executionChain:
  - REL-4
  - REL-3
  - REL-5
---

# Sprint Change Proposal: REL-AI-1 Exact-Artifact Ownership and Execution Reconciliation

## 1. Issue Summary

REL-AI-1 already exists as the Release Owner-accountable umbrella for PRD FR-24. The canonical
requirement, target architecture, compliance ledger, and the REL-4 / REL-3 / REL-5 delivery split
describe the required exact-artifact release gate. The remaining issue is execution and truth-state
drift: several planning and deployment artifacts say the temporary REL-4 freeze is technically
enforced even though REL-4 remains `ready-for-dev` and the executable release workflow contains no
freeze guard.

The current executable path remains publish-capable:

- `.github/workflows/release.yml` invokes the shared `domain-release.yml` after successful push CI
  with no `freeze-guard` or `HEXALITH_RELEASE_PUBLISH_ENABLED` condition;
- `.releaserc.json` packs candidates and pushes `.nupkg` and `.snupkg` files directly, without a
  pre-publication sealed-manifest/readiness check;
- `.github/workflows/release-evidence.yml` reconstructs and evaluates evidence after Release;
- `eng/release_evidence.py` still describes publication approval through prohibited
  `workflow_dispatch` inputs even though the approved REL-3 / REL-5 decision keeps
  `workflow_dispatch` out of `release.yml`;
- the deployment guide describes REL-4 as implemented when it is not.

The trigger is supported by the controlled REL-AI-1 ledger for v3.2.1 and v3.2.2: both releases
published the expected inventory, but the packages were unsigned, the manifest was invalid,
readiness was blocked, `publish_authorized=false`, durable GitHub Release evidence was absent, and
the evidence workflow still concluded successfully. Post-publication reconstruction cannot prove or
authorize the bytes already delivered to NuGet.

### Problem Classification

- Failed approach requiring a different solution: REL-2 G1 generated useful diagnostics but could
  not enforce FR-24 before publication.
- Execution/planning drift: the approved REL-4 / REL-3 / REL-5 correction is represented as partially
  operational in documentation while all three stories remain `ready-for-dev`.
- Contract drift: the evidence helper's machine-readable approval matrix conflicts with the approved
  caller-variable and Release Owner authorization model.

## 2. Impact Analysis

### Epic Impact

No product epic is invalidated and no new product or release epic is required. The existing release
governance stream remains viable:

```text
REL-AI-1 — Release Owner accountability
  -> REL-4 — immediate technical freeze
  -> REL-3 — exact-artifact technical gate and verification
  -> REL-5 — Release Owner enablement, first governed release, and closure
```

REL-2 remains done. Its CI alignment, package inventory, consumer validation, and historical evidence
are retained; it is not rolled back or reopened.

Product implementation may continue. No NuGet or GitHub package publication is authorized while
REL-AI-1 remains open, and REL-4 must execute before REL-3 development or any other change may enable
publication.

### Story Impact

- **REL-4:** remains the stop-the-line first implementation story. Its completion evidence is
  tightened so static workflow edits alone cannot mark it done.
- **REL-3:** remains the technical FR-24 implementation story. It gains an explicit acceptance
  criterion requiring the evidence helper's approval matrix to match the actual REL-4 / REL-5
  authorization mechanism.
- **REL-5:** remains the Release Owner-executed enablement and closure story. No scope change is
  required. Its immediate identity, timestamp-authority, secret-custody, and BUILD-REL-1 filing work
  may proceed in parallel with REL-4 / REL-3 development.

### Artifact Conflicts

| Artifact | Impact |
| --- | --- |
| `prd.md` | FR-24 and D-6 correctly define the gate but incorrectly describe REL-4 as operational. |
| `epics.md` | The FR-24 update incorrectly says the release freeze is technically enforced. |
| `architecture.md` | No change. It correctly defines the normative exact-artifact pipeline and ownership boundaries. |
| UX planning artifacts | No impact. The change has no UI, journey, interaction, accessibility, or visual-design consequence. |
| `sprint-status.yaml` | REL-AI-1 ownership is correct, but its execution-chain field omits REL-4 and its release-control field overstates implementation. |
| `deployment-guide.md` | The current-state section says REL-4 is implemented although `release.yml` has no guard. |
| REL-3 story | Needs an explicit machine-readable approval-contract reconciliation criterion. |
| REL-4 story | Needs truthful pre-implementation documentation wording and live completion evidence. |

### Technical Impact

This proposal does not implement the release workflow. After approval it routes the already-approved
implementation in this order:

1. REL-4 adds the default-frozen repository guard and governance tests.
2. REL-3 replaces the G1 publication lifecycle with pack-once validation, signing/timestamping,
   provenance attestation, checksums, sealed-manifest verification,
   `classify-release --require-publishable`, same-byte publication, durable initial GitHub Release
   evidence, downloaded-byte verification, partial-publication handling, and reconciliation.
3. REL-5 provisions operational authority and authorizes/proves the first governed release.

The upstream BUILD-REL-1 governed workflow contract remains blocking unless the Release Owner records
the already-defined bounded FrontComposer-owned contingency.

## 3. Recommended Approach

### Selected Path: Direct Adjustment

Preserve the current requirement, target architecture, completed REL-2 evidence, and REL-4 / REL-3 /
REL-5 decomposition. Reconcile false current-state claims, make sequencing explicit, and add the
missing approval-matrix acceptance criterion.

### Alternatives Considered

- **Rollback REL-2:** rejected. REL-2's reusable CI alignment, inventory, consumer validation, and
  diagnostic evidence remain valuable; rollback would not create a pre-publication gate.
- **Create another release story or epic:** rejected. It would duplicate REL-4, REL-3, or REL-5 and
  blur REL-AI-1 accountability.
- **Reduce or defer FR-24:** rejected. The PRD release-readiness objective remains achievable, and
  unsigned or unverified publication is not an acceptable MVP trade-off.

### Effort, Risk, and Timeline

- Planning and documentation reconciliation: low effort.
- REL-4: minor implementation scope, low-to-medium implementation risk, executed first.
- REL-3: moderate scope with high release-automation risk; requires negative-path and non-publishing
  orchestration evidence before a real release.
- REL-5: moderate operational scope with external lead time for identity, custody, timestamp authority,
  and Hexalith.Builds coordination.
- Overall timeline: the next package release remains blocked until the complete chain passes. Product
  development is not otherwise delayed.

## 4. Detailed Change Proposals

All proposals below were approved individually by Administrator on 2026-07-16 in Incremental mode.
They were applied after Administrator approved the complete Sprint Change Proposal.

### Proposal 1 — Sprint Status Ownership and Sequencing

Artifact: `_bmad-output/implementation-artifacts/sprint-status.yaml`

OLD:

```yaml
implementation_story: "REL-3 (development) + REL-5 (Release Owner enablement and closure)"
release_control: "frozen — ... technically enforced by REL-4's fail-closed ... gate"
```

NEW:

```yaml
implementation_story: "REL-4 (immediate technical freeze) -> REL-3 (exact-artifact implementation) -> REL-5 (Release Owner enablement, first governed release, and closure)"
release_control: "administratively frozen; technical enforcement remains pending until REL-4 is implemented and verified. No publication is authorized. REL-4 executes first, followed by REL-3; REL-5 enablement tasks may proceed in parallel and owns final authorization and closure."
```

REL-AI-1 remains `owner: Release Owner`, `status: open`, and subject to its existing exact-artifact
closure rule.

### Proposal 2 — PRD Release-Freeze Truth State

Artifact: `_bmad-output/planning-artifacts/prd.md`

Sections: FR-24 consequences and Decision D-6.

OLD:

```markdown
While the REL-3 gate is not yet operational, automated publication is technically frozen by a
fail-closed publish control (REL-4)...
```

NEW:

```markdown
Until REL-4 is implemented and its live behavior is verified, publication is administratively
prohibited but is not yet technically blocked by the repository workflow. REL-4 is the stop-the-line
predecessor to REL-3. Once operational, its fail-closed control keeps publication disabled by default
and permits execution only through the Release Owner-controlled condition.
```

D-6 records that REL-4 is approved and `ready-for-dev`, but the executable gate has not landed.
FR-24's exact-artifact requirements and REL-AI-1 closure rule remain unchanged.

### Proposal 3 — Epics Release-Governance Status

Artifact: `_bmad-output/planning-artifacts/epics.md`

Section: FR-24 / RG-1 freeze-enforcement update.

OLD:

```markdown
The REL-3 freeze is now technically enforced. REL-4 adds a fail-closed publish gate to release.yml...
```

NEW:

```markdown
REL-4 is the approved stop-the-line predecessor to REL-3, but remains ready-for-dev. Until its
fail-closed publish gate is implemented and verified in release.yml, the freeze is administrative
and the current workflow remains publish-capable. REL-4 must land before REL-3 development or any
other change may authorize package publication.
```

No product epic or duplicate release story is added.

### Proposal 4 — Deployment Guide Reconciliation

Artifact: `_bmad-output/project-docs/deployment-guide.md`

Section: Release freeze control.

OLD:

```markdown
The freeze above is technically enforced by a fail-closed publish gate in release.yml
(implemented by REL-4...)
```

NEW:

```markdown
REL-4 defines the approved fail-closed release-freeze control, but implementation and live
verification remain pending. The current release.yml does not yet contain the freeze-guard and must
be treated as publish-capable. Publication is prohibited administratively until REL-4 lands.
```

The existing runbook remains as the approved target, labeled as effective only after REL-4
implementation. REL-4 completion updates it with governance-test evidence and the first live
frozen-run URL.

### Proposal 5 — REL-3 Machine-Readable Approval Contract

Artifact: `_bmad-output/implementation-artifacts/rel-3-enforce-fr24-pre-publish-and-reconcile-releases.md`

Add Acceptance Criterion 20:

```markdown
20. Given the machine-readable APPROVAL_MATRIX, when REL-3 implements release authorization, then it
describes the actual approved mechanisms: the Release Owner-controlled REL-4 variable, publishable
readiness evidence, and any upstream protected release environment. It must not name
workflow_dispatch, release_owner_approved, or release_approver inputs that release.yml forbids.
Governance tests pin consistency between the helper, release workflow, REL-4, and REL-5.
```

REL-3 tasks explicitly include updating `eng/release_evidence.py` and its governance tests.

### Proposal 6 — REL-4 Completion Evidence

Artifact: `_bmad-output/implementation-artifacts/rel-4-enforce-temporary-release-freeze.md`

Amend Acceptance Criterion 6.

OLD:

```markdown
The deployment guide documents the freeze runbook (done at approval; verify accuracy against the
landed workflow).
```

NEW:

```markdown
Before implementation, the deployment guide labels the freeze guard as an approved target that is
not yet operational. After REL-4 lands, the Developer updates it to active-state wording and records
governance-test results plus the first CI-authoritative frozen Release run URL showing freeze-guard
success, release-job skip, and no publication side effect.
```

REL-4 cannot be marked done from static workflow edits alone.

## 5. Change Analysis Checklist Record

### Section 1 — Understand the Trigger and Context

| Item | Status | Finding |
| --- | --- | --- |
| 1.1 Triggering story | [N/A] | Trigger is REL-AI-1 plus REL-2 real-release evidence, not a product story. |
| 1.2 Core problem | [x] | Failed G1 approach plus execution/planning and approval-contract drift. |
| 1.3 Supporting evidence | [x] | Ledger, sprint state, workflow/config, helper, tests, and deployment guide provide concrete evidence. |

### Section 2 — Epic Impact Assessment

| Item | Status | Finding |
| --- | --- | --- |
| 2.1 Current epic viability | [x] | Release-governance stream remains viable. |
| 2.2 Epic-level changes | [x] | No new epic; correct the release-governance status text. |
| 2.3 Remaining epic impact | [x] | Product work may continue; publication remains blocked. |
| 2.4 New/obsolete epics | [N/A] | None. |
| 2.5 Priority/order | [!] | Enforce REL-4 -> REL-3 -> REL-5, with REL-5 enablement tasks starting in parallel. |

### Section 3 — Artifact Conflict and Impact Analysis

| Item | Status | Finding |
| --- | --- | --- |
| 3.1 PRD conflicts | [!] | Requirement is correct; current REL-4 implementation claim is not. |
| 3.2 Architecture conflicts | [x] | Normative target and boundaries are correct; no redesign. |
| 3.3 UI/UX conflicts | [N/A] | No impact. |
| 3.4 Other artifacts | [!] | Sprint status, deployment guide, release workflow/config, helper approval matrix, and governance tests require coordinated handling. |

### Section 4 — Path Forward Evaluation

| Option | Status | Finding |
| --- | --- | --- |
| 4.1 Direct Adjustment | Viable / selected | Reconcile existing stories and artifacts; preserve scope. |
| 4.2 Potential Rollback | Not viable | REL-2 evidence remains useful and rollback does not provide authorization. |
| 4.3 PRD MVP Review | Not viable | Product scope remains achievable; only publication is blocked. |
| 4.4 Recommended path | [x] | Direct Adjustment with hard sequencing and truth-state reconciliation. |

### Sections 5–6 — Proposal and Handoff

| Item | Status | Finding |
| --- | --- | --- |
| 5.1–5.5 Proposal components | [x] | Issue, impact, approach, MVP effect, and handoff are documented. |
| 6.1 Checklist review | [x] | All applicable items addressed. |
| 6.2 Proposal accuracy | [x] | Recommendations are supported by executable and planning evidence. |
| 6.3 Explicit final approval | [x] | Administrator approved the complete proposal on 2026-07-16. |
| 6.4 Sprint status update | [x] | Proposal 1 was applied after final approval. |
| 6.5 Next steps/handoff | [x] | Handoff is active with REL-4 first, REL-5 AC1–4 in parallel, REL-3 next, and REL-5 closure last. |

## 6. Implementation Handoff

### Scope Classification

**Moderate.** No product replan is required, but backlog truth-state, story acceptance criteria,
release documentation, and a high-risk implementation sequence require Product Owner / Developer /
Release Owner coordination.

### Recipients and Responsibilities

| Recipient | Responsibility |
| --- | --- |
| Release Owner | Remains accountable for REL-AI-1; starts REL-5 AC1–4; files BUILD-REL-1; owns identity, secret custody, timestamp authority, publication authorization, ledger sign-off, and closure. |
| Developer | Applies approved artifact edits; implements REL-4 first; implements REL-3 only after the freeze is effective; preserves unrelated work. |
| QA / Test Architect | Verifies REL-4 negative cases and live frozen-run evidence; verifies REL-3 exact-artifact, same-byte, failure, partial-publication, and downloaded-byte paths. |
| Product Owner | Keeps REL-AI-1 open, prevents duplicate release stories, and keeps canonical planning synchronized with execution state. |
| Hexalith.Builds owner | Implements or accepts BUILD-REL-1 upstream; FrontComposer does not directly modify the shared submodule. |

### Success Criteria

- REL-AI-1 remains the single Release Owner-accountable umbrella.
- Canonical planning and deployment documentation describe the actual implementation state.
- REL-4 is implemented and proven by governance tests plus a live frozen Release run before REL-3 or
  any publication authorization.
- REL-3's helper, workflow, readiness, and approval contracts agree and contain no prohibited
  workflow-dispatch approval semantics.
- A non-publishing end-to-end preparation run proves the exact-artifact chain.
- The first governed release is authorized before publication, publishes the same manifest-bound
  signed/timestamped bytes, attaches durable evidence during initial GitHub Release creation, and
  passes downloaded NuGet/GitHub verification.
- v3.2.1 and v3.2.2 remain recorded as non-compliant affected releases.
- REL-AI-1 closes only on durable real-release evidence satisfying every FR-24 criterion.

## 7. Approval Record

- Proposal 1: approved by Administrator on 2026-07-16.
- Proposal 2: approved by Administrator on 2026-07-16.
- Proposal 3: approved by Administrator on 2026-07-16.
- Proposal 4: approved by Administrator on 2026-07-16.
- Proposal 5: approved by Administrator on 2026-07-16.
- Proposal 6: approved by Administrator on 2026-07-16.
- Complete proposal: approved by Administrator on 2026-07-16 and applied to the listed planning, status, story, and deployment artifacts.

## 8. Workflow Execution Log

- Approved edits applied to `sprint-status.yaml`, `prd.md`, `epics.md`, `deployment-guide.md`, and the REL-3 and REL-4 story files.
- No executable release workflow, semantic-release configuration, evidence-helper implementation, or shared submodule was changed by Correct Course.
- Active handoff: Release Owner retains REL-AI-1 accountability; Developer implements REL-4 before REL-3; REL-5 AC1–4 may proceed in parallel; QA/Test Architect validates the technical controls and real-release evidence; Product Owner maintains canonical truth-state; Hexalith.Builds owner receives BUILD-REL-1.
