---
title: 'Reconcile EventStore Provider Pacts and Live Evidence'
type: 'bugfix'
created: '2026-08-31'
status: done
baseline_commit: 'ee08c8eed5e4b57b702693d078a1339c95c82b4a'
baseline_revision: c6fe14c6613534d7397edd2e2c9eb5dccabd09df
review_loop_iteration: 0
followup_review_recommended: false
context:
  - '{project-root}/_bmad-output/project-context.md'
  - '{project-root}/.bmad-loop/runs/20260831-140103-5800/bundles/pact-provider-reconciliation/intent.md'
  - '{project-root}/_bmad-output/implementation-artifacts/spec-11-24-adopt-the-owner-approved-eventstore-runtime-identity.md'
warnings: [oversized]
deferred: []
---

<intent-contract>

## Intent

**Problem:** FrontComposer's 19 committed EventStore interactions encode synthetic response bodies, flattened query envelopes, invalid legacy ETags, and stale provider-state identities. The historical provider run consequently records 16 contract failures, while Gate 2c treats that immutable Story 11.24 capture as if it were current verification and the AppHost evidence stops at unauthenticated failures.

**Approach:** Reconcile the production Shell adapters and generated Pacts to the unchanged provider wire contract, correct only EventStore-owned verifier/test seams needed for truthful deterministic playback, and introduce separate live provider/AppHost evidence gates. Preserve the approved provider behavior and the complete historical Story 11.24 archive.

## Boundaries & Constraints

**Always:** Keep the provider verifier, host, and state seams in `references/Hexalith.EventStore`; exercise production controllers, middleware, model binding, serialization, and real IPv4 loopback Kestrel. Bind live reports to the exact current Pacts, provider source/version/Builds provenance, all 19 setup/results/teardown events, readiness, cleanup, redaction, and closed port. Keep source and Release provider identities truthful even when they differ. Treat the invocation as explicit approval for test-only EventStore submodule changes required by this bundle, and keep each repository's changes separately attributable. Outside the Shell adapters, permit only the smallest FrontComposer UI/AppHost build-input or project-metadata correction required to make the existing topology build past the observed `GenerateStaticWebAssetsDevelopmentManifest` failure; this exception must leave product and UI behavior, declared resources and topology, runtime contracts, package/catalog/gitlink identities, and dependency versions unchanged.

**Block If:** Any passing result requires changing EventStore production API/runtime behavior, changing package/catalog/gitlink identity, weakening auth/tenant enforcement, inventing runtime approval, or lacking the credentials/infrastructure needed to capture authenticated AppHost observations after exhausting repository-provided local test identities. Block again if repairing the AppHost build requires broader production-source, UI-behavior, topology, runtime-contract, package, catalog, gitlink, or dependency-version changes than the narrow build-only exception permits.

**Never:** Edit `_bmad-output/implementation-artifacts/deferred-work.md`; rewrite, reseal, or repurpose `_bmad-output/implementation-artifacts/evidence/frontcomposer-story-11-24/**`; move provider execution into FrontComposer; accept mock-only, `TestServer`, incomplete, unsafe, expected-nonzero, unauthenticated-401, or stale-input evidence; suppress a failed interaction; or weaken stale-Pact, redaction, cleanup, or CI exit-code gates.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|---------------|---------------------------|----------------|
| Accepted command | Valid ULID command through `EventStoreCommandClient` | 202 contract accepts provider `Location` ending in the submitted message ID, `Retry-After: 1`, and canonical optional response fields | Missing/mismatched identity or headers fails the interaction |
| Query success | Provider gateway envelope with payload and metadata | Shell returns items and reads `metadata.paging.totalCount`, falling back to item count when paging is absent | `success:false` or malformed canonical envelope follows the existing bounded query-failure path |
| Conditional query | Default/no-criteria query and valid self-routing ETag | Shell emits no synthetic criteria payload; provider can return 304 and cached/caller-owned paths retain their distinct semantics | Invalid/unsafe ETags are not sent; an unexpected 200 is classified from its canonical envelope |
| Provider failure | Provider-specific 400/403/404/409/429/500 ProblemDetails | Pact asserts stable status/header/body subsets and Shell retains its public failure classification | No synthetic common error fixture or leaked detail is accepted |
| Historical archive | Immutable Story 11.24 capture plus intentionally changed live Pacts | Archive integrity still validates without comparing historical report inputs to live Pact bytes | Any byte/pin/manifest forgery fails closed |
| Live provider run | Current Pacts/catalog and exact observed provider provenance | All 19 interactions pass over real loopback; report is complete, redaction-clean, stopped, and port-closed | Any identity lie, interaction/state/cleanup failure, or incomplete report exits nonzero |
| Live AppHost smoke | Current topology and repository-provided authenticated local identity | Full declared resource set is accounted for; health/readiness, command submit/status, query provenance, and SignalR connect are observed and clean shutdown is proven | The observed static-web-assets build failure may be repaired only under the narrow build-only exception; a broader repair, missing auth, or missing infrastructure is a recorded blocking condition, never passing evidence |

</intent-contract>

## Code Map

- `src/Hexalith.FrontComposer.Shell/Infrastructure/EventStore/EventStoreQueryClient.cs:95-288,480-522` -- request serialization and canonical response-envelope/count parsing; omit empty criteria payload so provider conditional evaluation is reachable.
- `src/Hexalith.FrontComposer.Shell/Infrastructure/EventStore/EventStoreCommandClient.cs:100-205` -- production accepted/error classification exercised by Pact; retain bounded fallbacks while accepting canonical response identity.
- `tests/Hexalith.FrontComposer.Shell.Tests/Infrastructure/EventStore/` -- focused adapter tests and reusable `EventStoreTestSupport`; add canonical envelope, semantic failure, nested count, and conditional-query coverage here.
- `tests/Hexalith.FrontComposer.Shell.Tests/Pact/EventStorePactContractTests.cs:54-804` -- sole generator for four Pacts, manifest, state catalog, and handoff; replace synthetic fixtures and hand serialization assumptions with Pact matchers/canonical provider shapes.
- `tests/Hexalith.FrontComposer.Shell.Tests/Pact/{frontcomposer-eventstore-*.json,interaction-manifest.json,provider-state-catalog.json,provider-verification-handoff.md}` -- regenerated live consumer contract; keep exactly 19 unique interactions/states.
- `references/Hexalith.EventStore/tests/Hexalith.EventStore.ProviderVerification/ProviderVerificationHost.cs:44-299` -- read-only production-pipeline composition; test overrides only.
- `references/Hexalith.EventStore/tests/Hexalith.EventStore.ProviderVerification/{StatefulETagService.cs,StatefulAuthorizationValidator.cs,ProviderStateCoordinator.cs,RuntimeIdentityValidator.cs,ProviderVerificationApplication.cs}` -- provider-owned test seams; issue valid self-routing ETags, keep state identity truthful, and separate live observed provenance from frozen Story 11.24 migration authorization without relaxing the historical mode.
- `references/Hexalith.EventStore/tests/Hexalith.EventStore.ProviderVerification.Tests/` and `README.md` -- prove both historical fail-closed identity mode and current live compatibility mode, real Kestrel, all states, and cleanup.
- `.gitattributes` and `_bmad-output/implementation-artifacts/evidence/frontcomposer-story-11-24/**` -- immutable historical bytes; read-only.
- `eng/eventstore_runtime_evidence.py:59-95,1003-1367` and `tests/eng/test_eventstore_runtime_evidence.py` -- retain capture-forgery pins, decouple historical input integrity from live Pact bytes, and validate a distinct live reconciliation report.
- `eng/validate-contract-artifacts.ps1:217-259` and `.github/workflows/quality.yml:169-236` -- split immutable-archive validation from required passing live-provider evidence; preserve fail-closed propagation, stale diff, redaction, diagnostics, and success-only evidence upload.
- `_bmad-output/contracts/frontcomposer-eventstore-approved-runtime-identity-v1.json` and `tests/Hexalith.FrontComposer.Shell.Tests/Governance/CiGovernanceTests.cs:3416-3533` -- stop presenting stale source/Builds values as the live provider; retain the historical pointer while recording exact current provenance without claiming migration approval.
- `src/Hexalith.FrontComposer.AppHost/{Program.cs,Hexalith.FrontComposer.AppHost.csproj}`, `src/Hexalith.FrontComposer.UI/Hexalith.FrontComposer.UI.csproj`, and directly implicated existing UI/AppHost build metadata -- topology and buildability inputs only; apply at most the narrow build-only correction and do not redesign the topology or change product/UI behavior. A new bounded `eng/` capture/validation helper must enumerate its resources and perform authenticated observations.
- `_bmad-output/implementation-artifacts/evidence/pact-provider-reconciliation/` -- new provider report/run receipt and AppHost smoke evidence; never place current evidence under the historical Story 11.24 tree.
- `docs/reference/pact-contracts.md` -- document historical-versus-live lanes, provider ownership, current run commands, and passing criteria.

## Tasks & Acceptance

**Execution:**
- [x] `src/Hexalith.FrontComposer.Shell/Infrastructure/EventStore/EventStoreQueryClient.cs`, `EventStoreCommandClient.cs`, `tests/Hexalith.FrontComposer.Shell.Tests/Infrastructure/EventStore/EventStoreClientTests.cs`, and `EventStoreQueryCacheIntegrationTests.cs` -- consume the canonical provider envelopes/identity and make no-criteria conditional requests provider-safe while preserving public classifications.
- [x] `tests/Hexalith.FrontComposer.Shell.Tests/Pact/EventStorePactContractTests.cs` and `tests/Hexalith.FrontComposer.Shell.Tests/Pact/{frontcomposer-eventstore-*.json,interaction-manifest.json,provider-state-catalog.json,provider-verification-handoff.md}` -- model real accepted/error/query/header shapes with deterministic values or Pact matchers, correct tenant/aggregate/state metadata, and regenerate exactly 19 interactions from production adapters.
- [x] `references/Hexalith.EventStore/tests/Hexalith.EventStore.ProviderVerification/{StatefulETagService.cs,StatefulAuthorizationValidator.cs,RuntimeIdentityValidator.cs,ProviderVerificationApplication.cs,ProviderVerificationOptions.cs,README.md}` and `references/Hexalith.EventStore/tests/Hexalith.EventStore.ProviderVerification.Tests/{StatefulProviderDependenciesTests.cs,RuntimeIdentityValidatorTests.cs,ProviderVerificationApplicationTests.cs,ProviderVerificationOptionsTests.cs,RealKestrelPactTests.cs}` -- correct verifier-only state fixtures and add a live provenance mode whose contract verdict is not poisoned by the immutable Story 11.24 migration receipt; retain historical mode and production sources unchanged.
- [x] `eng/eventstore_runtime_evidence.py`, `tests/eng/test_eventstore_runtime_evidence.py`, `eng/validate-contract-artifacts.ps1`, and `_bmad-output/implementation-artifacts/evidence/pact-provider-reconciliation/{provider-verification.json,run-evidence.json}` -- preserve historical capture validation and add strict passing live-report validation for current inputs.
- [x] `src/Hexalith.FrontComposer.UI/Hexalith.FrontComposer.UI.csproj`, directly implicated existing UI/AppHost build metadata, `eng/pact_provider_apphost_smoke.py`, `tests/eng/test_pact_provider_apphost_smoke.py`, and `_bmad-output/implementation-artifacts/evidence/pact-provider-reconciliation/apphost-smoke.json` -- first apply only the smallest correction needed to clear the observed static-web-assets build failure without changing behavior, topology, contracts, or dependency identities; then start/describe/probe/stop the existing Aspire topology with bounded timeouts and repository-provided auth, hash live topology inputs, and emit support-safe authenticated observations for every required surface.
- [x] `.github/workflows/quality.yml`, `tests/Hexalith.FrontComposer.Shell.Tests/Governance/CiGovernanceTests.cs`, `_bmad-output/contracts/frontcomposer-eventstore-approved-runtime-identity-v1.json`, `tests/Hexalith.FrontComposer.Shell.Tests/Pact/provider-verification-handoff.md`, and `docs/reference/pact-contracts.md` -- require both immutable historical integrity and current passing provider/AppHost evidence without `continue-on-error`, forged approval, or stale identity labels.

**Acceptance Criteria:**
- Given the four regenerated Pact files and 19-state catalog, when the EventStore-owned verifier runs against the current pinned source, then all 19 production-pipeline interactions pass and the report proves exact setup/teardown coverage, readiness, redaction safety, host stop, and port closure.
- Given default and meaningful query criteria, when `EventStoreQueryClient` sends and receives provider traffic, then wire payloads are provider-safe and canonical envelope metadata produces correct items, counts, ETags, 304 cache reuse, and bounded semantic failures.
- Given the immutable Story 11.24 archive and new live evidence, when Gate 2c runs, then historical byte integrity and current compatibility are evaluated independently; edited/resealed history, stale current inputs, a failed interaction, or a nonzero verifier exit fails the gate.
- Given the current AppHost topology and the observed `GenerateStaticWebAssetsDevelopmentManifest` failure, when the bounded smoke producer runs, then at most the narrow build-only correction makes that unchanged topology build, every declared resource is accounted for, and authenticated health/readiness, command submit/status, query provenance, and projection SignalR observations succeed before a verified clean stop; any broader repair requirement blocks the story again.
- Given the completed bundle diff, when repository ownership is inspected, then EventStore changes are limited to provider-verification tests/tooling, FrontComposer production changes are limited to adapters plus any minimal behavior-neutral UI/AppHost build-only correction authorized above, and neither the ledger, historical evidence tree, production provider API, topology, runtime contracts, package/catalog/gitlink identities, nor dependency versions changed.

## Spec Change Log

- 2026-08-31: Human resolution authorizes only the minimal behavior-neutral UI/AppHost build-input or project-metadata repair needed to clear the observed static-web-assets build failure while retaining the real authenticated AppHost smoke and Gate 2c; any broader repair blocks and re-escalates.

## Review Triage Log

- BH1 (19 MB baseline dump / skill-tree noise) — `false`. The dump is the workflow baseline-to-HEAD package, not a product defect. FrontComposer and EventStore remain separately attributable in their own repositories.
- BH2 (EventStore `059f6a89-dirty` Timeline rewrite) — `medium`. The only EventStore hunk is an uncommitted `ProviderVerificationTimeline` change. CI rebuilds the verifier from gitlink `059f6a8917bfab26b85775be464840a1610dfdeb`, so clones cannot consume the rewrite and the parent tree is dirty.
- BH3 (spec still `in-review`; pins differ from spec-2) — `false`. `in-review` is this workflow step. Spec-2 recorded an earlier compatibility tuple; current evidence binds the current gitlinks. Spec-2 leftover gaps are not this change.
- BH4 (`bmad-build-auto-result-pact-provider-reconciliation.md` says blocked) — `false`. That file is a stale auto-run receipt, not the current working tree or live evidence verdict.
- BH5 / EC4 (`Program.cs` Keycloak bind) — `false`. The ten declared resources are unchanged. EventStore 3.103's two-arg `WithEventStoreClientCredentials` is documented to create empty required parameters; the four-arg call supplies the existing Keycloak realm `admin-user` / `admin-pass` identity. Auth is not weakened, this is not a smoke fork (`modifiedForSmoke: false` matches the real AppHost), and the remaining “edit the spec” fix is rejected by triage rules.
- BH6 (`payload: null` still serialized) — `false`. `QueryClient_DefaultCriteria_OmitsSyntheticPayload` asserts `payload` is JSON null, cache Pacts pair that body with `If-None-Match` and 304, and the live report is 19/19 `interaction.passed` against the current provider.
- BH7 (no empty-criteria + 304 pairing; cache tests missing from dump) — `false`. `frontcomposer-eventstore-cache-validation.json` has `query-etag-match` / `query-etag-no-cache` with `payload: null` and `If-None-Match`. `EventStoreQueryCacheIntegrationTests.cs` is present in the tree.
- BH8 (failure Pacts only `{"status":N}`) — `false`. `ProblemDetailsSubset` is the stable status subset the contracts lock; live failure interactions passed. The spec forbids a synthetic common error fixture, not a status-only matcher.
- BH9 (`command-not-found` setup vs `seededAggregateId: order-1`) — `false`. Setup seeds the tenant without aggregate `order-1`; `seededAggregateId` names the requested missing identity, not a seeded row.
- BH10 (`write_live_receipt` placeholder command / skips `validate_live`) — `false`. The receipt is a bounded template. Gate 2c writes it after the live verifier, then the next step runs the evidence unit suite and `validate-contract-artifacts.ps1 -RequireProviderVerification`.
- BH11 (health `authenticated: true` even on failure) — `low`. Failed health still records `result: failed` and Gate 2c rejects a non-passing smoke. Everyday CI only accepts the passing path. Rejected: not met in everyday use, and tightening would add branches.
- BH12 (cleanup probes HTTPS/DCP while wait is 15s) — `low`. Pre-existing cleanup heuristic. The captured smoke has `hostStopped: true`, `portsClosed: true`, and `runningAppHostsAfterAttempt: 0`. Rejected: everyday passing run already proves clean stop; a harder wait is more than a direct correction.
- BH13 (`DropPublishedFrontComposerAssemblies.targets` nuget-path matcher) — `false`. That file is not part of this run's working-tree change; it appears only because the review diff starts at the August 31 baseline.
- BH14 (CreateTenant left behind; SignalR handshake-only) — `low`. Bounded local smoke by design. Rejected: leftover `pact-reconciliation-*` tenants and handshake-only SignalR are not everyday product defects, and cleanup/subscribe work is more than a direct correction.
- BH15 (`FakeRuntime` HTTPS-only success paths) — `false`. Unit-test doubles are pre-existing coverage limits, not a defect introduced by the current evidence recapture.
- BH16 (Aspire 13.5.3 / Dapr CLI 1.18.0 / runtime 1.18.2; 300s smoke in Gate 2c) — `false`. Workflow versions and job shape are pre-existing CI, not this run's delta.
- BH17 (missing trailing newline on handoff/report) — `low`. Receipt SHA-256 binds the exact current bytes. Rejected: everyday validation already hashes those bytes; adding a newline would force a recapture.
- BH18 (`deferred-work.md` appears in the baseline dump) — `false`. This run did not modify the ledger (`git diff HEAD` is empty for that path). The dump includes later unrelated stories.
- EC1 (`SortDescending` without `SortColumn` omitted) — `false`. `HasMeaningfulQueryPayload` requires a sort column. A dangling direction bit with no column is not a meaningful wire payload.
- EC2 (`availableTransports` null → TypeError) — `low`. `document.get("availableTransports", [])` returns `None` when the key is JSON null, and `any(... for item in transports)` would throw. SignalR negotiate returns an array on the captured success path. Rejected: not met in everyday use; a type guard is extra complexity.
- EC3 (15s stop wait then `aspire start`) — `low`. `_wait_until_host_absent_or_ports_closed` returns after timeout without raising. The capture still begins with `aspire stop` and the committed smoke proves a clean stop. Rejected: pre-existing, everyday passing run did not hit it, and a hard fail-closed wait is more than a direct correction.
- EC5 (SourceTools / MCP / Gate 4 in the dump) — `false`. Those paths are later unrelated commits inside the August 31 baseline window, not this run's FrontComposer production delta.
- VG (verification-gap layer) — no findings filed.

## Design Notes

Historical Story 11.24 evidence answers “what ran then” and remains immutable. The new live lane answers “do current consumer bytes verify against the current provider provenance.” They must not share mutable hash authority or collapse migration authorization into compatibility.

The provider's conditional-query gate rejects synthetic non-empty criteria and requires self-routing ETags. Fix the Shell's empty-query wire shape and the verifier's legacy ETag fixture rather than changing production conditional semantics.

## Verification

**Commands:**
- `dotnet build tests/Hexalith.FrontComposer.Shell.Tests/Hexalith.FrontComposer.Shell.Tests.csproj --configuration Release -m:1` -- expected: zero warnings/errors.
- `dotnet tests/Hexalith.FrontComposer.Shell.Tests/bin/Release/net10.0/Hexalith.FrontComposer.Shell.Tests.dll -class Hexalith.FrontComposer.Shell.Tests.Pact.EventStorePactContractTests` -- expected: all contract tests pass and regeneration is stable.
- `dotnet build references/Hexalith.EventStore/tests/Hexalith.EventStore.ProviderVerification.Tests/Hexalith.EventStore.ProviderVerification.Tests.csproj --configuration Release -m:1` followed by its built DLL -- expected: historical and live modes plus real-loopback tests pass.
- EventStore README live verifier command against the FrontComposer Pact directory -- expected: exit 0, `finalVerdict: passed`, 19/19 interactions and state pairs, clean teardown.
- `python3 -m unittest tests/eng/test_eventstore_runtime_evidence.py` and the new AppHost evidence suite -- expected: canonical success and adversarial historical/live failures pass.
- `pwsh ./eng/validate-contract-artifacts.ps1 -RequireProviderVerification` -- expected: immutable archive valid and current live provider/AppHost evidence accepted.
- Focused Governance test DLL plus `git diff --exit-code -- tests/Hexalith.FrontComposer.Shell.Tests/Pact` -- expected: quality workflow contract passes and regenerated artifacts are stable.

