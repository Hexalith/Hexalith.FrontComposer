---
title: 'Fix AppHost MSBuild Result Output Boundary'
type: 'bugfix'
created: '2026-09-19'
status: 'in-progress'
route: 'oneshot'
review_loop_iteration: 0
context: []
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** The authenticated AppHost smoke requests a large, authoritative MSBuild evaluation document, but the generic command boundary retains only the final 1 MiB of stdout. Current GitHub-hosted output exceeds that bound, discards the JSON prefix, and fails Gate 2c as `apphost-msbuild-output-invalid` before any credential-bearing or host-start operation.

**Approach:** Redirect MSBuild `-getProperty`/`-getItem` results into an exclusive temporary result file, validate that file through a bounded duplicate-safe JSON read, and preserve bounded process output only for redacted diagnostics. Reject missing, malformed, symlinked, changed-during-read, or oversized result files and cover the live large-output regression without relaxing exact source-graph validation.

</frozen-after-approval>

## Implementation Notes

- Measured the exact AppHost `ResolveReferences` get-result document at 1,473,568 bytes with 352 `ReferencePath` items; the former 1,048,576-character stdout tail cannot contain the authoritative JSON object.
- Added an 8 MiB bounded, duplicate-safe MSBuild result-file boundary backed by an exclusive temporary directory. Process stdout remains bounded and is used only for redacted diagnostics.
- Updated injectable test runtimes to honor `-getResultOutputFile`, added the large-output regression and missing/malformed/symlinked/oversized rejection cases, and pinned the result-file/bound contract in governance source assertions.
- Verified the same patch against an isolated detached clone of current `origin/main` at `4b5a57cacc270ac28d334c659b1a36f87096fdce`; focused current-origin tests pass. Full capture remains unavailable locally because its runtime-input authority intentionally rejects the permitted sibling symlinks and nested dependency materialization is prohibited.
