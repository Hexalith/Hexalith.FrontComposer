---
project: frontcomposer
date: 2026-07-05
workflow: bmad-correct-course
mode: Batch
trigger: _bmad-output/planning-artifacts/implementation-readiness-report-2026-07-05-post-correct-course.md
status: approved
approval: approved-by-administrator-2026-07-05
scope: Moderate
---

# Sprint Change Proposal - Post-Correction Readiness Follow-Up

## Section 1 - Issue Summary

The post-correction implementation readiness assessment still reports **NEEDS WORK** after the
previous missing-artifact blocker was resolved.

The trigger is `_bmad-output/planning-artifacts/implementation-readiness-report-2026-07-05-post-correct-course.md`.
That report confirms that canonical PRD, architecture, UX, and Epic 11 restructuring artifacts now
exist, but it identifies eight remaining readiness issues:

1. Story 11.0 is unresolved and blocks Epic 11 implementation kickoff.
2. FR24 lacks explicit release-evidence ownership in the epic coverage map.
3. Story 11.8 remains an unresolved package-boundary gate before Stories 11.11-11.14.
4. The explicit FR Coverage Map in `epics.md` stops at FR22 while the PRD now defines FR1-FR26.
5. The PRD is still marked `status: draft`.
6. PRD open questions and assumptions remain active.
7. Epic 11 detailed story order may mislead naive next-story selection.
8. The UX planning artifact is traceable but compact, so visual stories still need richer local design context.

This proposal is not caused by a failed implementation story. It is a planning-readiness follow-up
after the first July 5 correction.

## Section 2 - Impact Analysis

### Checklist Status

| Item | Status | Finding |
| --- | --- | --- |
| 1.1 Triggering story | [N/A] | Trigger is the post-correction readiness report, not story implementation. |
| 1.2 Core problem | [x] | Remaining issues are decision gates and traceability gaps, not missing core artifacts. |
| 1.3 Evidence | [x] | Evidence is the readiness report plus current `prd.md`, `epics.md`, and `sprint-status.yaml`. |
| 2.1 Current epic impact | [!] | Epic 11 must not start until Story 11.0 is completed. |
| 2.2 Epic-level changes | [!] | `epics.md` needs FR23-FR26 coverage and FR24 ownership. |
| 2.3 Future epic impact | [x] | Epics 9 and 10 are not invalidated. Epic 11 remains the affected backlog area. |
| 2.4 New/remove epics | [x] | No new epic is required. FR24 can be owned by a release-governance gate. |
| 2.5 Priority/order | [!] | Sprint status should keep the suggested Epic 11 order stronger than file order. |
| 3.1 PRD conflicts | [!] | PRD is discoverable but still draft and contains unresolved decisions. |
| 3.2 Architecture conflicts | [x] | Architecture artifact is discoverable; Story 11.8 correctly gates package-boundary changes. |
| 3.3 UX conflicts | [!] | UX artifact is discoverable but compact; story-local notes are still needed for visual work. |
| 3.4 Other artifacts | [!] | `sprint-status.yaml` should close stale artifact-discovery action E11-AI-3 and add new release/PRD actions. |
| 4.1 Direct adjustment | Viable | Recommended. Update planning traceability and action ownership without reopening shipped epics. |
| 4.2 Rollback | Not viable | No implementation work needs rollback. |
| 4.3 MVP review | Not viable | No MVP scope reduction is needed. |
| 4.4 Recommended path | [x] | Direct Adjustment plus explicit decision-gate routing. |
| 5.1-5.5 Proposal components | [x] | Captured below. |
| 6.1-6.2 Final review | [x] | Proposal is actionable and scoped. |
| 6.3 Approval | [!] | Pending Administrator approval. |
| 6.4 Sprint status update | [!] | Apply only after approval. |
| 6.5 Handoff | [x] | Moderate scope: PO/Developer, Release Owner, Architect + Product, Architect + PM. |

### Epic Impact

Epic 11 remains blocked by design until Story 11.0 records the command route contract. This is not
an epic decomposition defect anymore, but it is a real implementation gate and should stay visible.

Story 11.8 remains a separate decision gate for package-boundary work. That is also intentional: the
Contracts split affects public API baselines, package compatibility, deprecation posture, and v1.0
release risk. Stories 11.11-11.14 must remain blocked until Story 11.8 is done.

No new epic is required for FR24 if the release evidence work is explicitly represented as a
release-governance gate with a named owner, due date, and checklist. If later review discovers that
release workflow code changes are still needed, that gate can produce a story.

### Artifact Impact

PRD:

- Remains discoverable but `status: draft`.
- Open questions 1, 2, 3, and 8 are partly resolved by the prior correction.
- Open questions 4 and 6 are active story gates: Story 11.0 and Story 11.8.
- Open questions 5 and 7 need Product/Architecture decision or accepted-risk wording.

Epics:

- `epics.md` FR Coverage Map needs FR23-FR26 entries.
- FR24 needs named release-evidence ownership.
- Epic 11 suggested order is already present, but sprint status and story creation guidance should
  keep it authoritative over detailed file order.

Sprint status:

- E11-AI-3 can be closed or replaced because the canonical artifacts were created and readiness was
  rerun.
- New explicit action items should track PRD approval/open-question disposition and FR24 release
  evidence ownership.

UX:

- `ux-design.md` is enough for readiness discovery, but visual implementation stories must load
  richer sources from `epics.md`, architecture section 4, component inventory, and story-local
  design notes when pixel/layout choices are material.

## Section 3 - Recommended Approach

Use **Direct Adjustment**.

Rationale:

- The missing artifact blocker is resolved; another broad replan would add churn.
- Story 11.0 and Story 11.8 are legitimate gates and should not be bypassed by a developer agent.
- FR24 can be made traceable with a release-governance owner and checklist, without inventing a
  feature epic.
- The stale FR map and sprint-status actions are mechanical planning fixes.
- The PRD should not be promoted to final without Product sign-off; it should instead get an
  explicit decision-disposition pass.

Effort estimate: Low to Medium.

Risk level: Low for traceability edits; Medium for the gated decisions because they affect URL and
package/public API contracts.

Timeline impact: One planning edit pass plus two decision gates before Epic 11 implementation. The
release-evidence gate should be due before the v1.0 release candidate, not before Story 11.1.

## Section 4 - Detailed Change Proposals

### Proposal A - Update `epics.md` FR Coverage Map For FR23-FR26

Artifact: `_bmad-output/planning-artifacts/epics.md`

Section: `### FR Coverage Map`

OLD:

```text
- FR20 (`frontcomposer inspect`): **Epic 7** + **Epic 10** (text-output parity guard)
- FR21 (`frontcomposer migrate`): **Epic 7** + **Epic 10** (HFCM9002 production-emission decision)
- FR22 (Testing library): **Epic 7** + **Epic 10** (default-lane redaction guard) + **Epic 11** (11.6 harness failure modes)
```

NEW:

```text
- FR20 (`frontcomposer inspect`): **Epic 7** + **Epic 10** (text-output parity guard)
- FR21 (`frontcomposer migrate`): **Epic 7** + **Epic 10** (HFCM9002 production-emission decision)
- FR22 (Testing library): **Epic 7** + **Epic 10** (default-lane redaction guard) + **Epic 11** (11.6 harness failure modes)
- FR23 (component and skill documentation): **Epic 1** (1.5 component docs), **Epic 5** (skill resources), **Epic 7** (diagnostics/tooling docs), **Epic 10** (10.2/10.4 docs cleanup), and **Epic 11** (11.14 package-compat docs)
- FR24 (signed package artifacts with evidence): **Release Governance Gate RG-1**, owner **Release Owner**, tracked in `sprint-status.yaml` action `REL-AI-1`
- FR25 (public contracts and deprecation paths): **Epic 7**, **Epic 10**, and **Epic 11** (11.8, 11.11-11.14, 11.19)
- FR26 (post-MVP hardening backlog): **Epic 9**, **Epic 10**, and **Epic 11**
```

Rationale:

The readiness report is correct: the PRD now has FR1-FR26, while the explicit map stops at FR22.
Adding FR23-FR26 closes the stale traceability issue without changing product scope.

### Proposal B - Add Explicit FR24 Release-Evidence Ownership

Artifacts:

- `_bmad-output/planning-artifacts/epics.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`

Current state:

```text
OLD:
FR24 exists in the PRD and is reflected in release NFRs, but no named epic/story/gate owns:
- signed `.nupkg`
- signed `.snupkg`
- SBOM
- checksums
- package inventory
- release manifest/evidence chain
- GitHub Release assets
```

Proposed state:

```text
NEW:
Add Release Governance Gate RG-1:

Owner: Release Owner
Due: before v1.0 release candidate publication
Scope:
- expected NuGet package inventory validated
- `.nupkg` and `.snupkg` signing evidence captured
- SBOM generated and linked
- checksums captured
- release manifest/evidence chain produced
- GitHub Release assets attached or dry-run evidence recorded
- package-consumer validation result linked
Exit:
- release owner marks `REL-AI-1` done with evidence paths
- if workflow/code changes are required, create a focused implementation story before RC
```

Sprint status action:

```yaml
- epic: release
  action: "REL-AI-1: Own FR24 release evidence gate for signed packages, symbols, SBOM, checksums, package inventory, release manifest/evidence chain, GitHub Release assets, and package-consumer validation before v1.0 RC."
  owner: "Release Owner"
  assigned: "2026-07-05"
  due: "before v1.0 release candidate"
  status: open
```

Rationale:

FR24 is a release-readiness requirement, not a runtime feature. Treating it as a named release gate is
clearer than forcing it into Epic 10 after Epic 10 is done or overloading Epic 11.19.

### Proposal C - Replace Stale Artifact-Discovery Action With PRD Approval Action

Artifact: `_bmad-output/implementation-artifacts/sprint-status.yaml`

Current state:

```yaml
- epic: 11
  action: "E11-AI-3: Complete/promote the draft PRD, create canonical architecture and UX planning artifacts, and rerun readiness before Epic 11 implementation starts."
  owner: "Product Owner + Developer"
  assigned: "2026-07-05"
  due: "before Epic 11 dev kickoff"
  status: open
```

Proposed state:

```yaml
- epic: 11
  action: "E11-AI-3: Complete/promote the draft PRD, create canonical architecture and UX planning artifacts, and rerun readiness before Epic 11 implementation starts."
  owner: "Product Owner + Developer"
  assigned: "2026-07-05"
  due: "before Epic 11 dev kickoff"
  status: done

- epic: prd
  action: "PRD-AI-1: Resolve, route, or explicitly accept PRD open questions and assumptions; update PRD status only after Product approval."
  owner: "Product Owner"
  assigned: "2026-07-05"
  due: "before final v1.0 readiness approval"
  status: open
```

Rationale:

E11-AI-3 is now stale: the canonical PRD, architecture, and UX artifacts exist and the readiness check
was rerun. The remaining PRD issue is approval/open-decision governance, so it should be tracked as
that instead of pretending artifact discovery is still the blocker.

### Proposal D - Add PRD Open-Question Disposition Section

Artifact: `_bmad-output/planning-artifacts/prd.md`

Section: after `## 12. Open Questions`

OLD:

```text
## 12. Open Questions

1. Should this PRD become the canonical planning artifact referenced by future readiness checks, or should `epics.md` remain the primary planning artifact with this PRD as a synthesis?
2. Should `_bmad-output/project-docs` be included in future readiness discovery configuration so architecture and UX source documents are not falsely reported as missing?
3. Should Epic 11.7 be extracted to Story 11.0 or a pre-epic decision record before any 11.x implementation story starts?
4. What is the approved route family for generated command pages versus palette/CTA command links?
5. What is the approved FC-NIP row identity payload source: EventStore status, command outcome metadata, projection materialization event, or another payload?
6. What exact v1.0 release gate decides whether Contracts kernel split ships before v1.0 or is deferred with explicit breaking-change posture?
7. Which success metric targets should be quantitative before finalization?
8. Should a standalone UX spec be produced, or are the embedded UX-DRs plus this PRD sufficient for downstream work?
```

NEW:

```text
## 12. Open Questions

### Resolved Or Routed By 2026-07-05 Planning Corrections

1. PRD canonicality: proposed disposition - `prd.md` is the canonical requirements source; `epics.md` is the backlog decomposition and traceability map.
2. Readiness discovery: proposed disposition - planning mirrors `architecture.md` and `ux-design.md` remain under `_bmad-output/planning-artifacts`; `_bmad-output/project-docs` remains detailed source material.
3. Epic 11 route gate: resolved structurally - Story 11.0 is the pre-implementation decision gate.
8. UX source: proposed disposition - `ux-design.md` is sufficient as the standalone readiness source; visual implementation stories must cite richer design context when needed.

### Active Decision Gates

4. Generated command route family: owned by Story 11.0, Architect + Product, due before Epic 11 dev kickoff.
5. FC-NIP row identity source: owned by Epic 9 follow-through; Story 9.1 confirmed the current upstream gap and Story 9.2 remains blocked on a precise producer payload.
6. Contracts kernel split release posture: owned by Story 11.8, Architect + PM, due before Story 11.11 starts.
7. Quantitative success metric targets: Product Owner decision before final v1.0 readiness approval.
```

Do not change `status: draft` to a final state unless Product explicitly approves it. If Product approves
the above dispositions, change `status: draft` to `status: approved-for-v1-readiness` or another team-approved
status value.

Rationale:

This keeps the PRD honest: it remains draft until Product approves, but its open questions are no
longer unstructured.

### Proposal E - Strengthen Epic 11 Story-Order Guidance

Artifacts:

- `_bmad-output/planning-artifacts/epics.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`

Current state:

```text
OLD:
Epic 11 already states a suggested order, but detailed story sections list 11.11-11.14 before 11.9.
Sprint status lists every story as backlog.
```

Proposed state:

```text
NEW:
Add this note beside the Epic 11 suggested order and sprint-status Epic 11 block:

Story creation order is authoritative. Do not infer the next story from file order or numeric sort.
Story 11.0 blocks all Story 11.1+ create-story work. Story 11.8 blocks only Stories 11.11-11.14.
After Story 11.7, create lower-risk remediation in the stated order before package-boundary stories.
```

Rationale:

The report's minor issue is preventable with stronger workflow text. Reordering the whole file is
optional and higher churn.

### Proposal F - Require Story-Local Design Notes For Visual Epic 11 Work

Artifacts:

- `_bmad-output/planning-artifacts/ux-design.md`
- future Story 11.x implementation artifacts

Proposed addition:

```text
For visual or layout-sensitive stories, the story file must cite the richer design source used:
`epics.md` UX-DRs, architecture section 4, component inventory, approved sprint-change proposal, or a
story-local design note. The concise `ux-design.md` artifact is sufficient for readiness discovery,
but not automatically sufficient for pixel/layout decisions.
```

Rationale:

This accepts the compact UX artifact while preventing visual stories from implementing from an
underspecified source.

## Section 5 - Implementation Handoff

Scope classification: **Moderate**.

Route to:

- Product Owner / Developer: apply the mechanical planning edits after approval.
- Release Owner: own Release Governance Gate RG-1 / `REL-AI-1`.
- Product Owner: resolve PRD open-question dispositions and approve or keep draft status.
- Architect + Product: complete Story 11.0 before Epic 11 implementation starts.
- Architect + PM: complete Story 11.8 before Stories 11.11-11.14.

Recommended sequence:

1. Approve this proposal.
2. Update `epics.md` FR Coverage Map for FR23-FR26.
3. Add FR24 Release Governance Gate RG-1 and `REL-AI-1`.
4. Mark E11-AI-3 done and add `PRD-AI-1`.
5. Add PRD open-question disposition text without promoting PRD status unless Product approves.
6. Strengthen Epic 11 story-order guidance.
7. Complete Story 11.0 route decision.
8. Re-run implementation readiness.

Success criteria:

- FR23-FR26 appear in the explicit FR Coverage Map.
- FR24 has a named owner and release-evidence checklist.
- Artifact-discovery action E11-AI-3 is no longer reported as open after it is complete.
- PRD open questions are resolved, routed to story gates, or accepted as release risks.
- Epic 11 story creation remains blocked by Story 11.0 until the route contract is recorded.
- Story 11.8 continues to gate package-boundary implementation stories only.

## Section 6 - Approval State

This proposal was approved by Administrator on 2026-07-05.

Approved planning changes were applied to `epics.md`, `sprint-status.yaml`, `prd.md`, and
`ux-design.md`. No product source code changes are part of this proposal.

Handoff completion:

- Scope: Moderate.
- Routed to: Product Owner / Developer for planning artifact maintenance; Release Owner for FR24 evidence; Architect + Product for Story 11.0; Architect + PM for Story 11.8.
- Next gate: complete Story 11.0 before any Epic 11 implementation story is created.
