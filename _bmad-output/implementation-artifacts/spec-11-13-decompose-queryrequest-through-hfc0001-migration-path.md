---
title: 'Story 11.13: Decompose QueryRequest through the HFC0001 migration path'
type: 'refactor'
created: '2026-07-11T00:00:00+02:00'
status: 'in-review'
baseline_revision: 'ff166ac2134b13e839e6e1c9bbab35472ad09019'
review_loop_iteration: 0
followup_review_recommended: false
context:
  - 'references/Hexalith.AI.Tools/hexalith-llm-instructions.md'
  - '_bmad-output/project-context.md'
  - '_bmad-output/contracts/fc-contracts-kernel-split-compatibility-plan-2026-07-05.md'
warnings: [oversized]
---

<intent-contract>

## Intent

**Problem:** `Hexalith.FrontComposer.Contracts.Communication.QueryRequest` mixes canonical projection criteria with EventStore routing, conditional-request, and local cache concerns in one 19-parameter public record. That coupling obscures ownership and makes future query evolution change transport/cache and adopter serialization surfaces together.

**Approach:** Introduce a kernel-safe projection-criteria value and make `QueryRequest` a composed transport/cache envelope, while retaining the released v1.12 flattened constructor, properties, deconstruction, and JSON shape as HFC0001-obsolete migration shims. Migrate repository-owned callers to the composed surface without changing `IQueryService`, EventStore HTTP, MCP, generated output, or Testing callback contracts.

## Boundaries & Constraints

**Always:** Keep both types in the netstandard2.0-clean Contracts kernel; preserve old constructor parameter names/order/defaults, flattened property semantics, 19-value deconstruction, record equality/`with` behavior, and direct JSON compatibility. The canonical criteria own projection type, paging, column/status filters, search, and ordering; the envelope owns tenant, EventStore routing, ETags, and cache metadata. Use HFC0001 with its canonical help link on every legacy entry point, and compile-test legacy versus canonical consumer diagnostics for both Contracts TFMs. Validate against the actual v1.12.0 package baseline.

**Block If:** The old public surface cannot coexist with a coherent canonical composed model without silent data divergence; preserving the released JSON/EventStore shape requires an ambiguous serializer contract; or completion requires changing a root-declared submodule or choosing a new wire/schema version not authorized by this story.

**Never:** Change `IQueryService.QueryAsync`, `TestQueryService.SucceedWith` callback signatures, EventStore request JSON/header/cache-key behavior, MCP resource output, SourceTools dependencies or snapshots, `GridViewPersistenceBlob`, CLI schemas, or the EventStore submodule. Do not remove the flattened API, claim the stale v0.4 removal already happened, or add Blazor/Fluent dependencies to Contracts.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|---------------|---------------------------|----------------|
| Canonical request | Composed criteria plus routing/cache envelope | Shell, MCP, badges, and Testing observe the same query, tenant, paging, ETag, and cache values | Existing validation and failures remain unchanged |
| Legacy source | Existing 19-argument construction, flattened reads, `with`, or deconstruction | Source remains functional and receives HFC0001 on net10; netstandard receives the documented obsolete diagnostic | No runtime failure or state divergence |
| Direct serialization | Legacy or canonical request serialized with repository web JSON options | Existing flat property names, order-sensitive golden shape, defaults, and round-trip values remain stable; no nested duplicate is emitted | Incompatible JSON drift fails the contract test |
| EventStore execution | Filters, paging, routing, validators, and cache discriminator supplied | Existing body, `If-None-Match`, cache eligibility/key/version, pact, and result behavior are byte/semantically equivalent | Existing injection, tenant, cancellation, and protocol-drift guards remain intact |

</intent-contract>

## Code Map

- `src/Hexalith.FrontComposer.Contracts/Communication/{QueryRequest,IQueryService}.cs` -- current public envelope and stable service seam; add the canonical criteria type beside them.
- `src/Hexalith.FrontComposer.Shell/Infrastructure/EventStore/EventStoreQueryClient.cs` -- maps criteria, routing, validators, and cache metadata to the unchanged private EventStore DTO.
- `src/Hexalith.FrontComposer.{Mcp,Shell,Testing}` -- repository-owned constructors/readers to migrate while preserving outward contracts.
- `src/Hexalith.FrontComposer.Contracts/Diagnostics/FcDiagnosticIds.cs` and `docs/diagnostics/{diagnostic-registry.json,HFC0001.md}` -- HFC0001 identity, lifecycle, and migration truth.
- `tests/Hexalith.FrontComposer.{Contracts,SourceTools,Shell,Mcp,Testing}.Tests` -- source/binary/JSON diagnostics, mapping, pact, and consumer evidence.

## Tasks & Acceptance

**Execution:**
- `src/Hexalith.FrontComposer.Contracts/Communication/ProjectionQuery.cs` and `src/Hexalith.FrontComposer.Contracts/Communication/QueryRequest.cs` -- add the immutable canonical criteria record and compose it into the envelope; retain explicit legacy constructor/properties/deconstruct and deterministic equality/`with`/serialization shims with one type per file.
- `src/Hexalith.FrontComposer.Contracts/Communication/IQueryService.cs` and `src/Hexalith.FrontComposer.Contracts/Diagnostics/FcDiagnosticIds.cs` -- document the composed contract and broaden the existing HFC0001 symbol without changing method or diagnostic identity.
- `src/Hexalith.FrontComposer.Shell/Infrastructure/EventStore/EventStoreQueryClient.cs`, `src/Hexalith.FrontComposer.Shell/Badges/EventStoreActionQueueCountReader.cs`, `src/Hexalith.FrontComposer.Mcp/Invocation/FrontComposerMcpProjectionReader.cs`, and `src/Hexalith.FrontComposer.Testing/TestQueryService.cs` -- use canonical criteria internally while preserving routing, legacy `Filter` forwarding, cache safety, evidence, and public callbacks.
- `tests/Hexalith.FrontComposer.Contracts.Tests/Communication/QueryRequestTests.cs` -- pin canonical/legacy defaults, constructor and deconstruct compatibility, `with` synchronization, equality, exact flat JSON and round-trip behavior for null/non-null collections.
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Diagnostics/{DiagnosticRegistryTests,QueryRequestDeprecationTests}.cs` -- compile net10 canonical and legacy consumers to prove HFC0001/no-warning behavior, document the netstandard obsolete identity, and keep registry/docs metadata consistent with a real post-v1 removal target.
- `tests/Hexalith.FrontComposer.Shell.Tests/Infrastructure/EventStore/{EventStoreClientTests,EventStoreQueryCacheIntegrationTests,QueryAndCacheTenantIsolationTests}.cs`, `tests/Hexalith.FrontComposer.Shell.Tests/Badges/EventStoreActionQueueCountReaderTests.cs`, and `tests/Hexalith.FrontComposer.Shell.Tests/Pact/EventStorePactContractTests.cs` -- update construction and add mapping/cache/security equivalence assertions; pact fixtures must not drift.
- `tests/Hexalith.FrontComposer.Mcp.Tests/Invocation/ProjectionReaderTests.cs` and `tests/Hexalith.FrontComposer.Testing.Tests/{FrontComposerTestHostTests,TestingFailureModeTests}.cs` -- prove canonical request capture with unchanged MCP output, Testing callback behavior, evidence, and public API baseline.
- `docs/diagnostics/diagnostic-registry.json` and `docs/diagnostics/HFC0001.md` -- replace the missed v0.4 promise with accurate flattened-query migration steps, compiler-emission semantics, TFM behavior, and an explicit v2.0.0 removal target.

**Acceptance Criteria:**
- Given a canonical composed request or any supported flattened v1.12 construction, when Shell, MCP, badge, and Testing consumers execute it, then the same projection criteria, routing, ETag, cache, tenant, and result behavior is observed.
- Given legacy net10 consumer source and canonical consumer source, when both compile, then only legacy constructor/property use emits HFC0001 with the documented help link; given netstandard2.0, the documented obsolete fallback is emitted and the contract remains loadable.
- Given legacy and canonical instances, when they are deconstructed, copied with changed criteria, compared, and serialized/round-tripped, then legacy values remain synchronized and the established flat JSON shape has no added nested field.
- Given the EventStore pact and cache/tenant/security suites, when requests contain filters, paging, ETags, routing fields, unsafe validators, or cache discriminators, then HTTP body/headers and fail-closed behavior are unchanged and pact artifacts have no diff.
- Given a v1.12.0 `QueryRequest` API-signature fixture, the packed Contracts assembly, and clean consumers, when compatibility checks run, then every retained legacy query signature remains, the new criteria surface is additive, and SourceTools still references only Contracts.

## Spec Change Log

## Review Triage Log

## Design Notes

The canonical `ProjectionQuery` deliberately excludes obsolete `Filter`; the envelope retains it only as an HFC0001 compatibility shim because EventStore still forwards that legacy payload member. The composed property must not add a second serialized representation. Story 11.14 owns broad release/package migration documentation; this story updates only the diagnostic contract needed to make the migration truthful.

## Verification

**Commands:**
- `dotnet restore Hexalith.FrontComposer.slnx -p:Configuration=Release -p:NuGetAudit=false` -- restore succeeds.
- `dotnet build src/Hexalith.FrontComposer.Contracts/Hexalith.FrontComposer.Contracts.csproj -c Release -f netstandard2.0 --no-restore -m:1 /nr:false` -- kernel remains analyzer-host compatible.
- `DiffEngine_Disabled=true dotnet test tests/Hexalith.FrontComposer.Contracts.Tests/Hexalith.FrontComposer.Contracts.Tests.csproj -c Release --no-restore` -- contract, JSON, and compatibility tests pass.
- `DiffEngine_Disabled=true dotnet test tests/Hexalith.FrontComposer.SourceTools.Tests/Hexalith.FrontComposer.SourceTools.Tests.csproj -c Release --no-restore` -- diagnostic registry and compiled-consumer checks pass.
- `for project in Shell Mcp Testing; do DiffEngine_Disabled=true dotnet test "tests/Hexalith.FrontComposer.${project}.Tests/Hexalith.FrontComposer.${project}.Tests.csproj" -c Release --no-restore || exit 1; done` -- focused mapping, pact, MCP, and Testing projects pass.
- `dotnet pack src/Hexalith.FrontComposer.Contracts/Hexalith.FrontComposer.Contracts.csproj -c Release --no-restore` -- the package builds and the focused v1.12 query-signature fixture in Contracts tests reports no removed or changed legacy member.
- `DiffEngine_Disabled=true dotnet test Hexalith.FrontComposer.slnx -c Release --no-restore --filter "Category!=Performance&Category!=e2e-palette&Category!=NightlyProperty&Category!=Quarantined" && git diff --exit-code -- 'tests/Hexalith.FrontComposer.Shell.Tests/Pact/*.json' && git diff --check` -- default lane, Pact JSON fixture stability, and whitespace gate pass.
