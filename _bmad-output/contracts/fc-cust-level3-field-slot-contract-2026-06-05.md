# FC-CUST Level-3 Field-Slot Contract

Date: 2026-06-05
Status: confirmed-runtime
Scope: FC-CUST v1 Level-3 field-slot overrides.
Owner: FrontComposer Epic 6, Story 6.2.

## Story Number Reconciliation

The live source and tests still contain historical labels such as `Story 6-3`, `GB-P*`, and `GC-P*`. Under the current Epic 6 plan, this surface is Story 6.2. This artifact maps the existing implementation to Story 6.2 without relabeling stable source comments.

## Registration Contract

Status: confirmed-stable.

- Adopters register one selected field through `AddSlotOverride<TProjection,TField,TComponent>(...)` or the `Type componentType` overload.
- Selectors must be direct projection property access. Nested members, method calls, captured values, indexers, computed expressions, and unsupported conversions throw `ProjectionSlotSelectorException` with HFC1038-shaped guidance at the call site.
- `ProjectionSlotDescriptor` carries metadata only: projection type, field name, field type, optional role, component type, and packed contract version.
- `ProjectionSlotContractVersion.Current` is `1_000_000`.
- `AddSlotOverride` self-registers `IProjectionSlotRegistry`; `AddHexalithFrontComposer` / Quickstart also registers the Level-2, Level-3, and Level-4 registries.
- `ProjectionSlotDescriptorSource` defensive-copies its descriptor input. The registry then builds a descriptor-only snapshot that excludes ambiguous or rejected entries.

Evidence:

- Source: `src/Hexalith.FrontComposer.Contracts/Rendering/ProjectionSlotDescriptor.cs:26`.
- Source: `src/Hexalith.FrontComposer.Contracts/Rendering/ProjectionSlotSelector.cs:23`.
- Source: `src/Hexalith.FrontComposer.Contracts/Rendering/ProjectionSlotContractVersion.cs:25`.
- Source: `src/Hexalith.FrontComposer.Contracts/Rendering/IProjectionSlotRegistry.cs:15`.
- Source: `src/Hexalith.FrontComposer.Shell/Extensions/ProjectionSlotServiceCollectionExtensions.cs:37`, `:61`, `:82`, and `:83`.
- Source: `src/Hexalith.FrontComposer.Shell/Extensions/ServiceCollectionExtensions.cs:397`.
- Source: `src/Hexalith.FrontComposer.Shell/Services/ProjectionSlots/ProjectionSlotDescriptorSource.cs:18`.
- Sample: `samples/Counter/Counter.Web/Program.cs:77`.
- Test: `tests/Hexalith.FrontComposer.Contracts.Tests/Rendering/ProjectionSlotContractsTests.cs:201`.
- Test: `tests/Hexalith.FrontComposer.Shell.Tests/Extensions/ProjectionSlotServiceCollectionExtensionsTests.cs`.
- Test: `tests/Hexalith.FrontComposer.Shell.Tests/Services/ProjectionSlots/ProjectionSlotRegistryTests.cs:221`.

## Resolution Semantics

Status: confirmed-stable.

- Resolution key is `(projectionType, role, fieldName)`.
- Exact role wins over any-role.
- Any-role is used when no exact-role descriptor exists.
- Different fields do not match.
- Duplicate different components for the same tuple fail closed: `Resolve(...)` returns `null`, the public descriptor snapshot omits the ambiguous entry, and generated default rendering wins.
- Invalid components fail soft: the registry logs HFC1039, ignores the descriptor, and generated default rendering wins.
- Invalid or major-incompatible contract versions fail soft: the registry logs HFC1041, ignores the descriptor, and generated default rendering wins.
- Minor contract drift is accepted and logged at Information level.
- Descriptor snapshots are metadata-only and safe for singleton registry caching.

Evidence:

- Source: `src/Hexalith.FrontComposer.Shell/Services/ProjectionSlots/ProjectionSlotRegistry.cs:30`, `:59`, `:62`, `:88`, `:100`, `:130`, `:144`, and `:164`.
- Test: `tests/Hexalith.FrontComposer.Shell.Tests/Services/ProjectionSlots/ProjectionSlotRegistryTests.cs:14` pins exact-role precedence.
- Test: `tests/Hexalith.FrontComposer.Shell.Tests/Services/ProjectionSlots/ProjectionSlotRegistryTests.cs:25` pins any-role fallback.
- Test: `tests/Hexalith.FrontComposer.Shell.Tests/Services/ProjectionSlots/ProjectionSlotRegistryTests.cs:35` pins different-field misses.
- Test: `tests/Hexalith.FrontComposer.Shell.Tests/Services/ProjectionSlots/ProjectionSlotRegistryTests.cs:43` and `:177` pin duplicate fail-closed behavior and HFC1040 logging.
- Test: `tests/Hexalith.FrontComposer.Shell.Tests/Services/ProjectionSlots/ProjectionSlotRegistryTests.cs:62` and `:162` pin invalid component fallback and HFC1039 logging.
- Test: `tests/Hexalith.FrontComposer.Shell.Tests/Services/ProjectionSlots/ProjectionSlotRegistryTests.cs:107`, `:124`, and `:141` pin invalid, major-incompatible, and minor-drift HFC1041 behavior.

## Render Contract

Status: confirmed-stable.

- Generated views inject `IProjectionSlotRegistry` and emit `HasProjectionSlot(fieldName)` checks before wrapping a field in `FcFieldSlotHost<TProjection,TField>`.
- When no slot exists, generated DataGrid/header/envelope and field rendering stay on the existing generated path.
- When a slot exists, `RenderSlotField` builds a fresh `FieldDescriptor`, render context, and `FcFieldSlotHost` boundary for that field.
- `FcFieldSlotHost` builds a fresh `FieldSlotContext<TProjection,TField>` per render, passes it as the slot component's `Context` parameter, and invokes `Context.RenderDefault` only through the generated non-recursive fallback supplied by the emitter.
- Level-2 template `FieldRenderer` delegates call the same slot-aware field rendering path, so Level 3 composes inside the selected Level-2 template body.
- Story 6.1 precedence remains authoritative: Level 4 full-view override -> Level 2 template -> generated default body. Level 3 composes inside whichever body renders.
- Slot render faults are isolated by an `ErrorBoundary`, logged/published as redacted HFC2115 once per fault episode, and leave generated default rendering available through the lower-level path.
- Missing required host parameters report HFC2120 and render nothing because no safe context can be constructed.

Evidence:

- Source: `src/Hexalith.FrontComposer.SourceTools/Emitters/RazorEmitter.cs:407`, `:505`, `:511`, and `:514`.
- Source: `src/Hexalith.FrontComposer.SourceTools/Emitters/ColumnEmitter.cs:403` and `:409`.
- Source: `src/Hexalith.FrontComposer.SourceTools/Emitters/ProjectionRoleBodyEmitter.cs:722` and `:762`.
- Source: `src/Hexalith.FrontComposer.Shell/Components/Rendering/FcFieldSlotHost.cs:60`, `:80`, `:98`, `:110`, `:120`, `:124`, `:178`, and `:191`.
- Source: `_bmad-output/contracts/fc-cust-override-resolution-and-level2-template-contract-2026-06-05.md`.
- Test: `tests/Hexalith.FrontComposer.Shell.Tests/Components/Rendering/FcFieldSlotHostTests.cs:89`, `:115`, `:152`, and `:202`.
- Test: `tests/Hexalith.FrontComposer.Shell.Tests/Generated/CounterStoryVerificationTests.cs:193` proves one selected field is replaced while adjacent fields and headers remain generated.
- Test: `tests/Hexalith.FrontComposer.Shell.Tests/Generated/CounterStoryVerificationTests.cs:249` proves invalid Counter slot fallback.
- Test: `tests/Hexalith.FrontComposer.Shell.Tests/Generated/CounterStoryVerificationTests.cs:307` proves Level-2 `FieldRenderer` resolves Level-3 slots.

## Diagnostic Disposition

Status: confirmed-runtime.

| Diagnostic | Current phase | Severity | Current behavior | Fallback | Proving evidence |
|---|---|---|---|---|---|
| HFC1038 | call-site/startup | Error-shaped exception | `ProjectionSlotSelector.Parse(...)` / `AddSlotOverride(...)` throws `ProjectionSlotSelectorException` for non-direct selectors. | Registration fails before descriptor creation. | `ProjectionSlotSelector.cs:23`; `ProjectionSlotContractsTests.cs:201`; `ProjectionSlotServiceCollectionExtensionsTests.cs`. |
| HFC1039 | startup/runtime render | Warning | Registry rejects invalid components; host rejects descriptor field-type drift. | Descriptor ignored or default renderer used. | `ProjectionSlotRegistry.cs:144`; `FcFieldSlotHost.cs:110`; `ProjectionSlotRegistryTests.cs:162`; `FcFieldSlotHostTests.cs:115`. |
| HFC1040 | startup | Warning | Registry marks duplicate different-component tuples ambiguous. | `Resolve(...)` returns `null`; generated default renderer wins. | `ProjectionSlotRegistry.cs:164`; `ProjectionSlotRegistryTests.cs:177`. |
| HFC1041 | startup | Warning / Information | Invalid or major-incompatible versions are ignored; minor drift is accepted and logged. | Invalid/major: default renderer wins. Minor drift: slot remains selectable. | `ProjectionSlotRegistry.cs:88`, `:100`, `:130`; `ProjectionSlotRegistryTests.cs:107`, `:124`, `:141`. |
| HFC2120 | runtime render | Warning | Slot host required parameters are missing. | Host renders nothing because no valid context/default renderer can be safely invoked. | `FcFieldSlotHost.cs:80`; `FcFieldSlotHostTests.cs:89`. |
| HFC2115 | runtime render | Warning | Slot component throws during render. | Error boundary isolates the fault and publishes redacted diagnostics; lower-level default renderer remains available through the contract. | `FcFieldSlotHost.cs:124`, `:178`; `FcFieldSlotHostTests.cs:152`, `:202`. |

Build-time SourceTools emission for HFC1038-HFC1041 is not implemented or proven for Level-3 slot registrations today. `src/Hexalith.FrontComposer.SourceTools/Diagnostics/DiagnosticDescriptors.cs:491` through `:527` defines descriptors and `FrontComposerGenerator.cs:375` through `:378` can map diagnostic IDs, but there is no `[ProjectionFieldSlot]` marker parser/emitter or default-lane build-diagnostic test for slot registrations. The Epic wording is therefore recorded as `confirmed-runtime` / `startup-runtime`, not build-confirmed.

Residual cross-story metadata seam (recorded, not corrected by Story 6.2): the adopter-facing narrative bodies of `docs/diagnostics/HFC1038.md`-`HFC1041.md` were corrected by this story to state the current runtime/startup phase. The machine-managed diagnostic catalog still encodes these IDs as build-time SourceTools diagnostics and is **owned by Story 9-4/9-5, not Story 6.2**, so this story deliberately did not edit it:

- `docs/diagnostics/diagnostic-registry.json` carries `ownerPackage: SourceTools`, `compilerSeverity: Error` (HFC1038) / `Warning` (HFC1039-HFC1041), and `runtimeLogLevel: null`.
- The `docs/diagnostics/HFC1038.md`-`HFC1041.md` front-matter (`severity:`, `ownerPackage: SourceTools`, `story-9-5:metadata` block) and `src/Hexalith.FrontComposer.SourceTools/AnalyzerReleases.Unshipped.md` rows likewise label these as `HexalithFrontComposer` build diagnostics.
- `_bmad-output/project-docs/api-contracts.md` lists HFC1041 only as `Warn` and does not record the minor-drift `Information` path.

These remain accurate only once the build-time analyzer/source-generation follow-up below lands; until then they over-state build-time enforcement. Reconciling these catalog fields (severity, `runtimeLogLevel`, owner package) belongs to the Story 9-4/9-5 diagnostic-ID-system owner and the build-time follow-up, not to a confirm-and-pin Level-3 story, because they are cross-checked by the docs-validation gate as a single severity contract.

Open follow-up:

- Owner: Epic 7 authoring/tooling or Story 6.4 if Product decides Level-3 slot validation belongs with customization diagnostics.
- Reason: provide compile-visible analyzer/source-generation feedback for invalid slot registrations without broad Roslyn analyzer package creep.
- Risk: until implemented, adopters see these diagnostics at registration/startup/runtime rather than at build, and the Story 9-4/9-5 catalog (registry JSON, doc front-matter, AnalyzerReleases, api-contracts) keeps a build-time label that the corrected narrative now contradicts.
- Follow-up story: create a narrow slot-registration validation design that keeps SourceTools IR pure/equatable and does not let `ISymbol` escape parse state; coordinate with the Story 9-4/9-5 owner to flip the catalog `compilerSeverity`/`runtimeLogLevel` fields in the same change.

## Cache Safety

Status: confirmed-stable.

- Runtime descriptors contain type metadata only and do not capture projection instances, render fragments, tenant/user state, localized strings, or scoped services.
- Registry lifetime is singleton-safe because `FieldSlotContext` and `RenderContext` values are constructed per render by generated output and `FcFieldSlotHost`.
- `ProjectionSlotDescriptorSource` protects the registry from caller mutation by copying input descriptors.

Evidence:

- Source: `src/Hexalith.FrontComposer.Contracts/Rendering/ProjectionSlotDescriptor.cs:11`.
- Source: `src/Hexalith.FrontComposer.Shell/Services/ProjectionSlots/ProjectionSlotDescriptorSource.cs:18`.
- Source: `src/Hexalith.FrontComposer.Shell/Services/ProjectionSlots/ProjectionSlotRegistry.cs:45`.
- Test: `tests/Hexalith.FrontComposer.Shell.Tests/Services/ProjectionSlots/ProjectionSlotRegistryTests.cs:221`.

## Non-Goals and Boundaries

Status: confirmed-stable.

- Level-4 full-view precedence is out of scope except for citing Story 6.1 precedence.
- HFC1050-HFC1055 accessibility-safety analyzer changes are out of scope. Follow-up: Story 6.4.
- `FcCustomizationDiagnosticPanel` authoring guidance is out of scope beyond the existing runtime panel use for HFC2115.
- This story does not change `CanonicalSchemaMaterial`, schema fingerprints, MCP projection URI/security behavior, package versions, pacts, public API baselines, or generated-output paths.
- Historical source/test labels are not renamed for cosmetic reasons.

