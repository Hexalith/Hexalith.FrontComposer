---
title: 'Canonical fingerprint vector coverage'
type: 'chore'
created: '2026-09-06'
status: 'done'
baseline_revision: '29edff1d5e5fe2944bfed3491171acee68aec5eb'
review_loop_iteration: 0
followup_review_recommended: false
context: []
warnings: []
deferred:
  - summary: >-
      Canonical fingerprint golden coverage does not independently pin the public schema test-vector identifier.
    evidence: |-
      SchemaFingerprint.TestVectorId defaults to SchemaFingerprintAlgorithm.TestVectorIdV1, and the reviewed tests do not assert the literal hfc-schema-v1 identity. A change to that shared constant could propagate without failing these vectors. This predates DW-695 and is separate from collection and EnumValues ordering.
    location: >-
      src/Hexalith.FrontComposer.Contracts/Schema/SchemaFingerprintContracts.cs:364-387
    severity: medium
---

<intent-contract>

## Intent

**Problem:** The canonical schema golden vector pins document metadata and empty nested maps, but it does not prove that collection descriptors or populated field enum values are serialized in deterministic ordinal order. A regression in either normalization path could therefore change fingerprints without focused golden evidence.

**Approach:** Enrich the existing fixed document with deliberately non-canonical collection and enum sequences, pin their ordinal canonical JSON and independently derived SHA-256, and prove equivalent reordered inputs converge on that same literal vector.

## Boundaries & Constraints

**Always:** Keep this a Contracts test-only change; make input order differ from expected canonical order; assert literal canonical JSON, lowercase SHA-256, algorithm, and canonicalizer version; derive the expected hash independently with `sha256sum`; retain existing metadata, nested-map, family-validation, and null-argument coverage.

**Never:** Do not change `CanonicalSchemaMaterial`, schema records, encoders, sentinel handling, ordinal comparers, source-generation context, wire JSON, production fingerprints, package dependencies, `references/**`, or any deferred-work ledger, including `_bmad-output/implementation-artifacts/deferred-work.md`.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Golden canonicalization | Multiple collections and non-empty enum values supplied out of ordinal order | Exact JSON contains collections ordered by name and enum values ordered with ordinal semantics; exact independent SHA-256 matches | No error expected |
| Equivalent permutations | Same collection descriptors and enum values supplied in different sequence orders | Both payloads converge on the same pinned JSON and fingerprint | No error expected |

</intent-contract>

## Code Map

- `tests/Hexalith.FrontComposer.Contracts.Tests/Schema/CanonicalSchemaMaterialFingerprintVectorTests.cs:20-38,143-158` -- existing literal canonical JSON/SHA-256 vector and fixed input document; enrich this test surface and add reordered-input evidence without weakening current assertions.
- `src/Hexalith.FrontComposer.Contracts/Schema/SchemaFingerprintContracts.cs:171-213` -- read-only implementation evidence: fields and collections sort by name and enum values sort through `StringComparer.Ordinal` before source-generated JSON serialization.
- `tests/Hexalith.FrontComposer.Contracts.Tests/Schema/SchemaFingerprintContractsTests.cs:10-40` -- read-only neighboring self-consistency test; it reverses fields but does not pin collection or enum ordering, so the golden-vector class owns this gap.
- `_bmad-output/implementation-artifacts/11-21-recommended-analyzer-product-and-generator-burndown.md:362-368` -- read-only origin evidence identifying the exact deferred vector gap and preserving it as test-only follow-up work.
- `.bmad-loop/runs/20260906-162951-8225/bundles/canonical-fingerprint-vector-coverage/intent.md` -- read-only bundle intent and verbatim DW-695 entry; the orchestrator owns ledger resolution.

## Tasks & Acceptance

**Execution:**
- [x] `tests/Hexalith.FrontComposer.Contracts.Tests/Schema/CanonicalSchemaMaterialFingerprintVectorTests.cs` -- add out-of-order collection descriptors and populated enum members to the fixed input, update the exact canonical JSON and independently computed fingerprint, and add an equivalent-permutation assertion -- closes DW-695 with observable ordinal serialization evidence.

**Acceptance Criteria:**
- Given a fixed schema document whose collection descriptors and populated enum members are supplied outside ordinal order, when `CanonicalSchemaMaterial.CreatePayload` runs, then its complete JSON and lowercase SHA-256 exactly match independently pinned values whose collections and enum members are ordinally ordered.
- Given the same collection and enum content in different input sequences, when both payloads are created, then both yield the identical pinned JSON, fingerprint value, algorithm identifier, and canonicalizer version.
- Given the focused Contracts test project, when its default lane runs, then all tests pass with no production or ledger changes.

## Spec Change Log

## Review Triage Log

### 2026-09-06 — Review pass
- verdicts: 10 findings — high 0, medium 4, low 1, false 5, maybe-false 0
- findings:
  - `[medium]` `[patch]` Collection names and `SchemaCollectionOrder` values sorted in the same direction, so a wrong sort key could pass -- patched the fixture to serialize `Events` (`Order=1`) before `commands` (`Order=0`), making ordinal name order disagree with numeric order.
  - `[medium]` `[patch]` Lowercase ASCII collection names did not distinguish ordinal from linguistic comparison -- patched the fixture with mixed-case `Events` and `commands`, whose required ordinal order differs from ordinary linguistic order.
  - `[medium]` `[defer]` `SchemaFingerprint.TestVectorId` is not independently pinned -- verified the public identifier participates in compatibility provenance and no test asserts its literal value; this predates DW-695 and is recorded in frontmatter for separate handling.
  - `[false]` `[reject]` Replacing the prior private fixture removes published-vector protection -- repository search found the old hash only in this private test, while the enriched fixture retains the original empty enum and nested-map cases and adds the requested ordering coverage.
  - `[false]` `[reject]` Independent hash and test outcomes are absent from a completed artifact -- the review occurred before workflow finalization; the hash was independently verified and the required `## Auto Run Result` records actual outcomes during finalization.
  - `[low]` `[reject]` Code Map line anchors drifted after implementation -- the anchors are stale but the named file and symbols remain unambiguous; the only fix edits this build's spec, which review policy rejects.
  - `[false]` `[reject]` Ignored bundle provenance leaves the tracked spec without DW-695 context -- the complete problem and constraints are captured in the tracked intent contract, and the tracked Story 11.21 origin evidence is also cited.
  - `[medium]` `[patch]` The strict ordinal reading was not discriminator-safe for collections -- same verified root cause as the first two findings; the patched names and opposing enum-order values now distinguish ordinal name sorting from both linguistic and numeric-order sorting.
  - `[false]` `[reject]` Collection and enum permutations require separate golden documents -- each property occupies a distinct JSON location in the exact literal assertion, so a defect in either normalization path independently fails the vector even though one test varies both.
  - `[false]` `[reject]` The intent requires producer or MCP end-to-end fingerprint coverage -- the verbatim ledger names `Collections`, `EnumValues`, and the existing Contracts golden-vector test; `CanonicalSchemaMaterial.CreatePayload` is the referenced outer surface, with no generator or MCP behavior requested.

## Verification

**Commands:**
- `dotnet build tests/Hexalith.FrontComposer.Contracts.Tests/Hexalith.FrontComposer.Contracts.Tests.csproj --configuration Release` -- expected: build succeeds with zero errors.
- `DiffEngine_Disabled=true dotnet test --project tests/Hexalith.FrontComposer.Contracts.Tests/Hexalith.FrontComposer.Contracts.Tests.csproj --configuration Release --no-build --filter-class "*CanonicalSchemaMaterialFingerprintVectorTests*"` -- expected: focused golden-vector class passes.
- `DiffEngine_Disabled=true dotnet test --project tests/Hexalith.FrontComposer.Contracts.Tests/Hexalith.FrontComposer.Contracts.Tests.csproj --configuration Release --no-build` -- expected: complete Contracts test project passes.
- `git diff --check` -- expected: no whitespace errors.

## Auto Run Result

Status: done

Summary: Expanded the canonical schema golden fixture with populated, non-canonical `EnumValues` and collection descriptors whose mixed-case names, input sequence, and numeric `Order` values discriminate ordinal name sorting. Added an equivalent-permutation assertion and pinned the resulting complete JSON plus independently derived SHA-256 `0c44b927ac1a3a56ada1dadeac6e08a4b16b79ad909d1c12f74e4c7006cd6766`.

Files changed:
- `tests/Hexalith.FrontComposer.Contracts.Tests/Schema/CanonicalSchemaMaterialFingerprintVectorTests.cs` -- enriched the literal vector and added collection/enum permutation coverage.
- `_bmad-output/implementation-artifacts/spec-canonical-fingerprint-vector-coverage.md` -- recorded the implementation contract, review triage, verification, and completion evidence.

Review findings breakdown:
- Patches applied: one medium entry (three corroborating findings) made collection ordering discriminator-safe against linguistic comparison and numeric-order sorting.
- Items deferred: one pre-existing medium item records that the public schema test-vector identifier is not independently pinned; it is separate from DW-695.
- Rejected: replacing the private fixture did not remove a published vector because the old hash existed only in this test and the enriched fixture retains its prior cases; missing execution evidence was premature because finalization records it here; stale Code Map coordinates were low-impact spec-only bookkeeping; ignored bundle provenance was disproved by the captured intent contract and tracked Story 11.21 citation; separate golden documents are unnecessary because each ordering property occupies a distinct location in the exact JSON assertion; producer/MCP end-to-end coverage is not part of the property-level ledger intent.

Follow-up review recommendation: false. Patched entry counts: high 0, medium 1, low 0. The patch was focused, independently reverified, and leaves no named unverified patch risk.

Verification performed:
- Release build of `Hexalith.FrontComposer.Contracts.Tests.csproj`: passed with 0 warnings and 0 errors.
- Focused `CanonicalSchemaMaterialFingerprintVectorTests` lane: 7 passed, 0 failed, 0 skipped.
- Complete Contracts test project: 233 passed, 0 failed, 0 skipped.
- Independent extraction plus `sha256sum`: matched `0c44b927ac1a3a56ada1dadeac6e08a4b16b79ad909d1c12f74e4c7006cd6766`.
- `git diff --check`: passed; no whitespace errors.
- Matrix audit: both golden canonicalization and equivalent-permutation rows are covered by executed passing tests.
- Scope audit: production code, dependencies, submodules, `.bmad-loop/**`, and `_bmad-output/implementation-artifacts/deferred-work.md` are unchanged.

Residual risks: The deferred `TestVectorId` literal-pin gap remains pre-existing and does not affect collection or enum canonicalization coverage. No residual risk remains for DW-695.
