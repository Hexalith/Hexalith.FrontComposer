---
title: 'Replace derivable command prefill reflection with typed emission'
type: 'refactor'
created: '2026-08-28'
status: 'blocked'
baseline_revision: '0bb1d4b5117666bced0383e9a647c4e21bd8937d'
review_loop_iteration: 0
followup_review_recommended: false
context:
  - '{project-root}/_bmad-output/project-context.md'
warnings: []
deferred: []
---

<intent-contract>

## Intent

**Problem:** Generated command renderers discover writable derivable properties with `PropertyInfo.GetProperty` and assign through `PropertyInfo.SetValue`. That late-bound path is trim/AOT-hostile and discards type information already known by the source generator.

**Approach:** Preserve each derivable property's name and fully qualified source type through renderer IR, then emit a deterministic property-name switch whose arms convert and assign directly to the statically known command members. Keep the existing provider ordering, culture, conversion-failure, logging, and refresh-before-submit behavior.

## Boundaries & Constraints

**Always:** Keep SourceTools netstandard2.0-clean and its IR pure/equatable; use ordinal property-name matching and deterministic property order; preserve `CurrentCulture` for numeric/date conversion, case-insensitive enum parsing, invariant Guid text parsing, nullable/null assignment, and the existing narrow conversion-failure handling; update equality/hash code whenever IR changes; run Verify with `DiffEngine_Disabled=true`.

**Block If:** A direct generated assignment cannot compile for a derivable property shape accepted by the parser without adding a new adopter-facing restriction or diagnostic, or preserving existing conversion semantics would require runtime member reflection.

**Never:** Edit the deferred-work ledger; retain or replace member reflection with `dynamic`, expression compilation, `Type.GetProperty`, `PropertyInfo`, or trimmer annotations; change derivable-property classification, density, provider precedence, command dispatch, or unrelated generator output; hand-edit generated `obj/**` files.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Direct value | Provider returns the declared property type | Typed switch assigns the command member and stops provider iteration | No error expected |
| Convertible value | Provider returns supported text/numeric/enum/Guid/date input | Existing culture and parse rules produce the declared value before direct assignment | Narrow conversion failures return `false` and retain warning flow |
| Null value | Target is nullable/reference or non-nullable | Direct arm preserves the prior null/default assignment outcome | No member lookup occurs |
| Unknown name | Name has no emitted arm | Method returns `false` without mutation | Existing not-assigned logging remains authoritative |

</intent-contract>

## Code Map

- `src/Hexalith.FrontComposer.SourceTools/Parsing/AttributeParser.cs` -- parse boundary where Roslyn can capture the unwrapped property's fully qualified source type before symbols are discarded.
- `src/Hexalith.FrontComposer.SourceTools/Parsing/DomainModel.cs` -- `PropertyModel` pure IR and equality/hash contract; carry source type metadata here without leaking `ISymbol`.
- `src/Hexalith.FrontComposer.SourceTools/Transforms/CommandRendererModel.cs` -- renderer IR currently reduces derivable properties to `EquatableArray<string>`; replace that lossy seam with typed property IR.
- `src/Hexalith.FrontComposer.SourceTools/Transforms/CommandRendererTransform.cs` -- preserve `CommandModel.DerivableProperties` instead of rebuilding a name-only array.
- `src/Hexalith.FrontComposer.SourceTools/Emitters/CommandRendererEmitter.cs:304` -- prefill loop and reflective `TrySetPropertyValue`; emit compile-time cases and direct assignments here.
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Emitters/CommandRendererEmitterTests.cs` -- model fixture, parseability/determinism tests, and focused no-reflection/typed-case assertions.
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Emitters/CommandRendererEmitterTests.*.verified.txt` -- eight owned Verify approvals; rerun all snapshot cases and approve only intentional renderer changes.
- `tests/Hexalith.FrontComposer.Shell.Tests/Generated/CommandRendererFullPageTests.cs:81` -- existing runtime proof that a resolved `MessageId` reaches the dispatched command; read-only unless behavior regresses.

## Tasks & Acceptance

**Execution:**
- `src/Hexalith.FrontComposer.SourceTools/Parsing/DomainModel.cs`, `src/Hexalith.FrontComposer.SourceTools/Parsing/AttributeParser.cs` -- retain a fully qualified, symbol-free property type in equatable parse IR.
- `src/Hexalith.FrontComposer.SourceTools/Transforms/CommandRendererModel.cs`, `src/Hexalith.FrontComposer.SourceTools/Transforms/CommandRendererTransform.cs` -- carry `EquatableArray<PropertyModel> DerivableProperties` through renderer transformation and update all consumers/equality members.
- `src/Hexalith.FrontComposer.SourceTools/Emitters/CommandRendererEmitter.cs` -- replace reflective lookup/set with deterministic typed switch emission while preserving conversion and logging semantics.
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Emitters/CommandRendererEmitterTests.cs`, `tests/Hexalith.FrontComposer.SourceTools.Tests/Emitters/CommandRendererEmitterTests.*.verified.txt` -- add regression assertions and refresh the complete eight-case approval set.

**Acceptance Criteria:**
- Given a parsed command with derivable properties, when renderer IR is transformed, then each property name and fully qualified type survives in deterministic pure/equatable IR.
- Given generated renderer source, when a provider resolves a derivable property, then a compile-time property-name switch converts and directly assigns that declared member with no `System.Reflection`, `PropertyInfo`, or `GetProperty` emission.
- Given exact, convertible, null, unknown, or invalid provider values, when prefill runs, then successful assignments, provider fall-through, culture rules, and warning behavior remain compatible with the prior contract.
- Given the eight command-renderer approval cases and the focused runtime prefill test, when verification runs, then snapshots are intentionally approved, generated code parses/compiles, derived `MessageId` reaches dispatch, and no unrelated snapshot changes occur.

## Spec Change Log

## Review Triage Log

## Verification

**Commands:**
- `dotnet build Hexalith.FrontComposer.slnx --configuration Release -m:1 /nr:false` -- expected: zero warnings and errors.
- `DiffEngine_Disabled=true dotnet test Hexalith.FrontComposer.slnx --no-build --configuration Release --filter "FullyQualifiedName~CommandRendererEmitterTests|FullyQualifiedName~CommandRendererFullPageTests"` -- expected: focused SourceTools approvals and runtime prefill tests pass.
- `DiffEngine_Disabled=true dotnet test Hexalith.FrontComposer.slnx --no-build --configuration Release --filter "Category!=Performance&Category!=e2e-palette&Category!=NightlyProperty&Category!=Quarantined"` -- expected: default lane passes.
- `git diff --check && ! git diff -- '*.verified.txt' | rg 'System\.Reflection|PropertyInfo|GetProperty'` -- expected: clean diff and no reflective renderer approval output.

## Auto Run Result

Status: blocked
Blocking condition: The parser accepts derivable init-only and non-public-set properties, but a compile-time switch cannot directly assign those members after construction without changing the adopter contract. The existing regression `CommandFormEmitterTests.Emit_ProviderTargetWithInitOnlyDerivedPropertyUsesObjectInitializerClone` passed, confirming the init-only shape remains supported; direct assignment would fail with CS8852. Resolution requires either permitting a new diagnostic/restriction for derivable setters or specifying a reflection-free compatible assignment mechanism for these accepted shapes.
