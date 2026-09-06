---
title: 'Replace derivable command prefill reflection with typed emission'
type: 'refactor'
created: '2026-08-28'
status: in-review
baseline_commit: '0d2fdc8ae633f503d8be90da254d4ae7e19ea5a5'
baseline_revision: '0bb1d4b5117666bced0383e9a647c4e21bd8937d'
review_loop_iteration: 1
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
- [x] `src/Hexalith.FrontComposer.SourceTools/Parsing/CommandParser.cs`, `src/Hexalith.FrontComposer.SourceTools/Diagnostics/DiagnosticDescriptors.cs`, `src/Hexalith.FrontComposer.SourceTools/AnalyzerReleases.Unshipped.md`, `docs/diagnostics/HFC1016.md`, `docs/diagnostics/diagnostic-registry.json` -- generalize HFC1016 to reject every derivable `init`, non-public-set, and setterless property with an Error before renderer emission; retain the existing non-derivable validation and remove `[DerivedFrom]` as a remediation for an incompatible setter.
- [x] `src/Hexalith.FrontComposer.SourceTools/Parsing/DomainModel.cs`, `src/Hexalith.FrontComposer.SourceTools/Parsing/AttributeParser.cs` -- retain a fully qualified, symbol-free property type in equatable parse IR.
- [x] `src/Hexalith.FrontComposer.SourceTools/Transforms/CommandRendererModel.cs`, `src/Hexalith.FrontComposer.SourceTools/Transforms/CommandRendererTransform.cs` -- carry `EquatableArray<PropertyModel> DerivableProperties` through renderer transformation and update all consumers/equality members.
- [x] `src/Hexalith.FrontComposer.SourceTools/Emitters/CommandRendererEmitter.cs` -- replace reflective lookup/set with deterministic typed switch emission while preserving conversion and logging semantics.
- [x] `tests/Hexalith.FrontComposer.SourceTools.Tests/Emitters/CommandRendererEmitterTests.cs`, `tests/Hexalith.FrontComposer.SourceTools.Tests/Emitters/CommandRendererEmitterTests.*.verified.txt` -- add regression assertions and refresh the complete eight-case approval set.
- [x] `tests/Hexalith.FrontComposer.SourceTools.Tests/Parsing/CommandParserTests.cs` -- cover attributed, convention-based, and inherited derivable properties with public setters plus `init`, non-public-set, and setterless rejection cases.

**Acceptance Criteria:**
- Given an attributed, convention-based, declared, or inherited derivable property, when its setter is `init`, non-public, or absent, then HFC1016 is reported as an Error at that property, its remediation requires a public non-init setter without suggesting `[DerivedFrom]`, and no renderer is generated for the invalid command.
- Given a parsed command with derivable properties, when renderer IR is transformed, then each property name and fully qualified type survives in deterministic pure/equatable IR.
- Given generated renderer source, when a provider resolves a derivable property, then a compile-time property-name switch converts and directly assigns that declared member with no `System.Reflection`, `PropertyInfo`, or `GetProperty` emission.
- Given exact, convertible, null, unknown, or invalid provider values, when prefill runs, then successful assignments, provider fall-through, culture rules, and warning behavior remain compatible with the prior contract.
- Given the eight command-renderer approval cases and the focused runtime prefill test, when verification runs, then snapshots are intentionally approved, generated code parses/compiles, derived `MessageId` reaches dispatch, and no unrelated snapshot changes occur.

## Spec Change Log

- 2026-08-28: Human escalation resolution authorizes a breaking adopter validation rule: every derivable property must expose a public, non-init setter. HFC1016 is generalized to reject incompatible derivable shapes before typed renderer emission.

## Review Triage Log

| ID | Layer | Verdict | Route | Evidence |
|---|---|---|---|---|
| BH-01 | blind-hunter | high | defer | Verified: the current Builds gitlink is `d0049833f2ac0416075eed37f173712f3ac90e56`, while both live reports retain `0a54e63a7903bd599e35b79159782b4c84d01c07`; the three cited evidence tests fail on that stale identity. This is unrelated to the typed-prefill story. |
| BH-02 | blind-hunter | medium | defer | Verified: `docs/reference/pact-contracts.md` calls `0a54e63a7903bd599e35b79159782b4c84d01c07` current although the pinned Builds gitlink has advanced. This shares BH-01's unrelated stale-evidence root cause. |
| BH-03 | blind-hunter | medium | defer | Verified: the Gate 2c stale-diff command checks only `tests/Hexalith.FrontComposer.Shell.Tests/Pact`, although the preceding commands rewrite tracked live-evidence files. This contract-gate issue is unrelated to typed prefill. |
| BH-04 | blind-hunter | high | defer | Verified: a failed or unparsable post-stop `aspire describe` sets `host_stopped = true`, and an empty endpoint list then copies that value into `ports_closed`; a successful stop command can therefore produce clean evidence without authoritative absence proof. This is unrelated to typed prefill. |
| BH-05 | blind-hunter | medium | defer | Verified: every smoke observation hard-codes `authenticated: true` after sending a token and no no-token control probes exist, so the evidence cannot detect anonymous-access regressions on protected surfaces. This is unrelated to typed prefill. |
| BH-06 | blind-hunter | high | defer | Verified: live AppHost validation checks generic passed/authenticated/nonblank-reason fields for most observations; only query provenance receives semantic cross-checking, so contradictory health, command, or SignalR details can pass after report tampering. This is unrelated to typed prefill. |
| BH-07 | blind-hunter | medium | defer | Verified: `write_live_receipt` checks only eight top-level provider scalars and `main` skips `validate_live` in receipt-writing mode, allowing a receipt to be created before interaction-level validation. Later combined validation mitigates CI acceptance but does not make the receipt writer's own contract truthful. |
| BH-08 | blind-hunter | medium | defer | Verified: without `--expected-test`, `validate_mtp_evidence` never rejects non-passing `UnitTestResult` outcomes; the existing mixed failed/passed `nested-a` fixture is intentionally accepted by that path. Upstream blocking test commands mitigate ordinary CI runs, but retained evidence itself is not fail-closed. |
| BH-09 | blind-hunter | medium | defer | Verified: `parse_trx` reconciles only `Counters.total`; contradictory passed, failed, or notExecuted counters are ignored, allowing internally inconsistent evidence. This shares BH-08's unrelated incomplete TRX-semantic-validation root cause. |
| BH-10 | blind-hunter | medium | defer | Verified: coverage validation checks count, XML shape, and measured lines but extracts no module identity, so copied reports from one module can satisfy the eight-file count. This is unrelated to typed prefill. |
| BH-11 | blind-hunter | medium | defer | Verified: every drop condition matches the literal `.nuget/packages/hexalith.frontcomposer` fragment rather than `$(NuGetPackageRoot)` or package metadata; a custom global-packages path bypasses the target. This is unrelated to typed prefill. |
| BH-12 | blind-hunter | high | defer | Verified: `CounterCommandProjectionCatchUpChannel.Publish` catches every `Exception` without the repository fatal-exception guard, and increments `PublishedCount` before subscriber invocation; fatal subscriber failures can be swallowed. This sample issue is unrelated to typed prefill. |
| BH-13 | blind-hunter | high | defer | Verified: after the two-second disposal timeout, the continuation only disposes the CTS; a later fatal task fault is stored by `OnLoopCompleted`, while all later `DisposeAsync` calls return immediately because `_disposed` is already set. This is unrelated to typed prefill. |
| BH-14 | blind-hunter | medium | patch | Verified: HFC1016 suppression can hide the diagnostic but cannot restore generation because the parser returns a null model for the invalid assignment target. The documentation's suppression recommendation is therefore non-remediating and misleading. |
| BH-15 | blind-hunter | medium | patch | Verified: the migration section says no action is required even though this story deliberately extends HFC1016 to previously accepted derivable init-only, non-public-set, and setterless properties. It shares BH-14's incomplete HFC1016 documentation root cause. |
| BH-16 | blind-hunter | medium | defer | Verified: the Pact guide still uses VSTest-style project `--filter`, while this repository is MTP-native and its working trait syntax is `--filter-trait`. This unrelated command prevents adopters from following the documented regeneration path. |
| BH-17 | blind-hunter | high | defer | Verified: `_write_event` checks and opens only the final events directory with no-follow semantics; symlinked parent components are resolved normally, so the control-plane write can escape through an ancestor redirect. This is unrelated to typed prefill. |
| EC-01 | edge-case-hunter | high | intent_gap | Verified: command parsing admits public derivable ref-like, pointer, and function-pointer properties with at most HFC1002, while emission uses each such type as `TryConvertPropertyValue<T>`; those types cannot be boxed through `object` or used by this unconstrained generic helper, so accepted commands can fail consumer compilation. The approved intent does not choose rejection versus compatible soft failure. |
| EC-02 | edge-case-hunter | false | reject | Refuted: an error-producing type use-site attribute is already reported at the command property's source type use, so repeating the type in generated assignment code does not make an otherwise compilable command fail. A concrete type whose original property declaration compiles without that use-site error would be needed to reopen the claim. |
| EC-03 | edge-case-hunter | low | patch | Verified: the new `hasInvalidAssignmentTarget` return precedes HFC1011, HFC1021, and HFC1007 checks, so setter errors hide otherwise independent diagnostics until the next build. Moving the final invalid-model return after diagnostic collection is a direct correction. |
| EC-04 | edge-case-hunter | medium | patch | Verified: for a custom value-type target, `Convert.ChangeType` can call a provider's `IConvertible.ToType` and receive null; `(T)convertedValue!` then throws `NullReferenceException`, whereas the former reflection setter assigned the value type's default. A null converted value must preserve the prior default-assignment success contract. |
| EC-05 | edge-case-hunter | high | intent_gap | Verified independently as the EC-01 claim: the emitted generic call is not compilable for admitted non-boxable derivable types. It shares EC-01's unresolved intent root cause. |
| EC-06 | edge-case-hunter | medium | patch | Verified independently as the EC-04 claim: null returned from custom conversion escapes the narrow catch via value-type unboxing. It shares EC-04's conversion-compatibility root cause. |
| VG-01 | verification-gap | medium | defer | Pre-verified: no executable test imports or runs `.bmad-loop/bmad_loop_hook.py`, so short-write, selected-directory, atomic-name, and redirect-refusal behavior can regress undetected. This is unrelated to typed prefill. |
| VG-02 | verification-gap | medium | defer | Pre-verified: cached 304 handling calls `EnsureSuccessfulEnvelope`, but existing tests cover only fresh semantic failure, valid cached payload, and malformed cached JSON; removing the cached call remains undetected. This is unrelated to typed prefill. |
| VG-03 | verification-gap | medium | defer | Pre-verified: adapter tests exercise only a valid positive nested total count, while negative/non-integer/overflow checks are covered only downstream or not at all. This is unrelated to typed prefill. |
| VG-04 | verification-gap | medium | patch | Pre-verified: DateTimeOffset conversion is asserted only as emitted text; the compiled runtime matrix omits a DateTimeOffset property, so that branch can fail while all focused runtime coverage stays green. |

## File List

- `_bmad-output/contracts/analyzer-policy-exception-ledger-v1.json` - Refresh the analyzer inventory seal after the diagnostic and test-source changes.
- `_bmad-output/implementation-artifacts/spec-dw-1137-try-set-property-value-compile-time-switch-refactor.md` - Track implementation and review lifecycle evidence.
- `docs/diagnostics/HFC1016.md` - Document the generalized public non-init setter requirement.
- `docs/diagnostics/diagnostic-registry.json` - Update the HFC1016 registry contract.
- `docs/validation/producer-fingerprints.json` - Refresh documentation producer fingerprints.
- `src/Hexalith.FrontComposer.SourceTools/AnalyzerReleases.Unshipped.md` - Publish the generalized HFC1016 analyzer entry.
- `src/Hexalith.FrontComposer.SourceTools/Diagnostics/DiagnosticDescriptors.cs` - Update HFC1016 title and guidance.
- `src/Hexalith.FrontComposer.SourceTools/Emitters/CommandRendererEmitter.cs` - Emit typed direct-assignment switch cases and preserve conversion behavior.
- `src/Hexalith.FrontComposer.SourceTools/Parsing/AttributeParser.cs` - Capture fully qualified symbol-free property type names.
- `src/Hexalith.FrontComposer.SourceTools/Parsing/CommandParser.cs` - Enforce public non-init setters for every generated assignment target.
- `src/Hexalith.FrontComposer.SourceTools/Parsing/DomainModel.cs` - Carry source type metadata through pure equatable IR.
- `src/Hexalith.FrontComposer.SourceTools/Transforms/CommandRendererModel.cs` - Retain typed derivable-property IR.
- `src/Hexalith.FrontComposer.SourceTools/Transforms/CommandRendererTransform.cs` - Preserve typed derivable properties during transformation.
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Emitters/CommandFormEmitterTests.cs` - Update PropertyModel fixtures for the expanded IR contract.
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Emitters/CommandRendererEmitterTests.Renderer_FiveFields_FullPageBoundarySnapshot.verified.txt` - Approve the typed prefill renderer output.
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Emitters/CommandRendererEmitterTests.Renderer_FourFields_CompactInlineBoundarySnapshot.verified.txt` - Approve the typed prefill renderer output.
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Emitters/CommandRendererEmitterTests.Renderer_OneField_InlinePopoverSnapshot.verified.txt` - Approve the typed prefill renderer output.
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Emitters/CommandRendererEmitterTests.Renderer_OneField_WithIconAttributeSnapshot.verified.txt` - Approve the typed prefill renderer output.
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Emitters/CommandRendererEmitterTests.Renderer_OneField_WithoutIconUsesDefaultSnapshot.verified.txt` - Approve the typed prefill renderer output.
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Emitters/CommandRendererEmitterTests.Renderer_TwoFields_CompactInlineSnapshot.verified.txt` - Approve the typed prefill renderer output.
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Emitters/CommandRendererEmitterTests.Renderer_ZeroFields_InlineSnapshot.verified.txt` - Approve the typed prefill renderer output.
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Emitters/CommandRendererEmitterTests.cs` - Assert deterministic typed switch emission and absence of member reflection.
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Incremental/DomainModelCacheEqualityTests.cs` - Cover source type participation in IR equality and hashing.
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Integration/GeneratorDriverTests.cs` - Execute the compiled typed-prefill conversion matrix.
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Parsing/CommandParserTests.cs` - Cover valid and invalid derivable setter shapes and typed renderer IR.
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Parsing/TestFixtures/CommandTestSources.cs` - Keep parser fixtures compatible with the generalized setter contract.

## Verification

**Commands:**
- `dotnet build Hexalith.FrontComposer.slnx --configuration Release -m:1 /nr:false` -- expected: zero warnings and errors.
- `DiffEngine_Disabled=true dotnet test Hexalith.FrontComposer.slnx --no-build --configuration Release --filter "FullyQualifiedName~CommandRendererEmitterTests|FullyQualifiedName~CommandRendererFullPageTests"` -- expected: focused SourceTools approvals and runtime prefill tests pass.
- `DiffEngine_Disabled=true dotnet test Hexalith.FrontComposer.slnx --no-build --configuration Release --filter "Category!=Performance&Category!=e2e-palette&Category!=NightlyProperty&Category!=Quarantined"` -- expected: default lane passes.
- `git diff --check && ! git diff -- '*.verified.txt' | rg 'System\.Reflection|PropertyInfo|GetProperty'` -- expected: clean diff and no reflective renderer approval output.

