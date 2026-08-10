---
title: 'Story 11.24: Adopt the Owner-Approved EventStore Runtime Identity'
type: 'refactor'
created: '2026-08-10'
updated: '2026-08-10'
status: 'draft'
review_loop_iteration: 0
decision: 'remain-backlog'
blocked_by:
  - 'eventstore-owner-approved-replacement-runtime-identity'
  - 'eventstore-owned-provider-verification-and-pact-reconciliation'
context:
  - '{project-root}/_bmad-output/implementation-artifacts/epic-11-context.md'
  - '{project-root}/_bmad-output/planning-artifacts/architecture.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Debug currently consumes EventStore source commit `5bcfdbc8b28ac2706053075cc4e71160ee029ad8`, while Release resolves `3.91.1` packages built from `bb94d93e9b84132cff83a38fba84f25455820d31`. Story 1.20 approved `fa2d1c9910f8976553adb33dcdb1c9ff2ea75594` plus `999.1.20-proof.fa2d1c9910f8`, but all 14 approved package archives are documented as unrecoverable and no FrontComposer waiver exists.

**Approach:** Keep Story 11.24 in backlog without repository or dependency mutations. Resume only after EventStore and Release owners approve a retrievable replacement identity and EventStore supplies real provider verification with pact compatibility resolved separately.

## Boundaries & Constraints

**Always:** Require a 40-hex approved source SHA, published package version and SHA-256 inventory, named approvals, and an already-selected Builds catalog commit. Verify the root gitlink equals the checkout; restore Release from an isolated cache; reject EventStore project assets in Release; use real loopback TCP with deterministic provider-state cleanup; keep evidence bounded and redaction-clean.

**Ask First:** Any proposal to replace the upstream prerequisites with a FrontComposer waiver, move provider ownership into FrontComposer, or expand this identity-only story into pact/API migration work.

**Never:** Treat current `3.91.1`, `5bcfdbc8...`, ancestry, or catalog presence as approval. Edit EventStore submodule contents, initialize nested submodules, accept a merely present provider report, or redesign adapters, rollback, Aspire topology, or EventStore container ownership.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|---------------|----------------------------|----------------|
| Authorized identity | Complete owner record; matching source, catalog, package bytes | Debug and Release resolve the same approved runtime; evidence records exact identities | Fail on any missing/mismatched field or asset |
| Retired identity | Approved version exists only in historical documentation | No gitlink, catalog, or code mutation | Report package retrieval/authority blocker |
| Provider drift | A committed pact differs from real provider behavior | No passing report is fabricated | Route behavioral reconciliation to separately approved work |

</frozen-after-approval>

## Code Map

- `references/Hexalith.EventStore/_bmad-output/implementation-artifacts/1-20-owner-approved-parity-closure-proof-packet.md` -- read-only historical authority; exact source, 14 hashes, approvals, and migration flag.
- `references/Hexalith.EventStore/_bmad-output/implementation-artifacts/3-13-deployed-runtime-parity-closure-proof-packet.md` -- read-only evidence that 0/14 approved archives remain and no replacement is authorized.
- `Directory.Build.props`, `deps.local.props`, `deps.nuget.props` -- Debug/source versus Release/package selection.
- `references/Hexalith.Builds/Props/Directory.Packages.props`, `eng/dependency-graph-policy.json` -- selected EventStore version and governed Release graph.
- `src/Hexalith.FrontComposer.AppHost/Hexalith.FrontComposer.AppHost.csproj` -- conditional Aspire project/package edge; Release AppHost needs explicit isolated validation because the solution excludes it.
- `tests/Hexalith.FrontComposer.Shell.Tests/Pact/EventStorePactContractTests.cs` -- generates 19 mock-server interactions; no provider verifier exists.
- `tests/Hexalith.FrontComposer.Shell.Tests/Pact/provider-state-catalog.json` -- claimed EventStore-owned state setup/teardown, currently inconsistent with several pact requests.
- `eng/validate-contract-artifacts.ps1` -- must validate a success schema, exact runtime, interaction/state counts, loopback execution, bounds, and redaction.
- `.github/workflows/quality.yml`, `tests/Hexalith.FrontComposer.Shell.Tests/Governance/CiGovernanceTests.cs` -- Gate 2c currently emits `BLOCKED_HANDOFF` and does not require provider verification.
- `src/Hexalith.FrontComposer.AppHost/Program.cs` -- existing EventStore/DAPR/SignalR topology to exercise without redesign.

## Tasks & Acceptance

**Execution:**
- [ ] `references/Hexalith.EventStore/_bmad-output/implementation-artifacts/` -- external prerequisite: EventStore and Release owners publish a retrievable replacement source/package authority with Builds commit and hashes.
- [ ] EventStore-owned provider-test project -- external prerequisite: verify FrontComposer pacts over real loopback TCP after separately reconciling provider-wire drift.
- [ ] `references/Hexalith.EventStore`, `eng/dependency-graph-policy.json` -- align the root source gitlink and governed package identity to the approved record without editing submodule contents.
- [ ] `eng/validate-contract-artifacts.ps1` -- fail closed on report schema, success, exact runtime, all interactions/states, loopback TCP, cleanup, bounds, and redaction.
- [ ] `.github/workflows/quality.yml`, `CiGovernanceTests.cs` -- run the explicit provider lane and require its report; remove the obsolete solution handoff.
- [ ] AppHost/integration test surface -- prove health, command submit/status, query provenance, and projection SignalR with existing topology.

**Acceptance Criteria:**
- Given incomplete, retired, or non-authorizing identity evidence, when validation starts, then Story 11.24 remains backlog and no dependency pointer changes.
- Given a complete replacement authority, when Debug and isolated Release restore run, then source checkout and every EventStore package asset match the approved identity and Release contains no EventStore project edge.
- Given the committed pacts, when the real provider lane runs over loopback TCP, then every deterministic state passes and `validate-contract-artifacts.ps1 -RequireProviderVerification` accepts only the exact bounded report.
- Given aligned identities, when Governance, default tests, explicit AppHost builds, and live Aspire smoke run, then they pass without adapter, rollback, topology, or container redesign.

## Spec Change Log

## Design Notes

On 2026-08-10 the user selected the fail-closed recommendation: keep 11.24 in backlog, require a new upstream identity packet, and keep provider verification plus pact reconciliation as upstream prerequisites. The historical approval cannot be translated to a newer release, and Tenants Story 2.12's waiver excludes other consumers.

## Verification

**Commands:**
- `git ls-tree HEAD references/Hexalith.EventStore && git -C references/Hexalith.EventStore rev-parse HEAD` -- expected: both equal the newly approved source SHA.
- `runtime_packages="$(mktemp -d)" && dotnet restore src/Hexalith.FrontComposer.AppHost/Hexalith.FrontComposer.AppHost.csproj --configuration Release --packages "$runtime_packages"` -- expected: all resolved EventStore assets are approved packages with verified hashes and no project assets.
- `DiffEngine_Disabled=true dotnet test Hexalith.FrontComposer.slnx --configuration Release --filter "Category=Governance"` -- expected: all identity and contract gates pass.
- `pwsh ./eng/validate-contract-artifacts.ps1 -RequireProviderVerification` -- expected: exact-runtime provider verification succeeds for all committed interactions.
