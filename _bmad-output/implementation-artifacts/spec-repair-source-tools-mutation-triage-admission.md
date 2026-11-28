---
title: 'Repair SourceTools mutation-triage admission'
type: 'bugfix'
created: '2026-11-28'
status: 'in-review'
baseline_commit: '3297cbdc559f28a3250c97b963a820e06cd1700a'
route: 'dispatch'
review_loop_iteration: 0
context:
  - '{project-root}/docs/how-to/mutation-and-property-quality-gates.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

Restore Quality Gate 2b's finite mutation-triage admission for the current SourceTools source. Preserve the exact checks for current source and mutation identities, recorded dispositions, both native mutation reports, and test evidence. Keep this repair separate from the EventStore migration decision.

## Boundaries & Constraints

**Always:** Bind results to an exact source checkout and the authenticated pinned Stryker build. Preserve the historical observations and literal-control receipts with their original provenance. Require current replay evidence where the reviewed literal or its behavior changed. Keep thresholds, target roots, and the independent nightly gates in force.

**Never:** Refresh a hash to accept an unverified mutation, treat a historical numeric ID as proof of current identity, or derive a historical outcome from a current result. Keep source behavior, dependency pins, and release workflows outside this repair.

| Scenario | Input | Expected behavior |
| --- | --- | --- |
| Verified relocation | A current native mutation matches the reviewed path, literal, replacement, mutator and an unambiguous relocated span | Bind the current identity to the verified source and preserve the historical evidence. |
| Changed expression or unmatched identity | A reviewed expression changed or has no unique current native match | Replay an identified current control or record the retirement with exact evidence. Keep an unresolved identity blocked. |
| New or mismatched problem mutant | A current report contains an unreviewed problem or altered identity | The existing admission check refuses it until reviewed and covered by an explicit disposition. |

### Approved successor ledger

On 2026-11-28 the user authorized a successor ledger and validator update that preserves historical receipts and checks current native identities. The pinned runner assigned the supplemental historical `12812` mutation to current `13362`. Four historical mutations now correspond to two shared helper mutations. Keep the historical ledger and control catalog byte-for-byte. Bind the successor ledger to them and to an exact reviewed current source and report pair. Each historical row must have a current binding or an explicit evidenced retirement. Validate current IDs, source, spans, status, controls, and aggregate totals. Preserve the existing native report, threshold, and provenance gates.

### Approved publication scanner repair

On 2026-11-28 the user authorized the exact xUnit cancellation-token declaration to be recognized in the publication scan while keeping all credential checks. Apply the change to the shared scan used by preparation and strict validation. Recheck the retained original artifacts through the existing publication path. The first UTC observation after the answer was `2026-11-28T12:24:05Z`.

</frozen-after-approval>

## Code Map

- `eng/check-mutation-triage.py` reads locked source bytes, confirms UTF-16 spans, joins the ledger, control catalog and aggregate manifest, and checks each report against the current native IDs and status limits.
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Mutation/` holds `mutation-problem-followups.json`, `mutation-regression-evidence.json` and `mutation-target-manifest.json`. Use the narrow metadata update pattern in `3a2a9d46ce86ff3e2f4ad3caed2314ed5c8b2650` and `0042b5d0985b192da526deb56da731a7537f5003` for identities proven unchanged.
- `eng/stryker_compat.py` supplies `prepare`, `run` and `verify` against a clean exact commit. `tests/eng/test_mutation_triage.py` exercises verified fixtures and refusal cases.
- Source changes in `31cd7c30acf3146e9cd31b0ae05ca1e164a61dd4` leave ten stale sealed files. Of 483 affected ledger rows, 476 text spans map unambiguously, including all 96 in `AttributeParser.cs`. Seven source expressions in `SchemaFingerprintTransform.cs` changed or moved into a shared encoder (`15214`, `15217`–`15222`). All preserved-assertion file fingerprints still match.

## Tasks & Acceptance

- [x] `eng/stryker_compat.py` — capture both authenticated native reports for the exact committed production inputs in a clean disposable checkout. Retain the reports and their provenance outside the scratch directory using the documented artifact rules.
- [x] `tests/Hexalith.FrontComposer.SourceTools.Tests/Mutation/mutation-problem-followups.json` — compare the entire current report inventory with reviewed rows. Prove each current binding and review any genuinely new problem. Preserve original observations and explicit status restrictions.
- [x] `tests/Hexalith.FrontComposer.SourceTools.Tests/Mutation/mutation-regression-evidence.json` — verify preserved assertions and replay controls where the production expression changed. Keep old execution provenance tied to its original input.
- [x] `tests/Hexalith.FrontComposer.SourceTools.Tests/Mutation/mutation-target-manifest.json` — update the exact ledger digest and preserve reviewed per-segment coverage boundaries. Bind the new aggregates and the successor ledger by file digest.
- [x] `eng/check-mutation-triage.py` — support the successor ledger while keeping the historical validator path for earlier metadata, verify complete historical coverage and current identity bindings, and fail closed on malformed or incomplete data.
- [x] `tests/eng/test_mutation_triage.py` — cover successor admission, ID collisions, historical coverage, and stale or malformed evidence. Run the validator against both current reports, including exact source and identity checks.

Given authenticated reports on the exact current source and reviewed dispositions, when the admission check reads both reports and the successor ledger, then it admits only the reviewed identities and statuses. Given a mismatched report or unknown problem identity, when that check runs, then it refuses admission. Native thresholds and report provenance must also pass independently before Quality can be claimed.

## Implementation Notes

The native replay uses clean commit `3297cbdc559f28a3250c97b963a820e06cd1700a`. While it ran, `main` advanced to `8935f22de016918aa864697c5cbb3237b01557dd`, which changed only the three legacy mutation metadata files. An exact committed-object comparison confirmed preservation of all 829 historical IDs, observations, allowed statuses and actions, all 95 executed and preserved-control records, both original report hashes, and the native segment configuration references. Keep both commits intact. Bind new native receipts to the checkout that emitted them and the successor ledger to the committed legacy ledger it preserves.

The successor checker validates historical identity records and current native identities separately. Current records cover each historical ID once, through a verified current binding or a reasoned retirement. Controls reused for a relocated mutation must still match their current assertion source. Replayed controls bind current test sources and a retained result. The current aggregate manifest must describe exactly the permitted current identities and statuses.

## Spec Change Log

- 2026-11-28: The user authorized separating historical and current identities after the exact native generation trace confirmed ID reassignment and shared helper mutations. Preserve exact historical metadata, reviewed status restrictions for carried identities, and independent native gates. Support explicit retirement so a historical identity need not occupy an unrelated current native ID.

## Review Triage Log

| Finding | Verdict and route | Evidence |
| --- | --- | --- |
| Blind 1 | High, patched | The loader accepted a rehashed unrelated replacement for deferred native ID `12384`. Carried non-kill dispositions require the old path, literal, replacement and mutator. |
| Blind 2 | High, patched | Swapping the history of exact-text natives `12728` and `12816` passed. Span corrections require structured exact crosswalk bindings. |
| Blind 3 | Medium, patched | The fixture accepted only a digest-shaped value and locator. Current controls now bind result and review receipt digests to the current identity and test sources. Locally retained bytes are checked when present. |
| Blind 4 | Medium, patched | Both reviewed report entries originally permitted missing or unchecked provenance fields. The successor validates the recorded original, published, private-build and capture digest fields and checks available retained files. |
| Blind 5 | Medium, patched | The format accepted the all-zero commit. Source and archive commitments require nonzero full IDs. Exact reviewed checkout provenance was independently verified for both reports. |
| Blind 6 | Medium, patched | The archived catalog digest field was unused. Bind it to the digest already authenticated against the archived catalog. |
| Blind 7 | Medium, patched | Retirement originally required two text fields. It now checks the historical row, source fingerprint, both reviewed report hashes and crosswalk evidence. |
| Blind 8 | High, patched | Passing only segment rows skipped validation of 146 known Happy-path occurrences and 11 known Error-handling occurrences. The entire current ID index is now checked. |
| Blind 9 | Medium, recorded retention limit | Authenticated artifacts and their digests are retained locally. Clean checkouts validate current sources and their own reports. Durable remote retention would require separate authorized work under the evidence-retention rules. |
| Blind 10 | Low, patched documentation | The strict audit records both pre-normalization and validated report digests. Document the reviewed publication digest and the finite audit's actual input bytes separately. |
| Blind 11 | Medium, preserved committed history | The `8935f22d` retarget is an intervening committed change. Preserve it and the preceding full Git snapshot. The current `15222` lineage is bound to the original legacy behavior and current `15851` replay. |
| Blind 12 | Medium, patched | Python JSON default duplicate keys could replace a field silently. Metadata loading refuses conflicting duplicate properties. |
| Edge 1 | High, patched | Known IDs outside historical segment allowances are now validated wherever they appear. Only their required presence and allowed problem statuses depend on segment. |
| Edge 2 | High, patched | A historical allowance can only carry to its reviewed expression. A correction requires a binding to the exact reviewed crosswalk. |
| Edge 3 | Medium, patched | Replayed kill controls bind the current mutation, assertion hashes, result digest and review receipt. Available retained bytes must match. |
| Edge 4 | Medium, patched | Retirement metadata is validated against the archived row and both reviewed report bindings. |
| Edge 5 | Medium, patched | Embedded HTML source can end immediately after the exact C# declaration. Its closing JSON string boundary is now covered alongside literal and escaped line endings. |
| Edge 6 | Medium, preserved committed history | The earlier legacy data remains reachable at `3297cbdc`, and the intervening `8935f22d` committed files remain intact. The successor records fresh controls against the exact current native bindings. |
| Edge 7 | Medium, recorded gate refusal | The 69 fresh IDs are reviewed as follow-up candidates and remain outside the approved allowances. Nightly admission remains blocked by the 94 refused observations. |
| Verification 1 | Medium, patched | The successor fixtures no longer exercised legacy dispatch. The focused legacy test checks both acceptance and refusal of a mismatched aggregate. |
| Verification 2 | High, patched | Changing a reviewed Happy-path occurrence of `12389` to `Ignored` escaped the filter. CLI coverage now checks a reviewed occurrence outside its allowance segment. |
| Verification 3 | High, patched | Swapping `12389` and `12484` historical bindings admitted unrelated expressions with equal status policy. Carried identity and span checks refuse the swap. |

## Verification

Run `run-with-temp env PYTHONDONTWRITEBYTECODE=1 python3 -m unittest tests/eng/test_mutation_triage.py`, the authenticated segment and report checks in the mutation how-to, and `git diff --check`.

Local verification against the working tree at `8935f22de016918aa864697c5cbb3237b01557dd` passed 36 tests in `tests/eng/test_mutation_triage.py` and 15 tests in `tests/eng/test_mutation_artifact_publication.py`. Story artifact validation and the whitespace check passed. The authenticated native replay is running independently on the exact production and test inputs at `3297cbdc559f28a3250c97b963a820e06cd1700a`.

### Error-handling replay

The authenticated Error-handling segment at `3297cbdc559f28a3250c97b963a820e06cd1700a` completed 2,459 active mutations and passed its 60% threshold with 76.04%. Private provenance verification passed. The artifact scanner refused both report formats because the report embeds the tracked C# declaration `CancellationToken token = TestContext.Current.CancellationToken;`. The original reports remain in private scratch. The original JSON SHA-256 is `b2812867893ca0814f1ed1c1f32bdafeb4dc536d3dfd371e92a54c2451cfd0d4`. [Native validation summary](../../artifacts/mutation/replay-3297cbdc-20261128T085616Z-prerequisites/error-handling-native-validation-summary.json)

The read-only comparison of that private JSON found 815 distinct current IDs for 817 applicable historical bindings, including two shared bindings. No matched identity exceeded its preserved status allowance, and historical `13939` has no exact current native candidate. The intermediate legacy reseal's 15 differing span bindings have exact native matches at their preserved contexts. It also pointed `15222` at a framed sort expression. The replayed legacy sort `15851` was killed, and the distinct framed sort `15853` survived. [Private-report comparison summary](../../artifacts/mutation/replay-3297cbdc-20261128T085616Z-prerequisites/error-handling-private-crosswalk-summary.json) · [Intermediate legacy comparison](../../artifacts/mutation/replay-3297cbdc-20261128T085616Z-prerequisites/error-handling-intervening-legacy-comparison.json)

The segment found 60 fresh problems outside historical allowances, covering 53 survivors, six no-coverage cases and one timeout. The derivative review groups them by behavior with owners and bounded follow-ups. Admission for these identities remains closed. Happy-path is still running. [Fresh problem review](../../artifacts/mutation/replay-3297cbdc-20261128T085616Z-prerequisites/error-handling-fresh-problem-review.json)

After the owner authorized the narrow scanner fix, the existing `prepare-mutation-artifacts.ps1 -Segment error-handling` accepted the preserved originals in a separate private staging tree. The publication fixture suite passed 16 tests, including both report formats and refusal of literal token values and other token assignments. The published JSON digest is `a5a655273c5b9a621cd605d1bbbf688c242fc5bebe0fd63f06f9baa803a28c07`. The published HTML digest is `87bf8329d5b6ce1378433f01fef57bb4702d07579338ddde1eb52199721f337b`. The native source checkout and independently verified execution receipt were preserved. [Staged publication receipt](../../artifacts/mutation/replay-3297cbdc-20261128T085616Z-prerequisites/error-handling-approved-policy-publication.json) · [Digest binding](../../artifacts/mutation/replay-3297cbdc-20261128T085616Z-prerequisites/error-handling/publication-digests.json)

### Published pair and successor activation

Both complete local native reports are bound to FrontComposer `3297cbdc559f28a3250c97b963a820e06cd1700a`. Happy-path finished 2,490 active mutations at 80.89% against its 80% threshold. Its local elapsed time was `06:12:16.1442802`, longer than the hosted job's 360-minute limit. Error-handling finished 2,459 active mutations at 76.04% against its 60% threshold. Both exact runtime receipts and publication digest bindings verified. The initial combined verifier adapter failed to pass its scanner variable to the child script. The corrected repeat independently reproduced all 220 runtime file hashes, reverified both original receipts, and passed paired publication-lineage verification. The strict report validator then refused one extra timeout in each segment. [Strict validation errors](../../artifacts/mutation/replay-3297cbdc-20261128T085616Z-prerequisites/combined-strict-verification-retry/mutation-validation-errors.txt)

The working-tree manifest now selects `mutation-current-identities.json`, SHA-256 `013552a68620134ca34e866272dd183352e95e4f03fc23ba2ecd33ca2d6ef12d`. The new ledger binds 828 historical rows to 826 current identities and records one exact retirement (`13939`). Both reports agree on the native IDs and identities. The retired row had no exact current native match. The historical ledger and control catalog remain byte-for-byte at their committed `8935f22de016918aa864697c5cbb3237b01557dd` versions. All existing segment, threshold and target configuration values were preserved. User changes advanced `main` to `1df797a868fc6c5fe29c6610ae91d1e1f6cd234d` during replay, including package-validation baseline and diagnostic test changes. The mutation targets, legacy metadata and preserved assertion hashes stayed identical. The replay evidence retains its exact earlier commit.

From a disposable `1df797a8` checkout with the working checker and successor metadata, `python3 eng/check-mutation-triage.py` loaded the successor, found all 680 Happy-path and 815 Error-handling reviewed bindings, and exited 1 for exactly 94 fresh observations across 69 native IDs (34 Happy-path, 60 Error-handling, 25 shared). These are the only finite admission refusals. The intact gate requires an approved disposition or verified corrective work for each new problem. [Working-tree admission audit](../../artifacts/mutation/replay-3297cbdc-20261128T085616Z-prerequisites/local-working-tree-finite-admission.json) · [Error-handling fresh problem review](../../artifacts/mutation/replay-3297cbdc-20261128T085616Z-prerequisites/error-handling-fresh-problem-review.json) · [Happy-only review](../../artifacts/mutation/replay-3297cbdc-20261128T085616Z-prerequisites/happy-only-fresh-problem-review.json)

This is a local mutation repair and fail-closed check. Nightly acceptance and hosted timing remain pending. Neither Story 16.3 qualification nor G-3 migration approval follows from these results.

The combined current-tree Python command `run-with-temp env PYTHONDONTWRITEBYTECODE=1 python3 -m unittest tests/eng/test_mutation_triage.py tests/eng/test_mutation_artifact_publication.py` passed 52 tests in 131.065 seconds. Story-artifact and whitespace checks passed.

After review repairs, the same combined Python command passed 63 tests in 133.363 seconds. The final active successor digest and all 15 span corrections matched the retained evidence. Rechecking both published reports with the final checker again exited 1 for only the same 94 fresh observations, across 69 IDs. The code review findings are recorded above. The repair remains in review while the fresh mutation dispositions and nightly acceptance are open.
