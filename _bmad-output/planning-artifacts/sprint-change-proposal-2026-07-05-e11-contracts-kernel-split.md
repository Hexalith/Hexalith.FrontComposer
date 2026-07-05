---
project: frontcomposer
date: 2026-07-05
workflow: bmad-correct-course
mode: Batch
trigger: E11-AI-2 Contracts kernel split sign-off
status: applied
approval: user-directed-2026-07-05
scope: Moderate
---

# Sprint Change Proposal - E11 Contracts Kernel Split

## Section 1 - Issue Summary

Story 11.8 was the remaining Epic 11 package-boundary decision gate. The architecture review found
that `Hexalith.FrontComposer.Contracts` was acting as both a netstandard wire/attribute kernel and a
net10 Blazor/Fluent rendering assembly. That leaks the pinned Fluent UI RC and runtime-oriented types
to consumers that should only need the kernel, including analyzer and adopter module paths.

The requested correction signs off the split, amends the documented multi-TFM decision in
`project-context.md` and architecture docs, and keeps package-compat implementation work in the
pre-v1.0 window. It remains deliberately ordered last after the lower-risk Epic 11 remediation stories.

Evidence:

- `_bmad-output/project-docs/architecture-quality-review-2026-07-04.md` H11, M24, and M25.
- `_bmad-output/planning-artifacts/epics.md` Story 11.8 and Stories 11.11-11.14.
- `_bmad-output/project-context.md` previously documented `Contracts` as `net10.0;netstandard2.0`.
- `_bmad-output/project-docs/architecture.md` previously placed the rendering model in Layer 0.

## Section 2 - Impact Analysis

### Checklist Status

| Item | Status | Finding |
| --- | --- | --- |
| 1.1 Triggering story | [x] | Story 11.8: Contracts kernel split decision and compatibility plan. |
| 1.2 Core problem | [x] | Technical architecture limitation: the kernel leaks net10/Blazor/Fluent and runtime/testing concerns. |
| 1.3 Evidence | [x] | H11 cites `Typography`, `RenderFragment` contexts, and `KeyboardEventArgs`; M24 cites runtime/test types; M25 cites `QueryRequest`. |
| 2.1 Current epic impact | [x] | Epic 11 remains valid; the decision only gates Stories 11.11-11.14. |
| 2.2 Epic-level changes | [x] | Story 11.8 moves from backlog to done; implementation stories stay backlog and deliberately ordered last. |
| 2.3 Remaining epics | [x] | No earlier epic is reopened. Release/package evidence is affected before v1.0. |
| 2.4 New/remove epics | [N/A] | No epic added or removed. |
| 2.5 Priority/order | [x] | Existing order is preserved: lower-risk Story 11 work first, package-boundary work last. |
| 3.1 PRD conflicts | [x] | PRD D-5 changes from open/default-defer to resolved/approved split. |
| 3.2 Architecture conflicts | [x] | Layering and multi-TFM text now distinguish `Contracts` kernel from `Contracts.UI`. |
| 3.3 UI/UX conflicts | [x] | UX-DR1 ownership of `Typography` / `FcTypoToken` is affected and remains a Story 11.14 docs task. |
| 3.4 Other artifacts | [x] | Sprint status, project context, contract artifact, and package-compat handoff are updated. |
| 4.1 Direct adjustment | Viable | Recommended: record the decision and update planning/status artifacts. |
| 4.2 Rollback | Not viable | No completed implementation needs rollback. |
| 4.3 MVP review | Not viable | V1 scope is unchanged; package-compat evidence remains required before publication. |
| 4.4 Recommended path | [x] | Direct Adjustment with Moderate handoff to Developer/Release Owner for Stories 11.11-11.14. |
| 5.1-5.5 Proposal components | [x] | Captured in this proposal and the contract artifact. |
| 6.1-6.2 Final review | [x] | Proposal is internally consistent and actionable. |
| 6.3 Approval | [x] | User-directed sign-off on 2026-07-05. |
| 6.4 Sprint status update | [x] | Story 11.8 and E11-AI-2 marked done. |
| 6.5 Handoff | [x] | Stories 11.11-11.14 own implementation/evidence; Story 11.14 owns package-compat docs. |

## Section 3 - Recommended Approach

Use **Direct Adjustment**.

Approved target:

```text
Contracts kernel: netstandard2.0-clean wire/attribute/schema/diagnostic contracts
Contracts.UI: net10-only Blazor/Fluent rendering contracts
SourceTools: netstandard2.0, references only Contracts
```

Effort estimate:

- Low for the decision record and planning/status updates.
- Medium for the implementation set because package boundaries, public API baselines, docs, and release
  inventory must all move together before v1.0.

Risk level:

- Low for the decision.
- Medium for implementation due to public API/package compatibility and adopter build impact.

No MVP scope reduction is needed. The risk is contained by leaving Stories 11.11-11.14 last and requiring
package-compat evidence before the v1.0 release candidate.

## Section 4 - Detailed Change Proposals

### Proposal A - Record The Split Contract

Artifact: `_bmad-output/contracts/fc-contracts-kernel-split-compatibility-plan-2026-07-05.md`

OLD:

```text
Story 11.8 has no standalone decision artifact.
The documented default is to defer if compatibility evidence is incomplete.
```

NEW:

```text
Approve the Contracts kernel split.
Contracts becomes the netstandard2.0-clean kernel.
Contracts.UI is the approved net10-only Blazor/Fluent rendering contract assembly.
Stories 11.11-11.14 implement/evidence the package-boundary change last.
```

Rationale: Story 11.8 requires the decision, affected packages, public API impact, deprecation path, and
release compatibility posture before Story 11.11 starts.

### Proposal B - Amend Project Context Multi-TFM Rules

Artifact: `_bmad-output/project-context.md`

OLD:

```text
Contracts targets net10.0;netstandard2.0.
Guard net10/Fluent-only code with #if NET10_0_OR_GREATER.
```

NEW:

```text
Story 11.8 approves a v1.0 target of a netstandard2.0-clean Contracts kernel plus net10-only
Contracts.UI for Blazor/Fluent rendering contracts. SourceTools remains netstandard2.0 and references
only Contracts. Existing pre-split UI code stays guarded until Story 11.11 moves it.
```

Rationale: Agent implementation guidance must not keep telling future stories to add UI surface to the
kernel.

### Proposal C - Amend Architecture Layering

Artifacts:

- `_bmad-output/planning-artifacts/architecture.md`
- `_bmad-output/project-docs/architecture.md`

OLD:

```text
Layer 0 - Contracts kernel: attributes, communication contracts, rendering model, registration model,
MCP descriptors, schema fingerprint contracts, and diagnostics IDs.
```

NEW:

```text
Layer 0 - Contracts kernel: attributes, communication contracts, registration abstractions, MCP
descriptors, schema fingerprint contracts, and diagnostics IDs.
Layer 0A - Contracts.UI: net10-only Blazor/Fluent rendering contract assembly for Typography,
FcTypoToken, RenderFragment contexts, KeyboardEventArgs members, and rendering contracts.
```

Rationale: The architecture must distinguish kernel contracts from UI rendering contracts before
implementation stories are created.

### Proposal D - Update PRD And Epic Decision State

Artifacts:

- `_bmad-output/planning-artifacts/prd.md`
- `_bmad-output/planning-artifacts/prds/prd-frontcomposer-2026-07-05/prd.md`
- `_bmad-output/planning-artifacts/epics.md`

OLD:

```text
D-5: Open Story 11.8 gate; default is defer the split if compatibility evidence is incomplete.
NFR5: Contracts & SourceTools target net10.0+netstandard2.0.
```

NEW:

```text
D-5: Resolved 2026-07-05. Approve the split.
NFR5/NFR12: Contracts kernel stays netstandard2.0-clean; Contracts.UI carries net10/Blazor/Fluent
rendering contracts; SourceTools references only Contracts.
```

Rationale: The decision gate is complete, but implementation evidence remains assigned to Stories
11.11-11.14.

### Proposal E - Update Sprint Status

Artifact: `_bmad-output/implementation-artifacts/sprint-status.yaml`

OLD:

```yaml
11-8-contracts-kernel-split-decision-and-compatibility-plan: backlog
E11-AI-2 status: open
```

NEW:

```yaml
11-8-contracts-kernel-split-decision-and-compatibility-plan: done
E11-AI-2 status: done
```

Rationale: Story 11.8 is a decision gate. Its implementation stories remain backlog.

## Section 5 - Implementation Handoff

Scope classification: **Moderate**.

Route to:

- Developer agent for Stories 11.11-11.13 implementation.
- Release Owner / Developer for Story 11.14 package compatibility, docs, public API baselines, and
  release inventory updates.

Success criteria:

- `Contracts` can be consumed by netstandard2.0 analyzer/build hosts without inheriting Blazor or the
  pinned Fluent UI RC.
- `SourceTools` still references only `Contracts`.
- Shell/UI consumers compile against the moved rendering contracts.
- Public API baselines, package validation, docs, release inventory, and migration/deprecation guidance
  are updated intentionally.
- Package-consumer validation covers at least one representative Hexalith adopter path before v1.0 RC.
