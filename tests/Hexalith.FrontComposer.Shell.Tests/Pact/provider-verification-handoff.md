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
real loopback Kestrel, host stopped, port closed). Its adjacent `run-evidence.json` binds the exact report.
Compatibility evidence records current source/version/Builds provenance without claiming migration approval.

Provider verification must run in `Hexalith.EventStore` against a real loopback TCP endpoint. Do not use ASP.NET Core `TestServer` or `WebApplicationFactory` for Pact verifier playback, because the native verifier calls an HTTP endpoint.

Current command shape, run from the EventStore repository root:

```powershell
dotnet tests/Hexalith.EventStore.ProviderVerification/bin/Release/net10.0/Hexalith.EventStore.ProviderVerification.dll --verification-mode live-compatibility <validated canonical inputs>
```

Any failed interaction, stale input/provenance, unsafe host, incomplete cleanup, or nonzero process exit
rejects the current lane. Gate 2c separately requires a passing authenticated Aspire AppHost smoke;
missing infrastructure or credentials remains a blocker and cannot be relabeled as passing evidence.

Required pact path: `tests/Hexalith.FrontComposer.Shell.Tests/Pact/*.json`
Required manifest: `tests/Hexalith.FrontComposer.Shell.Tests/Pact/interaction-manifest.json`
Required provider-state catalog: `tests/Hexalith.FrontComposer.Shell.Tests/Pact/provider-state-catalog.json`

Ownership split: FrontComposer generates consumer pacts, preserves the byte-identical historical
snapshot, captures current evidence outside that tree, and validates both lanes independently.
Deterministic provider states remain owned by
the EventStore HTTP pipeline/test host so setup, teardown, health probing, port allocation, and
stale-process detection are verified beside the provider. Regenerating this evidence therefore
requires a fresh EventStore-owned run; see `docs/reference/pact-contracts.md`.