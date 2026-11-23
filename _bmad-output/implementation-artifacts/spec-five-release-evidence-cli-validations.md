---
title: 'Validate release evidence CLI inputs'
type: 'bugfix'
created: '2026-11-23'
status: 'done'
route: 'oneshot'
review_loop_iteration: 0
baseline_commit: '00513bf77e3dbbd7a543210599fbb6f38681630e'
context: []
---

<frozen-after-approval reason="Five open release evidence CLI findings">

## Intent

Close DW-1466, DW-1468, DW-1478, DW-1479, and DW-1480. Report an invalid manifest artifact root once before processing the manifest. Reject simultaneous live-root and offline verification flags. Report which release-budget timestamp is invalid, reject an end time earlier than its start, and identify the history row containing an invalid package count. Keep successful live and offline verification and budget summaries compatible. Exercise each failure through the release evidence CLI.

</frozen-after-approval>

## Implementation Notes

The manifest CLI validates an explicitly supplied artifact root before reading the evidence and uses the existing exit 1 and JSON or stdout reporting contract. The direct manifest validator short-circuits on an invalid root. The parser rejects conflicting verification modes with its standard usage diagnostic.

Budget timestamps retain UTC offsets and support paired offset-free timestamps. Explicit empty timestamps and mixed offsets produce input diagnostics. A reversed duration fails before writing a summary. History retains integer strings, whole-valued JSON numbers and the zero default for missing counts. Invalid counts identify the one-based row without printing its contents or replacing an existing summary.

The regressions use the real CLI and the existing Python release evidence test lane.

## Review Triage Log

- Medium, fixed overlong path handling in the direct manifest validator and CLI.
- Medium, fixed explicit empty start timestamps to name the argument. Date-only values also receive a timestamp diagnostic.
- Medium, fixed offset validation so impossible offset components cannot be normalized into valid values.
- Medium, preserved whole-valued historical JSON counts and rejected fractional counts.
- Low, named `--package-count` when a negative count comes from that flag.
- Low, added positive elapsed durations for offset-aware and paired offset-free inputs.
- Low, recorded all five closures in the deferred-work ledger.

## Verification

- The pre-change CLI regressions reproduced the five deferred findings. The final focused run passed all seven tests.
- `python3 -m unittest discover -s tests/eng -p 'test_release_*.py'` passed all 90 tests with the workspace .NET 11 SDK and writable runtime directory.
- `python3 -m unittest tests/eng/test_pack_release_packages.py` passed all 56 tests in the same environment.
- The existing offline manifest and three-breach budget fixtures passed through the CLI.
- `aspire run` exited 127 because Aspire CLI is unavailable in the workspace. The changes are in the standalone release evidence helper.
