# EventStore Provider Verification Handoff

Story: 10-3-consumer-driven-contract-tests-pact (handoff), provider reconciliation live lane
Consumer: Hexalith.FrontComposer.Shell
Provider: Hexalith.EventStore
Interaction count: 19

Provider verification must run in `Hexalith.EventStore` against a real loopback TCP endpoint. Do not use ASP.NET Core `TestServer` or `WebApplicationFactory` for Pact verifier playback, because the native verifier calls an HTTP endpoint.

The pinned EventStore test assembly locates the accepted Pact corpus through its
`references/Hexalith.FrontComposer` gitlink. In a FrontComposer workflow, leave that nested
submodule uninitialized and stage the six current generated Pact JSON files into its empty
worktree. Then run the built provider test assembly from the EventStore repository root:

```powershell
Set-Location references/Hexalith.EventStore
dotnet tests/Hexalith.EventStore.ProviderVerification.Tests/bin/Release/net10.0/Hexalith.EventStore.ProviderVerification.Tests.dll
```

That test invocation does not create the live compatibility report. Run the provider application
separately from the EventStore repository root. Quality Gate 2c writes its report to
`artifacts/contracts/provider-verification.json` and uploads it as a workflow artifact:

```powershell
dotnet tests/Hexalith.EventStore.ProviderVerification/bin/Release/net10.0/Hexalith.EventStore.ProviderVerification.dll --verification-mode live-compatibility <validated canonical inputs>
```

Any failed interaction, unsafe host, incomplete cleanup, or nonzero process exit fails Gate 2c.
Proof of a run is the CI result: the story or spec records the command, pass/fail counts, commit
SHA, and CI run link. Provider reports are never committed.

Required pact path: `tests/Hexalith.FrontComposer.Shell.Tests/Pact/*.json`
Required manifest: `tests/Hexalith.FrontComposer.Shell.Tests/Pact/interaction-manifest.json`
Required provider-state catalog: `tests/Hexalith.FrontComposer.Shell.Tests/Pact/provider-state-catalog.json`

Ownership split: FrontComposer generates the consumer pacts and validates their manifest,
provider-state catalog, and redaction. Deterministic provider states remain owned by the
EventStore HTTP pipeline/test host so setup, teardown, health probing, port allocation, and
stale-process detection are verified beside the provider; see `docs/reference/pact-contracts.md`.
