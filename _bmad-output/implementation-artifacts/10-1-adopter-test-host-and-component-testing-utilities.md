# Story 10.1: Adopter Test Host & Component Testing Utilities

Status: ready-for-dev

> **Epic 10** - Framework Quality & Adopter Confidence. Covers **FR71**, **NFR51**, **NFR52**, and **NFR53**. Promotes existing internal test-host patterns into an adopter-facing testing package while preserving the current Shell and SourceTools test infrastructure. Applies lessons **L01**, **L06**, **L07**, **L08**, **L10**, and **L11**.

---

## Executive Summary

Story 10-1 makes FrontComposer testable by adopters without requiring them to run a full application:

- Add or complete the `Hexalith.FrontComposer.Testing` package as the supported test-host surface for adopter component tests.
- Promote the useful parts of the existing internal `FrontComposerTestBase`, generated component bUnit bases, in-memory storage, fake user context, registry fakes, command/query doubles, and SignalR fault harnesses into reusable public test utilities.
- Keep framework internals and adopter helpers separated: adopter tests consume stable builders, fakes, assertions, and render helpers; internal Shell/SourceTools tests may keep deeper hooks.
- Provide generated DataGrid and customization override assertions for columns, headers, cells, badges, empty states, lifecycle wrapper context, density/theme context, render context, and accessibility attributes.
- Add package smoke tests, sample adopter tests, docs snippets, and coverage reporting guardrails so the utilities do not silently rot.

---

## Story

As a developer,
I want a test host and utilities that let me write component tests for my customization overrides and auto-generated views,
so that I can verify my customizations work correctly without manually running the application.

### Adopter Job To Preserve

An adopter should be able to create an xUnit v3 + bUnit test project, reference `Hexalith.FrontComposer.Testing`, inherit from or compose `FrontComposerTestBase`, register their generated domain assembly and customization overrides, render a generated view or override component, and make deterministic assertions without live EventStore, SignalR, browser localStorage, DAPR, authentication infrastructure, or a running app host.

---

## Dev Agent Cheat Sheet

| Area | Required outcome |
| --- | --- |
| Package | Create or complete `src/Hexalith.FrontComposer.Testing/Hexalith.FrontComposer.Testing.csproj` and include it in the solution. It references Contracts, Shell, SourceTools as needed for tests, and may reference EventStore/Mcp only for adapter doubles that are genuinely required. |
| Public base | Ship `FrontComposerTestBase` as an optional public base class and a composable service builder so adopters can avoid inheritance. Do not expose `tests/` project internals directly. |
| Existing reuse | Start from `tests/Hexalith.FrontComposer.Shell.Tests/FrontComposerTestBase.cs`, `GeneratedComponentTestBase.cs`, `CommandRendererTestBase.cs`, and `Infrastructure/EventStore/FaultInjection/*`; extract stable patterns instead of duplicating ad hoc setup. |
| Test stack | Use current repo pins from `Directory.Packages.props`: `xunit.v3` 3.2.2, `xunit.runner.visualstudio` 3.1.5, `bunit` 2.7.2, `Shouldly` 4.3.0, `NSubstitute` 5.3.0, `Microsoft.NET.Test.Sdk` 18.3.0. Older architecture text mentions xUnit v2; the repository has already moved to xUnit v3, so do not downgrade. |
| Default services | Preconfigure Fluxor features, `InMemoryStorageService`, fake user/tenant context, fake override/template/slot registries, fake command dispatcher/service, fake query executor/page loader, lifecycle services, feedback publisher, auth redirector, localization, logging, options, Fluent UI, density/theme state, and loose JS interop defaults. |
| Generated views | Provide helpers for generated projection/DataGrid rendering and assertions: column count, headers, cells, formatting, status badges, empty states, virtualization-lane data, lifecycle wrapper presence, and accessibility attributes. |
| Customization overrides | Provide test descriptors/builders for Level 1 annotations, Level 2 typed templates, Level 3 slots, and Level 4 view overrides. Preserve existing contract-version checks and accessibility diagnostics. |
| Builders | Add domain-model builders for adopter test data, not framework-specific fixture dumping. Builders must support deterministic IDs, timestamps, tenant/user, badge states, nulls, unsupported fields, and collection counts. |
| Containment | No live server, DAPR sidecar, real localStorage, recursive submodule traversal, network, machine-specific paths, or hidden global state in default tests. |
| Verification | Add package-level tests, sample adopter tests, and CI/package smoke validation. Keep coverage thresholds measurable but do not implement the later Epic 10 mutation/Pact/flaky governance stories here. |

Start here: T1 package surface -> T2 service builder/base class -> T3 projection/customization helpers -> T4 builders/assertions -> T5 fault/query doubles -> T6 docs/sample -> T7 packaging/CI validation.

---

## Acceptance Criteria

| AC | Given | When | Then |
| --- | --- | --- | --- |
| AC1 | An adopter references the framework test host package from an xUnit v3 test project | The test project restores and builds | `FrontComposerTestBase` and composable setup extensions are available without referencing internal framework test projects. |
| AC2 | `FrontComposerTestBase` initializes | A bUnit test context is created | Services include Fluxor store/features, `InMemoryStorageService`, fake `IUserContextAccessor`, fake override/template/slot registries, fake command/query/page-loader seams, lifecycle services, logging, localization, options, Fluent UI components, and deterministic default test values. |
| AC3 | A test needs custom service replacements | The adopter configures the test host before first render/store initialization | Replacements are honored, the service provider is not prematurely locked, and store initialization is idempotent. |
| AC4 | A customization override component is rendered | The test uses the package helpers | Lifecycle wrapper context, density context, theme context, render context, tenant/user context, and override registry context are available in the component tree. |
| AC5 | A Level 2 typed template, Level 3 slot, or Level 4 view override is registered | Generated view rendering resolves overrides | The helper validates projection type, role, contract version, and accessibility metadata before rendering. |
| AC6 | A generated DataGrid view is rendered with mock projection data | The adopter asserts the output | Helpers can assert column count, canonical headers, cell values, formatting, badge states, empty-state behavior, row details, and accessibility attributes. |
| AC7 | Mock projection data contains nulls, enums, collections, IDs, currency/number/date values, unsupported-field placeholders, and status badge states | The generated view renders | Builders and assertions cover the same formatting categories used by SourceTools and Shell tests, with deterministic culture/time behavior. |
| AC8 | A generated command component is rendered | The adopter submits or inspects the form | The fake command service/dispatcher records command payload, tenant/user, correlation/message IDs, validation/rejection outcome, and lifecycle transitions without reaching a live EventStore. |
| AC9 | A generated projection view requests query data | The fake query/page-loader seam is used | Tests can provide deterministic success, empty, not-modified, error, cancellation, slow-query, and stale-cache responses without network access. |
| AC10 | A SignalR/reconnection scenario is tested through the test package | Fault harness helpers are used | The package can simulate drop, delay, partial delivery, reorder, and reconnect nudges using the existing Story 5-7 fault-injection model, without requiring a live hub. |
| AC11 | The adopter uses builders | Test data is created | Builders follow the project naming convention and generate deterministic domain models with explicit tenant/user, timestamps, IDs, badge values, and optional edge cases. |
| AC12 | Coverage is measured | The package and sample tests run under the repository coverage command | Core framework unit coverage remains measurable against NFR51, generated component coverage remains measurable against NFR52, and API-boundary integration coverage can be reported for NFR53 without this story inventing new global gates. |
| AC13 | The test package is packed | `dotnet pack` runs | The package includes only required runtime/test assets and XML docs; it excludes repo `tests/`, samples not meant for packaging, `bin`, `obj`, local settings, screenshots, submodule internals, and generated temporary artifacts. |
| AC14 | The package is consumed by a sample adopter test project | CI runs the sample | The sample restores from the locally packed package or project reference, renders one override and one generated DataGrid, and passes without referencing internal test assemblies. |
| AC15 | A test fails because required setup is missing | The failure is reported | Errors identify the missing service or invalid override/registry setup and suggest the relevant setup method; failures do not surface as null-reference noise. |
| AC16 | The package is used in parallel tests | Multiple bUnit contexts run | No static mutable state, shared fake queues, shared tenant/user values, or JSInterop assumptions leak between tests. |
| AC17 | The repo has root-level submodules | Test discovery and package validation run | They do not initialize or update nested submodules and do not scan nested repository internals unless a test explicitly targets a root-level submodule contract. |
| AC18 | Documentation snippets are published for the test host | Docs/sample validation runs | Snippets compile against the package, use xUnit v3 + bUnit 2.7.2, and demonstrate base-class and composable setup usage. |

---

## Tasks / Subtasks

- [ ] T1. Add the adopter-facing testing package surface (AC1, AC13, AC14)
  - [ ] Create `src/Hexalith.FrontComposer.Testing/Hexalith.FrontComposer.Testing.csproj` if it does not already exist.
  - [ ] Add the project to `Hexalith.FrontComposer.sln` and Central Package Management only where needed.
  - [ ] Reference Contracts and Shell directly; reference SourceTools only for generated-component test helpers that truly need generator-driver support.
  - [ ] Keep test-only package references explicit: bUnit, xUnit v3 abstractions where required, Shouldly only if public assertion helpers expose it, NSubstitute only if fakes depend on it.
  - [ ] Add XML docs for public test-host APIs; do not enable broad CS1591 cleanup outside this package.
  - [ ] Add pack metadata and content exclusions so package output is deterministic and does not include repo-local artifacts.

- [ ] T2. Promote `FrontComposerTestBase` without leaking internal test project details (AC1-AC4, AC15, AC16)
  - [ ] Extract stable setup from `tests/Hexalith.FrontComposer.Shell.Tests/FrontComposerTestBase.cs` into the Testing package.
  - [ ] Provide both an optional base class and service-collection/setup extensions such as `AddFrontComposerTestHost(...)`.
  - [ ] Preserve delayed/idempotent Fluxor store initialization so adopters can replace services before first render.
  - [ ] Register default fake tenant/user context with explicit `TestTenantId` and `TestUserId`; require explicit opt-in for null/invalid context scenarios.
  - [ ] Register logging, localization, options, Fluent UI components, loose JS interop defaults, in-memory storage, lifecycle services, feedback publisher, auth redirector, and projection connection state.
  - [ ] Add diagnostics or setup validation for missing service seams instead of allowing null-reference failures.
  - [ ] Add parallel-test isolation tests that create multiple contexts with different tenants/users and verify no state leaks.

- [ ] T3. Add generated projection/DataGrid test helpers (AC4, AC6, AC7, AC9, AC12)
  - [ ] Extract stable patterns from `GeneratedComponentTestBase.cs` without copying internal-only Shell test hacks.
  - [ ] Provide a generated view host helper that can register domain manifests, loaded-page state, DataGrid navigation state, expanded-row state, reconciliation state, fallback scheduler, page loader, and template/slot/view override registries.
  - [ ] Add DataGrid assertions for column count, canonical headers, cell text, formatted values, badge states, empty placeholder, row-detail visibility, loading skeleton, slow-query notice, and accessibility attributes.
  - [ ] Provide deterministic culture/time configuration helpers for number, currency, date/time, relative-time, enum, collection, ID truncation, null, and unsupported-field cases.
  - [ ] Add tests that render at least one generated Counter projection/DataGrid using package helpers, not internal test bases.

- [ ] T4. Add customization override test helpers and contract validation (AC4, AC5, AC6, AC15)
  - [ ] Provide builders/descriptors for Level 1 annotation expectations, Level 2 typed templates, Level 3 field slots, and Level 4 projection view overrides.
  - [ ] Reuse contract concepts from `IProjectionTemplateRegistry`, `IProjectionSlotRegistry`, `IProjectionViewOverrideRegistry`, `ProjectionViewOverrideDescriptor`, `ProjectionViewOverrideContractVersion`, and `RenderContext`.
  - [ ] Validate projection type, role, contract version, context density/theme/read-only values, and accessibility metadata before rendering.
  - [ ] Add negative tests for invalid projection type, duplicate override, contract-version mismatch, missing render context, invalid role, and inaccessible override metadata.

- [ ] T5. Add command/query/fault doubles for adopter tests (AC8-AC10, AC15-AC17)
  - [ ] Provide fake command service/dispatcher helpers that capture payload, tenant/user, command name, bounded context, correlation/message IDs, lifecycle sequence, validation outcome, rejection reason, and idempotency state.
  - [ ] Provide fake query/page-loader helpers for success, empty, not-modified, stale-cache, cancellation, error, slow-query, and bounded oversized response cases.
  - [ ] Promote reusable parts of `tests/Hexalith.FrontComposer.Shell.Tests/Infrastructure/EventStore/FaultInjection/*` for SignalR/reconnection simulation while keeping internal trace files out of the package.
  - [ ] Ensure fault helpers do not require live SignalR, network, DAPR, EventStore, or submodule initialization.
  - [ ] Add tests for drop, delay, partial delivery, reorder, reconnect, cancellation, and deterministic disposal behavior.

- [ ] T6. Add builders, samples, and docs snippets (AC7, AC11, AC14, AC18)
  - [ ] Add domain model builders for representative projection and command models, including deterministic IDs, timestamps, tenant/user, badges, nulls, enums, collections, and unsupported fields.
  - [ ] Keep builders composable and adopter-oriented; do not expose internal SourceTools IR mutation helpers as the primary API.
  - [ ] Add a sample adopter test project or sample folder that references the package and contains one override test and one generated DataGrid test.
  - [ ] Add documentation snippets for inheritance and composition usage. Snippets must compile against xUnit v3 + bUnit 2.7.2.
  - [ ] Use naming convention `{Method}_{Scenario}_{Expected}` or `Should_{Behavior}_When_{Condition}` consistently in new tests.

- [ ] T7. Add package, coverage, and CI validation (AC12-AC18)
  - [ ] Add tests for the Testing package public API surface and setup validation messages.
  - [ ] Add `dotnet pack` validation for the Testing package.
  - [ ] Add local package-consumption smoke validation using the sample adopter tests.
  - [ ] Wire CI to run the package tests and smoke validation without introducing the later Epic 10 CI governance machinery.
  - [ ] Verify coverage collection can include core unit coverage, generated component bUnit coverage, and API-boundary integration coverage reports, but keep enforcement thresholds aligned with existing repo policy until a dedicated gate exists.
  - [ ] Add package artifact hygiene checks for `tests/`, `bin`, `obj`, `.git`, local settings, screenshots, submodule internals, and temp files.

- [ ] T8. Final verification and handoff (AC1-AC18)
  - [ ] Run `dotnet restore Hexalith.FrontComposer.sln`.
  - [ ] Run Testing package tests.
  - [ ] Run Shell generated/component tests touched by extraction.
  - [ ] Run SourceTools generated-output tests if helpers depend on generator output.
  - [ ] Run `dotnet pack src/Hexalith.FrontComposer.Testing/Hexalith.FrontComposer.Testing.csproj`.
  - [ ] Run the sample adopter test project against the packed package or documented project-reference fallback.
  - [ ] Update completion notes with package path, public APIs added, sample tests run, coverage command used, and any deferred package/API decisions.

---

## Dev Notes

### Current Repository State

- `src/Hexalith.FrontComposer.Testing` does not exist at story creation time, but the architecture and PRD reserve `Hexalith.FrontComposer.Testing` for xUnit, bUnit, Playwright, FsCheck, snapshot, and fault-injection test utilities.
- `tests/Hexalith.FrontComposer.Shell.Tests/FrontComposerTestBase.cs` already preconfigures Fluxor, `InMemoryStorageService`, fake user context, `IOverrideRegistry`, logging, options, capability discovery services, projection connection state, DataGrid navigation effects, lifecycle dependencies, feedback publisher, and auth redirector.
- `tests/Hexalith.FrontComposer.Shell.Tests/Generated/GeneratedComponentTestBase.cs` and `CommandRendererTestBase.cs` already carry many generated-component setup requirements. Extract the reusable patterns; do not make adopters reference these internal classes.
- `tests/Hexalith.FrontComposer.Shell.Tests/Infrastructure/EventStore/FaultInjection/*` already implements fault-injection concepts from Story 5-7. Promote only stable helpers needed for adopter testing and keep trace/debug artifacts internal.
- `Directory.Packages.props` currently pins xUnit v3 and bUnit 2.7.2. Some older planning text says xUnit v2; current repository state is authoritative for this story.
- `IStorageService` and `InMemoryStorageService` already live in Contracts. Use them as the default storage seam; do not invent a parallel localStorage abstraction.
- Registry contracts exist for projection templates, slots, and view overrides. Use the existing contract-version and descriptor types rather than creating Testing-only duplicates.

### Architecture and Package Boundaries

| Surface | Story 10-1 responsibility |
| --- | --- |
| `src/Hexalith.FrontComposer.Testing` | Public adopter-facing test host, setup extensions, builders, fakes, assertions, docs snippets, package metadata. |
| `tests/Hexalith.FrontComposer.*.Tests` | Internal tests may be refactored to consume the package when useful, but are not the public API. |
| `src/Hexalith.FrontComposer.Contracts` | Stable contracts only when a missing testing seam is truly framework-wide. Avoid broad contract churn. |
| `src/Hexalith.FrontComposer.Shell` | Runtime components and services. Do not move runtime code into Testing. |
| `src/Hexalith.FrontComposer.SourceTools` | Generator/IR internals. Testing package may provide helpers that consume generated artifacts, not a second generator. |
| `Hexalith.EventStore` submodule | Do not initialize nested submodules or package submodule internals. Use fakes/doubles for adopter tests. |

### Public API Shape Guidance

Prefer a small, stable API set:

- `FrontComposerTestBase` for inheritance-based bUnit tests.
- `FrontComposerTestHostBuilder` or `IServiceCollection` extensions for composition-based setup.
- `FrontComposerTestOptions` for tenant/user, culture/time, default density/theme, JSInterop mode, and store initialization mode.
- `ProjectionTestDataBuilder<TProjection>` / domain-specific examples for deterministic projection data.
- `CommandTestDataBuilder<TCommand>` or command capture helpers for generated command components.
- `GeneratedProjectionAssertions` and `GeneratedCommandAssertions` for high-signal DOM assertions.
- Fake command/query/page-loader/fault providers with explicit captured evidence objects.

Do not expose internal Fluxor reducer/effect classes as the main API. Adopters should configure scenarios, not know the Shell implementation graph.

### Test and Coverage Guidance

- NFR51: keep unit coverage for generator core, command pipeline, and SignalR reconnection measurable through existing coverage tooling.
- NFR52: generated component bUnit coverage target starts at 15 percent line coverage for auto-generated Razor components. Story 10-1 should make this measurable and demonstrate representative coverage; it does not need to solve full coverage governance.
- NFR53: API-boundary integration tests require at least three tests per boundary. Story 10-1 should provide utilities that make those tests easy; Pact/provider governance belongs to Story 10-3.
- Current test naming commonly uses `{Method}_{Scenario}_{Expected}`. Continue that pattern unless a specific project already uses `Should_{Behavior}_When_{Condition}` consistently.

### Latest Technical Notes

- NuGet listed `bunit` 2.7.2 with computed `net10.0` compatibility in May 2026.
- NuGet listed `xunit.v3` 3.2.2 and `Microsoft.NET.Test.Sdk` 18.3.0 in May 2026; these match current repository pins.
- bUnit's current `BunitContext`/test authorization APIs support service-provider setup and fake auth state through test doubles. Use bUnit APIs rather than hand-rolling a separate component renderer.
- xUnit v3 exposes ambient `TestContext.Current` and the repo already uses `TestContext.Current.CancellationToken`; keep new tests compatible with that pattern.

### Cross-Story Contract Table

| Producer | Consumer | Contract |
| --- | --- | --- |
| Stories 2-3 and 2-4 | Story 10-1 | Command lifecycle services, lifecycle wrapper expectations, correlation/message ID semantics, rejection/idempotency display. |
| Stories 3-1 through 3-6 | Story 10-1 | Theme, density, navigation, storage, command palette, user context, and shell setup dependencies. |
| Stories 4-1 through 4-6 | Story 10-1 | Generated projection/DataGrid rendering, role strategies, formatting, badges, empty states, virtualization, row details, unsupported placeholders. |
| Stories 5-1 through 5-7 | Story 10-1 | EventStore command/query seams, ETag/not-modified behavior, reconnection reconciliation, SignalR fault harness. |
| Stories 6-1 through 6-4 | Story 10-1 | Customization gradient contracts for annotations, typed templates, slots, and full view overrides. |
| Stories 7-1 through 7-3 | Story 10-1 | Fake auth/user/tenant and authorization policy test seams must fail closed by default. |
| Stories 8-1 through 8-6a | Story 10-1 | MCP/schema helpers remain producer-owned; Testing package may provide fakes only when adopter component tests need them. |
| Stories 9-1 through 9-5 | Story 10-1 | Drift diagnostics, CLI/IDE/docs snippets, diagnostic HelpLinkUri, and generated-output docs should consume Testing package examples where relevant. |

### Scope Guardrails

Do not implement these in Story 10-1:

- Pact consumer/provider contract framework. Owner: Story 10-3.
- Mutation testing and property-based idempotency governance. Owner: Story 10-4.
- Flaky quarantine automation and CI diet governance. Owner: Story 10-5.
- Accessibility CI gates and visual specimen verification. Owner: Story 10-2.
- LLM benchmark, signed releases, or SBOM. Owner: Story 10-6.
- A live EventStore, DAPR, SignalR hub, Aspire app host, or browser runner requirement for default component tests.
- Recursive or nested submodule initialization.
- Broad package-train upgrades, especially Roslyn, Fluent UI, Fluxor, bUnit, xUnit, or .NET SDK changes.
- A second runtime registry, second source generator, or second storage abstraction created only for tests.
- Public exposure of internal test-only hacks that lock the current Shell implementation graph.

### Known Gaps / Follow-Ups

| Gap | Owner |
| --- | --- |
| Axe-core and visual screenshot gates for generated UI specimens. | Story 10-2 |
| Pact REST-to-generated-UI contracts and provider verification. | Story 10-3 |
| Stryker.NET mutation score gates and FsCheck command idempotency suites. | Story 10-4 |
| Flaky quarantine automation, reintroduction PRs, and CI duration governance. | Story 10-5 |
| Release package signing, SBOM, and test package provenance evidence. | Story 10-6 |
| Full Playwright adopter helper package shape if it grows beyond smoke tests. | Product/architecture decision after Story 10-2 |

---

## References

- [Source: `_bmad-output/planning-artifacts/epics/epic-10-framework-quality-adopter-confidence.md#Story-10.1`] - story statement and acceptance criteria foundation.
- [Source: `_bmad-output/planning-artifacts/prd/functional-requirements.md#FR71`] - test host and utilities for generated components.
- [Source: `_bmad-output/planning-artifacts/prd/non-functional-requirements.md#Testability`] - NFR51/NFR52/NFR53 coverage and integration expectations.
- [Source: `_bmad-output/planning-artifacts/prd/developer-tool-specific-requirements.md#Package-Strategy`] - `Hexalith.FrontComposer.Testing` package role.
- [Source: `_bmad-output/planning-artifacts/architecture.md#Package-dependency-graph`] - Testing package references all framework surfaces.
- [Source: `_bmad-output/planning-artifacts/architecture.md#Test-Infrastructure-Conventions`] - `FrontComposerTestBase`, bUnit, naming, builders, and coverage conventions.
- [Source: `tests/Hexalith.FrontComposer.Shell.Tests/FrontComposerTestBase.cs`] - current internal test-host setup to extract from.
- [Source: `tests/Hexalith.FrontComposer.Shell.Tests/Generated/GeneratedComponentTestBase.cs`] - current generated projection bUnit setup.
- [Source: `tests/Hexalith.FrontComposer.Shell.Tests/Generated/CommandRendererTestBase.cs`] - current generated command bUnit setup.
- [Source: `tests/Hexalith.FrontComposer.Shell.Tests/Infrastructure/EventStore/FaultInjection/`] - current SignalR/EventStore fault harness patterns.
- [Source: `Directory.Packages.props`] - current package pins for xUnit v3, bUnit, Shouldly, NSubstitute, and Microsoft.NET.Test.Sdk.
- [Source: bUnit `BunitContext` API](https://bunit.dev/api/Bunit.BunitContext.html) - current bUnit test context API.
- [Source: bUnit test authorization docs](https://bunit.dev/docs/test-doubles/auth.html) - current bUnit auth/test-double setup.
- [Source: xUnit v3 release 3.2.2](https://xunit.net/releases/v3/3.2.2) - current xUnit v3 package/release context.
- [Source: NuGet `bunit` 2.7.2](https://www.nuget.org/packages/bunit/2.7.2) - current bUnit package compatibility.
- [Source: NuGet `xunit.v3` 3.2.2](https://www.nuget.org/packages/xunit.v3) - current xUnit v3 package version.

---

## Dev Agent Record

### Agent Model Used

(to be filled in by dev agent)

### Debug Log References

(to be filled in by dev agent)

### Completion Notes List

- 2026-05-07: Story created via `/bmad-create-story 10-1-adopter-test-host-and-component-testing-utilities` during recurring pre-dev hardening job. Ready for party-mode review on a later run.

### File List

(to be filled in by dev agent)
