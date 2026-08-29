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

NFR55 release rule: a release is blocked unless the checked-in pacts verify against the pinned EventStore provider version, or a named contract-drift issue explicitly blocks the release. Story 11.24 does not change that rule; the preserved compatibility verdict is the named, recorded drift, and it is non-authorizing in the other direction as well - its failures do not revoke the separately approved runtime identity. Contract/API reconciliation and any broader release disposition remain separately approved work.

### Evidence hash domains

The preserved evidence is bound by two different hash domains, and reproducing a hash requires knowing which one applies:

- `sha256-manifest.json` hashes each preserved evidence file's exact bytes. `.gitattributes` marks the evidence tree `-text` so checkout never rewrites them, and every preserved file is byte-identical to the EventStore-owned capture.
- The provider report's `inputHashes` entries of `kind: pact`, `interaction-manifest`, and `provider-state-catalog` hash the CRLF-normalized text of the live committed files under `tests/Hexalith.FrontComposer.Shell.Tests/Pact/`, not their on-disk bytes and not their Git blob ids. This keeps the binding stable across Windows and Linux checkouts of files that are not marked `-text`.

### Re-capturing the evidence

The gate binds the preserved report to live repository bytes on purpose: the provider report's contract inputs must equal the committed pacts, the interaction manifest, and the provider-state catalog, and `apphost-smoke.json` must equal the current `src/Hexalith.FrontComposer.AppHost/Program.cs` and `.csproj`. An ordinary edit to any of those therefore fails Gate 2c, because the preserved evidence no longer describes what is in the tree. There is no in-repo way to weaken that binding; the remedy is to re-capture:

1. Pact, manifest, or provider-state changes require a fresh EventStore-owned provider run over real loopback TCP against the new pacts, and a new preserved report plus run receipt.
2. AppHost topology changes require a fresh AppHost smoke capture against the edited topology.
3. Update `sha256-manifest.json` and the pinned constants in `eng/eventstore_runtime_evidence.py` to the re-captured bytes, and re-run `pwsh ./eng/validate-contract-artifacts.ps1 -RequireProviderVerification`.

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
