---
title: "EventStore Pact Contracts"
description: "File-based Pact evidence for the FrontComposer and Hexalith.EventStore REST contract."
genre: reference
audience: adopter
ownerStory: 11-24-adopt-the-owner-approved-eventstore-runtime-identity
status: published
reviewed: 2026-08-29
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

The EventStore-owned command shape and the preserved run are recorded in `provider-verification-handoff.md`. The run uses the committed pacts plus `provider-state-catalog.json` and produces a bounded report artifact.

CI is split deliberately: EventStore owns provider execution over real loopback TCP. FrontComposer owns the byte-identical evidence snapshot, verifies its SHA-256 manifest, validates all 19 interactions and cleanup events, scans it for redaction leaks, and uploads it with the consumer artifacts. A missing, incomplete, unbounded, unbound, or unsafe report fails closed.

Story 11.24 treats the preserved compatibility verdict as non-authorizing evidence: its failures do not revoke the separately approved runtime identity. Contract/API reconciliation and any broader release disposition remain separately approved work.

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
