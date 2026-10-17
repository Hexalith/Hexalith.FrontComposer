# Third parent verification — retained failed evidence

Source `b7a5fd4a44d8108e8fdb81d3f471077fa20127be943cdba714693e4b685f6632`, run `gap-root-review-2c48946e2441`: browser 529 passed, 12 preserved visual failures, two existing skips. This is not the final successful evidence packet. [Final follow-up](final/report.md) is separate.

[State validator](state-validation.json) finds four incomplete required inventories in every engine: rejected lifecycle icons (both themes) and policy-denied control boundaries (both themes). Clipping correctly excludes unseen paint, but these two fixture capture calls did not inspect that paint in view. All raw/curated engine gates fail; no artifact pass is claimed. [Artifact results](artifact-validation.json), [source](source.json), [browser outcomes](browser-verification.json), [execution](third-run-verification.json) and [lossless raw/HTML archive](final-run-retention.json) preserve this attempt. Derived browser outcome summary only updates its execution-record link; [original summary bytes](third-original-browser-verification.json) are retained.

Earlier failed diagnostics and all original images remain retained. Do not combine this run's fragments with the final follow-up to claim a complete attempt.
