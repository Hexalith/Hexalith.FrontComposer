---
title: 'DW-671-675: Harden the FC-NIP command-target contract'
type: 'feature'
created: '2026-08-27'
status: 'done'
baseline_revision: 'e54232737a9a62879810588bd7f5b41c10f8ddd3'
baseline_commit: 'e54232737a9a62879810588bd7f5b41c10f8ddd3'
review_loop_iteration: 1
followup_review_recommended: false
context:
  - '{project-root}/_bmad-output/project-context.md'
warnings: [oversized]
deferred: []
---

<intent-contract>

## Intent

**Problem:** The FC-NIP target-identity contract is guarded by duplicated C# and TypeScript literals, its guard names and decision chronology have drifted, its two timestamps appear to use different clock seams, and target-resolution suppression lacks a redaction and rate-observability contract.

**Approach:** Move shared contract expectations into one language-neutral manifest, rename the guards around command-target identity, clarify historical provenance and the common clock, and define plus implement bounded success/failure telemetry whose counts expose the suppression rate without business data.

## Boundaries & Constraints

**Always:** Preserve the 2026-07-04 base record and its 2026-07-05 approval/update as distinct chronology; keep `CapturedAt` and `ObservedAt` semantically distinct while naming the same Shell `TimeProvider`; keep target failure FC-NIP-only and dispatch/lifecycle-neutral; use source-generated logging with unique event IDs, a closed framework-owned category, and no command, target, scope, or exception payload; keep C# and TypeScript pins case-sensitive and table-exact from the same manifest.

**Block If:** Resolving the bundle would require a new public telemetry API, a package/dependency change, a different FC-NIP target/materiality decision, or an occupied event ID.

**Never:** Edit `_bmad-output/implementation-artifacts/deferred-work.md`; rewrite historical validation evidence as though renamed guards existed earlier; infer expectations from the Markdown under test; log `ViewKey`, `EntityKey`, `PriorStatus`, `ExpectedStatus`, `TenantId`, `UserId`, command values, provider exception text, or other adopter data; edit generated output or submodules.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Shared contract validation | C# or TypeScript guard loads the tracked manifest | Both apply identical document fragments and exact table rows | Missing/malformed schema, document, heading, separator, or cell fails the guard |
| Successful target resolution | Declared target resolves to a valid immutable snapshot | Information event 5913 records one payload-free success for the generated form category | Logging-provider failure is swallowed and does not affect dispatch |
| Failed target resolution | Provider/validation returns a closed failure category | Warning event 5912 records only that category and target remains ineligible | Dispatch/lifecycle continue; no fallback and no sensitive data is emitted |
| Cancellation | Caller cancellation interrupts resolution | Neither success nor fail-closed completion contributes to the rate | Existing cancellation semantics remain authoritative |
| Rate calculation | Events 5912 and 5913 in one form-category/window | `5912 / (5912 + 5913)` is the exact target-resolution suppression rate | Zero completions yields no rate rather than a fabricated value |

</intent-contract>

## Code Map

- `tests/contract-fixtures/fc-nip-command-target-identity-contract.json` -- new sole source for shared paths, normalized positive/negative pins, table headings, and exact rows; never derive expected values from the documents under test.
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Docs/FcNipRowIdentityProducerContractTests.cs` -- rename file/class to `FcNipCommandTargetIdentityContractTests`; deserialize and execute the shared manifest while retaining C#-specific source/order assertions and section-bounded Markdown parsing.
- `tests/e2e/specs/fc-nip-row-identity-contract.spec.ts` -- rename to `fc-nip-command-target-identity-contract.spec.ts`; load the same manifest and retain only TypeScript-specific source/order assertions.
- `tests/e2e/package.json` -- point `test:fc-nip` at the renamed Playwright spec; `.github/workflows/quality.yml` already invokes this script and is read-only.
- `_bmad-output/contracts/fc-nip-row-identity-producer-contract-2026-07-04.md` -- preserve historical filename/title; distinguish record creation (2026-07-04) from decision approval/update (2026-07-05), label Story 9.1 as originating ownership, and use the canonical Shell clock name.
- `_bmad-output/contracts/fc-nip-command-target-identity-contract-2026-08-12.md` -- clarify base chronology, use `FrontComposer Shell TimeProvider` for capture and fallback observation, and specify events 5912/5913, logger category, redaction, and rate formula.
- `_bmad-output/planning-artifacts/prd.md` -- reconcile D-4 with the 2026-07-04 record-creation and 2026-07-05 approval/update chronology.
- `src/Hexalith.FrontComposer.SourceTools/Emitters/CommandFormEmitter.cs` -- retain Warning event 5912 and its closed failure categories; emit payload-free Information event 5913 once after non-null target resolution through a non-throwing helper.
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Emitters/CommandFormEmitterTests.cs` -- pin generated logger category, IDs/names/levels/templates, the exact closed failure-category producer set, success placement, failure neutrality, non-fatal logging-fault containment, and absence of sensitive placeholders.
- `tests/Hexalith.FrontComposer.Shell.Tests/Generated/CommandTargetGeneratedFormTests.cs` -- prove one redacted 5912 failure versus one payload-free 5913 success, non-fatal logging-provider faults for both events, and unchanged dispatch/lifecycle behavior with sentinel target data.
- `docs/reference/components/datagrid.md` -- document the operator-facing target-resolution events, redaction boundary, and suppression-rate calculation.
- `_bmad-output/contracts/analyzer-policy-exception-ledger-v1.json` -- reseal the tracked test identifier inventory after the C# file/class move; this governance seal is not the deferred-work ledger.
- `_bmad-output/implementation-artifacts/spec-9-3-define-explicit-command-target-identity.md` and `_bmad-output/implementation-artifacts/spec-9-4-converge-terminal-outcomes-on-one-producer-boundary.md` -- update live Code Map/review navigation references to the renamed guards while leaving dated evidence commands untouched.

## Tasks & Acceptance

**Execution:**
- [x] `tests/contract-fixtures/fc-nip-command-target-identity-contract.json`, `tests/Hexalith.FrontComposer.SourceTools.Tests/Docs/FcNipRowIdentityProducerContractTests.cs`, and `tests/e2e/specs/fc-nip-row-identity-contract.spec.ts` -- add the declarative manifest, rename both guards, and make both validate the manifest schema then apply its shared pins while preserving language-specific checks. Normalize manifest fragments before comparison; reject backslash traversal, duplicate table identities, and inconsistent row widths; retain the existing ten-second TTL implementation pin.
- [x] `_bmad-output/contracts/fc-nip-row-identity-producer-contract-2026-07-04.md`, `_bmad-output/contracts/fc-nip-command-target-identity-contract-2026-08-12.md`, `_bmad-output/planning-artifacts/prd.md`, `_bmad-output/implementation-artifacts/spec-9-3-define-explicit-command-target-identity.md`, and `docs/reference/components/datagrid.md` -- record explicit creation-versus-approval chronology across every named consumer, one clock seam, redaction, event identity/category/level, the suppression-rate formula, and the requirement that both Information and Warning completions be retained before computing a rate.
- [x] `src/Hexalith.FrontComposer.SourceTools/Emitters/CommandFormEmitter.cs`, `tests/Hexalith.FrontComposer.SourceTools.Tests/Emitters/CommandFormEmitterTests.cs`, and `tests/Hexalith.FrontComposer.Shell.Tests/Generated/CommandTargetGeneratedFormTests.cs` -- add event 5913; swallow only non-fatal logging-provider exceptions; compare the produced 5912 category set exactly; and add focused success, suppression, both-event logging-fault, cancellation, redaction, and lifecycle-neutrality regression evidence.
- [x] `tests/e2e/package.json`, `_bmad-output/implementation-artifacts/spec-9-3-define-explicit-command-target-identity.md`, and `_bmad-output/implementation-artifacts/spec-9-4-converge-terminal-outcomes-on-one-producer-boundary.md` -- update live guard discovery/navigation references while preserving dated evidence text.
- [x] `_bmad-output/contracts/analyzer-policy-exception-ledger-v1.json` -- reseal the test identifier inventory using an alternate temporary Git index that models the rename without touching the real index.

**Acceptance Criteria:**
- Given either governance suite, when a shared FC-NIP fragment or exact table cell drifts, then both suites fail from the same manifest expectation.
- Given repository navigation and test discovery, when FC-NIP target guards run, then their file/class/spec names describe command-target identity and `test:fc-nip` remains blocking.
- Given the base contract, successor, PRD D-4, and live Story 9.3 authority wording, when their chronology is read, then 2026-07-04 is unambiguously record creation and 2026-07-05 approval/update, with no implied missing contract.
- Given capture and terminal fallback, when clock ownership is reviewed, then both name the same FrontComposer Shell `TimeProvider` while their timestamps remain distinct.
- Given valid and failed target resolutions containing sentinel business data, when logs are captured, then exactly one completion event contributes to the documented rate, 5912 remains Warning, 5913 is Information, and no sensitive value or exception text appears.
- Given a logging-provider fault or target failure, when submit proceeds, then command dispatch/lifecycle semantics remain unchanged and FC-NIP alone fails closed.
- Given target-resolution counts, when an operator computes the documented rate, then both Information 5913 and Warning 5912 retention is a stated prerequisite and filtering either level yields no rate.

## File List

- `_bmad-output/contracts/analyzer-policy-exception-ledger-v1.json`
- `_bmad-output/contracts/fc-nip-command-target-identity-contract-2026-08-12.md`
- `_bmad-output/contracts/fc-nip-row-identity-producer-contract-2026-07-04.md`
- `_bmad-output/implementation-artifacts/spec-9-3-define-explicit-command-target-identity.md`
- `_bmad-output/implementation-artifacts/spec-9-4-converge-terminal-outcomes-on-one-producer-boundary.md`
- `_bmad-output/implementation-artifacts/spec-dw-671-675-harden-fc-nip-command-target-contract.md`
- `_bmad-output/planning-artifacts/prd.md`
- `docs/reference/components/datagrid.md`
- `src/Hexalith.FrontComposer.SourceTools/Emitters/CommandFormEmitter.cs`
- `tests/Hexalith.FrontComposer.Shell.Tests/Generated/CommandTargetGeneratedFormTests.cs`
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Docs/FcNipCommandTargetIdentityContractTests.cs`
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Docs/FcNipRowIdentityProducerContractTests.cs` (renamed)
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Emitters/CommandFormEmitterTests.cs`
- `tests/contract-fixtures/fc-nip-command-target-identity-contract.json`
- `tests/e2e/package.json`
- `tests/e2e/specs/fc-nip-command-target-identity-contract.spec.ts`
- `tests/e2e/specs/fc-nip-row-identity-contract.spec.ts` (renamed)

## Spec Change Log

### 2026-08-27 — Review repair iteration 1

- Triggering findings: the plan omitted the PRD and live Story 9.3 chronology consumers named by DW-673; it also allowed fatal logging exceptions to be swallowed, did not require the 5912 logging-fault runtime path, and omitted the both-level retention prerequisite for a valid 5912/5913 rate.
- Amended: the Code Map, execution tasks, acceptance criteria, and File List now cover all chronology consumers, preserve fatal exception propagation while containing non-fatal logger faults for both events, require exact category closure and stronger shared-manifest validation, retain the TTL implementation pin, and condition rate calculation on complete Information/Warning retention.
- Known-bad state avoided: a partially reconciled contract set, a misleading rate under level filtering, fatal process failures masked as logger faults, and a 5912 path whose dispatch neutrality was only inferred.
- KEEP: one language-neutral manifest consumed by both renamed guards; case-sensitive exact contract pins; 2026-07-04 creation versus 2026-07-05 approval/update wording; the shared FrontComposer Shell `TimeProvider` terminology; redacted events 5912/5913 in the generated form category; payload-free success logging; dispatch/lifecycle neutrality; cancellation silence; historical evidence commands left unchanged; analyzer resealing through an alternate index; and the deferred-work ledger remaining untouched.

## Review Triage Log

### 2026-08-27 — Review pass
- intent_gap: 0
- bad_spec: 4: (high 1, medium 3, low 0)
- patch: 6: (high 0, medium 5, low 1)
- defer: 2: (high 0, medium 0, low 2)
- reject: 8
- addressed_findings:
  - `[medium]` `[bad_spec]` Added the PRD and live Story 9.3 to the chronology reconciliation plan so every DW-673 consumer explains the 2026-07-04/2026-07-05 relationship.
  - `[medium]` `[bad_spec]` Required complete Information-and-Warning retention before computing the documented 5912/5913 rate.
  - `[high]` `[bad_spec]` Replaced catch-all telemetry containment with non-fatal-only containment that preserves the repository's fatal-exception invariant.
  - `[medium]` `[bad_spec]` Required a runtime 5912 logging-provider-fault test in addition to the existing 5913 path.

### 2026-09-05 — Review pass
- intent_gap: 0
- bad_spec: 0
- patch: 6: (high 0, medium 6, low 0)
- defer: 3: (high 0, medium 0, low 0)
- reject: 15
- findings:
  - `[medium]` `[patch]` `architecture.md:67-69` still names the historical base “the 2026-07-05 row-context contract,” which is the dual-dating confusion this bundle closes.
  - `[medium]` `[patch]` Shared-manifest `notContains` arrays are empty on chronology documents, so stale “2026-07-05 row-context” phrasing can remain beside new wording.
  - `[false]` `[reject]` Logger-null / non-fatal logger swallow omitting 5912/5913 from the rate: specified containment; the rate is defined over retained events, not a second sink.
  - `[medium]` `[patch]` `CommandFormEmitterTests.cs:447-454` collects prefix-shaped quoted strings, so `FailCommandTargetResolution("internal-error")` cannot fail the exact closed-set pin.
  - `[false]` `[reject]` SameAsSource-only runtime 5912/5913 and untested extra 5912 categories: spec asked for one success and one failure; both events share `ResolveCommandTargetAsync`.
  - `[low]` `[reject]` `datagrid.md` omits the closed 5912 list and a formal observation-window definition; cancellation is already “per non-cancelled attempt,” and the successor contract remains the operator authority.
  - `[false]` `[reject]` Success-path redaction omitting LaneKey/ProjectionTypeName and logger scopes: 5913 state is only `{OriginalFormat}`, and the helper does not `BeginScope`.
  - `[low]` `[reject]` Duplicate `IsFatalCommandTargetResolutionException` vs `IsFatalCommandCleanupException` lists: pre-existing cleanup helper cloned; unifying them is a broader emitter refactor.
  - `[defer]` Analyzer ledger `schemaVersion` 1.0→1.1 and identifier algorithm rewrite in the since-baseline diff: Story 11.23+ governance, not this rename reseal.
  - `[false]` `[reject]` C# `notContains` using `Case.Sensitive`: Intent Always requires case-sensitive pins.
  - `[low]` `[reject]` Dropped Story 9.4 retirement comment on EventStore status-query pins: comment-only, not a live guard failure.
  - `[defer]` `tests/e2e/package.json` Playwright/TypeScript/Node/cross-env churn besides `test:fc-nip`: other stories since the Aug 27 baseline.
  - `[defer]` `CommandFormEmitterTests` HFC1016 parse-time rejection and DW-683 syntax-tree fixture: other stories in the same file since baseline.
  - `[medium]` `[patch]` `CommandFormEmitter.cs:678-683` logs 5913 after Core returns a snapshot even when the caller token is already cancelled.
  - `[false]` `[reject]` carried logger-fault rate omission (edge-case duplicate of the specified containment finding).
  - `[low]` `[reject]` OOM wrapped in `TypeInitializationException` treated as non-fatal: not a demonstrated logger fault; unwrapping wrappers adds catch complexity.
  - `[medium]` `[patch]` carried exact-set regex finding (edge-case duplicate of the `FailCommandTargetResolution` pin).
  - `[medium]` `[patch]` C# manifest schema accepts `tables: []`, so exact table-cell pins never run.
  - `[medium]` `[patch]` C# accepts a table row with two identical cells while TypeScript `requireStringArray` rejects duplicates.
  - `[low]` `[reject]` Non-integer `schemaVersion` throws `InvalidOperationException` from `GetInt32` rather than `InvalidDataException`: both suites still fail closed; tracked schema is integer `1`.
  - `[false]` `[reject]` Base-contract heading pins (`Approved Payload Source`, `Resolution date:`) not copied into the manifest: those headings remain; chronology/table pins replaced the old prose checklist.
  - `[medium]` `[patch]` `datagrid.md` was a named chronology consumer but received only 5912/5913 rate text, not 2026-07-04 creation vs 2026-07-05 approval/update.
  - `[medium]` `[patch]` carried “compare the produced 5912 category set exactly” claim (same regex pin).
  - `[medium]` `[patch]` No runtime test that a fatal 5912/5913 logger fault (`OutOfMemoryException`) escapes submit without dispatch; the non-fatal theory still passes if the fatal helper always returns false.

## Design Notes

The shared manifest is declarative (`documents[]` with path/contains/notContains and `tables[]` with path/heading/rows). Consumers validate its schema before applying it; neither consumer owns a second expectation list. Event 5912 failures and event 5913 successes share the generated `<Command>Form` logger category, so their counts have the same population and window without adding a public Meter or telemetry service.

## Verification

**Commands:**
- `npm --prefix tests/e2e run typecheck && npm --prefix tests/e2e run test:fc-nip` -- expected: shared-manifest TypeScript checks and renamed browserless suite pass.
- `dotnet build tests/Hexalith.FrontComposer.SourceTools.Tests/Hexalith.FrontComposer.SourceTools.Tests.csproj --configuration Release --no-restore -m:1 /nr:false -p:NuGetAudit=false -p:MinVerVersionOverride=4.0.0` -- expected: zero warnings/errors.
- `DiffEngine_Disabled=true dotnet tests/Hexalith.FrontComposer.SourceTools.Tests/bin/Release/net10.0/Hexalith.FrontComposer.SourceTools.Tests.dll -class Hexalith.FrontComposer.SourceTools.Tests.Docs.FcNipCommandTargetIdentityContractTests` -- expected: focused manifest guard passes.
- `dotnet build tests/Hexalith.FrontComposer.Shell.Tests/Hexalith.FrontComposer.Shell.Tests.csproj --configuration Release --no-restore -m:1 /nr:false -p:NuGetAudit=false -p:MinVerVersionOverride=4.0.0` followed by focused direct-run methods for `CommandTargetGeneratedFormTests` and `AnalyzerPolicyGovernanceTests.AnalyzerPolicy_IdentifierInventory_MatchesSeal` -- expected: runtime telemetry/redaction and alternate-index inventory evidence pass.
- `pwsh ./eng/validate-docs.ps1` -- expected: documentation validation passes.
- `DiffEngine_Disabled=true dotnet test Hexalith.FrontComposer.slnx --filter "Category!=Performance&Category!=e2e-palette&Category!=NightlyProperty&Category!=Quarantined"` -- expected: blocking solution lane passes.
- `git diff --check` -- expected: no whitespace or conflict-marker errors.

**Results (implementation pass):**
- `npm --prefix tests/e2e run typecheck && npm --prefix tests/e2e run test:fc-nip` -- typecheck clean; browserless suite 3/3 passed.
- SourceTools.Tests Release build -- 0 warnings / 0 errors.
- `FcNipCommandTargetIdentityContractTests` -- 5/5 passed; `Emit_CommandTargetTelemetryUsesClosedRedactedCompletionContract` -- 1/1 passed.
- Shell.Tests Release build -- 0 warnings / 0 errors; `CommandTargetGeneratedFormTests` -- 54/54 passed; `AnalyzerPolicy_IdentifierInventory_MatchesSeal` -- 1/1 passed. An alternate `GIT_INDEX_FILE` read-tree of HEAD listed only the renamed command-target guards; no identifier-inventory reseal was required because the rename adds no underscore-bearing public/protected test declarations.
- `pwsh ./eng/validate-docs.ps1` -- passed (`artifacts/docs/validation-manifest.json`).
- Blocking solution lane -- 4670 succeeded / 1 failed. The sole failure is baseline `CiGovernanceTests.EventStoreRuntimeIdentitySeparatesCurrentCompatibilityFromHistoricalApproval`, which expects Builds gitlink `0a54e63a7903bd599e35b79159782b4c84d01c07` while HEAD has `308e3921d60d2e8f87dd69a7f9b6f3dd016df9ef`. That guard is outside this bundle.
- `git diff --check` -- no whitespace or conflict-marker errors. Git reported the usual CRLF normalization warning for the JSON fixture.

## Auto Run Result

Status: implementation-complete; mandatory review pending.

Summary: Shared FC-NIP manifest, renamed guards, 2026-07-04 creation versus 2026-07-05 approval/update chronology, shared Shell `TimeProvider` wording, and redacted 5912/5913 completion telemetry are in place. Live Story 9.3 authority wording now states the distinct chronology; dated 9.3 evidence commands still name the pre-rename guards.
