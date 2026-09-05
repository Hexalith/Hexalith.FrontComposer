---
title: 'Finish Pact Provider Reconciliation at Current Provenance'
type: 'bugfix'
created: '2026-09-01'
status: 'done'
baseline_commit: 'a739d2f77daaa369b42518f5998326c117830649'
baseline_revision: 'a739d2f77daaa369b42518f5998326c117830649'
review_loop_iteration: 0
followup_review_recommended: false
context:
  - '{project-root}/_bmad-output/project-context.md'
  - '{project-root}/.bmad-loop/runs/20260831-140103-5800/bundles/pact-provider-reconciliation/intent.md'
  - '{project-root}/_bmad-output/implementation-artifacts/spec-pact-provider-reconciliation.md'
warnings: [oversized]
deferred: []
---

<intent-contract>

## Intent

**Problem:** The consumer adapters, four Pacts, and EventStore-owned verifier now pass all 19 interactions, but FrontComposer's committed live evidence and current-compatibility metadata identify superseded EventStore/Builds revisions. The committed AppHost smoke is failed and a governance test enshrines that failure, so DW-1788/DW-1901 are not resolved at the current repository revision.

**Approach:** Re-capture the successful provider run against the exact current Pacts and gitlinks, restore the distinction between the immutable owner-approved tuple and non-authorizing current compatibility, and require a passing authenticated AppHost smoke. Preserve all reconciled adapter/provider behavior; if the current unavailable proof-package tuple prevents the unchanged topology from starting and cannot be cleared by the already-authorized narrow build-metadata exception, stop truthfully as blocked.

## Boundaries & Constraints

**Always:** Keep the EventStore production API and current 19-interaction Pact contract unchanged; use the EventStore-owned real IPv4 loopback Kestrel verifier; bind current evidence to exact raw Pact/catalog bytes, source/version/Builds provenance, setup/teardown, readiness, redaction, host stop, and port closure. Keep the top-level approved identity tuple historical and owner-authorized while recording current compatibility separately. Preserve repository ownership and report source/submodule work separately.

**Block If:** A passing AppHost run requires changing a package, catalog, gitlink, dependency version, runtime contract, production behavior, declared resource/topology, auth/tenant enforcement, or more than the narrow behavior-neutral UI/AppHost build-input correction previously authorized. Also block after exhausting repository-provided local identities if authenticated command, status, query, SignalR, or readiness observations cannot be captured.

**Never:** Edit `_bmad-output/implementation-artifacts/deferred-work.md`; modify or reseal `_bmad-output/implementation-artifacts/evidence/frontcomposer-story-11-24/**`; fabricate approval or runtime success; accept stale, mock-only, `TestServer`, incomplete, unauthenticated, expected-nonzero, or failed evidence; suppress interactions or weaken stale-Pact, redaction, cleanup, or exit-code gates.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|---------------|---------------------------|----------------|
| Current provider verification | Current four Pacts, 19-state catalog, EventStore `d6b8d2e5c1763713a126ff627822ead738e0f642`, Builds `9d77ed7cb22dc8e5cde8d51b7284b3e9a94cd3b6` | Exit 0; 19/19 interactions and state pairs pass with clean Kestrel teardown; report and receipt bind exact current inputs | Any stale identity/hash, failed interaction/state, leak, incomplete cleanup, or nonzero exit fails Gate 2c |
| Identity contract | Historical approved tuple plus current checked-out compatibility tuple | Approved tuple remains `bb94d93e9b84132cff83a38fba84f25455820d31` / `3.91.1` / `a8a50859fa2f27f511a9470dfe1e3ae54d0ebc1a`; `currentCompatibility` truthfully records current non-authorizing provenance | Current compatibility must never replace or claim the approved tuple |
| Authenticated AppHost smoke | Existing ten-resource topology and repository-provided local identity | All resources become healthy; readiness, command submit/status, query provenance, and projection SignalR pass; cleanup proves zero running AppHosts and closed ports | Unavailable proof packages or missing infrastructure produce a blocking result, never passing evidence |
| Drift/adversarial input | Mutated Pact, stale report, failed smoke, unsafe output, or edited history | Historical and live lanes fail independently and report the relevant rejection | Earlier provenance errors must not mask the intended adversarial diagnostic in focused tests |

</intent-contract>

## Code Map

- `src/Hexalith.FrontComposer.Shell/Infrastructure/EventStore/EventStoreQueryClient.cs:95-288,482-554` and `EventStoreCommandClient.cs:112-181,297-327` -- already-reconciled production adapter behavior; read-only unless a focused test proves a remaining provider-contract defect.
- `tests/Hexalith.FrontComposer.Shell.Tests/Pact/EventStorePactContractTests.cs:55-603` and sibling Pact JSON/manifest/catalog files -- generator and exact current 19-interaction consumer inputs; regeneration must remain byte-stable.
- `references/Hexalith.EventStore/tests/Hexalith.EventStore.ProviderVerification/ProviderVerificationApplication.cs:65-213`, `RuntimeIdentityValidator.cs:222-276`, and `README.md` -- working provider-owned live mode and canonical capture command; no EventStore edit is currently indicated.
- `_bmad-output/implementation-artifacts/evidence/pact-provider-reconciliation/{provider-verification.json,run-evidence.json,apphost-smoke.json}` -- current evidence only; provider files are stale and AppHost evidence is failed.
- `_bmad-output/contracts/frontcomposer-eventstore-approved-runtime-identity-v1.json` -- preserve approved top-level tuple; update only `currentCompatibility` for current provenance.
- `eng/eventstore_runtime_evidence.py:1460-1810` and `tests/eng/test_eventstore_runtime_evidence.py:390-430,845-900` -- exact live provenance/input/evidence validation and three presently failing stale-fixture cases.
- `eng/pact_provider_apphost_smoke.py:29-83,305-510` and `tests/eng/test_pact_provider_apphost_smoke.py` -- ten-resource authenticated capture, bounded probes, and cleanup.
- `tests/Hexalith.FrontComposer.Shell.Tests/Governance/CiGovernanceTests.cs:3416-3535` -- stale identity constants and assertions that currently require failed AppHost evidence.
- `eng/validate-contract-artifacts.ps1:217-293` and `.github/workflows/quality.yml:177-245` -- fail-closed historical/live Gate 2c orchestration; preserve separate statuses, nonzero propagation, stale-diff, and success-only upload.
- `_bmad-output/implementation-artifacts/evidence/frontcomposer-story-11-24/**` and `_bmad-output/implementation-artifacts/deferred-work.md` -- immutable/read-only.

## Tasks & Acceptance

**Execution:**
- [x] `_bmad-output/implementation-artifacts/evidence/pact-provider-reconciliation/{provider-verification.json,run-evidence.json}` -- replace stale live files with the fresh EventStore-owned 19/19 run and atomically generated receipt bound to current raw inputs.
- [x] `_bmad-output/contracts/frontcomposer-eventstore-approved-runtime-identity-v1.json` and `tests/Hexalith.FrontComposer.Shell.Tests/Governance/CiGovernanceTests.cs` -- restore the approved/current identity separation and make governance require truthful current evidence rather than the committed failed smoke.
- [x] `tests/eng/test_eventstore_runtime_evidence.py` -- keep canonical success and adversarial tests stable at the current provenance so Pact/AppHost failures are not masked by unrelated stale identity errors.
- [x] `eng/pact_provider_apphost_smoke.py` and `_bmad-output/implementation-artifacts/evidence/pact-provider-reconciliation/apphost-smoke.json` -- run the unchanged topology with bounded authenticated probes; change code/metadata only for a proven in-scope defect, and block on the current unavailable proof-package tuple if resolution would cross the boundary.
- [x] `eng/validate-contract-artifacts.ps1`, `.github/workflows/quality.yml`, and `docs/reference/pact-contracts.md` -- retain or tighten the already-separated fail-closed lanes and document the exact current commands/outcomes; do not weaken acceptance to accommodate a failed smoke.

**Acceptance Criteria:**
- Given the current Pacts and provider checkout, when the live EventStore verifier and receipt writer run, then the owned report passes all 19 production-pipeline interactions and binds current source/version/Builds plus every exact Pact/manifest/catalog byte.
- Given approved historical identity and current compatibility are different concepts, when governance reads the identity contract, then the approved tuple remains owner-authorized history and current provenance is truthful, non-authorizing, and independently freshness-checked.
- Given Gate 2c runs at the candidate revision, when provider/AppHost evidence is stale, failed, unsafe, incomplete, unauthenticated, or nonzero, then the job fails and uploads no success evidence; when both live surfaces pass, historical integrity is still validated separately.
- Given the current AppHost dependency graph requests unavailable `999.1.20-proof.fa2d1c9910f8` packages, when the narrow authorized repair boundary is evaluated, then no package/catalog/gitlink/dependency identity is changed and the run blocks unless the unchanged repository inputs can start and complete every authenticated observation.

## Spec Change Log

## Review Triage Log

- BH1 spec still describes failed smoke / proof packages — `false` (rejected): the required fix is to edit this build's spec; current HEAD gitlinks and the passing capture are the candidate revision the intent asked to bind.
- BH2 `_validate_live_apphost` accepts any non-empty observation `reasonCode` and never pins `queryProvenance.provenance` — `medium` (patch): `eng/eventstore_runtime_evidence.py:1757-1764` only checks `result` / `authenticated` / non-empty `reasonCode`, so a drifted stamp still validates.
- BH3 `CiGovernanceTests` does not pin health/command/cleanup — `low` (rejected): Gate 2c already runs `_validate_live_apphost`, which requires every observation `result=passed` and clean cleanup; extra C# pins would not change everyday CI.
- BH4 combined provider+smoke failure fixture — `false` (rejected): `test_live_lane_rejects_failed_provider_or_apphost_evidence` asserts both independent diagnostics, and `validate_live` collects both error lists without short-circuit.
- BH5 `json_request` swallows non-JSON HTTP 200 — `false` (rejected): EventStore `/health` is status `200`/`204` text/plain; token and command paths still require JSON fields (`access_token`, `correlationId`).
- BH6 describe stdout still tail-truncated at 1 MiB — `low` (rejected): current ten-resource describe is far under the new cap; fail-closed truncation would add branches the capture did not need.
- BH7 cleanup `_port_open` uses `_resource_endpoint` HTTPS URLs while probes use HTTP loopback — `medium` (patch): `eng/pact_provider_apphost_smoke.py:452-454` vs `:618`.
- BH8 SignalR handshake does not wait for a projection payload — `false` (rejected): intent requires an authenticated projection-hub connection, not a topology change to EventStore.Sample or an event wait.
- BH9 `connectionToken` fallback to `connectionId` — `maybe-false` (rejected): would only be `low`; `negotiateVersion=1` still prefers `connectionToken` when present.
- BH10 pre-stop is `SmokeRuntime` + `sleep(8)` rather than a stopped/ports-closed wait — `medium` (patch): `eng/pact_provider_apphost_smoke.py:419-424` can still start against a half-stopped tree.
- BH11 drop-published matcher is slash- and case-sensitive and filename-allow-listed — `medium` (patch): `DropPublishedFrontComposerAssemblies.targets:17-31`.
- BH12 docs still say Gate 2c uses only start/wait/describe/stop — `medium` (patch): `docs/reference/pact-contracts.md:49` omits cold stop, HTTP-preferred probes, and the UI FromSource drop-published exception this capture depends on.
- BH13 `.gitattributes` says receipts hash every live-evidence file — `low` (rejected): cosmetic comment; `-text` is still required so `provider-verification.json` receipt SHA-256 is not rewritten.
- EC1 describe truncation nested JSON — `low` (rejected): same defect as BH6; 1 MiB cap covers the current topology.
- EC2 pre-stop race after 8s — `medium` (patch): same defect as BH10.
- EC3 token loop can overrun the 30s deadline by one 15s request — `low` (rejected): Keycloak answers quickly here; the 300s smoke budget still bounds the run.
- EC4 health/alive has no extra deadline — `low` (rejected): request count is finite and each call already has `urlopen` timeout.
- EC5 WebSocket `recv` can block the full socket timeout — `low` (rejected): SignalR connect timeout is 5s per URL.
- EC6 `://localhost` substring rewrite — `low` (rejected): Aspire advertised URLs use `localhost`/`127.0.0.1` as the hostname, not a prefix of another host.
- EC7 `_resource_endpoint` DFS into internal URLs when `urls` exists but public is empty — `medium` (patch): after the public-url filter fails, DFS still walks the record (`:229-239`).
- EC8 `json_request` does not catch `IncompleteRead` / `OSError` / `HTTPException` — `medium` (patch): those escape `_capture` instead of trying the next base (`:117-118`).
- EC9 HTTP probe redirects to hanging HTTPS — `false` (rejected): the passing capture used plaintext HTTP successfully; a redirect-to-hang was not shown.
- EC10 cleanup reports clean while HTTP probe ports remain — `medium` (patch): same defect as BH7.
- EC11 first healthy EventStore base is reused for command/query — `maybe-false` (defer, would be `medium` if true): live capture succeeded on that base; a later-call mismatch was not shown. Settle by failing command on the health-selected base while another advertised URL would succeed.
- EC12 SignalR tries URLs serially — `low` (rejected): per-URL timeout is 5s and the smoke budget is 300s.
- EC13 `StaticWebAssetEndpoint` Identity may be a route, so nuget-path Contains misses — `maybe-false` (defer, would be `medium` if true): this host's AppHost smoke compiled and waited `frontcomposer-ui` healthy. Settle by inspecting a failing `GenerateStaticWebAssetsDevelopmentManifest` item Identity on Windows or a nested portal.
- EC14 backslash / custom `NUGET_PACKAGES` — `medium` (patch): same defect as BH11.
- EC15 drop allow-list misses unlisted FrontComposer filenames — `medium` (patch): same defect as BH11.
- VG1 capture never asserts HTTP-first / multi-base fallback — `medium` (patch, pre-verified): FakeRuntime success path only sees `https://*.invalid:443` and never fails a request.
- VG2 non-JSON HTTP 200 now counts as health success — `false` (rejected): same as BH5.

## Design Notes

The provider verification problem is already solved at the provider and consumer wire surfaces. This pass is evidence reconciliation at the exact candidate revision: refreshing current evidence is legitimate; rewriting the historical approval tuple or treating compatibility as approval is not.

## Verification

**Commands:**
- `dotnet build references/Hexalith.EventStore/tests/Hexalith.EventStore.ProviderVerification.Tests/Hexalith.EventStore.ProviderVerification.Tests.csproj --configuration Release -m:1` followed by its built test DLL -- expected: zero warnings/errors and 77/77 tests pass.
- `dotnet references/Hexalith.EventStore/tests/Hexalith.EventStore.ProviderVerification/bin/Release/net10.0/Hexalith.EventStore.ProviderVerification.dll --verification-mode live-compatibility --pact-directory tests/Hexalith.FrontComposer.Shell.Tests/Pact --manifest tests/Hexalith.FrontComposer.Shell.Tests/Pact/interaction-manifest.json --provider-state-catalog tests/Hexalith.FrontComposer.Shell.Tests/Pact/provider-state-catalog.json --report-output _bmad-output/implementation-artifacts/evidence/pact-provider-reconciliation/provider-verification.json` followed by `python3 eng/eventstore_runtime_evidence.py --live-evidence-root _bmad-output/implementation-artifacts/evidence/pact-provider-reconciliation --pact-dir tests/Hexalith.FrontComposer.Shell.Tests/Pact --write-live-receipt` -- expected: exit 0, current provenance, 19/19 pass, exact input hashes, clean teardown, bound receipt.
- `dotnet build tests/Hexalith.FrontComposer.Shell.Tests/Hexalith.FrontComposer.Shell.Tests.csproj --configuration Release -m:1` and its Pact test class -- expected: clean build, contract tests pass, no tracked Pact drift.
- `python3 -m unittest tests/eng/test_eventstore_runtime_evidence.py tests/eng/test_pact_provider_apphost_smoke.py` -- expected: all tests pass, including adversarial diagnostic assertions.
- `python3 eng/pact_provider_apphost_smoke.py --output _bmad-output/implementation-artifacts/evidence/pact-provider-reconciliation/apphost-smoke.json --timeout-seconds 300` -- expected: passing authenticated ten-resource report and clean stop, or an exact blocking condition without forged evidence.
- `pwsh ./eng/validate-contract-artifacts.ps1 -RequireProviderVerification` -- expected: historical archive and current provider/AppHost lanes pass independently.
