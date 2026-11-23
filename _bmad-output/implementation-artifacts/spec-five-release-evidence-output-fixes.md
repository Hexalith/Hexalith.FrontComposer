---
title: 'Validate release evidence inputs and protect report writes'
type: 'bugfix'
created: '2026-11-23'
status: 'done'
route: 'oneshot'
review_loop_iteration: 0
baseline_commit: 'd5cb718362a76a2cd572d0df84d5a8712667440a'
context: []
---

<frozen-after-approval reason="Five open release evidence CLI and report findings">

## Intent

Close DW-1482, DW-1496, DW-1524, DW-1534, and DW-1550 in FrontComposer's release evidence helper.

- Given an incident output path that names a directory, return an actionable `--output` diagnostic and preserve its contents.
- Given an absolute checksum pattern, report the invalid `--pattern` and preserve any prior checksums. Relative patterns continue to produce the same sorted result.
- Given a current budget manifest, validate the types of its tag and package list. A nonblank manifest tag and a present package list still take precedence over their CLI fallbacks.
- Given an append with no nonblank effective tag, report the missing tag before writing the summary. Existing historical rows remain readable.
- Given simultaneous JSON writes to the same output, each write stages independently and the destination contains a complete report from one writer. A failure leaves the previous report intact and removes that writer's staging files.

</frozen-after-approval>

## Implementation Notes

`eng/release_evidence.py` validates patterns before globbing and checks the incident destination before reading or writing evidence. Budget appends use the manifest tag and package list when supplied and valid. An absent manifest or omitted fields preserve the CLI fallbacks, and existing history keeps its original read behavior.

JSON reports stage in an exclusively created sibling file. This preserves atomic replacement and the existing output mode under the caller's umask. The test suite coordinates two writers at replacement and checks that a simulated replacement failure preserves the prior report and leaves no staging files.

## Review Triage Log

- Medium, deferred. The placeholder's incident check and write are separate operations. A controlled interleaving on the pulled baseline confirmed that a placeholder can overwrite a real incident created between those operations.
- Medium, fixed. A staging directory required owner search permission under the caller's umask. Exclusive sibling files keep the prior file creation behavior. The POSIX regression verifies the final mode.
- Medium, fixed. An unrestricted checksum glob could match the new staging directory and try to open it as a file. The writer creates a sibling file instead of a directory.
- Low, deferred. A missing path passed through the optional budget manifest flag already selects CLI fallbacks. Changing that contract requires checking callers that may use it when there is no manifest to consume.
- Medium, fixed. Absolute Windows drive and UNC patterns could reach the Linux glob implementation and replace the prior report with an empty result. Both are rejected before globbing.
- Low, fixed. An incident path ending in a separator could be normalized to a regular filename. The CLI now recognizes an explicit directory path before normalization can change its meaning.

## Verification

- The CLI and concurrent-write tests reproduced all five findings on the pulled baseline.
- `python3 -m unittest discover -s tests/eng -p 'test_release_*.py'` passed all 98 tests with the workspace .NET 11 SDK and writable .NET runtime and data directories.
- `python3 -m unittest discover -s tests/eng -p test_pack_release_packages.py` passed all 56 tests in the same environment.
- `python3 eng/validate-story-artifacts.py` passed. The committed dependency policy validated all 76 edges on the pulled baseline.
- `aspire run` exited 127 because the Aspire CLI is unavailable in this workspace. The modified entry points run through the standalone release-evidence CLI tests.
