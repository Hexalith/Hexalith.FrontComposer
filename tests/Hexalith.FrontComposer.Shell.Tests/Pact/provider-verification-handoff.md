# EventStore Provider Verification Handoff

Story: 10-3-consumer-driven-contract-tests-pact
Consumer: Hexalith.FrontComposer.Shell
Provider: Hexalith.EventStore
Interaction count: 19
Release status: blocked until provider verification runs against the pinned EventStore provider version.

Provider verification must run in `Hexalith.EventStore` against a real loopback TCP endpoint. Do not use ASP.NET Core `TestServer` or `WebApplicationFactory` for Pact verifier playback, because the native verifier calls an HTTP endpoint.

Required command shape:

```powershell
dotnet test Hexalith.EventStore.sln --configuration Release --filter "Category=ContractProvider" -- `
  --pact-source "..\tests\Hexalith.FrontComposer.Shell.Tests\Pact" `
  --provider-state-catalog "..\tests\Hexalith.FrontComposer.Shell.Tests\Pact\provider-state-catalog.json" `
  --report-output "artifacts/contracts/provider-verification.json"
```

Required pact path: `tests/Hexalith.FrontComposer.Shell.Tests/Pact/*.json`
Required manifest: `tests/Hexalith.FrontComposer.Shell.Tests/Pact/interaction-manifest.json`
Required provider-state catalog: `tests/Hexalith.FrontComposer.Shell.Tests/Pact/provider-state-catalog.json`

Blocking reason in this repository: the current FrontComposer repo can generate consumer pacts and validate artifacts, but deterministic provider states must be owned by the EventStore HTTP pipeline/test host so setup, teardown, health probing, port allocation, and stale-process detection are verified beside the provider.