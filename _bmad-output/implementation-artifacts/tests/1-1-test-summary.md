# Test Automation Summary — Story 1.1: Bootstrap a minimal, bootable shell

**Workflow:** `bmad-qa-generate-e2e-tests` · **Date:** 2026-06-03 · **Engineer:** QA automation (Administrator)
**Framework:** xUnit v3 + Shouldly + NSubstitute + bUnit (existing project stack — no new framework introduced)
**Feature under test:** the three-call DI bootstrap (`AddHexalithFrontComposerQuickstart` → `AddHexalithDomain<T>` → `AddHexalithEventStore`), the AC2 fail-fast guard, and the empty-shell render.

> Story 1.0's summary lives in the sibling `test-summary.md`; this file is Story 1.1's so both are preserved.

## Scope

Story 1.1 shipped with three test files already covering the happy paths (20 tests). This QA pass
**discovered and auto-applied 8 gap tests** against the acceptance criteria and the implementation.
No production code was changed — tests only.

## Discovered gaps — auto-applied

### `Extensions/FrontComposerBootstrapGuardTests.cs` (AC2 guard) — +7 tests

- [x] `Validate_NullMarkers_ThrowsArgumentNullException` — defensive `ArgumentNullException` guard (public method, previously unexercised).
- [x] `Validate_NoMarkersAtAll_ThrowsNamingForgottenQuickstart` — empty marker set → "incomplete", names Quickstart.
- [x] `Gate_StartAsync_MisorderedMarkers_LogsErrorWithNamedMessage_BeforeThrowing` — **Task 1's explicit "write to the logger AND throw" contract**, previously unverified; asserts a single `LogLevel.Error` entry carrying both offending call names.
- [x] `Gate_StartAsync_ValidMarkers_LogsNothing` — no error logged on the happy path.
- [x] `Gate_StartAsync_CancelledToken_ThrowsOperationCanceled` — `StartAsync` honors a cancelled token.
- [x] `Gate_StopAsync_DoesNotThrow` — `IHostedService.StopAsync` no-op contract.
- [x] `EventStoreAlone_RegistersGate_AndValidatorThrowsNamingForgottenQuickstart` — **the headline AC2 "a required call is missing" scenario through the real `AddHexalithEventStore`-only wiring** (the documented reason the gate is registered by all three entry points); previously only proven with synthesized markers.
- [x] `GranularAddHexalithFrontComposer_RegistersQuickstartMarkerAndGate` — the ordering marker lives on the foundational `AddHexalithFrontComposer()` call, not just the Quickstart wrapper, so the granular 3-call path is guarded identically.

### `Extensions/FrontComposerServiceGraphTests.cs` (AC1 service graph) — +1 assertion

- [x] `ThreeCallGraph_ResolvesEndToEndUnderScopeValidation` now also resolves `IQueryService` — AC1 names the "command/query stub path"; only the command half was asserted before.

## Coverage (Story 1.1 acceptance criteria)

| AC | Before | After |
| --- | --- | --- |
| AC1 — three-call graph + frame render | command path, registries, lifetimes, frame, shortcuts | **+ query-service half of the stub path** |
| AC2 — fail-fast guard names the bad call | validator + gate happy/misorder via synthesized markers | **+ log-and-throw, cancellation, stop, null/empty guard, missing-call through real EventStore-only wiring, granular foundational path** |
| AC3 — empty shell renders empty state, nav omitted | empty-state render + nav omitted | unchanged (already complete) |

## Test run

- `dotnet test … -c Release` (Story 1.1 files `FrontComposerBootstrapGuardTests` + `FrontComposerServiceGraphTests` + `Story11BootstrapShellRenderTests`): **28 passed, 0 failed** (was 20; +8 added).
- Regression: Story 1.0 spike suite + `AddHexalithFrontComposerQuickstartTests`: **12 passed, 0 failed**.
- Release build clean (TreatWarningsAsErrors).
- `DiffEngine_Disabled=true` set as required by the project's solution-level test rule.

## Notes

- Followed the repo idiom for hosted-service gates: call `StartAsync`/`StopAsync` directly (no full `IHost`), matching `FrontComposerAuthorizationPolicyCatalogValidatorTests` and the existing guard tests.
- `CapturingLogger<T>` is a small private nested type in the guard test file, mirroring the pattern already used in `EventStoreDiagnosticsTests`.
- The 13 pre-existing full-solution failures recorded in the story (8 Shell + 3 SourceTools + 2 Cli) are out of Story 1.1 scope and untouched by this pass.

## Next steps

- Run in the full CI lane: `DiffEngine_Disabled=true dotnet test Hexalith.FrontComposer.slnx --filter "Category!=Performance&Category!=e2e-palette&Category!=NightlyProperty&Category!=Quarantined"`.
- Add edge cases as the bootstrap surface grows (e.g. multiple `AddHexalithDomain<T>` calls registering one ordering marker).
