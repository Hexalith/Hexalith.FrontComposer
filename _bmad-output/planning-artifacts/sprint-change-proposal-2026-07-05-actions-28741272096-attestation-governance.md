---
title: Sprint Change Proposal - Actions 28741272096 Attestation Governance
date: 2026-07-05
status: approved-and-implemented
approval: approved-by-administrator-2026-07-05
scope: minor
trigger:
  - https://github.com/Hexalith/Hexalith.FrontComposer/actions/runs/28741272096/job/85224498728
---

# Sprint Change Proposal - Actions 28741272096 Attestation Governance

## 1. Issue Summary

GitHub Actions release job `85224498728` in run `28741272096` failed during `Run release tests`.
The workflow itself was already using `actions/attest-build-provenance@v4`, but two release-governance
tests still asserted `actions/attest-build-provenance@v2`.

Failed assertions:

- `Story12_4_Def14_AttestBuildProvenanceStep_IsWiredInReleaseWorkflow`
- `ReleaseWorkflow_RunsAutomaticPackageReleaseAfterBlockingTests`

The mismatch was introduced after the approved and implemented July 5 release-pack correction moved the
attestation workflow action from `@v2` to `@v4` to avoid the Node 20-based attestation stack warning while
preserving the workflow `subject-path` input.

## 2. Impact Analysis

Epic impact: no epic scope or ordering change is required. This is a narrow FR-24 release-governance
contract reconciliation.

Story impact: no story inventory update is required. `REL-AI-1` remains open until full FR-24 evidence
exists.

Artifact impact:

- `tests/Hexalith.FrontComposer.Shell.Tests/Governance/CiGovernanceTests.cs`
- `tests/Hexalith.FrontComposer.Shell.Tests/Governance/Story12_4_RedPhaseDefTests.cs`
- `_bmad-output/implementation-artifacts/deferred-work.md`

PRD impact: FR-24 is reinforced; MVP scope does not change.

Architecture impact: no architecture decision changes. The current release workflow continues to use the
approved `@v4` attestation action, and the deeper attestation-bundle integration rewrite remains deferred.

UX impact: not applicable.

Technical impact: release tests now assert the actual release workflow contract. The deferred-work ledger
no longer says the provenance step is missing; it now records the remaining attestation-bundle integration
gap accurately.

## 3. Recommended Approach

Recommended path: Direct Adjustment.

Rationale:

- The failure is a stale test assertion, not a broken release workflow step.
- Downgrading the workflow back to `@v2` would undo the already approved Node 20 warning fix.
- Updating the tests to `@v4` preserves the release workflow's current behavior and keeps the action
  ordering pin before live publish.

Effort: Low.
Risk: Low.
Timeline impact: immediate release-test unblock.

## 4. Detailed Change Proposals

### Release Workflow Governance Test

File: `tests/Hexalith.FrontComposer.Shell.Tests/Governance/CiGovernanceTests.cs`

OLD:

```csharp
workflow.ShouldContain("actions/attest-build-provenance@v2");
workflow.IndexOf("actions/attest-build-provenance@v2", StringComparison.Ordinal)
    .ShouldBeLessThan(workflow.IndexOf("Run semantic-release", StringComparison.Ordinal));
```

NEW:

```csharp
workflow.ShouldContain("actions/attest-build-provenance@v4");
workflow.IndexOf("actions/attest-build-provenance@v4", StringComparison.Ordinal)
    .ShouldBeLessThan(workflow.IndexOf("Run semantic-release", StringComparison.Ordinal));
```

Rationale: the workflow intentionally uses `@v4`; the governance pin should assert that actual contract.

### Story 12.4 Def14 Regression Pin

File: `tests/Hexalith.FrontComposer.Shell.Tests/Governance/Story12_4_RedPhaseDefTests.cs`

OLD:

```csharp
string attestStep = CiGovernanceTests.FindStepBlockContaining(workflow, "actions/attest-build-provenance@v2");
```

NEW:

```csharp
string attestStep = CiGovernanceTests.FindStepBlockContaining(workflow, "actions/attest-build-provenance@v4");
```

Rationale: the structural step-body check should keep proving a real, enabled attestation step exists
before semantic-release, but it must use the approved action major.

### Deferred Work Ledger

File: `_bmad-output/implementation-artifacts/deferred-work.md`

OLD:

```text
CR-12-4-Def14 - `actions/attest-build-provenance@v2` workflow step still missing
```

NEW:

```text
CR-12-4-Def14 - attestation bundle integration still incomplete
```

Rationale: the workflow step now exists. The remaining deferred item is the deeper release workflow
rewrite needed to bind and verify the attestation bundle through the full release manifest path.

## 5. Implementation Handoff

Scope classification: Minor.

Route to: Developer agent for direct implementation.

Success criteria:

- Release workflow keeps `actions/attest-build-provenance@v4`.
- Governance tests assert `@v4`, not `@v2`.
- Def14 still checks the step is real, enabled, non-advisory, and before live publish.
- Full release workflow test-project loop passes locally with `Category!=Quarantined`.

Verification completed:

```sh
DiffEngine_Disabled=true dotnet test tests/Hexalith.FrontComposer.Shell.Tests/Hexalith.FrontComposer.Shell.Tests.csproj --configuration Release --filter "FullyQualifiedName~ReleaseWorkflow_RunsAutomaticPackageReleaseAfterBlockingTests|FullyQualifiedName~Story12_4_Def14"
```

Result: passed, 3/3.

```sh
DiffEngine_Disabled=true dotnet test tests/Hexalith.FrontComposer.Shell.Tests/Hexalith.FrontComposer.Shell.Tests.csproj --configuration Release --filter "Category!=Quarantined"
```

Result: passed, 2053/2053.

```sh
DiffEngine_Disabled=true TEST_PROJECTS="..." bash -lc 'set -euo pipefail; while IFS= read -r project; do [ -z "$project" ] && continue; dotnet test "$project" --configuration Release --filter "Category!=Quarantined"; done <<< "$TEST_PROJECTS"'
```

Result: passed across the release workflow test set: Cli 67/67, Contracts 177/177, Mcp 358/358,
Shell 2053/2053, SourceTools 1051/1051, Testing 30/30.

## 6. Checklist Status

- [x] 1.1 Triggering issue identified: release job `85224498728` in run `28741272096`.
- [x] 1.2 Core problem defined: governance tests asserted stale `@v2` action after workflow moved to `@v4`.
- [x] 1.3 Evidence gathered: GitHub Actions log and local test reproduction.
- [x] 2.1 Current epic remains viable: FR-24 release path remains valid.
- [N/A] 2.2-2.5 Epic scope/order changes: no backlog restructure required.
- [x] 3.1 PRD conflicts checked: FR-24 is reinforced; no MVP scope change.
- [x] 3.2 Architecture conflicts checked: no architecture change.
- [N/A] 3.3 UI/UX conflicts: no UI behavior changes.
- [x] 3.4 Secondary artifacts checked: release governance tests and Def14 deferred-work ledger updated.
- [x] 4.1 Direct Adjustment selected.
- [N/A] 4.2 Rollback not useful.
- [N/A] 4.3 MVP review not required.
- [x] 5.1-5.5 Proposal and handoff documented.
- [x] 6.1-6.2 Proposal verified for consistency.
- [N/A] 6.3 Explicit approval: user requested direct fix.
- [N/A] 6.4 Sprint status update: no epic or story entries changed.
- [x] 6.5 Next steps and handoff plan defined.
