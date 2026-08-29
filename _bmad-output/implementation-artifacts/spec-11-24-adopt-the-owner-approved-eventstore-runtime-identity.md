---
title: 'Story 11.24: Adopt the Owner-Approved EventStore Runtime Identity'
type: 'refactor'
created: '2026-08-10'
updated: '2026-08-29'
status: done
baseline_commit: '2e556e8e1d622a10a4e43bb104e211af312fc826'
baseline_revision: '2e556e8e1d622a10a4e43bb104e211af312fc826'
review_loop_iteration: 0
followup_review_recommended: true
deferred:
  - summary: >-
      Reconcile the preserved EventStore provider and live AppHost compatibility failures in separately approved pact/API work.
    evidence: |-
      The complete 19-interaction provider report truthfully records 16 contract failures and a runtime identity mismatch, while the AppHost smoke records failed runtime observations. The frozen intent explicitly makes these outcomes non-authorizing and routes reconciliation to separate work.
    location: >-
      _bmad-output/implementation-artifacts/evidence/frontcomposer-story-11-24/
    severity: high
  - summary: >-
      Repair the pre-existing AppHost Release source/package selection gap for the Parties and Tenants UI modules.
    evidence: |-
      A Release AppHost build probe completed restore but failed compilation with 23 missing Parties/Tenants UI symbols because FrontComposerUiUsePublishedModulePackages defaults to false while Release source references are disabled. The isolated Story 11.24 EventStore restore remained valid and resolved only Hexalith.EventStore.Aspire 3.91.1.
    location: >-
      src/Hexalith.FrontComposer.AppHost/Hexalith.FrontComposer.AppHost.csproj
    severity: high
  - summary: >-
      Regain the newer Builds catalog once a newer EventStore runtime identity is owner-approved, and re-assess the package pins this story had to move backwards.
    evidence: |-
      Selecting the owner-approved Builds catalog `a8a50859fa2f27f511a9470dfe1e3ae54d0ebc1a` is required by the frozen intent, but it moves `eng/dependency-graph-policy.json` backwards from the previously selected `449d3643`: ModelContextProtocol.AspNetCore 2.2.0 -> 1.4.1 (major), Verify and Verify.XunitV3 32.0.0 -> 31.27.0, FsCheck.Xunit.v3 3.4.0 -> 3.3.4, Microsoft.Extensions.Localization and System.Collections.Immutable 10.0.11 -> 10.0.10, Microsoft.NET.Test.Sdk 18.9.0 -> 18.8.1, and Fluent UI v5 rc.5 -> rc.4. The Fluent step also regenerated two verified DataGrid snapshots that lost the `display-mode` and `cell-type` attributes and moved `col-justify` onto a class, which is an accessibility-observable DOM change. Nothing in this repository records a forward path back to the newer catalog.
    location: >-
      eng/dependency-graph-policy.json
    severity: high
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
- [x] `_bmad-output/implementation-artifacts/evidence/frontcomposer-story-11-24/` -- before retargeting, preserve byte-identical copies of the successor decision record, bound approval/package evidence, provider report, and run receipt from EventStore; add a hash manifest so governance remains reproducible after the historical gitlink removes those upstream files.
- [x] `references/Hexalith.EventStore`, `eng/dependency-graph-policy.json` -- only after the exact authorization and FrontComposer-owned evidence-snapshot gates, set gitlink/checkout to `bb94d93e9b84132cff83a38fba84f25455820d31` and select Builds `a8a50859fa2f27f511a9470dfe1e3ae54d0ebc1a` so Release resolves `3.91.1`. Do not edit submodule contents; do not treat `80d12ef5…` or `3.94.0` as approval.
- [x] Isolated Release restore of `src/Hexalith.FrontComposer.AppHost/Hexalith.FrontComposer.AppHost.csproj` -- every EventStore asset matches the successor hashes; no EventStore project edge.
- [x] `eng/validate-contract-artifacts.ps1` -- `-RequireProviderVerification` accepts only the FrontComposer-owned, hash-bound report with all 19 interactions/states accounted for, loopback TCP, cleanup, bounds, and redaction. Preserve its failed verdict and mismatch reason codes, but do not require `finalVerdict: passed`, `runtimeMatches: true`, or passing interactions for migration authorization.
- [x] `.github/workflows/quality.yml`, `CiGovernanceTests.cs` -- require the hash-bound, complete provider evidence without converting compatibility failures into an identity-adoption failure; remove the `BLOCKED_HANDOFF` handoff.
- [x] AppHost smoke on existing `Program.cs` topology -- record health, command submit/status, query provenance, and projection SignalR outcomes as non-authorizing compatibility evidence; route failures separately and do not redesign adapters, rollback, topology, or container ownership in this story.
- [x] Governance test for the I/O matrix -- incomplete/retired identity or incomplete/unsafe provider evidence leaves pointers unchanged; a complete truthful report's runtime/interaction failures do not revoke the separately authorized bound tuple.

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
- `runtime_packages="$(mktemp -d)" && dotnet restore src/Hexalith.FrontComposer.AppHost/Hexalith.FrontComposer.AppHost.csproj -p:Configuration=Release --packages "$runtime_packages"` -- approved `3.91.1` hashes only; no EventStore project assets.
- `DiffEngine_Disabled=true dotnet test Hexalith.FrontComposer.slnx --configuration Release --filter "Category=Governance"` -- identity and contract gates pass.
- `pwsh ./eng/validate-contract-artifacts.ps1 -RequireProviderVerification` -- hash-bound, complete provider evidence for all committed interactions; compatibility verdict and reason codes remain truthful and non-authorizing.

## File List

- `.gitattributes`
- `.github/workflows/quality.yml`
- `_bmad-output/contracts/analyzer-policy-exception-ledger-v1.json`
- `_bmad-output/implementation-artifacts/spec-11-24-adopt-the-owner-approved-eventstore-runtime-identity.md`
- `_bmad-output/implementation-artifacts/evidence/frontcomposer-story-11-24/apphost-smoke/apphost-smoke.json`
- `_bmad-output/implementation-artifacts/evidence/frontcomposer-story-11-24/bb94d93e9b84132cff83a38fba84f25455820d31/acceptances/9d074dfd0758a8934f122aab18659627dff1cf5d4c3e548b222cc0d79a881065/eventstore-owner.json`
- `_bmad-output/implementation-artifacts/evidence/frontcomposer-story-11-24/bb94d93e9b84132cff83a38fba84f25455820d31/acceptances/9d074dfd0758a8934f122aab18659627dff1cf5d4c3e548b222cc0d79a881065/release-owner.json`
- `_bmad-output/implementation-artifacts/evidence/frontcomposer-story-11-24/bb94d93e9b84132cff83a38fba84f25455820d31/nuget-sha256.txt`
- `_bmad-output/implementation-artifacts/evidence/frontcomposer-story-11-24/bb94d93e9b84132cff83a38fba84f25455820d31/owner-actions.md`
- `_bmad-output/implementation-artifacts/evidence/frontcomposer-story-11-24/bb94d93e9b84132cff83a38fba84f25455820d31/package-manifest.json`
- `_bmad-output/implementation-artifacts/evidence/frontcomposer-story-11-24/bb94d93e9b84132cff83a38fba84f25455820d31/release-catalog-provenance.json`
- `_bmad-output/implementation-artifacts/evidence/frontcomposer-story-11-24/bb94d93e9b84132cff83a38fba84f25455820d31/restore-receipt.json`
- `_bmad-output/implementation-artifacts/evidence/frontcomposer-story-11-24/bb94d93e9b84132cff83a38fba84f25455820d31/review-subject.json`
- `_bmad-output/implementation-artifacts/evidence/frontcomposer-story-11-24/bb94d93e9b84132cff83a38fba84f25455820d31/reviewer-roster.json`
- `_bmad-output/implementation-artifacts/evidence/frontcomposer-story-11-24/frontcomposer-11-24-runtime-identity-successor.md`
- `_bmad-output/implementation-artifacts/evidence/frontcomposer-story-11-24/provider-verification/provider-verification.json`
- `_bmad-output/implementation-artifacts/evidence/frontcomposer-story-11-24/provider-verification/run-evidence.json`
- `_bmad-output/implementation-artifacts/evidence/frontcomposer-story-11-24/release-restore/release-restore.json`
- `_bmad-output/implementation-artifacts/evidence/frontcomposer-story-11-24/sha256-manifest.json`
- `docs/reference/pact-contracts.md`
- `eng/dependency-graph-policy.json`
- `eng/eventstore_runtime_evidence.py`
- `eng/validate-contract-artifacts.ps1`
- `references/Hexalith.Builds`
- `references/Hexalith.EventStore`
- `tests/Hexalith.FrontComposer.Contracts.UI.Tests/PackageBoundaryTests.cs`
- `tests/Hexalith.FrontComposer.Shell.Tests/Generated/Epic9CompositionTests.cs`
- `tests/Hexalith.FrontComposer.Shell.Tests/Generated/CounterStoryVerificationTests.cs`
- `tests/Hexalith.FrontComposer.Shell.Tests/Generated/CounterStoryVerificationTests.CounterProjectionView_LoadedState_RendersColumnsAndFormatting.verified.txt`
- `tests/Hexalith.FrontComposer.Shell.Tests/Generated/CounterStoryVerificationTests.StatusProjectionView_NullAndBooleanValues_RenderSnapshot.verified.txt`
- `tests/Hexalith.FrontComposer.Shell.Tests/Governance/CiGovernanceTests.cs`
- `tests/Hexalith.FrontComposer.Shell.Tests/Governance/FluentConformanceTests.cs`
- `tests/Hexalith.FrontComposer.Shell.Tests/Pact/EventStorePactContractTests.cs`
- `tests/Hexalith.FrontComposer.Shell.Tests/Pact/provider-verification-handoff.md`
- `tests/Hexalith.FrontComposer.Shell.Tests/State/PendingCommands/PendingCommandOutcomeResolverTests.cs`
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Integration/PackagedAnalyzerConsumerTests.cs`
- `tests/Hexalith.FrontComposer.Testing.Tests/PackageBoundaryTests.cs`
- `tests/eng/test_eventstore_runtime_evidence.py`

## Documented Unrelated Workspace State

- `_bmad-output/implementation-artifacts/sprint-status.yaml` - Orchestrator-owned bookkeeping; this workflow neither owns nor commits it.
- `_bmad-output/implementation-artifacts/deferred-work.md` - Orchestrator-owned deferred-work ledger; the orchestrator owns entry status and resolution, so this workflow neither edits nor commits it.

## Review Triage Log

### 2026-08-29 — Review pass (follow-up)

- intent_gap: 0
- bad_spec: 0
- patch: 10: (high 3, medium 7, low 0)
- defer: 1: (high 1, medium 0, low 0)
- reject: 12: (high 1, medium 6, low 5)
- addressed_findings:
  - `high` `patch` Made Gate 2c fail closed on the evidence suite: under `shell: pwsh` a red `test_eventstore_runtime_evidence.py` did not fail the step, and the validator that follows reset `$LASTEXITCODE`, so the fail-closed suite's only CI execution site could not fail the job.
  - `high` `patch` Restored failure diagnostics: the `if: success()` upload suppressed `contract-validation-errors.txt`, `redaction-scan.txt`, and `stale-pact-check.txt` exactly when a rejected evidence tree needs triage. Added an always-run diagnostics upload that never carries the evidence tree, keeping the evidence upload success-gated.
  - `high` `patch` Made the preserved successor decision record byte-identical to the EventStore-owned capture. Seven markdown link targets had been rewritten, contradicting the frozen "byte-identical" constraint, and the validator encoded the divergence as a hard-coded exception; the record now restores capture bytes `69b08aba…`, the manifest binds them, and byte-identity is asserted for it and for `owner-actions.md`.
  - `medium` `patch` Stopped the CI job summary reporting `Provider verification: NOT_REQUIRED` for a required-and-rejected lane; it now reports `REQUIRED_REJECTED`.
  - `medium` `patch` Resolved `-ProviderVerificationReport` against the repository root instead of the caller's working directory, so a correct relative path is accepted from anywhere.
  - `medium` `patch` Updated `provider-verification-handoff.md` and its generator, which still declared the release blocked pending a provider run and pinned a superseded command shape contradicting both the preserved run receipt and `docs/reference/pact-contracts.md`.
  - `medium` `patch` Restored the published NFR55 release rule that the documentation change had deleted with no replacement, alongside the story-scoped non-authorizing clarification.
  - `medium` `patch` Documented the two evidence hash domains (exact preserved bytes versus CRLF-normalized live pact text) and the re-capture procedure for the deliberate live-byte binding of pacts and AppHost sources.
  - `medium` `patch` Widened provider-report duration validation to a one-millisecond tolerance so a truthful producer that rounds a sub-millisecond remainder is not rejected.
  - `medium` `patch` Asserted `inputHashes[].kind` for the identity inputs, which were parsed but never checked, so approval evidence cannot be relabeled as a contract input.

### 2026-08-29 — Review pass

- intent_gap: 0
- bad_spec: 0
- patch: 17: (high 14, medium 3, low 0)
- defer: 2: (high 2, medium 0, low 0)
- reject: 4: (high 1, medium 3, low 0)
- addressed_findings:
  - `high` `patch` Made the evidence tree byte-stable across Git checkouts with a path-scoped `-text` rule.
  - `high` `patch` Made the SHA-256 manifest exhaustive, bounded, and inclusive of AppHost smoke and Release restore evidence.
  - `high` `patch` Bound the frozen review subject's declared evidence and exact capture-source commit to the preserved manifest.
  - `high` `patch` Reconciled provider contract-input hashes with the current committed pact inputs while preserving the historical report bytes.
  - `high` `patch` Required exact provider-state coverage and pact-file attribution for every interaction.
  - `high` `patch` Enforced complete receipt fields, exact durable sources, timezone-aware chronology, and safe timestamp failures.
  - `high` `patch` Enforced the exact 14-package identity/hash/signature inventory and exact consumer counts.
  - `high` `patch` Bound the isolated Release restore command, configuration, assets, and approved package bytes.
  - `high` `patch` Rejected duplicate JSON keys, symlinked evidence, undeclared files, oversized inputs, and scalar type coercion.
  - `high` `patch` Made timing, verdict, reason-code, and run-receipt validation outcome-parametric and internally consistent.
  - `high` `patch` Closed the 64-hex secret redaction bypass with field-scoped hash recognition and mutation coverage.
  - `high` `patch` Prevented rejected or unsafe evidence from being uploaded by the quality workflow.
  - `high` `patch` Added root committed-gitlink versus checked-out-HEAD governance for EventStore and Builds.
  - `medium` `patch` Corrected relocated successor-record links while retaining the original report input hash.
  - `medium` `patch` Corrected the executable Release restore command and truthfully retained the rejected invalid-switch attempt.
  - `medium` `patch` Reconciled catalog-induced Fluent, localization, test SDK, analyzer-ledger, and verified-snapshot drift.
  - `high` `patch` Strengthened the invalid/retired evidence matrix to prove dependency bytes and checkouts remain unchanged.


### 2026-08-29 — Review pass (follow-up 2)

- intent_gap: 0
- bad_spec: 0
- patch: 9: (high 2, medium 6, low 1)
- defer: 0
- reject: 23: (high 2, medium 11, low 10)
- addressed_findings:
  - `high` `patch` Pinned every EventStore-captured evidence file by exact digest. Only 2 of the 12 captured files were pinned, so `sha256-manifest.json` was the sole authority for the other 10 — including the provider report. Two existing tests demonstrated the hole: they rewrote the report to an all-passing, `runtimeMatches: true` verdict, re-sealed the manifest, and validated with zero errors, which is exactly the "relabel a failed report as passing" the frozen Never forbids. `CAPTURED_EVIDENCE_SHA256` now pins all twelve; the two structural tests re-pin explicitly so outcome-parametric coverage survives, and a new test proves the un-repinned forgery is rejected.
  - `high` `patch` Covered the `-RequireProviderVerification` branch of `validate-contract-artifacts.ps1`, which executed in no test at all — its owned-report path check, its validator exit-code propagation, and its status reporting could all have been inverted or deleted with every lane green. Added three pwsh-subprocess tests: canonical acceptance from an unrelated working directory, rejection of a report outside the owned evidence tree, and propagation of an evidence-validator failure.
  - `medium` `patch` Stopped the job summary reporting `COMPLETE_HASH_BOUND_REPORT` when the provider report failed its redaction scan; the lane failed but the summary called the report complete, the same mis-summarization the previous pass added `REQUIRED_REJECTED` to prevent.
  - `medium` `patch` Completed the previous pass's repository-root path fix. `-PactDir` and every pact read still resolved against the caller's working directory, so the documented command only worked from the repository root; the script is now genuinely directory-independent, and a test asserts it.
  - `medium` `patch` Made the fail-closed propagation in Gate 2c actually fail closed. `$LASTEXITCODE` is `$null` until a native command sets it, and `exit $null` exits 0, so a suite that never launched would have exited the step green; the guard now exits 1 and `CiGovernanceTests` pins the safe form instead of the unsafe literal.
  - `medium` `patch` Brought the evidence-tree redaction scanner to parity with the sibling `Find-RedactionLeaks`, which only ever sees the pacts and the provider report. Connection strings, cookies, authorization payloads, raw `Authorization` headers, and environment-shaped secrets were rejected in a pact but accepted in the other 13 preserved evidence files.
  - `medium` `patch` Made `sha256-manifest.json` truthful about provenance. A single `capturedFromEventStoreCommit` covered all 14 files, but the AppHost smoke and Release restore evidence were produced by this repository after that commit; each entry now declares `eventstore-capture` or `frontcomposer-run` and the validator enforces it.
  - `medium` `patch` Required an AppHost observation recorded as `not-observed` to carry no response code, so a real observation cannot be relabelled as one the run never reached.
  - `low` `patch` Removed three UTF-8 BOMs this change had introduced into `eng/validate-contract-artifacts.ps1`, `CiGovernanceTests.cs`, and `EventStorePactContractTests.cs`; `.editorconfig` declares `charset = utf-8` and 309 of 311 sibling test files carry none.

## Auto Run Result

### Summary

Second follow-up review pass over the committed Story 11.24 change. The owner-approved runtime identity adoption is untouched: the EventStore gitlink and checkout are both `bb94d93e9b84132cff83a38fba84f25455820d31`, Release resolves `3.91.1` through Builds catalog `a8a50859fa2f27f511a9470dfe1e3ae54d0ebc1a`, and the provider report's `failed` verdict and runtime mismatch remain preserved and non-authorizing. Nine findings were patched; the two high ones both concern the gate rather than the identity — the preserved provider report was forgeable in-repo, and the required-provider branch of the contract validator was executed by no test.

### Files changed

- `.github/workflows/quality.yml` — the evidence-suite propagation exits 1 rather than re-using a possibly-`$null` `$LASTEXITCODE`.
- `_bmad-output/implementation-artifacts/evidence/frontcomposer-story-11-24/sha256-manifest.json` — each entry now declares per-file `provenance`.
- `docs/reference/pact-contracts.md` — documents the per-file provenance, the capture pins, and that changing a pin is the deliberate act of replacing owner-captured bytes.
- `eng/eventstore_runtime_evidence.py` — pins all twelve captured files by digest, enforces manifest provenance, scans evidence at parity with the PowerShell leak rules, and constrains `not-observed` AppHost outcomes.
- `eng/validate-contract-artifacts.ps1` — resolves `-PactDir` against the repository root, refuses to call a leaking report complete, and drops its BOM.
- `tests/Hexalith.FrontComposer.Shell.Tests/Governance/CiGovernanceTests.cs` — pins the safe propagation form and rejects the unsafe one; BOM dropped.
- `tests/Hexalith.FrontComposer.Shell.Tests/Pact/EventStorePactContractTests.cs` — BOM dropped.
- `tests/eng/test_eventstore_runtime_evidence.py` — nine new cases covering the capture pin, the pin inventory, manifest provenance, the widened redaction classes, the raw `Authorization` header, unobserved-with-a-status-code, and the three `-RequireProviderVerification` script paths.
- `_bmad-output/implementation-artifacts/spec-11-24-adopt-the-owner-approved-eventstore-runtime-identity.md` — records this pass's triage and result.

### Review findings breakdown

- Applied 9 patches: 2 high, 6 medium, 1 low.
- Deferred 0. The two open compatibility items and the backwards catalog pins were already recorded in this spec's `deferred` list in earlier passes and are unchanged.
- Rejected 23: findings that target upstream-owned frozen bytes this story must preserve verbatim (the report's `evidenceManifestSha256` field name, the two receipts' identical timestamps, the deliberately broken relative links, the absent upstream post-receipt test run); findings that contradict confirmed project rules (re-coupling `builds-execution-sha` to the deliberately independent Builds gitlink; treating this story's own analyzer-ledger drift as GOV-1-owned); documentation opinions already settled by the 2026-08-26 human decision (NFR55 naming); findings resting on a misreading (the Run block is not self-defeating because pact regeneration is deterministic and the stale-pact gate exists for that; the duplicate upload is the deliberate diagnostics/evidence split); requests to rewrite two other stories' completed `done` records that mention `BLOCKED_HANDOFF` as historical evidence; and scope creep (a cross-language pin file, an outcome-parametric AppHost resource gate, real restore/build/provider execution this story routes to EventStore).

### Follow-up review recommendation

`true` — patched findings: high 2, medium 6, low 1; a high-severity patch was applied, and the weighted medium/low score is `3 × 6 + 1 × 1 = 19`.

### Verification performed

- `python3 -m unittest tests/eng/test_eventstore_runtime_evidence.py` — 40 passed (31 pre-existing plus 9 new).
- `python3 -m unittest tests/eng/test_dependency_graph.py` — 99 passed.
- `python3 eng/eventstore_runtime_evidence.py --evidence-root _bmad-output/implementation-artifacts/evidence/frontcomposer-story-11-24 --pact-dir tests/Hexalith.FrontComposer.Shell.Tests/Pact` — passed.
- `pwsh ./eng/validate-contract-artifacts.ps1 -RequireProviderVerification` — passed, job summary `COMPLETE_HASH_BOUND_REPORT`; re-run from `/tmp` with a repository-relative report argument, also passed.
- `git ls-tree HEAD -- references/Hexalith.EventStore references/Hexalith.Builds` and `git -C … rev-parse HEAD` — gitlink equals checkout for both: `bb94d93e…` and `a8a50859…`.
- Shell governance lane (`Category=Governance`, direct xUnit v3 runner) — 227/227 passed.
- Full Shell default lane (`-notrait Category=Performance -notrait Category=e2e-palette -notrait Category=NightlyProperty -notrait Category=Quarantined`) — 2,688/2,688 passed; no pact drift after the run.
- `runtime_packages="$(mktemp -d)" && dotnet restore src/Hexalith.FrontComposer.AppHost/Hexalith.FrontComposer.AppHost.csproj -p:Configuration=Release --packages "$runtime_packages"` — restored; the sole EventStore asset is `hexalith.eventstore.aspire`.
- `dotnet build tests/Hexalith.FrontComposer.Shell.Tests/… --configuration Release -m:1` — clean, 0 warnings under `TreatWarningsAsErrors`.
- `pwsh ./eng/validate-docs.ps1` — passed.
- `python3 eng/validate-story-artifacts.py --story … --candidate HEAD` — passed.

### Residual risks

- The preserved provider and AppHost observations still record real compatibility drift; this pass hardened how that evidence is protected, not the drift itself, and the deferred pact/API reconciliation remains necessary.
- Gate 2c stays deliberately bound to live pact and AppHost bytes, and the captured files are now also pinned by constant. Any ordinary edit to those files fails the lane until the evidence is re-captured upstream and the pin is deliberately moved. That is the intended cost of the byte-identity constraint, and it is documented.
- The intent-mandated Builds catalog still leaves seven package pins on older versions and the two Fluent DataGrid snapshots without their `display-mode` / `cell-type` attributes; the forward path stays deferred.
- The pre-existing AppHost Release UI source/package configuration still prevents a full Release AppHost compilation.
