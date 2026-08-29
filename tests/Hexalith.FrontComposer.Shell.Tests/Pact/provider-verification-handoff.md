# EventStore Provider Verification Handoff

Story: 10-3-consumer-driven-contract-tests-pact (handoff), 11-24-adopt-the-owner-approved-eventstore-runtime-identity (preserved run)
Consumer: Hexalith.FrontComposer.Shell
Provider: Hexalith.EventStore
Interaction count: 19
Release status: provider verification has run. The preserved EventStore-owned report is
`_bmad-output/implementation-artifacts/evidence/frontcomposer-story-11-24/provider-verification/provider-verification.json`
(`finalVerdict: failed`, 19/19 interactions, 19 setup and 19 teardown events, host stopped, port closed).
Its compatibility failures are preserved evidence and do not authorize or revoke the owner-approved
runtime identity; contract/API reconciliation is separately approved work.

Provider verification must run in `Hexalith.EventStore` against a real loopback TCP endpoint. Do not use ASP.NET Core `TestServer` or `WebApplicationFactory` for Pact verifier playback, because the native verifier calls an HTTP endpoint.

Command shape of the preserved run, as recorded by the EventStore-owned run receipt
`_bmad-output/implementation-artifacts/evidence/frontcomposer-story-11-24/provider-verification/run-evidence.json`:

```powershell
dotnet run --project tests/Hexalith.EventStore.ProviderVerification/Hexalith.EventStore.ProviderVerification.csproj --configuration Release --no-build -- <validated canonical inputs>
```

The run exits non-zero (`exitCode: 4`) when interactions fail; that is the truthful compatibility
outcome, not a broken harness. `eng/eventstore_runtime_evidence.py` pins this exact command shape.

Required pact path: `tests/Hexalith.FrontComposer.Shell.Tests/Pact/*.json`
Required manifest: `tests/Hexalith.FrontComposer.Shell.Tests/Pact/interaction-manifest.json`
Required provider-state catalog: `tests/Hexalith.FrontComposer.Shell.Tests/Pact/provider-state-catalog.json`

Ownership split: FrontComposer generates consumer pacts, preserves the byte-identical
EventStore-owned evidence snapshot, and validates it. Deterministic provider states remain owned by
the EventStore HTTP pipeline/test host so setup, teardown, health probing, port allocation, and
stale-process detection are verified beside the provider. Regenerating this evidence therefore
requires a fresh EventStore-owned run; see `docs/reference/pact-contracts.md`.