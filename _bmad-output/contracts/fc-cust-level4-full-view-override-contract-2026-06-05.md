# FC-CUST Level-4 Full-View Override Contract

Date: 2026-06-05
Status: confirmed-runtime
Scope: FC-CUST v1 Level-4 full projection-view overrides.
Owner: FrontComposer Epic 6, Story 6.3.

## Story Number Reconciliation

The live source and tests still contain historical labels such as `Story 6-4`, `DN*`, `P*`, and `AC*`. Under the current Epic 6 plan, Level-4 full-view overrides are Story 6.3. This artifact maps the existing implementation to Story 6.3 without relabeling stable source comments.

## Registration Contract

Status: confirmed-stable.

- Adopters register one full projection-body replacement through `AddViewOverride<TProjection,TComponent>(ProjectionRole? role = null, ...)`.
- Descriptor-list registrations use `ProjectionViewOverrideDescriptorSource` and `ProjectionViewOverrideDescriptor`.
- `ProjectionViewOverrideDescriptor` carries metadata only: projection type, optional role, component type, packed contract version, and registration source.
- `ProjectionViewOverrideContractVersion.Current` is `1_000_000`.
- `AddViewOverride` self-registers `IProjectionViewOverrideRegistry`; `AddHexalithFrontComposer` / Quickstart also registers the Level-2, Level-3, and Level-4 registries.
- `ProjectionViewOverrideDescriptorSource` defensive-copies its descriptor input.

Evidence:

- Source: `src/Hexalith.FrontComposer.Contracts/Rendering/ProjectionViewOverrideDescriptor.cs` defines immutable descriptor metadata and excludes render/item/scoped state.
- Source: `src/Hexalith.FrontComposer.Contracts/Rendering/ProjectionViewOverrideContractVersion.cs` defines the packed version.
- Source: `src/Hexalith.FrontComposer.Contracts/Rendering/ProjectionViewContext.cs` defines the per-render replacement context.
- Source: `src/Hexalith.FrontComposer.Contracts/Rendering/IProjectionViewOverrideRegistry.cs` defines exact-role then any-role resolution and singleton cache-safety constraints.
- Source: `src/Hexalith.FrontComposer.Shell/Extensions/ProjectionViewOverrideServiceCollectionExtensions.cs` builds the descriptor and self-registers the registry.
- Source: `src/Hexalith.FrontComposer.Shell/Extensions/ServiceCollectionExtensions.cs` registers `IProjectionViewOverrideRegistry` in the quickstart graph.
- Source: `src/Hexalith.FrontComposer.Shell/Services/ProjectionViewOverrides/ProjectionViewOverrideDescriptorSource.cs` copies descriptor inputs.
- Sample: `samples/Counter/Counter.Web/Program.cs` registers `CounterFullViewReplacement`.
- Test: `tests/Hexalith.FrontComposer.Contracts.Tests/Rendering/ProjectionViewOverrideContractsTests.cs` pins descriptor/context/version shape.
- Test: `tests/Hexalith.FrontComposer.Shell.Tests/Extensions/ProjectionViewOverrideServiceCollectionExtensionsTests.cs` pins registration shape, self-registration, descriptor-source defensive copy, and version drift.

## Resolution Semantics

Status: confirmed-stable.

- Resolution key is `(projectionType, role)`.
- Exact role wins over role-agnostic registrations.
- Role-agnostic registration is used when no exact-role descriptor exists.
- Invalid components are ignored and generated/template fallback remains available.
- Invalid or major-incompatible contract versions are ignored; minor drift is accepted and logged at Information level.
- Duplicate different components for the same tuple fail deterministically during registry construction with HFC1044.
- Idempotent same-component, same-version re-registration for the same tuple is a no-op.
- Public descriptor snapshots expose only valid, non-ambiguous metadata and are safe to keep in a singleton registry.

Evidence:

- Source: `src/Hexalith.FrontComposer.Shell/Services/ProjectionViewOverrides/ProjectionViewOverrideRegistry.cs` implements exact-role lookup, any-role fallback, invalid component rejection, version handling, duplicate hard-fail, and idempotent same-registration handling.
- Test: `ProjectionViewOverrideServiceCollectionExtensionsTests.Registry_RoleSpecificOverride_WinsBeforeRoleAgnosticOverride` pins exact-role precedence and any-role fallback.
- Test: `ProjectionViewOverrideServiceCollectionExtensionsTests.Registry_DuplicateDifferentComponent_FailsHardOnConstruction` pins HFC1044 startup hard-fail.
- Test: `ProjectionViewOverrideServiceCollectionExtensionsTests.Registry_IdempotentReRegistration_KeepsSingleDescriptor` pins idempotent re-registration.
- Test: `ProjectionViewOverrideServiceCollectionExtensionsTests.Registry_InvalidComponent_IsIgnored_AndGeneratedFallbackCanRun` pins invalid component fallback.
- Test: `ProjectionViewOverrideServiceCollectionExtensionsTests.DescriptorSource_DefensiveCopiesInputList` pins source snapshot immutability.
- Test: `ProjectionViewOverrideServiceCollectionExtensionsTests.Registry_MinorContractVersionDrift_LogsHfc1045Information_AndDescriptorIsAccepted` pins minor drift acceptance.

## Render Contract

Status: confirmed-stable.

- Generated projection views resolve Level 4 before Level 2 and generated default body.
- The generated `defaultBody` render fragment is created before override lookup and bypasses the active Level-4 descriptor for the same projection/role.
- A Level-4 replacement receives a fresh `ProjectionViewContext<TProjection>` per render.
- The replacement is hosted through `FcProjectionViewOverrideHost<TProjection>` with explicit `Descriptor` and `Context` parameters.
- Framework-owned envelope remains outside the replacement body: page/shell wrapper, loading/empty policy, lifecycle/render context, grid envelope where generator-owned, authorization/telemetry/disposal hooks, and sibling navigation surfaces.
- Level-3 slots compose only when the Level-4 replacement explicitly calls `Context.FieldRenderer(...)`, `Context.RowRenderer(...)`, `Context.SectionRenderer(...)`, or `Context.DefaultBody`.
- Directly remounting the generated projection component inside a replacement is unsupported because it re-enters the registry and can select the same descriptor again.
- HFC2121 render faults are isolated by `FcProjectionViewOverrideHost`, logged/published with redacted payloads, tenant/user hash-only correlation, once-per-fault-episode logging, and bounded recovery.

Evidence:

- Source: `src/Hexalith.FrontComposer.SourceTools/Emitters/RazorEmitter.cs` builds `defaultBody`, resolves `ProjectionViewOverrideRegistry.Resolve(...)`, constructs `ProjectionViewContext<TProjection>`, renders `FcProjectionViewOverrideHost<TProjection>`, then resolves `ProjectionTemplateRegistry.Resolve(...)` only in the Level-4 miss path.
- Source: `src/Hexalith.FrontComposer.Shell/Components/Rendering/FcProjectionViewOverrideHost.cs` passes `Context`, isolates render faults, redacts logs, hashes tenant/user, publishes HFC2121, and documents the recursion boundary.
- Sample: `samples/Counter/Counter.Web/Components/Replacements/CounterFullViewReplacement.razor` owns body markup, keeps `aria-labelledby`/`aria-live`, and delegates selected fields through `Context.FieldRenderer(...)`.
- Test: `tests/Hexalith.FrontComposer.Shell.Tests/Generated/CounterStoryVerificationTests.cs` pins Level-4 replacement inside the framework envelope, field delegation to Level 3/generated rendering, invalid component fallback, and Level-4 precedence over simultaneous Level-2 registration.
- Test: `tests/Hexalith.FrontComposer.Shell.Tests/Components/Rendering/FcProjectionViewOverrideHostTests.cs` pins context passing, fresh contexts across renders, HFC2121 redaction/publication, descriptor-change recovery, item-churn log suppression, null-context tolerance, and host accessibility fallback shape.
- Test: `tests/Hexalith.FrontComposer.SourceTools.Tests/Emitters/CounterProjectionApprovalTests.cs` pins generated output containing `IProjectionViewOverrideRegistry`, `ProjectionViewContext`, and `FcProjectionViewOverrideHost`.

## Diagnostic Disposition

Status: confirmed-runtime with reserved build descriptors.

| Diagnostic | Current phase | Severity | Current behavior | Fallback | Proving evidence |
|---|---|---|---|---|---|
| HFC1042 | reserved/catalog only | Error descriptor | ID and SourceTools descriptor exist for invalid projection type, but no `[ProjectionViewOverride]` marker/parser/emitter path is implemented or proven. | Not applicable today. | `FcDiagnosticIds.cs`; `DiagnosticDescriptors.cs`; `DiagnosticDescriptorTests`; `Hfc1026ReservationTests`. |
| HFC1043 | startup registry | Warning log | Registry rejects invalid components or missing/wrong public `[Parameter] Context`. | Descriptor ignored; Level 2 or generated default can render. | `ProjectionViewOverrideRegistry.cs`; `CounterStoryVerificationTests.CounterProjectionView_Level4InvalidComponent_LogsHfc1043_AndRendersGeneratedDefault`. |
| HFC1044 | startup registry | Error-shaped hard fail | Different components registered for the same `(projection, role)` tuple are logged and registry construction throws deterministically; same-component re-registration is a no-op. | Startup fails for conflicting registrations; idempotent repeats continue. | `ProjectionViewOverrideRegistry.cs`; `ProjectionViewOverrideServiceCollectionExtensionsTests.Registry_DuplicateDifferentComponent_FailsHardOnConstruction`; `Registry_IdempotentReRegistration_KeepsSingleDescriptor`. |
| HFC1045 | startup registry | Warning / Information | Invalid or major-incompatible versions are ignored and logged; minor drift is accepted and logged at Information. | Invalid/major: descriptor ignored. Minor drift: descriptor remains selectable. | `ProjectionViewOverrideRegistry.cs`; `ProjectionViewOverrideServiceCollectionExtensionsTests.Registry_MinorContractVersionDrift_LogsHfc1045Information_AndDescriptorIsAccepted`. |
| HFC1046 | reserved/adjacent analyzer scope | Warning descriptor | ID and SourceTools descriptor exist, but current accessibility analyzer reports HFC1050-HFC1055 for customization components, including `AddViewOverride` references. HFC1046 is not a proven emitted Level-4 diagnostic today. | Accessibility enforcement belongs to Story 6.4 / HFC1050-HFC1055. | `FcDiagnosticIds.cs`; `DiagnosticDescriptors.cs`; `CustomizationAccessibilityAnalyzer.cs`. |
| HFC2121 | runtime render | Warning log/diagnostic event | Host isolates replacement render faults, redacts raw exception and payload, publishes a diagnostic event once per fault episode, and offers bounded recovery. | Diagnostic panel renders; shell/navigation/sibling surfaces remain alive. | `FcProjectionViewOverrideHost.cs`; `FcProjectionViewOverrideHostTests.Render_ThrowingReplacement_IsolatesFault_AndRendersDiagnosticFallback`; `Render_PersistentlyThrowingReplacement_DoesNotLogPerItemsTick`. |

Build-time SourceTools emission for HFC1042-HFC1046 is not implemented or proven for Level-4 full-view registrations today. There is no `[ProjectionViewOverride]` marker attribute equivalent to `[ProjectionTemplate]`, no Level-4 marker parser/emitter path, and no default-lane build-diagnostic tests for these IDs. This contract therefore records HFC1042/HFC1046 as reserved/catalog and HFC1043-HFC1045 as registry/startup-runtime behavior, not build-confirmed analyzer behavior.

Residual cross-story metadata seam (recorded, not corrected by Story 6.3):

- `docs/diagnostics/HFC1042.md` through `HFC1046.md` front matter and narrative still label the producer as `SourceTools` and refer to SourceTools emission.
- `docs/diagnostics/diagnostic-registry.json` records HFC1042-HFC1046 with SourceTools owner/severity fields.
- `src/Hexalith.FrontComposer.SourceTools/AnalyzerReleases.Unshipped.md` lists HFC1042-HFC1046 as `HexalithFrontComposer` analyzer diagnostics.
- `_bmad-output/project-docs/api-contracts.md` lists HFC1042-HFC1049 as Level-4/dev-mode reserved.

Open follow-up:

- Owner: Story 9-4/9-5 diagnostic-ID-system owner with Story 6.4 accessibility owner if HFC1046 is folded into the HFC1050-HFC1055 analyzer lane.
- Reason: reconcile catalog/front-matter/release-table ownership and phase with actual runtime/startup behavior, or add a narrow compile-visible Level-4 validation path with default-lane build-diagnostic tests.
- Risk: adopters may expect HFC1042-HFC1046 to appear during compilation when the live Level-4 registration path currently reports only registry/startup/runtime outcomes for HFC1043-HFC1045 and reserves HFC1042/HFC1046.
- Follow-up story: design Level-4 registration validation without broad third-party analyzer package creep, `CompilationProvider` drift coupling, or `ISymbol` escaping parse/analysis state; update diagnostic catalog fields in the same change.

## Cache Safety

Status: confirmed-stable.

- Runtime descriptors contain type metadata only and do not capture projection instances, render fragments, tenant/user state, localized strings, scoped services, or item payloads.
- Registry lifetime is singleton-safe because `ProjectionViewContext<TProjection>` and `RenderContext` values are constructed per render by generated output and never cached by the registry.
- Descriptor-source defensive copy prevents caller mutation from changing the registry input after construction.

Evidence:

- Source: `ProjectionViewOverrideDescriptor.cs`; `IProjectionViewOverrideRegistry.cs`; `ProjectionViewOverrideDescriptorSource.cs`.
- Source: `RazorEmitter.cs` constructs `ProjectionViewContext<TProjection>` inside the render path.
- Test: `ProjectionViewOverrideServiceCollectionExtensionsTests.DescriptorSource_DefensiveCopiesInputList`.

## Non-Goals and Boundaries

Status: confirmed-stable.

- Level-2 marker diagnostics are out of scope except as precedence source; follow Story 6.1.
- Level-3 field-slot behavior is out of scope except for explicit delegated composition through generated delegates; follow Story 6.2.
- HFC1050-HFC1055 accessibility-safety analyzer changes are Story 6.4.
- MCP resource security/schema negotiation changes are out of scope.
- Schema fingerprint changes, `CanonicalSchemaMaterial`, package/version bumps, generated output path changes, pacts, public API baselines, EventStore boundaries, and broad docs-site rewrites are out of scope.
- Published diagnostic catalog rewrites are out of scope for this confirm-and-pin story except for the recorded follow-up above.

## Open Items

- Owner: Story 9-4/9-5 diagnostic-ID-system owner, coordinated with Story 6.4 for accessibility.
- Reason: HFC1042-HFC1046 catalog/doc/release metadata still overstates SourceTools/build-time enforcement relative to the live Level-4 registry/runtime implementation.
- Risk: diagnostic phase confusion for adopters and future agents.
- Follow-up story: reconcile the catalog to runtime/startup disposition or implement/test a narrow Level-4 build-time validation path, then update HFC1042-HFC1046 docs, registry JSON, AnalyzerReleases, and api-contracts together.
