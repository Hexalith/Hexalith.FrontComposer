---
title: 'Story 11.24: Adopt the Owner-Approved EventStore Runtime Identity'
type: 'refactor'
created: '2026-08-10'
updated: '2026-08-14'
status: 'in-progress'
baseline_commit: '9a3d14b8460ff05ea74d7adbba1547ea9d1ba0b0'
review_loop_iteration: 0
decision: 'remain-backlog'
blocked_by:
  - 'eventstore-owned-provider-verification-and-pact-reconciliation'
context:
  - '{project-root}/_bmad-output/implementation-artifacts/epic-11-context.md'
  - '{project-root}/references/Hexalith.EventStore/_bmad-output/implementation-artifacts/frontcomposer-11-24-runtime-identity-successor.md'
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

Successor (EventStore-owned, 2026-08-12; read-only):
- `references/Hexalith.EventStore/_bmad-output/implementation-artifacts/frontcomposer-11-24-runtime-identity-successor.md` -- `available` + `authorize_consumer_migration: true`. Bound tuple: source `bb94d93e9b84132cff83a38fba84f25455820d31`, version `3.91.1`, Builds `a8a50859fa2f27f511a9470dfe1e3ae54d0ebc1a`, subject `9d074dfd0758a8934f122aab18659627dff1cf5d4c3e548b222cc0d79a881065`. Rejects Story 1.20 proofs, ancestry, catalog presence, and Tenants 2.12 waiver.
- `.../evidence/frontcomposer-story-11-24/bb94d93e9b84132cff83a38fba84f25455820d31/nuget-sha256.txt` -- 14 retrievable NuGet.org hashes; manifest `6b0b70b856839d4117bcd969f6a2de0093c477c109cb79f3f2882b1f05effcae`.
- `.../acceptances/9d074dfd0758a8934f122aab18659627dff1cf5d4c3e548b222cc0d79a881065/` -- `eventstore-owner.json` and `release-owner.json` (`github:jpiquot`).
- `.../1-20-owner-approved-parity-closure-proof-packet.md` and `.../3-13-deployed-runtime-parity-closure-proof-packet.md` -- retired / 0-of-14 recovered; do not adopt.

Workspace 2026-08-14 (not authorized): EventStore gitlink/checkout `80d12ef5eee71a9fe3ea7be51171da4a71b69a28`; Builds `606d9f119965c273104d707b9cc8c179fe648237` selects `HexalithEventStoreVersion=3.94.0` in `references/Hexalith.Builds/Props/Directory.Packages.props` L8. `eng/dependency-graph-policy.json` requires that property name only.

Selection and contract lanes:
- `Directory.Build.props` L16-L26, `deps.local.props` L5/L16, `deps.nuget.props` L4 -- Debug source vs Release package.
- `src/Hexalith.FrontComposer.AppHost/Hexalith.FrontComposer.AppHost.csproj` L15-L19, L30-L39 -- Aspire ProjectReference vs PackageReference; isolate Release restore (AppHost is outside the `.slnx`).
- `src/Hexalith.FrontComposer.AppHost/Program.cs` L22-L139 -- existing Keycloak/DAPR/SignalR topology; reuse only.
- `tests/Hexalith.FrontComposer.Shell.Tests/Pact/EventStorePactContractTests.cs` -- 19 consumer interactions, mock-only verify, blocked handoff. No FrontComposer provider verifier.
- `tests/Hexalith.FrontComposer.Shell.Tests/Pact/provider-state-catalog.json` -- 19 EventStore-owned states; seeded-id mismatches are provider reconciliation, not this story.
- `eng/validate-contract-artifacts.ps1` L5, L209-L232 -- `-RequireProviderVerification` today accepts a non-empty file (`VERIFICATION_REPORT_PRESENT`). Gate 2c default writes `BLOCKED_HANDOFF`.
- `.github/workflows/quality.yml` L140-L163 and `CiGovernanceTests.QualityWorkflow_PinsContractPactStaleAndArtifactGates` L189-L209 -- consumer pacts + validator without the switch + stale-diff pin.

## Tasks & Acceptance

**Execution:**
- [x] Successor packet + EventStore provider lane -- fail-closed gate: both owner receipts still authorize the bound tuple, and EventStore now emits a real loopback report for all 19 committed interactions after separately reconciled pact drift. If either fails, mutate nothing and leave sprint-status `backlog`.
- [ ] `references/Hexalith.EventStore`, `eng/dependency-graph-policy.json` -- only after both gates, set gitlink/checkout to `bb94d93e9b84132cff83a38fba84f25455820d31` and select Builds `a8a50859fa2f27f511a9470dfe1e3ae54d0ebc1a` so Release resolves `3.91.1`. Do not edit submodule contents; do not treat `80d12ef5…` or `3.94.0` as approval.
- [ ] Isolated Release restore of `src/Hexalith.FrontComposer.AppHost/Hexalith.FrontComposer.AppHost.csproj` -- every EventStore asset matches the successor hashes; no EventStore project edge.
- [ ] `eng/validate-contract-artifacts.ps1` -- `-RequireProviderVerification` accepts only a success-schema report for the exact runtime, all 19 interactions/states, loopback TCP, cleanup, bounds, and redaction. Reject mere presence.
- [ ] `.github/workflows/quality.yml`, `CiGovernanceTests.cs` -- require that provider report; remove the `BLOCKED_HANDOFF` handoff.
- [ ] AppHost smoke on existing `Program.cs` topology -- health, command submit/status, query provenance, projection SignalR; no adapter/rollback/topology/container redesign.
- [ ] Governance test for the I/O matrix -- incomplete/retired identity or unmet provider report leaves pointers unchanged; only the bound tuple is accepted.

**Acceptance Criteria:**
- Given successor receipts exist but EventStore still lacks a real loopback provider report for all 19 interactions, when validation starts, then Story 11.24 remains backlog and no dependency pointer changes.
- Given incomplete, retired, or non-authorizing identity evidence — including workspace `80d12ef5…` / catalog `3.94.0` or Story 1.20 proofs — when validation starts, then no gitlink, catalog, or code mutation occurs.
- Given both prerequisites and the bound successor, when Debug and isolated Release restore run, then checkout and every EventStore package match `bb94d93e9b84132cff83a38fba84f25455820d31` / `3.91.1` hashes and Release has no EventStore project edge.
- Given the committed pacts, when the EventStore-owned provider lane runs over loopback TCP, then every deterministic state passes and `validate-contract-artifacts.ps1 -RequireProviderVerification` accepts only the exact bounded report.
- Given aligned identities, when Governance, default tests, explicit AppHost builds, and live Aspire smoke run, then they pass without adapter, rollback, topology, or container redesign.

## Spec Change Log

- 2026-08-14: Fail-closed gate evaluated. Successor receipts still authorize `bb94d93…` / `3.91.1`. EventStore's latest 19-interaction loopback report is `finalVerdict: failed` (`contract.interaction-failed` plus identity mismatch). No gitlink, catalog, or code mutation. Sprint-status left `backlog`.

## Design Notes

2026-08-10: user chose remain-backlog until a retrievable owner identity and EventStore-owned provider/pact reconciliation both exist.

2026-08-12: successor receipts authorize `bb94d93…` / `3.91.1`. That owner record is the authority — not catalog presence. Frozen Never still forbids an unreceipted `3.91.1` or later `3.94.0`. Do not inherit Tenants 2.12.

2026-08-14: workspace is `80d12ef5…` / `3.94.0`. No EventStore `Category=ContractProvider` lane at that commit; Gate 2c still passes with `BLOCKED_HANDOFF`. Do not retarget pointers until the provider gate passes. Story 11.23 is done and shares no identity continuity.

2026-08-14 gate: receipts still accept subject `9d074dfd…` / source `bb94d93…` / `3.91.1`. EventStore harness evidence at `references/Hexalith.EventStore/_bmad-output/implementation-artifacts/evidence/frontcomposer-story-11-24/provider-verification/provider-verification.json` is a complete loopback run (`19/19` results, cleanup succeeded) with `finalVerdict: failed` — 16 `interaction.contract-failed`, 3 passed, plus `identity.source/version/builds.mismatch` against observed `47afe55…` / `3.93.0`. Pact drift is not reconciled. Pointers unchanged.

## Verification

**Commands:**
- Confirm successor `final_decision: available`, both receipts, and an EventStore-owned 19-interaction provider report. If either fails: no FrontComposer mutations.
- `git ls-tree HEAD references/Hexalith.EventStore && git -C references/Hexalith.EventStore rev-parse HEAD` -- after unblocked adoption both equal `bb94d93e9b84132cff83a38fba84f25455820d31`.
- `runtime_packages="$(mktemp -d)" && dotnet restore src/Hexalith.FrontComposer.AppHost/Hexalith.FrontComposer.AppHost.csproj --configuration Release --packages "$runtime_packages"` -- approved `3.91.1` hashes only; no EventStore project assets.
- `DiffEngine_Disabled=true dotnet test Hexalith.FrontComposer.slnx --configuration Release --filter "Category=Governance"` -- identity and contract gates pass.
- `pwsh ./eng/validate-contract-artifacts.ps1 -RequireProviderVerification` -- exact-runtime provider verification for all committed interactions.
