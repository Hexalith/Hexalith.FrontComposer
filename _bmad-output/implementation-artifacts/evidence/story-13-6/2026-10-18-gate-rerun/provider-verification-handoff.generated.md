# EventStore Provider Verification Handoff

Story: 10-3-consumer-driven-contract-tests-pact (handoff), provider reconciliation live lane
Consumer: Hexalith.FrontComposer.Shell
Provider: Hexalith.EventStore
Interaction count: 19
Historical status: the immutable Story 11.24 EventStore-owned report remains at
`_bmad-output/implementation-artifacts/evidence/frontcomposer-story-11-24/provider-verification/provider-verification.json`
(`finalVerdict: failed`, 19/19 interactions, 19 setup and 19 teardown events, host stopped, port closed).
It records what ran then and is never compared to current Pact bytes.

Current status: the reconciliation report is
`_bmad-output/implementation-artifacts/evidence/pact-provider-reconciliation/provider-verification.json`
(`verificationMode: live-compatibility`, `finalVerdict: passed`, 19/19 interactions, exact setup/teardown,
real loopback Kestrel, host stopped, port closed). Its adjacent `run-evidence.json` binds the exact report
and, by path/size/SHA-256, the `provider-package-ledger.json` sidecar that carries the extracted-file
package inventory; the AppHost packet binds `apphost-package-ledger.json` the same way. The sidecars keep
the bounded evidence documents inside their size limit while the ledger itself stays byte-bound.
Compatibility evidence records current source/version/Builds/FrontComposer/runtime-input-tree provenance without claiming migration approval. The adjacent receipt uses the sibling-relative `provider-verification.json` coordinate so it resolves identically in the live lane and sealed v2 recapture.

Provider verification must run in `Hexalith.EventStore` against a real loopback TCP endpoint. Do not use ASP.NET Core `TestServer` or `WebApplicationFactory` for Pact verifier playback, because the native verifier calls an HTTP endpoint.

Before any provider preparation or execution, generate one clean fixed-scope runtime-input manifest and
select a fresh external non-symlinked NuGet package root. Then remove the prior live report, clean,
force-restore without cache, seal both provider assets graphs plus the exact nupkg and extracted-file
package bytes, and non-incrementally rebuild the Release provider graph. Receipt creation reuses the
exact pre-run manifest and package ledger, writes that ledger beside the receipt as
`provider-package-ledger.json`, and recomputes the ledger after execution.

The preserved package-less capture is exempt from the package-provenance and execution-boundary schema
only under the history evidence root. Active and live evidence must carry a genuine
`provider-verification-run-evidence.v4` receipt and `apphost-smoke.v3` packet with their ledger sidecars,
`executionStartedAt`, and the runtime-manifest/ledger/execution/completion chronology, or Gate 2c fails
closed.

The pinned EventStore 3.106 test assembly locates the accepted Pact corpus through its
`references/Hexalith.FrontComposer` gitlink. In a FrontComposer source-closure workflow,
leave that historical nested submodule uninitialized and stage the six current generated
Pact JSON files into its empty worktree. Then run the built provider test assembly from the
EventStore repository root:

```powershell
Set-Location references/Hexalith.EventStore
dotnet tests/Hexalith.EventStore.ProviderVerification.Tests/bin/Release/net10.0/Hexalith.EventStore.ProviderVerification.Tests.dll
```

That test invocation does not create the live compatibility report. Run the provider application separately from the EventStore repository root:

```powershell
dotnet tests/Hexalith.EventStore.ProviderVerification/bin/Release/net10.0/Hexalith.EventStore.ProviderVerification.dll --verification-mode live-compatibility <validated canonical inputs>
```

Any failed interaction, stale input/provenance, unsafe host, incomplete cleanup, or nonzero process exit
rejects the current lane. Gate 2c separately requires a passing authenticated Aspire AppHost smoke;
missing infrastructure or credentials remains a blocker and cannot be relabeled as passing evidence.
The smoke disables environment proxies, resolves every credential destination behind the same deadline
to a verified numeric loopback peer before sending credentials or tokens, and classifies support records only from
described type/parent metadata, and observes the exact ten-resource primary topology healthy under one
recorded deadline. `/health` alone is readiness evidence; `/alive` cannot substitute. A confirmed cold
stop and fresh Debug clean/forced-restore precede JSON-only AppHost assets discovery and package sealing.
The capture closes imports, references, analyzers, content/copy, native/runtime inputs, `.deps.json`, and
runtime outputs to the sealed repository, fresh package root, or selected SDK before `--no-build` startup. The build
pin `UseHexalithProjectReferences=true`, `UseNuGetDeps=false`, every selected `Hexalith*FromSource=true`,
`NuGetAudit=false`, and `CentralPackageTransitivePinningEnabled=false`; capture then evaluates those
properties, evaluated project/package references, regenerated assets, and the seven root source checkouts before starting. A dirty runtime-input preflight writes
failure evidence without issuing any Aspire lifecycle command. Redirects are refused,
and phase deadlines bound HTTP bodies, WebSocket upgrade headers, and bounded fragmented frames. It
proves invalid-bearer 401/403 rejection for protected command submit, command status, query, and the
actual SignalR WebSocket upgrade on a separately negotiated connection before any 101. A separate
standards-valid authenticated negotiation and handshake use the same endpoint. The successful
returned command correlation must equal the submitted ULID. Consistent header/body `HandlerComputed`
query provenance binds to the generated tenant/aggregate identity, and failed readiness
never reaches the state-changing command. Cleanup is successful only when `aspire ps --format Json`
independently reports no AppHost and all discovered ports are closed; the no-URL case additionally
requires proof that no host start was attempted after the confirmed cold stop. After confirmed shutdown,
capture removes only `nr.db`, `nr.db-shm`, and `nr.db-wal` files that were absent before and created by
this invocation, preserves every pre-existing file byte-for-byte, then requires the sealed runtime scope to be clean again. Aspire machine output must be
one authoritative JSON document; prefixed or trailing JSON cannot hide host state.

Required pact path: `tests/Hexalith.FrontComposer.Shell.Tests/Pact/*.json`
Required manifest: `tests/Hexalith.FrontComposer.Shell.Tests/Pact/interaction-manifest.json`
Required provider-state catalog: `tests/Hexalith.FrontComposer.Shell.Tests/Pact/provider-state-catalog.json`

Ownership split: FrontComposer generates consumer pacts, preserves the byte-identical historical
snapshot, captures current evidence outside that tree, and validates both lanes independently.
Deterministic provider states remain owned by
the EventStore HTTP pipeline/test host so setup, teardown, health probing, port allocation, and
stale-process detection are verified beside the provider. Regenerating this evidence therefore
requires a fresh EventStore-owned run; see `docs/reference/pact-contracts.md`. Identity v2 additionally
binds the deterministic fixed-scope runtime-input manifest, pre-frozen actor/source policy and roster,
ordered decision/subject timestamps, effective default or OI-18 roles, and post-subject approval receipts.
The fixed runtime scope includes `src/**`, `samples/Counter/**`, root build/package/toolchain files
(including an explicit present-or-absent `Directory.Build.rsp` coordinate), Pact inputs, and every
root-declared runtime dependency gitlink (Builds, EventStore, Tenants, Parties, Memories, Commons, and
PolymorphicSerializations). Hidden Git index flags, recognized but undeclared root build controls, and
worktree/index/HEAD byte mismatches reject generation before its atomic manifest replacement. Each
dependency checkout must be a real directory at its indexed root gitlink, with no hidden flags,
initialized nested submodule, symlinked input, untracked build input, or byte/index/HEAD drift. Active
validation accepts only the identity-owned canonical Pact directory. Manifest temporary files are
created exclusively under unpredictable sibling names. Approval and OI-18 receipts use validator-pinned
exact affirmative statements; repository-authored text cannot contradict an approving decision.
