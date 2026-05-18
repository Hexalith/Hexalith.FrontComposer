# Sprint Change Proposal: Pin .NET SDK 10.0.300

Date: 2026-05-18
Project: Hexalith.FrontComposer
Requested by: Jerome
Mode: Batch
Target SDK: .NET 10.0.300

## 1. Issue Summary

Jerome requested that Hexalith.FrontComposer use `.NET 10.0.300`.

The repository currently resolves to `.NET SDK 10.0.203` because `global.json` pins `10.0.203` with `rollForward: latestPatch`. The machine has `10.0.300` installed, so the change is not blocked by local toolchain availability. However, planning and release-readiness artifacts still contain older SDK pins:

- `global.json` pins `10.0.203`.
- `_bmad-output/project-context.md` says agents should use `10.0.103`.
- `Directory.Packages.props` has a comment tied to `10.0.203` and Roslyn host assumptions.
- `docs/ide-parity-matrix.md` and `docs/ide-parity-matrix.json` pin `10.0.103`.
- `docs/hot-reload-guide.md` records a measurement environment using `10.0.104`.
- Planning artifacts still reference the original `10.0.5` architecture/story baseline.

Concrete local evidence:

```text
dotnet --list-sdks
10.0.108
10.0.203
10.0.300

dotnet --version
10.0.203
```

This means the repo can move to `10.0.300`, but the change should be handled as a release-readiness correction: update the pin, update authoritative agent and parity documentation, then re-run the build/test/evidence lanes that depend on SDK behavior.

## 2. Impact Analysis

### Checklist Status

| Item | Status | Finding |
| --- | --- | --- |
| 1.1 Triggering story | [x] Done | Direct maintainer request during Epic 12 release-certification work. Closest active story impact is Story 12.4, Trusted Release Evidence Dry Run. |
| 1.2 Core problem | [x] Done | Technical/release-governance drift: SDK pin and documentation disagree with requested target. |
| 1.3 Evidence | [x] Done | Local SDK list includes `10.0.300`; `dotnet --version` still resolves `10.0.203`; docs contain stale `10.0.103`, `10.0.104`, and `10.0.5` references. |
| 2.1 Current epic viability | [x] Done | Epic 12 remains viable; SDK rebaseline strengthens release evidence. |
| 2.2 Epic-level changes | [x] Done | No new epic required. Add/execute a bounded SDK rebaseline task under Epic 12 release certification. |
| 2.3 Remaining epics | [x] Done | Completed Epics 1 and 9 contain stale baseline text; no feature reopening required if updated as historical/current-baseline corrections. |
| 2.4 New/obsolete epics | [N/A] Skip | No epics become obsolete. |
| 2.5 Priority/order | [x] Done | Perform before final release-readiness claims and before relying on IDE parity/hot-reload evidence. |
| 3.1 PRD conflicts | [x] Done | No PRD scope conflict. The PRD already says .NET 10 is first-class and .NET 8/9 are unsupported. |
| 3.2 Architecture conflicts | [!] Action-needed | Architecture still lists the original `10.0.5` baseline. Add a current-baseline note instead of rewriting history wholesale. |
| 3.3 UI/UX conflicts | [N/A] Skip | No user-facing UI/UX change. |
| 3.4 Other artifacts | [!] Action-needed | `global.json`, agent context, IDE parity matrix, hot-reload evidence notes, package comments, and release evidence need updates. |
| 4.1 Direct adjustment | [x] Viable | Low effort, medium validation risk because SDK/compiler host can affect generators/analyzers. |
| 4.2 Rollback | [x] Not viable | No recent work should be reverted. |
| 4.3 PRD MVP review | [x] Not viable | MVP still stands; this is a baseline correction, not scope expansion. |
| 4.4 Recommended path | [x] Done | Direct Adjustment with explicit validation. |
| 5.1-5.5 Proposal components | [x] Done | Captured below. |
| 6.1-6.5 Final review/handoff | [!] Action-needed | Requires explicit approval before implementation and sprint-status update. |

### Epic Impact

Epic 1: Project Scaffolding and First Auto-Generated View

- Story 1.1 currently contains an outdated acceptance criterion: `global.json pins .NET SDK 10.0.5`.
- This should be treated as historical planning drift. Update the acceptance/evidence note to say the current repo baseline is `10.0.300`, while the original planning baseline was superseded.
- No Story 1.1 implementation rollback is needed.

Epic 9: Developer Tooling and Documentation

- Story 9.3 owns IDE parity and the generated output path contract across SDK bumps.
- The IDE parity matrix currently pins `10.0.103`; this must move to `10.0.300`.
- Revalidation should cover generated path stability and NFR8 incremental generator behavior.

Epic 10: Framework Quality and Adopter Confidence

- SDK/compiler changes can affect unit/component test behavior, source-generator benchmarks, mutation/property lanes, and package/release evidence.
- No new Story 10 scope is required, but validation output should be captured before release readiness.

Epic 12: Release Certification and Evidence Alignment

- Story 12.4 is the natural owner for trusted-context release evidence. The SDK rebaseline should be recorded there or as a small follow-up release-gate task before Story 12.4 closes.
- Story 12.5 is only indirectly affected if accessibility/stakeholder evidence quotes the runtime/toolchain.

### Artifact Conflicts

PRD:

- No functional requirement change.
- The PRD language matrix already says C# on .NET 10 is first-class and .NET 8/9 are not v1.
- No MVP reduction required.

Architecture:

- Current conflict: architecture still lists `.NET SDK 10.0.5`.
- Recommended update: preserve original W1 baseline context, but add a current baseline note stating release certification now pins `10.0.300`.

Implementation and repo configuration:

- `global.json` must change from `10.0.203` to `10.0.300`.
- `Directory.Packages.props` comment must change from `10.0.203` to `10.0.300`, and the Roslyn compiler host statement must be verified or generalized if the host version differs.
- Any generated caches or local `.lscache` files should not be manually edited unless they are intentionally tracked and regenerated by the validation run.

Documentation:

- `_bmad-output/project-context.md` must change from `10.0.103` to `10.0.300`.
- `docs/ide-parity-matrix.md` and `docs/ide-parity-matrix.json` must change from `10.0.103` to `10.0.300`.
- `docs/hot-reload-guide.md` should keep the original measurement table as historical evidence, but add a revalidation note for `10.0.300` once measured.

Sprint status:

- No epic/story status should change until implementation and validation are complete.
- After approval, update `last_updated` to mention the SDK rebaseline and validation result.

## 3. Recommended Approach

Use Direct Adjustment.

Rationale:

- The requested SDK is installed locally.
- The project already targets `.NET 10`, so this is not a runtime/platform migration.
- The blast radius is real but bounded: SDK resolver, compiler host behavior, source-generator output, IDE parity evidence, hot-reload notes, and release evidence.
- Rollback would not simplify the change.
- MVP scope remains intact.

Effort estimate: Low to Medium.

Risk level: Medium until validation completes. The main risk is not ordinary compilation; it is subtle generator/analyzer, IDE parity, and release-evidence drift under a newer SDK feature band.

Required validation:

```powershell
dotnet --version
dotnet restore Hexalith.FrontComposer.slnx
dotnet test Hexalith.FrontComposer.slnx --configuration Release --filter "Category!=Performance&Category!=e2e-palette&Category!=NightlyProperty&Category!=Quarantined"
```

If time permits before release readiness:

```powershell
npm --prefix tests/e2e test
```

Only run E2E if the local web/apphost prerequisites are ready; otherwise record it as deferred release evidence rather than silently skipping it.

## 4. Detailed Change Proposals

### Repo Configuration

File: `global.json`

OLD:

```json
{
  "sdk": {
    "version": "10.0.203",
    "rollForward": "latestPatch"
  }
}
```

NEW:

```json
{
  "sdk": {
    "version": "10.0.300",
    "rollForward": "latestPatch"
  }
}
```

Rationale: Make the repo resolve the requested SDK feature band.

File: `Directory.Packages.props`

OLD:

```xml
<!-- Roslyn analyzers require an SDK/compiler host at least as new as these packages.
     global.json pins the repo to SDK 10.0.203, whose compiler host is Roslyn 5.3.0.
     Keep the source-generator package pins aligned with the pinned SDK feature band. -->
```

NEW:

```xml
<!-- Roslyn analyzers require an SDK/compiler host at least as new as these packages.
     global.json pins the repo to SDK 10.0.300.
     Keep the source-generator package pins aligned with the pinned SDK feature band and verify compiler-host compatibility after SDK bumps. -->
```

Rationale: Avoid carrying an unverified compiler-host version claim across the SDK bump.

### Agent Context

File: `_bmad-output/project-context.md`

OLD:

```markdown
- Use .NET SDK `10.0.103` from `global.json`; roll forward only to latest patch.
```

NEW:

```markdown
- Use .NET SDK `10.0.300` from `global.json`; roll forward only to latest patch.
```

Rationale: Prevent future agents from following stale SDK guidance.

### IDE Parity

File: `docs/ide-parity-matrix.md`

OLD:

```markdown
| .NET SDK | 10.0.103 |
```

NEW:

```markdown
| .NET SDK | 10.0.300 |
```

Rationale: Story 9.3 treats SDK version as part of the parity contract.

File: `docs/ide-parity-matrix.json`

OLD:

```json
"dotnetSdk": "10.0.103"
```

NEW:

```json
"dotnetSdk": "10.0.300"
```

Rationale: Keep machine-readable parity evidence aligned with the human matrix.

### Hot Reload Evidence

File: `docs/hot-reload-guide.md`

OLD:

```markdown
| .NET SDK (`dotnet --version`) | 10.0.104 |
```

NEW:

```markdown
| .NET SDK (`dotnet --version`) | 10.0.104 |

> Revalidation note: repo baseline moved to .NET SDK `10.0.300` on 2026-05-18. The historical Story 1.8 measurement above remains preserved as original evidence; release certification must capture a fresh `10.0.300` validation row before claiming current hot-reload parity.
```

Rationale: Preserve historical evidence while making the current certification requirement explicit.

### Planning Artifacts

File: `_bmad-output/planning-artifacts/architecture.md`

OLD:

```markdown
| **.NET SDK** | 10.0.5 (GA, LTS) | .NET 10 | Aligned |
```

NEW:

```markdown
| **.NET SDK** | 10.0.300 current release-certification baseline; original W1 baseline was 10.0.5 | .NET 10 | Aligned |
```

Rationale: Avoid rewriting original architecture history while correcting the current baseline.

File: `_bmad-output/planning-artifacts/epics/epic-1-project-scaffolding-first-auto-generated-view.md`

OLD:

```markdown
**And** global.json pins .NET SDK 10.0.5
```

NEW:

```markdown
**And** global.json pins the current release-certification .NET 10 SDK baseline (`10.0.300` as of 2026-05-18)
```

Rationale: Story 1.1 should no longer instruct future work to verify against an obsolete planning baseline.

### Sprint Status

File: `_bmad-output/implementation-artifacts/sprint-status.yaml`

Proposed `last_updated` addition after implementation:

```yaml
last_updated: "2026-05-18 (.NET SDK baseline corrected to 10.0.300; global.json, agent context, IDE parity matrix, architecture/story baseline notes, and release validation evidence updated; release/main-lane validation result recorded.)"
```

Rationale: Make the sprint record reflect that this is a release-certification baseline correction.

## 5. Implementation Handoff

Scope classification: Minor, with medium validation attention.

Route to: Developer agent.

Developer responsibilities:

- Update `global.json` to `10.0.300`.
- Update stale SDK references listed in this proposal.
- Re-run `dotnet --version` and confirm it resolves `10.0.300`.
- Run restore and the main-lane Release test command.
- Record any failures as SDK rebaseline findings instead of papering over them.
- Update `sprint-status.yaml` only after validation is complete.

Product/architecture responsibilities:

- No PRD or MVP replan required.
- Accept the architecture/story text changes as current-baseline corrections, not a reopening of completed Epic 1 work.

Success criteria:

- `dotnet --version` returns `10.0.300` from the repository root.
- `dotnet restore Hexalith.FrontComposer.slnx` succeeds.
- Main-lane Release tests pass or any failures are documented with owner and next action.
- Agent/project context and IDE parity matrix no longer point at `10.0.103`.
- Release-certification artifacts clearly distinguish historical evidence from current `10.0.300` evidence.

## Approval

Recommended decision: Approve Direct Adjustment.

Approved by Jerome on 2026-05-18.

## Implementation Result

Implemented on 2026-05-18.

Validation:

```text
dotnet --version
10.0.300

dotnet restore Hexalith.FrontComposer.slnx
Succeeded

dotnet test Hexalith.FrontComposer.slnx --configuration Release --filter "Category!=Performance&Category!=e2e-palette&Category!=NightlyProperty&Category!=Quarantined"
Passed
```

One SDK-drift failure was found and fixed during validation: `IdeParityMatrixContractTests.MatrixJson_HasFailClosedSchemaForEveryRow` still expected `10.0.103`. The test now expects `10.0.300`, matching the updated IDE parity matrix.
