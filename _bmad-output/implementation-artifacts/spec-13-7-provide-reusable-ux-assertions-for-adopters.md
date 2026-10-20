---
title: '13.7 [I · UX-F] Provide Reusable UX Assertions for Adopters'
type: 'feature'
created: '2026-10-18'
status: 'in-progress'
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
- [x] `src/Hexalith.FrontComposer.Testing/TestTimeProvider.cs`, `src/Hexalith.FrontComposer.Testing/TestTimeProviderTimer.cs` — retain deterministic due-order/Change/disposal/UTC behavior, invoke callbacks outside the clock lock, capture creation ExecutionContext; cover concurrent change/dispose and ambient context.
- [x] `src/Hexalith.FrontComposer.Testing/UxAnnouncementAssertions.cs` — retain exact canonical sequence/coalescing/channel checks; reject conflicting lifecycle terminal outcomes (including Confirmed/IdempotentConfirmed) for one operation; retain all unfiltered observations. Permit legitimate unnamed polite status semantics while asserting canonical message content separately.
- [x] `src/Hexalith.FrontComposer.Testing/UxAnnouncementCatalog.cs`, `tests/Hexalith.FrontComposer.Testing.Tests/TestingFailureModeTests.cs` — independently pin each AM row and supported variant to the intended localized resource/copy; seeded wrong-resource mapping must fail.
- [x] `src/Hexalith.FrontComposer.Testing/UxSurfaceSnapshot.cs`, `src/Hexalith.FrontComposer.Testing/UxStateAssertions.cs` — add caller-measured state-specific facts for SS rows (including disabled/selected tab and panel ownership, failure/no-success truth, non-live/focus semantics where required); add independent table-driven SS-01..49 acceptance/terminality/lifecycle and mismatch checks. Reject absent blocked-submit handles.
- [x] `src/Hexalith.FrontComposer.Testing/UxFieldSnapshot.cs`, `src/Hexalith.FrontComposer.Testing/UxValidationAssertions.cs`, supporting single-type files — supply measured summary text/count, declared group membership/name/description/order, measured group relationships, and prior stable control/error identities. Check canonical AM-18/19 copy separately from accessible label, invalid-only links in complete declared order, stable rerender targets, preserved useful input, visible label within a richer accessible name, and every ASCII whitespace IDREF separator.
- [x] `src/Hexalith.FrontComposer.Testing/AccessibilityMeasurements.cs`, `src/Hexalith.FrontComposer.Testing/AccessibilityEvidence.cs`, recorder/formatter — preserve the bounded typed schema and add safe ordinal surface/state references plus typed theme. Retain observed loss/clipping/overlap/truncation, forced-color cue preservation, animation suppression, silent expiry and keyboard/axe outcomes as applicable; no arbitrary text or sensitive identifiers.
- [x] `tests/Hexalith.FrontComposer.Testing.Tests/TestingFailureModeTests.cs` — preserve realistic rejection/denial/offline/admission/exhaustion consumers and all frozen matrix cases; add authoritative safe mapped-rejection consumption retaining Rejected identity with no duplicate AM-14 speech. Parse and verify all supplied AE values, including failed findings and measured geometry. Use existing/new tests with meaningful seeded drift.
- [x] `src/Hexalith.FrontComposer.Testing/PublicAPI.Shipped.txt`, allowed ledger count/hash, README/how-to — intentionally update the public measurement API, test-identifier seal and adopter examples. Preserve unrelated seals/dependencies/Shell runtime.

**Acceptance Criteria:**
- Given the epics.md Story 13.7 criteria, when the Testing suite runs, then every helper passes against realistic failure states and fails on seeded drift.
- Given evidence output, when inspected, then it contains no token, JWT, tenant payload, stack trace or PII.

### Review Findings

Review round 4 (2026-10-20) of the round-3 fix commit `fc429d0b`; per-finding verdicts are under Review Triage Log → Review round 4.

- [ ] [Review][Patch] AE-02 requires an effective CSS viewport of at most 320 px at ≥400% zoom; document `ViewportWidth` as the effective layout width after zoom (D1 resolved 2026-10-20: effective width, option 1) [src/Hexalith.FrontComposer.Testing/TestAccessibilityEvidenceRecorder.cs:61]
- [ ] [Review][Patch] Reject a Passed AE-06 packet below the 3:1 floor shared by every AE-06 category [src/Hexalith.FrontComposer.Testing/TestAccessibilityEvidenceRecorder.cs:75]
- [ ] [Review][Patch] Seed AE-03 paragraph/letter-spacing, reverse forced-colour/theme and non-positive equivalent-ordinal recorder regressions [tests/Hexalith.FrontComposer.Testing.Tests/TestingFailureModeTests.cs:1199]
- [ ] [Review][Patch] Pin a non-intersecting negative-size occluder (fails) and a zero-width occluder over the target (passes) [tests/Hexalith.FrontComposer.Testing.Tests/TestingFailureModeTests.cs:669]
- [ ] [Review][Patch] Pin zero-size target bounds in `AssertTargetSize` ("Bounds") and as Passed AE-04 evidence [tests/Hexalith.FrontComposer.Testing.Tests/TestingFailureModeTests.cs:526]
- [ ] [Review][Patch] Pin a single null required SS fact naming `Facts.X` [tests/Hexalith.FrontComposer.Testing.Tests/TestingFailureModeTests.cs:1226]
- [ ] [Review][Patch] Require a positive AE-01 viewport width [src/Hexalith.FrontComposer.Testing/TestAccessibilityEvidenceRecorder.cs:60]
- [ ] [Review][Patch] Name the missing, wrong-setup and contradicted facts in recorder diagnostics without values [src/Hexalith.FrontComposer.Testing/TestAccessibilityEvidenceRecorder.cs:74]
- [ ] [Review][Patch] Run per-field validation checks whenever structural preconditions hold and report every failing field [src/Hexalith.FrontComposer.Testing/UxValidationAssertions.cs:44]
- [ ] [Review][Patch] Correct misleading diagnostic labels: focused `aria-atomic` reported as `AriaLive`, terminal-branch "or wrong channel" prefix, `UnneededException` under "incomplete" [src/Hexalith.FrontComposer.Testing/UxAnnouncementAssertions.cs:76]
- [ ] [Review][Patch] Scope the README diagnostic-naming sentence to the helpers that name checks; note `AssertUnobscured` prints redacted geometry [src/Hexalith.FrontComposer.Testing/README.md:33]
- [ ] [Review][Patch] Sync README rows and the how-to with the round-3 behavior changes and P9 guidance [src/Hexalith.FrontComposer.Testing/README.md:26]
- [ ] [Review][Patch] Remove the unneeded SS-06 diagnostic exemption from the canonical-rows theory [tests/Hexalith.FrontComposer.Testing.Tests/TestingFailureModeTests.cs:1233]

Rejected:

- R4-B3 (low): spacing inputs are documented applied multipliers; integer font sizes reproduce exactly, and a float tolerance is a new rule.
- R4-B5 (false): each row enforces its own setup; extra active conditions do not make a packet another row's evidence, and both forced-colour disagreement directions are rejected.
- R4-B6 (false): a packet is one row's serialized evidence; Passed with a retained failed outcome is the self-contradiction P1 rejects.
- R4-B8 (low): `Bounds`/`KeyboardPath` are non-nullable contract members (R3-E19); null fails loudly.
- R4-B9 (low): duplicate target ordinals require a broken caller inventory; uniqueness is a new guard.
- R4-B11 (low): a dangling describedby IDREF is ignored by name computation; a retained error element is measured as `ErrorId` and fails P6.
- R4-B16 (false): the XML doc states explicit truncation; serialization failure of the sealed schema is not shown reachable.
- R4-B18 (false): `UxAnnouncement` has exactly the five compared members.
- R4-B19c (false): every polite channel requires `role="status"`, so implicit-live scope cannot change an outcome.
- R4-B20 (false): `ownerStory` has no lineage rule; history retains the 10-1 lineage.
- R4-E1 (low): same as R4-B8.
- R4-E2 (low): null or non-positive geometry in a failed packet is a broken caller measurement (R3-E5).
- R4-E6 (low): same as R4-B3.
- R4-E7 (false): same as R4-B6.
- R4-E8 (low): carried R3-E5 (negative counts, Critical > Finding).
- R4-E9 (low): same as R4-B9.
- R4-E10 (low): empty/uppercase explicit `aria-live` on a status node is unlikely markup; token normalization is new parsing.
- R4-E12 (maybe-false, low if true): needs the blocked attempt's `Lifecycle` filled from the original command; the realistic gate consumer measures none.
- R4-E13 (low): same as R4-B11.
- R4-E15 (low): same as R4-B11; P6's stale `ErrorId` is implemented and pinned.
- R4-V6 (low): same as R4-B8.

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

### Review round 3

Scope: the Story 13.7 File List paths from `b9e1490cfe1c51406d3c8d4dc213971d06911a24` to `8713e14b04af43423e80368b4b48ff932db6394e`, excluding the story/tracking artifacts. This includes the post-review `ee9762e6` Testing edits. The 15 interleaved CI/release/e2e/Shell commits in that range have their own specs and are not reviewed here. The Testing API shipped in `v4.7.0`, so changing a public signature is now a breaking change. No row below is a carried repeat of a round 1 or 2 row.

| Finding | Verdict | Route | Verified evidence |
| --- | --- | --- | --- |
| R3-B1 | low | reject | `ISurfaceAnnouncementCoordinator.Subscribe` publishes text only; identity was deliberately kept off the published interface (`cdc18282`). Observer-assigned identities still let copy, order, count and channel drift fail. A new identity recorder is a new public API, not a direct correction. |
| R3-B2 | low | patch (P3) | `SafeLabel` and the generic `AssertState`/`AssertLinkedSummary`/`AssertChannel`/`AssertTargetSize` messages do not name the failed check, so an adopter must read source to find which of up to 30 conditions failed. The fix names the failed checks; values and identifiers stay excluded. |
| R3-B3 | low | reject | The `speechOwnerCount`/`inheritedResultCountSpeechCount` defaults are documented, caller-supplied opt-in measurements for the Shell-owned palette (AM-28). Making them required changes a published `v4.7.0` signature. |
| R3-B4 | low | reject | `AssertBlockedSubmit` derives the expected AM-20 identity from the observed event, but that identity is observer-assigned because speech carries text only. Checking it against an expected operation needs a new parameter on a published method. |
| R3-B5 | low | reject | Copy resolves through `CurrentUICulture`, which is the bUnit renderer culture; callers can set it. A culture parameter would change published signatures. |
| R3-B6 | low | reject | `AssertBoundary` documents its operation-start precondition. Misuse fails loudly for an on-time implementation, and an explicit start timestamp is a new parameter. |
| R3-B7 | medium | patch (P15) | The nested/concurrent `Advance` guard is real and untested (see V7). The throw and zero-due re-arm sub-claims are verified and rejected under E1/E2. |
| R3-B8 | medium | patch (P1, P2) | `ValidateMeasurements` rejects `Passed` only for five outcomes. Overlap, truncation, page overflow, operations/order/tab, forced-colour, motion, expiry, hover, WCAG 2.2 tags, and failing AE-04/AE-05 geometry are all accepted as passing (P1). Row setup is unchecked (P2). The impossible-count sub-claim is rejected with E5. |
| R3-B9 | medium | patch (P2) | `UxTheme.ForcedColors` and `AccessibilityMeasurements.ForcedColors` can disagree. AE-07 is accepted under `UxTheme.Light`, and the all-row fixture records that contradiction as valid. |
| R3-B10 | low | patch (P9) | The `AccessibilityEvidence.RedactedMeasurements` XML doc says property names are removed; the JSON keeps the fixed schema names. Truncation itself is the documented explicit bound (see E7). |
| R3-B11 | low | reject | A public recorder constructor taking host options is documented and works. Making it internal or host-wired would break the `v4.7.0` API, and the example returns the record it creates. |
| R3-B12 | low | patch (P9) | The README redaction list omits the nine fragments this story added. Substring over-redaction (E9) and neutral-key/value leakage (E20) are rejected separately. |
| R3-B13 | low | reject | Substring label-in-name accepts `Name` inside `Username`. That mismatch is unlikely in declared labels, and word-boundary matching adds parsing beyond a direct correction. |
| R3-B14 | low | patch (P9) | The consumer reads ARIA from the Fluent host. Fluent v5 host ARIA does not reach the shadow control, and the README does not tell adopters to measure the focusable control's computed accessibility tree. |
| R3-B15 | low | reject | The listed consumers fix semantics but exercise the helpers against real lifecycle/policy state. The helpers do not over-constrain those rows, and Shell semantics are owned by the Shell and e2e suites. |
| R3-B16 | low | patch (P3) | The required SS facts per row are discoverable only from source, which shares B2's root cause. The identical-facts sub-claim is false: SS rows are surface-scoped, and Loading/Empty/Data truth is the same on each surface. |
| R3-B17 | low | defer (D1, D2) | Both issues predate this story. `PackageBoundaryTests` pins catalog versions that break on every Builds bump (D1). The 13.1 scope stamp on directly dispatched projection actions is undocumented for adopters (D2). The fingerprint re-seal is documented above (see E21). |
| R3-B18 | low | reject | The API-file header comment predates the story, the spec requires the Shipped baseline, and semantic-release generates the CHANGELOG. |
| R3-B19 | false | reject | Inline, user-agent and essential are categorical WCAG 2.5.8 classifications. AE-04 requires exactly the recorded exception, rationale and keyboard path, so no measurable fact is omitted. |
| R3-B20 | low | reject | Missing format arguments throw `FormatException` loudly, and extra arguments cannot change the resolved copy. |
| R3-E1 | low | reject | An exception from a timer callback propagates out of `Advance` (a real timer would crash the process). The intermediate tick matters only if a test swallows the failure. |
| R3-E2 | low | reject | A zero-due self re-arm spins in real time as well, and no Shell timer re-arms at zero. An iteration cap is a new guard. |
| R3-E3 | medium | patch (P1) | Same root cause as B8: `Passed` is accepted alongside failed measured outcomes. |
| R3-E4 | medium | patch (P2) | AE-01/02/03/07/08 accept packets measured under another setup (for example, AE-07 with `ForcedColors = false`). |
| R3-E5 | low | reject | Negative counts and impossible ratios require a broken caller measurement. Range guards add complexity for a scenario unlikely in practice. |
| R3-E6 | medium | patch (P1) | Passed AE-04 targets that fail `AssertTargetSize`, Passed AE-05 geometry that fails `AssertUnobscured`, and non-positive target/equivalent ordinals are accepted. |
| R3-E7 | false | reject | Truncation is explicitly marked and documented: the README and how-to say to raise `MaxDiagnosticPayloadCharacters` for a full packet, and R2-B8 preserved that bound. |
| R3-E8 | low | reject | Configured identifiers default to `test-tenant`/`test-user`. Numeric or empty overrides are unlikely, and a failure yields an explicit marker. |
| R3-E9 | low | reject | Substring redaction can over-redact keys such as `PersonCount`. Over-redaction is the safe direction, and segment matching is more than a direct correction. |
| R3-E10 | low | patch (P7) | `AssertChannel` requires explicit `aria-live="polite"` (and atomic for AM-28), but `role="status"` is implicitly polite and atomic. Valid markup therefore fails, unlike the R2-B4 state semantics. |
| R3-E11 | low | reject | Same claim as B20. |
| R3-E12 | low | reject | `UxFocusTarget.Handle` is documented as a stable direct-element identity, and reference semantics are a KEEP decision. A boxed struct fails loudly, and a comparer would be a new parameter. |
| R3-E13 | low | patch (P8) | A hidden app-owned occluder measured as a zero-size rectangle fails as invalid geometry even though it cannot obscure anything. |
| R3-E14 | medium | patch (P4) | SS-25/37/38 reject dispatch/queue but accept a non-idle `Lifecycle`, so a did-not-run attempt that entered the lifecycle passes. |
| R3-E15 | medium | patch (P5) | SS-19/21 success rows accept `FailureVisible = true`, so a confirmed surface that still shows stale failure text passes. |
| R3-E16 | low | reject | Same claim as B4. |
| R3-E17 | false | reject | Repeated links are legitimate: `FcValidationSummary` emits one entry per message (R2-B1). `ErrorCount >= links` and the link-order check bound them. |
| R3-E18 | low | patch (P6) | A valid field may keep a non-empty `ErrorId` (a stale error target), although the contract says it is empty without an error. |
| R3-E19 | low | reject | `AccessibleName` and `DescribedBy` are non-nullable contract members. Passing null fails loudly. |
| R3-E20 | low | reject | Dispatch redaction is a pre-existing key-name design, and adopter test payloads are synthetic. Value scanning is a heuristic beyond a direct fix; the new AE evidence is text-free. |
| R3-E21 | false | reject | The fingerprint and package-pin corrections are documented above in "Final validation fixture reconciliation" and Implementation Notes. |
| R3-V1 | medium | patch (P10) | Pre-verified: mismatched rationale, empty `Neighbors`, the neighbour-centre distance, a sized target with an exception, an empty keyboard path, and a self-referencing equivalent are all unpinned. |
| R3-V2 | medium | patch (P11) | Pre-verified: removing the FocusedHeading copy clause leaves every test green. |
| R3-V3 | medium | patch (P12) | Pre-verified: deleting the unsupported-variant guard leaves every test green. |
| R3-V4 | medium | patch (P1) | Pre-verified: individual contradiction conditions and `StateNumber = 0` are unpinned. |
| R3-V5 | medium | patch (P13) | Pre-verified: removing the `address` or `person` fragment leaves every test green. |
| R3-V6 | medium | patch (P14) | Pre-verified: terminal-once is pinned only for AM-13/14. |
| R3-V7 | medium | patch (P15) | Pre-verified: the nested/concurrent `Advance` guard is untested. |
| R3-V8 | medium | patch (P1) | Same root cause as B8/E3: the contradiction rule covers only five outcomes. |

Survivor groups:

- P1 (B8/E3/E6/V4/V8): reject a Passed packet with any failed measured outcome or failing AE-04/AE-05 geometry, and reject non-positive target ordinals.
- P2 (B8/B9/E4): enforce canonical row setup and theme/forced-colour agreement.
- P3 (B2/B16): name the failed checks and SS facts in diagnostics.
- P4 (E14): no lifecycle entry for did-not-run rows.
- P5 (E15): success rows show no failure.
- P6 (E18): no stale error target on a valid field.
- P7 (E10): implicit `status` live/atomic semantics.
- P8 (E13): ignore zero-size occluders.
- P9 (B10/B12/B14): adopter documentation accuracy.
- P10–P15 (V1/V2/V3/V5/V6/V7+B7): regression tests.

Every patch keeps the published signatures and adds no public API; tests are added inside existing methods, so the identifier seal is unchanged. D1 and D2 go to the deferred-work ledger. Nothing routes to `intent_gap` or `bad_spec`, so no loopback is needed.

### Review round 4

Scope: the round-3 fix commit `fc429d0b` (`8713e14b..fc429d0b`) over the Story 13.7 File List paths, excluding story/tracking artifacts (10 files, +328/−130). Rounds 1–3 cover the earlier delivery. Layers: Blind Hunter (B), Edge Case Hunter (E), Verification Gap (V), Acceptance Auditor (A); none failed. Rows marked carried repeat a round-3 disposition.

| Finding | Verdict | Route | Verified evidence |
| --- | --- | --- | --- |
| R4-B1 | medium | patch (P1) | The P1 contradiction rule never reads `ContrastRatio`, so `Record(AE06, Light, { Passed = true, ContrastRatio = 1.2 })` is retained although every UX-AE-1 AE-06 category requires at least 3:1. R2-B12 rejected a category API; a 3:1 floor needs no category. The ratio-range sub-claim is carried R3-E5. |
| R4-B2 | low | patch (P6) | `AE01 => m.ViewportWidth <= 320` accepts 0 or a negative width as the canonical 320 CSS-pixel setup. The negative overflow/count and Critical > Finding sub-claims are carried R3-E5. |
| R4-B3 | low | reject | Spacing values are documented applied multipliers; integer font sizes 10–40 px reproduce exactly. Only a derived ratio at an unusual size (26.4/17.6) falls 2e-16 short, and a tolerance is a new rule. |
| R4-B4 | medium | decision (D1) | `AE02 => m.ViewportWidth.HasValue && m.ZoomPercent >= 400` accepts 400% on any viewport, contrary to UX-AE-1 "400% at a 1280 CSS-pixel reference viewport". The bound depends on whether `ViewportWidth` is the effective CSS width (fixture and in-repo e2e: 320) or the 1280 reference. |
| R4-B5 | false | reject | Each row enforces its own canonical setup; reduced motion in an AE-01 packet or an unrecorded `ForcedColors` under a ForcedColors theme does not make it another row's evidence. Forced colours alone is tied to theme provenance, and both disagreement directions are rejected. |
| R4-B6 | false | reject | A packet is one row's evidence and is serialized whole; a Passed packet retaining `KeyboardPassed = false` is the self-contradiction P1 specified to reject. |
| R4-B7 | low | patch (P7) | `ValidateMeasurements` throws one merged missing/setup message and one generic contradiction message, so an adopter must read source to learn which AE fact failed (the R3-B2/B16 root cause, outside P3's helpers). |
| R4-B8 | low | reject | `AccessibilityTargetEvidence.Bounds` and `KeyboardPath` are non-nullable contract members (R3-E19 precedent); null fails loudly with `NullReferenceException`. |
| R4-B9 | low | reject | Duplicate `TargetNumber`s require a broken caller inventory; a uniqueness guard is a new rule. |
| R4-B10 | low | patch (P8) | `for (int i = 0; failed.Count == 0 && ...)` skips every per-field check after any aggregate failure (Summary, Links, ErrorTargets) and stops at the first failing field. Only DeclaredFields, DeclaredGroups, FieldIdentity and PriorTargets protect the index and `SingleOrDefault` lookups. |
| R4-B11 | low | reject | A valid field whose `DescribedBy` keeps a removed error IDREF is a dangling reference ignored by name computation; a retained error element is measured as `ErrorId` and fails P6. The `ErrorId` XML doc already says empty for a valid field. |
| R4-B12 | low | patch (P10) | README line 33 says failure messages name failed checks without printing observed values; `AssertUnobscured` appends formatted measured geometry, and `AssertRestored`, `AssertBlockedSubmit`, `AssertInputPreserved`, timing and recorder messages name no check. |
| R4-B13 | low | patch (P11) | `IsUnobscured` ignores zero-size occluders (P8), but neither the README AE-05 row nor the how-to says an occluder is measured as its painted bounds including overflowing descendants; a zero-size wrapper with a painting child then passes. Reverting to fail-closed would contradict P8. |
| R4-B14 | low | patch (P11) | Tightened checks (SS-19/21 explicit `FailureVisible = false`, SS-25/37/38 no lifecycle entry, valid field empty `ErrorId`) and loosened ones (implicit `role="status"`, zero-size occluders) are absent from the README rows. The commit-type sub-claim is rejected: published history is not rewritten. |
| R4-B15 | low | patch (P11) | The 13.7-owned how-to line 105 still lists only token/secret/password redaction and lacks P9's key-name-only caveat and Fluent v5 shadow-control measurement guidance. |
| R4-B16 | false | reject | The XML doc states explicit truncation beyond the payload limit; omitted absent facts keep fixed schema names, and serialization failure of the sealed schema is not shown reachable. |
| R4-B17 | low | patch (P9) | The terminal branch of `AssertDistinctChannels` keeps the "or wrong channel" prefix for a second terminal outcome, and `AssertTargetSize` reports `UnneededException` under "…is incomplete". |
| R4-B18 | false | reject | `UxAnnouncement` has exactly the five compared members; no current state yields an empty differing list. |
| R4-B19a | low | patch (P12) | The theory's only SS-06 case is the visible denied state with `PolicyHidden` unset, so every flipped fact names `Facts.X`; the `row != "SS-06"` exemption only weakens the pin. |
| R4-B19b | medium | patch (P5) | The theory flips present booleans and nulls the whole `Facts`, but never nulls one required fact. Accepting a null (unmeasured) fact in `FactFailures` leaves the suite green — the R1 known-bad "missing state semantics certified". |
| R4-B19c | false | reject | Every polite channel requires `role="status"`, so implicit-live scope cannot change an `AssertChannel` outcome. |
| R4-B20 | false | reject | `ownerStory` is required front matter with no lineage rule (`eng/validate-docs.ps1:486`); the drift sweep reassigned it deliberately and history retains the 10-1 lineage. |
| R4-E1 | low | reject | Same claim as R4-B8. |
| R4-E2 | low | reject | Null or non-positive geometry in a `Passed = false` packet is a broken caller measurement (R3-E5 precedent). |
| R4-E3 | medium | patch (P1) | Same root cause as R4-B1. |
| R4-E4 | low | patch (P6) | Same root cause as R4-B2. |
| R4-E5 | medium | decision (D1) | Same claim as R4-B4. |
| R4-E6 | low | reject | Same claim as R4-B3. |
| R4-E7 | false | reject | Same claim as R4-B6. |
| R4-E8 | low | reject | Carried R3-E5: negative counts and `CriticalFindingCount > FindingCount` require a broken caller measurement. |
| R4-E9 | low | reject | Same claim as R4-B9. |
| R4-E10 | low | reject | Empty or uppercase explicit `aria-live` on a status node is unlikely markup; ARIA token normalization is new parsing. |
| R4-E11 | low | patch (P11) | Same root cause as R4-B13. |
| R4-E12 | maybe-false | reject | Requires an adopter to fill the blocked attempt's `Lifecycle` from the original command; `Dispatched`/`Queued` describe "this attempted command", and the realistic admission-gate consumer measures no pending entry for the denied attempt. Low if true. |
| R4-E13 | low | reject | Same claim as R4-B11. |
| R4-E14 | medium | patch (P1) | Same root cause as R4-B1. |
| R4-E15 | low | reject | Same claim as R4-B11; P6's stale `ErrorId` is implemented and pinned. |
| R4-E16 | low | patch (P10) | Same root cause as R4-B12. |
| R4-E17 | medium | decision (D1) / patch (P6) | The AE-02 half joins R4-B4; the AE-01 half joins R4-B2. |
| R4-V1 | medium | patch (P2) | Pre-verified: the setup loop varies only `LineHeight`/`WordSpacing` for AE-03; weakening `ParagraphSpacing >= 2` or `LetterSpacing >= .12` to `HasValue` stays green. |
| R4-V2 | medium | patch (P2) | Pre-verified: only "forced colours measured under Light" is rejected in tests; a `UxTheme.ForcedColors` context with `ForcedColors = false` on a non-AE-07 row is never exercised. |
| R4-V3 | medium | patch (P2) | Pre-verified: no recorder case supplies a non-positive `EquivalentControlNumber`; dropping that clause stays green. |
| R4-V4 | medium | patch (P3) | Pre-verified broken verification: the "negative sizes remain invalid" occluder `(30,105,-4,10)` intersects the target, so deleting `Width >= 0 && Height >= 0` stays green; the zero-width disjunct is never isolated. |
| R4-V5 | medium | patch (P4) | Pre-verified: no test uses zero/negative target bounds; deleting the `"Bounds"` failure accepts a 0×0 Inline target in `AssertTargetSize` and as Passed AE-04 evidence. |
| R4-V6 | low | reject | Same claim as R4-B8. |
| R4-V7 | low | patch (P6) | Same root cause as R4-B2. |
| R4-V8 | low | patch (P10) | Same root cause as R4-B12. |
| R4-A1 | medium | decision (D1) | Same claim as R4-B4; the fixture's AE-02 `ViewportWidth = 320` fits only the effective-width reading. |
| R4-A2 | medium | patch (P2) | Independently confirms R4-V1/V2/V3. |
| R4-A3 | low | patch (P8, P9) | Confirms R4-B10; separately, a focused summary/heading carrying `aria-atomic` fails under the `AriaLive` label because the focused branch folds both attributes into one check. |
| R4-A4 | low | patch (P10) | Same root cause as R4-B12. |
| R4-A5 | low | patch (P11) | The round-3 Documentation Drift Sweep lists implicit status, success/no-failure, did-not-run lifecycle, valid-field target and zero-size occluder changes, but the README/how-to document only recorder rejections and named diagnostics, and its no-update decisions do not explain the rest. |

Survivor groups:

- D1 (B4/E5/E17/A1): AE-02 viewport bound; needs the `ViewportWidth` reading. Resolved 2026-10-20 by the user: `ViewportWidth` is the effective CSS layout width after zoom, matching its "CSS pixels" doc, the fixture, AE-01 and the in-repo e2e approximation (1280 ÷ 4). AE-02 therefore requires `ViewportWidth is > 0 and <= 320` with `ZoomPercent >= 400`; a reference-width reading would give one property two meanings and still accept a wide window measured by `innerWidth`. Routes to a patch.
- P1 (B1/E3/E14): reject a Passed AE-06 packet below 3:1.
- P2 (V1/V2/V3/A2): seeded regressions for AE-03 paragraph/letter spacing, reverse forced-colour/theme disagreement and a non-positive equivalent ordinal.
- P3 (V4): non-intersecting negative-size occluder fails; zero-width positive-height occluder over the target passes.
- P4 (V5): zero-size Inline target fails `AssertTargetSize` and Passed AE-04.
- P5 (B19b): a single null required fact names `Facts.X`.
- P6 (B2/E4/E17/V7): AE-01 requires a positive viewport width.
- P7 (B7): recorder diagnostics name the missing, wrong-setup and contradicted facts without values.
- P8 (B10/A3): per-field validation checks run whenever structural preconditions hold and report every failing field.
- P9 (B17/A3): diagnostic labels and prefixes match the failing check.
- P10 (B12/E16/V8/A4): README diagnostic sentence scoped to the helpers that name checks.
- P11 (B13/B14/B15/E11/A5): README rows and how-to synced with the round-3 behavior changes and P9 guidance.
- P12 (B19a): remove the SS-06 exemption.

Every patch keeps the published signatures and adds no public API; tests go inside existing methods, so the identifier seal is unchanged. Nothing defers.

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


### Completion reconciliation — 2026-10-19

The implementation remains delivered. The prior done status is corrected to in-review to agree with the sprint row and the required mechanical completion gate. Historical passing evidence above remains historical; it does not override current artifact validation. The original frozen intent and baseline are preserved.

The initial strict command `python3 eng/validate-story-artifacts.py --story _bmad-output/implementation-artifacts/spec-13-7-provide-reusable-ux-assertions-for-adopters.md --candidate HEAD` exited 1 before completion. The missing File List and eight abbreviated task-path failures are corrected in this follow-up. The inventory below records the actual Story 13.7 implementation, tests, adopter documentation, governance fixture and completion records from the original delivery, separately from generated validation output retained under `/tmp/fc137-build-20261019`.

Historical attribution remains a blocker: the original delivery commit `7f57da411c3abf8e1d459d0611c49632c07393a7` has no matching story ID in its subject, while `ee9762e6c74914ccd33ec1d2caf322bdc21a10b4` names Story 13.7 and also changes unrelated CI/release files. Those unrelated paths are deliberately excluded from the inventory. The published history is not rewritten, no ownership exception is invented and no validator policy is weakened. This follow-up cannot close the story while that strict gate fails. OI-16, G-4, FLUENT-APP-1 and Product approval remain separately owned.


### Current-candidate verification — 2026-10-19

| Lane | Required command | Local result | Blocker timing | Fallback evidence | CI authority |
| --- | --- | --- | --- | --- | --- |
| Testing Debug build | `DiffEngine_Disabled=true dotnet build tests/Hexalith.FrontComposer.Testing.Tests/Hexalith.FrontComposer.Testing.Tests.csproj -c Debug -m:1 -p:NuGetAudit=false -p:MinVerVersionOverride=1.0.0` | Passed: 0 warnings, 0 errors | N/A | N/A | Not applicable |
| Testing Release build for package consumers | `DiffEngine_Disabled=true dotnet build tests/Hexalith.FrontComposer.Testing.Tests/Hexalith.FrontComposer.Testing.Tests.csproj -c Release -m:1 -p:NuGetAudit=false -p:MinVerVersionOverride=1.0.0` | Passed: 0 warnings, 0 errors | N/A | N/A | Not applicable |
| Testing complete project | `DiffEngine_Disabled=true NuGetAudit=false MinVerVersionOverride=1.0.0 dotnet test --project tests/Hexalith.FrontComposer.Testing.Tests/Hexalith.FrontComposer.Testing.Tests.csproj -c Release --no-build --results-directory /tmp/fc137-build-20261019/results --report-xunit-trx` | Passed: 185/185, 0 skipped; delta from prior 185-case evidence: 0 | N/A | Debug matrix also passed 139/139 | Not applicable |
| Story artifact completion gate | `python3 eng/validate-story-artifacts.py --story _bmad-output/implementation-artifacts/spec-13-7-provide-reusable-ux-assertions-for-adopters.md --candidate HEAD` | Failed: exit 1, unmapped original delivery and interleaved latest commit | During commit attribution; no test execution involved | Passing tests do not replace this gate | Not applicable |

The direct Debug matrix command was `DiffEngine_Disabled=true dotnet tests/Hexalith.FrontComposer.Testing.Tests/bin/Debug/net10.0/Hexalith.FrontComposer.Testing.Tests.dll -class '*TestingFailureModeTests' -result-xml /tmp/fc137-build-20261019/matrix-debug.xml`. It is additional local evidence; the complete native MTP project lane above also passed. Build logs, test output/TRX and strict validator reports are under `/tmp/fc137-build-20261019/`.

The corrected inventory and task references eliminated all missing-file and checked-task failures. Only the two historical attribution errors remain at candidate `ee9762e6c74914ccd33ec1d2caf322bdc21a10b4`. No tests were added, deleted, skipped or relaxed. This documentation-only reconciliation changes no contract behavior; the existing README, how-to and public API baseline remain applicable. The prior docs gate is historical evidence and was not rerun for these implementation-record edits.

The FrontComposer repository maintainer owns the historical-attribution repair. The tracked handoff in `deferred-work.md` requires an explicitly authorized, policy-compliant repair, followed by the original strict command over the preserved baseline and the final transition commit. This reconciliation does not select or authorize a published-history rewrite. Both original and follow-up strict validators must be rerun after this reconciliation commit; the final handoff records the resulting canonical candidate and outcomes.

### Review round 3 verification — 2026-10-19

This section records the patched working tree on top of candidate `8713e14b04af43423e80368b4b48ff932db6394e`. Logs are under `/tmp/claude-1000/-home-administrator-projects-hexalith-frontcomposer/c9ddc917-4a27-4b25-991d-ebfb9f9f2a2b/scratchpad/r3/`.

- `DiffEngine_Disabled=true dotnet build Hexalith.FrontComposer.slnx -c Release -m:1 -nodeReuse:false --disable-build-servers -p:NuGetAudit=false -p:MinVerVersionOverride=1.0.0` passed with 0 warnings and 0 errors (`solution-release.log`). The Testing test project Release build also passed with 0 warnings and 0 errors (`testing-release-build-final.log`).
- `DiffEngine_Disabled=true NuGetAudit=false MinVerVersionOverride=1.0.0 dotnet test --project tests/Hexalith.FrontComposer.Testing.Tests/Hexalith.FrontComposer.Testing.Tests.csproj -c Release --no-build --report-xunit-trx` passed 185/185, 0 skipped (`testing-suite-final.log`, `results-final/`). The count is unchanged because every round-3 regression case was added inside an existing test method.
- `dotnet tests/Hexalith.FrontComposer.Shell.Tests/bin/Release/net10.0/Hexalith.FrontComposer.Shell.Tests.dll -method '*AnalyzerPolicy_IdentifierInventory_MatchesSeal'` passed 1/1 (`identifier.log`). `analyzer-policy-exception-ledger-v1.json` is unchanged, and the GOV-1 drift seal is untouched.
- Seeded mutations: each one reverts a single round-3 behavior, rebuilds, and runs `-class '*TestingFailureModeTests'`. All 16 were caught:
  - test gaps: V1 empty neighbours, V2 heading copy, V3 variant guard, V5 address fragment, V6 AM-24 terminal, V7 advance guard;
  - P1 page overflow and AE-05 geometry;
  - P2 AE-07 forced colours and theme agreement;
  - P3 fact names, P4 did-not-run lifecycle, P5 success/no-failure, P6 stale error target, P7 implicit live, P8 zero-size occluder.

  The first V3 mutant was rejected by analyzer CA1062, so V3 was re-run with an analyzer-clean mutation and caught. Every source file was restored byte-for-byte, and HEAD stayed at `8713e14b` (`mutants/`, `mutants-v3/`, `mutate.py`).
- `NuGetAudit=false MinVerVersionOverride=1.0.0 pwsh -NoProfile -File eng/validate-docs.ps1` passed, including the compiled how-to snippets (`docs.log`, `artifacts/docs/validation-manifest.json`).
- `git diff --check` passed. No public signature, dependency, Shell runtime file, submodule pointer, or approval/evidence seal changed.
- The strict completion gate is still governed by the historical-attribution handoff above. Round 3 neither resolves nor bypasses it, so the story stays in review until the maintainer-owned repair and the rerun strict validator pass. Under reconciliation checklist item 10, the Build workflow's done transition is not applied: spec status stays `in-review`, and the sprint row already reads `review`.

### Documentation Drift Sweep

- Source change: the round-3 review fixes change adopter-facing helper behavior.
  - The AE recorder rejects wrong-row setup or theme, Passed packets with a failed outcome or failing AE-04/AE-05 geometry, and non-positive target ordinals.
  - `AssertChannel` accepts implicit `role="status"` live/atomic semantics.
  - SS-19/21 require no visible failure, and SS-25/37/38 require no lifecycle entry.
  - A valid field must have no error target.
  - Zero-size occluders are ignored.
  - Failure messages name the failed checks.
- Search terms: `TestAccessibilityEvidenceRecorder`, `AccessibilityMeasurements`, `UxAnnouncementAssertions`, `UxStateAssertions`, `UxValidationAssertions`, `UxFocusAssertions`, `UxAccessibilityAssertions`, `AssertUnobscured`, `AssertTargetSize`, `AssertLinkedSummary`.
- Scope searched: `docs/reference/**`, `docs/how-to/**`, `docs/tutorials/**`, `docs/skills/**`, `_bmad-output/project-docs/**` and `_bmad-output/contracts/**`.
- Contract docs checked:
  - `_bmad-output/contracts/**`: no match, so no contract family governs these helpers.
  - `src/Hexalith.FrontComposer.Testing/README.md`: package README with current contract claims.
  - `src/Hexalith.FrontComposer.Testing/PublicAPI.Shipped.txt`.
- Documents updated:
  - `src/Hexalith.FrontComposer.Testing/README.md`: redaction list, Fluent shadow-control measurement, recorder rejections, named diagnostics.
  - `docs/how-to/test-generated-components.md`: recorder rejections; front matter `ownerStory` set to `13-7-i-ux-f-provide-reusable-ux-assertions-for-adopters` and `reviewed` to `2026-10-19`.
  - `AccessibilityEvidence` XML doc.
- No-update decisions:
  - `PublicAPI.Shipped.txt`: no signature changed.
  - The how-to's pre-existing dispatch-redaction sentence predates this story.
  - No other public page references these helpers.
- Validation: `eng/validate-docs.ps1` passed both with full DocFX/snippets and with `-SkipDocFx -SkipSnippetBuild` after the front-matter update (`docs.log`, `docs-quick.log`).
- Residual risk: adopter guidance for scope-stamping directly dispatched projection actions is deferred (R3-B17/D2).

## File List

The paths below are the Story 13.7 delivery and its completion records. Test files include the rendered validation consumer; adopter documentation and intentional API/governance fixtures are listed explicitly. This run changes the two Story 13.7 spec files and appends one maintainer-owned blocker to the deferred-work ledger. No generated evidence or submodule pointer is claimed as story output.

- `_bmad-output/contracts/analyzer-policy-exception-ledger-v1.json`
- `_bmad-output/implementation-artifacts/spec-13-7-provide-reusable-ux-assertions-for-adopters.md`
- `_bmad-output/implementation-artifacts/spec-13-7-reconcile-completion-evidence-and-tracking.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `docs/how-to/test-generated-components.md`
- `docs/validation/producer-fingerprints.json`
- `src/Hexalith.FrontComposer.Testing/AccessibilityEvidence.cs`
- `src/Hexalith.FrontComposer.Testing/AccessibilityEvidenceContext.cs`
- `src/Hexalith.FrontComposer.Testing/AccessibilityEvidenceRow.cs`
- `src/Hexalith.FrontComposer.Testing/AccessibilityMeasurements.cs`
- `src/Hexalith.FrontComposer.Testing/AccessibilityTargetEvidence.cs`
- `src/Hexalith.FrontComposer.Testing/Evidence.cs`
- `src/Hexalith.FrontComposer.Testing/PublicAPI.Shipped.txt`
- `src/Hexalith.FrontComposer.Testing/README.md`
- `src/Hexalith.FrontComposer.Testing/TestAccessibilityEvidenceRecorder.cs`
- `src/Hexalith.FrontComposer.Testing/TestTimeProvider.cs`
- `src/Hexalith.FrontComposer.Testing/TestTimeProviderTimer.cs`
- `src/Hexalith.FrontComposer.Testing/UxAccessibilityAssertions.cs`
- `src/Hexalith.FrontComposer.Testing/UxAccessibilitySnapshot.cs`
- `src/Hexalith.FrontComposer.Testing/UxAnnouncement.cs`
- `src/Hexalith.FrontComposer.Testing/UxAnnouncementAssertions.cs`
- `src/Hexalith.FrontComposer.Testing/UxAnnouncementCatalog.cs`
- `src/Hexalith.FrontComposer.Testing/UxAnnouncementChannel.cs`
- `src/Hexalith.FrontComposer.Testing/UxFieldDeclaration.cs`
- `src/Hexalith.FrontComposer.Testing/UxFieldGroupDeclaration.cs`
- `src/Hexalith.FrontComposer.Testing/UxFieldIdentity.cs`
- `src/Hexalith.FrontComposer.Testing/UxFieldSnapshot.cs`
- `src/Hexalith.FrontComposer.Testing/UxFocusAssertions.cs`
- `src/Hexalith.FrontComposer.Testing/UxFocusTarget.cs`
- `src/Hexalith.FrontComposer.Testing/UxKeyboardKey.cs`
- `src/Hexalith.FrontComposer.Testing/UxRect.cs`
- `src/Hexalith.FrontComposer.Testing/UxStateAssertions.cs`
- `src/Hexalith.FrontComposer.Testing/UxSurfaceFacts.cs`
- `src/Hexalith.FrontComposer.Testing/UxSurfaceSnapshot.cs`
- `src/Hexalith.FrontComposer.Testing/UxTargetSizeExceptionKind.cs`
- `src/Hexalith.FrontComposer.Testing/UxTargetSizeRationale.cs`
- `src/Hexalith.FrontComposer.Testing/UxTheme.cs`
- `src/Hexalith.FrontComposer.Testing/UxTiming.cs`
- `src/Hexalith.FrontComposer.Testing/UxTimingAssertions.cs`
- `src/Hexalith.FrontComposer.Testing/UxValidationAssertions.cs`
- `src/Hexalith.FrontComposer.Testing/UxValidationSummarySnapshot.cs`
- `tests/Hexalith.FrontComposer.Testing.Tests/FrontComposerTestHostTests.cs`
- `tests/Hexalith.FrontComposer.Testing.Tests/PackageBoundaryTests.cs`
- `tests/Hexalith.FrontComposer.Testing.Tests/TestingFailureModeTests.cs`
- `tests/Hexalith.FrontComposer.Testing.Tests/ValidationConsumer.razor`
- `_bmad-output/implementation-artifacts/deferred-work.md`
