---
title: '13.7 [I · UX-F] Provide Reusable UX Assertions for Adopters'
type: 'feature'
created: '2026-10-18'
status: 'done'
baseline_commit: 'b9e1490cfe1c51406d3c8d4dc213971d06911a24'
route: 'dispatch'
review_loop_iteration: 1
context:
  - '{project-root}/_bmad-output/implementation-artifacts/epic-13-context.md'
  - '{project-root}/_bmad-output/planning-artifacts/ux-design.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Adopter test engineers have no deterministic Testing-package helpers for the canonical UX matrices (UX-AM-1, VR-1, FM-1, OF-1, SS-1, AE-1), so downstream modules need app-specific selectors and machine-dependent waits to verify generated failure and recovery UX.

**Approach:** Extend `Hexalith.FrontComposer.Testing` (fake time, redacted evidence, assertions) with message-sequence, coalescing-boundary, channel-distinction, origin-capture, unobscured-focus geometry, timing-constant and AE-evidence helpers, each exercised by a consumer test against a realistic failure/policy state.

## Boundaries & Constraints

**Always:** Extend existing `Evidence.cs` / `RedactedEvidenceFormatter` / `Assertions.cs` / `FrontComposerTestOptions.TimeProvider`; never duplicate. Identify messages by AM row ID plus canonical copy. Role/name assertions first; `data-testid` only supplements. Evidence redacted by default (no token, JWT, tenant payload, stack trace, raw backend metadata, PII). Update `PublicAPI.Shipped.txt` baseline for any public API change. Re-seal analyzer test-identifier inventory in `analyzer-policy-exception-ledger-v1.json` (count + sha256) for added underscore test names.

**Decisions:** (1) Helpers live in the .NET Testing package only: pure assertions over caller-supplied measured data (geometry rects, AX snapshots) plus fake-time helpers; no browser dependency and no new TypeScript distribution. (2) Ship a minimal in-package fake `TimeProvider`; no `Microsoft.Extensions.TimeProvider.Testing` reference.

**Never:** Close OI-16, G-4, FLUENT-APP-1 or Product approval. Re-seal `AnalyzerPolicy_GovernanceContract_FailsClosed` drift (GOV-1 owned). Change Shell runtime behavior or constants. Use real-time waits.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Sequence match | Ordered [AM-11, AM-13], count 2, one operation | Passes | N/A |
| Sequence drift | Extra, missing, reordered or duplicate message | Fails naming AM row and channel | Redacted diff |
| Coalescing | Advance 249ms then 250ms | Nothing at 249; last eligible at 250; stale discarded | Fails on early/late emit |
| Terminal cancel | Terminal outcome / focused summary / nav failure while pending | Pending cancelled, immediate announce | Fails if intermediate emitted |
| Channel misuse | One event hits live region and focus | Fails | Names wrong channel |
| Origin removed | Origin removed/disabled before close | Asserts route-h1 (FM-04/05) or form-heading (FM-11) fallback, never body | Fails on body |
| Time boundary | 1,999/2,000ms, 119,999/120,000ms | N-1 not fired, N fired | Fails on drift |

</frozen-after-approval>

## Code Map

- `src/Hexalith.FrontComposer.Testing/Evidence.cs` -- `RedactedEvidenceFormatter` (l.50-70), extend for AE evidence.
- `src/Hexalith.FrontComposer.Testing/Assertions.cs` -- add new assertion classes alongside.
- `src/Hexalith.FrontComposer.Testing/TestFaultEvidenceRecorder.cs` -- recorder pattern to copy.
- `src/Hexalith.FrontComposer.Testing/FrontComposerTestOptions.cs:33`, `FrontComposerTestHostBuilder.cs:215` -- TimeProvider seam.
- `src/Hexalith.FrontComposer.Testing/PublicAPI.Shipped.txt`; `tests/Hexalith.FrontComposer.Testing.Tests/PackageBoundaryTests.cs:21` -- baseline check.
- `src/Hexalith.FrontComposer.Shell/Services/Announcements/SurfaceAnnouncementCoordinator.cs` (250ms, l.8) -- behavior under test; `FreshRowAnnouncementLedger.cs:11` (10s).
- `src/Hexalith.FrontComposer.Shell/Options/FcShellOptions.cs` -- 2000/3000/15s/1000/120000 constants; `ProjectionHubRetryPolicy.cs:9` (30000, internal: duplicate + drift test); `FcLifecycleWrapper.razor.cs:26` (10000).
- `_bmad-output/planning-artifacts/ux-design.md` l.144-163 (AM channels, coalescing), 164-196 (AM rows), 198-332 (VR/FM/OF/SS/AE) -- authority.
- `src/Hexalith.FrontComposer.Shell/Resources/FcShellResources.resx` -- canonical copy; no AM-ID catalog exists, helper defines one resolved from resources.
- `tests/e2e/helpers/responsive-accessibility.ts` (`focusGeometry` l.536, `expectTargetSizes` l.257) -- reference semantics only.
- `_bmad-output/contracts/analyzer-policy-exception-ledger-v1.json` l.98-104 -- identifier seal.
- Docs: `src/Hexalith.FrontComposer.Testing/README.md`, `docs/how-to/test-generated-components.md`.

## Tasks & Acceptance

**Execution (review round 2):**
- [x] `Testing/TestTimeProvider.cs`, `TestTimeProviderTimer.cs` — retain deterministic due-order/Change/disposal/UTC behavior, invoke callbacks outside the clock lock, capture creation ExecutionContext; cover concurrent change/dispose and ambient context.
- [x] `Testing/UxAnnouncementAssertions.cs` — retain exact canonical sequence/coalescing/channel checks; reject conflicting lifecycle terminal outcomes (including Confirmed/IdempotentConfirmed) for one operation; retain all unfiltered observations. Permit legitimate unnamed polite status semantics while asserting canonical message content separately.
- [x] `Testing/UxAnnouncementCatalog.cs`, `TestingFailureModeTests.cs` — independently pin each AM row and supported variant to the intended localized resource/copy; seeded wrong-resource mapping must fail.
- [x] `Testing/UxSurfaceSnapshot.cs`, `UxStateAssertions.cs` — add caller-measured state-specific facts for SS rows (including disabled/selected tab and panel ownership, failure/no-success truth, non-live/focus semantics where required); add independent table-driven SS-01..49 acceptance/terminality/lifecycle and mismatch checks. Reject absent blocked-submit handles.
- [x] `Testing/UxFieldSnapshot.cs`, `UxValidationAssertions.cs`, supporting single-type files — supply measured summary text/count, declared group membership/name/description/order, measured group relationships, and prior stable control/error identities. Check canonical AM-18/19 copy separately from accessible label, invalid-only links in complete declared order, stable rerender targets, preserved useful input, visible label within a richer accessible name, and every ASCII whitespace IDREF separator.
- [x] `Testing/AccessibilityMeasurements.cs`, `AccessibilityEvidence.cs`, recorder/formatter — preserve the bounded typed schema and add safe ordinal surface/state references plus typed theme. Retain observed loss/clipping/overlap/truncation, forced-color cue preservation, animation suppression, silent expiry and keyboard/axe outcomes as applicable; no arbitrary text or sensitive identifiers.
- [x] `Testing.Tests/TestingFailureModeTests.cs` — preserve realistic rejection/denial/offline/admission/exhaustion consumers and all frozen matrix cases; add authoritative safe mapped-rejection consumption retaining Rejected identity with no duplicate AM-14 speech. Parse and verify all supplied AE values, including failed findings and measured geometry. Use existing/new tests with meaningful seeded drift.
- [x] `Testing/PublicAPI.Shipped.txt`, allowed ledger count/hash, README/how-to — intentionally update the public measurement API, test-identifier seal and adopter examples. Preserve unrelated seals/dependencies/Shell runtime.

**Acceptance Criteria:**
- Given the epics.md Story 13.7 criteria, when the Testing suite runs, then every helper passes against realistic failure states and fails on seeded drift.
- Given evidence output, when inspected, then it contains no token, JWT, tenant payload, stack trace or PII.

## Implementation Notes

- Added the in-package `TestTimeProvider` through the existing options seam, canonical `UxTiming` budgets, and exact N-1/N assertions. Shell runtime code and constants are unchanged.
- Added localized AM-01..31 expectations; exact message/count/group/channel assertions; trailing-window and immediate-cancellation checks; linked validation/field/input assertions; SS-01..49 state truth and blocked-submit checks; reference-based origin restoration and full-focus geometry assertions.
- Extended the existing evidence formatter for AE-01..09 with a fixed numeric/boolean/enum schema, measured geometry, target-size exceptions/rationale/equivalent references, and keyboard paths. Diagnostic diffs omit arbitrary observed copy, handles, identities, and input values.
- Added realistic rejection, denial, offline, mapped/client validation, admission-gate, and polling-exhaustion consumers plus seeded drift. Updated the intentional API baseline, README/how-to, and only the allowed test-identifier count/hash seal (3568 / `0fac9aca8b99a62fde39d3bbfbca307e54fe74afc2240641522c94da32c97aa6`).
- The unchanged pre-implementation Release assembly reproduced four existing Testing-suite failures. Test-only fixture corrections align pinned package expectations with current central versions and stamp tenant/user ownership on the existing projection seed. No dependency was changed.
- OI-16, G-4, FLUENT-APP-1, Product approval, and the GOV-1-owned drift seal remain unchanged.

## Spec Change Log

### Review round 1 → implementation round 2

The review verified insufficient non-frozen measurement contracts for SS semantics, VR summary/group/stability relationships, and AE provenance/outcomes (R1-B4/B5/B6/B7/B11/B12). The execution tasks now name the measured facts and independent regressions required to prevent those false positives; the frozen intent is unchanged. Patch-class findings are absorbed into re-derivation because this bad-spec loop takes precedence.

**KEEP:** Preserve deterministic clock/timing APIs and budgets, canonical localized AM catalog, exact sequence and 249/250/immediate-cancellation checks, reference-origin/fallback and unobscured geometry behavior, fixed redaction schema/retention, existing failure-state consumers, API baseline discipline, allowed identifier-only seal, docs example, and the confirmed package/scope fixture corrections. Retain 89/89 suite and clean Release build evidence as prior-round evidence; rerun relevant checks on the re-derived tree. Preserve the original frozen block byte-for-byte after line-ending normalization. The first-round source snapshot at `/tmp/fc137-round1-source` contains these known-good portions for continuity; re-derive affected assertions/measurement contracts from the tasks above.

**Known-bad states to prevent:** caller-provided labels certifying missing state semantics; invented summary/group facts; unstable IDs accepted across rerenders; ambiguous or incomplete AE records; self-referential catalog/state/evidence tests; false rejection of valid labels/IDREFs/order; duplicate terminal claims; callback lock inversion; missing focus handles.

### Final validation fixture reconciliation

The complete docs gate exposed an existing stale content hash for the unchanged, committed `docs/ide-parity-matrix.md`. Its normalized HEAD and worktree bytes both hash to `55ccda2c5292b34ba542b1e3578dfbe4fbf538336c12afda89cb29e30b08fde1`. Updated only that entry in `docs/validation/producer-fingerprints.json` so the required validator binds the current document. The document, manual evidence, SDK/date manifests and explicit `revalidation-pending` status are unchanged. This is a validation-fixture correction, like the current-catalog test pins; no approval or historical evidence seal is advanced. The execution task's preservation requirement continues to apply to approval/evidence seals, including the forbidden GOV-1 drift seal.

## Review Triage Log

| Finding | Verdict | Route | Verified evidence |
| --- | --- | --- | --- |
| R1-B1 | medium | patch | TestTimeProvider.Advance invokes callbacks while holding the clock gate; SurfaceAnnouncementCoordinator holds its own gate while disposing timers and Flush reacquires that gate. Concurrent calls can invert the two locks and hang an adopter test. |
| R1-B2 | medium | patch | TestTimeProviderTimer invokes its callback directly without capturing creation ExecutionContext; the isolated consumer observes the advancing AsyncLocal instead of the timer-creation value, changing ambient culture/context for timer-driven assertions. |
| R1-B3 | medium | patch | AssertDistinctChannels keys only operation/state; distinct Confirmed/Rejected and Confirmed/IdempotentConfirmed states bypass terminal-once validation for the same operation. |
| R1-B4 | medium | bad_spec | UxSurfaceSnapshot has no disabled/selected/panel measurements; AssertState accepts SS-33 as a named button. The original tasks did not specify row-specific observable facts for SS truth. |
| R1-B5 | medium | bad_spec | AssertLinkedSummary constructs canonical AM-18/19 copy but never compares measured summary text/count. The original task did not specify summary-content measurements separate from accessible naming. |
| R1-B6 | medium | bad_spec | AssertLinkedSummary checks only nonempty group identifiers/names; UxFieldSnapshot cannot carry expected group membership or measured/declared description. The task omitted the required measurement contract. |
| R1-B7 | medium | bad_spec | Control/error IDs are checked for uniqueness only within one snapshot; consistently regenerated IDs pass. The task omitted prior stable field identities and rerender comparison. |
| R1-B8 | medium | patch | VisibleLabel != AccessibleName rejects the reachable valid Name / Name (required) input, as the isolated consumer confirms. Additional accessible-name context must preserve the visible label. |
| R1-B9 | medium | patch | The mapped validation consumer adds a client ValidationMessageStore entry and switches kind without an authoritative rejection, safe map, retained Rejected identity, or AM-14 suppression. Its claimed mapped-path regression proof is incomplete. |
| R1-B10 | medium | patch | AssertBlockedSubmit uses ReferenceEquals before checking handles, so two null handles pass; this falsely certifies retained attempted-control focus. |
| R1-B11 | medium | bad_spec | AE records contain only row/time/measurements, with no safe theme or journey provenance. Multiple same-instant AE-06 packets cannot independently identify light/dark proof; the task omitted a safe context schema. |
| R1-B12 | medium | bad_spec | AccessibilityMeasurements records setup plus aggregate Passed but lacks measured clipping/overlap/truncation, cue preservation, animation-stop and silent-expiry outcomes. The evidence task omitted these per-dimension observations. |
| R1-E1 | medium | patch | AssertLinkedSummary compares invalid controls with the complete declared-control list; a declaration containing valid controls therefore rejects a correct invalid-only summary. The isolated consumer confirms this. |
| R1-E2 | medium | patch | DescribedBy.Split accepts only literal spaces; tab/newline/CR/form-feed IDREF separators are valid caller measurements and are rejected, as the isolated consumer confirms. |
| R1-E3 | medium | patch | The edge reviewer independently confirms the exact label/name equality defect described by B8; preserve this finding separately and group by the same root cause. |
| R1-V1 | medium | patch | Pre-verified broken-verification gap: mapping AM-03 to Am02Empty leaves nonempty French copy and channel tests green. Add independent expected resource/copy/variant assertions. |
| R1-V2 | medium | patch | Pre-verified regression gap: no AssertState consumer covers SS-21. Replacing its IdempotentConfirmed mapping with Confirmed leaves current state tests green; verify canonical state/terminality/lifecycle in an independent all-row table. |
| R1-V3 | medium | patch | Pre-verified broken-verification gap: evidence tests check target strings but no retained numeric/boolean facts. Dropping CriticalFindingCount leaves the test green; parse and compare success/failure values and geometry. |


### Review round 2

| Finding | Verdict | Route | Verified evidence |
| --- | --- | --- | --- |
| R2-B1 | medium | patch | FcValidationSummary.RebuildEntries creates one entry per message and unlinked form-level entries; the helper instead compares count/links with invalid-field count. Existing summary.ErrorCount and ordered observed links can represent these valid cases without a new public API. |
| R2-B2 | medium | patch | ErrorId is documented empty when no error exists, but unconditional equality with prior.ErrorId rejects both first failure and error removal after correction. Control IDs must remain stable; compare only error targets that exist in both observations. |
| R2-B3 | medium | patch | Control and error IDs are only separately unique; the same ID can pass both sets and describe its own control. Reject control/error namespace intersections. |
| R2-B4 | medium | patch | Silent and hidden-policy semantics exclude explicit live attributes and alert only; status/log have implicit live semantics and pass. The reported SS-36 status example is reachable. |
| R2-B5 | medium | patch | SS-36 currently checks Editable but allows a supplied Submitting lifecycle and Dispatched/Queued flags. Existing snapshot fields can reject this demonstrated contradiction without introducing an in-flight property. |
| R2-B6 | medium | patch | A Modal=true SS-43 snapshot with role=button passes. Require the modal surface dialog role using existing Semantics. Separate initial-focus geometry remains measured through the focus helpers; this concrete role defect needs no new public measurement API. |
| R2-B7 | medium | patch | Length plus int.TryParse accepts AM-+1 and SS-+1 (and leading whitespace) as row 1; enforce exactly two ASCII digits in announcement/state row IDs. |
| R2-B8 | medium | patch | FormatAccessibility serializes unused nullable properties before the default 256-character bound; a default AE-06 packet truncates before ContrastRatio. Omit absent/unused facts so required measurements survive ordinary default packets, preserving explicit truncation for oversized packets. |
| R2-B9 | medium | patch | Capture returns null under SuppressFlow and Invoke then runs directly in the advancing thread context. The new deterministic timer must execute that demonstrated branch in a clean context instead of inheriting advancing AsyncLocals. |
| R2-B10 | medium | patch | DisposeAsync immediately returns CompletedTask after Remove even when Invoke is still running on a concurrent advancement. Awaiting teardown can therefore race an active callback; track active invocations privately and complete disposal after they drain. |
| R2-B11 | medium | patch | The current real-summary consumer supplies fabricated field/group/error/prior snapshots and renders no referenced controls/errors. Replace these supplied facts with measurements from rendered grouped Fluent controls and error targets before and after validation. |
| R2-B12 | false | reject | AccessibilityEvidenceContext.StateNumber explicitly references a caller-maintained measured journey/state inventory, which can distinguish each contrast measurement and its expected category; the recorder retains caller-measured Passed and ratio and does not claim to infer WCAG thresholds. Same-context records do not establish that required caller inventory attribution is unavailable. An additional pair/category API is not required to express this evidence. |
| R2-E1 | medium | patch | Independently confirms B1: two field messages or form-level errors are emitted by the actual summary but invalid-field-count validation rejects them. |
| R2-E2 | medium | patch | Independently confirms B3: an ErrorId equal to its control or another control passes separate-set uniqueness checks. |
| R2-E3 | medium | patch | Record accepts a required NaN/infinite scalar because HasValue is true; serialization then collapses all facts to the unavailable marker. Reject nonfinite measurement inputs before recording. |
| R2-E4 | medium | patch | Independently confirms the AM half of B7; noncanonical signed/space-padded row identifiers pass ValidateRowChannel. |
| R2-E5 | medium | patch | Independently confirms the SS half of B7; noncanonical signed/space-padded row identifiers select a valid state-table entry. |
| R2-E6 | medium | patch | Independently confirms B4: absent aria-live does not disable status/log implicit live semantics in rows claiming silence. |
| R2-V1 | medium | patch | Pre-verified broken-verification gap: deleting only CriticalFindingCount.HasValue still passes all 134 tests; complete packets and entirely empty packets do not test individual required-field omissions. Add per-row one-field-at-a-time omission cases. |
| R2-V2 | medium | patch | Pre-verified regression gap: changing 24x24 compliance from width AND height to OR still passes all 134 tests, since rectangles are square. Add asymmetric direct and equivalent-control measurements. |

Survivors are grouped as summary entries (B1/E1), error transitions (B2), target ID collision (B3/E2), effective live semantics (B4/E6), Ready truth (B5), modal role (B6), exact row IDs (B7/E4/E5), compact default evidence (B8), suppressed context (B9), async disposal (B10), measured consumer fixtures (B11), finite evidence (E3), AE omission regressions (V1), and asymmetric size regressions (V2). All route to a patch using the existing public surface; no intent or spec loopback is needed. R2-B12 is rejected on the documented external-inventory contract.

## Verification

- `dotnet build Hexalith.FrontComposer.slnx -c Release -m:1 -p:NuGetAudit=false -p:MinVerVersionOverride=1.0.0` — passed, 0 warnings / 0 errors; final log `/tmp/fc137-solution-release.log`. This includes the required Testing Release build.
- `dotnet test --project tests/Hexalith.FrontComposer.Testing.Tests/Hexalith.FrontComposer.Testing.Tests.csproj -c Release --no-build` — passed 89/89, including public API, package contents, offline clean-package consumer, and all new UX assertions; `/tmp/fc137-testing-suite-release.log`.
- `dotnet tests/Hexalith.FrontComposer.Testing.Tests/bin/Release/net10.0/Hexalith.FrontComposer.Testing.Tests.dll -class '*TestingFailureModeTests' -xml /tmp/fc137-matrix-tests.xml` — passed 43/43; all 14 new UX cases have named `Pass` results in the XML. Log `/tmp/fc137-matrix-tests.log`. The runner accepts `-xml` with a deprecation notice in favor of `-result-xml`.
- `dotnet tests/Hexalith.FrontComposer.Shell.Tests/bin/Release/net10.0/Hexalith.FrontComposer.Shell.Tests.dll -method '*AnalyzerPolicy_IdentifierInventory_MatchesSeal'` — passed 1/1; `/tmp/fc137-identifier-tests.log`. No GOV-1 drift seal was changed.
- `NuGetAudit=false MinVerVersionOverride=1.0.0 pwsh -NoProfile -File eng/validate-docs.ps1` — all 7 executable snippets passed, including both how-to examples, and DocFX completed. The overall gate failed on one pre-existing stale producer fingerprint for untouched `docs/ide-parity-matrix.md`: expected `02b8efd28ba927956de79f0a81bcdfec8a31a29a221be9c6cee312a5fe6e6a74`, actual `55ccda2c5292b34ba542b1e3578dfbe4fbf538336c12afda89cb29e30b08fde1`. HEAD and working-tree normalized hashes both equal that actual value. The unrelated producer seal is unchanged. Log `/tmp/fc137-docs-validation.log`; generated manifest `artifacts/docs/validation-manifest.json`.
- Before edits, `aspire start --apphost src/Hexalith.FrontComposer.AppHost/Hexalith.FrontComposer.AppHost.csproj --non-interactive` succeeded. The corresponding exact-target `aspire describe` showed counter-web Running/Healthy, security Running/Unhealthy, and dependent resources Waiting. Exact-target `aspire stop ... --non-interactive` succeeded before builds; no topology change was made.
- `git diff --check` — passed.


### Review round 2 implementation verification

- Re-derived SS measurements (including silent policy-hidden SS-06 and selected-but-disabled SS-33 alternatives), VR summary/group/prior-target contracts, and safe ordinal/theme AE provenance and per-dimension outcomes. Timer callbacks execute outside the clock lock with creation execution context; AM terminal-once checks include distinct terminal identities for one operation. Authoritative mapped-rejection consumption retains Rejected and suppresses AM-14 through the pending window.
- `dotnet build Hexalith.FrontComposer.slnx -c Release -m:1 -p:NuGetAudit=false -p:MinVerVersionOverride=1.0.0` — passed, 0 warnings / 0 errors; `/tmp/fc137-round2-solution-release.log`.
- `dotnet test --project tests/Hexalith.FrontComposer.Testing.Tests/Hexalith.FrontComposer.Testing.Tests.csproj -c Release --no-build` — passed 180/180, including API baseline, package contents, and the offline clean consumer; `/tmp/fc137-round2-suite.log`.
- `dotnet tests/Hexalith.FrontComposer.Testing.Tests/bin/Release/net10.0/Hexalith.FrontComposer.Testing.Tests.dll -class '*TestingFailureModeTests' -result-xml /tmp/fc137-round2-matrix.xml` — passed 134/134; named results include all frozen matrix cases plus independently pinned AM resources/variants and SS-01..49 semantics. Log `/tmp/fc137-round2-matrix.log`.
- `dotnet tests/Hexalith.FrontComposer.Shell.Tests/bin/Release/net10.0/Hexalith.FrontComposer.Shell.Tests.dll -method '*AnalyzerPolicy_IdentifierInventory_MatchesSeal'` — passed 1/1; `/tmp/fc137-round2-identifier.log`. The only ledger changes are the allowed test identifier count/hash, now 3566 / `3ea11a917ec7d3decb74c916e8f83beee78e97d9ad64e3a9b48802c249793a09`.
- Seeded verification mutations each failed as expected: AM-03 mapped to Am02Empty, SS-21 mapped to Confirmed, and omitted CriticalFindingCount. Separate expected-failure logs/XML are `/tmp/fc137-round2-mutant-catalog.*`, `/tmp/fc137-round2-mutant-ss21.*`, and `/tmp/fc137-round2-mutant-ae.*`. Source files were restored before the final clean solution build and 180/180 suite.
- The first complete run passed 177/180 and exposed stale package-test constants. Test-only expectations now match the existing central catalog (Fluent 5.0.1, localization 10.0.13, test SDK 18.11.0, runner 4.0.1, xUnit 4.0.2); no dependency or Shell runtime changed.
- `NuGetAudit=false MinVerVersionOverride=1.0.0 pwsh -NoProfile -File eng/validate-docs.ps1` is still running at implementation handoff; session `54550`, log `/tmp/fc137-round2-docs.log`. Its final result remains pending and is owned by the parent workflow. Earlier round-one documentation evidence and its unrelated producer-fingerprint blocker remain recorded above.

### Final independent verification and review resolution

All verified findings from both review rounds are resolved. The second-round fixes preserve the reviewed public API. Rendered Fluent-control consumers cover repeated field messages, form-level/form-only errors, first-error appearance, correction/removal, stable persistent targets and ID collisions. Regressions also cover silent/Ready/modal semantics, exact row IDs, suppressed execution context, draining asynchronous timer disposal, each required AE-field omission, finite measurements, default compact packets and asymmetric target sizes. R2-B12 is rejected for the documented caller-inventory contract; nothing is deferred.

- `DiffEngine_Disabled=true dotnet build Hexalith.FrontComposer.slnx -c Release -m:1 -p:NuGetAudit=false -p:MinVerVersionOverride=1.0.0` — passed, 0 warnings / 0 errors; `/tmp/fc137-final/solution-release.log`.
- `DiffEngine_Disabled=true dotnet test --project tests/Hexalith.FrontComposer.Testing.Tests/Hexalith.FrontComposer.Testing.Tests.csproj -c Release --no-build --results-directory /tmp/fc137-final/results --report-xunit-trx` — passed 185/185, 0 skipped; `/tmp/fc137-final/testing-suite.log`. The final TRX contains a named Passed result for every frozen matrix covering case, including both immediate-cancellation reasons, terminal cancellation, origin fallback, exact timing and sequence drift.
- The final focused class passed 139/139; `/tmp/fc137-review2-patch-tests.xml` records its named outcomes. The all-row and independently expected copy tables, measured consumers and seeded regressions are included in the 185-case final suite.
- `DiffEngine_Disabled=true dotnet tests/Hexalith.FrontComposer.Shell.Tests/bin/Release/net10.0/Hexalith.FrontComposer.Shell.Tests.dll -method '*AnalyzerPolicy_IdentifierInventory_MatchesSeal'` — passed 1/1; `/tmp/fc137-final/identifier.log`. Only the permitted identifier count/hash differ from the baseline ledger.
- `NuGetAudit=false MinVerVersionOverride=1.0.0 pwsh -NoProfile -File eng/validate-docs.ps1` — passed after the documented producer-hash reconciliation; `/tmp/fc137-final/docs.log`. All eight executable snippets passed and the generated `artifacts/docs/validation-manifest.json` has no blocking failures. The earlier fingerprint failures above are historical and resolved.
- The original frozen block, forbidden GOV-1 seal, Shell runtime/constants, dependencies and submodule state were checked against the baseline and preserved. The IDE document retains `revalidation-pending`; OI-16, G-4, FLUENT-APP-1 and Product approval remain independently owned.
- `node node_modules/@commitlint/cli/cli.js --edit /tmp/fc137-final/commit-message.txt --verbose` — passed with the lockfile-pinned and installed 21.2.2 CLI, 0 problems / 0 warnings; successful validation evidence `/tmp/fc137-final/commitlint.log`.
- `git diff --check` — passed. Local commit is the Build workflow's completion step; remote publication is not part of this run.
