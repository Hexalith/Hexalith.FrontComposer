---
title: 'Story 11.24: Adopt the Owner-Approved EventStore Runtime Identity'
type: 'refactor'
created: '2026-08-10'
updated: '2026-08-26'
status: ready-for-dev
baseline_commit: '9a3d14b8460ff05ea74d7adbba1547ea9d1ba0b0'
baseline_revision: '25cd54bd502b933900fceeb439ee7f6238c44553'
review_loop_iteration: 0
decision: 'adopt-owner-approved-identity'
context:
  - '{project-root}/_bmad-output/implementation-artifacts/epic-11-context.md'
  - '{project-root}/references/Hexalith.EventStore/_bmad-output/implementation-artifacts/frontcomposer-11-24-runtime-identity-successor.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Debug currently consumes EventStore source commit `5bcfdbc8b28ac2706053075cc4e71160ee029ad8`, while Release resolves `3.91.1` packages built from `bb94d93e9b84132cff83a38fba84f25455820d31`. Story 1.20 approved `fa2d1c9910f8976553adb33dcdb1c9ff2ea75594` plus `999.1.20-proof.fa2d1c9910f8`, but all 14 approved package archives are documented as unrecoverable and no FrontComposer waiver exists.

**Approach:** Adopt the exact retrievable identity authorized by the EventStore and Release owners. Require a complete, bounded EventStore-owned loopback report as truthful compatibility evidence, but do not make its verdict, observed current-runtime identity, or interaction outcomes a migration-authorization gate; preserve failures and route pact/API reconciliation to separately approved work.

## Boundaries & Constraints

**Always:** Require a 40-hex approved source SHA, published package version and SHA-256 inventory, named approvals, and an already-selected Builds catalog commit. Before retargeting the EventStore gitlink, preserve byte-identical, hash-bound copies of the successor record, its approval/package evidence, and the complete provider report under FrontComposer-owned evidence because the approved historical commit does not contain them. Verify the root gitlink equals the checkout; restore Release from an isolated cache; reject EventStore project assets in Release; use real loopback TCP with deterministic provider-state cleanup; keep evidence bounded and redaction-clean.

**Ask First:** Any proposal to replace the upstream prerequisites with a FrontComposer waiver, move provider ownership into FrontComposer, or expand this identity-only story into pact/API migration work.

**Never:** Treat current `3.91.1`, `5bcfdbc8...`, ancestry, catalog presence, or the report's observed runtime as approval. Edit EventStore submodule contents, initialize nested submodules, accept an incomplete or unsafe provider report, relabel a failed report as passing, or redesign adapters, rollback, Aspire topology, or EventStore container ownership.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|---------------|----------------------------|----------------|
| Authorized identity | Complete owner record; matching source, catalog, package bytes | Debug and Release resolve the same approved runtime; evidence records exact identities | Fail on any missing/mismatched field or asset |
| Retired identity | Approved version exists only in historical documentation | No gitlink, catalog, or code mutation | Report package retrieval/authority blocker |
| Provider drift | A complete, safely bounded 19-interaction report records current-runtime identity or contract failures | Preserve the failed verdict; the separately authorized exact tuple remains eligible for adoption | Reject incomplete/unsafe evidence; route behavioral reconciliation to separately approved work |

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
- [x] Successor packet + EventStore provider lane -- authorization/evidence gate: both owner receipts authorize the bound tuple, and the real loopback report accounts for all 19 committed interactions with complete cleanup and safe bounds. Preserve its failed verdict and drift reason codes as non-authorizing compatibility evidence. If authorization fails or the report is incomplete/unsafe, mutate nothing.
- [ ] `_bmad-output/implementation-artifacts/evidence/frontcomposer-story-11-24/` -- before retargeting, preserve byte-identical copies of the successor decision record, bound approval/package evidence, provider report, and run receipt from EventStore; add a hash manifest so governance remains reproducible after the historical gitlink removes those upstream files.
- [ ] `references/Hexalith.EventStore`, `eng/dependency-graph-policy.json` -- only after the exact authorization and FrontComposer-owned evidence-snapshot gates, set gitlink/checkout to `bb94d93e9b84132cff83a38fba84f25455820d31` and select Builds `a8a50859fa2f27f511a9470dfe1e3ae54d0ebc1a` so Release resolves `3.91.1`. Do not edit submodule contents; do not treat `80d12ef5…` or `3.94.0` as approval.
- [ ] Isolated Release restore of `src/Hexalith.FrontComposer.AppHost/Hexalith.FrontComposer.AppHost.csproj` -- every EventStore asset matches the successor hashes; no EventStore project edge.
- [ ] `eng/validate-contract-artifacts.ps1` -- `-RequireProviderVerification` accepts only the FrontComposer-owned, hash-bound report with all 19 interactions/states accounted for, loopback TCP, cleanup, bounds, and redaction. Preserve its failed verdict and mismatch reason codes, but do not require `finalVerdict: passed`, `runtimeMatches: true`, or passing interactions for migration authorization.
- [ ] `.github/workflows/quality.yml`, `CiGovernanceTests.cs` -- require the hash-bound, complete provider evidence without converting compatibility failures into an identity-adoption failure; remove the `BLOCKED_HANDOFF` handoff.
- [ ] AppHost smoke on existing `Program.cs` topology -- record health, command submit/status, query provenance, and projection SignalR outcomes as non-authorizing compatibility evidence; route failures separately and do not redesign adapters, rollback, topology, or container ownership in this story.
- [ ] Governance test for the I/O matrix -- incomplete/retired identity or incomplete/unsafe provider evidence leaves pointers unchanged; a complete truthful report's runtime/interaction failures do not revoke the separately authorized bound tuple.

**Acceptance Criteria:**
- Given successor receipts exist but FrontComposer lacks a complete, hash-bound, safely bounded loopback report accounting for all 19 interactions and cleanup, when validation starts, then no dependency pointer changes.
- Given incomplete, retired, or non-authorizing identity evidence — including workspace `80d12ef5…` / catalog `3.94.0` or Story 1.20 proofs — when validation starts, then no gitlink, catalog, or code mutation occurs.
- Given the complete successor authorization and preserved evidence snapshot, when Debug and isolated Release restore run, then checkout and every EventStore package match `bb94d93e9b84132cff83a38fba84f25455820d31` / `3.91.1` hashes and Release has no EventStore project edge.
- Given the committed pacts, when the EventStore-owned provider lane runs over loopback TCP, then every deterministic state attempt and cleanup event is accounted for, the failed verdict remains truthful, and `validate-contract-artifacts.ps1 -RequireProviderVerification` accepts the bounded evidence without treating compatibility as migration authority.
- Given aligned Debug and Release identities, when Governance, default tests, and explicit AppHost builds run, then identity, restore, and build lanes pass; any live command/query/projection drift is recorded and routed to separately approved work without adapter, rollback, topology, or container redesign.

## Spec Change Log

- 2026-08-26: Human chose to decouple identity migration authorization from provider compatibility. The exact owner-approved `bb94d93…` / `3.91.1` / Builds `a8a50859…` tuple may be adopted with complete, safe, hash-bound provider evidence; the truthful failed verdict, current-runtime mismatch, and interaction failures remain separate reconciliation work.
- 2026-08-14: Fail-closed gate evaluated. Successor receipts still authorize `bb94d93…` / `3.91.1`. EventStore's latest 19-interaction loopback report is `finalVerdict: failed` (`contract.interaction-failed` plus identity mismatch). No gitlink, catalog, or code mutation. Sprint-status left `backlog`.

## Design Notes

2026-08-10: user chose remain-backlog until a retrievable owner identity and EventStore-owned provider/pact reconciliation both exist.

2026-08-12: successor receipts authorize `bb94d93…` / `3.91.1`. That owner record is the authority — not catalog presence. Frozen Never still forbids an unreceipted `3.91.1` or later `3.94.0`. Do not inherit Tenants 2.12.

2026-08-14: workspace is `80d12ef5…` / `3.94.0`. No EventStore `Category=ContractProvider` lane at that commit; Gate 2c still passes with `BLOCKED_HANDOFF`. The then-current decision was not to retarget until the provider gate passed; the 2026-08-26 human decision supersedes that gate. Story 11.23 is done and shares no identity continuity.

2026-08-14 gate: receipts still accept subject `9d074dfd…` / source `bb94d93…` / `3.91.1`. EventStore harness evidence at `references/Hexalith.EventStore/_bmad-output/implementation-artifacts/evidence/frontcomposer-story-11-24/provider-verification/provider-verification.json` is a complete loopback run (`19/19` results, cleanup succeeded) with `finalVerdict: failed` — 16 `interaction.contract-failed`, 3 passed, plus `identity.source/version/builds.mismatch` against observed `47afe55…` / `3.93.0`. Pact drift is not reconciled. Pointers unchanged.

2026-08-26 decision: the successor's two owner receipts are the migration authority. The current-runtime provider report is required as complete, safe evidence but is not an authorization verdict. Because `bb94d93…` predates the successor packet and verifier artifacts, preserve their byte-identical, hash-bound FrontComposer-owned snapshot before retargeting the EventStore gitlink.

## Verification

**Commands:**
- Confirm successor `final_decision: available`, both receipts, and a complete, safely bounded EventStore-owned 19-interaction report; preserve byte-identical, hash-bound FrontComposer-owned copies before retargeting. Missing/unsafe evidence blocks mutation, but the report's truthful failed verdict and current-runtime mismatch do not revoke authorization.
- `git ls-tree HEAD references/Hexalith.EventStore && git -C references/Hexalith.EventStore rev-parse HEAD` -- after unblocked adoption both equal `bb94d93e9b84132cff83a38fba84f25455820d31`.
- `runtime_packages="$(mktemp -d)" && dotnet restore src/Hexalith.FrontComposer.AppHost/Hexalith.FrontComposer.AppHost.csproj --configuration Release --packages "$runtime_packages"` -- approved `3.91.1` hashes only; no EventStore project assets.
- `DiffEngine_Disabled=true dotnet test Hexalith.FrontComposer.slnx --configuration Release --filter "Category=Governance"` -- identity and contract gates pass.
- `pwsh ./eng/validate-contract-artifacts.ps1 -RequireProviderVerification` -- hash-bound, complete provider evidence for all committed interactions; compatibility verdict and reason codes remain truthful and non-authorizing.

