---
title: "EventStore Pact Contracts"
description: "File-based Pact evidence for the FrontComposer and Hexalith.EventStore REST contract."
genre: reference
audience: adopter
ownerStory: 11-25-current-eventstore-release-identity-and-evidence
status: published
reviewed: 2026-09-15
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

CI is split deliberately: EventStore owns provider execution over real loopback TCP. Before any provider output preparation or execution, CI creates one clean fixed-scope runtime manifest and a fresh external, non-symlinked NuGet package root. It removes the prior report, cleans, force-restores without cache, then seals both provider `project.assets.json` files and every selected package byte before any build or execution. The ledger binds each assets hash and complete package set, the exact global package union, canonical Base64 SHA-512 content hashes, recomputed nupkg hashes, and every regular extracted file; links, nonregular files, orphan packages, ambient caches, and pre/post-execution drift fail closed. CI then non-incrementally rebuilds the single-node Release provider-verification graph, runs its tests, and requires the current invocation to create a non-empty replacement. Receipt creation reuses the pre-run manifest and sealed ledger and applies the full canonical Pact, report-shape, interaction, timing, provenance, package, and redaction validation before it can write success. FrontComposer owns the current Pact bytes and validates all 19 interactions and state pairs, exact non-boolean indices, state-event containment, empty live approval bindings, exact current source/version/Builds/FrontComposer provenance, readiness, redaction, host shutdown, and port closure. A missing, incomplete, unbounded, stale, unsafe, or nonzero run fails closed. Technical evidence and migration approval are separate states.

Gate 2c also runs `eng/pact_provider_apphost_smoke.py` against the existing AppHost using that same pre-run manifest and a separate fresh external package root. A dirty sealed-input preflight writes failure evidence and returns without issuing a stop or start. A clean capture first requires a successful stop plus single-document Aspire-state confirmation, then cleans and force-restores without cache. It discovers the complete AppHost assets closure from JSON only, seals every graph and package byte, records `executionStartedAt`, enumerates imports plus project/package/reference/analyzer/additional/content/copy/native/runtime inputs, and accepts only the sealed repository, package root, or selected SDK installation. That deterministic binding is checked before and after the non-incremental build; `.deps.json` and every runtime output are inventoried before `--no-build` startup and rechecked after execution. The commands pin and record `UseHexalithProjectReferences=true`, `UseNuGetDeps=false`, every selected `Hexalith*FromSource=true`, and the intentional NuGet audit/transitive-pinning values. Tracked symlinks fail even when their link text hashes to the index. One recorded wall-clock deadline bounds cold-stop readiness, hostname resolution, build/start, every `aspire wait`/`describe`, every HTTP/WebSocket attempt (including negative-response bodies and fragmented frames), and final cleanup. HTTP disables environment proxies and never follows redirects. Before any credential is sent, `localhost` resolution runs behind the same absolute deadline and is rejected if it times out or any result is non-loopback, then the connection uses a verified numeric loopback peer. Generated Dapr/auth support records are excluded from the exact ten-resource primary topology only when explicit described type and parent metadata identify them. `/health` alone is readiness evidence; `/alive` cannot satisfy it. The smoke proves invalid-bearer rejection (401/403) separately for command submission, command status, query execution, and the actual projection SignalR WebSocket upgrade before recording authenticated success. SignalR uses a separately valid negotiation/upgrade for success on the exact same `/hubs/projection-changes` endpoint; success rejects a preemptive acknowledgement and validates the full HTTP upgrade, `Sec-WebSocket-Accept`, bounded WebSocket framing, fragmentation, and the exact SignalR acknowledgement. Command correlation must equal the submitted ULID. The successful query must return the same generated tenant/aggregate identity and must have consistent header/body `HandlerComputed` provenance. Failed readiness returns before the state-changing `CreateTenant` command. Cleanup independently requires `aspire ps --format Json` to show no AppHost and probes every discovered listener closed; without a discovered URL it passes only when the artifact proves this capture never attempted to start a host after a confirmed cold stop. After confirmed shutdown it removes only local Dapr `nr.db*` files absent before and created by the invocation, preserving pre-existing files byte-for-byte, then requires the sealed runtime scope, package ledger, and runtime-output inventory to remain exact. Missing credentials, Docker/Dapr infrastructure, topology startup failure, a missing authorization rejection, deadline exhaustion, or incomplete cleanup is a failed blocker.

NFR55 release rule: a release is blocked unless the checked-in pacts verify against the pinned EventStore provider version, or a named contract-drift issue explicitly blocks the release. Story 11.24 remains immutable authorization history. Story 11.25 and identity v3 preserve historical compatibility for their sealed tuple; they do not prove the successor Builds target or close G-3. Closure still requires genuine successor evidence and valid EventStore-maintainer, FrontComposer-maintainer, and Release-Owner receipts.

### Independent evidence and approval authorities

The lanes answer different questions and never share mutable hash authority:

- The immutable Story 11.24 archive under `evidence/frontcomposer-story-11-24/` answers “what ran then.” `.gitattributes`, its SHA-256 manifest, and the hard-coded capture pins protect its exact bytes. Historical validation does not compare that report to current Pact files.
- The dated archive under `evidence/pact-provider-reconciliation-history/2026-09-08-builds-35c3d1e5/` preserves the prior `059f6a89… / 3.103.0 / 35c3d1e5…` packet under hard-coded hashes.
- Identity v2 and `evidence/eventstore-runtime-identity-v2/` bind identity v1's SHA-256, the exact active tuple, the FrontComposer capture revision, the recapture decision, and a sealed provider/receipt/AppHost packet. `frontcomposer-runtime-inputs.json` inventories exact bytes for `src/**`, `samples/Counter/**`, root build/package/toolchain inputs (including an explicit present-or-absent `Directory.Build.rsp` coordinate), the identity-owned canonical Pact inputs, and the root-declared Builds, EventStore, Tenants, Parties, Memories, Commons, and PolymorphicSerializations gitlinks. Dirty, staged, untracked, ignored or out-of-scope root build controls, hidden `assume-unchanged`/`skip-worktree` flags in the root or a dependency, dependency nested submodules, worktree/index/HEAD byte mismatch, symlinks, missing inputs, detached Pacts, and later drift all fail. Inside a dependency checkout the rejection is scoped to paths the sealed restore, evaluated build/source, resolved-asset, or runtime-input graph can select: `node_modules/`, `.husky/`, and `__pycache__/` trees are inert tooling and are permitted whether tracked or untracked, while an untracked symlink at a project `bin`/`obj` root still fails before the generated-output exemption. The CLI creates an unpredictable temporary sibling exclusively and atomically replaces the manifest only after every check passes.
- The reconciliation lane under `evidence/pact-provider-reconciliation/` answers “do the current consumer bytes work now?” Its provider `inputHashes` bind the exact raw bytes supplied to the live verifier, its sibling-relative receipt binds the FrontComposer revision and runtime-input tree digest, and its AppHost report binds the same provenance plus current `Program.cs` and `.csproj` hashes. It remains CI's recapture target.
- Resolved-package provenance lives in two sidecar artifacts, `provider-package-ledger.json` and `apphost-package-ledger.json`, beside the packets that bind them by path, byte count, SHA-256, schema, `capturedAt`, and `treeSha256`. The extracted-file inventory required of the ledger is far larger than a bounded evidence document, so it carries its own derived size limit while `run-evidence.json` and `apphost-smoke.json` stay inside the ordinary bound. A missing sidecar, a binding that does not match its bytes, or a sidecar field that disagrees with the binding fails closed.
- The preserved package-less capture is exempt from the package-provenance and execution-boundary schema only under the history evidence root. Active and live evidence must present a genuine `provider-verification-run-evidence.v4` receipt and `apphost-smoke.v3` packet, including `executionStartedAt` and the runtime-manifest → package-ledger → execution-start → completion chronology.
- Approval receipts are a separate authority. The hash-bound policy maps role/actor pairs to exact HTTPS durable-source coordinates before the roster and subject freeze. Approval stays false with named issues when an actor or receipt is absent or invalid, and each normal or OI-18 receipt must carry its validator-pinned exact affirmative statement. The default effective roles are distinct EventStore maintainer, FrontComposer maintainer, and Release Owner actors. An approved OI-18 instead requires separate authorized Product and Architecture transfer receipts, then substitutes an explicitly named `accountable-frontcomposer-maintainer`; that replacement's migration receipt must follow the completed transfer.

### Run and validate the live lane

Run the live lane in this exact order: capture the immutable pre-run manifest; clean, force-restore, build, and test the provider graph; execute the provider application to create the report; write the receipt; run the AppHost smoke with the same manifest; then run the combined validator. Do not prepare or execute the provider before the manifest exists, and do not create the receipt before the provider application has produced a non-empty current report.

```bash
set -euo pipefail
workspace_root="$PWD"
capture_tmp="$(mktemp -d)"
trap 'find "$capture_tmp" -mindepth 1 -delete; rmdir "$capture_tmp"' EXIT
runtime_manifest="$capture_tmp/frontcomposer-runtime-inputs.json"
provider_ledger="$capture_tmp/provider-packages.json"
provider_packages="$capture_tmp/provider-packages"
apphost_packages="$capture_tmp/apphost-packages"
mkdir "$provider_packages" "$apphost_packages"
test ! -L "$capture_tmp" && test ! -L "$provider_packages" && test ! -L "$apphost_packages"
python3 eng/eventstore_runtime_evidence.py \
  --write-runtime-input-manifest \
  --runtime-input-manifest-output "$runtime_manifest" \
  --pact-dir tests/Hexalith.FrontComposer.Shell.Tests/Pact
export NUGET_PACKAGES="$provider_packages"
rm -f _bmad-output/implementation-artifacts/evidence/pact-provider-reconciliation/provider-verification.json
dotnet clean references/Hexalith.EventStore/tests/Hexalith.EventStore.ProviderVerification.Tests/Hexalith.EventStore.ProviderVerification.Tests.csproj \
  --configuration Release -m:1 -p:NuGetAudit=false
dotnet restore references/Hexalith.EventStore/tests/Hexalith.EventStore.ProviderVerification.Tests/Hexalith.EventStore.ProviderVerification.Tests.csproj \
  --force --force-evaluate --no-cache --disable-parallel \
  -p:Configuration=Release -p:NuGetAudit=false
python3 eng/eventstore_runtime_evidence.py \
  --write-package-ledger \
  --package-ledger-output "$provider_ledger" \
  --package-root "$provider_packages" \
  --package-assets references/Hexalith.EventStore/tests/Hexalith.EventStore.ProviderVerification.Tests/obj/project.assets.json \
  --package-assets references/Hexalith.EventStore/tests/Hexalith.EventStore.ProviderVerification/obj/project.assets.json \
  --pact-dir tests/Hexalith.FrontComposer.Shell.Tests/Pact
dotnet build references/Hexalith.EventStore/tests/Hexalith.EventStore.ProviderVerification.Tests/Hexalith.EventStore.ProviderVerification.Tests.csproj \
  --configuration Release --no-restore --no-incremental -m:1 -p:NuGetAudit=false
dotnet references/Hexalith.EventStore/tests/Hexalith.EventStore.ProviderVerification.Tests/bin/Release/net10.0/Hexalith.EventStore.ProviderVerification.Tests.dll
(
  cd references/Hexalith.EventStore
  dotnet "$workspace_root/references/Hexalith.EventStore/tests/Hexalith.EventStore.ProviderVerification/bin/Release/net10.0/Hexalith.EventStore.ProviderVerification.dll" \
    --verification-mode live-compatibility \
    --pact-directory "$workspace_root/tests/Hexalith.FrontComposer.Shell.Tests/Pact" \
    --manifest "$workspace_root/tests/Hexalith.FrontComposer.Shell.Tests/Pact/interaction-manifest.json" \
    --provider-state-catalog "$workspace_root/tests/Hexalith.FrontComposer.Shell.Tests/Pact/provider-state-catalog.json" \
    --report-output "$workspace_root/_bmad-output/implementation-artifacts/evidence/pact-provider-reconciliation/provider-verification.json"
)
test -s _bmad-output/implementation-artifacts/evidence/pact-provider-reconciliation/provider-verification.json
python3 eng/eventstore_runtime_evidence.py \
  --live-evidence-root _bmad-output/implementation-artifacts/evidence/pact-provider-reconciliation \
  --pact-dir tests/Hexalith.FrontComposer.Shell.Tests/Pact \
  --runtime-input-manifest "$runtime_manifest" \
  --package-ledger "$provider_ledger" \
  --package-root "$provider_packages" \
  --write-live-receipt
python3 eng/pact_provider_apphost_smoke.py --timeout-seconds 300 \
  --runtime-input-manifest "$runtime_manifest" \
  --package-root "$apphost_packages"
pwsh -NoLogo -NoProfile -File ./eng/validate-contract-artifacts.ps1 \
  -RequireProviderVerification \
  -ProviderVerificationReport _bmad-output/implementation-artifacts/evidence/pact-provider-reconciliation/provider-verification.json \
  -ProviderPackageRoot "$provider_packages" \
  -AppHostPackageRoot "$apphost_packages" \
  -RuntimeInputManifest "$runtime_manifest"
```

The provider receipt step also writes `provider-package-ledger.json` beside the receipt, and the AppHost
smoke writes `apphost-package-ledger.json` beside its packet. Both sidecars are part of the evidence set:
the lane fails closed if either is missing, altered, or unbound.

### Prepare the current runtime successor capture

Identity v3 is sealed at EventStore source `ba7ac196e60db8820525961791eccfacec24633f`,
package `3.106.0`, and Builds catalog `4f522a8caa62ad82584bdf56d54e16109b717b1c`.
The selected successor advances the EventStore source to
`66cb4edaa2b474090f2b4e375d481a4bbcd70a08` and the Builds catalog to
`2fba3497043fe5ffcfe4dc44c51a09eae9b950ab`, while retaining package `3.106.0`.
Do not rewrite or relabel identity v3.
After the preparation merge is pushed to `main`, dispatch the target-bound capture with its exact
40-hex merge revision:

```bash
set -euo pipefail
test "$(git branch --show-current)" = "main"
local_main_revision="$(git rev-parse refs/heads/main)"
test "$(git rev-parse HEAD)" = "$local_main_revision"
remote_main_revision="$(git ls-remote --exit-code https://github.com/Hexalith/Hexalith.FrontComposer.git refs/heads/main | awk 'NR == 1 { print $1 }')"
test -n "$remote_main_revision"
test "$local_main_revision" = "$remote_main_revision"
gh workflow run quality.yml \
  --repo Hexalith/Hexalith.FrontComposer \
  --ref main \
  -f frontcomposer_revision="$local_main_revision" \
  -f eventstore_source_revision=66cb4edaa2b474090f2b4e375d481a4bbcd70a08 \
  -f eventstore_package_version=3.106.0 \
  -f builds_catalog_revision=2fba3497043fe5ffcfe4dc44c51a09eae9b950ab
```

The command refuses to dispatch unless the checked-out local `main` and the hosted `main` resolve to
the same revision. For either a `push` to `main` or a `workflow_dispatch` on `main`, the workflow
preflight requires the checked-out FrontComposer revision and the exact source/package/Builds target,
validates identity v3 and its evidence as the byte-exact future-v4 predecessor, and requires v3
approval to remain open. Any mismatch stops before live capture. Provider and authenticated AppHost
validation must both pass before the workflow uploads
`eventstore-runtime-successor-candidate-<run-attempt>`, containing exactly
`frontcomposer-runtime-inputs.json` plus the five current live-evidence files.

This is phase 1 only. Download and review that six-file artifact without copying it into the repository.
A later phase 2 change may import those exact bytes, record the decision and approval subject after the
capture timestamps, and create identity v4. Until then, the v4 identity and evidence tree remain absent,
receipts remain empty, `migrationApprovalClaimed=false`, and identity v3 deliberately fails active
selection against the successor tuple.

Re-capture rules:

1. Pact, manifest, or provider-state changes require a fresh current EventStore-owned provider run and receipt. Do not update historical capture pins.
2. AppHost topology changes require a fresh authenticated AppHost smoke.
3. Identity v3 is sealed historical compatibility and must never be rewritten or relabelled. A future phase 2 may import only the reviewed six-file hosted candidate under `eventstore-runtime-identity-v4/`, bind v3 as its byte-exact predecessor, and create the v4 decision and approval subject.
4. Generate `frontcomposer-runtime-inputs.json` only from a clean fixed scope, before the provider/AppHost runs. Evidence capture times precede the decision, the decision precedes the subject, and every migration receipt follows the subject. Future-dated records beyond the five-minute skew allowance fail.
5. Run the combined validator; it independently rejects any historical forgery, prior-archive tamper, stale active identity, runtime-tree drift, failed current result, or false approval claim. Governance-only commits after the capture revision are accepted only while the fixed runtime tree remains byte-equivalent.

### Current successor status

Identity v3 is sealed historical compatibility for EventStore source
`ba7ac196e60db8820525961791eccfacec24633f`, package `3.106.0`, and Builds catalog
`4f522a8caa62ad82584bdf56d54e16109b717b1c`. The current checkout target
selects EventStore source `66cb4edaa2b474090f2b4e375d481a4bbcd70a08`, package `3.106.0`, and Builds catalog
`2fba3497043fe5ffcfe4dc44c51a09eae9b950ab`.

Identity v4 remains pending genuine hosted provider and authenticated AppHost evidence for that exact
target. No v4 identity, evidence tree, decision, approval subject, or receipt is present.
Migration approval remains open: identity v3 has `migrationApprovalClaimed=false` and an empty receipt
set, and any future v4 must begin in the same open state. Until phase 2 imports and validates the
reviewed hosted bytes, active release selection fails closed instead of treating the stale v3 packet
as current.

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
