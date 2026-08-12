---
title: 'Story 9.3: Define explicit command target identity'
type: 'feature'
created: '2026-08-12'
status: 'done'
baseline_commit: '8ba36a8c0494cd8f5640b4383ff2fab0742ff836'
review_loop_iteration: 0
context:
  - '{project-root}/_bmad-output/project-context.md'
  - '{project-root}/_bmad-output/implementation-artifacts/epic-9-context.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** The approved FC-NIP base source silently treats an ambient generated source row as the command target. It cannot safely describe standalone create, cross-row, status-move, delete, or no-op outcomes, so Stories 9.4-9.6 have no trustworthy target contract to implement.

**Approach:** Approve a successor decision contract in which a generated command-target descriptor and FrontComposer-owned typed provider resolve one immutable target snapshot before asynchronous dispatch. Keep terminal materiality separate, fail closed on unknown identity/materiality, and pin the decision and complete outcome matrix with governance tests.

## Boundaries & Constraints

**Always:** Preserve the 2026-07-05 row-context decision as historical base authority. Define target `ProjectionTypeName`, canonical view/lane, exact `EntityKey`, change kind, prior/expected status, and `CapturedAt`; attach `MessageId` after acceptance and keep terminal `ObservedAt` separate. Resolve dynamic values only through an explicit command-to-projection declaration plus typed `ICommandTargetIdentityProvider<TCommand>`; an explicitly declared `SameAsSource` mode may consume a pre-dispatch source snapshot. Terminal adapters report `Material`, `NoOp`, or `Unknown`; `Unknown` suppresses the indicator.

**Ask First:** Any public runtime API implementation, EventStore contract change, multi-target command support, or change to the accepted idempotent/ten-second-linger UX requires a separate human decision.

**Never:** Implement Stories 9.4-9.6 here; infer a target from ambient source-row placement, property-name conventions, routes, projection nudges, visible-row diffs, broad lane marking, unproven EventStore `AggregateId`, or opaque result payloads; edit generated output or submodules; alter packages, schema fingerprints, deployment, or public API baselines.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Standalone create | Explicit projection declaration + typed target provider | Snapshot new target before dispatch; material confirmation is indicator-eligible | Missing/unknown target suppresses publication |
| Same-row update | Explicit `SameAsSource` declaration + source snapshot | Copy source as the named target before dispatch; material confirmation is eligible | Never fall back to ambient cascade |
| Cross-row update | Provider resolves a target distinct from source | Publish only for the declared target | Source reuse without declaration is invalid |
| Status move | Target row + prior and destination status | Target destination lane; preserve both statuses | Missing destination status suppresses publication |
| Delete | Explicit target + `Delete` kind | Preserve target for lifecycle/audit; no fresh-row indicator | Material delete remains suppressed |
| Idempotent confirmation | Material target + `IdempotentConfirmed` | Preserve existing eligible/TTL disposition | `NoOp` or `Unknown` materiality suppresses |
| Rejected / needs review | Any declared target | No indicator | Preserve rejection/review lifecycle state |
| No-op | Typed terminal `NoOp` (`EventCount == 0` or equivalent) | No indicator | Never infer materiality from status text |

</frozen-after-approval>

## Code Map

- `_bmad-output/contracts/fc-nip-row-identity-producer-contract-2026-07-04.md` -- historical base contract; retain its decision and link the approved successor.
- `_bmad-output/contracts/fc-nip-command-target-identity-contract-2026-08-12.md` -- new authoritative Story 9.3 decision and disposition matrix.
- `_bmad-output/planning-artifacts/prd.md` and `_bmad-output/planning-artifacts/architecture.md` -- resolve D-4 and name the target-provider/materiality invariants.
- `_bmad-output/project-docs/architecture.md` and `docs/reference/components/datagrid.md` -- synchronize developer/adopter truth without claiming composed completion.
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Docs/FcNipRowIdentityProducerContractTests.cs` -- governance guard; replace stale pre-remediation wording with base-plus-successor assertions.
- `tests/e2e/specs/fc-nip-row-identity-contract.spec.ts` -- browserless contract guard mirroring the successor fields, forbidden sources, and eight dispositions.
- `src/Hexalith.FrontComposer.Shell/State/PendingCommands/PendingCommandRowIdentity.cs` and `src/Hexalith.FrontComposer.SourceTools/Emitters/{CommandFormEmitter,RazorEmitter}.cs` -- read-only gap evidence for later stories; do not change in 9.3.

## Tasks & Acceptance

**Execution:**
- [x] `_bmad-output/contracts/fc-nip-command-target-identity-contract-2026-08-12.md` -- record the approved provider, immutable snapshot, materiality model, complete matrix, and downstream ownership.
- [x] Base contract, PRD, both architecture sources, and DataGrid reference -- link the successor decision and resolve D-4 while keeping Epic 9/FR-13/FR-26 open through Story 9.8.
- [x] C# and Playwright FC-NIP contract guards -- pin required fields, pre-dispatch capture, explicit source-versus-target rules, fail-closed behavior, and all matrix rows without weakening no-guessing checks.

**Acceptance Criteria:**
- Given Product + Architecture approve the successor contract, when target identity is reviewed, then every required field has one framework-owned source and target capture precedes dispatch.
- Given every supported command/outcome shape, when the matrix is evaluated, then its target and indicator disposition are explicit and no source row is silently reused.
- Given the decision artifact or synchronized truth sources drift, when governance runs, then focused guards fail while Stories 9.4-9.6 remain implementation owners.

## Spec Change Log

## Design Notes

The provider is the trust boundary: SourceTools may generate its registration from an explicit declaration, but generic reflection over command fields is not authoritative. Terminal materiality is independent of target intent so an existing EventStore `EventCount` or equivalent typed callback can distinguish material work from no-op without treating EventStore identity as projection identity.

## Verification

**Commands:**
- `DiffEngine_Disabled=true dotnet test Hexalith.FrontComposer.slnx --filter "Category!=Performance&Category!=e2e-palette&Category!=NightlyProperty&Category!=Quarantined"` -- expected: blocking .NET lane passes.
- `cd tests/e2e && npm run test:fc-nip` -- expected: all FC-NIP contract checks pass without a web server.
- `pwsh ./eng/validate-docs.ps1` -- expected: canonical and published documentation validates.

**Actual evidence (2026-08-12):**
- `dotnet build tests/Hexalith.FrontComposer.SourceTools.Tests/Hexalith.FrontComposer.SourceTools.Tests.csproj --configuration Release --no-restore` and `DiffEngine_Disabled=true dotnet tests/Hexalith.FrontComposer.SourceTools.Tests/bin/Release/net10.0/Hexalith.FrontComposer.SourceTools.Tests.dll -class Hexalith.FrontComposer.SourceTools.Tests.Docs.FcNipRowIdentityProducerContractTests` -- Release build passed with 0 warnings / 0 errors; 5/5 focused governance tests passed.
- `cd tests/e2e && npm run test:fc-nip` -- 6/6 browserless FC-NIP tests passed.
- `pwsh ./eng/validate-docs.ps1` -- passed; emitted `artifacts/docs/validation-manifest.json`.
- `dotnet build tests/Hexalith.FrontComposer.Shell.Tests/Hexalith.FrontComposer.Shell.Tests.csproj --configuration Release --no-restore` and `DiffEngine_Disabled=true dotnet tests/Hexalith.FrontComposer.Shell.Tests/bin/Release/net10.0/Hexalith.FrontComposer.Shell.Tests.dll -method Hexalith.FrontComposer.Shell.Tests.Governance.AnalyzerPolicyGovernanceTests.AnalyzerPolicy_IdentifierInventory_MatchesSeal` -- Release build passed with 0 warnings / 0 errors; identifier inventory guard passed 1/1 after resealing the story-owned test identifier delta.
- `DiffEngine_Disabled=true dotnet test Hexalith.FrontComposer.slnx --filter "Category!=Performance&Category!=e2e-palette&Category!=NightlyProperty&Category!=Quarantined"` -- 4,333/4,333 passed with 0 failures.
- `python3 eng/validate-story-artifacts.py --story _bmad-output/implementation-artifacts/spec-9-3-define-explicit-command-target-identity.md --base 8ba36a8c0494cd8f5640b4383ff2fab0742ff836` -- passed.
- `git diff --check` -- passed.

## File List

- `_bmad-output/contracts/analyzer-policy-exception-ledger-v1.json`
- `_bmad-output/contracts/fc-nip-command-target-identity-contract-2026-08-12.md`
- `_bmad-output/contracts/fc-nip-row-identity-producer-contract-2026-07-04.md`
- `_bmad-output/implementation-artifacts/epic-9-context.md`
- `_bmad-output/implementation-artifacts/spec-9-3-define-explicit-command-target-identity.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `_bmad-output/planning-artifacts/architecture.md`
- `_bmad-output/planning-artifacts/prd.md`
- `_bmad-output/project-docs/architecture.md`
- `docs/reference/components/datagrid.md`
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Docs/FcNipRowIdentityProducerContractTests.cs`
- `tests/e2e/specs/fc-nip-row-identity-contract.spec.ts`

## Suggested Review Order

**Approved target-identity decision**

- Start with the successor boundary and explicit provider resolution model.
  [`fc-nip-command-target-identity-contract-2026-08-12.md:11`](../contracts/fc-nip-command-target-identity-contract-2026-08-12.md#L11)

- Review immutable fields and fail-closed server-assigned-key handling.
  [`fc-nip-command-target-identity-contract-2026-08-12.md:49`](../contracts/fc-nip-command-target-identity-contract-2026-08-12.md#L49)

- Confirm successor concepts map unambiguously onto historical carrier fields.
  [`fc-nip-command-target-identity-contract-2026-08-12.md:88`](../contracts/fc-nip-command-target-identity-contract-2026-08-12.md#L88)

- Trace capture, acceptance, race buffering, conflict, and timestamp separation.
  [`fc-nip-command-target-identity-contract-2026-08-12.md:104`](../contracts/fc-nip-command-target-identity-contract-2026-08-12.md#L104)

- Validate all eight target, materiality, indicator, and duplicate dispositions.
  [`fc-nip-command-target-identity-contract-2026-08-12.md:145`](../contracts/fc-nip-command-target-identity-contract-2026-08-12.md#L145)

- Check inference prohibitions and later-story ownership boundaries.
  [`fc-nip-command-target-identity-contract-2026-08-12.md:169`](../contracts/fc-nip-command-target-identity-contract-2026-08-12.md#L169)

**Synchronized product and architecture truth**

- Verify the historical base remains authoritative and links its approved successor.
  [`fc-nip-row-identity-producer-contract-2026-07-04.md:5`](../contracts/fc-nip-row-identity-producer-contract-2026-07-04.md#L5)

- Confirm D-4 is resolved while Epic 9 completion remains blocked.
  [`prd.md:528`](../planning-artifacts/prd.md#L528)

- Review planning invariants without mistaking the decision for runtime delivery.
  [`architecture.md:56`](../planning-artifacts/architecture.md#L56)

- Check published architecture mirrors pre-dispatch and fail-closed boundaries.
  [`architecture.md:103`](../project-docs/architecture.md#L103)

- Review adopter-facing wording and remaining Story 9.4–9.8 ownership.
  [`datagrid.md:83`](../../docs/reference/components/datagrid.md#L83)

**Governance and evidence**

- Inspect structural C# guards for snapshot, matrix, and no-smuggling invariants.
  [`FcNipRowIdentityProducerContractTests.cs:56`](../../tests/Hexalith.FrontComposer.SourceTools.Tests/Docs/FcNipRowIdentityProducerContractTests.cs#L56)

- Cross-check browserless parity and source-level forbidden-mapping evidence.
  [`fc-nip-row-identity-contract.spec.ts:50`](../../tests/e2e/specs/fc-nip-row-identity-contract.spec.ts#L50)

- Confirm the intentional governance identifier inventory reseal.
  [`analyzer-policy-exception-ledger-v1.json:98`](../contracts/analyzer-policy-exception-ledger-v1.json#L98)
