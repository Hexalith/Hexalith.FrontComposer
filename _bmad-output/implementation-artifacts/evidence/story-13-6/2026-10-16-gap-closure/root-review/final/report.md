# Historical fourth parent verification

This candidate remains failed: **526 passed, 15 failed, 2 skipped** in complete run `gap-root-review-13af441b28ac` on source `bffa6d8b1fb4bd869dd0d9cafa85e531fb38970466a488d3a58d6c11ab70ac76`.

The twelve unchanged Firefox/WebKit screenshot failures remain open. Firefox additionally timed out in two tests, including context teardown; those outcomes remain failed and no environmental cause is asserted. WebKit also sampled a detached selected palette row during the 150 ms query debounce. The trace proved its selected-visible guard passed against the old Commands row before replacement results committed. P07/G19 corrects this synchronization while preserving the strict cue check.

G18's required inventory gaps are resolved in Chromium; Firefox/WebKit state validation remains rejected by the owning failed tests and the aborted WebKit toolbar capture. This packet is historical failure evidence, not the final candidate's verification.

[Source](source.json), [execution](execution.json), [browser outcomes](browser-verification.json), [state audit](state-validation.json), [artifact gates](artifact-validation.json) and [all original bytes](final-run-retention.json) preserve the complete attempt. [Later verification](../verified/report.md) is recorded separately.
