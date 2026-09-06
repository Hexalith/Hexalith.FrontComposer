---
title: 'Replace derivable command prefill reflection with typed emission'
type: 'refactor'
created: '2026-09-06'
status: 'draft'
route: 'dispatch'
review_loop_iteration: 1
followup_review_recommended: false
context:
  - '{project-root}/_bmad-output/project-context.md'
warnings: []
deferred: []
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Generated command renderers discover writable derivable properties with `PropertyInfo.GetProperty` and assign through `PropertyInfo.SetValue`. That late-bound path is trim/AOT-hostile and discards type information already known by the source generator.

**Approach:** Preserve each derivable property's source type and boxed-assignment capability through pure renderer IR, then emit a deterministic property-name switch whose ordinary arms convert and assign statically. Keep ref-like, pointer, and function-pointer properties accepted under existing HFC1002 behavior, but emit type-free `false` arms so provider fall-through matches the former reflective path; generalize HFC1016 only for invalid setter shapes.

## Boundaries & Constraints

**Always:** Keep SourceTools netstandard2.0-clean and all parse/renderer IR symbol-free and fully equatable. Preserve ordinal case order, provider ordering/fall-through, refresh-before-submit, logging, `CurrentCulture` number/date conversion, case-insensitive enum parsing, invariant Guid parsing, nullable/null assignment, and narrow conversion failures. Treat only ref-like, pointer, and function-pointer types as non-boxable; null or non-null provider values for those properties return `false` without naming the type in emitted code. Ordinary unsupported object-representable types retain conversion behavior. A custom conversion returning null for a value type succeeds with `default(T)`, matching reflection. HFC1016 remains Error for absent, non-public, or init-only setters and suppresses generation only after all independent command diagnostics are collected. HFC1016 docs must state that suppression cannot restore generation and identify the breaking derivable-setter migration.

**Never:** Reject non-boxable derivable types, introduce a new diagnostic for them, mark generated renderers unsafe, or treat every HFC1002 type as non-boxable. Do not retain or replace member reflection with `dynamic`, expression compilation, `Type.GetProperty`, `PropertyInfo`, or trimmer annotations. Do not change derivable classification, density, provider precedence, dispatch, HFC1002 suppression/lifecycle, unrelated generator output, the deferred-work ledger, or generated `obj/**` files.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Direct value | Provider returns the declared property type | Typed switch assigns the command member and stops provider iteration | No error expected |
| Convertible value | Provider returns supported text/numeric/enum/Guid/date input | Existing culture and parse rules produce the declared value before direct assignment | Narrow conversion failures return `false` and retain warning flow |
| Null value | Target is nullable/reference or non-nullable | Direct arm preserves the prior null/default assignment outcome | No member lookup occurs |
| Null custom conversion | `IConvertible.ToType` returns null for a custom value type | Direct arm assigns `default(T)` and reports success | No `NullReferenceException` escapes |
| Non-boxable property | Derivable ref-like, pointer, or function-pointer property; provider resolves null or non-null | Type-free case returns `false`, logs not-assigned, and tries later providers | HFC1002 behavior remains unchanged; generated code stays safe and compilable |
| Unknown name | Name has no emitted arm | Method returns `false` without mutation | Existing not-assigned logging remains authoritative |
| Invalid derivable setter | Derivable property has an `init` accessor, non-public setter, or no setter | Parser reports HFC1016 Error and emits no renderer for the invalid command | Diagnostic requires a public non-init `{ get; set; }`; it must not suggest adding `[DerivedFrom]` |
| Multiple command violations | Invalid setter plus size or destructive-command violation | All applicable diagnostics are reported in one parse | Invalid model still suppresses generation |

</frozen-after-approval>

## Code Map

- `src/Hexalith.FrontComposer.SourceTools/Parsing/AttributeParser.cs`, `Parsing/DomainModel.cs` -- capture the unwrapped fully qualified type plus `SupportsObjectValueAssignment`; keep both in `PropertyModel.Equals/GetHashCode` and never retain Roslyn symbols.
- `src/Hexalith.FrontComposer.SourceTools/Parsing/CommandParser.cs` -- apply HFC1016 to all assignment targets, collect HFC1011/HFC1021/HFC1007 before returning an invalid model, and keep non-boxable properties accepted.
- `src/Hexalith.FrontComposer.SourceTools/Transforms/CommandRendererModel.cs`, `CommandRendererTransform.cs` -- replace the lossy derivable-name array with unchanged `EquatableArray<PropertyModel>`.
- `src/Hexalith.FrontComposer.SourceTools/Emitters/CommandRendererEmitter.cs` -- replace reflection with typed arms; emit type-free `return false` arms for the three non-boxable categories and preserve conversion/default/fall-through behavior.
- `tests/Hexalith.FrontComposer.SourceTools.Tests/{Parsing,Incremental,Emitters,Integration}/` -- cover IR equality, diagnostic accumulation, all non-boxable categories, deterministic source, compiled fall-through, DateTimeOffset, and custom-null conversion.
- `docs/diagnostics/HFC1016.md`, `docs/diagnostics/diagnostic-registry.json`, `src/Hexalith.FrontComposer.SourceTools/{Diagnostics/DiagnosticDescriptors.cs,AnalyzerReleases.Unshipped.md}` -- publish the setter-only breaking contract without changing HFC1002 or adding a diagnostic.
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Docs/DocsSiteValidationTests.cs`, `docs/validation/producer-fingerprints.json`, `_bmad-output/contracts/analyzer-policy-exception-ledger-v1.json` -- pin the non-remediating suppression/migration prose and reseal derived governance values after tests settle.
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Emitters/CommandRendererEmitterTests.*.verified.txt` -- rerun all eight approvals; seven renderer snapshots may change, while the page snapshot must remain unchanged.
- `tests/Hexalith.FrontComposer.Shell.Tests/Generated/CommandRendererFullPageTests.cs` -- existing end-to-end resolved `MessageId` dispatch proof; read-only unless behavior regresses.

## Tasks & Acceptance

**Execution:**
- [ ] `Parsing/AttributeParser.cs`, `Parsing/DomainModel.cs`, `Transforms/CommandRendererModel.cs`, `Transforms/CommandRendererTransform.cs` -- carry fully qualified source type and object-assignment capability through pure equatable IR.
- [ ] `Parsing/CommandParser.cs`, `Diagnostics/DiagnosticDescriptors.cs`, `AnalyzerReleases.Unshipped.md` -- generalize setter validation while retaining all independent diagnostics and accepting non-boxable HFC1002 properties.
- [ ] `Emitters/CommandRendererEmitter.cs` -- emit deterministic typed/direct arms, type-free soft-fail arms, nullable-aware conversion, and successful default assignment for null custom conversions.
- [ ] `tests/Hexalith.FrontComposer.SourceTools.Tests/{Parsing,Incremental,Emitters,Integration}/` and renderer approvals -- prove setter shapes, diagnostic accumulation, IR equality, all non-boxable categories/provider fall-through, conversion matrix, deterministic output, and no reflection.
- [ ] `docs/diagnostics/HFC1016.md`, registry/release/fingerprint files, `DocsSiteValidationTests.cs`, and analyzer inventory ledger -- document and pin the non-remediating suppression and breaking migration, then refresh only derived seals.

**Acceptance Criteria:**
- Given an attributed, convention-based, declared, or inherited derivable property, when its setter is `init`, non-public, or absent, then HFC1016 is reported as an Error at that property, its remediation requires a public non-init setter without suggesting `[DerivedFrom]`, and no renderer is generated for the invalid command.
- Given a command has an invalid setter plus size or destructive-shape violations, when parsing runs, then all applicable HFC1016/HFC1011/HFC1021/HFC1007 diagnostics are returned together and no model is emitted.
- Given derivable properties include object-representable and non-boxable types, when renderer IR is transformed, then name, source type, and assignment capability survive deterministically in equality and hashing.
- Given a provider resolves an object-representable property, when prefill runs, then the emitted ordinal switch converts and directly assigns the member with no member reflection.
- Given a provider resolves a ref-like, pointer, or function-pointer property with null or non-null, when prefill runs, then the type-free arm returns false, logs not-assigned, and continues to later providers without unsafe or uncompilable output.
- Given exact, convertible, null, custom-null, DateTimeOffset, unknown, or invalid values, when the compiled generated helper runs, then assignment, defaulting, culture, fall-through, and unchanged-on-failure behavior match the prior contract.
- Given HFC1016 documentation and governance validation, when an adopter reads suppression and migration guidance, then it states suppression cannot restore generation and names the breaking setter migration for derivable properties.
- Given all eight approvals and focused runtime checks, when verification runs, then seven renderer snapshots contain only intentional typed-prefill changes, the page snapshot is unchanged, generated output compiles, dispatch receives the derived MessageId, and reflection scans stay empty.

## Implementation Notes

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
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Emitters/CommandRendererEmitterTests.Page_FiveFields_FullPageBoundarySnapshot.verified.txt` - Verify the page snapshot remains byte-for-byte unchanged.
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Emitters/CommandRendererEmitterTests.cs` - Assert deterministic typed switch emission and absence of member reflection.
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Docs/DocsSiteValidationTests.cs` - Pin HFC1016 suppression and migration semantics in the normal governance test suite.
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Incremental/DomainModelCacheEqualityTests.cs` - Cover source type participation in IR equality and hashing.
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Integration/GeneratorDriverTests.cs` - Execute the compiled typed-prefill conversion matrix.
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Parsing/CommandParserTests.cs` - Cover valid and invalid derivable setter shapes and typed renderer IR.
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Parsing/TestFixtures/CommandTestSources.cs` - Keep parser fixtures compatible with the generalized setter contract.

## Design Notes

The renderer must branch on symbol-free IR before it writes any target type into generated source:

```text
case property.Name when !property.SupportsObjectValueAssignment:
    return false
case property.Name:
    if value is null: assign default
    else if TryConvert<property.SourceType>(value, out converted): assign converted
    return conversion result
```

Both null and non-null values soft-fail for non-boxable targets. The enclosing provider loop remains responsible for warning logs and trying the next provider. `SupportsObjectValueAssignment` is false only for ref-like, pointer, and function-pointer types; all other properties keep the typed conversion path.

## Verification

**Commands:**
- `dotnet build Hexalith.FrontComposer.slnx --configuration Release -m:1 /nr:false` -- expected: zero warnings and errors.
- `DiffEngine_Disabled=true dotnet tests/Hexalith.FrontComposer.SourceTools.Tests/bin/Release/net10.0/Hexalith.FrontComposer.SourceTools.Tests.dll -class Hexalith.FrontComposer.SourceTools.Tests.Emitters.CommandRendererEmitterTests -class Hexalith.FrontComposer.SourceTools.Tests.Integration.GeneratorDriverTests -class Hexalith.FrontComposer.SourceTools.Tests.Parsing.CommandParserTests -class Hexalith.FrontComposer.SourceTools.Tests.Incremental.DomainModelCacheEqualityTests -class Hexalith.FrontComposer.SourceTools.Tests.Docs.DocsSiteValidationTests` -- expected: focused approvals, compiled conversion/fall-through, parser, cache, and governance checks pass under the xUnit v3 runner.
- `DiffEngine_Disabled=true dotnet tests/Hexalith.FrontComposer.Shell.Tests/bin/Release/net10.0/Hexalith.FrontComposer.Shell.Tests.dll -class Hexalith.FrontComposer.Shell.Tests.Generated.CommandRendererFullPageTests` -- expected: derived `MessageId` reaches dispatch through the generated full-page renderer.
- `pwsh ./eng/validate-docs.ps1` -- expected: documentation governance and producer fingerprints pass.
- `git diff --check && ! git diff -- '*.verified.txt' | rg 'System\.Reflection|PropertyInfo|GetProperty' && git diff --exit-code -- tests/Hexalith.FrontComposer.SourceTools.Tests/Emitters/CommandRendererEmitterTests.Page_FiveFields_FullPageBoundarySnapshot.verified.txt` -- expected: clean diff, no reflective renderer approval output, and an unchanged page snapshot.

