---
title: 'Preserve release evidence during local retries and incomplete runs'
type: 'bugfix'
created: '2026-11-23'
status: 'done'
route: 'oneshot'
review_loop_iteration: 0
baseline_commit: 'fa32fb6c795db770bc173c3f461d7efc2e95c82f'
context: []
---

<frozen-after-approval reason="Five deferred release-evidence CLI findings">

## Intent

Make the release evidence helper preserve existing reports and explain incomplete inputs correctly.

- Given an incident and a placeholder writing to the same path, the real incident survives. Repeating a recognized placeholder write is idempotent, and unrecognized existing evidence is preserved.
- Given an explicitly supplied missing budget manifest, append using the established CLI tag and package count and report the fallback. Omitting the option keeps the fallback silent.
- Given a budget manifest with packages and no tag, use the CLI tag and the manifest's package count. Pin that combination with a CLI regression for DW-1527.
- Given a malformed budget manifest, report that the budget is unavailable, preserve any existing summary, and optionally write a typed skip record for DW-1552.
- Given an explicit local invocation with a run attempt greater than one, report the local context and explain the invalid retry input for DW-1474. Preserve hosted retry, real incident, fork, and publish-authorization behavior.

</frozen-after-approval>

## Implementation Notes

`eng/release_evidence.py` stages the placeholder in the target directory and links it into place only if the destination is absent. The real incident writer still replaces atomically. `eng/release_prepublish.py` delegates directly to that writer so failure leaves any prior report visible.

An explicit missing budget manifest uses CLI defaults and emits a diagnostic on stderr. A manifest that cannot be read as JSON prevents the append and can emit the existing budget-unavailable record at a separate path. The CLI regression independently checks the tag fallback and manifest package count.

Local attempts above one retain a blocking contextual diagnostic. Fork and partial-publication precedence and hosted rerun classification are covered by the CLI regression.

The helper moves to 4.3.2. The release evidence contract binds the helper version to the fallback approval digest.

## Review Triage Log

- Medium, fixed. The helper version now advances with the observable CLI behavior so prior fallback approvals cannot carry forward silently.
- Medium, fixed. A path beneath a non-directory retains absent-manifest fallback, as does an explicitly empty path. CLI coverage checks both cases.
- Medium, fixed. A failed marker write reports both the manifest read failure and the marker destination failure. The summary and manifest are preserved.
- Low, documented and verified. A producer that needs a skip record for the current invocation can supply a fresh destination and use the exit status. A corrected retry preserves the previous marker as evidence and updates the summary.
- Medium, fixed. A placeholder collision refuses non-regular destinations before attempting to read them. The named pipe regression checks the entry survives.
- Medium, fixed. Idempotent success requires the existing report to contain the stable placeholder fields with the expected values. Incomplete records are preserved and reported.
- Medium, deferred. The restored-manifest verifier already used a phase the incident CLI refuses. The ledger now records that producer/consumer mismatch for separate work.
- Medium, fixed. A failed incident writer logs its failure and leaves the previous report intact. The regression checks the exact log and preserved bytes.

## Verification

- `python3 -m unittest discover -s tests/eng -p 'test_release_*.py'` passed all 110 tests, and `python3 -m unittest discover -s tests/eng -p test_pack_release_packages.py` passed all 56. Both ran in an isolated checkout using the committed Builds pin and the workspace .NET 11 SDK.
- The first run against the refreshed reference checkout reproduced a catalog mismatch. Its Builds checkout selected EventStore 5.5.1 while standalone FrontComposer expects 5.4.0. The isolated pinned checkout resolved that mismatch.
- `python3 eng/validate-story-artifacts.py`, the staged file policy check, and the pinned commitlint check passed.
- `aspire run` exited 127 because the Aspire CLI is unavailable in this environment. The changes are exercised through the standalone release tools.
