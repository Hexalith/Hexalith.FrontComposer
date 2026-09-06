---
title: 'Replace derivable command prefill reflection with typed emission'
type: 'refactor'
created: '2026-09-06'
status: 'in-progress'
route: 'dispatch'
review_loop_iteration: 2
followup_review_recommended: false
baseline_commit: '3270030c0db3c9beabe5f7c6e10c72fc9504eb1a'
context:
  - '{project-root}/_bmad-output/project-context.md'
warnings: []
deferred: []
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Generated command renderers discover writable derivable properties with `PropertyInfo.GetProperty` and assign through `PropertyInfo.SetValue`. That late-bound path is trim/AOT-hostile and discards type information already known by the source generator.

**Approach:** Preserve each derivable property's source-ready type, alias provenance, and safe-static-assignment capability through pure renderer IR, then emit a deterministic property-name switch whose ordinary arms convert and assign statically. Keep ref-like, pointer, function-pointer, recursively pointer-containing array, and `[Obsolete(error: true)]` derivable properties accepted, but emit type-free `false` arms so provider fall-through remains safe; generalize HFC1016 only for invalid setter shapes.

## Boundaries & Constraints

**Always:** Keep SourceTools netstandard2.0-clean and all parse/renderer IR symbol-free and fully equatable. Preserve ordinal case order, provider ordering/fall-through, refresh-before-submit, logging, `CurrentCulture` number/date conversion, case-insensitive enum parsing, invariant Guid parsing, nullable/null assignment, and narrow conversion failures. Treat ref-like, pointer, function-pointer, recursively pointer-containing array, and error-obsolete derivable properties as unsafe to reference statically; null or non-null provider values for them return `false` without naming their type or member in emitted code. Ordinary object-representable properties retain conversion behavior, including types reached through `extern alias`, whose alias declarations and qualified syntax must be reproduced in generated source. A custom conversion returning null for a value type succeeds with `default(T)`, matching reflection, while a wrong non-null result returns `false`. HFC1016 remains Error for absent, non-public, or init-only setters and suppresses generation only after all independent command diagnostics are collected. HFC1016 docs must state that suppression cannot restore generation and identify the breaking derivable-setter migration.

**Never:** Reject soft-fail derivable properties, introduce a new diagnostic for them, mark generated renderers unsafe, or soft-fail every HFC1002 type. Do not retain or replace member reflection with `dynamic`, expression compilation, `Type.GetProperty`, `PropertyInfo`, unsafe accessors, or trimmer annotations. Do not change derivable classification, density, provider precedence, dispatch, HFC1002 suppression/lifecycle, unrelated generator output, the deferred-work ledger, or generated `obj/**` files.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Direct value | Provider returns the declared property type | Typed switch assigns the command member and stops provider iteration | No error expected |
| Convertible value | Provider returns supported text/numeric/enum/Guid/date input | Existing culture and parse rules produce the declared value before direct assignment | Narrow conversion failures return `false` and retain warning flow |
| Null value | Target is nullable/reference or non-nullable | Direct arm preserves the prior null/default assignment outcome | No member lookup occurs |
| Null custom conversion | `IConvertible.ToType` returns null for a custom value type | Direct arm assigns `default(T)` and reports success | No `NullReferenceException` escapes |
| Soft-fail property | Derivable ref-like, pointer, function-pointer, recursively pointer-containing array, or error-obsolete property; provider resolves null or non-null | Type- and member-free case returns `false`, logs not-assigned, and tries later providers | Existing diagnostic behavior remains unchanged; generated code stays safe and compilable |
| Unknown name | Name has no emitted arm | Method returns `false` without mutation | Existing not-assigned logging remains authoritative |
| Invalid derivable setter | Derivable property has an `init` accessor, non-public setter, or no setter | Parser reports HFC1016 Error and emits no renderer for the invalid command | Diagnostic requires a public non-init `{ get; set; }`; it must not suggest adding `[DerivedFrom]` |
| Multiple command violations | Invalid setter plus size or destructive-command violation | All applicable diagnostics are reported in one parse | Invalid model still suppresses generation |

</frozen-after-approval>

## Code Map

- `src/Hexalith.FrontComposer.SourceTools/Parsing/CommandParser.cs` -- pass the active `Compilation` into property parsing, generalize HFC1016 to every generated assignment target, prefer a source property location with command-location fallback for metadata-only members, and accumulate HFC1016 with HFC1011/HFC1021/HFC1007 before suppressing the model.
- `src/Hexalith.FrontComposer.SourceTools/Parsing/AttributeParser.cs` -- unwrap nullable types, classify static-reference safety recursively for ref-like, pointer, function-pointer, pointer-containing array, and error-obsolete properties, and retain safe properties for typed emission.
- `src/Hexalith.FrontComposer.SourceTools/Parsing/SourceTypeNameFormatter.cs` -- format source-ready named, nested, generic, and array type syntax from the parsing-time compilation; prefer `global::` only for globally reachable references, otherwise choose a deterministic reference alias and return the required alias provenance.
- `src/Hexalith.FrontComposer.SourceTools/Parsing/DomainModel.cs` -- add symbol-free `SourceTypeName`, sorted/deduplicated `RequiredExternAliases`, and `SupportsStaticAssignment` fields to `PropertyModel`, including their equality and hash semantics.
- `src/Hexalith.FrontComposer.SourceTools/Transforms/CommandRendererModel.cs`, `CommandRendererTransform.cs` -- replace the lossy derivable-name array with unchanged `EquatableArray<PropertyModel>` so the emitter receives typed and alias-aware IR.
- `src/Hexalith.FrontComposer.SourceTools/Emitters/CommandRendererEmitter.cs` -- union and sort aliases used by statically emitted properties, write `extern alias` declarations, replace reflection with deterministic typed/direct switch arms, use escaped/verbatim member syntax, emit type- and member-free `return false` arms for unsafe properties, and validate custom conversion results before assignment.
- `tests/Hexalith.FrontComposer.SourceTools.Tests/CompilationHelper.cs`, `Parsing/AttributeParserTests.cs`, `Parsing/CommandParserTests.cs`, `Incremental/DomainModelCacheEqualityTests.cs`, `Emitters/CommandRendererEmitterTests.cs`, `Integration/GeneratorDriverTests.cs` -- cover unsafe syntax parsing, obsolete variants, alias-only references, keyword members, location and suppression semantics, diagnostic accumulation, IR equality, source emission, compiled provider fall-through, and the complete conversion matrix.
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Snapshots/AttributeParserTests.*.verified.txt`, `Emitters/CommandRendererEmitterTests.*.verified.txt` -- reapprove six parser IR snapshots and seven renderer snapshots; the page snapshot remains byte-for-byte unchanged.
- `docs/diagnostics/HFC1016.md`, `docs/diagnostics/diagnostic-registry.json`, `_bmad-output/project-docs/api-contracts.md`, `src/Hexalith.FrontComposer.SourceTools/{Diagnostics/DiagnosticDescriptors.cs,AnalyzerReleases.Unshipped.md}` -- publish one consistent setter-only breaking contract without changing HFC1002 or adding a diagnostic.
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Docs/DocsSiteValidationTests.cs`, `docs/validation/producer-fingerprints.json`, `_bmad-output/contracts/analyzer-policy-exception-ledger-v1.json` -- pin the non-remediating suppression/migration prose and refresh only the derived registry fingerprint and analyzer inventory seals after tests settle.
- `tests/Hexalith.FrontComposer.Shell.Tests/Generated/CommandRendererFullPageTests.cs` -- existing end-to-end resolved `MessageId` dispatch proof; read-only unless behavior regresses.

## Tasks & Acceptance

**Execution:**
- [ ] Extend property parsing and pure IR with deterministic source type syntax, required `extern alias` provenance, recursive static-safety classification, and complete equality/hash participation; keep all Roslyn symbols confined to parsing.
- [ ] Generalize HFC1016 validation and diagnostic accumulation, including exact source-property coordinates, metadata fallback, and suppressed-diagnostic fail-closed behavior; keep unsafe-to-reference properties accepted.
- [ ] Carry complete property IR into the renderer and emit sorted alias declarations, escaped direct assignments, conversion validation, and type/member-free soft-fail switch arms with no reflection or unsafe generated context.
- [ ] Add parser, emitter, incremental, and compiled integration tests for aliases, keyword identifiers, setter shapes, every soft-fail category, diagnostics, provider continuation, and the complete conversion matrix; reapprove exactly the affected six parser and seven renderer snapshots.
- [ ] Align HFC1016 diagnostics, release notes, registry, API-contract documentation, validation tests, fingerprints, and analyzer inventory; run the full SourceTools, focused Shell, documentation, diff, and snapshot checks.

**Acceptance Criteria:**
- Given an attributed, convention-based, declared, or inherited derivable property has an `init`, non-public, or absent setter, when parsing runs, then HFC1016 is reported as an Error at the source property when available or at the command for metadata-only members; suppression hides the diagnostic but still emits no renderer, and remediation requires a public non-init setter without suggesting `[DerivedFrom]`.
- Given a command has an invalid setter plus size or destructive-shape violations, when parsing runs, then all applicable HFC1016/HFC1011/HFC1021/HFC1007 diagnostics are returned independently in one result and no model is emitted.
- Given derivable properties include global, alias-only, nested, generic, array, and unsafe-to-reference types, when renderer IR is produced, then source syntax, sorted/deduplicated alias provenance, and `SupportsStaticAssignment` survive deterministically through transformation, equality, and hashing without retaining Roslyn symbols.
- Given a provider resolves an ordinary object-representable property, including an alias-only type or escaped C# keyword member, when prefill runs, then generated source declares any required `extern alias`, converts the value, and directly assigns the member with no member reflection.
- Given a provider resolves a ref-like, pointer, function-pointer, recursively pointer-containing array, or `[Obsolete(error: true)]` derivable property with null or non-null, when prefill runs, then its case contains no target type, member reference, or unsafe syntax, returns `false`, logs not-assigned, and allows later providers to run while the generated renderer still compiles.
- Given exact, convertible, null, non-null nullable, custom-null, wrong non-null custom, DateTimeOffset, unknown, or invalid values, when the compiled generated helper runs, then assignment, defaulting, culture, provider fall-through, and unchanged-on-failure behavior match the approved contract.
- Given HFC1016 documentation and governance validation, when an adopter reads the descriptor, release note, diagnostic page, registry, and API contract, then each states the same public non-init setter rule, suppression cannot restore generation, and the breaking derivable-setter migration is named.
- Given verification runs against all affected approvals and runtime suites, then six parser and seven renderer snapshots contain only intentional IR/typed-prefill changes, the renderer page snapshot is unchanged, the complete SourceTools assembly and focused Shell proof pass without skips, alias-qualified generated output compiles, dispatch receives the derived `MessageId`, and newly added reflection references remain absent.

## Implementation Notes

## Spec Change Log

- 2026-08-28: Human escalation resolution authorizes a breaking adopter validation rule: every derivable property must expose a public, non-init setter. HFC1016 is generalized to reject incompatible derivable shapes before typed renderer emission.
- 2026-09-06: Review iteration 2 resolved EC2-01 and EC2-02 by human choice: recursively pointer-containing arrays and `[Obsolete(error: true)]` derivable properties remain accepted but use type- and member-free soft-fail arms. BH2-07 exposed alias-only CS0400 from `global::` formatting, so the plan now preserves alias provenance, emits deterministic `extern alias` declarations, and avoids `global::` for types reachable only through aliases. Keep the typed switch, safe soft-fail arms, accumulated HFC1016 diagnostics, compatible conversion behavior, exactly seven changed renderer approvals with an unchanged page snapshot, and escaped identifier handling.

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
| BH2-01 | blind-hunter | medium | patch | Verified by running the complete SourceTools assembly: 1,216 tests ran and six `AttributeParserTests` approvals failed because the new `PropertyModel` fields were absent from their verified snapshots. The implementation is not suite-clean until those approvals are reviewed and accepted. |
| BH2-02 | blind-hunter | medium | reject | Verified that the focused verification command omits `AttributeParserTests`, but the proposed remedy edits this build's spec and is therefore rejected by review policy. BH2-01 retains the executable failure itself for correction after re-derivation. |
| BH2-03 | blind-hunter | low | reject | The new fields are populated for projection `PropertyModel` instances, but no unrelated generated projection output or distinct cache invalidation harm was demonstrated: existing type/FQN fields already change with the underlying property type. Isolating a second IR would add complexity for negligible practical benefit. |
| BH2-04 | blind-hunter | low | reject | Carrying full `PropertyModel` values can rerun renderer emission after derivable display-only metadata changes, but the effect is bounded incremental-build work with no output or runtime divergence. A separate public equatable assignment model is disproportionate to this low-frequency cost. |
| BH2-05 | blind-hunter | medium | patch | Verified: `IConvertible.ToType` can return a non-null value of the wrong runtime type, and the generated `(T)convertedValue` cast then escapes as `InvalidCastException`. The conversion helper must return `false` for this invalid result. |
| BH2-06 | blind-hunter | false | reject | Production parsing uses the internal constructor carrying exact source/capability data, and the SourceTools package exposes its DLL only as an analyzer asset, not a supported compile reference. No supported public caller of the convenience `PropertyModel` constructor reaching renderer emission was shown. |
| BH2-07 | blind-hunter | high | bad_spec | Verified with an alias-only project reference: `global::Namespace.Type` fails with CS0400 when the assembly is available only through `extern alias`. The plan's source-ready type strategy omitted alias provenance needed for valid typed emission. |
| BH2-08 | blind-hunter | medium | patch | Verified: an invalid inherited property from metadata has no source tree, so the current fallback combines the derived command file with a meaningless metadata line/column. Location selection must use the property only when `IsInSource`, otherwise the command declaration. |
| BH2-09 | blind-hunter | medium | patch | Verified: parser rejection is independent of diagnostic reporting, but no executable suppression case proves the documented fail-closed behavior. Add a suppressed-HFC1016 generator test that observes no visible diagnostic and no command artifacts. |
| BH2-10 | blind-hunter | medium | patch | Verified in part: the runtime matrix exercises nullable null but not a non-null conversion into a nullable value type, leaving the new underlying-type path unpinned. DateTime/DateOnly/TimeOnly still share the pre-existing conversion branches, so the direct correction is the nullable assertion. |
| BH2-11 | blind-hunter | low | reject | Struct commands already receive HFC1004 stating they are unsupported; the change from ineffective boxed reflection mutation to direct field mutation does not regress a supported contract. Suppressing all warned output or documenting unsupported runtime details is disproportionate here. |
| BH2-12 | blind-hunter | medium | patch | Verified: `_bmad-output/project-docs/api-contracts.md` still describes HFC1016 as non-derivable-only, contradicting the new registry, descriptor, and adopter page. Refresh the tracked diagnostic inventory and pin it through existing governance where practical. |
| BH2-13 | blind-hunter | low | patch | Verified: HFC1016 documentation attributes every rejection to emitted assignment even though approved non-boxable arms deliberately never assign. Clarify that those setter shapes are rejected by the uniform command-shape policy. |
| BH2-14 | blind-hunter | false | reject | The blocked auto-result is an unrelated untracked artifact produced by another process, is not staged or owned by this implementation, and no commit is authorized. It is preserved as external work rather than deleted from the user's tree. |
| VG2-01 | verification-gap | medium | patch | Pre-verified: the runtime matrix assigns only null to nullable `UserId`, so changing the helper back to `typeof(T)` would break non-null nullable conversion without failing current coverage. Add a convertible non-null value and assert the resulting nullable value. |
| VG2-02 | verification-gap | medium | patch | Pre-verified: documentation text alone pins non-remediating HFC1016 suppression; no generator test suppresses the diagnostic while asserting artifacts remain absent. Add that executable fail-closed case. |
| VG2-03 | verification-gap | medium | patch | Pre-verified: no multi-tree test asserts that inherited HFC1016 points to its base declaration, so command-file/location mismatches can pass. Add cross-file source-location coverage alongside the location fallback correction. |
| EC2-01 | edge-case-hunter | high | intent_gap | Verified with a compiler probe: arrays whose elements are pointer or function-pointer types are boxable, so the current capability flag is true, but both their direct member access and generic type syntax require an unsafe context and fail generated safe code with CS0214. The frozen boundary permits soft-fail only for the three direct non-boxable type kinds while also forbidding unsafe renderers, so the human must choose how pointer-containing arrays behave. |
| EC2-02 | edge-case-hunter | high | intent_gap | Verified with a compiler probe: directly assigning a property marked `[Obsolete(error: true)]` produces unsuppressible CS0619, whereas name-based reflection did not reference the member. The frozen boundary requires ordinary object-representable types to stay typed and does not authorize rejection or soft-failure for this case, so the human must choose the compatibility behavior. |
| EC2-03 | edge-case-hunter | medium | patch | Verified independently as BH2-05: a custom conversion returning a non-null wrong-type object escapes the cast instead of returning `false`. It shares BH2-05's conversion-validation root cause. |
| EC2-04 | edge-case-hunter | false | reject | Refuted: the prior reflective implementation used the same narrow exception filter around `Convert.ChangeType`; arbitrary exceptions from `IConvertible` propagated before and remain intentionally outside the approved narrow failure set. |
| EC2-05 | edge-case-hunter | false | reject | Refuted: the prior `PropertyInfo.SetValue` path also aborted prefill when a setter threw, merely wrapping the exception. Provider fall-through is promised for unresolved/conversion failure, not for user setter exceptions. |
| EC2-06 | edge-case-hunter | medium | patch | Verified independently as BH2-08: metadata-only inherited properties produce misleading HFC1016 coordinates. It shares BH2-08's source-location root cause. |
| EC2-07 | edge-case-hunter | low | patch | Verified: the equality test requires unequal objects to have different hash codes, which `GetHashCode` never guarantees and can make the test spuriously fail on a collision. Remove the non-collision assertion while retaining inequality coverage. |
| EC2-08 | edge-case-hunter | low | patch | Verified independently for `CommandRendererModel`: unequal renderer models need not produce distinct hash codes. Remove that non-contractual assertion while retaining equality checks. |
| EC2-09 | edge-case-hunter | false | reject | Refuted by the approved frozen decision: both null and non-null provider values for ref-like, pointer, and function-pointer properties must return `false`. The new behavior intentionally supersedes any reflective null behavior. |

## File List

- `_bmad-output/contracts/analyzer-policy-exception-ledger-v1.json` - Refresh only the derived analyzer inventory after the diagnostic and test-source changes.
- `_bmad-output/implementation-artifacts/spec-dw-1137-try-set-property-value-compile-time-switch-refactor.md` - Track implementation, verification, and append-only review lifecycle evidence.
- `_bmad-output/project-docs/api-contracts.md` - Align the documented HFC1016 setter contract and breaking migration.
- `docs/diagnostics/HFC1016.md` - Document the generalized public non-init setter requirement and non-remediating suppression.
- `docs/diagnostics/diagnostic-registry.json` - Update the HFC1016 registry contract.
- `docs/validation/producer-fingerprints.json` - Refresh only the derived registry fingerprint after content settles.
- `src/Hexalith.FrontComposer.SourceTools/AnalyzerReleases.Unshipped.md` - Publish the generalized HFC1016 analyzer entry.
- `src/Hexalith.FrontComposer.SourceTools/Diagnostics/DiagnosticDescriptors.cs` - Update HFC1016 title and guidance.
- `src/Hexalith.FrontComposer.SourceTools/Emitters/CommandRendererEmitter.cs` - Emit aliases, typed direct-assignment switch cases, safe soft-fail arms, escaped members, and compatible conversion behavior.
- `src/Hexalith.FrontComposer.SourceTools/Parsing/AttributeParser.cs` - Classify property static-reference safety and capture source-ready type metadata.
- `src/Hexalith.FrontComposer.SourceTools/Parsing/CommandParser.cs` - Pass compilation context, enforce valid setters, accumulate diagnostics, and select accurate HFC1016 locations.
- `src/Hexalith.FrontComposer.SourceTools/Parsing/DomainModel.cs` - Carry source type, alias provenance, and static-assignment capability through pure equatable IR.
- `src/Hexalith.FrontComposer.SourceTools/Parsing/SourceTypeNameFormatter.cs` - Add deterministic compilation-aware formatting for global and alias-only type references.
- `src/Hexalith.FrontComposer.SourceTools/Transforms/CommandRendererModel.cs` - Retain complete typed derivable-property IR.
- `src/Hexalith.FrontComposer.SourceTools/Transforms/CommandRendererTransform.cs` - Preserve typed derivable properties during transformation.
- `tests/Hexalith.FrontComposer.SourceTools.Tests/CompilationHelper.cs` - Permit explicitly unsafe parser and generator compilation fixtures.
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Docs/DocsSiteValidationTests.cs` - Pin HFC1016 suppression and migration semantics in the normal governance suite.
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Emitters/CommandFormEmitterTests.cs` - Update `PropertyModel` fixtures for the expanded IR contract.
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Emitters/CommandRendererEmitterTests.cs` - Assert aliases, escaped members, deterministic typed switches, safe soft-fail arms, conversion validation, and absence of reflection.
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Emitters/CommandRendererEmitterTests.Renderer_FiveFields_FullPageBoundarySnapshot.verified.txt` - Approve intentional typed-prefill renderer output.
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Emitters/CommandRendererEmitterTests.Renderer_FourFields_CompactInlineBoundarySnapshot.verified.txt` - Approve intentional typed-prefill renderer output.
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Emitters/CommandRendererEmitterTests.Renderer_OneField_InlinePopoverSnapshot.verified.txt` - Approve intentional typed-prefill renderer output.
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Emitters/CommandRendererEmitterTests.Renderer_OneField_WithIconAttributeSnapshot.verified.txt` - Approve intentional typed-prefill renderer output.
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Emitters/CommandRendererEmitterTests.Renderer_OneField_WithoutIconUsesDefaultSnapshot.verified.txt` - Approve intentional typed-prefill renderer output.
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Emitters/CommandRendererEmitterTests.Renderer_TwoFields_CompactInlineSnapshot.verified.txt` - Approve intentional typed-prefill renderer output.
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Emitters/CommandRendererEmitterTests.Renderer_ZeroFields_InlineSnapshot.verified.txt` - Approve intentional typed-prefill renderer output.
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Emitters/CommandRendererEmitterTests.Page_FiveFields_FullPageBoundarySnapshot.verified.txt` - Read-only guard proving the page snapshot remains byte-for-byte unchanged.
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Incremental/DomainModelCacheEqualityTests.cs` - Cover every new IR field in equality and equal-object hashing without asserting unequal hash codes.
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Integration/GeneratorDriverTests.cs` - Compile and execute alias-only, suppression, soft-fail, provider-continuation, keyword-member, and conversion scenarios.
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Parsing/AttributeParserTests.cs` - Cover type formatting, alias provenance, unsafe categories, arrays, obsolete variants, and symbol-free parsed models.
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Parsing/CommandParserTests.cs` - Cover valid/invalid derivable setters, source and metadata diagnostic locations, accumulation, and renderer IR.
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Parsing/TestFixtures/CommandTestSources.cs` - Keep parser fixtures compatible with the generalized setter contract.
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Snapshots/AttributeParserTests.Parse_AllFieldTypesProjection_Covers29Types.verified.txt` - Reapprove expanded property IR.
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Snapshots/AttributeParserTests.Parse_BadgeMappingProjection_ExtractsBadgeSlots.verified.txt` - Reapprove expanded property IR.
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Snapshots/AttributeParserTests.Parse_BasicProjection_ProducesCorrectIR.verified.txt` - Reapprove expanded property IR.
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Snapshots/AttributeParserTests.Parse_GlobalNamespaceProjection_HandlesEmptyNamespace.verified.txt` - Reapprove expanded property IR.
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Snapshots/AttributeParserTests.Parse_MultiAttributeProjection_ExtractsBoundedContextAndRole.verified.txt` - Reapprove expanded property IR.
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Snapshots/AttributeParserTests.Parse_RecordProjection_ProducesCorrectIR.verified.txt` - Reapprove expanded property IR.

## Design Notes

The parser computes source syntax and safety while Roslyn symbols are available. `SourceTypeNameFormatter` traverses the unwrapped type graph, identifies referenced assemblies through the active `Compilation`, uses `global::` only when every referenced type is globally reachable, and otherwise selects the first reference alias in ordinal order. It returns sorted/deduplicated required aliases with the formatted type; neither symbols nor compilations cross into `PropertyModel`.

Static safety is false when a property is ref-like, its type is a pointer or function pointer, an array element recursively contains either pointer shape, or the property is marked `[Obsolete(error: true)]`. Error-obsolete detection reads the well-known attribute symbol and its `error` constructor argument; ordinary and warning-level obsolete properties retain typed behavior. The renderer branches on this symbol-free capability before writing any target type or member into generated source:

```text
requiredAliases = sorted union of aliases from statically assigned properties
emit each required alias before using directives

case property.Name when !property.SupportsStaticAssignment:
    return false
case property.Name:
    if value is null:
        command.@Member = default(property.SourceTypeName)
        return true
    if value is property.SourceTypeName exact:
        command.@Member = exact
        return true
    if narrow conversion succeeds and converted is null:
        command.@Member = default(property.SourceTypeName)
        return true
    if narrow conversion succeeds and converted is property.SourceTypeName typed:
        command.@Member = typed
        return true
    return false
```

Using a verbatim identifier for ordinary member access keeps keywords source-safe without introducing a Roslyn dependency in the emitter. Both null and non-null values soft-fail for unsafe-to-reference targets. Those arms name neither the property type nor the member, so they require no unsafe context and cannot trigger error-level obsolete access. The enclosing provider loop remains responsible for warning logs and trying the next provider. Existing narrow conversion catches remain unchanged; only a wrong non-null conversion result is validated and returned as `false` instead of escaping a cast.

HFC1016 validation is independent from assignment safety. Invalid setter shapes remain fail-closed even when the diagnostic is suppressed. The diagnostic location uses the property's source location only when available and otherwise falls back to the command declaration, avoiding meaningless metadata coordinates.

## Verification

**Commands:**
- `dotnet build Hexalith.FrontComposer.slnx --configuration Release -m:1 /nr:false` -- expected: zero warnings and errors.
- `DiffEngine_Disabled=true dotnet tests/Hexalith.FrontComposer.SourceTools.Tests/bin/Release/net10.0/Hexalith.FrontComposer.SourceTools.Tests.dll` -- expected: the complete SourceTools assembly passes with no failures or skips, including all 13 intentionally refreshed approvals.
- `DiffEngine_Disabled=true dotnet tests/Hexalith.FrontComposer.Shell.Tests/bin/Release/net10.0/Hexalith.FrontComposer.Shell.Tests.dll -class Hexalith.FrontComposer.Shell.Tests.Generated.CommandRendererFullPageTests` -- expected: derived `MessageId` reaches dispatch through the generated full-page renderer.
- `pwsh ./eng/validate-docs.ps1` -- expected: documentation governance and producer fingerprints pass.
- `git diff --check && ! git diff -- '*.verified.txt' | rg '^\+.*(System\.Reflection|PropertyInfo|GetProperty|SetValue)' && git diff --exit-code -- tests/Hexalith.FrontComposer.SourceTools.Tests/Emitters/CommandRendererEmitterTests.Page_FiveFields_FullPageBoundarySnapshot.verified.txt` -- expected: clean diff, no newly added reflective renderer approval output, and an unchanged page snapshot.

