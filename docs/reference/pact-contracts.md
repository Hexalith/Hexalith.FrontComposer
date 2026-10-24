---
title: "EventStore Pact Contracts"
description: "File-based Pact evidence for the FrontComposer and Hexalith.EventStore REST contract."
genre: reference
audience: adopter
ownerStory: 11-25-current-eventstore-release-identity-and-evidence
status: published
reviewed: 2026-10-23
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
pwsh ./eng/validate-contract-artifacts.ps1
git diff -- tests/Hexalith.FrontComposer.Shell.Tests/Pact
```

Review pact diffs as API evidence. Expected diffs should name the interaction, method/path, expected status/header/body shape, provider state, owning acceptance criteria, adapter path, and classifier expectation. Unexpected diffs block the change until the adapter, test, or provider-state fixture is corrected.

The validator checks that `interaction-manifest.json` exactly matches the committed pact interactions by description, provider state, method, and path. Missing or orphaned manifest entries fail the lane.

## Provider Verification

Provider verification belongs beside the `Hexalith.EventStore` provider host because PactNet's native verifier must call a real loopback TCP endpoint. Do not use ASP.NET Core `TestServer` or `WebApplicationFactory` for Pact verifier playback.

The EventStore-owned command shape is recorded in `provider-verification-handoff.md`. Live compatibility uses the committed pacts plus `interaction-manifest.json` and `provider-state-catalog.json`.

Quality Gate 2c (`.github/workflows/quality.yml`) runs, in order: the Contract pact tests; the EventStore provider-verification test assembly; the EventStore provider application in `live-compatibility` mode, which writes its report to `artifacts/contracts/provider-verification.json`; `eng/validate-contract-artifacts.ps1`; and the stale-pact diff guard. The provider application exits nonzero on any failed interaction, unsafe host, or incomplete cleanup, and that fails the job. The report is uploaded with the `contract-artifacts` workflow artifact and is never committed.

Run the provider application locally from the repository root:

```bash
set -euo pipefail
workspace_root="$PWD"
report="$workspace_root/artifacts/contracts/provider-verification.json"
dotnet build references/Hexalith.EventStore/tests/Hexalith.EventStore.ProviderVerification.Tests/Hexalith.EventStore.ProviderVerification.Tests.csproj \
  --configuration Release -m:1 -p:NuGetAudit=false
mkdir -p "$(dirname "$report")"
(
  cd references/Hexalith.EventStore
  dotnet "$workspace_root/references/Hexalith.EventStore/tests/Hexalith.EventStore.ProviderVerification/bin/Release/net10.0/Hexalith.EventStore.ProviderVerification.dll" \
    --verification-mode live-compatibility \
    --pact-directory "$workspace_root/tests/Hexalith.FrontComposer.Shell.Tests/Pact" \
    --manifest "$workspace_root/tests/Hexalith.FrontComposer.Shell.Tests/Pact/interaction-manifest.json" \
    --provider-state-catalog "$workspace_root/tests/Hexalith.FrontComposer.Shell.Tests/Pact/provider-state-catalog.json" \
    --report-output "$report"
)
pwsh -NoLogo -NoProfile -File ./eng/validate-contract-artifacts.ps1 -RequireProviderVerification
```

`-RequireProviderVerification` additionally requires that report to exist, be non-empty, and pass the same redaction scan as the committed contract files.

NFR55 release rule: a release is blocked unless the checked-in pacts verify against the pinned EventStore provider version, or a named contract-drift issue explicitly blocks the release.

### Recording a verification

Proof is the repository's tests plus CI. The story or spec that needs a provider verification records the commands run, the pass/fail counts, the commit SHA, and the CI run link. Provider reports, package ledgers, AppHost smoke packets, runtime-identity records, and approval receipts are not committed: every `evidence/` folder is ignored local scratch, and `eng/check-committed-files.sh` rejects evidence paths and files over 1 MiB in the pre-commit hook and in CI.

Re-verification rules:

1. Pact, manifest, or provider-state changes require a fresh provider run.
2. An EventStore submodule or package change requires a fresh provider run.
3. A failed provider run blocks the change until the adapter, test, provider-state fixture, or a named contract-drift issue resolves it.

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
