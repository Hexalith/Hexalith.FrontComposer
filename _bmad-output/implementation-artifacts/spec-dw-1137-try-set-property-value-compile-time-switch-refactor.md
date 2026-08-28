---
title: 'Replace derivable command prefill reflection with typed emission'
type: 'refactor'
created: '2026-08-28'
status: ready-for-dev
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

**Approach:** Preserve each derivable property's name and fully qualified source type through renderer IR, then emit a deterministic property-name switch whose arms convert and assign directly to the statically known command members. Generalize HFC1016 so every derivable property must expose a public, non-init setter before renderer emission. Keep the existing provider ordering, culture, conversion-failure, logging, and refresh-before-submit behavior.

## Boundaries & Constraints

**Always:** Keep SourceTools netstandard2.0-clean and its IR pure/equatable; use ordinal property-name matching and deterministic property order; preserve `CurrentCulture` for numeric/date conversion, case-insensitive enum parsing, invariant Guid text parsing, nullable/null assignment, and the existing narrow conversion-failure handling; require every derivable property, whether convention-based, attributed, declared, or inherited, to expose a public `{ get; set; }`-style non-init setter; report HFC1016 Error at the property for an `init` accessor, non-public setter, or absent setter and suppress renderer generation for that invalid command; update equality/hash code whenever IR changes; run Verify with `DiffEngine_Disabled=true`.

**Block If:** An invalid derivable setter shape can reach renderer emission without HFC1016 Error, or preserving existing conversion semantics would require runtime member reflection.

**Never:** Edit the deferred-work ledger; retain or replace member reflection with `dynamic`, expression compilation, `Type.GetProperty`, `PropertyInfo`, or trimmer annotations; change derivable-property classification, density, provider precedence, command dispatch, or unrelated generator output; hand-edit generated `obj/**` files.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Direct value | Provider returns the declared property type | Typed switch assigns the command member and stops provider iteration | No error expected |
| Convertible value | Provider returns supported text/numeric/enum/Guid/date input | Existing culture and parse rules produce the declared value before direct assignment | Narrow conversion failures return `false` and retain warning flow |
| Null value | Target is nullable/reference or non-nullable | Direct arm preserves the prior null/default assignment outcome | No member lookup occurs |
| Unknown name | Name has no emitted arm | Method returns `false` without mutation | Existing not-assigned logging remains authoritative |
| Invalid derivable setter | Derivable property has an `init` accessor, non-public setter, or no setter | Parser reports HFC1016 Error and emits no renderer for the invalid command | Diagnostic requires a public non-init `{ get; set; }`; it must not suggest adding `[DerivedFrom]` |

</intent-contract>

## Code Map

- `src/Hexalith.FrontComposer.SourceTools/Parsing/AttributeParser.cs` -- parse boundary where Roslyn can capture the unwrapped property's fully qualified source type before symbols are discarded.
- `src/Hexalith.FrontComposer.SourceTools/Parsing/CommandParser.cs`, `src/Hexalith.FrontComposer.SourceTools/Diagnostics/DiagnosticDescriptors.cs` -- generalize HFC1016 from non-derivable properties to every generated-assignment target and reject incompatible derivable setters before emission.
- `src/Hexalith.FrontComposer.SourceTools/Parsing/DomainModel.cs` -- `PropertyModel` pure IR and equality/hash contract; carry source type metadata here without leaking `ISymbol`.
- `src/Hexalith.FrontComposer.SourceTools/Transforms/CommandRendererModel.cs` -- renderer IR currently reduces derivable properties to `EquatableArray<string>`; replace that lossy seam with typed property IR.
- `src/Hexalith.FrontComposer.SourceTools/Transforms/CommandRendererTransform.cs` -- preserve `CommandModel.DerivableProperties` instead of rebuilding a name-only array.
- `src/Hexalith.FrontComposer.SourceTools/Emitters/CommandRendererEmitter.cs:304` -- prefill loop and reflective `TrySetPropertyValue`; emit compile-time cases and direct assignments here.
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Emitters/CommandRendererEmitterTests.cs` -- model fixture, parseability/determinism tests, and focused no-reflection/typed-case assertions.
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Parsing/CommandParserTests.cs`, `docs/diagnostics/HFC1016.md`, `docs/diagnostics/diagnostic-registry.json`, `src/Hexalith.FrontComposer.SourceTools/AnalyzerReleases.Unshipped.md` -- prove and publish the generalized HFC1016 contract.
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Emitters/CommandRendererEmitterTests.*.verified.txt` -- eight owned Verify approvals; rerun all snapshot cases and approve only intentional renderer changes.
- `tests/Hexalith.FrontComposer.Shell.Tests/Generated/CommandRendererFullPageTests.cs:81` -- existing runtime proof that a resolved `MessageId` reaches the dispatched command; read-only unless behavior regresses.

## Tasks & Acceptance

**Execution:**
- `src/Hexalith.FrontComposer.SourceTools/Parsing/CommandParser.cs`, `src/Hexalith.FrontComposer.SourceTools/Diagnostics/DiagnosticDescriptors.cs`, `src/Hexalith.FrontComposer.SourceTools/AnalyzerReleases.Unshipped.md`, `docs/diagnostics/HFC1016.md`, `docs/diagnostics/diagnostic-registry.json` -- generalize HFC1016 to reject every derivable `init`, non-public-set, and setterless property with an Error before renderer emission; retain the existing non-derivable validation and remove `[DerivedFrom]` as a remediation for an incompatible setter.
- `src/Hexalith.FrontComposer.SourceTools/Parsing/DomainModel.cs`, `src/Hexalith.FrontComposer.SourceTools/Parsing/AttributeParser.cs` -- retain a fully qualified, symbol-free property type in equatable parse IR.
- `src/Hexalith.FrontComposer.SourceTools/Transforms/CommandRendererModel.cs`, `src/Hexalith.FrontComposer.SourceTools/Transforms/CommandRendererTransform.cs` -- carry `EquatableArray<PropertyModel> DerivableProperties` through renderer transformation and update all consumers/equality members.
- `src/Hexalith.FrontComposer.SourceTools/Emitters/CommandRendererEmitter.cs` -- replace reflective lookup/set with deterministic typed switch emission while preserving conversion and logging semantics.
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Emitters/CommandRendererEmitterTests.cs`, `tests/Hexalith.FrontComposer.SourceTools.Tests/Emitters/CommandRendererEmitterTests.*.verified.txt` -- add regression assertions and refresh the complete eight-case approval set.
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Parsing/CommandParserTests.cs` -- cover attributed, convention-based, and inherited derivable properties with public setters plus `init`, non-public-set, and setterless rejection cases.

**Acceptance Criteria:**
- Given an attributed, convention-based, declared, or inherited derivable property, when its setter is `init`, non-public, or absent, then HFC1016 is reported as an Error at that property, its remediation requires a public non-init setter without suggesting `[DerivedFrom]`, and no renderer is generated for the invalid command.
- Given a parsed command with derivable properties, when renderer IR is transformed, then each property name and fully qualified type survives in deterministic pure/equatable IR.
- Given generated renderer source, when a provider resolves a derivable property, then a compile-time property-name switch converts and directly assigns that declared member with no `System.Reflection`, `PropertyInfo`, or `GetProperty` emission.
- Given exact, convertible, null, unknown, or invalid provider values, when prefill runs, then successful assignments, provider fall-through, culture rules, and warning behavior remain compatible with the prior contract.
- Given the eight command-renderer approval cases and the focused runtime prefill test, when verification runs, then snapshots are intentionally approved, generated code parses/compiles, derived `MessageId` reaches dispatch, and no unrelated snapshot changes occur.

## Spec Change Log

- 2026-08-28: Human escalation resolution authorizes a breaking adopter validation rule: every derivable property must expose a public, non-init setter. HFC1016 is generalized to reject incompatible derivable shapes before typed renderer emission.

## Review Triage Log

## Verification

**Commands:**
- `dotnet build Hexalith.FrontComposer.slnx --configuration Release -m:1 /nr:false` -- expected: zero warnings and errors.
- `DiffEngine_Disabled=true dotnet test Hexalith.FrontComposer.slnx --no-build --configuration Release --filter "FullyQualifiedName~CommandRendererEmitterTests|FullyQualifiedName~CommandRendererFullPageTests"` -- expected: focused SourceTools approvals and runtime prefill tests pass.
- `DiffEngine_Disabled=true dotnet test Hexalith.FrontComposer.slnx --no-build --configuration Release --filter "Category!=Performance&Category!=e2e-palette&Category!=NightlyProperty&Category!=Quarantined"` -- expected: default lane passes.
- `git diff --check && ! git diff -- '*.verified.txt' | rg 'System\.Reflection|PropertyInfo|GetProperty'` -- expected: clean diff and no reflective renderer approval output.

