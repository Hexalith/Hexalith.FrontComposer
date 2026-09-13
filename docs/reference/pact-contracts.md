---
title: "EventStore Pact Contracts"
description: "File-based Pact evidence for the FrontComposer and Hexalith.EventStore REST contract."
genre: reference
audience: adopter
ownerStory: 11-25-current-eventstore-release-identity-and-evidence
status: published
reviewed: 2026-09-12
uid: frontcomposer.reference.pact-contracts
slug: reference/pact-contracts/
---

# EventStore Pact Contracts

FrontComposer v1 contract evidence is file based. The source of truth lives in `tests/Hexalith.FrontComposer.Shell.Tests/Pact/`:

- `frontcomposer-eventstore-command-dispatch.json`
- `frontcomposer-eventstore-query-execution.json`
- `frontcomposer-eventstore-cache-validation.json`
- `frontcomposer-eventstore-auth-tenant-propagation.json`
- `interaction-manifest.json`
- `provider-state-catalog.json`
- `provider-verification-handoff.md`

The contract tests exercise the production `EventStoreCommandClient` and `EventStoreQueryClient` paths through the existing command/query abstractions and replay each committed interaction through PactNet's mock HTTP server. They do not use Pact Broker, PactFlow, browser-only coverage, mutation testing, property-based idempotency, flaky-test quarantine, accessibility gates, release signing, SBOM, or LLM benchmark governance.

## Regenerate Pacts

Run:

```powershell
dotnet test tests/Hexalith.FrontComposer.Shell.Tests/Hexalith.FrontComposer.Shell.Tests.csproj --filter "Category=Contract"
pwsh ./eng/validate-contract-artifacts.ps1 -RequireProviderVerification
git diff -- tests/Hexalith.FrontComposer.Shell.Tests/Pact
```

Review pact diffs as API evidence. Expected diffs should name the interaction, method/path, expected status/header/body shape, provider state, owning acceptance criteria, adapter path, and classifier expectation. Unexpected diffs block the change until the adapter, test, or provider-state fixture is corrected.

The validator checks that `interaction-manifest.json` exactly matches the committed pact interactions by description, provider state, method, and path. Missing or orphaned manifest entries fail the lane.

## Provider Verification

Provider verification belongs beside the `Hexalith.EventStore` provider host because PactNet's native verifier must call a real loopback TCP endpoint. Do not use ASP.NET Core `TestServer` or `WebApplicationFactory` for Pact verifier playback.

The EventStore-owned command shape is recorded in `provider-verification-handoff.md`. Live compatibility uses the committed pacts plus `interaction-manifest.json` and `provider-state-catalog.json`, and produces a bounded report under `_bmad-output/implementation-artifacts/evidence/pact-provider-reconciliation/`.

CI is split deliberately: EventStore owns provider execution over real loopback TCP. Before any provider output preparation or execution, CI creates one clean fixed-scope runtime manifest. It then removes the prior report, cleans, force-restores without cache, non-incrementally rebuilds the single-node Release provider-verification graph, runs its tests, and requires the current invocation to create a non-empty replacement. Receipt creation reuses the pre-run manifest and applies the full canonical Pact, report-shape, interaction, timing, provenance, and redaction validation before it can write success. FrontComposer owns the current Pact bytes and validates all 19 interactions and state pairs, exact non-boolean indices, state-event containment, empty live approval bindings, exact current source/version/Builds/FrontComposer provenance, readiness, redaction, host shutdown, and port closure. A missing, incomplete, unbounded, stale, unsafe, or nonzero run fails closed. Technical evidence and migration approval are separate states.

Gate 2c also runs `eng/pact_provider_apphost_smoke.py` against the existing AppHost using that same pre-run manifest. A dirty sealed-input preflight writes failure evidence and returns without issuing a stop or start. A clean capture first requires a successful stop plus single-document Aspire-state confirmation, then cleans, force-restores without cache, and rebuilds the Debug output graph with `--no-restore --no-incremental` before starting with `--no-build`; stale restore intermediates or ignored `bin`/`obj` assemblies therefore cannot satisfy the smoke. The commands pin and record `UseHexalithProjectReferences=true`, `UseNuGetDeps=false`, every selected `Hexalith*FromSource=true`, and the intentional NuGet audit/transitive-pinning values, then enumerate evaluated project/package references and traverse regenerated assets to prove the seven exact root checkouts before startup. Tracked symlinks fail even when their link text hashes to the index. One recorded wall-clock deadline bounds cold-stop readiness, build/start, every `aspire wait`/`describe`, every HTTP/WebSocket attempt (including negative-response bodies and fragmented frames), and final cleanup. HTTP disables environment proxies and never follows redirects. Before any credential is sent, `localhost` is resolved and rejected if any result is non-loopback, then the connection uses a verified numeric loopback peer. Generated Dapr/auth support records are excluded from the exact ten-resource primary topology only when explicit described type and parent metadata identify them. Health/liveness is recorded strictly as readiness, not authentication. The smoke proves invalid-bearer rejection (401/403) separately for command submission, command status, query execution, and projection SignalR before recording authenticated success. The SignalR control and success use the exact same `/hubs/projection-changes` endpoint; success rejects a preemptive acknowledgement and validates the full HTTP upgrade, `Sec-WebSocket-Accept`, bounded WebSocket framing, fragmentation, and the exact SignalR acknowledgement. Command correlation must equal the submitted ULID. The successful query must return the same generated tenant/aggregate identity and must have consistent header/body `HandlerComputed` provenance. Failed readiness returns before the state-changing `CreateTenant` command. Cleanup independently requires `aspire ps --format Json` to show no AppHost and probes every discovered listener closed; without a discovered URL it passes only when the artifact proves this capture never attempted to start a host after a confirmed cold stop. After confirmed shutdown it removes only local Dapr `nr.db*` files absent before and created by the invocation, preserving pre-existing files byte-for-byte, then requires the sealed runtime scope to be clean again. Missing credentials, Docker/Dapr infrastructure, topology startup failure, a missing authorization rejection, deadline exhaustion, or incomplete cleanup is a failed blocker.

NFR55 release rule: a release is blocked unless the checked-in pacts verify against the pinned EventStore provider version, or a named contract-drift issue explicitly blocks the release. Story 11.24 remains immutable authorization history. Story 11.25 proves the current tuple but does not close G-3 until the required EventStore-maintainer, FrontComposer-maintainer, and Release-Owner receipts validate.

### Independent evidence and approval authorities

The lanes answer different questions and never share mutable hash authority:

- The immutable Story 11.24 archive under `evidence/frontcomposer-story-11-24/` answers “what ran then.” `.gitattributes`, its SHA-256 manifest, and the hard-coded capture pins protect its exact bytes. Historical validation does not compare that report to current Pact files.
- The dated archive under `evidence/pact-provider-reconciliation-history/2026-09-08-builds-35c3d1e5/` preserves the prior `059f6a89… / 3.103.0 / 35c3d1e5…` packet under hard-coded hashes.
- Identity v2 and `evidence/eventstore-runtime-identity-v2/` bind identity v1's SHA-256, the exact active tuple, the FrontComposer capture revision, the recapture decision, and a sealed provider/receipt/AppHost packet. `frontcomposer-runtime-inputs.json` inventories exact bytes for `src/**`, `samples/Counter/**`, root build/package/toolchain inputs (including an explicit present-or-absent `Directory.Build.rsp` coordinate), the identity-owned canonical Pact inputs, and the root-declared Builds, EventStore, Tenants, Parties, Memories, Commons, and PolymorphicSerializations gitlinks. Dirty, staged, untracked, ignored or out-of-scope root build controls, hidden `assume-unchanged`/`skip-worktree` flags in the root or a dependency, dependency nested submodules/untracked inputs, worktree/index/HEAD byte mismatch, symlinks, missing inputs, detached Pacts, and later drift all fail. The CLI creates an unpredictable temporary sibling exclusively and atomically replaces the manifest only after every check passes.
- The reconciliation lane under `evidence/pact-provider-reconciliation/` answers “do the current consumer bytes work now?” Its provider `inputHashes` bind the exact raw bytes supplied to the live verifier, its sibling-relative receipt binds the FrontComposer revision and runtime-input tree digest, and its AppHost report binds the same provenance plus current `Program.cs` and `.csproj` hashes. It remains CI's recapture target.
- Approval receipts are a separate authority. The hash-bound policy maps role/actor pairs to exact HTTPS durable-source coordinates before the roster and subject freeze. Approval stays false with named issues when an actor or receipt is absent or invalid, and each normal or OI-18 receipt must carry its validator-pinned exact affirmative statement. The default effective roles are distinct EventStore maintainer, FrontComposer maintainer, and Release Owner actors. An approved OI-18 instead requires separate authorized Product and Architecture transfer receipts, then substitutes an explicitly named `accountable-frontcomposer-maintainer`; that replacement's migration receipt must follow the completed transfer.

### Run and validate the live lane

Build and run the provider from `references/Hexalith.EventStore` using the live command in its provider-verification README, then bind the successful report and run the AppHost smoke:

```bash
runtime_manifest="$(mktemp)"
python3 eng/eventstore_runtime_evidence.py \
  --write-runtime-input-manifest \
  --runtime-input-manifest-output "$runtime_manifest" \
  --pact-dir tests/Hexalith.FrontComposer.Shell.Tests/Pact
python3 eng/eventstore_runtime_evidence.py \
  --live-evidence-root _bmad-output/implementation-artifacts/evidence/pact-provider-reconciliation \
  --pact-dir tests/Hexalith.FrontComposer.Shell.Tests/Pact \
  --runtime-input-manifest "$runtime_manifest" \
  --write-live-receipt
python3 eng/pact_provider_apphost_smoke.py --timeout-seconds 300 \
  --runtime-input-manifest "$runtime_manifest"
pwsh ./eng/validate-contract-artifacts.ps1 -RequireProviderVerification
```

Re-capture rules:

1. Pact, manifest, or provider-state changes require a fresh current EventStore-owned provider run and receipt. Do not update historical capture pins.
2. AppHost topology changes require a fresh authenticated AppHost smoke.
3. A release-identity update must seal the accepted current packet under `eventstore-runtime-identity-v2/recapture/` and update its hash-bound approval subject; routine CI recapture never rewrites that sealed packet.
4. Generate `frontcomposer-runtime-inputs.json` only from a clean fixed scope, before the provider/AppHost runs. Evidence capture times precede the decision, the decision precedes the subject, and every migration receipt follows the subject. Future-dated records beyond the five-minute skew allowance fail.
5. Run the combined validator; it independently rejects any historical forgery, prior-archive tamper, stale active identity, runtime-tree drift, failed current result, or false approval claim. Governance-only commits after the capture revision are accepted only while the fixed runtime tree remains byte-equivalent.

### Current reconciliation outcome

The Story 11.25 provider run passes all 19 interactions at EventStore source
`059f6a8917bfab26b85775be464840a1610dfdeb`, EventStore version `3.103.0`, and current Builds catalog
`a32cb422749352cce8dec948aa3e78c8f00eb4cf`. The authenticated AppHost smoke on the same provenance
starts the existing ten-resource topology, observes readiness health, authenticated command submit/status, tenant query
provenance (`HandlerComputed`) with the response bound to the generated tenant/aggregate identity,
and projection SignalR, proves all four protected surfaces reject an invalid bearer,
then confirms `aspire ps` is empty and all advertised ports are closed. Identity v1 remains
byte-identical historical authorization for `bb94d93e… / 3.91.1 / a8a50859…`, and the dated
`35c3d1e5…` capture remains prior compatibility evidence. Migration approval is still open because
the three required active-identity receipts do not yet exist.

## Troubleshooting

Native verifier startup failures usually mean unsupported OS/architecture, missing runtime pieces, or local process constraints. PactNet `5.0.1` is pinned; CI should use supported Windows x64 or Linux x64/ARM64 runners. If startup fails before interactions are evaluated, use the documented containerized or provider-owned fallback and mark the release evidence as blocked.

Provider startup failures block verification when there is a port collision, failed health probe, stale provider process, startup timeout, or provider-state teardown failure. The verifier must reset tenant/user/aggregate/cache state per interaction and isolate retry or parallel runs by a verification run id.

Stale pact files are cleaned up by deleting only intentionally removed interactions, regenerating the contract lane, and confirming `interaction-manifest.json` no longer lists orphaned or duplicate interactions.

## Decision Record

Decision: use committed file-based Pact JSON and real-TCP provider verification for the FrontComposer/EventStore REST boundary.

Rejected alternatives:

- Broker-first workflow: deferred until multiple provider versions, external consumers, or cross-repo release coordination require it.
- In-memory provider verification: rejected because the native Pact verifier calls an HTTP endpoint.
- Hand-built JSON-only tests: rejected as the only source of truth; contract artifacts must be generated from the production EventStore adapter behavior.
- Browser-only contract coverage: rejected because REST drift is best isolated at the Shell/EventStore adapter boundary.
